using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using NuclearOption.SavedMission;


namespace NOBlackBox
{
    [BepInPlugin("xyz.KopterBuzz.NOBlackBox", "NOBlackBox", "0.2.0")]
    [BepInProcess("NuclearOption.exe")]
    internal class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;
        private Recorder? recorder;
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
        private void Update() => recorder?.Update(Time.deltaTime);
        private void OnMissionLoad(Mission mission)
        {
            Logger.LogInfo("[NOBlackBox]: MISSION LOADED.");
            recorder = new Recorder(mission);
        }
        private void OnMissionUnload()
        {
            Logger.LogInfo("[NOBlackBox]: MISSION UNLOADED.");
            recorder.CloseStreamWriter();
            recorder = null;
        }
    }
}
