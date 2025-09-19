using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using NuclearOption.SavedMission;
using System;
using System.Threading.Tasks;
using NuclearOption.SceneLoading;
using System.Linq;
using NuclearOption.Networking;
using Mirage;
using NuclearOption.MissionEditorScripts;
using NuclearOption.DedicatedServer;
using UnityEngine.SceneManagement;

#if BEP6
using BepInEx.Unity.Mono;
#endif


namespace NOBlackBox
{
    [BepInPlugin("xyz.KopterBuzz.NOBlackBox", "NOBlackBox", "0.3.7.3")]
    [BepInProcess("NuclearOption.exe")]
    [BepInProcess("NuclearOptionServer.exe")]
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
        internal static BasePlayer ?localPlayer = null;

        internal static bool recordingManually = false;

        private Action? OnGameStateChange;

        public Plugin()
        {
            Logger = base.Logger;
        }
        private void Awake()
        {
            Configuration.InitSettings(Config);
            Logger?.LogDebug("LOADED.");

            waitTime = Configuration.UpdateRate != 0 ? 1f / Configuration.UpdateRate : 0f;
            //waitTime = 1f / Configuration.UpdateRate.Value;
            waitTime = MathF.Round(waitTime, 3);
            Logger?.LogDebug($"Wait Time = {waitTime}");

            OnGameStateChange += ResetRecordingManually;
            GameManager.OnGameStateChanged.AddListener(OnGameStateChange);

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
                recordingManually = true;
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

            

            if (!isRecording && MissionManager.IsRunning && !recordingManually)
            {
                StartRecording();
            }
            
            if (isRecording && !MissionManager.IsRunning && !recordingManually)
            {
                StopRecording();
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


        private void StartRecording()
        {
            if (isRecording)
            {
                return;
            }
            isRecording = true;
            recorderMono = new GameObject();
            recorderMono.AddComponent<Recorder_mono>();
            recorderMono.GetComponent<Recorder_mono>().enabled = true;
            

            autoSaveCountDown = new GameObject();
            autoSaveCountDown.AddComponent<UIElements>();
            autoSaveCountDown.GetComponent<UIElements>().enabled = true;

            //recordingIndicator = new GameObject();
            //recordingIndicator.AddComponent<RecordingIndicator>();
            //recordingIndicator.GetComponent<RecordingIndicator>().enabled = true;
        }

        private void StopRecording()
        {
            if (!isRecording)
            {
                return;
            }
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

        private void ResetRecordingManually()
        {
            recordingManually = false;
        }

        private void ResetRecordingManuallyCallback()
        {
            OnGameStateChange?.Invoke();
        }
    }
}
