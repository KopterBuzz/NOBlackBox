using Mirage.Serialization;
using NuclearOption.SavedMission;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NOBlackBox
{
    internal class ACMIWriter
    {
        private readonly StreamWriter output;
        //private readonly StringBuilder sb;
        private readonly DateTime reference;
        private TimeSpan lastUpdate;
        internal ACMIWriter(DateTime reference)
        {
            string dir = Configuration.OutputPath.Value;
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string basename = dir + DateTime.Now.ToString("s").Replace(":", "-");
            basename += " " + MissionManager.CurrentMission.Name;
            string filename = basename + ".acmi";
            int postfix = 0;
            while (File.Exists(filename))
                filename = basename + $" ({++postfix}).acmi";

            output = File.CreateText(filename);
            //sb = new StringBuilder();
            this.reference = reference;

            output.WriteLine("FileType=text/acmi/tacview");
            output.WriteLine("FileVersion=2.2");
            //sb.Append("FileType=text/acmi/tacview");
            //sb.Append("FileVersion=2.2\n");

            Dictionary<string, string> initProps = new()
            {
                { "ReferenceTime", reference.ToString("s") + "Z" },
                { "DataSource", $"Nuclear Option {Application.version}" },
                { "DataRecorder", $"NOBlackBox" },
                { "Author", GameManager.LocalPlayer.PlayerName.Replace(",", "\\,") },
                { "RecordingTime", DateTime.Today.ToString("s") + "Z" },
            };

            Mission mission = MissionManager.CurrentMission;
            initProps.Add("Title", mission.Name.Replace(",", "\\,"));

            string briefing = mission.missionSettings.description.Replace(",", "\\,");
            if (briefing != "")
                initProps.Add("Briefing", briefing);

            output.WriteLine($"0,{StringifyProps(initProps)}");
            output.Flush();
            //sb.Append($"0,{StringifyProps(initProps)}\n");
        }

        ~ACMIWriter()
        {
            output.Close();
            //sb.Clear();
        }

        internal void UpdateObject(ACMIObject aObject, DateTime updateTime, Dictionary<string, string> props)
        {
            if (props.Count == 0)
                return;

            TimeSpan diff = updateTime - reference;
            if (diff != lastUpdate)
            {
                lastUpdate = diff;
                output.WriteLine("#" + diff.TotalSeconds);
            }
            
            output.WriteLine($"{aObject.id:X},{StringifyProps(props)}");
        }

        internal void RemoveObject(ACMIObject aObject, DateTime updateTime)
        {
            TimeSpan diff = updateTime - reference;
            if (diff != lastUpdate)
            {
                lastUpdate = diff;
                output.WriteLine("#" + diff.TotalSeconds);
            }

            output.WriteLine($"-{aObject.id:X}");
        }

        internal void WriteEvent(DateTime eventTime, string name, string[] items)
        {
            TimeSpan diff = eventTime - reference;
            if (diff != lastUpdate)
            {
                lastUpdate = diff;
                output.WriteLine("#" + diff.TotalSeconds);
            }

            output.WriteLine($"0,Event={name}|{string.Join("|", items)}");
        }

        private string StringifyProps(Dictionary<string, string> props)
        {
            string[] propStrings = props.Select(x => x.Key + "=" + x.Value.Replace(",", "\\,")).ToArray();
            return string.Join(",", propStrings);
        }

        internal void Flush()
        {
            output.Flush();
        }

        internal void CloseStreamWriter()
        {
            output.Flush();
            output.Close();
        }
    }
}
