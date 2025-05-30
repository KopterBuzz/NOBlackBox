using System.Collections.Generic;
using System.Globalization;

namespace NOBlackBox
{
    internal class ACMIGroundVehicle: ACMIUnit
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

        public new readonly GroundVehicle unit;

        public ACMIGroundVehicle(GroundVehicle unit): base(unit)
        {
            this.unit = unit;

            unit.onDisableUnit += (Unit _) => FireEvent("Destroyed", [id], "");
        }

        public override Dictionary<string, string> Init()
        {
            Dictionary<string, string> props = base.Init();

            props.Add("Type", TYPES.GetValueOrDefault(unit.definition.unitName, "Ground"));
            
            
            if (RANGE.TryGetValue(unit.definition.unitName, out int range))
                
                props.Add("EngagementRange", range.ToString(CultureInfo.InvariantCulture));
            
            return props;
        }

        public override Dictionary<string, string> Update()
        {
            Dictionary<string, string> props = base.Update();

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

            return props;
        }
    }
}
