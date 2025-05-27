using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace NOBlackBox
{
    internal class ACMIAircraft: ACMIUnit
    {
        private readonly static Dictionary<string, string> TYPES = new()
        {
            { "CI-22", "Air+FixedWing+Light" },
            { "T/A-30", "Air+FixedWing+Light" },
            { "SAH-46", "Air+Rotorcraft+Medium" },
            { "FS-12", "Air+FixedWing+Medium" },
            { "KR-67", "Air+FixedWing+Medium" },
            { "EW-25", "Air+FixedWing+Heavy" },
            { "SFB-81", "Air+FixedWing+Heavy" },
            { "VL-49", "Air+Rotorcraft+Heavy" },
            { "FS-20", "Air+FixedWing+Light" }
        };

        private bool lastGear = false;
        private bool lastRadar = false;
        private float lastAGL = float.NaN;
        private float lastTAS = float.NaN;
        private float lastAOA = float.NaN;
        private Vector3 lastHead = Vector3.zero;

        public new readonly Aircraft unit;

        public ACMIAircraft(Aircraft aircraft): base(aircraft)
        {
            unit = aircraft;

            unit.onDisableUnit += (Unit _) =>
            {
                if (unit.IsLanded() && unit.speed < 2.5)
                    FireEvent("LeftArea", [id], "");
                else
                    FireEvent("Destroyed", [id], "");
            };
        }

        public override Dictionary<string, string> Init()
        {
            Dictionary<string, string> props = base.Init();
            props.Add("Type", TYPES.GetValueOrDefault(unit.definition.code, "Air"));

            if (unit.Player != null)
            {
                props.Add("Pilot", unit.Player.PlayerName);
                props.Add("CallSign", unit.Player.PlayerName);
            }

            return props;
        }

        public override Dictionary<string, string> Update()
        {
            Dictionary<string, string> baseProps = base.Update();

            if (unit.speed != lastTAS && Configuration.RecordSpeed.Value == true)
            {
                baseProps.Add("TAS", unit.speed.ToString("0.##", CultureInfo.InvariantCulture));
                baseProps.Add("Mach", (unit.speed / 340).ToString("0.###", CultureInfo.InvariantCulture));
                lastTAS = unit.speed;
            }

            Vector3 vector3 = unit.cockpit.transform.InverseTransformDirection(unit.cockpit.rb.velocity);
            float num = MathF.Round(Mathf.Atan2(vector3.y, vector3.z) * -57.29578f, 2);

            if (num != lastAOA && Configuration.RecordAOA.Value == true)
            {
                baseProps.Add("AOA", num.ToString("0.##"));
                lastAOA = num;
            }

            if (unit.radarAlt != lastAGL && Configuration.RecordAGL.Value == true)
            {
                baseProps.Add("AGL", Mathf.Max(0, unit.radarAlt).ToString("0.##", CultureInfo.InvariantCulture));
                lastAGL = unit.radarAlt;
            }

            if (unit.gearDeployed != lastGear && Configuration.RecordLandingGear.Value == true)
            {
                baseProps.Add("LandingGear", unit.gearDeployed ? "1" : "0");
                lastGear = unit.gearDeployed;
            }

            if (unit.radar != lastRadar && Configuration.RecordRadarMode.Value == true)
            {
                baseProps.Add("RadarMode", unit.radar.activated ? "1" : "0");
                lastRadar = unit.radar;
            }

            if (unit.Player == GameManager.LocalPlayer && CameraStateManager.cameraMode == CameraMode.cockpit && Configuration.RecordPilotHead.Value == true)
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
                        baseProps.Add("PilotHeadPitch", adjusted_pitch.ToString("0.##", CultureInfo.InvariantCulture));
                    }

                    if (!Mathf.Approximately(newRot.y, lastHead.y))
                    {
                        float adjusted_yaw = newRot.y;
                        baseProps.Add("PilotHeadYaw", adjusted_yaw.ToString("0.##", CultureInfo.InvariantCulture));
                    }

                    lastHead = newRot;
                }
            }

            return baseProps;
        }
    }
}
