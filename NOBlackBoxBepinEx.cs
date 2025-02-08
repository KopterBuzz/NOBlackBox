using BepInEx;
using HarmonyLib;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;


namespace NOBlackBox
{
    [BepInPlugin("xyz.KopterBuzz.NOBlackBoxBepinEx", "NOBlackBox", "0.1.2")]
    internal class NOBlackBoxBepInEx : BaseUnityPlugin
    {
        private static NOBlackBoxBepInEx _instance;
        private void Awake()
        {
            _instance = this;
            DontDestroyOnLoad(this);
            _instance.gameObject.AddComponent<NOBlackBoxRecorder>();
        }
    }
}
