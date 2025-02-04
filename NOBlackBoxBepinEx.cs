using BepInEx;
using HarmonyLib;
using UnityEngine;


namespace NOBlackBox
{
    [BepInPlugin("com.NuclearOption.NOBlackBoxBepinEx", "NOBlackBox", "0.1.2")]
    internal class NOBlackBoxBepinEx : BaseUnityPlugin
    {
        private static GameObject _recorder;
        private Harmony _harmony;

        void Awake()
        { 
            _harmony = new Harmony("com.NuclearOption.NOBlackBoxBepinEx");
            Harmony.DEBUG = true;
            _harmony.PatchAll();
            Load();
        }

        private void Load()
        {
            _recorder = new GameObject();
            _recorder.AddComponent<NOBlackBoxRecorder>();
            _recorder.GetComponent<NOBlackBoxRecorder>().enabled = true;
        }
    }
}
