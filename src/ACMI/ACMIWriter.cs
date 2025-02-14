using NuclearOption.SavedMission;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace NOBlackBox
{
    internal class ACMIWriter
    {
        private StreamWriter output;
        private readonly DateTime reference;
        private TimeSpan lastUpdate;
        internal string filename;
        internal ACMIWriter(DateTime reference)
        {
            string dir = Configuration.OutputPath.Value;
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string basename = dir + DateTime.Now.ToString("s").Replace(":", "-");
            basename += " " + MissionManager.CurrentMission.Name;
            filename = basename + ".acmi";
            int postfix = 0;
            while (File.Exists(filename))
                filename = basename + $" ({++postfix}).acmi";

            output = File.CreateText(filename);
            //sb = new StringBuilder();
            this.reference = reference;

            output.WriteLine("FileType=text/acmi/tacview");
            output.WriteLine("FileVersion=2.2");

            Dictionary<string, string> initProps = new()
            {
                { "ReferenceTime", reference.ToString("s") + "Z" },
                { "DataSource", $"Nuclear Option {Application.version}" },
                { "DataRecorder", $"NOBlackBox 0.2.2" },
                //{ "Author", GameManager.LocalPlayer.PlayerName.Replace(",", "\\,") },
                { "RecordingTime", DateTime.Today.ToString("s") + "Z" },
            };

            Mission mission = MissionManager.CurrentMission;
            initProps.Add("Title", mission.Name.Replace(",", "\\,"));

            //string briefing = mission.missionSettings.description.Replace(",", "\\,");
            //if (briefing != "")
            //    initProps.Add("Briefing", briefing);

            output.WriteLine($"0,{StringifyProps(initProps)}");
            output.Flush();
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
            string[] propStrings = props.Select(x => x.Key + "=" + x.Value.Replace(",", "\\,")).ToArray();
            return string.Join(",", propStrings);
        }

        internal void WriteLine(string line)
        {
            if (lastUpdate.TotalSeconds > Configuration.AutoSaveInterval.Value && (lastUpdate.TotalSeconds % Configuration.AutoSaveInterval.Value < 1))
            {
                output.Flush();
                output.Close();
                output = new StreamWriter(filename, append: true);
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
