using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NOBlackBox
{
    internal static class HeightMapGenerator
    {
        private static AssetBundle noBlackBoxBundle = AssetBundle.LoadFromFile(BepInEx.Paths.PluginPath + "/NOBlackBox/noblackbox");
        private static Shader vertexYToColorShader = noBlackBoxBundle.LoadAsset<Shader>("NOBlackBox_VertexYToColor");
        private static Material vertexYToColorShaderMat = new Material(vertexYToColorShader);
        private static int patchSize = 0;
        private static float terrainSize = 0;
        private static int raysPerKilometer;
        private static float minHeight = 0;
        private static float maxHeight = 0;
        private static int tileSize = 0;
        private static int tileCount = 0;
        public static void ExportCustomTerrainHeightmap()
        {
            //do checks
            if (null == MissionManager.CurrentMission)
            {
                Plugin.Logger?.LogInfo("No Terrain found. In order to use this feature, you must launch a mission first.");
            }
            try
            {
                Plugin.Logger?.LogInfo("Attempting to export custom terrain heightmap");
                // Get terrain dimensions from the static TerrainGrid class
                if (TerrainGrid.terrainSize.y >= TerrainGrid.terrainSize.x)
                {
                    terrainSize = TerrainGrid.terrainSize.y;
                }
                else
                {
                    terrainSize = TerrainGrid.terrainSize.x;
                }
                if (terrainSize == 0f)
                {
                    return;
                }
                patchSize = 8192;
                tileCount = (int)(terrainSize / patchSize);
                //tileSize = (int)(MathF.Round((8192 / tileCount), 0, 0));
                tileSize = 512;
                Plugin.Logger?.LogInfo($"terrainSize: {terrainSize.ToString()}");
                Plugin.Logger?.LogInfo($"patchSize: {patchSize.ToString()}");
                Plugin.Logger?.LogInfo($"tileSize: {tileSize.ToString()}");
                //call Heightmap generator
                //RayCastHeightmap();
                GenerateHeightMapWithShader();
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error exporting heightmap: {ex.Message}\n{ex.StackTrace}");
            }
        }
        //casts 1 Physics.Raycast per meter, returns a 2d array of y coordinates per each RaycastHit
        private static float[,] RayCastHeightmapTile(int posX, int posZ)
        {
            float[,] heights = new float[patchSize, patchSize];
            for (int z = 0; z < patchSize; z++)
            {
                for (int x = 0; x < patchSize; x++)
                {
                    int posX_ = posX + x;
                    int posZ_ = posZ + z;
                    
                    Vector3 rayStart = new Vector3(posX_, 5000f, posZ_);
                    RaycastHit hit;
                    if (Physics.Raycast(rayStart, Vector3.down, out hit, 10000f, 1 << 6))
                    {
                        heights[z, x] = 0.1f + (hit.point.y / 10000);
                        if (heights[z, x] < minHeight) { minHeight = heights[z, x];}
                        if (heights[z, x] > maxHeight) { maxHeight = heights[z, x];}
                    } else
                    {
                        heights[z, x] = Int16.MinValue;
                    }
                    
                    //heights[z, x] = 0.1f + ((x + z) / 10000);
                }
            }
            return heights;
        }
        //resizes a tecxture
        private static Texture2D ResizeTexture(Texture2D texture2D, int targetX, int targetY)
        {
            RenderTexture rt = new RenderTexture(targetX, targetY, 16);
            RenderTexture.active = rt;
            UnityEngine.Graphics.Blit(texture2D, rt);
            Texture2D result = new Texture2D(targetX, targetY, TextureFormat.RGBA32, false);
            result.filterMode = UnityEngine.FilterMode.Trilinear;
            result.anisoLevel = 16;
            result.ReadPixels(new Rect(0, 0, targetX, targetY), 0, 0);
            result.Apply();
            return result;
        }
        //convert raw height array to Texture2D
        private static Texture2D GenerateTextureSegment(float[,] heights)
        {
            Texture2D tex = new Texture2D(patchSize, patchSize, TextureFormat.RGBA32, false);
            Texture2D texOut;
            tex.filterMode = UnityEngine.FilterMode.Trilinear;
            tex.anisoLevel = 16;
            for (int z = 0; z < patchSize; z++)
            {
                for (int x = 0; x < patchSize; x++)
                {
                    Color col = new Color(heights[x, z], heights[x, z], heights[x, z]);
                    //tex.SetPixel(x, patchSize - z - 1, col);
                    tex.SetPixel(x, z, col);
                }
            }
            tex.Apply();
            texOut = ResizeTexture(tex, tileSize, tileSize);
            texOut.Apply();
            return texOut;
        }
        //adds Heightmap Texture2D tiles to final Texture2D
        private static Texture2D stitchSegmentToFinalHeightmap (Texture2D _finalHeightmap,Texture2D _segment, int x_, int z_)
        {
            int innerX = (int)((x_ / patchSize) * tileSize);
            int innerZ = (int)((z_ / patchSize) * tileSize);
            Plugin.Logger?.LogInfo($"innerX: {innerX.ToString()}, innerZ: {innerZ.ToString()}");

            Color[] pixels = _segment.GetPixels();
            _finalHeightmap.SetPixels(innerZ,innerX,tileSize,tileSize,pixels);
            _finalHeightmap.Apply();
            return _finalHeightmap;

        } 
        // main function to trace the game world and output  assembled Heightmap
        //VERY SLOW AND HUNGERS FOR RAM
        private static void RayCastHeightmap()
        {
            
            Texture2D finalHeightmap = new Texture2D((tileSize * tileCount), (tileSize * tileCount), TextureFormat.RGBA32, false);
            finalHeightmap.filterMode = UnityEngine.FilterMode.Trilinear;
            finalHeightmap.anisoLevel = 16;

            string outputDir = Path.Combine(BepInEx.Paths.PluginPath, "NOBlackBox_RayCast_HeightmapExports");
            Directory.CreateDirectory(outputDir);
            for (int z = (int)(0 - (terrainSize / 2)); z < (terrainSize / 2); z = z + patchSize)
            {
                for (int x = (int)(0 - (terrainSize / 2)); x < (terrainSize / 2); x = x + patchSize)
                {
                    Plugin.Logger?.LogInfo($"X: {x.ToString()}, Z: {z.ToString()}");
                    Plugin.Logger?.LogInfo($"Generating segment...");
                    float[,] heightMapTile = RayCastHeightmapTile(x, z);
                    Texture2D segment = GenerateTextureSegment(heightMapTile);
                    int textureZ = (int)(z + (terrainSize / 2));
                    int textureX = (int)(x + (terrainSize / 2));
                    finalHeightmap = stitchSegmentToFinalHeightmap(finalHeightmap, segment, textureX, textureZ);
                    finalHeightmap.Apply();
                    Plugin.Logger?.LogInfo($"minHeight: {minHeight.ToString()}, maxHeight: {maxHeight.ToString()}");
                    string filename = $"NOBlackBox_heightmap_Z-{z}_X-{x}_{MapSettingsManager.i.MapLoader.CurrentMap.Path}.png";
                    string outputPath = Path.Combine(outputDir, filename);
                    byte[] bytes = finalHeightmap.EncodeToPNG();
                    File.WriteAllBytes(outputPath, bytes);
                    Plugin.Logger?.LogInfo($"Heightmap PNG exported to: {outputPath}");
                }
            }
            Plugin.Logger?.LogInfo($"Finished all segments...");
            //finalHeightmap = ResizeTextureSegment(finalHeightmap, 8192, 8192);
            //finalHeightmap.Apply();
            string filenameRaw = $"NOBlackBox_heightmap_{MapSettingsManager.i.MapLoader.CurrentMap.Path}.data";
            string outputPathRaw = Path.Combine(outputDir, filenameRaw);
            byte[] rawBytes = finalHeightmap.GetRawTextureData();
            File.WriteAllBytes(outputPathRaw, rawBytes);
            Plugin.Logger?.LogInfo($"Heightmap RAW exported to: {outputPathRaw}");
            Plugin.Logger?.LogInfo($"minHeight: {minHeight.ToString()}, maxHeight: {maxHeight.ToString()}");
            Plugin.Logger?.LogInfo($"Generating segment...");
        }



        private static Mesh scaleMeshXZToInt(Mesh baseMesh)
        {
            Mesh outputMesh = new Mesh();

            Vector3[] outputVertices = new Vector3[baseMesh.vertices.Length];

            int sizeXint = (int)(Math.Truncate((double)(baseMesh.bounds.size.x)));
            int sizeZint = (int)(Math.Truncate((double)(baseMesh.bounds.size.z)));
            float scaleX = sizeXint / baseMesh.bounds.size.x;
            float scaleZ = sizeZint / baseMesh.bounds.size.z;

            for (int i = 0; i < outputVertices.Length; i++)
            {
                var vertex = baseMesh.vertices[i];
                vertex.x = (int)(vertex.x * scaleX);
                vertex.z = (int)(vertex.z * scaleZ);
                outputVertices[i] = vertex;
            }

            outputMesh.vertices = outputVertices;
            outputMesh.RecalculateNormals();
            outputMesh.RecalculateBounds();
            return outputMesh;
        }

        private static Texture2D PaintHeightmapSegmentTextureFromMesh(MeshFilter meshFilter)
        {
            RenderTexture rt = new RenderTexture(tileSize, tileSize, 32, RenderTextureFormat.ARGB32);
            rt.Create();

            Matrix4x4 rot = Matrix4x4.Rotate(Quaternion.Euler(0, 0, 0)); // 90° around Z
            Matrix4x4 matrix = rot * Matrix4x4.identity;

            UnityEngine.Graphics.SetRenderTarget(rt);
            GL.Clear(true, true, Color.black);
            vertexYToColorShaderMat.SetPass(0);
            UnityEngine.Graphics.DrawMeshNow(meshFilter.sharedMesh, matrix);
            UnityEngine.Graphics.SetRenderTarget(null);

            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(tileSize, tileSize, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, tileSize, tileSize), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            Texture2D texOut = ResizeTexture(tex,tileSize,tileSize);
            return texOut;
        }

        public static void GenerateHeightMapWithShader()
        {
            GameObject[] objs;
            Scene scn = SceneManager.GetActiveScene();
            
            if (scn != null)
            {
                bool foundTerrain = false;
                Plugin.Logger?.LogInfo($"Found SCENE: {scn.name}");
                Plugin.Logger?.LogInfo($"Listing Root GameObjects:");
                objs = scn.GetRootGameObjects();

                foreach ( GameObject obj in objs ) 
                {
                    if (foundTerrain) { break; }
                    Plugin.Logger?.LogInfo($"Current GameObject: {obj.name}");
                    Component[] children = obj.GetComponentsInChildren(typeof(Component));
                    //Plugin.Logger?.LogInfo($"Begin Listing Child Components of: {obj.name}");
                    foreach (Component child in children)
                    {
                        //Plugin.Logger?.LogInfo($"Name: {child.name}, Type: {child.GetType().Name}");
                        if (child.name == MapSettingsManager.i.MapLoader.CurrentMap.Path + "(Clone)")
                        {
                            Plugin.Logger?.LogInfo($"FOUND TERRAIN {child.name}");
                            foundTerrain = true;
                            Plugin.Logger?.LogInfo($"BEGIN LISTING TERRAIN COMPONENTS...");
                            Component[] terrainChildren = child.GetComponentsInChildren<MeshFilter>();
                            foreach (Component terrainChild in terrainChildren)
                            { 
                                try
                                {
                                    //name,sizeX,sizeZ,posX,posZ
                                    Plugin.Logger?.LogInfo($"TERRAIN_DATA"+
                                                           $"{terrainChild.name}," +
                                                           $"{terrainChild.GetComponent<MeshFilter>().sharedMesh.bounds.size.x}," +
                                                           $"{terrainChild.GetComponent<MeshFilter>().sharedMesh.bounds.size.z}," +
                                                           $"{terrainChild.transform.GlobalPosition().x}," +
                                                           $"{terrainChild.transform.GlobalPosition().z}");
                                    if (terrainChild.name.Contains("terrain"))
                                    {
                                        Texture2D currentTex = PaintHeightmapSegmentTextureFromMesh(terrainChild.GetComponent<MeshFilter>());
                                        currentTex.Apply();
                                        if (currentTex != null)
                                        {
                                            string outputDir = Path.Combine(BepInEx.Paths.PluginPath, "NOBlackBox_SHADER_HeightmapExports");
                                            string filename = $"NOBlackBox_{terrainChild.name}_Z-{terrainChild.transform.GlobalPosition().z}_X-{terrainChild.transform.GlobalPosition().z}_{MapSettingsManager.i.MapLoader.CurrentMap.Path}.png";
                                            string outputPath = Path.Combine(outputDir, filename);
                                            byte[] bytes = currentTex.EncodeToPNG();
                                            File.WriteAllBytes(outputPath, bytes);
                                            Plugin.Logger?.LogInfo($"Heightmap Tile PNG exported to: {outputPath}");
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    Plugin.Logger?.LogInfo(e);
                                }  
                            }
                            Plugin.Logger?.LogInfo($"FINISHED LISTING TERRAIN COMPONENTS.");
                            break;
                        }
                    }
                }
            }
            Plugin.Logger?.LogInfo($"FINISHED LISTING ROOT GameObjects.");
        }
    }
}
