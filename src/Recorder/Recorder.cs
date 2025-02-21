using MonoMod.Utils;
using NuclearOption.SavedMission;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace NOBlackBox
{
    // TODO: Review order of operations. It may be possible a single frame of data is lost due to order of scheduling.
    internal class Recorder
    {
        private static readonly FieldInfo bSim = typeof(Gun).GetField("bulletSim", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo bullets = typeof(BulletSim).GetField("bullets", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo effects = typeof(Missile).GetField("detonationEffects", BindingFlags.NonPublic | BindingFlags.Instance);

        private readonly DateTime startDate;
        private DateTime curTime;
        private readonly ACMIWriter writer;

        private readonly Dictionary<long, ACMIUnit> objects = [];

        private readonly List<ACMIFlare> flares = [];
        private readonly List<ACMIFlare> newFlare = [];

        private readonly Dictionary<BulletSim.Bullet, ACMITracer> tracers = [];
        private readonly Dictionary<BulletSim.Bullet, ACMITracer> newTracers = [];

        private readonly Dictionary<Shockwave, ACMIShockwave> waves = [];
        private readonly Dictionary<Shockwave, ACMIShockwave> newWaves = [];

        internal Recorder(Mission mission)
        {
            startDate = DateTime.Today + TimeSpan.FromHours(mission.environment.timeOfDay);
            curTime = startDate;

            writer = new ACMIWriter(startDate);
            Plugin.Logger?.LogInfo("[NOBlackBox]: RECORDING STARTED");

            var HQs = FactionRegistry.GetAllHQs();
            foreach (FactionHQ hq in HQs)
                hq.onRegisterUnit += OnUnit;

            foreach (Unit unit in UnitRegistry.allUnits)
                OnUnit(unit);
        }

        ~Recorder()
        {
            Plugin.Logger?.LogInfo("[NOBlackBox]: RECORDING ENDED");
            Close();
        }

        internal void Update(float delta)
        {
            curTime += TimeSpan.FromSeconds(delta);

            foreach (var acmi in flares.ToList())
                if (acmi.flare == null || !acmi.flare.enabled) // Apparently we can lose references? wtf?
                {
                    flares.Remove(acmi);
                    writer.RemoveObject(acmi, curTime);
                }

            foreach (var acmi in tracers.Values.ToList())
                if (!acmi.bullet.active)
                {
                    tracers.Remove(acmi.bullet);
                    writer.RemoveObject(acmi, curTime);
                }

            foreach (var acmi in waves.Values.ToList())
                if (acmi.shockwave == null || acmi.shockwave.enabled == false)
                {
                    waves.Remove(acmi.shockwave);
                    writer.RemoveObject(acmi, curTime);
                }

            foreach (ACMIFlare flare in flares)
                writer.UpdateObject(flare, curTime);

            foreach (ACMITracer tracer in tracers.Values)
                writer.UpdateObject(tracer, curTime);

            foreach (ACMIShockwave wave in waves.Values)
                writer.UpdateObject(wave, curTime);

            flares.AddRange(newFlare);
            newFlare.Clear();

            tracers.AddRange(newTracers);
            newTracers.Clear();

            waves.AddRange(newWaves);
            newWaves.Clear();

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

        private void OnUnit(Unit unit)
        {
            if (!unit.networked || unit.disabled)
                return;

            ACMIUnit acmi;
            switch (unit)
            {
                case Aircraft aircraft:
                    acmi = new ACMIAircraft(aircraft);
                    aircraft.onAddIRSource += OnIR;
                    break;
                case Missile missile:
                    acmi = new ACMIMissile(missile);
                    ((ACMIMissile)acmi).OnDetonate += OnDetonate;
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
                    Plugin.Logger?.LogWarning("Unrecognized unit type");
                    return;
            }

            objects.Add(unit.persistentID, acmi);
            acmi.OnEvent += WriteEvent;
            writer.InitObject(acmi, curTime);

            unit.onDisableUnit += OnDisable;
            acmi.OnGunFired += OnGunFired;

            writer.Flush();
        }

        private void OnDisable(Unit unit)
        {
            ACMIUnit acmi = objects[unit.persistentID];

            objects.Remove(unit.persistentID);
            writer.RemoveObject(acmi, curTime);

            writer.Flush();
        }

        private void OnIR(IRSource source)
        {
            if (source.flare)
            {
                ACMIFlare acmi = new(source);
                newFlare.Add(acmi);
                writer.InitObject(acmi, curTime);
            }
        }

        private void OnDetonate(ACMIMissile missile)
        {
            if (missile.unit is GuidedBomb && HasShockwave(missile.unit))
            {
                Shockwave[] shockwaves = UnityEngine.Object.FindObjectsByType<Shockwave>(FindObjectsSortMode.None);

                foreach (Shockwave wave in shockwaves)
                {
                    if (waves.ContainsKey(wave) || newWaves.ContainsKey(wave))
                        continue;

                    ACMIShockwave acmi = new(wave);
                    writer.InitObject(acmi, curTime);

                    newWaves.Add(wave, acmi);
                }
            }
        }

        private bool HasShockwave(Missile missile)
        {
            Missile.DetonationEffect[] curEffects = (Missile.DetonationEffect[])effects.GetValue(missile);
            foreach (Missile.DetonationEffect detonationEffect in curEffects)
                foreach (GameObject effectObj in detonationEffect.effects)
                    if (effectObj.GetComponentInChildren<Shockwave>() != null)
                        return true;

            return false;
        }

        private void OnGunFired(ACMIUnit _, List<Weapon> weapons)
        {
            foreach (Weapon weapon in weapons)
            {
                if (weapon is Gun gun)
                {
                    BulletSim bulletSim = (BulletSim)bSim.GetValue(gun);
                    List<BulletSim.Bullet> bullets = (List<BulletSim.Bullet>)Recorder.bullets.GetValue(bulletSim);

                    foreach (var bullet in bullets)
                        if (!tracers.ContainsKey(bullet) && !newTracers.ContainsKey(bullet))
                        {
                            ACMITracer aBullet = new(bulletSim, bullet);

                            newTracers.Add(bullet, aBullet);
                            writer.InitObject(aBullet, curTime);
                        }
                }
            }
        }
    }
}
