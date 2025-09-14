using BepInEx.Configuration;
using System.Linq;
using UnityEngine;
using System;
using System.Globalization;

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

        internal const float DefaultUnitDiscoveryDelta = 1f;
        internal const float DefaultBulletSimDiscoveryDelta = 1f;
        internal const float DefaultAircraftUpdateDelta = 0.2f;
        internal const float DefaultVehicleUpdateDelta = 1f;
        internal const float DefaultMunitionUpdateDelta = 0.2f;
        internal const float DefaultShockwaveUpdateDelta = 0.016f;
        internal const float DefaultShockwaveDiscoveryDelta = 0.5f;
        internal const float DefaultTracerUpdateDelta = 1f;
        internal const float DefaultFlareUpdateDelta = 1f;
        internal const float DefaultBuildingUpdateDelta = 1f;

        internal const int DefaultAutoSaveInterval = 60;

        internal const bool DefaultUseMissionTime = true;
        internal const bool DefaultRecordSteamID = true;

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

        internal const bool DefaultRecordEjectedPilots = false;
        internal const bool DefaultAutoStartRecording = true;

        internal const bool DefaultEnableUnitLogging = false;
        internal const bool DefaultEnableEncyclopediaExporter = false;
        internal const KeyCode DefaultEncyclopediaExporterKey = KeyCode.F11;

        internal const KeyCode DefaultStartStopRecordingKey = KeyCode.F8;

