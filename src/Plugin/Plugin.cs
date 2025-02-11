using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using NuclearOption.SavedMission;
using System.Collections;


namespace NOBlackBox
{
    [BepInPlugin("xyz.KopterBuzz.NOBlackBox", "NOBlackBox", "0.2.0")]
    [BepInProcess("NuclearOption.exe")]
    internal class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;
        private Recorder? recorder;

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
           Logger.LogInfo("[NOBlackBox]: LOADED.");
        }
        private void Update()
        {
            timer += Time.deltaTime;
            Logger.LogInfo(Time.deltaTime.ToString());
            if (recorder != null && timer >= waitTime)
            {
                Logger.LogInfo("[NOBlackBox]: UPDATE!");
                recorder?.Update(timer);
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
            recorder?.CloseStreamWriter();
            recorder = null;
        }
    }
}
