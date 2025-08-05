using NuclearOption.SavedMission;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;

namespace NOBlackBox
{
    internal class ACMIMissile : ACMIUnit
    {
        private readonly Dictionary<string, string> TACVIEWTYPES = new()
        {
            {"MSL", "Weapon+Missile"},
            {"BOMB", "Weapon+Bomb"},
            {"SHL", "Projectile+Shell" }
        };

        public readonly new Missile unit;

        public event Action<ACMIMissile>? OnDetonate;
        //private FieldInfo detonatedFieldInfo;
        //private FieldInfo warheadField;
        //private Type warheadType;
        //private object warheadInstance;

        FieldInfo warheadField;
        FieldInfo detonatedField;
        FieldInfo armedField;

        private float lastAGL = float.NaN;
        private float lastTAS = float.NaN;
        private float lastAOA = float.NaN;
        private int lastTarget = -1;
        internal bool Detonated
        {
            get; private set;
        }

        public ACMIMissile(Missile missile) : base(missile)
        {
            unit = missile;
            //warheadType = missile.GetType().GetNestedType("Warhead", BindingFlags.NonPublic);
            //warheadField = missile.GetType().GetField("warhead", BindingFlags.NonPublic | BindingFlags.Instance);
            //detonatedFieldInfo = missile.GetType().GetNestedType("Warhead", BindingFlags.NonPublic | BindingFlags.Public).GetField("detonated", BindingFlags.NonPublic | BindingFlags.Instance);
            base.postDisableAction = true;


            missile.onDisableUnit += (Unit _) =>
            {
                if (Detonated)
                {
                    Plugin.Logger?.LogInfo($"{unit.persistentID} DETONATED (onDisableUnit)");
                    OnDetonate?.Invoke(this);
                    FireEvent("LeftArea", [id], "");
                    
                } else
                {
                    FireEvent("Destroyed", [id], "");
                }   
            };
        }

        override public Dictionary<string, string> Init()
        {
            Dictionary<string, string> baseProps = base.Init();
            baseProps["Name"] = unit.definition.unitName;
            baseProps.Add("CallSign", unit.definition.unitName);
            baseProps.Add("Type", TACVIEWTYPES.GetValueOrDefault(unit.definition.code, "Weapon"));
            baseProps.Add("Parent", unit.ownerID.ToString("X", CultureInfo.InvariantCulture));

            return baseProps;
        }

        public override Dictionary<string, string> Update()
        {
            Dictionary<string, string> baseProps = base.Update();

            warheadField = typeof(Missile).GetField("warhead", BindingFlags.NonPublic | BindingFlags.Instance);
            object warheadInstance = warheadField.GetValue(unit);
            Type warheadType = warheadInstance.GetType();
            detonatedField = warheadType.GetField("detonated", BindingFlags.NonPublic | BindingFlags.Instance);
            armedField = warheadType.GetField("Armed", BindingFlags.Public | BindingFlags.Instance);
            bool isDetonated = (bool)detonatedField.GetValue(warheadInstance);
            bool isArmed = (bool)armedField.GetValue(warheadInstance);

            if (!Detonated && isDetonated && base.postDisableAction)
            {
                if (isArmed)
                {
                    OnDetonate?.Invoke(this);
                }
                
                Detonated = true;
                base.postDisableAction = false;
                FireEvent("LeftArea", [unit.persistentID], string.Empty);
            }
            
            if (unit.speed != lastTAS && Configuration.RecordSpeed.Value == true)
            {
                baseProps.Add("TAS", unit.speed.ToString("0.##", CultureInfo.InvariantCulture));
                baseProps.Add("Mach", (unit.speed / 340).ToString("0.###", CultureInfo.InvariantCulture));
                lastTAS = unit.speed;
            }

            Vector3 vector3 = unit.transform.InverseTransformDirection(unit.rb.velocity);
            float num = Mathf.Atan2(vector3.y, vector3.z) * -57.29578f;

            if (num != lastAOA && Configuration.RecordAOA.Value == true)
            {
                baseProps.Add("AOA", num.ToString("0.##", CultureInfo.InvariantCulture));
                lastAOA = num;
            }

            if (unit.radarAlt != lastAGL && Configuration.RecordAGL.Value == true)
            {
                baseProps.Add("AGL", unit.radarAlt.ToString("0.##", CultureInfo.InvariantCulture));
                lastAGL = unit.radarAlt;
            }

            if (unit.targetID != lastTarget)
            {
                if (unit.targetID != -1)
                {
                    baseProps.Add("LockedTarget", unit.targetID.ToString("X", CultureInfo.InvariantCulture));

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
