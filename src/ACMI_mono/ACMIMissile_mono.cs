using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace NOBlackBox
{
    internal class ACMIMissile_mono : ACMIUnit_mono
    {
        private readonly Dictionary<string, string> TACVIEWTYPES = new()
        {
            {"MSL", "Weapon+Missile"},
            {"BOMB", "Weapon+Bomb"},
            {"SHL", "Projectile+Shell" }
        };

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
        Missile missile;
        GameObject shockwave;

        public event Action<ACMIMissile_mono>? OnDetonate;

        public virtual void Init(Missile missile)
        {
            base.unit = missile;
            this.missile = (Missile)base.unit;
            base.unitId = missile.persistentID;
            base.tacviewId = missile.persistentID + 1;
            lastState = missile.unitState;
            Faction? faction = base.unit.NetworkHQ?.faction;
            props = new Dictionary<string, string>()
            {
                { "Name", base.unit.definition.unitName },
                { "Coalition", faction?.factionName ?? "Neutral" },
                { "Color", faction == null ? "Green" : (faction.factionName == "Boscali" ? "Blue" : "Red") },
                { "Debug", lastState.ToString()}
            };
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
            if (timer < Configuration.munitionUpdateDelta.Value)
            {
                return;
            }
            UpdatePose();
            UpdateState();
            Plugin.recorderMono.GetComponent<Recorder_mono>().invokeWriterUpdate(this);
            props = [];
            timer = 0;
        }

        void UpdateMissile()
        {
            warheadField = typeof(Missile).GetField("warhead", BindingFlags.NonPublic | BindingFlags.Instance);
            object warheadInstance = warheadField.GetValue(unit);
            Type warheadType = warheadInstance.GetType();
            detonatedField = warheadType.GetField("detonated", BindingFlags.NonPublic | BindingFlags.Instance);
            armedField = warheadType.GetField("Armed", BindingFlags.Public | BindingFlags.Instance);
            bool isDetonated = (bool)detonatedField.GetValue(warheadInstance);
            bool isArmed = (bool)armedField.GetValue(warheadInstance);

            if (unit.speed != lastTAS && Configuration.RecordSpeed.Value == true)
            {
                props.Add("TAS", unit.speed.ToString("0.##", CultureInfo.InvariantCulture));
                props.Add("Mach", (unit.speed / 340).ToString("0.##", CultureInfo.InvariantCulture));
                lastTAS = unit.speed;
            }

            Vector3 vector3 = unit.transform.InverseTransformDirection(unit.rb.velocity);
            float num = Mathf.Atan2(vector3.y, vector3.z) * -57.29578f;

            if (num != lastAOA && Configuration.RecordAOA.Value == true)
            {
                props.Add("AOA", num.ToString("0.##", CultureInfo.InvariantCulture));
                lastAOA = num;
            }

            if (unit.radarAlt != lastAGL && Configuration.RecordAGL.Value == true)
            {
                props.Add("AGL", unit.radarAlt.ToString("0.##", CultureInfo.InvariantCulture));
                lastAGL = unit.radarAlt;
            }
            
            if (missile.targetID != lastTarget)
            {
                if (missile.targetID != -1)
                {
                    props.Add("LockedTarget", missile.targetID.ToString("X", CultureInfo.InvariantCulture));

                    if (lastTarget == -1)
                        props.Add("LockedTargetMode", "1");
                }
                else
                    props.Add("LockedTargetMode", "0");


                lastTarget = missile.targetID;
            }
        }
    }
}
