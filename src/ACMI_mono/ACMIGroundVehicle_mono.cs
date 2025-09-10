using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace NOBlackBox
{
    internal class ACMIGroundVehicle_mono : ACMIUnit_mono
    {
        private readonly static Dictionary<string, string> TYPES = new()
        {
            { "HLT", "Ground+Heavy+Vehicle" },
            { "Stratolance R9 Launcher", "Ground+Heavy+Static+AntiAircraft" },
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
            { "Stratolance R9 Launcher", 50000 },
            { "T9K41 Boltstrike", 15000 },
            { "LCV24 AA", 5000 },
            { "AFV6 AA", 5000 },
            { "AFV8 Mobile Air Defense", 5000 },
            { "Linebreaker SAM", 5000 },
            { "AeroSentry SPAAG", 4000 },
            { "FGA-57 Anvil", 5500 }
        };

        private Unit? lastTarget;

        public virtual void Init(GroundVehicle unit)
        {

            base.unit = unit;
            base.unitId = unit.persistentID;
            base.tacviewId = unit.persistentID + 1;
            lastState = unit.unitState;
            Faction? faction = base.unit.NetworkHQ?.faction;
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
            if (!this.enabled || unit.disabled)
            {
                return;
            }
            timer += Time.deltaTime;
            if (timer < Plugin.vehicleUpdateDelta)
            {
                return;
            }
            UpdatePose();
            UpdateWeaponStations();
            UpdateState();
            Plugin.recorderMono.GetComponent<Recorder_mono>().invokeWriterUpdate(this);
            props = [];
            timer = 0;
        }

        public void UpdateWeaponStations()
        {
            if (unit.weaponStations.Count > 0)
            {
                Turret turret = unit.weaponStations[0].GetTurret();
                Unit? target = turret.GetTarget();

                if (target != lastTarget)
                {
                    if (target != null)
                    {
                        props["LockedTarget"] = target.persistentID.ToString(CultureInfo.InvariantCulture);

                        if (lastTarget == null)
                            props["LockedTargetMode"] = "1";

                        lastTarget = target;
                    }
                    else
                    {
                        if (lastTarget != null)
                        {
                            if (lastTarget.disabled || !turret.GetWeaponStation().reloading) // When reloading we get false negatives
                            {
                                props["LockedTargetMode"] = "0";
                                lastTarget = target;
                            }
                        }
                    }
                }
            }
        }
    }
}