#pragma warning disable CS8618
        private static ConfigEntry<int> _UpdateRate;

        internal static ConfigEntry<float> unitDiscoveryDelta;
        internal static ConfigEntry<float> bulletSimDiscoveryDelta;
        internal static ConfigEntry<float> aircraftUpdateDelta;
        internal static ConfigEntry<float> vehicleUpdateDelta;
        internal static ConfigEntry<float> munitionUpdateDelta;
        internal static ConfigEntry<float> shockwaveUpdateDelta;
        internal static ConfigEntry<float> shockwaveDiscoveryDelta;
        internal static ConfigEntry<float> tracerUpdateDelta;
        internal static ConfigEntry<float> flareUpdateDelta;
        internal static ConfigEntry<float> buildingUpdateDelta;

        private static ConfigEntry<string> _OutputPath;
        private static ConfigEntry<int> _AutoSaveInterval;
        private static ConfigEntry<bool> _CompressIDs;
        internal static ConfigEntry<bool> UseMissionTime;
        internal static ConfigEntry<bool> RecordSteamID;
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
        internal static ConfigEntry<bool> RecordEjectedPilots;
        internal static ConfigEntry<bool> AutoStartRecording;

        internal static ConfigEntry<KeyboardShortcut> EncyclopediaExporterKey;

        internal static ConfigEntry<KeyboardShortcut> StartStopRecordingKey;

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
            Plugin.Logger?.LogDebug("Loading Settings.");

            _UpdateRate = config.Bind(GeneralSettings, "UpdateRate", DefaultUpdateRate, "DEPRECATED. SEE THE OTHER UPDATE RATE SETTINGS");
            Plugin.Logger?.LogDebug($"UpdateRate = {_UpdateRate.Value}");
            if (!Enumerable.Range(0, 1001).Contains(_UpdateRate.Value))
            {
                Plugin.Logger?.LogWarning($"UpdateRate out of range! Setting default value {DefaultUpdateRate}!");
                _UpdateRate.Value = DefaultUpdateRate;
            }
            //NEW UPDATE RATE SETTINGS
            unitDiscoveryDelta = config.Bind(GeneralSettings, "Unit Discovery Rate", DefaultUnitDiscoveryDelta, $"How many times per second to discover Units. Default = {DefaultUnitDiscoveryDelta.ToString(CultureInfo.InvariantCulture)}");
            if (unitDiscoveryDelta.Value <= 0f)
            {
                Plugin.Logger?.LogWarning($"Invalid unitDiscoveryDelta! Setting default value {DefaultUnitDiscoveryDelta}!");
                Plugin.Logger?.LogDebug($"unitDiscoveryDelta = {unitDiscoveryDelta.Value.ToString(CultureInfo.InvariantCulture)}");
            }
            bulletSimDiscoveryDelta = config.Bind(GeneralSettings, "BulletSim Discovery Rate", DefaultBulletSimDiscoveryDelta, $"How many times per second to discover objects that fire bullets. Default = {DefaultBulletSimDiscoveryDelta.ToString(CultureInfo.InvariantCulture)}");
            if (bulletSimDiscoveryDelta.Value <= 0f)
            {
                Plugin.Logger?.LogWarning($"Invalid bulletSimDiscoveryDelta! Setting default value {DefaultBulletSimDiscoveryDelta}!");
                Plugin.Logger?.LogDebug($"bulletSimDiscoveryDelta = {bulletSimDiscoveryDelta.Value.ToString(CultureInfo.InvariantCulture)}");
            }
            shockwaveDiscoveryDelta = config.Bind(GeneralSettings, "Shockwave Discovery Rate", DefaultShockwaveDiscoveryDelta, $"How many times per second to discover explosion shockwaves. Default = {DefaultShockwaveDiscoveryDelta.ToString(CultureInfo.InvariantCulture)}");
            if (shockwaveDiscoveryDelta.Value <= 0f)
            {
                Plugin.Logger?.LogWarning($"Invalid shockwaveDiscoveryDelta! Setting default value {DefaultShockwaveDiscoveryDelta}!");
                Plugin.Logger?.LogDebug($"shockwaveDiscoveryDelta = {shockwaveDiscoveryDelta.Value.ToString(CultureInfo.InvariantCulture)}");
            }
            aircraftUpdateDelta = config.Bind(GeneralSettings, "Aircraft Update Rate", DefaultAircraftUpdateDelta, $"How many times per second to update Aircraft. Default = {DefaultAircraftUpdateDelta.ToString(CultureInfo.InvariantCulture)}");
            if (aircraftUpdateDelta.Value <= 0f)
            {
                Plugin.Logger?.LogWarning($"Invalid aircraftUpdateDelta! Setting default value {DefaultAircraftUpdateDelta}!");
                Plugin.Logger?.LogDebug($"aircraftUpdateDelta = {aircraftUpdateDelta.Value.ToString(CultureInfo.InvariantCulture)}");
            }
            vehicleUpdateDelta = config.Bind(GeneralSettings, "Vehicle Update Rate", DefaultVehicleUpdateDelta, $"How many times per second to update Vehicles and Ships. Default = {DefaultVehicleUpdateDelta.ToString(CultureInfo.InvariantCulture)}");
            if (vehicleUpdateDelta.Value <= 0f)
            {
                Plugin.Logger?.LogWarning($"Invalid vehicleUpdateDelta! Setting default value {DefaultVehicleUpdateDelta}!");
                Plugin.Logger?.LogDebug($"vehicletUpdateDelta = {vehicleUpdateDelta.Value.ToString(CultureInfo.InvariantCulture)}");
            }
            munitionUpdateDelta = config.Bind(GeneralSettings, "Munition Update Rate", DefaultMunitionUpdateDelta, $"How many times per second to update Bombs, Missiles and Rockets. Default = {DefaultMunitionUpdateDelta.ToString(CultureInfo.InvariantCulture)}");
            if (munitionUpdateDelta.Value <= 0f)
            {
                Plugin.Logger?.LogWarning($"Invalid munitionUpdateDelta! Setting default value {DefaultMunitionUpdateDelta}!");
                Plugin.Logger?.LogDebug($"munitionUpdateDelta = {munitionUpdateDelta.Value.ToString(CultureInfo.InvariantCulture)}");
            }
            shockwaveUpdateDelta = config.Bind(GeneralSettings, "Shockwave Update Rate", DefaultShockwaveUpdateDelta, $"How many times per second to update Shockwave Propagation. Default = {DefaultShockwaveUpdateDelta.ToString(CultureInfo.InvariantCulture)}");
            if (shockwaveUpdateDelta.Value <= 0f)
            {
                Plugin.Logger?.LogWarning($"Invalid shockwaveUpdateDelta! Setting default value {DefaultShockwaveUpdateDelta}!");
                Plugin.Logger?.LogDebug($"aircraftUpdateDelta = {shockwaveUpdateDelta.Value.ToString(CultureInfo.InvariantCulture)}");
            }
            tracerUpdateDelta = config.Bind(GeneralSettings, "Tracer Update Rate", DefaultTracerUpdateDelta, $"How many times per second to Projectile Tracers. Default = {DefaultTracerUpdateDelta.ToString(CultureInfo.InvariantCulture)}");
            if (tracerUpdateDelta.Value <= 0f)
            {
                Plugin.Logger?.LogWarning($"Invalid tracerUpdateDelta! Setting default value {DefaultTracerUpdateDelta}!");
                Plugin.Logger?.LogDebug($"tracerUpdateDelta = {tracerUpdateDelta.Value.ToString(CultureInfo.InvariantCulture)}");
            }
            flareUpdateDelta = config.Bind(GeneralSettings, "Flare Update Rate", DefaultFlareUpdateDelta, $"How many times per second to update Flares. Default = {DefaultFlareUpdateDelta.ToString(CultureInfo.InvariantCulture)}");
            if (flareUpdateDelta.Value <= 0f)
            {
                Plugin.Logger?.LogWarning($"Invalid flareUpdateDelta! Setting default value {DefaultFlareUpdateDelta}!");
                Plugin.Logger?.LogDebug($"flareUpdateDelta = {flareUpdateDelta.Value.ToString(CultureInfo.InvariantCulture)}");
            }
            buildingUpdateDelta = config.Bind(GeneralSettings, "Building Update Rate", DefaultBuildingUpdateDelta, $"How many times per second to update Buildings. Default = {DefaultBuildingUpdateDelta.ToString(CultureInfo.InvariantCulture)}");
            if (buildingUpdateDelta.Value <= 0f)
            {
                Plugin.Logger?.LogWarning($"Invalid buildingUpdateDelta! Setting default value {DefaultBuildingUpdateDelta}!");
                Plugin.Logger?.LogDebug($"buildingUpdateDelta = {buildingUpdateDelta.Value.ToString(CultureInfo.InvariantCulture)}");
            }


            string DefaultOutputPath = Application.persistentDataPath + "/Replays/";
            _OutputPath = config.Bind(GeneralSettings, "OutputPath", DefaultOutputPath, "The location where Tacview files will be saved. Must be a valid folder path.");
            Plugin.Logger?.LogDebug($"OutputPath = {_OutputPath.Value}");

            (bool isFolder, bool success) = Helpers.IsFileOrFolder(_OutputPath.Value);
            if (!isFolder || !success)
            {
                Plugin.Logger?.LogWarning($"Invalid OutputPath! Setting default value {DefaultOutputPath}!");
                _OutputPath.Value = DefaultOutputPath;
            }

            _AutoSaveInterval = config.Bind(GeneralSettings, "AutoSaveInterval", DefaultAutoSaveInterval, "Time interval for automatically updating the Tacview file. Min value: 60");
            Plugin.Logger?.LogDebug($"AutoSaveInterval = {_AutoSaveInterval.Value}");

            if (_AutoSaveInterval.Value < 60)
            {
                Plugin.Logger?.LogWarning($"Invalid AutoSaveInterval! Setting default value {DefaultAutoSaveInterval}!");
                _AutoSaveInterval.Value = DefaultAutoSaveInterval;
            }

            UseMissionTime = config.Bind(OptionalDataSettings, "UseMissionTime", DefaultUseMissionTime, "Use Mission (true) or Server Time (false) for the clock in the recording.");
            Plugin.Logger?.LogDebug($"UseMissionTime = {UseMissionTime.Value}");

            RecordSteamID = config.Bind(OptionalDataSettings, "RecordSteamID", DefaultRecordSteamID, "Record Steam ID in the Registration label of player aircraft objects.");
            Plugin.Logger?.LogDebug($"RecordSteamID = {RecordSteamID.Value}");

            RecordSpeed = config.Bind(OptionalDataSettings, "RecordSpeed", DefaultRecordSpeed, "Toggle recording True Airspeed and Mach number. Default: true");
            Plugin.Logger?.LogDebug($"RecordSpeed = {RecordSpeed?.Value}");

            RecordAOA = config.Bind(OptionalDataSettings, "RecordAOA", DefaultRecordAOA, "Toggle recording Angle of Attack. Default: true");
            Plugin.Logger?.LogDebug($"RecordAOA = {RecordAOA?.Value}");

            RecordAGL = config.Bind(OptionalDataSettings, "RecordAGL", DefaultRecordAGL, "Toggle recording height above ground level. Default: true");
            Plugin.Logger?.LogDebug($"RecordAGL = {RecordAGL?.Value}");

            RecordRadarMode = config.Bind(OptionalDataSettings, "RecordRadarMode", DefaultRecordRadarMode, "Toggle recording radar mode changes. Default: true");
            Plugin.Logger?.LogDebug($"RecordRadarMode = {RecordRadarMode?.Value}");

            RecordLandingGear = config.Bind(OptionalDataSettings, "RecordLandingGear", DefaultRecordLandingGear, "Toggle recording landing gear changes. Default: true");
            Plugin.Logger?.LogDebug($"RecordLandingGear = {RecordLandingGear?.Value}");

            RecordPilotHead = config.Bind(OptionalDataSettings, "RecordPilotHead", DefaultRecordPilotHead, "Toggle recording pilot head movement. Default: true");
            Plugin.Logger?.LogDebug($"RecordPilotHead = {RecordPilotHead?.Value}");

            _CompressIDs = config.Bind(OptionalDataSettings, "CompressIDs", DefaultCompressIDs, "Compress IDs to reduce filesize with less determinism.");
            Plugin.Logger?.LogDebug($"CompressIDs = {_CompressIDs.Value}");

            MetersPerScan = config.Bind(HeightMapGeneratorSettings, "MetersPerScan", DefaultMetersPerScan, "Sample rate of the Heightmap generator. Does a scan per X meter. Default: 4");
            if (MetersPerScan.Value < 1)
            {
                Plugin.Logger?.LogWarning($"Invalid MetersPerScan! Setting default value {DefaultMetersPerScan}!");
                MetersPerScan.Value = DefaultMetersPerScan;
            }
            Plugin.Logger?.LogDebug($"MetersPerScan = {MetersPerScan.Value}");

            HeightMapResolution = config.Bind(HeightMapGeneratorSettings, "HeightMapResolution", DefaultHeightMapResolution, "Resolution of the Heightmap. Must be divisible by 4. Default: 4096");
            if ((HeightMapResolution.Value % 4) != 0)
            {
                Plugin.Logger?.LogWarning($"HeightMapResolution must be divisible by 4! Setting default value {DefaultHeightMapResolution}!");
                HeightMapResolution.Value = DefaultHeightMapResolution;
            }
            Plugin.Logger?.LogDebug($"HeightMapResolution = {HeightMapResolution.Value}");
            /*
            OrtoGraphicSize = config.Bind(HeightMapGeneratorSettings, "OrtographicSize", DefaultOrtoGraphicSize, "Size of Ortographic Camera for Texture Generation. Must be bigger than 0f.");
            if (OrtoGraphicSize.Value <= 0f)
            {
                Plugin.Logger?.LogWarning($"OrtographicSize must be divisible by 4! Setting default value {DefaultOrtoGraphicSize}!");
                OrtoGraphicSize.Value = DefaultOrtoGraphicSize;
            }
            Plugin.Logger?.LogDebug($"OrtographicSize = {OrtoGraphicSize.Value}");
            */
            _GenerateHeightMapKey = config.Bind("Hotkeys", "Generate Heightmap", new KeyboardShortcut(DefaultGenerateHeightMapKey));
            Plugin.Logger?.LogDebug($"Generate Heightmap key = {_GenerateHeightMapKey.Value}");

            EnableHeightmapGenerator = config.Bind(HeightMapGeneratorSettings, "EnableHeightmapGenerator", DefaultEnableHeightmapGenerator, "Enable/Disable Heightmap Generator. Default: false");
            Plugin.Logger?.LogDebug($"EnableHeightmapGenerator = {EnableHeightmapGenerator.Value}");

            //TacviewBetaHeightMapGenerator = config.Bind(HeightMapGeneratorSettings, "TacviewBetaHeightMapGenerator", DefaultTacviewBetaHeightMapGenerator, "True: Compatibility set for Tacview 1.9.5 Beta 11, False: Compatibility set for Tacview Stable. Default: True");
            //Plugin.Logger?.LogDebug($"TacviewBetaHeightMapGenerator = {TacviewBetaHeightMapGenerator.Value}");

            RecordEjectedPilots = config.Bind(GeneralSettings, "RecordEjectedPilots", DefaultRecordEjectedPilots, "Toggle Recording Ejected Pilots.");
            Plugin.Logger?.LogDebug($"RecordEjectedPilots = {RecordEjectedPilots.Value}");

            AutoStartRecording = config.Bind(GeneralSettings, "AutoStartRecording", DefaultAutoStartRecording, "Toggle Automatically starting to record on mission load.");
            Plugin.Logger?.LogDebug($"AutoStartRecording = {AutoStartRecording.Value}");

            EnableAutoSaveCountDown = config.Bind(VisualSettings, "EnableAutoSaveCountDown", DefaultEnableAutoSaveCountDown, "Toggle AutoSave Countdown Timer.");
            Plugin.Logger?.LogDebug($"EnableAutoSaveCountDown = {EnableAutoSaveCountDown.Value}");

            AutoSaveCountDownX = config.Bind(VisualSettings, "AutoSaveCountDownX", DefaultAutoSaveCountDownX, "X coordinate of Auto Save Countdown Timer on GUI. Scales with Resolution. Value range: 0.0 - 1.0");
            if (AutoSaveCountDownX.Value < 0f || AutoSaveCountDownX.Value > 1.0f)
            {
                Plugin.Logger?.LogWarning($"AutoSaveCountDownX must be within 0.0 - 1.0 range! Setting default value {DefaultAutoSaveCountDownX}!");
            }
            Plugin.Logger?.LogDebug($"AutoSaveCountDownX = {AutoSaveCountDownX.Value}");

            AutoSaveCountDownY = config.Bind(VisualSettings, "AutoSaveCountDownY", DefaultAutoSaveCountDownY, "Y coordinate of Auto Save Countdown Timer on GUI. Scales with Resolution. Value range: 0.0 - 1.0");
            if (AutoSaveCountDownY.Value < 0f || AutoSaveCountDownY.Value > 1.0f)
            {
                Plugin.Logger?.LogWarning($"AutoSaveCountDownX must be within 0.0 - 1.0 range! Setting default value {DefaultAutoSaveCountDownX}!");
            }
            Plugin.Logger?.LogDebug($"AutoSaveCountDownX = {AutoSaveCountDownY.Value}");

            TextColorR = config.Bind(VisualSettings, "TextColorR", DefaultTextColorR, "Red color value for GUI Text. Value range: 0.0 - 1.0");
            if (TextColorR.Value < 0f || TextColorR.Value > 1.0f)
            {
                Plugin.Logger?.LogWarning($"TextColorR must be within 0.0 - 1.0 range! Setting default value {DefaultTextColorR}!");
            }
            Plugin.Logger?.LogDebug($"TextColorR = {TextColorR.Value}");

            TextColorG = config.Bind(VisualSettings, "TextColorG", DefaultTextColorG, "Green color value for GUI Text. Value range: 0.0 - 1.0");
            if (TextColorG.Value < 0f || TextColorG.Value > 1.0f)
            {
                Plugin.Logger?.LogWarning($"TextColorG must be within 0.0 - 1.0 range! Setting default value {DefaultTextColorG}!");
            }
            Plugin.Logger?.LogDebug($"TextColorG = {TextColorG.Value}");

            TextColorB = config.Bind(VisualSettings, "TextColorB", DefaultTextColorB, "Blue color value for GUI Text. Value range: 0.0 - 1.0");
            if (TextColorB.Value < 0f || TextColorB.Value > 1.0f)
            {
                Plugin.Logger?.LogWarning($"TextColorB must be within 0.0 - 1.0 range! Setting default value {DefaultTextColorB}!");
            }
            Plugin.Logger?.LogDebug($"TextColorB = {TextColorB.Value}");

            TextColorA = config.Bind(VisualSettings, "TextColorA", DefaultTextColorA, "Transparency value for GUI Text. Value range: 0.0 - 1.0");
            if (TextColorA.Value < 0f || TextColorA.Value > 1.0f)
            {
                Plugin.Logger?.LogWarning($"TextColorA must be within 0.0 - 1.0 range! Setting default value {DefaultTextColorA}!");
            }
            Plugin.Logger?.LogDebug($"TextColorA = {TextColorA.Value}");

            EnableUnitLogging = config.Bind(DeveloperFeatures, "EnableUnknownUnitLogging", DefaultEnableUnitLogging, "Toggle logging Unknown Units that are unknown to ACMI Recorder. Default: false");
            Plugin.Logger?.LogDebug($"EnableUnknownUnitLogging = {EnableUnitLogging.Value}");

            EnableEncyclopediaExporter = config.Bind(DeveloperFeatures, "EnableEncyclopediaExporter", DefaultEnableEncyclopediaExporter, "Toggle Encyclopedia Exporter. Default: false");
            Plugin.Logger?.LogDebug($"EnableEncyclopediaExporter = {EnableEncyclopediaExporter.Value}");

            EncyclopediaExporterKey = config.Bind("Hotkeys", "EncyclopediaExporterKey", new KeyboardShortcut(DefaultEncyclopediaExporterKey));
            Plugin.Logger?.LogDebug($"EncyclopediaExporterKey = {EncyclopediaExporterKey.Value}");

            StartStopRecordingKey = config.Bind("Hotkeys", "StartStopRecordingKey", new KeyboardShortcut(DefaultStartStopRecordingKey));
            Plugin.Logger?.LogDebug($"StartStopRecordingKey = {StartStopRecordingKey.Value}");

        }
    }
}