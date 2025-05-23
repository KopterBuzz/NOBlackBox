using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

using static MapSettingsManager;
using static UnityEngine.UIElements.StylePropertyAnimationSystem;

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
        private static readonly int STATICS = LayerMask.NameToLayer("Statics");
        private static float SCALE = 0f;
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
                FieldInfo MapInScene = typeof(MapSettingsManager).GetField("mapInScene", BindingFlags.Instance | BindingFlags.NonPublic);
                MapSettings map = (MapSettings)MapInScene.GetValue(MapSettingsManager.i);
                GameObject mapHost = map.gameObject;
                maxHeight =
                    mapHost.GetComponentsInChildren<MeshCollider>()
                    .Where(collider => collider.gameObject.layer == STATICS && collider.gameObject.GetComponents<Component>().Length == 4)
                    .Select(collider => collider.bounds.max.GlobalY())
                    .Max() - Datum.originPosition.GlobalY();
                minHeight =
                    mapHost.GetComponentsInChildren<MeshCollider>()
                    .Where(collider => collider.gameObject.layer == STATICS && collider.gameObject.GetComponents<Component>().Length == 4)
                    .Select(collider => collider.bounds.min.GlobalY())
                    .Min() - Datum.originPosition.GlobalY();

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
                SCALE = 32767;
                Plugin.Logger?.LogInfo($"terrainSize: {terrainSize.ToString()}");
                Plugin.Logger?.LogInfo($"textureSize: {textureSize.ToString()}");
                Plugin.Logger?.LogInfo($"terrainScale: {terrainScale.ToString()}");
                Plugin.Logger?.LogInfo($"terrainSeaLevel: {terrainSeaLevel.ToString()}");
                Plugin.Logger?.LogInfo($"maxHeight: {maxHeight.ToString()}");
                Plugin.Logger?.LogInfo($"minHeight: {minHeight.ToString()}");
                //call Heightmap generator
                GenerateRayCastHeightmap();
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error exporting heightmap: {ex.Message}\n{ex.StackTrace}");
            }
        }
        //RayCasts over the entire terrain. sample rate controlled by metersPerRay, output size is controlled by textureSize
        private static short[,] RayCastTerrain()
        {
            short[,] heights = new short[textureSize, textureSize];
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

                    Vector3 target = new GlobalPosition(x, maxHeight + 1, z).ToLocalPosition();
                    RaycastHit hit;
                    if (oldPosX != posX && oldPosZ != posZ) {
                        if (Physics.Raycast(target, Vector3.down, out hit, maxHeight + 2, 1 << STATICS))
                        {
                            heights[textureSize - posZ -1,posX] = (short)(hit.point.GlobalY());
                        }
                        else
                        {
                            heights[textureSize - posZ - 1,posX] = (short)(minHeight);
                        }
                    }
                    oldPosX = posX;
                }
                oldPosZ = posZ;
            }

            return heights;
           
        }
       
        // main function to trace the game world and output assembled Heightmap
        private static void GenerateRayCastHeightmap()
        {
          
            string outputDir = Path.Combine(BepInEx.Paths.PluginPath, "NOBlackBox_RayCast_HeightmapExports");
            Directory.CreateDirectory(outputDir);
            short[,] heightMapTile = RayCastTerrain();           
            string filenameRaw = $"NOBlackBox_heightmap_{MapSettingsManager.i.MapLoader.CurrentMap.Path}.data";
            string outputPathRaw = Path.Combine(outputDir, filenameRaw);
            SaveHeightMapAsRAW(heightMapTile, outputPathRaw); 
        }

        static void SaveHeightMapAsRAW(short[,] array, string filePath)
        {
            int rows = array.GetLength(0);
            int cols = array.GetLength(1);

            using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            using (BinaryWriter writer = new BinaryWriter(fs))
            {
                for (int y = 0; y < rows; y++)
                {
                    for (int x = 0; x < cols; x++)
                    {
                        writer.Write(array[y, x]);
                    }
                }
            }
            Plugin.Logger?.LogInfo($"Heightmap RAW exported to: {filePath}");
        }

    }
}
