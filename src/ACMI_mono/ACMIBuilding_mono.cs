using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace NOBlackBox
{
    internal class ACMIBuilding_mono : ACMIUnit_mono
    {
        public Building building;
        private string coalition;
        public virtual void Init(Building building)
        {
            base.unit = building;
            this.building = (Building)base.unit;
            base.unitId = unit.persistentID.Id;
            base.tacviewId = unit.persistentID.Id + 1;
            base.destroyedEvent = true;
            lastState = unit.unitState;
            Faction? faction = this.unit.NetworkHQ?.faction;
            props = new Dictionary<string, string>()
            {
                { "Name", this.unit.definition.unitName },
                { "CallSign", $"{building.definition.code} {tacviewId:X}"},
                { "Coalition", faction?.factionName ?? "Neutral" },
                { "Color", faction == null ? "Green" : (faction.factionName == "Boscali" ? "Blue" : "Red") },
                { "Type","Ground+Static+Building"},
                { "Debug", lastState.ToString()}
            };
            UpdatePose();
            UpdateState();
            Plugin.recorderMono.GetComponent<Recorder_mono>().invokeWriterUpdate(this);
            props = [];
            this.enabled = true;
            base.enabled = true;
        }

        public override void Update()
        {
            timer += Time.deltaTime;
            if (timer < Configuration.buildingUpdateDelta.Value)
            {
                return;
            }
            if (unit.NetworkHQ?.faction.factionName != coalition)
            {
                coalition = unit.NetworkHQ?.faction.factionName ?? "Neutral";
                props.Add("Coalition", unit.NetworkHQ?.faction.factionName ?? "Neutral");

                string color = "Green";
                switch (unit.NetworkHQ?.faction.factionName)
                {
                    case "Boscali":
                        color = "Blue";
                        break;
                    case "Primeva":
                        color = "Red";
                        break;
                    default:
                        color = "Green";
                        break;
                }

                props.Add("Color", color);
                Plugin.recorderMono.GetComponent<Recorder_mono>().invokeWriterUpdate(this);
                props = [];
                timer = 0;
            }
        }
    }
}
