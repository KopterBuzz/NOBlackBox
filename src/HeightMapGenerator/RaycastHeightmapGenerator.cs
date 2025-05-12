using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.Text;

namespace NOBlackBox
{
    internal static class RaycastHeightmapGenerator
    {
        private static int textureSize = 4096;
        private static float terrainSize = 0;
        private static int metersPerRay = 4;
        private static float minHeight = 0;
        private static float maxHeight = 0;
        private static float terrainScale = 0;
        private static float terrainHalf = 0;
        private static float terrainSizeX = 0;
        private static float terrainSizey = 0;
        private static Vector3 terrainCenter,playerPosition;


        public static void Generate()
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
                terrainCenter = Datum.originPosition;
                playerPosition = GameManager.LocalPlayer.transform.GlobalPosition().ToLocalPosition();
                Plugin.Logger?.LogInfo($"Datum.originPosition IS AT: {terrainCenter.x},{terrainCenter.y}");
                Plugin.Logger?.LogInfo($"Datum.origin.Position IS AT: {playerPosition.x},{playerPosition.y}");
                
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
                //terrainSize = terrainSize * 2;
                terrainHalf = terrainSize / 2;
                terrainScale = textureSize / terrainSize;
                Plugin.Logger?.LogInfo($"terrainSize: {terrainSize.ToString()}");
                Plugin.Logger?.LogInfo($"textureSize: {textureSize.ToString()}");
                Plugin.Logger?.LogInfo($"terrainScale: {terrainScale.ToString()}");
                //call Heightmap generator
                GenerateRayCastHeightmap();
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error exporting heightmap: {ex.Message}\n{ex.StackTrace}");
            }
        }
        //casts 1 Physics.Raycast per meter, returns a 2d array of y coordinates per each RaycastHit
        private static float[,] RayCastTerrain()
        {
            float[,] heights = new float[textureSize, textureSize];
            Plugin.Logger?.LogInfo($"Generating Heightmap...");
            for (int z =(int)(-1 * terrainHalf); z < terrainHalf; z += metersPerRay)
            {
                Plugin.Logger?.LogInfo($"{((z + terrainHalf) / terrainSize) * 100}%");
                if (z + metersPerRay > terrainSize - 1) { z = (int)(terrainSize - 1); }
                for (int x = (int)(-1 * terrainHalf); x < terrainHalf; x += metersPerRay)
                {
                    if (x + metersPerRay > terrainSize - 1) { x = (int)(terrainSize - 1); }
                    int posX = (int)((x + (terrainHalf)) * terrainScale );
                    if (posX > textureSize - 1) { posX = textureSize - 1; }
                    int posZ = (int)((z + (terrainHalf)) * terrainScale );
                    if (posZ > textureSize - 1) { posZ = textureSize - 1; }

                    Vector3 rayStart = new Vector3(x + terrainCenter.x, 5000f, z + terrainCenter.z);
                    RaycastHit hit;
                    if (Physics.Raycast(rayStart, Vector3.down, out hit, 10000f, 1 << 6))
                    {
                        heights[posZ, posX] = 0.1f + (hit.point.y / 10000);
                        if (heights[posZ, posX] < minHeight) { minHeight = heights[posZ, posX];}
                        if (heights[posZ, posX] > maxHeight) { maxHeight = heights[posZ, posX];}
                    } else
                    {
                        heights[posZ, posX] = Int16.MinValue;
                    }
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
            Texture2D result = new Texture2D(targetX, targetY, TextureFormat.RHalf, false);
            result.filterMode = UnityEngine.FilterMode.Trilinear;
            result.anisoLevel = 16;
            result.ReadPixels(new Rect(0, 0, targetX, targetY), 0, 0);
            result.Apply();
            return result;
        }

        private static Texture2D rotateTexture(Texture2D originalTexture, bool clockwise)
        {
            Color[] original = originalTexture.GetPixels();
            Color[] rotated = new Color[original.Length];
            int w = originalTexture.width;
            int h = originalTexture.height;

            int iRotated, iOriginal;

            for (int j = 0; j < h; ++j)
            {
                for (int i = 0; i < w; ++i)
                {
                    iRotated = (i + 1) * h - j - 1;
                    iOriginal = clockwise ? original.Length - 1 - (j * w + i) : j * w + i;
                    rotated[iRotated] = original[iOriginal];
                }
            }

            Texture2D rotatedTexture = new Texture2D(h, w,TextureFormat.RHalf, false);
            rotatedTexture.SetPixels(rotated);
            rotatedTexture.Apply();
            return rotatedTexture;
        }
        //convert raw height array to Texture2D
        private static Texture2D GenerateHeightmapTexture(float[,] heights)
        {
            Texture2D tex = new Texture2D(textureSize, textureSize, TextureFormat.RHalf, false);
            Texture2D texOut;
            tex.filterMode = UnityEngine.FilterMode.Trilinear;
            tex.anisoLevel = 16;
            for (int z = 0; z < textureSize; z++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    Color col = new Color(heights[x, z], heights[x, z], heights[x, z]);
                    tex.SetPixel(x, z, col);
                    //tex.SetPixel(x, textureSize - z - 1, col);
                    //tex.SetPixel(textureSize - x - 1, z, col);
                }
            }
            
            tex.Apply();
            texOut = rotateTexture(tex, true);
            texOut.Apply();
            return texOut;
        }
        // main function to trace the game world and output  assembled Heightmap
        //VERY SLOW AND HUNGERS FOR RAM
        private static void GenerateRayCastHeightmap()
        {
          
            string outputDir = Path.Combine(BepInEx.Paths.PluginPath, "NOBlackBox_RayCast_HeightmapExports");
            Directory.CreateDirectory(outputDir);
            float[,] heightMapTile = RayCastTerrain();
            Texture2D heightMap = GenerateHeightmapTexture(heightMapTile);
            heightMap.Apply();

            Plugin.Logger?.LogInfo($"minHeight: {minHeight.ToString()}, maxHeight: {maxHeight.ToString()}");

            /*
            string filename = $"NOBlackBox_heightmap_{MapSettingsManager.i.MapLoader.CurrentMap.Path}.png";
            string outputPath = Path.Combine(outputDir, filename);
            byte[] bytes = heightMap.EncodeToPNG();
            File.WriteAllBytes(outputPath, bytes);
            Plugin.Logger?.LogInfo($"Heightmap PNG exported to: {outputPath}");
            */
            
            string filenameRaw = $"NOBlackBox_heightmap_{MapSettingsManager.i.MapLoader.CurrentMap.Path}.data";
            string outputPathRaw = Path.Combine(outputDir, filenameRaw);
            byte[] rawBytes = heightMap.GetRawTextureData();
            File.WriteAllBytes(outputPathRaw, rawBytes);
            Plugin.Logger?.LogInfo($"Heightmap RAW exported to: {outputPathRaw}");
            
        }

    }
}
