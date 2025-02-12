using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace NOBlackBox
{
    internal class ACMIMissile: ACMIUnit
    {
        private readonly Dictionary<string, string> TYPES = new()
        {
            { "IRM-S1", "Weapon+Missile+Light" },
            { "MMR-S3[IR]", "Weapon+Missile+Light" },
            { "AAM-29 Scythe [R]", "Weapon+Missile+Medium" },
            { "AGM-48", "Weapon+Missile+Light" },
            { "Ground-to-ground missile", "Weapon+Missile+Light" },
            { "AGM-68", "Weapon+Missile+Medium" },
            { "RAM-45", "Weapon+Missile+Medium" },
            { "StratoLance R9", "Weapon+Missile+Heavy" },
            { "AGR-18", "Weapon+Rocket+Light" },
            { "ARAD-116", "Weapon+Missile+Heavy" },
            { "ALM-C450", "Weapon+Missile+Heavy" },
            { "ALND-4 (20kt)", "Weapon+Missile+Heavy" },
            { "AShM-300", "Weapon+Missile+Heavy" }
        };

        public readonly new Missile unit;

        private float lastAGL = float.NaN;
        private float lastTAS = float.NaN;
        private float lastAOA = float.NaN;
        private int lastTarget = -1;
        internal bool Detonated
        {
            get; private set;
        }

        public ACMIMissile(Missile missile): base(missile)
        {
            unit = missile;

            missile.onDisableUnit += (Unit _) =>
            {
                if (Detonated)
                    FireEvent("LeftArea", [id], "");
                else
                    FireEvent("Destroyed", [id], "");
            };
        }

        override public Dictionary<string, string> Init()
        {
            Dictionary<string, string> baseProps = base.Init();
            baseProps["Name"] = unit.definition.unitName;
            baseProps.Add("Type", TYPES.GetValueOrDefault(unit.definition.unitName, "Weapon"));
            baseProps.Add("Parent", unit.ownerID.ToString("X"));

            return baseProps;
        }

        public override Dictionary<string, string> Update()
        {
            Dictionary<string, string> baseProps = base.Update();

            bool isDetonated = (bool)typeof(Missile).GetField("detonated", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(unit);
            if (!Detonated && isDetonated)
            {
                Plugin.Logger.LogDebug("Detonated");
                Detonated = true;
                FireEvent("LeftArea", [unit.persistentID], string.Empty);
            }

            if (unit.speed != lastTAS)
            {
                baseProps.Add("TAS", unit.speed.ToString("0.##"));
                baseProps.Add("Mach", (unit.speed / 340).ToString("0.###"));
                lastTAS = unit.speed;
            }

            Vector3 vector3 = unit.transform.InverseTransformDirection(unit.rb.velocity);
            float num = Mathf.Atan2(vector3.y, vector3.z) * -57.29578f;

            if (num != lastAOA)
            {
                baseProps.Add("AOA", num.ToString("0.##"));
                lastAOA = num;
            }

            if (unit.radarAlt != lastAGL)
            {
                baseProps.Add("AGL", unit.radarAlt.ToString("0.##"));
                lastAGL = unit.radarAlt;
            }

            if (unit.targetID != lastTarget)
            {
                if (unit.targetID != -1)
                {
                    baseProps.Add("LockedTarget", unit.targetID.ToString("X"));

                    if (lastTarget == -1)
                        baseProps.Add("LockedTargetMode", "1");
                }
                else
                    baseProps.Add("LockedTargetMode", "0");


                lastTarget = unit.targetID;
            }

            return baseProps;
        }
    }
}
