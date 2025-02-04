using HarmonyLib;
using UnityEngine;
using static UnityModManagerNet.UnityModManager;

namespace NOBlackBox
{
    public class NOBlackBox
    {
        private static GameObject _recorder;
        private static Settings _settings;
        private static bool enabled;

        public static bool Load(ModEntry modEntry)
        {

            _settings = Settings.Load<Settings>(modEntry);
            _recorder = new GameObject();
            //modEntry.OnGUI = OnGUI;
            //modEntry.OnSaveGUI = OnSaveGUI;
            _recorder = new GameObject();
            _recorder.AddComponent<NOBlackBoxRecorder>();
            modEntry.OnToggle = OnToggle;

            UnityEngine.Debug.Log("--- NOBlackBox Initialized ---");
            Harmony harmony = new Harmony("KopterBuzz.NuclearOption.NOBlackBox");
            Harmony.DEBUG = true;

            harmony.PatchAll();
            return true;
        }
        static bool OnToggle(ModEntry modEntry, bool value)
        {
            if (enabled)
            {
                enabled = false;
                _recorder.SetActive(false);
                _recorder.GetComponent<NOBlackBoxRecorder>().SendMessage("Flush");

            }
            else
            {
                enabled = true;
                _recorder.SetActive(true);
                _recorder.GetComponent<NOBlackBoxRecorder>().SendMessage("Flush");
            }

            return true;
        }


    }
}
