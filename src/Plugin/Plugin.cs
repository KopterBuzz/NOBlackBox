using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using NuclearOption.SavedMission;
using System;

#if BEP6
using BepInEx.Unity.Mono;
#endif


namespace NOBlackBox
{
    [BepInPlugin("xyz.KopterBuzz.NOBlackBox", "NOBlackBox", "0.2.0")]
    [BepInProcess("NuclearOption.exe")]
    internal class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;
        private Recorder? recorder;
        private int ticker = 0;
        private float waitTime = 0.2f;
        private float timer = 0f;

        public Plugin()
        {
            Logger = base.Logger;

            MissionManager.onMissionStart += OnMissionLoad;
            LoadingManager.MissionUnloaded += OnMissionUnload;
        }
        private void Awake()
        {
            Configuration.InitSettings(Config);
            Logger.LogInfo("[NOBlackBox]: LOADED.");

            waitTime = 1f / Configuration.UpdateRate.Value;
            waitTime = MathF.Round(waitTime, 3);
        }
        private void Update()
        {
            timer += Time.deltaTime;
            if (recorder != null && timer >= waitTime)
            {
                recorder.Update(timer);
                timer = 0f;
            }
        }
        private void OnMissionLoad(Mission mission)
        {
            Logger.LogInfo("[NOBlackBox]: MISSION LOADED.");
            recorder = new Recorder(mission);
        }
        private void OnMissionUnload()
        {
            Logger.LogInfo("[NOBlackBox]: MISSION UNLOADED.");
            recorder?.Close();
            recorder = null;
        }
    }
}
