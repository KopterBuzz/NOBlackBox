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
        private static int textureSize = Configuration.HeightMapResolution.Value;
        private static float terrainSize = 0;
        private static int metersPerRay = Configuration.MetersPerScan.Value;
        private static float minHeight = 0;
        private static float maxHeight = 0;
        private static float terrainScale = 0;
        private static float terrainHalf = 0;
        private static readonly int STATICS = LayerMask.NameToLayer("Statics");
        
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

                // find minimum and maximum terrain height. shoutout to TYKUHN2 ^^
                // this is not necessary for the current method, but useful for debugging
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
                // Initialize variables required for scaling world coordinates to "texture" coordinates.
                terrainHalf = terrainSize / 2;
                terrainScale = textureSize / terrainSize;
                Plugin.Logger?.LogInfo($"terrainSize: {terrainSize.ToString()}");
                Plugin.Logger?.LogInfo($"textureSize: {textureSize.ToString()}");
                Plugin.Logger?.LogInfo($"terrainScale: {terrainScale.ToString()}");
                Plugin.Logger?.LogInfo($"maxHeight: {maxHeight.ToString()}");
                Plugin.Logger?.LogInfo($"minHeight: {minHeight.ToString()}");
                // call Heightmap generator
                GenerateRayCastHeightmap();
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error exporting heightmap: {ex.Message}\n{ex.StackTrace}");
            }
        }
        // RayCasts over the entire terrain. sample rate controlled by metersPerRay, output size is controlled by textureSize
        private static short[,] RayCastTerrain()
        {
            short[,] heights = new short[textureSize, textureSize];
            int oldPosX = -2;
            int oldPosZ = -2;
            int posX = -1;
            int posZ  = -1;
            Plugin.Logger?.LogInfo($"Generating Heightmap...");

            // iterate over both axis, scale the world position to heightmap position using terrainScale.
            // any raycast where the hit would land on a heightmap position that's already been populated is skipped, massive time save
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

        // helper function to dump heightmap to disk
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
