using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace NOBlackBox
{ 
    internal class ACMIPilotDismounted_mono : ACMIUnit_mono
    {
        public PilotDismounted pilot;

        public virtual void Init(PilotDismounted pilot)
        {
            base.unit = pilot;
            this.pilot = (PilotDismounted)base.unit;
            base.unitId = unit.persistentID;
            base.tacviewId = unit.persistentID + 1;
            lastState = unit.unitState;
            Faction? faction = this.unit.NetworkHQ?.faction;
            props = new Dictionary<string, string>()
            {
                { "Name", this.unit.definition.unitName },
                { "Coalition", faction?.factionName ?? "Neutral" },
                { "Color", faction == null ? "Green" : (faction.factionName == "Boscali" ? "Blue" : "Red") },
                { "Type", "Ground+Light+Human+Air+Parachutist" },
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
            if (timer < Configuration.vehicleUpdateDelta.Value)
            {
                return;
            }
            if (pilot.radarAlt <= 1)
            {
                props.Add("Visible", "0.0");
                props.Add("Type", null);
                Plugin.Logger?.LogDebug($"PARACHUTE LANDED {unitId.ToString(CultureInfo.InvariantCulture)}");
                Plugin.recorderMono.GetComponent<Recorder_mono>().invokeWriterUpdate(this);
                Plugin.recorderMono.GetComponent<Recorder_mono>().invokeWriterRemove(this);
                GameObject.Destroy(this);
            }
            UpdatePose();
            UpdateState();
            Plugin.recorderMono.GetComponent<Recorder_mono>().invokeWriterUpdate(this);
            props = [];
            timer = 0;
        }
    }
}
