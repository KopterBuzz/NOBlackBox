using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace NOBlackBox
{
    internal class ACMIAircraft_mono : ACMIUnit_mono
    {
        private readonly static Dictionary<string, string> TYPES = new()
        {
            { "CI-22", "Air+FixedWing" },
            { "T/A-30", "Air+FixedWing" },
            { "SAH-46", "Air+Rotorcraft" },
            { "FS-12", "Air+FixedWing" },
            { "KR-67", "Air+FixedWing" },
            { "EW-25", "Air+FixedWing" },
            { "SFB-81", "Air+FixedWing" },
            { "VL-49", "Air+Rotorcraft" },
            { "FS-20", "Air+FixedWing" },
            { "UH-90", "Air+Rotorcraft" },
            { "A-19", "Air+FixedWing" }
        };

        private bool lastGear = false;
        private bool lastRadar = false;
        private float lastAGL = float.NaN;
        private float lastTAS = float.NaN;
        private float lastAOA = float.NaN;
        private float lastThrottle = float.NaN;
        private float lastPitch = float.NaN;
        private float lastYaw = float.NaN;
        private float lastRoll = float.NaN;
        private float lastThrust = float.NaN;
        private float avgThrust = float.NaN;

        private Vector3 lastHead = Vector3.zero;

        Aircraft aircraft;
        Aircraft localAircraft = null;

        public virtual void Init(Aircraft aircraft)
        {
            GameManager.GetLocalAircraft(out localAircraft);
            if(localAircraft && localAircraft.persistentID == aircraft.persistentID) { base.unit = localAircraft; } else { base.unit = aircraft; }
            this.aircraft = (Aircraft)base.unit;
            base.unitId = aircraft.persistentID.Id;
            base.tacviewId = aircraft.persistentID.Id + 1;
            base.destroyedEvent = true;
            base.canTarget = true;
            lastState = aircraft.unitState;
            Faction? faction = this.unit.NetworkHQ?.faction;
            props = new Dictionary<string, string>()
            {
                { "Name", this.unit.definition.unitName },
                { "Coalition", faction?.factionName ?? "Neutral" },
                { "Color", faction == null ? "Green" : (faction.factionName == "Boscali" ? "Blue" : "Red") },
                { "Debug", lastState.ToString()},
                { "Type", TYPES.GetValueOrDefault(aircraft.definition.code, "Air")},
                { "CallSign", $"{aircraft.definition.code} {tacviewId:X}" }
            };
            if (aircraft.Player != null)
            {
                //props.Add("Pilot", aircraft.Player.PlayerName);
                props["CallSign"] = $"{aircraft.definition.code} ({aircraft.Player.PlayerName}) {tacviewId:X}";
                if (Configuration.RecordSteamID.Value == true)
                {
                    props.Add("Registration", aircraft.Player.SteamID.ToString());
                } 
            }
            Plugin.recorderMono.GetComponent<Recorder_mono>().invokeWriterUpdate(this);
            props = [];
            this.enabled = true;
            base.enabled = true;
        }

        public override void Update()
        {
            
            base.destroyedEvent = !aircraft.IsLanded();
            if (!this.enabled || unit.disabled)
            {
                return;
            }
            timer += Time.deltaTime;
            if (timer < Configuration.aircraftUpdateDelta.Value)
            {
                return;
            }
            UpdatePose();
            UpdateAircraft();
            UpdateState();
            UpdateTargets();
            Plugin.recorderMono.GetComponent<Recorder_mono>().invokeWriterUpdate(this);
            props = [];
            timer = 0;
        }

        internal override void UpdateTargets()
        {
            targets = aircraft.weaponManager.GetTargetList().ToArray();
            if (targets.Any() && targets != lastTargets)
            {
                lastTargets = targets;
                int max = targets.Length;
                if (max > 10)
                {
                    max = 10;
                }
                if (targets.Length > 1)
                {
                    
                    for (int i = 0; i < max; i++)
                    {
                        if (i == 0)
                        {
                            lockedTargetString = "LockedTarget";
                        }
                        else
                        {
                            lockedTargetString = $"LockedTarget{i:X}";
                        }
                            props.Add(lockedTargetString, $"{GetTacviewIdOfUnit(targets[i].persistentID.Id):X}");
                    }
                }
            }
        }

        private float getAvgThrust()
        {
            float avgThrust = float.NaN;

            int count = aircraft.engines.Count;
            for (int i = 0; i < aircraft.engines.Count;i++)
            {
                float thrust = aircraft.engines[i].GetThrust();
                if (thrust != 0f)
                {
                    avgThrust = avgThrust + thrust;
                } else
                {
                    count = count - 1;
                } 
            }
            if (avgThrust != 0f)
            {
                avgThrust /= count;
            }
            return avgThrust;
        }

        void UpdateAircraft()
        {
            if (aircraft.speed != lastTAS && Configuration.RecordSpeed.Value == true)
            {
                props.Add("TAS", aircraft.speed.ToString("0.##", CultureInfo.InvariantCulture));
                props.Add("Mach", (aircraft.speed / 340).ToString("0.###", CultureInfo.InvariantCulture));
                lastTAS = aircraft.speed;
            }

            Vector3 vector3 = aircraft.cockpit.transform.InverseTransformDirection(aircraft.cockpit.rb.velocity);
            float num = MathF.Round(Mathf.Atan2(vector3.y, vector3.z) * -57.29578f, 2);

            if (num != lastAOA && Configuration.RecordAOA.Value == true)
            {
                props.Add("AOA", num.ToString("0.##"));
                lastAOA = num;
            }

            if (aircraft.radarAlt != lastAGL && Configuration.RecordAGL.Value == true)
            {
                props.Add("AGL", Mathf.Max(0, aircraft.radarAlt).ToString("0.##", CultureInfo.InvariantCulture));
                lastAGL = aircraft.radarAlt;
            }

            if (aircraft.gearDeployed != lastGear && Configuration.RecordLandingGear.Value == true)
            {
                props.Add("LandingGear", aircraft.gearDeployed ? "1" : "0");
                lastGear = aircraft.gearDeployed;
            }

            if (aircraft.radar != lastRadar && Configuration.RecordRadarMode.Value == true)
            {
                props.Add("RadarMode", aircraft.radar.activated ? "1" : "0");
                lastRadar = aircraft.radar;
            }

            if (localAircraft && localAircraft.persistentID == aircraft.persistentID && CameraStateManager.cameraMode == CameraMode.cockpit && Configuration.RecordPilotHead.Value == true)
            {
                
                Camera camera = CameraStateManager.i.mainCamera;

                Vector3 rot = camera.transform.localEulerAngles;

                float fax = MathF.Round(rot.x, 2);
                float fay = MathF.Round(rot.y, 2);
                float faz = MathF.Round(rot.z, 2);

                Vector3 newRot = new(fax, fay, faz);

                if (newRot != lastHead)
                {
                    if (!Mathf.Approximately(newRot.x, lastHead.x))
                    {
                        float adjusted_pitch = newRot.x > 180.0f ? 360 - newRot.x : -newRot.x;
                        props.Add("PilotHeadPitch", adjusted_pitch.ToString("0.##", CultureInfo.InvariantCulture));
                    }

                    if (!Mathf.Approximately(newRot.y, lastHead.y))
                    {
                        float adjusted_yaw = newRot.y;
                        props.Add("PilotHeadYaw", adjusted_yaw.ToString("0.##", CultureInfo.InvariantCulture));
                    }

                    lastHead = newRot;
                }
            }

            if (Configuration.RecordExtraTelemetry.Value == true)
            {
               
                if (lastThrottle != aircraft.GetInputs().throttle)
                {
                    props.Add("Throttle", aircraft.GetInputs().throttle.ToString("0.##", CultureInfo.InvariantCulture));
                    lastThrottle = aircraft.GetInputs().throttle;
                }
                if (lastRoll != aircraft.GetInputs().roll)
                {
                    props.Add("RollControlInput", aircraft.GetInputs().roll.ToString("0.##", CultureInfo.InvariantCulture));
                    lastRoll = aircraft.GetInputs().roll;
                }
                if (lastPitch != aircraft.GetInputs().pitch)
                {
                    props.Add("PitchControlInput", aircraft.GetInputs().pitch.ToString("0.##", CultureInfo.InvariantCulture));
                    lastPitch = aircraft.GetInputs().pitch;
                }
                if (lastYaw != aircraft.GetInputs().yaw)
                {
                    props.Add("YawControlInput", aircraft.GetInputs().yaw.ToString("0.##", CultureInfo.InvariantCulture));
                    lastYaw = aircraft.GetInputs().yaw;
                }
                if (lastThrust != getAvgThrust())
                {
                    avgThrust = getAvgThrust();
                    props.Add("EngineRPM", avgThrust.ToString("0.##", CultureInfo.InvariantCulture));
                    lastThrust = avgThrust;
                }
            }
        }
    }
}
