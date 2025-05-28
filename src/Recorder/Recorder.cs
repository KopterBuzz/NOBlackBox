using NuclearOption.SavedMission;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace NOBlackBox
{
    internal class Recorder
    {
        private static readonly FieldInfo bulletSim = typeof(Gun).GetField("bulletSim", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo bullets = typeof(BulletSim).GetField("bullets", BindingFlags.NonPublic | BindingFlags.Instance);

        private readonly DateTime startDate;
        private DateTime curTime;
        private ACMIWriter writer;
        private readonly Dictionary<long, ACMIUnit> objects = [];
        private readonly List<ACMIFlare> flares = [];
        private readonly List<ACMIFlare> newFlare = [];
        
        private readonly List<ACMITracer> newTracers = [];
        private readonly Dictionary<BulletSim.Bullet, ACMITracer> tracers = [];

        internal Recorder(Mission mission)
        {
            if (Configuration.UseMissionTime?.Value == true)
            {
                startDate = DateTime.Today + TimeSpan.FromHours(mission.environment.timeOfDay);
                Plugin.Logger?.LogInfo("[NOBlackBox]: USING MISSION CLOCK");
            }
            else
            {
                startDate = DateTime.Now;
                Plugin.Logger?.LogInfo("[NOBlackBox]: USING SERVER CLOCK");
            }

            curTime = startDate;

            writer = new ACMIWriter(startDate);
            Plugin.Logger?.LogInfo("[NOBlackBox]: RECORDING STARTED");

        }

        ~Recorder()
        {
            Plugin.Logger?.LogInfo("[NOBlackBox]: RECORDING ENDED");
            Close();
        }

        internal void Update(float delta)
        {
            curTime += TimeSpan.FromSeconds(delta);

            foreach (var acmi in objects.Values.ToList())
                if (acmi.unit.disabled)
                {
                    objects.Remove(acmi.id);
                    writer.RemoveObject(acmi, curTime);
                }

            foreach (var acmi in flares.ToList())
                if (acmi.flare == null || !acmi.flare.enabled) // Apparently we can lose references? wtf?
                {
                    flares.Remove(acmi);
                    writer.RemoveObject(acmi, curTime);
                }

            foreach (var acmi in tracers.Values.ToList())
                if (acmi.bullet.tracer == null || !acmi.bullet.tracer.activeSelf) // Apparently we can lose references? wtf?
                {
                    tracers.Remove(acmi.bullet);
                    writer.RemoveObject(acmi, curTime);
                }

            Unit[] units = UnityEngine.Object.FindObjectsByType<Unit>(FindObjectsSortMode.None);

            foreach (var unit in units)
            {
                if (!unit.networked || unit.disabled || unit.persistentID == 0)
                    continue;

                bool isNew = false;
                if (!objects.TryGetValue(unit.persistentID, out ACMIUnit acmi))
                {
                    switch (unit)
                    {
                        case Aircraft aircraft:
                            acmi = new ACMIAircraft(aircraft);

                            aircraft.onAddIRSource += (IRSource source) =>
                            {
                                if (source.flare)
                                    newFlare.Add(new(source));
                            };
                            if (Configuration.EnableUnitLogging.Value == true)
                            {
                                Plugin.Logger?.LogInfo($"NOBLACKBOX_RECORDED_AIRCRAFT,{unit.definition.name}," +
                                                        $"{unit.definition.unitName}," +
                                                        $"{unit.definition.code}");
                            }
                            break;
                        case Missile:
                            acmi = new ACMIMissile((Missile)unit);
                            if (Configuration.EnableUnitLogging.Value == true)
                            {
                                Plugin.Logger?.LogInfo($"NOBLACKBOX_RECORDED_WEAPON,{unit.definition.name}," +
                                                        $"{unit.definition.unitName}," +
                                                        $"{unit.definition.code}");
                            }
                            break;
                        case GroundVehicle:
                            acmi = new ACMIGroundVehicle((GroundVehicle)unit);
                            if (Configuration.EnableUnitLogging.Value == true)
                            {
                                Plugin.Logger?.LogInfo($"NOBLACKBOX_RECORDED_GROUND,{unit.definition.name}," +
                                                        $"{unit.definition.unitName}," +
                                                        $"{unit.definition.code}");
                            }
                            break;
                        case Building:
                            acmi = new ACMIBuilding((Building)unit);
                            if (Configuration.EnableUnitLogging.Value == true)
                            {
                                Plugin.Logger?.LogInfo($"NOBLACKBOX_RECORDED_BUILDING,{unit.definition.name}," +
                                                        $"{unit.definition.unitName}," +
                                                        $"{unit.definition.code}");
                            }
                            break;
                        case Ship:
                            acmi = new ACMIShip((Ship)unit);
                            if (Configuration.EnableUnitLogging.Value == true)
                            {
                                Plugin.Logger?.LogInfo($"NOBLACKBOX_RECORDED_SHIP,{unit.definition.name}," +
                                                        $"{unit.definition.unitName}," +
                                                        $"{unit.definition.code}");
                            }
                            break;
                        default:
                            /*
                            if(Configuration.EnableUnitLogging.Value == true)
                            {
                                Plugin.Logger?.LogInfo( $"NOBLACKBOX_UNKNOWN: {unit.definition.name}," +
                                                        $"{unit.definition.unitName}," +
                                                        $"{unit.definition.code},");
                            }
                            */
                            continue;
                    }

                    objects.Add(unit.persistentID, acmi);
                    isNew = true;

                    acmi.OnEvent += WriteEvent;
                }

                Dictionary<string, string> props = acmi.Update();
                if (isNew)
                    props = props.Concat(acmi.Init()).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                if (unit.IsLocalPlayer)
                    Plugin.Logger?.LogInfo(props["T"]);

                writer.UpdateObject(acmi, curTime, props);
            }

            foreach (ACMIFlare flare in newFlare)
            {
                Dictionary<string, string> props = flare.Update();
                props = props.Concat(flare.Init()).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                writer.UpdateObject(flare, curTime, props);
            }

            foreach (ACMIFlare flare in flares)
                writer.UpdateObject(flare, curTime, flare.Update());

            flares.AddRange(newFlare);
            newFlare.Clear();

            var bulletSims = UnityEngine.Object.FindObjectsByType<BulletSim>(FindObjectsSortMode.None);
            
            foreach (var bulletSim in bulletSims)
            {
                List<BulletSim.Bullet> bullets = (List<BulletSim.Bullet>)Recorder.bullets.GetValue(bulletSim);

                foreach (var bullet in bullets)
                {
                    if (!tracers.ContainsKey(bullet))
                    {
                        newTracers.Add(new ACMITracer(bulletSim, bullet));
                    }
                }
            }
            
            foreach (ACMITracer tracer in tracers.Values)
                writer.UpdateObject(tracer, curTime, tracer.Update());

            foreach (ACMITracer tracer in newTracers)
            {
                Dictionary<string, string> props = tracer.Update();
                props = props.Concat(tracer.Init()).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                writer.UpdateObject(tracer, curTime, props);
                tracers.Add(tracer.bullet, tracer);
            }

            newTracers.Clear();

            writer.Flush();
        }

        private void WriteEvent(string name, long[] ids, string text)
        {
            writer.WriteEvent(curTime, name, [.. ids.Select(a => a.ToString("X")), text]);
            writer.Flush();
        }

        internal void Close()
        {
            writer?.Close();
        }
    }
}
