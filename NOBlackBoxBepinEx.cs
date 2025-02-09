using BepInEx;
using UnityEngine;

namespace NOBlackBox
{

    [BepInPlugin("xyz.KopterBuzz.NOBlackBox", "NOBlackBox", "0.1.4")]
    [BepInProcess("NuclearOption.exe")]
    internal class NOBlackBoxBepInEx : BaseUnityPlugin
    {
        private static NOBlackBoxBepInEx _instance;

        private void Awake()
        {
            _instance = this;
            //DontDestroyOnLoad(this);
            _instance.gameObject.AddComponent<NOBlackBoxRecorder>();
            Debug.Log("[NOBLACKBOX]: LOADED.");
        }
    }

}
