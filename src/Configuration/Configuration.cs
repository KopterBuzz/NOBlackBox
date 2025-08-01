﻿using BepInEx.Configuration;
using System.Linq;
using UnityEngine;
using System;

namespace NOBlackBox
{
    public static class Configuration
    {
        internal const string GeneralSettings = "General Settings";
        internal const string OptionalDataSettings = "Optional Data Settings";
        internal const string HeightMapGeneratorSettings = "Heightmap Generator Settings";
        internal const string VisualSettings = "Visual Settings";
        internal const string DeveloperFeatures = "Developer Features";

        internal const int DefaultUpdateRate = 5;

        internal const int DefaultAutoSaveInterval = 60;

        internal const bool DefaultUseMissionTime = true;

        internal const bool DefaultRecordSpeed = true;
        internal const bool DefaultRecordAOA = true;
        internal const bool DefaultRecordAGL = true;
        internal const bool DefaultRecordRadarMode = true;
        internal const bool DefaultRecordLandingGear = true;
        internal const bool DefaultRecordPilotHead = true;
        internal const bool DefaultCompressIDs = false;
        
        internal const KeyCode DefaultGenerateHeightMapKey = KeyCode.F10;
        internal const int DefaultMetersPerScan = 4;
        internal const int DefaultHeightMapResolution = 4096;
        internal const bool DefaultEnableHeightmapGenerator = false;
        internal const bool DefaultTacviewBetaHeightMapGenerator = true;
        //internal const float DefaultOrtoGraphicSize = 5f;

        internal const float DefaultTextColorR = 1.0f;
        internal const float DefaultTextColorG = 1.0f;
        internal const float DefaultTextColorB = 1.0f;
        internal const float DefaultTextColorA = 1.0f;

        internal const bool DefaultEnableAutoSaveCountDown = false;
        internal const float DefaultAutoSaveCountDownX = 0.1f;
        internal const float DefaultAutoSaveCountDownY = 0.1f;

        internal const bool DefaultEnableUnitLogging = false;
        internal const bool DefaultEnableEncyclopediaExporter = false;
        internal const KeyCode DefaultEncyclopediaExporterKey = KeyCode.F11;

#pragma warning disable CS8618
        private static ConfigEntry<int> _UpdateRate;
        private static ConfigEntry<string> _OutputPath;
        private static ConfigEntry<int> _AutoSaveInterval;
        private static ConfigEntry<bool> _CompressIDs;
        internal static ConfigEntry<bool> UseMissionTime;
        internal static ConfigEntry<bool> RecordSpeed;
        internal static ConfigEntry<bool> RecordAOA;
        internal static ConfigEntry<bool> RecordAGL;
        internal static ConfigEntry<bool> RecordRadarMode;
        internal static ConfigEntry<bool> RecordLandingGear;
        internal static ConfigEntry<bool> RecordPilotHead;

        internal static ConfigEntry<int> HeightMapResolution;
        internal static ConfigEntry<int> MetersPerScan;
        internal static ConfigEntry<KeyboardShortcut> _GenerateHeightMapKey;
        internal static ConfigEntry<bool> EnableHeightmapGenerator;
        //internal static ConfigEntry <bool> TacviewBetaHeightMapGenerator;
        //internal static ConfigEntry<float> OrtoGraphicSize;

        internal static ConfigEntry<float> TextColorR;
        internal static ConfigEntry<float> TextColorG;
        internal static ConfigEntry<float> TextColorB;
        internal static ConfigEntry<float> TextColorA;

        internal static ConfigEntry<bool> EnableAutoSaveCountDown;
        internal static ConfigEntry<float> AutoSaveCountDownX;
        internal static ConfigEntry<float> AutoSaveCountDownY;

        internal static ConfigEntry<bool> EnableUnitLogging;
        internal static ConfigEntry<bool> EnableEncyclopediaExporter;
        internal static ConfigEntry<KeyboardShortcut> EncyclopediaExporterKey;
#pragma warning restore

        internal static int UpdateRate
        {
            get
            {
                return _UpdateRate.Value;
            }
        }

        internal static string OutputPath
        {
            get
            {
                return _OutputPath.Value;
            }
        }

        internal static int AutoSaveInterval
        {
            get
            {
                return _AutoSaveInterval.Value;
            }
        }

