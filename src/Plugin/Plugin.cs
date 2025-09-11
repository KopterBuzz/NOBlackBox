using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using NuclearOption.SavedMission;
using System;
using System.Threading.Tasks;
using NuclearOption.SceneLoading;
using System.Linq;
using NuclearOption.Networking;

#if BEP6
using BepInEx.Unity.Mono;
#endif


namespace NOBlackBox
{
    [BepInPlugin("xyz.KopterBuzz.NOBlackBox", "NOBlackBox", "0.3.7.0")]
    [BepInProcess("NuclearOption.exe")]
    internal class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource ?Logger;
        internal static GameObject recorderMono;
        internal static bool isRecording = false;
        internal static GameObject ?autoSaveCountDown;
        private float waitTime = 0.2f;
        private float timer = 0f;
        internal static int recordedScreenWidth, recordedScreenHeight;
        internal static float guiAnchorLeft, guiAnchorRight;

        public Plugin()
        {
            Logger = base.Logger;
            LoadingManager.MissionLoaded += OnMissionLoad;
            LoadingManager.MissionUnloaded += OnMissionUnload;
        }
        private void Awake()
        {
            Configuration.InitSettings(Config);
            Logger?.LogDebug("[NOBlackBox]: LOADED.");

            waitTime = Configuration.UpdateRate != 0 ? 1f / Configuration.UpdateRate : 0f;
            //waitTime = 1f / Configuration.UpdateRate.Value;
            waitTime = MathF.Round(waitTime, 3);
            Logger?.LogDebug($"[NOBlackBox]: Wait Time = {waitTime}");
        }
        private void Update()
        {
            if (Configuration._GenerateHeightMapKey.Value.IsDown())
            {
                RaycastHeightmapGenerator.Generate();
            }
            if (Configuration.EncyclopediaExporterKey.Value.IsDown())
            {
                EncyclopediaExporter.ExportEncyclopediaCSV();
            }
            UpdateGuiAnchors();
        }

        private static void UpdateGuiAnchors()
        {
            recordedScreenWidth = Screen.width;
            recordedScreenHeight = Screen.height;
            guiAnchorLeft = (int)Math.Round(0.03 * recordedScreenWidth);
            guiAnchorRight = (int)Math.Round(0.7 * recordedScreenWidth);
        }

        private async Task<bool> WaitForLocalPlayer()
        {
            Logger?.LogDebug("[NOBlackBox]: TRYING TO GET PLAYERNAME...");
            Logger?.LogDebug($"[NOBlackBox]: {GameManager.LocalPlayer.PlayerName}");
            while (GameManager.LocalPlayer.PlayerName == null)
            {
                Logger?.LogDebug("[NOBlackBox]: Waiting for LocalPlayer...");
                await Task.Delay(100);
            }
            return true;
        }

        private async void OnMissionLoad()
        {
            await WaitForLocalPlayer();
            Logger?.LogDebug("[NOBlackBox]: MISSION LOADED.");
            LevelInfo levelInfo = LevelInfo.i;
            
            if (levelInfo.LoadedMapSettings)
            {
                Logger?.LogDebug($"[NOBlackBox]: Terrain Size: {levelInfo.LoadedMapSettings.MapSize}");
            } else
            {
                Logger?.LogWarning($"[NOBlackBox]: NO LEVELINFO!!!!");
            }
            

            recorderMono = new GameObject();
            recorderMono.AddComponent<Recorder_mono>();
            recorderMono.GetComponent<Recorder_mono>().enabled = true;
            
            isRecording = true;
            autoSaveCountDown = new GameObject();
            autoSaveCountDown.AddComponent<AutoSaveCountDown>();
            autoSaveCountDown.GetComponent<AutoSaveCountDown>().enabled = true;
        }
        private void OnMissionUnload()
        {
  
            Logger?.LogDebug("[NOBlackBox]: MISSION UNLOADED.");
            isRecording = false;
            recorderMono.GetComponent<Recorder_mono>().enabled = false;
            GameObject.Destroy(autoSaveCountDown);
            GameObject.Destroy(recorderMono);
            
        }
    }
}
