using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using System;
using System.Threading.Tasks;
using NuclearOption.SceneLoading;
using System.Linq;
using NOBlackBox.src.HeightMapGenerator;
using System.IO;
using static System.Net.Mime.MediaTypeNames;
using System.Buffers.Binary;



#if BEP6
using BepInEx.Unity.Mono;
#endif


namespace NOBlackBox
{
    [BepInPlugin("xyz.KopterBuzz.NOBlackBox", "NOBlackBox", "0.2.4")]
    [BepInProcess("NuclearOption.exe")]
    internal class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource ?Logger;
        private Recorder? recorder;
        private float waitTime = 0.2f;
        private float timer = 0f;

        public Plugin()
        {
            Logger = base.Logger;
            LoadingManager.MissionLoaded += OnMissionLoad;
            LoadingManager.MissionUnloaded += OnMissionUnload;
        }
        private void Awake()
        {
            Configuration.InitSettings(Config);
            Logger?.LogInfo("[NOBlackBox]: LOADED.");

            waitTime = Configuration.UpdateRate != 0 ? 1f / Configuration.UpdateRate : 0f;
            //waitTime = 1f / Configuration.UpdateRate.Value;
            waitTime = MathF.Round(waitTime, 3);
            Logger?.LogInfo($"[NOBlackBox]: Wait Time = {waitTime}");
        }
        private void Update()
        {
            timer += Time.deltaTime;
            if (recorder != null && timer >= waitTime)
            {
                recorder.Update(timer);
                timer = 0f;
            }
            if (Configuration._GenerateHeightMapKey.Value.IsDown())
            {
                //RaycastHeightmapGenerator.Generate();

                /*Raycaster instance = Raycaster.ScanMap();

                instance.OnFinished += tex =>
                {
                    string outputDir = Path.Combine(BepInEx.Paths.PluginPath, "NOBlackBox_RayCast_HeightmapExports");
                    Directory.CreateDirectory(outputDir);

                    string filename = $"NOBlackBox_heightmap_{MapSettingsManager.i.MapLoader.CurrentMap.Path}.png";
                    string outputPath = Path.Combine(outputDir, filename);
                    byte[] bytes = tex.EncodeToPNG();
                    File.WriteAllBytes(outputPath, bytes);
                    Plugin.Logger?.LogInfo($"Heightmap PNG exported to: {outputPath}");

                    string filenameRaw = $"NOBlackBox_heightmap_{MapSettingsManager.i.MapLoader.CurrentMap.Path}.data";
                    string outputPathRaw = Path.Combine(outputDir, filenameRaw);
                    byte[] rawBytes = tex.GetRawTextureData();
                    File.WriteAllBytes(outputPathRaw, rawBytes);
                    Plugin.Logger?.LogInfo($"Heightmap RAW exported to: {outputPathRaw}");
                };*/

                RaycasterV2 instance = RaycasterV2.ScanMap();

                instance.OnFinished += map =>
                {
                    string outputDir = Path.Combine(BepInEx.Paths.PluginPath, "NOBlackBox_RayCast_HeightmapExports");
                    Directory.CreateDirectory(outputDir);

                    string filenameRaw = $"NOBlackBox_heightmap_{MapSettingsManager.i.MapLoader.CurrentMap.Path}.data";
                    string outputPathRaw = Path.Combine(outputDir, filenameRaw);
                    File.WriteAllBytes(outputPathRaw, map);
                    Logger?.LogInfo($"Heightmap RAW exported to: {outputPathRaw}");
                };
            }
        }

        private async Task<bool> WaitForLocalPlayer()
        {
            Logger?.LogInfo("[NOBlackBox]: TRYING TO GET PLAYERNAME...");
            Logger?.LogInfo($"[NOBlackBox]: {GameManager.LocalPlayer.PlayerName}");
            while (GameManager.LocalPlayer.PlayerName == null)
            {
                Logger?.LogInfo("[NOBlackBox]: Waiting for LocalPlayer...");
                await Task.Delay(100);
            }
            return true;
        }

        private async void OnMissionLoad()
        {
            MapLoader mapLoader = Resources.FindObjectsOfTypeAll<MapLoader>().First();
            
            await WaitForLocalPlayer();
            Logger?.LogInfo("[NOBlackBox]: MISSION LOADED.");
            Logger?.LogInfo("[NOBlackBox]: Terrain Size: " + TerrainGrid.terrainSize.ToString());
            
            foreach (var name in mapLoader.MapPrefabNames)
            {
                Logger?.LogInfo($"Map Prefab: {name}");
            }
            recorder = new Recorder(MissionManager.CurrentMission);
        }
        private void OnMissionUnload()
        {
            Logger?.LogInfo("[NOBlackBox]: MISSION UNLOADED.");
            recorder?.Close();
            recorder = null;
        }

    }
}
