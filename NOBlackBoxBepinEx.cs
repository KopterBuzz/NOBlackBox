#if BEPINEX
using BepInEx;
#endif
using UnityEngine;

namespace NOBlackBox
{
    #if BEPINEX
    [BepInPlugin("xyz.KopterBuzz.NOBlackBox", "NOBlackBox", "0.1.4")]
    [BepInProcess("NuclearOption.exe")]
    internal class NOBlackBoxBepInEx : BaseUnityPlugin
    {
        private static NOBlackBoxBepInEx _instance;

        private void Awake()
        {
            _instance = this;
            DontDestroyOnLoad(this);
            _instance.gameObject.AddComponent<NOBlackBoxRecorder>();
            LoadingManager.MissionUnloaded += MissionUnload;
            LoadingManager.MissionLoaded += LoadingFinished;
            Debug.Log("[NOBLACKBOX]: LOADED.");
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
    #endif
}
