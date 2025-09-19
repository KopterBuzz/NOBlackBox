using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NOBlackBox
{
    internal class ACMIGroundVehicle_mono : ACMIUnit_mono
    {
        private readonly static Dictionary<string, string> TYPES = new()
        {
            { "HLT", "Ground+Heavy+Vehicle" },
            { "StratoLance R9 Launcher", "Ground+Heavy+Vehicle+AntiAircraft" },
            { "T9K41 Boltstrike", "Ground+Heavy+Vehicle+AntiAircraft" },
            { "Spearhead MBT", "Ground+Heavy+Vehicle+Tank" },
            { "Type-12 MBT", "Ground+Heavy+Vehicle+Tank" },
            { "HLT Radar Truck", "Ground+Heavy+Vehicle+Sensor" },
            { "HLT Flatbed", "Ground+Heavy+Vehicle" },
            { "HLT Munitions Truck", "Ground+Heavy+Vehicle" },
            { "HLT Tractor", "Ground+Heavy+Vehicle" },
            { "HLT Fuel Tanker", "Ground+Heavy+Vehicle" },
            { "LCV25 AA", "Ground+Light+Vehicle+AntiAircraft" },
            { "LCV25 AT", "Ground+Light+Vehicle" },
            { "LCV45 Recon Truck", "Ground+Light+Vehicle" },
            { "AFV6 APC", "Ground+Medium+Vehicle" },
            { "AFV6 IFV", "Ground+Medium+Vehicle" },
            { "AFV6 AA", "Ground+Medium+Vehicle+AntiAircraft" },
            { "AFV6 AT", "Ground+Medium+Vehicle" },
            { "AFV8 APC", "Ground+Medium+Vehicle" },
            { "AFV8 IFV", "Ground+Medium+Vehicle" },
            { "AFV8 Mobile Air Defense", "Ground+Medium+Vehicle+AntiAircraft" },
            { "Linebreaker APC", "Ground+Medium+Vehicle" },
            { "Linebreaker IFV", "Ground+Medium+Vehicle" },
            { "Linebreaker SAM", "Ground+Medium+Vehicle+AntiAircraft" },
            { "AeroSentry SPAAG", "Ground+Medium+Vehicle+AntiAircraft" },
            { "FGA-57 Anvil", "Ground+Medium+Vehicle+AntiAircraft" }
        };

        private readonly static Dictionary<string, int> RANGE = new()
        {
            { "StratoLance R9 Launcher", 50000 },
            { "T9K41 Boltstrike", 15000 },
            { "LCV24 AA", 5000 },
            { "AFV6 AA", 5000 },
            { "AFV8 Mobile Air Defense", 5000 },
            { "Linebreaker SAM", 5000 },
            { "AeroSentry SPAAG", 4000 },
            { "FGA-57 Anvil", 5500 }
        };

        private Unit? lastTarget;

        public virtual void Init(GroundVehicle vehicle)
        {

            base.unit = vehicle;
            base.unitId = unit.persistentID.Id;
            base.tacviewId = unit.persistentID.Id + 1;
            lastState = unit.unitState;
            Faction? faction = base.unit.NetworkHQ?.faction;

            if (new[] {"SAM","RDR","SPAAG"}.Any(c => unit.definition.code.Contains(c)))
            {
                base.destroyedEvent = true;
            }

            props = new Dictionary<string, string>()
            {
                { "Name", this.unit.definition.unitName },
                { "Coalition", faction?.factionName ?? "Neutral" },
                { "CallSign", $"{unit.definition.code} {tacviewId:X}"},
                { "Color", faction == null ? "Green" : (faction.factionName == "Boscali" ? "Blue" : "Red") },
                { "Type", TYPES.GetValueOrDefault(unit.definition.unitName, "Ground") },
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
            foreach (WeaponStation station in unit.weaponStations)
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
