using BepInEx.Logging;
using NuclearOption.SavedMission;
using NuclearOption.SceneLoading;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NOBlackBox
{
    internal class ACMIWriter
    {
        private MultiThreadedStreamWriter output;
        private readonly DateTime reference;
        public static TimeSpan lastUpdate;
        public static DateTime lastFlushTime;
        internal string filename;
        internal MapKey currentMapKey;
        internal ACMIWriter(DateTime reference)
        {
            string dir = Configuration.OutputPath;
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string basename = dir + DateTime.Now.ToString("s").Replace(":", "-");
            basename += "_" + MissionManager.CurrentMission.Name + "_" + MapSettingsManager.i.MapLoader.CurrentMap.Path;
            filename = basename + ".acmi";
            int postfix = 0;
            while (File.Exists(filename))
                filename = basename + $" ({++postfix}).acmi";

            output = new MultiThreadedStreamWriter(File.CreateText(filename));
            //sb = new StringBuilder();
            this.reference = reference;
            currentMapKey = MapSettingsManager.i.MapLoader.CurrentMap;

            Plugin.Logger?.LogInfo("[NOBlackBox]: MAP NAME IS " + currentMapKey.Path);
            output.WriteLine("FileType=text/acmi/tacview");
            output.WriteLine("FileVersion=2.2");

            Dictionary<string, string> initProps = new()
            {
                { "ReferenceTime", reference.ToString("s") + "Z" },
                { "DataSource", $"Nuclear Option {Application.version}" },
                { "DataRecorder", $"NOBlackBox 0.3.5" },
                { "Author", GameManager.LocalPlayer.PlayerName.Replace(",", "\\,") },
                { "RecordingTime", DateTime.Now.ToString("s") + "Z" },
				{ "MapId", $"NuclearOption.{currentMapKey.Path}"},
            };

            Mission mission = MissionManager.CurrentMission;
            initProps.Add("Title", mission.Name.Replace(",", "\\,"));
            
            /*
            if (mission.missionSettings.description != null)
            {
                string briefing = mission.missionSettings.description.Replace(",", "\\,");
                if (briefing != "")
                    initProps.Add("Briefing", briefing);
            }
            */
            output.WriteLine($"0,{StringifyProps(initProps)}");
            output.Flush();

            lastFlushTime = DateTime.Now;
        }

        ~ACMIWriter()
        {
            Close();
        }

        internal void UpdateObject(ACMIObject aObject, DateTime updateTime, Dictionary<string, string> props)
        {
            if (props.Count == 0)
                return;

            TimeSpan diff = updateTime - reference;
            if (diff != lastUpdate)
            {
                lastUpdate = diff;
                //output.WriteLine("#" + diff.TotalSeconds);
                WriteLine("#" + diff.TotalSeconds);
            }

            //output.WriteLine($"{aObject.id:X},{StringifyProps(props)}");
            WriteLine($"{aObject.id:X},{StringifyProps(props)}");
            //Plugin.Logger.LogInfo($"[NOBlackBox]: Time Elapsed = {diff.TotalSeconds}");
        }

        internal void RemoveObject(ACMIObject aObject, DateTime updateTime)
        {
            TimeSpan diff = updateTime - reference;
            if (diff != lastUpdate)
            {
                lastUpdate = diff;
                //output.WriteLine("#" + diff.TotalSeconds);
                WriteLine("#" + diff.TotalSeconds);
            }

            //output.WriteLine($"-{aObject.id:X}");
            WriteLine($"-{aObject.id:X}");
        }

        internal void WriteEvent(DateTime eventTime, string name, string[] items)
        {
            TimeSpan diff = eventTime - reference;
            if (diff != lastUpdate)
            {
                lastUpdate = diff;
                //output.WriteLine("#" + diff.TotalSeconds);
                WriteLine("#" + diff.TotalSeconds);
            }

            //output.WriteLine($"0,Event={name}|{string.Join("|", items)}");
            WriteLine($"0,Event={name}|{string.Join("|", items)}");
        }

        private string StringifyProps(Dictionary<string, string> props)
        {
            string[] propStrings = props.Select(x => x.Key + "=" + x.Value/*.Replace(",", "\\,")*/).ToArray();
            return string.Join(",", propStrings);
        }

        internal void WriteLine(string line)
        {
            if ((DateTime.Now - lastFlushTime).TotalSeconds > Configuration.AutoSaveInterval)
            {
                output.Flush();
                lastFlushTime = DateTime.Now;
                //output.Close();
                //output = new StreamWriter(filename, append: true);
            }
            output.WriteLine(line);
        }

        internal void Flush()
        {
            output.Flush();
        }

        internal void Close()
        {
            output.Close();
        }
    }
}
