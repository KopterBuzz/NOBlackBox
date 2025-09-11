using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace NOBlackBox
{
    internal class ACMIShip_mono : ACMIUnit_mono
    {
        private static readonly Dictionary<string, string> TYPES = new()
        {
            { "Shard Class Corvette", "Sea+Watercraft+Medium+Warship" },
            { "Dynamo Class Destroyer", "Sea+Watercraft+Medium+Warship" },
            { "Hyperion Class Carrier", "Sea+Watercraft+Heavy+AircraftCarrier" },
            { "Annex Class Carrier", "Sea+Watercraft+Heavy+AircraftCarrier" },
            { "OTB-31 landing craft", "Sea+Watercraft" }
        };

        private static readonly Dictionary<string, int> RANGES = new()
        {
            { "Shard Class Corvette", 15000 },
            { "Dynamo Class Destroyer", 50000 },
            { "Hyperion Class Carrier", 15000 }
        };

        private Unit?[] lastTarget;
        private Ship ship;

        public virtual void Init(Ship ship)
        {
            base.unit = ship;
            this.ship = (Ship)base.unit;
            base.unitId = ship.persistentID;
            base.tacviewId = ship.persistentID + 1;
            lastTarget = new Unit?[Math.Min(10, ship.weaponStations.Count)];
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
            if (timer < Configuration.vehicleUpdateDelta.Value)
            {
                return;
            }
            UpdatePose();
            UpdateState();
            Plugin.recorderMono.GetComponent<Recorder_mono>().invokeWriterUpdate(this);
            props = [];
            timer = 0;
        }
    }
}
