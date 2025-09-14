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
        internal static GameObject ?recorderMono;
        internal static bool isRecording = false;
        internal static GameObject ?autoSaveCountDown;
        internal static GameObject ?recordingIndicator;
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
            Logger?.LogDebug("LOADED.");

            waitTime = Configuration.UpdateRate != 0 ? 1f / Configuration.UpdateRate : 0f;
            //waitTime = 1f / Configuration.UpdateRate.Value;
            waitTime = MathF.Round(waitTime, 3);
            Logger?.LogDebug($"Wait Time = {waitTime}");
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

            if (Configuration.StartStopRecordingKey.Value.IsDown())
            {
                if (!isRecording)
                {
                    StartRecording();
                    if (isRecording)
                    {
                        Logger?.LogDebug("RECORDING STARTED MANUALLY");
                    }
                    
                } else
                {
                    StopRecording();
                    if (!isRecording)
                    {
                        Logger?.LogDebug("RECORDING STOPPED MANUALLY");
                    }
                    
                }    
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
            Logger?.LogDebug("TRYING TO GET PLAYERNAME...");
            Logger?.LogDebug($"{GameManager.LocalPlayer.PlayerName}");
            while (GameManager.LocalPlayer.PlayerName == null)
            {
                Logger?.LogDebug("Waiting for LocalPlayer...");
                await Task.Delay(100);
            }
            return true;
        }

        private async void OnMissionLoad()
        {
            if (Configuration.AutoStartRecording.Value == true)
            {
                bool ready = await WaitForLocalPlayer();
                if (ready)
                {
                    StartRecording();
                    Logger?.LogDebug("MISSION LOADED. START RECORDING.");
                }
            }
        }
        private void OnMissionUnload()
        {
            if (isRecording)
            {
                Logger?.LogDebug("MISSION UNLOADED. STOP RECORDING.");
                StopRecording();
            }
        }

        private void StartRecording()
        {
            if (null == MissionManager.CurrentMission)
            {
                Plugin.Logger?.LogWarning("No Mission found. In order to use this feature, you must launch a mission first.");
                return;
            }

            recorderMono = new GameObject();
            recorderMono.AddComponent<Recorder_mono>();
            recorderMono.GetComponent<Recorder_mono>().enabled = true;
            isRecording = true;

            autoSaveCountDown = new GameObject();
            autoSaveCountDown.AddComponent<AutoSaveCountDown>();
            autoSaveCountDown.GetComponent<AutoSaveCountDown>().enabled = true;

            //recordingIndicator = new GameObject();
            //recordingIndicator.AddComponent<RecordingIndicator>();
            //recordingIndicator.GetComponent<RecordingIndicator>().enabled = true;
        }

        private void StopRecording()
        {
            isRecording = false;
            recorderMono.GetComponent<Recorder_mono>().enabled = false;
            GameObject.Destroy(autoSaveCountDown);
            GameObject.Destroy(recorderMono);
            //GameObject.Destroy(recordingIndicator);
            ACMIObject_mono[] found = GameObject.FindObjectsByType<ACMIObject_mono>(FindObjectsSortMode.None);
            ACMIFlare_mono[] foundFlares = GameObject.FindObjectsByType<ACMIFlare_mono>(FindObjectsSortMode.None);
            foreach (ACMIObject_mono obj in found)
            {
                GameObject.Destroy(obj);
            }
            foreach (ACMIFlare_mono obj in foundFlares)
            {
                GameObject.Destroy(obj);
            }
        }
    }
}
