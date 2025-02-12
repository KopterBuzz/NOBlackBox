using BepInEx.Configuration;
using System.Linq;
using UnityEngine;
using System.IO;
using System;
using NOBlackBox.src.Helpers;

namespace NOBlackBox
{
    public static class Configuration
    {
        internal const string GeneralSettings = "General Settings";

        internal const int DefaultUpdateRate = 5;

        internal static ConfigEntry<int>? UpdateRate;
        internal static ConfigEntry<string>? OutputPath;

        internal static void InitSettings(ConfigFile config)
        {
            Plugin.Logger?.LogInfo("[NOBlackBox]: Loading Settings.");

            UpdateRate = config.Bind(GeneralSettings, "UpdateRate", DefaultUpdateRate, "The number of times per second NOBlackBox will record events. 0 = unlimited. Max Value: 1000");
            Plugin.Logger?.LogInfo($"[NOBlackBox]: UpdateRate = {UpdateRate.Value}");
            if (!Enumerable.Range(0,1000).Contains(UpdateRate.Value))
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
        }
    }
}
