using UnityEngine;
using static UnityModManagerNet.UnityModManager;

namespace NOBlackBox
{
    public class NOBlackBoxUMM
    {
        private static GameObject _instance;
        private static Settings _settings;
        private static bool enabled;
        public static bool Load(ModEntry modEntry)
        {
            _settings = Settings.Load<Settings>(modEntry);
            //modEntry.OnGUI = OnGUI;
            //modEntry.OnSaveGUI = OnSaveGUI;
            _instance = new GameObject();
            _instance.AddComponent<NOBlackBoxRecorder>();
            LoadingManager.MissionUnloaded += MissionUnload;
            LoadingManager.MissionLoaded += LoadingFinished;
            modEntry.OnToggle = OnToggle;
            UnityEngine.Debug.Log("--- NOBlackBox Initialized ---");
            //Harmony harmony = new Harmony("xyz.KopterBuzz.NOBlackBoxUMM");
            //Harmony.DEBUG = true;
            //harmony.PatchAll();
            return true;
        }
        static bool OnToggle(ModEntry modEntry, bool value)
        {
            if (enabled)
            {
                enabled = false;
                _instance.SetActive(false);
                _instance.GetComponent<NOBlackBoxRecorder>().SendMessage("Flush");
            }
            else
            {
                enabled = true;
                _instance.SetActive(true);
                _instance.GetComponent<NOBlackBoxRecorder>().SendMessage("Flush");
            }

            return true;
        }
        private static void LoadingFinished()
        {
            _instance.gameObject.GetComponent<NOBlackBoxRecorder>().SendMessage("StartRecording");
            Debug.Log("[NOBLACKBOX]: HIT LoadingFinished");
        }
        private static void MissionUnload()
        {
            _instance.gameObject.GetComponent<NOBlackBoxRecorder>().SendMessage("StopRecording");
            Debug.Log("[NOBLACKBOX]: HIT MissionUnload");
        }
    }
}
