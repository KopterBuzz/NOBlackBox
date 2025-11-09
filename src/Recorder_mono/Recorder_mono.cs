using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using NOBlackBox.src.ACMI_mono;
using NuclearOption.Networking;
using UnityEngine;

namespace NOBlackBox
{
    internal class Recorder_mono : MonoBehaviour
    {
        private static readonly FieldInfo bulletSim = typeof(Gun).GetField("bulletSim", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo bullets = typeof(BulletSim).GetField("bullets", BindingFlags.NonPublic | BindingFlags.Instance);

        private Dictionary<Shockwave, GameObject> waves = [];
        Shockwave[] shockwaves = [];

        private DateTime startDate;
        private DateTime curTime;
        public static ACMIWriter writer;
        private float unitDiscoveryTimer = 0f;
        private float bulletSimDiscoveryTimer = 0f;
        private float shockwaveDiscoveryTimer = 0f;

        internal Dictionary<long, GameObject> unitObjects = [];


        private Dictionary<BulletSim.Bullet, GameObject> tracers = [];

        private Unit[] units = [];
        private BulletSim[] bulletSims = [];
        private bool processUnits = false;
        private bool processBulletSims = false;
        private bool processShockWaves = false;
        
        public void invokeWriterUpdate(ACMIObject_mono obj)
        {
            writer.UpdateObject(obj, curTime);
        }

        public void invokeWriterRemove(ACMIObject_mono obj)
        {
            writer.RemoveObject(obj, curTime);
        }

        public void invokeWriterDestroy(ACMIObject_mono obj)
        {
            writer.WriteDestroyedEvent(obj, curTime);
        }

        public void invokeWriterRepair(ACMIObject_mono obj)
        {
            writer.WriteRepairedEvent(obj, curTime);
        }

        void Awake()
        {
            Plugin.Logger?.LogDebug("USING MONO RECORDER");
            if (Configuration.UseMissionTime?.Value == true)
            {
                startDate = DateTime.Today + TimeSpan.FromHours(MissionManager.CurrentMission.environment.timeOfDay);
                Plugin.Logger?.LogDebug("USING MISSION CLOCK");
            }
            else
            {
                startDate = DateTime.Now;
                Plugin.Logger?.LogDebug("USING SERVER CLOCK");
            }

            curTime = startDate;
            writer = new ACMIWriter(startDate);
            Plugin.Logger?.LogDebug("STARTED MONO RECORDER");
            Plugin.recordingManually = false;

        }
        void Update()
        {
            curTime += TimeSpan.FromSeconds(Time.deltaTime);
            if (!units.Any())
            {
                return;
            }
            if (processUnits)
            {
                foreach (var unit in units)
                {
                    if (!unit.networked || (unit.disabled && unit.GetType() != typeof(Missile)))
                    {
                        continue;
                    }
                    bool isNew = false;
                    if (!unitObjects.TryGetValue(unit.persistentID.Id, out GameObject acmi))
                    {
                        
                        switch (unit)
                        {
                            case Aircraft aircraft:

                                aircraft.onAddIRSource += (IRSource source) =>
                                {
                                    if (source.flare)
                                    {
                                        GameObject flare = new GameObject();
                                        flare.AddComponent<ACMIFlare_mono>();
                                        flare.GetComponent<ACMIFlare_mono>().Init(source);
                                        flare.GetComponent<ACMIFlare_mono>().enabled = true;
                                    }
                                };
                                acmi = new GameObject();
                                acmi.AddComponent<ACMIAircraft_mono>();
                                acmi.GetComponent<ACMIAircraft_mono>().Init(aircraft);
                                acmi.GetComponent<ACMIAircraft_mono>().enabled = true;
                                Plugin.Logger?.LogDebug($"RECORDED UNIT,{unit.definition.name}," +
                                    $"{unit.definition.unitName}," +
                                    $"{unit.definition.code}");
                                unitObjects.Add(unit.persistentID.Id, acmi);
                                isNew = true;
                                break;
                            case Missile missile:
                                acmi = new GameObject();
                                acmi.AddComponent<ACMIMissile_mono>();
                                acmi.GetComponent<ACMIMissile_mono>().Init(missile);
                                acmi.GetComponent<ACMIMissile_mono>().enabled = true;
                                Plugin.Logger?.LogDebug($"RECORDED UNIT,{unit.definition.name}," +
                                    $"{unit.definition.unitName}," +
                                    $"{unit.definition.code}");
                                unitObjects.Add(unit.persistentID.Id, acmi);
                                isNew = true;
                                break;
                            case GroundVehicle vehicle:
                                acmi = new GameObject();
                                acmi.AddComponent<ACMIGroundVehicle_mono>();
                                acmi.GetComponent<ACMIGroundVehicle_mono>().Init(vehicle);
                                acmi.GetComponent<ACMIGroundVehicle_mono>().enabled = true;
                                Plugin.Logger?.LogDebug($"RECORDED UNIT,{unit.definition.name}," +
                                    $"{unit.definition.unitName}," +
                                    $"{unit.definition.code}");
                                unitObjects.Add(unit.persistentID.Id, acmi);
                                isNew = true;
                                break;
                            case Ship ship:
                                acmi = new GameObject();
                                acmi.AddComponent<ACMIShip_mono>();
                                acmi.GetComponent<ACMIShip_mono>().Init(ship);
                                acmi.GetComponent<ACMIShip_mono>().enabled = true;
                                Plugin.Logger?.LogDebug($"RECORDED UNIT,{unit.definition.name}," +
                                    $"{unit.definition.unitName}," +
                                    $"{unit.definition.code}");
                                unitObjects.Add(unit.persistentID.Id, acmi);
                                isNew = true;
                                break;
                            case PilotDismounted pilot:
                                if (Configuration.RecordEjectedPilots.Value == true)
                                {
                                    acmi = new GameObject();
                                    acmi.AddComponent<ACMIPilotDismounted_mono>();
                                    acmi.GetComponent<ACMIPilotDismounted_mono>().Init(pilot);
                                    acmi.GetComponent<ACMIPilotDismounted_mono>().enabled = true;
                                    Plugin.Logger?.LogDebug($"RECORDED UNIT,{unit.definition.name}," +
                                        $"{unit.definition.unitName}," +
                                        $"{unit.definition.code}");
                                    unitObjects.Add(unit.persistentID.Id, acmi);
                                    isNew = true;
                                }
                                break;
                            case Building building:
                                acmi = new GameObject();
                                acmi.AddComponent<ACMIBuilding_mono>();
                                acmi.GetComponent<ACMIBuilding_mono>().Init(building);
                                acmi.GetComponent<ACMIBuilding_mono>().enabled = true;
                                Plugin.Logger?.LogDebug($"RECORDED UNIT,{unit.definition.name}," +
                                    $"{unit.definition.unitName}," +
                                    $"{unit.definition.code}");
                                unitObjects.Add(unit.persistentID.Id, acmi);
                                isNew = true;
                                break;
                            case Scenery scenery:
                                acmi = new GameObject();
                                acmi.AddComponent<ACMIScenery_mono>();
                                acmi.GetComponent<ACMIScenery_mono>().Init(scenery);
                                acmi.GetComponent<ACMIScenery_mono>().enabled = true;
                                Plugin.Logger?.LogDebug($"RECORDED UNIT,{unit.definition.name}," +
                                    $"{unit.definition.unitName}," +
                                    $"{unit.definition.code}");
                                unitObjects.Add(unit.persistentID.Id, acmi);
                                isNew = true;
                                break;
                            default:
                                break;
                        }
                        //acmi = new GameObject();
                    }
                }
                processUnits = false;
            }
            if (processBulletSims)
            {
                foreach (var bulletSim in bulletSims)
                {
                    List<BulletSim.Bullet> bullets = (List<BulletSim.Bullet>)Recorder_mono.bullets.GetValue(bulletSim);

                    foreach (var bullet in bullets)
                    {

                        if (!tracers.ContainsKey(bullet))
                        {
                            GameObject tracer = new GameObject();
                            tracer.AddComponent<ACMITracer_mono>();
                            tracer.GetComponent<ACMITracer_mono>().Init(bulletSim, bullet);
                            tracer.GetComponent<ACMITracer_mono>().enabled = true;
                            tracers.Add(tracer.GetComponent<ACMITracer_mono>().bullet, tracer);
                        }
                    }
                }
                processBulletSims = false;
            }

            if(processShockWaves)
            {
                foreach (Shockwave wave in shockwaves)
                {
                    if (waves.ContainsKey(wave))
                    {
                        continue;
                    }
                    GameObject shockwave = new GameObject();
                    shockwave.AddComponent<ACMIShockwave_mono>();
                    shockwave.GetComponent<ACMIShockwave_mono>().Init(wave);
                    shockwave.GetComponent<ACMIShockwave_mono>().enabled = true;
                    waves.Add(wave, shockwave);
                }
            }
        }
        void LateUpdate()
        {
            unitDiscoveryTimer += Time.deltaTime;
            bulletSimDiscoveryTimer += Time.deltaTime;
            shockwaveDiscoveryTimer += Time.deltaTime;
            if (shockwaveDiscoveryTimer >= Configuration.shockwaveDiscoveryDelta.Value)
            {
                processShockWaves = ShockWaveDiscovery();
            }
            if (unitDiscoveryTimer >= Configuration.unitDiscoveryDelta.Value)
            {
                processUnits = UnitDiscovery();
            }
            if (bulletSimDiscoveryTimer >= Configuration.bulletSimDiscoveryDelta.Value)
            {
                processBulletSims = BulletSimDiscovery();
            }

        }
        void OnDisable()
        {
            writer?.Close();
            Plugin.Logger?.LogDebug("DISABLED MONO RECORDER");
        }
        void OnDestroy()
        {
            unitObjects = [];
            waves = [];
            tracers = [];
            writer?.Close();
            Plugin.Logger?.LogDebug("DESTROYED MONO RECORDER");
        }

        private bool UnitDiscovery()
        {
            foreach (int key in unitObjects.Keys)
            {
                if (null == unitObjects[key])
                {
                    // Plugin.Logger?.LogDebug($"REMOVING OBJECT {key.ToString(CultureInfo.InvariantCulture)}");
                    unitObjects.Remove(key);
                }
            }
            //Plugin.Logger?.LogDebug($"Frame TICK at {curTime.ToString(CultureInfo.InvariantCulture)}");
            units = UnityEngine.Object.FindObjectsByType<Unit>(FindObjectsSortMode.None);
            //Plugin.Logger?.LogDebug($"DISCOVERED {units.Length.ToString(CultureInfo.InvariantCulture)} UNITS!");
            unitDiscoveryTimer = 0f;
            return true;

        }

        internal bool BulletSimDiscovery()
        {
            try
            {
                foreach (BulletSim.Bullet key in tracers.Keys)
                {
                    if (null == tracers[key])
                    {
                        //Plugin.Logger?.LogDebug($"REMOVING TRACER");
                        try
                        {
                            tracers.Remove(key);
                        }
                        catch
                        {
                            //wtf
                        }

                    }
                }
            }
            catch
            {
                //wtf2
            }

            bulletSims = UnityEngine.Object.FindObjectsByType<BulletSim>(FindObjectsSortMode.None);
            //Plugin.Logger?.LogDebug($"DISCOVERED {bulletSims.Length.ToString(CultureInfo.InvariantCulture)} BULLETSIMS!");
            bulletSimDiscoveryTimer = 0f;
            return true;
        }

        public bool ShockWaveDiscovery()
        {
            foreach (Shockwave key in waves.Keys)
            {
                if (null == waves[key])
                {
                    //Plugin.Logger?.LogDebug($"REMOVING TRACER");
                    waves.Remove(key);
                }
            }
            shockwaves = UnityEngine.Object.FindObjectsByType<Shockwave>(FindObjectsSortMode.None);
            //Plugin.Logger?.LogDebug($"DISCOVERED {shockwaves.Length.ToString(CultureInfo.InvariantCulture)} SHOCKWAVES!");
            shockwaveDiscoveryTimer = 0f;
            return true;
        }
    }
}
