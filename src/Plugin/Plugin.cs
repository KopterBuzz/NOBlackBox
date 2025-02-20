using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using System;
using System.Threading.Tasks;

#if BEP6
using BepInEx.Unity.Mono;
#endif


namespace NOBlackBox
{
    [BepInPlugin("xyz.KopterBuzz.NOBlackBox", MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInProcess("NuclearOption.exe")]
    internal class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource ?Logger;
        private Recorder? recorder;
        private float waitTime = 0.2f;
        private float timer = 0f;

        public Plugin()
        {
            Logger = base.Logger;

            LoadingManager.MissionLoaded += OnMissionLoad;
            LoadingManager.MissionUnloaded += OnMissionUnload;
        }
        private void Awake()
        {
            Configuration.InitSettings(Config);
            Logger?.LogInfo("[NOBlackBox]: LOADED.");

            waitTime = Configuration.UpdateRate != 0 ? 1f / Configuration.UpdateRate : 0f;
            //waitTime = 1f / Configuration.UpdateRate.Value;
            waitTime = MathF.Round(waitTime, 3);
            Logger?.LogInfo($"[NOBlackBox]: Wait Time = {waitTime}");
        }
        private void Update()
        {
            if (recorder == null)
                return;

            if (waitTime > 0)
            {
                timer += Time.deltaTime;

                if (timer < waitTime)
                    return;

                timer = 0f;
            }

            recorder.Update(timer);
        }

        private async Task<bool> WaitForLocalPlayer()
        {
            Logger?.LogInfo("[NOBlackBox]: TRYING TO GET PLAYERNAME...");
            Logger?.LogInfo($"[NOBlackBox]: {GameManager.LocalPlayer.PlayerName}");
            while (GameManager.LocalPlayer.PlayerName == null)
            {
                Logger?.LogInfo("[NOBlackBox]: Waiting for LocalPlayer...");
                await Task.Delay(100);
            }
            return true;
        }

        private async void OnMissionLoad()
        {
            await WaitForLocalPlayer();
            Logger?.LogInfo("[NOBlackBox]: MISSION LOADED.");
            recorder = new Recorder(MissionManager.CurrentMission);
        }
        private void OnMissionUnload()
        {
            Logger?.LogInfo("[NOBlackBox]: MISSION UNLOADED.");
            recorder?.Close();
            recorder = null;
        }
    }
}
