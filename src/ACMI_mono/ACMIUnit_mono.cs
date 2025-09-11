using Mirage.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using UnityEngine;
using static Mirage.NetworkBehaviour;

namespace NOBlackBox
{
    internal class ACMIUnit_mono : ACMIObject_mono
    {
        private Vector3 lastPos = new(float.NaN, float.NaN, float.NaN);
        private Vector3 lastRot = new(float.NaN, float.NaN, float.NaN);
        internal Unit unit;
        internal Unit.UnitState lastState;

        public virtual void Init(Unit unit)
        {
            this.unit = unit;
            base.unitId = unit.persistentID;
            base.tacviewId = unit.persistentID + 1;
            lastState = unit.unitState;
            Faction? faction = this.unit.NetworkHQ?.faction;
            props = new Dictionary<string, string>()
            {
                { "Name", this.unit.definition.unitName },
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

        }
        public virtual void LateUpdate()
        {
            try
            {
                if (!unit.enabled || unit.disabled)
                {
                    base.disabled = true;
                    props.Add("Visible", "0.0");
                    Plugin.recorderMono.GetComponent<Recorder_mono>().invokeWriterUpdate(this);
                    props = [];
                    Plugin.recorderMono.GetComponent<Recorder_mono>().invokeWriterRemove(this);
                    this.enabled = false;
                    Plugin.Logger?.LogDebug($"DISABLING UNIT {unitId.ToString(CultureInfo.InvariantCulture)}");
                    GameObject.Destroy(this);
                }
            } catch
            {
                GameObject.Destroy(this);
            }
        }

        private string UpdatePosition(Vector3 newPos, Vector3 newRot)
        {
            string x = Mathf.Approximately(newPos.x, lastPos.x) ? "" : newPos.x.ToString(CultureInfo.InvariantCulture);
            string y = Mathf.Approximately(newPos.y, lastPos.y) ? "" : newPos.y.ToString(CultureInfo.InvariantCulture);
            string z = Mathf.Approximately(newPos.z, lastPos.z) ? "" : newPos.z.ToString(CultureInfo.InvariantCulture);

            float adjusted_roll = newRot.z > 180.0f ? 360 - newRot.z : -newRot.z;
            float adjusted_pitch = newRot.x > 180.0f ? 360 - newRot.x : -newRot.x;
            float adjusted_yaw = newRot.y;

            string roll = Mathf.Approximately(newRot.z, lastRot.z) ? "" : adjusted_roll.ToString(CultureInfo.InvariantCulture);
            string pitch = Mathf.Approximately(newRot.x, lastRot.x) ? "" : adjusted_pitch.ToString(CultureInfo.InvariantCulture);
            string yaw = Mathf.Approximately(newRot.y, lastRot.y) ? "" : adjusted_yaw.ToString(CultureInfo.InvariantCulture);

            (float latitude, float longitude) = Helpers.CartesianToGeodetic(newPos.x, newPos.z);

            return $"{(newPos.x != lastPos.x ? longitude.ToString(CultureInfo.InvariantCulture) : string.Empty)}|{(newPos.z != lastPos.z ? latitude.ToString(CultureInfo.InvariantCulture) : string.Empty)}|{y}|{roll}|{pitch}|{yaw}|{x}|{z}|{yaw}";
        }

        internal void UpdatePose()
        {
            float fx = MathF.Round(unit.transform.position.GlobalX(), 2);
            float fy = MathF.Round(unit.transform.position.GlobalY(), 2);
            float fz = MathF.Round(unit.transform.position.GlobalZ(), 2);

            float fax = MathF.Round(unit.transform.eulerAngles.x, 2);
            float fay = MathF.Round(unit.transform.eulerAngles.y, 2);
            float faz = MathF.Round(unit.transform.eulerAngles.z, 2);

            Vector3 newPos = new(fx, fy, fz);
            Vector3 newRot = new(fax, fay, faz);

            if (newPos != lastPos || newRot != lastRot)
            {
                props.Add("T", UpdatePosition(newPos, newRot).ToString(CultureInfo.InvariantCulture));

                lastPos = newPos;
                lastRot = newRot;
            }
        }

        internal void UpdateState()
        {
            if (unit.unitState != lastState)
            {
                lastState = unit.unitState;
                props.Add("Debug",lastState.ToString());
            }
        }
    }
}
