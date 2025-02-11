using System;
using System.Collections.Generic;

namespace NOBlackBox
{
    public class ACMIShip: ACMIUnit
    {
        private static readonly Dictionary<string, string> TYPES = new()
        {
            { "Shard Class Corvette", "Sea+Watercraft+Medium+Warship" },
            { "Dynamo Class Destroyer", "Sea+Watercraft+Medium+Warship" },
            { "Hyperion Class Carrier", "Sea+Watercraft+Heavy+AircraftCarrier" }
        };

        private static readonly Dictionary<string, int> RANGES = new()
        {
            { "Shared Class Corvette", 15000 },
            { "Dynamo Class Destroyer", 50000 },
            { "Hyperion Class Carrier", 15000 }
        };

        private Unit?[] lastTarget;
        private new readonly Ship unit;

        public ACMIShip(Ship ship): base(ship)
        {
            unit = ship;

            ship.onDisableUnit += (Unit _) =>
            {
                FireEvent("Destroyed", [id], "");
            };

            lastTarget = new Unit?[Math.Min(10, ship.weaponStations.Count)]; 
        }

        public override Dictionary<string, string> Init()
        {
            Dictionary<string, string> props = base.Init();

            props.Add("Type", TYPES.GetValueOrDefault(unit.definition.code, "Sea+Watercraft"));

            if (RANGES.TryGetValue(unit.definition.code, out int range))
                props.Add("EngagementRange", range.ToString());

            return props;
        }
    }
}
