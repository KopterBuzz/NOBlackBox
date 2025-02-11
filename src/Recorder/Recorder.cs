using NuclearOption.SavedMission;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NOBlackBox
{
    internal class Recorder
    {
        private readonly DateTime startDate;
        private DateTime curTime;
        private ACMIWriter writer;
        private readonly Dictionary<long, ACMIUnit> objects = [];
        private readonly List<ACMIFlare> flares = [];
        private readonly List<ACMIFlare> newFlare = [];

        private List<GameObject> tracersClones = [];
        private readonly List<ACMITracer> newTracers = [];
        private readonly List<ACMITracer> tracers = [];

        internal Recorder(Mission mission)
        {
            startDate = DateTime.Today + TimeSpan.FromHours(mission.environment.timeOfDay);
            curTime = startDate;

            writer = new ACMIWriter(startDate);
        }

        ~Recorder()
        {
            writer?.CloseStreamWriter();
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

            Unit[] units = UnityEngine.Object.FindObjectsByType<Unit>(FindObjectsSortMode.None);

            foreach (var unit in units)
            {
                if (!unit.networked || unit.disabled)
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

                            break;
                        case Missile:
                            acmi = new ACMIMissile((Missile)unit);
                            break;
                        case GroundVehicle:
                            acmi = new ACMIGroundVehicle((GroundVehicle)unit);
                            break;
                        case Building:
                            acmi = new ACMIBuilding((Building)unit);
                            break;
                        case Ship:
                            acmi = new ACMIShip((Ship)unit);
                            break;
                        default:
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
                    Plugin.Logger.LogInfo(props["T"]);

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

            tracersClones = Resources.FindObjectsOfTypeAll<GameObject>().Where(obj => obj.name == "tracer(Clone)").ToList();
            tracersClones.ForEach(obj => { newTracers.Add(new ACMITracer(new Tracer(obj))); });

            foreach (ACMITracer tracer in newTracers)
            {
                Dictionary<string, string> props = tracer.Update();
                props = props.Concat(tracer.Init()).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                writer.UpdateObject(tracer, curTime, props);
            }
            foreach (ACMITracer tracer in tracers)
                writer.UpdateObject(tracer, curTime, tracer.Update());
            tracers.AddRange(newTracers);
            newTracers.Clear();

            writer.Flush();
        }

        private void WriteEvent(string name, long[] ids, string text)
        {
            writer.WriteEvent(curTime, name, [.. ids.Select(a => a.ToString("X")), text]);
            writer.Flush();
        }

        internal void CloseStreamWriter()
        {
            writer?.CloseStreamWriter();
        }
    }
}
