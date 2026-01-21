using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using NuclearOption.Networking;
using UnityEngine;

#if BEP6
using BepInEx.Unity.Mono;
#endif


namespace NOBlackBox
{
    [BepInPlugin("xyz.KopterBuzz.NOBlackBox", "NOBlackBox", "0.3.8.2")]
    [BepInProcess("NuclearOption.exe")]
    [BepInProcess("NuclearOptionServer.exe")]
    internal class Plugin : BaseUnityPlugin
    {

        internal static new ManualLogSource? Logger;
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

        internal static Dictionary<string, List<string>> aircraftInfo;
        internal static Dictionary<string, List<string>> groundInfo;
        internal static Dictionary<string, List<string>> missileInfo;
        internal static Dictionary<string, List<string>> shipInfo;

        public static Dictionary<string, Dictionary<string, string[]>> NOBlackBoxUnitInfo = new()
        {
            { "aircraft",   new Dictionary<string, string[]>() },
            { "missiles",   new Dictionary<string, string[]>() },
            { "ships",      new Dictionary<string, string[]>() },
            { "vehicles",   new Dictionary<string, string[]>() }

        };


        public Plugin()
        {
            //Logger = base.Logger;
        }
        private void Awake()
        {
            GameObject managerObject = Chainloader.ManagerObject;
            bool flag = managerObject != null;
            if (flag)
            {
                managerObject.hideFlags = HideFlags.HideAndDontSave;
                global::UnityEngine.Object.DontDestroyOnLoad(managerObject);
                Logger?.LogWarning("Force Hid ManagerGameObject");
            }
            Configuration.InitSettings(Config);
            if (Configuration.EnableLogging.Value == true)
            {
                Logger = base.Logger;
            }

            foreach (string key in NOBlackBoxUnitInfo.Keys)
            {
                LoadPluginUnitInfo(key);
            }

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

        public static void LoadPluginUnitInfo(string key)
        {
            string dirPath = Path.Combine(Paths.PluginPath, $"NOBlackBox/PluginUnitInfo/{key}");
            string defaultPath = Path.Combine(dirPath, "default.txt");
            if (File.Exists(defaultPath))
            {
                Plugin.Logger?.LogDebug($"{defaultPath} exists.");
                ParsePluginUnitInfo(key, defaultPath);
            } else
            {
                Plugin.Logger?.LogDebug($"{defaultPath} DOES NOT exist.");
            }
            var extraPaths = GetTxtFilesExcludingDefault(dirPath);
            if (extraPaths != null)
            {
                foreach (var path in extraPaths)
                {
                    Plugin.Logger?.LogDebug($"FOUND: {path}");
                    ParsePluginUnitInfo(key, path);
                }
            }
            Plugin.Logger?.LogDebug($"Loaded {NOBlackBoxUnitInfo[key].Keys.Count} {key} units.");
        }

        private static void ParsePluginUnitInfo(string PluginUnitInfoKey, string path)
        {

            if (!File.Exists(path))
            {
                return;
            }

            string[] lines = File.ReadAllLines(path);
            foreach (string line in lines)
            {
                string[] split = line.Split(",");
                string key = split[0];
                NOBlackBoxUnitInfo[PluginUnitInfoKey][key] = split;
                Plugin.Logger?.LogDebug($"{key} : {NOBlackBoxUnitInfo[PluginUnitInfoKey][key]}");
            }
        }

        public static IEnumerable<string> GetTxtFilesExcludingDefault(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
                throw new ArgumentException("Directory path cannot be null or empty.", nameof(directoryPath));

            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");

            return Directory
                .EnumerateFiles(directoryPath, "*.txt", SearchOption.TopDirectoryOnly)
                .Where(file =>
                    !string.Equals(
                        Path.GetFileName(file),
                        "default.txt",
                        StringComparison.OrdinalIgnoreCase));
        }
    }
}
