using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Xml.Linq;
using UnityEngine;

using static MapSettingsManager;
using static UnityEngine.UIElements.StylePropertyAnimationSystem;

namespace NOBlackBox
{
    internal static class RaycastHeightmapGenerator
    {
        private static string ?heightmapFileName;
        private static string ?textureFileName;
        private static string ?customHeightMapListXMLFileName;
        private static string ?customTextureListXMLFileName;
        private static string outputDir = Path.Combine(BepInEx.Paths.PluginPath, "NOBlackBox\\Developer");
        private static string CombinedCustomHeightMapListXMLPath = Path.Combine(
                                                                        BepInEx.Paths.PluginPath,
                                                                        "NOBlackBox\\Developer\\NuclearOption_Heightmaps.xml");
        private static int textureSize = Configuration.HeightMapResolution.Value;
        private static float terrainSize = 0;
        private static int metersPerRay = Configuration.MetersPerScan.Value;
        private static float minHeight = 0;
        private static float maxHeight = 0;
        private static float terrainScale = 0;
        private static float terrainHalf = 0;
        private static float terrainXHalf = 0;
        private static float terrainYHalf = 0;
        private static float terrainHalfInDegrees = 0;
        private static float terrainXHalfInDegrees = 0;
        private static float terrainYHalfInDegrees = 0;
        private static readonly int STATICS = LayerMask.NameToLayer("Statics");


        private static Texture2D ?texture;
        private static Camera ?renderCam;
        private static RenderTexture ?renderCamTexture;


        private static void SetupRenderCam()
        {
            if (renderCamTexture != null)
            {
                renderCamTexture.Release();
            }

            renderCamTexture = new RenderTexture(textureSize, textureSize, 24, RenderTextureFormat.ARGB32);
            renderCamTexture.enableRandomWrite = true;
            renderCamTexture.Create();

            GameObject camGO = new GameObject("HeightmapCamera");
            camGO.hideFlags = HideFlags.HideAndDontSave;
            renderCam = camGO.AddComponent<Camera>();
            renderCam.orthographic = true;
            renderCam.orthographicSize = terrainHalf;
            renderCam.cullingMask = 1 << 6;
            renderCam.targetTexture = renderCamTexture;
            renderCam.allowHDR = false;
            renderCam.enabled = true;
            renderCam.farClipPlane = maxHeight + 100;

            texture = new Texture2D(textureSize, textureSize, TextureFormat.RGB24, false);
        }

        private static void ProbeTextureColors(int X, int Y, int Z)
        {
            SetupRenderCam();
            Vector3 target = new GlobalPosition(X, Y, Z).ToLocalPosition();
            renderCam.transform.position = target;
            renderCam.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // look down
            renderCam.Render();
            RenderTexture.active = renderCamTexture;
            texture.ReadPixels(new Rect(0, 0, renderCamTexture.width, renderCamTexture.height), 0, 0, false);
            texture.Apply();
            RenderTexture.active = null;
            renderCam.targetTexture.Release();
        }

        public static void Generate()
        {
            //do checks
            if (Configuration.EnableHeightmapGenerator.Value == false)
            {
                Plugin.Logger?.LogInfo("HeightmapGenerator is disabled. Enable it in NOBlackBox Configuration settings by setting EnableHeightmapGenerator = true");
                return;
            }
            if (null == MissionManager.CurrentMission)
            {
                Plugin.Logger?.LogInfo("No Terrain found. In order to use this feature, you must launch a mission first.");
                return;
            }
            try

            {
				const string BasePath = "TacviewAssets\\Terrain\\";
				const string HeightmapsBasePath = BasePath + "Custom\\Nuclear Option\\";
				const string TexturesBasePath = BasePath + "Textures\\Nuclear Option\\";

				string currentMapName = MapSettingsManager.i.MapLoader.CurrentMap.Path;

				heightmapFileName = HeightmapsBasePath + $"NuclearOption_heightmap_{currentMapName}.raw";
				customHeightMapListXMLFileName = HeightmapsBasePath + $"NuclearOption_heightmap_{currentMapName}.xml";

				textureFileName = TexturesBasePath + $"NuclearOption_texture_{currentMapName}.png";
				customTextureListXMLFileName = TexturesBasePath + $"NuclearOption_texture_{currentMapName}.xml";
				
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
                Map thisMap = null;
                Plugin.Logger?.LogInfo("Looping through MapSettingsManager.i.Maps..");
                foreach (Map map_ in MapSettingsManager.i.Maps)
                {
                    
                    Plugin.Logger?.LogInfo($"checking {map_.Prefab.name}");
                    if (map_.Prefab.name == MapSettingsManager.i.MapLoader.CurrentMap.Path)
                    {
                        thisMap = map_;
                    }
                }
                // Get terrain dimensions from the static TerrainGrid class

                if (thisMap?.Prefab.MapSize.y >= thisMap?.Prefab.MapSize.x)
                {
                    terrainSize = thisMap.Prefab.MapSize.y;
                }
                else
                {
                    terrainSize = thisMap.Prefab.MapSize.x;
                }
                if (terrainSize == 0f)
                {
                    return;
                }
                // Initialize variables required for scaling world coordinates to "texture" coordinates.
                terrainHalf = terrainSize / 2;
                terrainScale = textureSize / terrainSize;

                terrainXHalf = thisMap.Prefab.MapSize.x / 2;
                terrainYHalf = thisMap.Prefab.MapSize.y / 2;

                

                /*
                 * needed for Tacview xml
                    1 degree° = 60 arc minutes '
                    1 arc minute ' = 60 arc seconds ''
                    1 arc second '' at equatorial sea level = 1852m/60 = 30.86666667m
                    https://www.opendem.info/arc2meters.html
                 */
                terrainHalfInDegrees = ((terrainHalf / 30.86666667f) / 60f) / 60f;
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
                    if (oldPosX != posX && oldPosZ != posZ)
					{
						if (Physics.Raycast(target, Vector3.down, out hit, maxHeight + 2, 1 << STATICS))
						{
							//ProbeTextureColors(x, (int)(hit.point.GlobalY() + 10),z);

							heights[textureSize - posZ - 1, posX] = (short)(hit.point.GlobalY());
						}
						else
						{
							heights[textureSize - posZ - 1, posX] = (short)(minHeight);
						}
					}
                    oldPosX = posX;
                }
                oldPosZ = posZ;
            }

