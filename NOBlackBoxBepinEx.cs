using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;


namespace NOBlackBox
{
    [BepInPlugin("xyz.KopterBuzz.NOBlackBoxBepinEx", "NOBlackBox", "0.1.2")]
    [BepInProcess("NuclearOption.exe")]
    internal class NOBlackBoxBepInEx : BaseUnityPlugin
    {
        private static NOBlackBoxBepInEx _instance;
        internal new static ManualLogSource Logger
        {
            get { return _instance._Logger; }
        }

        private ManualLogSource _Logger
        {
            get { return base.Logger; }
        }

        private void Awake()
        {
            _instance = this;
            DontDestroyOnLoad(this);
            _instance.gameObject.AddComponent<NOBlackBoxRecorder>();
            LoadingManager.MissionUnloaded += MissionUnload;
            LoadingManager.MissionLoaded += LoadingFinished;
            Debug.Log("[NOBLACKBOX]: LOFASZ RIADO!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
        }
        private void LoadingFinished()
        {
            _instance.gameObject.GetComponent<NOBlackBoxRecorder>().SendMessage("StartRecording");
            Debug.Log("[NOBLACKBOX]: HIT LoadingFinished");
        }
        private void MissionUnload()
        {
            _instance.gameObject.GetComponent<NOBlackBoxRecorder>().SendMessage("StopRecording");
            Debug.Log("[NOBLACKBOX]: HIT MissionUnload");
        }

    }
}
