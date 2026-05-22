using BepInEx.Logging;
using NuclearOption.SavedMission;
using NuclearOption.SceneLoading;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
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

            string basename = Path.Combine(dir, DateTime.Now.ToString("s").Replace(":", "-"));
            basename += "_" + MissionManager.CurrentMission.Name + "_" + MapSettingsManager.i.MapLoader.CurrentMap.Path;
            filename = basename + ".acmi";
            int postfix = 0;
            while (File.Exists(filename))
                filename = basename + $" ({++postfix}).acmi";

            output = new MultiThreadedStreamWriter(File.CreateText(filename));
            this.reference = reference;
            currentMapKey = MapSettingsManager.i.MapLoader.CurrentMap;

            Plugin.Logger?.LogDebug("MAP NAME IS " + currentMapKey.Path);

            Plugin.rttServer?.BeginSession();

            string fileTypeLine = "FileType=text/acmi/tacview";
            output.WriteLine(fileTypeLine);
            Plugin.rttServer?.PublishLine(fileTypeLine, replay: true);

            string fileVerLine = "FileVersion=2.2";
            output.WriteLine(fileVerLine);
            Plugin.rttServer?.PublishLine(fileVerLine, replay: true);

            Dictionary<string, string> initProps = new()
            {
                { "ReferenceTime", reference.ToString("s") + "Z" },
                { "DataSource", $"Nuclear Option {Application.version}" },
                { "DataRecorder", $"NOBlackBox 0.3.8.2" },
                { "Author", Plugin.localPlayer?.name.Replace(",", "\\,") ?? "Server" },
                { "RecordingTime", DateTime.Now.ToString("s") + "Z" },
				{ "MapId", $"NuclearOption.{currentMapKey.Path}"},
            };

            Mission mission = MissionManager.CurrentMission;
            initProps.Add("Title", mission.Name.Replace(",", "\\,"));

            string metadataLine = $"0,{StringifyProps(initProps)}";
            output.WriteLine(metadataLine);
            Plugin.rttServer?.PublishLine(metadataLine, replay: true);
            output.Flush();

            lastFlushTime = DateTime.Now;
        }

        ~ACMIWriter()
        {
            Close();
        }

        public void UpdateObject(ACMIObject_mono aObject, DateTime updateTime)
        {
            if (aObject.props.Count == 0)
                return;

            TimeSpan diff = updateTime - reference;
            if (diff != lastUpdate)
            {
                lastUpdate = diff;
                //output.WriteLine("#" + diff.TotalSeconds);
                WriteLine("#" + diff.TotalSeconds);
            }

            //output.WriteLine($"{aObject.id:X},{StringifyProps(props)}");
            WriteLine($"{aObject.tacviewId:X},{StringifyProps(aObject.props)}");
            //Plugin.Logger.LogDebug($"Time Elapsed = {diff.TotalSeconds}");
        }

        internal void RemoveObject(ACMIObject_mono aObject, DateTime updateTime)
        {

            TimeSpan diff = updateTime - reference;
            if (diff != lastUpdate)
            {
                lastUpdate = diff;
                //output.WriteLine("#" + diff.TotalSeconds);
                WriteLine("#" + diff.TotalSeconds);
            }

            WriteLine($"-{aObject.tacviewId:X}");
        }

        internal void WriteDestroyedEvent(ACMIObject_mono aObject, DateTime updateTime)
        {
            TimeSpan diff = updateTime - reference;
            if (diff != lastUpdate)
            {
                lastUpdate = diff;
                //output.WriteLine("#" + diff.TotalSeconds);
                WriteLine("#" + diff.TotalSeconds);
            }

            if (Configuration.DestructionEvents.Value == true)
            {
                WriteLine("#" + diff.TotalSeconds);
                if (aObject.destroyedEvent)
                {
                    WriteLine($"0,Event=Message|{aObject.tacviewId:X}|Has been destroyed.");
                }
            }
        }

        internal void WriteRepairedEvent(ACMIObject_mono aObject, DateTime updateTime)
        {
            TimeSpan diff = updateTime - reference;
            if (diff != lastUpdate)
            {
                lastUpdate = diff;
                //output.WriteLine("#" + diff.TotalSeconds);
                WriteLine("#" + diff.TotalSeconds);
            }

            if (Configuration.DestructionEvents.Value == true)
            {
                WriteLine("#" + diff.TotalSeconds);
                if (aObject.destroyedEvent)
                {
                    WriteLine($"0,Event=Message|{aObject.tacviewId:X}|Has been repaired.");
                }
            }
        }

        private string StringifyProps(Dictionary<string, string> props)
        {
            string[] propStrings = props.Select(x => x.Key + "=" + x.Value/*.Replace(",", "\\,")*/).ToArray();
            return string.Join(",", propStrings);
        }

        internal void WriteLine(string line)
        {
            if (line.StartsWith("#"))
            {
                Plugin.rttServer?.PublishFlushMarker();
            }

            if ((DateTime.Now - lastFlushTime).TotalSeconds > Configuration.AutoSaveInterval)
            {
                output.Flush();
                lastFlushTime = DateTime.Now;
            }
            output.WriteLine(line);

            Plugin.rttServer?.PublishLine(line, replay: false);
        }

        internal void Flush()
        {
            output.Flush();
        }

        private int _closed;

        internal async void Close()
        {
            if (Interlocked.Exchange(ref _closed, 1) == 1)
                return;

            Plugin.rttServer?.EndSession(disconnectClients: true);

            output.Close();
            string zipPath = await ZipHelper.ZipFileAsync(filename);
            Plugin.Logger?.LogDebug($"saving compressed acmi {zipPath}");
            await ZipHelper.DeleteFileAsync(filename);
            Plugin.Logger?.LogDebug($"deleted uncompressed acmi {filename}");
        }
    }
}
