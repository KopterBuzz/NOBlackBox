using NuclearOption.SavedMission;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace NOBlackBox
{
    internal class Recorder_mono : MonoBehaviour
    {
        private static readonly FieldInfo bulletSim = typeof(Gun).GetField("bulletSim", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo bullets = typeof(BulletSim).GetField("bullets", BindingFlags.NonPublic | BindingFlags.Instance);

        private readonly Dictionary<Shockwave, ACMIShockwave> waves = [];
        private readonly Dictionary<Shockwave, ACMIShockwave> newWaves = [];

        private DateTime startDate;
        private DateTime curTime;
        public static ACMIWriter writer;
        private float unitDiscoveryTimer = 0f;
        private readonly Dictionary<long, GameObject> objects = [];
        private readonly List<ACMIFlare> flares = [];
        private readonly List<ACMIFlare> newFlare = [];

        private readonly List<ACMITracer> newTracers = [];
        private readonly Dictionary<BulletSim.Bullet, ACMITracer> tracers = [];

        private Unit[] units = [];
        private BulletSim[] bulletSims = [];
        private bool processLateUpdate = true;

        public void invokeWriterUpdate(ACMIObject_mono obj)
        {
            writer.UpdateObject(obj, curTime);
        }

        public void invokeWriterRemove(ACMIObject_mono obj)
        {
            writer.RemoveObject(obj, curTime);
        }

        void Awake()
        {
            Plugin.Logger?.LogInfo("USING MONO RECORDER");
            if (Configuration.UseMissionTime?.Value == true)
            {
                startDate = DateTime.Today + TimeSpan.FromHours(MissionManager.CurrentMission.environment.timeOfDay);
                Plugin.Logger?.LogInfo("USING MISSION CLOCK");
            }
            else
            {
                startDate = DateTime.Now;
                Plugin.Logger?.LogInfo("USING SERVER CLOCK");
            }

            curTime = startDate;
            writer = new ACMIWriter(startDate);
            Plugin.Logger?.LogInfo("STARTED MONO RECORDER");

        }
        void Update()
        {
            curTime += TimeSpan.FromSeconds(Time.deltaTime);
            if (!units.Any())
            {
                return;
            }
            foreach (var unit in units)
            {
                if (!unit.networked || unit.persistentID == 0 || (unit.disabled && unit.GetType() != typeof(Missile)))
                {
                    continue;
                }
                bool isNew = false;
                if (!objects.TryGetValue(unit.persistentID, out GameObject acmi))
                {
                    acmi = new GameObject();
                    switch (unit)
                    {
                        case Aircraft aircraft:
                            acmi.AddComponent<ACMIAircraft_mono>();
                            acmi.GetComponent<ACMIAircraft_mono>().Init(aircraft);
                            acmi.GetComponent<ACMIAircraft_mono>().enabled = true;
                            break;
                        case Missile missile:
                            acmi.AddComponent<ACMIMissile_mono>();
                            acmi.GetComponent<ACMIMissile_mono>().Init(missile);
                            acmi.GetComponent<ACMIMissile_mono>().enabled = true;
                            break;
                        case GroundVehicle vehicle:
                            acmi.AddComponent<ACMIGroundVehicle_mono>();
                            acmi.GetComponent<ACMIGroundVehicle_mono>().Init(vehicle);
                            acmi.GetComponent<ACMIGroundVehicle_mono>().enabled = true;
                            break;
                        case Ship ship:
                            acmi.AddComponent<ACMIShip_mono>();
                            acmi.GetComponent<ACMIShip_mono>().Init(ship);
                            acmi.GetComponent<ACMIShip_mono>().enabled = true;
                            break;
                        case PilotDismounted pilot:
                            acmi.AddComponent<ACMIPilotDismounted_mono>();
                            acmi.GetComponent<ACMIPilotDismounted_mono>().Init(pilot);
                            acmi.GetComponent<ACMIPilotDismounted_mono>().enabled = true;
                            break;
                        default:
                            break;
                    }
                    //acmi = new GameObject();

                    Plugin.Logger?.LogInfo($"RECORDED UNIT,{unit.definition.name}," +
                        $"{unit.definition.unitName}," +
                        $"{unit.definition.code}");
                    objects.Add(unit.persistentID, acmi);
                    isNew = true;
                }
            }
        }
        void LateUpdate()
        {
            unitDiscoveryTimer += Time.deltaTime;
            if (unitDiscoveryTimer < Plugin.unitDiscoveryDelta)
            {
                return;
            }
            foreach (int key in objects.Keys)
            {
                if (null == objects[key])
                {
                    Plugin.Logger?.LogInfo($"REMOVING OBJECT {key.ToString(CultureInfo.InvariantCulture)}");
                    objects.Remove(key);
                }
            }
            Plugin.Logger?.LogInfo($"Frame TICK at {curTime.ToString(CultureInfo.InvariantCulture)}");
            unitDiscoveryTimer = 0f;
            units = UnityEngine.Object.FindObjectsByType<Unit>(FindObjectsSortMode.None);
            bulletSims = UnityEngine.Object.FindObjectsByType<BulletSim>(FindObjectsSortMode.None);
            Plugin.Logger?.LogInfo($"DISCOVERED {units.Length.ToString(CultureInfo.InvariantCulture)} UNITS!");
        }
        void OnDisable()
        {
            writer?.Close();
            Plugin.Logger?.LogInfo("DISABLED MONO RECORDER");
        }
        void OnDestroy()
        {
            writer?.Close();
            Plugin.Logger?.LogInfo("DESTROYED MONO RECORDER");
        }
    }
}
