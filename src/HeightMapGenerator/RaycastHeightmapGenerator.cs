using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
        private static float postMinHeight = 0;
        private static float postMaxHeight = 0;
        private static float terrainScale = 0;
        private static float terrainHalf = 0;
        private static float terrainSizeX = 0;
        private static float terrainSizey = 0;
        private static Vector3 terrainCenter;
        private static float terrainSeaLevel;


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
                terrainSeaLevel = Datum.LocalSeaY;
                //naval: terrainSeaLevel: -265.3623
                //heartland: terrainSeaLevel: -530.8
                Plugin.Logger?.LogInfo($"Datum.originPosition IS AT: {terrainCenter.x},{terrainCenter.y}");
                
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
                Plugin.Logger?.LogInfo($"terrainSeaLevel: {terrainSeaLevel.ToString()}");
                //call Heightmap generator
                GenerateRayCastHeightmap();
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error exporting heightmap: {ex.Message}\n{ex.StackTrace}");
            }
        }
        //RayCasts over the entire terrain. sample rate controlled by metersPerRay, output size is controlled by textureSize
        private static float[,] RayCastTerrain()
        {
            float[,] heights = new float[textureSize, textureSize];
            int oldPosX = -2;
            int oldPosZ = -2;
            int posX = -1;
            int posZ  = -1;
            Plugin.Logger?.LogInfo($"Generating Heightmap...");
            for (int z = (int)(-1 * terrainHalf); z < terrainHalf; z += metersPerRay)
            {
                Plugin.Logger?.LogInfo($"{((z + terrainHalf) / terrainSize) * 100}%");
                if (z + metersPerRay > terrainSize - 1) { z = (int)(terrainSize - 1); }
                for (int x = (int)(-1 * terrainHalf); x < terrainHalf; x += metersPerRay)
                {
                    if (x + metersPerRay > terrainSize - 1) { x = (int)(terrainSize - 1); }
                    posX = (int)((x + (terrainHalf)) * terrainScale);
                    if (posX > textureSize - 1) { posX = textureSize - 1; }
                    posZ = (int)((z + (terrainHalf)) * terrainScale);
                    if (posZ > textureSize - 1) { posZ = textureSize - 1; }

                    Vector3 rayStart = new Vector3(x + terrainCenter.x, 5000f, z + terrainCenter.z);
                    RaycastHit hit;
                    if (oldPosX != posX && oldPosZ != posZ) {
                        if (Physics.Raycast(rayStart, Vector3.down, out hit, 10000f, 1 << 6))
                        {
                            heights[posZ, posX] = 0.1f + (hit.point.y / 10000);
                            if (heights[posZ, posX] < minHeight) { minHeight = heights[posZ, posX]; }
                            if (heights[posZ, posX] > maxHeight) { maxHeight = heights[posZ, posX]; }
                        }
                        else
                        {
                            heights[posZ, posX] = Int16.MinValue;
                        }
                    }
                    oldPosX = posX;
                }
                oldPosZ = posZ;
            }
           
            float[,] blurred = GaussianBlur(heights, radius: 5, sigma: 1.0f);
            return blurred;
        }
        //resizes a texture
        //currently unused
        //will need it for future RayCastCommand Batching refactor
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

        private static float[] CreateGaussianKernel(int radius, float sigma)
        {
            float[] kernel = new float[2 * radius + 1];
            float sum = 0;
            for (int i = -radius; i <= radius; i++)
            {
                float value = (float)(Math.Exp(-(i * i) / (2 * sigma * sigma)));
                kernel[i + radius] = value;
                sum += value;
            }

            // Normalize kernel
            for (int i = 0; i < kernel.Length; i++)
                kernel[i] /= sum;

            return kernel;
        }

        private static float[,] GaussianBlur(float[,] input, int radius, float sigma)
        {
            int size = input.GetLength(0); // Assuming square array
            float[] kernel = CreateGaussianKernel(radius, sigma);
            float[,] temp = new float[size, size];
            float[,] output = new float[size, size];

            // Horizontal pass
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float sum = 0;
                    for (int k = -radius; k <= radius; k++)
                    {
                        int ix = Math.Clamp(x + k, 0, size - 1);
                        sum += input[y, ix] * kernel[k + radius];
                    }
                    temp[y, x] = (sum);
                }
            }

            // Vertical pass
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float sum = 0;
                    for (int k = -radius; k <= radius; k++)
                    {
                        int iy = Math.Clamp(y + k, 0, size - 1);
                        sum += temp[iy, x] * kernel[k + radius];
                    }
                    output[y, x] = (sum);
                }
            }

            return output;
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
                }
            }
            
            tex.Apply();
            texOut = rotateTexture(tex, true);
            texOut.Apply();
            return texOut;
        }
        // main function to trace the game world and output assembled Heightmap
        private static void GenerateRayCastHeightmap()
        {
          
            string outputDir = Path.Combine(BepInEx.Paths.PluginPath, "NOBlackBox_RayCast_HeightmapExports");
            Directory.CreateDirectory(outputDir);
            float[,] heightMapTile = RayCastTerrain();
            Texture2D heightMap = GenerateHeightmapTexture(heightMapTile);
            heightMap.Apply();

            Plugin.Logger?.LogInfo($"minHeight: {minHeight.ToString()}, maxHeight: {maxHeight.ToString()}");
            
            string filenameRaw = $"NOBlackBox_heightmap_{MapSettingsManager.i.MapLoader.CurrentMap.Path}.data";
            string outputPathRaw = Path.Combine(outputDir, filenameRaw);
            byte[] rawBytes = heightMap.GetRawTextureData();
            File.WriteAllBytes(outputPathRaw, rawBytes);
            Plugin.Logger?.LogInfo($"Heightmap RAW exported to: {outputPathRaw}");
            
        }

    }
}
