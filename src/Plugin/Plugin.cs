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
    [BepInPlugin("xyz.KopterBuzz.NOBlackBox", "NOBlackBox", "0.3.1")]
    [BepInProcess("NuclearOption.exe")]
    internal class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource ?Logger;
        private Recorder? recorder;
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
                RaycastHeightmapGenerator.Generate();
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
            LevelInfo levelInfo = LevelInfo.i;
            if (MapSettingsManager.i.Maps[0].Prefab)
            {
                Logger?.LogInfo($"[NOBlackBox]: Terrain Size: {MapSettingsManager.i.Maps[0].Prefab.MapSize}");
            } else
            {
                Logger?.LogWarning($"[NOBlackBox]: NO LEVELINFO!!!!");

            }
            
            
            /*
            foreach (var name in mapLoader.MapPrefabNames)
            {
                Logger?.LogInfo($"Map Prefab: {name}");
            }
            */
            recorder = new Recorder(MissionManager.CurrentMission);
            isRecording = true;
            autoSaveCountDown = new GameObject();
            autoSaveCountDown.AddComponent<AutoSaveCountDown>();
            autoSaveCountDown.GetComponent<AutoSaveCountDown>().enabled = true;

        }
        private void OnMissionUnload()
        {
            Logger?.LogInfo("[NOBlackBox]: MISSION UNLOADED.");
            recorder?.Close();
            recorder = null;
            isRecording = false;
            GameObject.Destroy(autoSaveCountDown);
        }

    }
}
