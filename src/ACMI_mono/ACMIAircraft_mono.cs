using System;
using System.Collections.Generic;
using System.Globalization;
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
            { "FS-20", "Air+FixedWing" }
        };

        private bool lastGear = false;
        private bool lastRadar = false;
        private float lastAGL = float.NaN;
        private float lastTAS = float.NaN;
        private float lastAOA = float.NaN;
        private Vector3 lastHead = Vector3.zero;

        Aircraft aircraft;

        public virtual void Init(Aircraft aircraft)
        {
            base.unit = aircraft;
            this.aircraft = (Aircraft)base.unit;
            base.unitId = aircraft.persistentID;
            base.tacviewId = aircraft.persistentID + 1;
            lastState = aircraft.unitState;
            Faction? faction = this.unit.NetworkHQ?.faction;
            props = new Dictionary<string, string>()
            {
                { "Name", this.unit.definition.unitName },
                { "Coalition", faction?.factionName ?? "Neutral" },
                { "Color", faction == null ? "Green" : (faction.factionName == "Boscali" ? "Blue" : "Red") },
                { "Debug", lastState.ToString()},
                { "Type", TYPES.GetValueOrDefault(aircraft.definition.code, "Air")}
            };
            if (aircraft.Player != null)
            {
                props.Add("Pilot", aircraft.Player.PlayerName);
                props.Add("CallSign", aircraft.definition.code);
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
            Plugin.recorderMono.GetComponent<Recorder_mono>().invokeWriterUpdate(this);
            props = [];
            timer = 0;
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

            if (aircraft.Player == GameManager.LocalPlayer && CameraStateManager.cameraMode == CameraMode.cockpit && Configuration.RecordPilotHead.Value == true)
            {

                Camera camera = Camera.main;

                float fax = MathF.Round(camera.transform.localEulerAngles.x, 2);
                float fay = MathF.Round(camera.transform.localEulerAngles.y, 2);
                float faz = MathF.Round(camera.transform.localEulerAngles.z, 2);

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
        }
    }
}
