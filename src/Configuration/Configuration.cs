using BepInEx.Configuration;
using System.Linq;
using UnityEngine;
using System;

namespace NOBlackBox
{
    public static class Configuration
    {
        internal const string GeneralSettings = "General Settings";

        internal const int DefaultUpdateRate = 5;

        internal const int DefaultAutoSaveInterval = 60;

        internal const bool DefaultUseMissionTime = true;

        internal const bool DefaultRecordTAS = true;
        internal const bool DefaultRecordMach = true;
        internal const bool DefaultRecordAOA = true;
        internal const bool DefaultRecordAGL = true;
        internal const bool DefaultRecordRadarMode = true;
        internal const bool DefaultRecordLandingGear = true;
        internal const bool DefaultRecordPilotHead = true;

        internal static ConfigEntry<int>? UpdateRate;
        internal static ConfigEntry<string>? OutputPath;
        internal static ConfigEntry<int>? AutoSaveInterval;
        internal static ConfigEntry<bool>? UseMissionTime;
        internal static ConfigEntry<bool>? RecordTAS;
        internal static ConfigEntry<bool>? RecordMach;
        internal static ConfigEntry<bool>? RecordAOA;
        internal static ConfigEntry<bool>? RecordAGL;
        internal static ConfigEntry<bool>? RecordRadarMode;
        internal static ConfigEntry<bool>? RecordLandingGear;
        internal static ConfigEntry<bool>? RecordPilotHead;

        internal static void InitSettings(ConfigFile config)
        {
            Plugin.Logger?.LogInfo("[NOBlackBox]: Loading Settings.");

            UpdateRate = config.Bind(GeneralSettings, "UpdateRate", DefaultUpdateRate, "The number of times per second NOBlackBox will record events. 0 = unlimited. Max Value: 1000");
            Plugin.Logger?.LogInfo($"[NOBlackBox]: UpdateRate = {UpdateRate.Value}");
            if (!Enumerable.Range(0,1001).Contains(UpdateRate.Value))
            {
                Plugin.Logger?.LogWarning($"[NOBlackBox]: UpdateRate out of range! Setting default value {DefaultUpdateRate}!");
                UpdateRate.Value = DefaultUpdateRate;
            }
            
            string DefaultOutputPath = Application.persistentDataPath + "/Replays/";
            OutputPath = config.Bind(GeneralSettings, "OutputPath", DefaultOutputPath, "The location where Tacview files will be saved. Must be a valid folder path.");
            Plugin.Logger?.LogInfo($"[NOBlackBox]: OutputPath = {OutputPath.Value}");

            (bool isFolder, bool success) = Helpers.IsFileOrFolder(OutputPath.Value);
            if (!isFolder || !success)
            {
                Plugin.Logger?.LogWarning($"[NOBlackBox]: Invalid OutputPath! Setting default value {DefaultOutputPath}!");
                OutputPath.Value = DefaultOutputPath;
            }

            AutoSaveInterval = config.Bind(GeneralSettings, "AutoSaveInterval", DefaultAutoSaveInterval, "Time interval for automatically updating the Tacview file. Min value: 60");
            Plugin.Logger?.LogInfo($"[NOBlackBox]: AutoSaveInterval = {AutoSaveInterval.Value}");

            if (AutoSaveInterval.Value < 60)
            {
                Plugin.Logger?.LogWarning($"[NOBlackBox]: Invalid AutoSaveInterval! Setting default value {DefaultAutoSaveInterval}!");
                AutoSaveInterval.Value = DefaultAutoSaveInterval;
            }

            UseMissionTime = config.Bind(GeneralSettings, "UseMissionTime", DefaultUseMissionTime, "Use Mission (true) or Server Time (false) for the clock in the recording.");
            Plugin.Logger?.LogInfo($"[NOBlackBox]: UseMissionTime = {UseMissionTime.Value}");

            UseMissionTime = config.Bind(GeneralSettings, "RecordTAS", DefaultRecordTAS, "Toggle recording True Airspeed. Default: true");
            Plugin.Logger?.LogInfo($"[NOBlackBox]: RecordTAS = {RecordTAS.Value}");

            UseMissionTime = config.Bind(GeneralSettings, "RecordTAS", DefaultRecordMach, "Toggle recording Mach number. Default: true");
            Plugin.Logger?.LogInfo($"[NOBlackBox]: RecordMach = {RecordMach.Value}");

            UseMissionTime = config.Bind(GeneralSettings, "RecordAOA", DefaultRecordAOA, "Toggle recording Angle of Attack. Default: true");
            Plugin.Logger?.LogInfo($"[NOBlackBox]: RecordAOA = {RecordAOA.Value}");

            UseMissionTime = config.Bind(GeneralSettings, "RecordAGL", DefaultRecordAGL, "Toggle recording height above ground level. Default: true");
            Plugin.Logger?.LogInfo($"[NOBlackBox]: RecordAGL = {RecordAGL.Value}");

            UseMissionTime = config.Bind(GeneralSettings, "RecordRadarMode", DefaultRecordRadarMode, "Toggle recording radar mode changes. Default: true");
            Plugin.Logger?.LogInfo($"[NOBlackBox]: RecordRadarMode = {RecordRadarMode.Value}");

            UseMissionTime = config.Bind(GeneralSettings, "RecordLandingGear", DefaultRecordLandingGear, "Toggle recording landing gear changes. Default: true");
            Plugin.Logger?.LogInfo($"[NOBlackBox]: RecordLandingGear = {RecordLandingGear.Value}");

            UseMissionTime = config.Bind(GeneralSettings, "RecordPilotHead", DefaultRecordPilotHead, "Toggle recording pilot head movement. Default: true");
            Plugin.Logger?.LogInfo($"[NOBlackBox]: RecordPilotHead = {RecordPilotHead.Value}");
        }
    }
}
