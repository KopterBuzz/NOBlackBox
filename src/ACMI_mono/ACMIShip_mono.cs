using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NOBlackBox
{
    internal class ACMIShip_mono : ACMIUnit_mono
    {
        /*
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
        */
        private Unit?[] lastTarget;
        private Ship ship;

        public virtual void Init(Ship ship)
        {
            base.unit = ship;
            this.ship = (Ship)base.unit;
            base.unitId = ship.persistentID.Id;
            base.tacviewId = ship.persistentID.Id + 1;
            base.destroyedEvent = true;
            base.canTarget = true;
            lastTarget = new Unit?[Math.Min(10, ship.weaponStations.Count)];
            Faction? faction = base.unit.NetworkHQ?.faction;
            string[] info = { "Default", "Sea+Watercraft" };
            if (Plugin.NOBlackBoxUnitInfo["ships"].ContainsKey(ship.definition.unitName))
            {
                info = Plugin.NOBlackBoxUnitInfo["ships"][ship.definition.unitName];
            }
            props = new Dictionary<string, string>()
            {
                { "Name", base.unit.definition.unitName },
                { "Coalition", faction?.factionName ?? "Neutral" },
                { "CallSign", $"{ship.definition.unitName} {tacviewId:X}"},
                { "Color", faction == null ? "Green" : (faction.factionName == "Boscali" ? "Blue" : "Red") },
                { "Type", info[1] },
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
            UpdateTargets();
            UpdateState();
            Plugin.recorderMono.GetComponent<Recorder_mono>().invokeWriterUpdate(this);
            props = [];
            timer = 0;
        }

        internal override void UpdateTargets()
        {
            foreach (WeaponStation station in ship.weaponStations)
            {
                try
                {
                    targets.AddItem<Unit>(station.GetTurret().GetTarget());
                }
                catch
                {
                    //no target
                }

            }

            if (targets.Any())
            {
                if (!lastTargets.Any())
                {
                    lastTargets = targets;
                }
                else
                {
                    if (lastTargets == targets)
                    {
                        return;
                    }
                }
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
            targets = [];
        }
    }
}