            return heights;
        }

		// main function to trace the game world and output assembled Heightmap

		// Recommended mission settings for best results:
		// - Disable clouds and fog (to avoid cloud shadows on the map)
		// - Set time to 16:00 for better lighting (less overexposure and shadows cast to the right)

		private static void GenerateRayCastHeightmap()
		{
			// Export Elevation Map

			short[,] heightMapTile = RayCastTerrain();
			SaveHeightMapAsRAW(heightMapTile);
			SaveCustomHeightmapListXML();

			// Export Terrain Texture

			ProbeTextureColors(0, (int)(maxHeight + 2), 0);
			SaveHeightMapCustomTexture();
			SaveCustomTextureListXML();

/*
			if (Configuration.TacviewBetaHeightMapGenerator.Value)
			{
				SaveCustomHeightmapListXML_Multiple();
			}
			else
			{
				SaveCustomHeightmapListXML();
			}
*/
		}

		// helper function to dump heightmap to disk
		static void SaveHeightMapAsRAW(short[,] array)
		{
			string filePath = Path.Combine(outputDir, heightmapFileName);
			Directory.CreateDirectory(Path.GetDirectoryName(filePath));

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

		static void SaveHeightMapCustomTexture()
		{
			string filePath = Path.Combine(outputDir, textureFileName);
			Directory.CreateDirectory(Path.GetDirectoryName(filePath));

			byte[] tex = texture.EncodeToPNG();

			string outputPath = Path.Combine(outputDir, textureFileName);
			File.WriteAllBytes(outputPath, tex);
			Plugin.Logger?.LogInfo($"Heightmap Texture exported to: {outputPath}");
			texture = null;
			renderCamTexture.Release();
			tex = null;
			System.GC.Collect();
		}

        static void SaveCustomHeightmapListXML()
        {
            XDocument doc = new XDocument(
                new XElement("Resources",
                    new XElement("CustomHeightmapList",
                        new XElement("CustomHeightmap",
                        new XAttribute("Layer", "Nuclear Option"),
                        new XAttribute("Id", $"NuclearOption.{MapSettingsManager.i.MapLoader.CurrentMap.Path}"),
                            new XElement("File", Path.GetFileName(heightmapFileName)),
                            new XElement("BigEndian", "0"),
                            new XElement("Width", textureSize.ToString()),
                            new XElement("Height", textureSize.ToString()),
                            new XElement("AltitudeFactor", "1.0"),
                            new XElement("AltitudeOffset", "0"),
                            new XElement("Projection", "Quad"),
                            new XElement("BottomLeft",
                            new XElement("Longitude", -terrainHalfInDegrees),
                            new XElement("Latitude", -terrainHalfInDegrees)),
                            new XElement("BottomRight",
                            new XElement("Longitude", terrainHalfInDegrees),
                            new XElement("Latitude", -terrainHalfInDegrees)),
                            new XElement("TopRight",
                            new XElement("Longitude", terrainHalfInDegrees),
                            new XElement("Latitude", terrainHalfInDegrees)),
                            new XElement("TopLeft",
                            new XElement("Longitude", -terrainHalfInDegrees),
                            new XElement("Latitude", terrainHalfInDegrees))
                        )
                    )
                )
            );
            string XMLPath = Path.Combine(outputDir, customHeightMapListXMLFileName);
            Plugin.Logger?.LogInfo($"Saving custom tacview custom XML to {XMLPath}");
            doc.Save(XMLPath);
        }

        static void SaveCustomHeightmapListXML_Multiple()
        {
            XDocument doc = null;

            try
            {
                doc = XDocument.Load(CombinedCustomHeightMapListXMLPath);

                XElement newHeightmap = new XElement("CustomHeightmap",
                    new XAttribute("Layer", "Nuclear Option"),
                    new XAttribute("Id", $"NuclearOption.{MapSettingsManager.i.MapLoader.CurrentMap.Path}"),
                    new XElement("File", Path.GetFileName(heightmapFileName)),
                    new XElement("BigEndian", "0"),
                    new XElement("Width", textureSize.ToString()),
                    new XElement("Height", textureSize.ToString()),
                    new XElement("AltitudeFactor", "1.0"),
                    new XElement("AltitudeOffset", "0"),
                    new XElement("Projection", "Quad"),
                    new XElement("BottomLeft",
                    new XElement("Longitude", -terrainHalfInDegrees),
                    new XElement("Latitude", -terrainHalfInDegrees)),
                    new XElement("BottomRight",
                    new XElement("Longitude", terrainHalfInDegrees),
                    new XElement("Latitude", -terrainHalfInDegrees)),
                    new XElement("TopRight",
                    new XElement("Longitude", terrainHalfInDegrees),
                    new XElement("Latitude", terrainHalfInDegrees)),
                    new XElement("TopLeft",
                    new XElement("Longitude", -terrainHalfInDegrees),
                    new XElement("Latitude", terrainHalfInDegrees))
                );

                XElement root = doc.Element("Resources");
                XElement heightmapList = root.Element("CustomHeightmapList");

                XElement existing = heightmapList.Elements("CustomHeightmap").FirstOrDefault(e => (string)e.Element("File") == heightmapFileName);

                if (existing != null)
                {
                    existing.ReplaceWith(newHeightmap);
                    Plugin.Logger?.LogInfo($"Updating {heightmapFileName} in {CombinedCustomHeightMapListXMLPath}...");
                }
                else
                {
                    heightmapList.Add(newHeightmap);
                    Plugin.Logger?.LogInfo($"Adding {heightmapFileName} to {CombinedCustomHeightMapListXMLPath}...");
                }

                doc.Save(CombinedCustomHeightMapListXMLPath);
                Plugin.Logger?.LogInfo($"Saved {CombinedCustomHeightMapListXMLPath} to disk.");

            } catch
            {
                doc = new XDocument(
                    new XElement("Resources",
                        new XElement("CustomHeightmapList",
                            new XElement("CustomHeightmap",
                            new XAttribute("Layer", "Nuclear Option"),
                            new XAttribute("Id", $"NuclearOption.{MapSettingsManager.i.MapLoader.CurrentMap.Path}"),
                                new XElement("File", Path.GetFileName(heightmapFileName)),
                                new XElement("BigEndian", "0"),
                                new XElement("Width", textureSize.ToString()),
                                new XElement("Height", textureSize.ToString()),
                                new XElement("AltitudeFactor", "1.0"),
                                new XElement("AltitudeOffset", "0"),
                                new XElement("Projection", "Quad"),
                                new XElement("BottomLeft",
                                new XElement("Longitude", -terrainHalfInDegrees),
                                new XElement("Latitude", -terrainHalfInDegrees)),
                                new XElement("BottomRight",
                                new XElement("Longitude", terrainHalfInDegrees),
                                new XElement("Latitude", -terrainHalfInDegrees)),
                                new XElement("TopRight",
                                new XElement("Longitude", terrainHalfInDegrees),
                                new XElement("Latitude", terrainHalfInDegrees)),
                                new XElement("TopLeft",
                                new XElement("Longitude", -terrainHalfInDegrees),
                                new XElement("Latitude", terrainHalfInDegrees))
                            )
                        )
                    )
                );
                Plugin.Logger?.LogInfo($"Saving custom tacview custom XML to {CombinedCustomHeightMapListXMLPath}");
                doc.Save(CombinedCustomHeightMapListXMLPath);
            }
        }

        static void SaveCustomTextureListXML()
        {
            XDocument doc = new XDocument(
                new XElement("Resources",
                    new XElement("CustomTextureList",
                        new XElement("CustomTexture",
                        new XAttribute("Layer", "Nuclear Option"),
                        new XAttribute("Id", $"NuclearOption.{MapSettingsManager.i.MapLoader.CurrentMap.Path}"),
                            new XElement("File", Path.GetFileName(textureFileName)),
                            new XElement("BottomLeft",
                            new XElement("Longitude", -terrainHalfInDegrees),
                            new XElement("Latitude", -terrainHalfInDegrees)),
                            new XElement("BottomRight",
                            new XElement("Longitude", terrainHalfInDegrees),
                            new XElement("Latitude", -terrainHalfInDegrees)),
                            new XElement("TopRight",
                            new XElement("Longitude", terrainHalfInDegrees),
                            new XElement("Latitude", terrainHalfInDegrees)),
                            new XElement("TopLeft",
                            new XElement("Longitude", -terrainHalfInDegrees),
                            new XElement("Latitude", terrainHalfInDegrees))
                        )
                    )
                )
            );
            string XMLPath = Path.Combine(outputDir, customTextureListXMLFileName);
            Plugin.Logger?.LogInfo($"Saving custom tacview custom XML to {XMLPath}");
            doc.Save(XMLPath);
        }

    }
}