        internal static bool CompressIDs
        {
            get
            {
                return _CompressIDs.Value;
            }
        }

        internal static bool GenerateHeightMapKey
        {
            get
            {
                return _GenerateHeightMapKey.Value.IsDown();
            }
        }


        internal static void InitSettings(ConfigFile config)
        {
            Plugin.Logger?.LogInfo("[NOBlackBox]: Loading Settings.");

            _UpdateRate = config.Bind(GeneralSettings, "UpdateRate", DefaultUpdateRate, "The number of times per second NOBlackBox will record events. 0 = unlimited. Max Value: 1000");
            Plugin.Logger?.LogInfo($"[NOBlackBox]: UpdateRate = {_UpdateRate.Value}");
            if (!Enumerable.Range(0, 1001).Contains(_UpdateRate.Value))
            {
                Plugin.Logger?.LogWarning($"[NOBlackBox]: UpdateRate out of range! Setting default value {DefaultUpdateRate}!");
                _UpdateRate.Value = DefaultUpdateRate;
            }

            string DefaultOutputPath = Application.persistentDataPath + "/Replays/";
            _OutputPath = config.Bind(GeneralSettings, "OutputPath", DefaultOutputPath, "The location where Tacview files will be saved. Must be a valid folder path.");
            Plugin.Logger?.LogInfo($"[NOBlackBox]: OutputPath = {_OutputPath.Value}");

            (bool isFolder, bool success) = Helpers.IsFileOrFolder(_OutputPath.Value);
            if (!isFolder || !success)
            {
                Plugin.Logger?.LogWarning($"[NOBlackBox]: Invalid OutputPath! Setting default value {DefaultOutputPath}!");
                _OutputPath.Value = DefaultOutputPath;
            }

            _AutoSaveInterval = config.Bind(GeneralSettings, "AutoSaveInterval", DefaultAutoSaveInterval, "Time interval for automatically updating the Tacview file. Min value: 60");
            Plugin.Logger?.LogInfo($"[NOBlackBox]: AutoSaveInterval = {_AutoSaveInterval.Value}");

            if (_AutoSaveInterval.Value < 60)
            {
                Plugin.Logger?.LogWarning($"[NOBlackBox]: Invalid AutoSaveInterval! Setting default value {DefaultAutoSaveInterval}!");
                _AutoSaveInterval.Value = DefaultAutoSaveInterval;
            }

            UseMissionTime = config.Bind(OptionalDataSettings, "UseMissionTime", DefaultUseMissionTime, "Use Mission (true) or Server Time (false) for the clock in the recording.");
            Plugin.Logger?.LogInfo($"[NOBlackBox]: UseMissionTime = {UseMissionTime.Value}");

            RecordSpeed = config.Bind(OptionalDataSettings, "RecordSpeed", DefaultRecordSpeed, "Toggle recording True Airspeed and Mach number. Default: true");
            Plugin.Logger?.LogInfo($"[NOBlackBox]: RecordSpeed = {RecordSpeed?.Value}");

            RecordAOA = config.Bind(OptionalDataSettings, "RecordAOA", DefaultRecordAOA, "Toggle recording Angle of Attack. Default: true");
            Plugin.Logger?.LogInfo($"[NOBlackBox]: RecordAOA = {RecordAOA?.Value}");

            RecordAGL = config.Bind(OptionalDataSettings, "RecordAGL", DefaultRecordAGL, "Toggle recording height above ground level. Default: true");
            Plugin.Logger?.LogInfo($"[NOBlackBox]: RecordAGL = {RecordAGL?.Value}");

            RecordRadarMode = config.Bind(OptionalDataSettings, "RecordRadarMode", DefaultRecordRadarMode, "Toggle recording radar mode changes. Default: true");
            Plugin.Logger?.LogInfo($"[NOBlackBox]: RecordRadarMode = {RecordRadarMode?.Value}");

            RecordLandingGear = config.Bind(OptionalDataSettings, "RecordLandingGear", DefaultRecordLandingGear, "Toggle recording landing gear changes. Default: true");
            Plugin.Logger?.LogInfo($"[NOBlackBox]: RecordLandingGear = {RecordLandingGear?.Value}");

            RecordPilotHead = config.Bind(OptionalDataSettings, "RecordPilotHead", DefaultRecordPilotHead, "Toggle recording pilot head movement. Default: true");
            Plugin.Logger?.LogInfo($"[NOBlackBox]: RecordPilotHead = {RecordPilotHead?.Value}");

            _CompressIDs = config.Bind(OptionalDataSettings, "CompressIDs", DefaultCompressIDs, "Compress IDs to reduce filesize with less determinism.");
            Plugin.Logger?.LogInfo($"[NOBlackBox]: CompressIDs = {_CompressIDs.Value}");

            MetersPerScan = config.Bind(HeightMapGeneratorSettings, "MetersPerScan", DefaultMetersPerScan, "Sample rate of the Heightmap generator. Does a scan per X meter. Default: 4");
            if (MetersPerScan.Value < 1)
            {
                Plugin.Logger?.LogWarning($"[NOBlackBox]: Invalid MetersPerScan! Setting default value {DefaultMetersPerScan}!");
                MetersPerScan.Value = DefaultMetersPerScan;
            }
            Plugin.Logger?.LogInfo($"[NOBlackBox]: MetersPerScan = {MetersPerScan.Value}");

            HeightMapResolution = config.Bind(HeightMapGeneratorSettings, "HeightMapResolution", DefaultHeightMapResolution, "Resolution of the Heightmap. Must be divisible by 4. Default: 4096");
            if ((HeightMapResolution.Value % 4) != 0)
            {
                Plugin.Logger?.LogWarning($"[NOBlackBox]: HeightMapResolution must be divisible by 4! Setting default value {DefaultHeightMapResolution}!");
                HeightMapResolution.Value = DefaultHeightMapResolution;
            }
            Plugin.Logger?.LogInfo($"[NOBlackBox]: HeightMapResolution = {HeightMapResolution.Value}");
            /*
            OrtoGraphicSize = config.Bind(HeightMapGeneratorSettings, "OrtographicSize", DefaultOrtoGraphicSize, "Size of Ortographic Camera for Texture Generation. Must be bigger than 0f.");
            if (OrtoGraphicSize.Value <= 0f)
            {
                Plugin.Logger?.LogWarning($"[NOBlackBox]: OrtographicSize must be divisible by 4! Setting default value {DefaultOrtoGraphicSize}!");
                OrtoGraphicSize.Value = DefaultOrtoGraphicSize;
            }
            Plugin.Logger?.LogInfo($"[NOBlackBox]: OrtographicSize = {OrtoGraphicSize.Value}");
            */
            _GenerateHeightMapKey = config.Bind("Hotkeys", "Generate Heightmap", new KeyboardShortcut(DefaultGenerateHeightMapKey));
            Plugin.Logger?.LogInfo($"[NOBlackBox]: Generate Heightmap key = {_GenerateHeightMapKey.Value}");

            EnableHeightmapGenerator = config.Bind(HeightMapGeneratorSettings, "EnableHeightmapGenerator", DefaultEnableHeightmapGenerator, "Enable/Disable Heightmap Generator. Default: false");
            Plugin.Logger?.LogInfo($"[NOBlackBox]: EnableHeightmapGenerator = {EnableHeightmapGenerator.Value}");

            //TacviewBetaHeightMapGenerator = config.Bind(HeightMapGeneratorSettings, "TacviewBetaHeightMapGenerator", DefaultTacviewBetaHeightMapGenerator, "True: Compatibility set for Tacview 1.9.5 Beta 11, False: Compatibility set for Tacview Stable. Default: True");
            //Plugin.Logger?.LogInfo($"[NOBlackBox]: TacviewBetaHeightMapGenerator = {TacviewBetaHeightMapGenerator.Value}");

            EnableAutoSaveCountDown = config.Bind(VisualSettings, "EnableAutoSaveCountDown", DefaultEnableAutoSaveCountDown, "Toggle AutoSave Countdown Timer.");
            Plugin.Logger?.LogInfo($"[NOBlackBox]: EnableAutoSaveCountDown = {EnableAutoSaveCountDown.Value}");

            AutoSaveCountDownX = config.Bind(VisualSettings, "AutoSaveCountDownX", DefaultAutoSaveCountDownX, "X coordinate of Auto Save Countdown Timer on GUI. Scales with Resolution. Value range: 0.0 - 1.0");
            if (AutoSaveCountDownX.Value < 0f || AutoSaveCountDownX.Value > 1.0f)
            {
                Plugin.Logger?.LogWarning($"[NOBlackBox]: AutoSaveCountDownX must be within 0.0 - 1.0 range! Setting default value {DefaultAutoSaveCountDownX}!");
            }
            Plugin.Logger?.LogInfo($"[NOBlackBox]: AutoSaveCountDownX = {AutoSaveCountDownX.Value}");

            AutoSaveCountDownY = config.Bind(VisualSettings, "AutoSaveCountDownY", DefaultAutoSaveCountDownY, "Y coordinate of Auto Save Countdown Timer on GUI. Scales with Resolution. Value range: 0.0 - 1.0");
            if (AutoSaveCountDownY.Value < 0f || AutoSaveCountDownY.Value > 1.0f)
            {
                Plugin.Logger?.LogWarning($"[NOBlackBox]: AutoSaveCountDownX must be within 0.0 - 1.0 range! Setting default value {DefaultAutoSaveCountDownX}!");
            }
            Plugin.Logger?.LogInfo($"[NOBlackBox]: AutoSaveCountDownX = {AutoSaveCountDownY.Value}");

            TextColorR = config.Bind(VisualSettings, "TextColorR", DefaultTextColorR, "Red color value for GUI Text. Value range: 0.0 - 1.0");
            if (TextColorR.Value < 0f || TextColorR.Value > 1.0f)
            {
                Plugin.Logger?.LogWarning($"[NOBlackBox]: TextColorR must be within 0.0 - 1.0 range! Setting default value {DefaultTextColorR}!");
            }
            Plugin.Logger?.LogInfo($"[NOBlackBox]: TextColorR = {TextColorR.Value}");

            TextColorG = config.Bind(VisualSettings, "TextColorG", DefaultTextColorG, "Green color value for GUI Text. Value range: 0.0 - 1.0");
            if (TextColorG.Value < 0f || TextColorG.Value > 1.0f)
            {
                Plugin.Logger?.LogWarning($"[NOBlackBox]: TextColorG must be within 0.0 - 1.0 range! Setting default value {DefaultTextColorG}!");
            }
            Plugin.Logger?.LogInfo($"[NOBlackBox]: TextColorG = {TextColorG.Value}");

            TextColorB = config.Bind(VisualSettings, "TextColorB", DefaultTextColorB, "Blue color value for GUI Text. Value range: 0.0 - 1.0");
            if (TextColorB.Value < 0f || TextColorB.Value > 1.0f)
            {
                Plugin.Logger?.LogWarning($"[NOBlackBox]: TextColorB must be within 0.0 - 1.0 range! Setting default value {DefaultTextColorB}!");
            }
            Plugin.Logger?.LogInfo($"[NOBlackBox]: TextColorB = {TextColorB.Value}");

            TextColorA = config.Bind(VisualSettings, "TextColorA", DefaultTextColorA, "Transparency value for GUI Text. Value range: 0.0 - 1.0");
            if (TextColorA.Value < 0f || TextColorA.Value > 1.0f)
            {
                Plugin.Logger?.LogWarning($"[NOBlackBox]: TextColorA must be within 0.0 - 1.0 range! Setting default value {DefaultTextColorA}!");
            }
            Plugin.Logger?.LogInfo($"[NOBlackBox]: TextColorA = {TextColorA.Value}");

            EnableUnitLogging = config.Bind(DeveloperFeatures, "EnableUnknownUnitLogging", DefaultEnableUnitLogging, "Toggle logging Unknown Units that are unknown to ACMI Recorder. Default: false");
            Plugin.Logger?.LogInfo($"[NOBlackBox]: EnableUnknownUnitLogging = {EnableUnitLogging.Value}");

            EnableEncyclopediaExporter = config.Bind(DeveloperFeatures, "EnableEncyclopediaExporter", DefaultEnableEncyclopediaExporter, "Toggle Encyclopedia Exporter. Default: false");
            Plugin.Logger?.LogInfo($"[NOBlackBox]: EnableEncyclopediaExporter = {EnableEncyclopediaExporter.Value}");

            EncyclopediaExporterKey = config.Bind("Hotkeys", "EncyclopediaExporterKey", new KeyboardShortcut(DefaultEncyclopediaExporterKey));
            Plugin.Logger?.LogInfo($"[NOBlackBox]: EncyclopediaExporterKey = {EncyclopediaExporterKey.Value}");

        }
    }
}