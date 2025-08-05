using System.Collections.Generic;
using System.Net;

namespace NOBlackBox
{
    public class ACMIBuilding: ACMIUnit
    {
        public new readonly Building unit;
        private string coalition;

        public ACMIBuilding(Building building) : base(building)
        {
            unit = building;

            building.onDisableUnit += (Unit _) =>
            {
                FireEvent("Destroyed", [id], "");
            };
        }

        public override Dictionary<string, string> Init()
        {
            Dictionary<string, string> props = base.Init();
            coalition = props["Coalition"];

            return new()
            {
                { "Name", unit.definition.unitName },
                { "Type", "Ground+Static+Building" + (unit.definition.code == "RDR" ? "+Sensor" : string.Empty) }
            };
        }

        public override Dictionary<string, string> Update()
        {
            Dictionary<string, string> baseProps = base.Update();

            if (unit.NetworkHQ?.faction.factionName != coalition)
            {
 
                baseProps.Add("Coalition", unit.NetworkHQ?.faction.factionName ?? "Neutral");

                string color = "Green";
                switch (unit.NetworkHQ?.faction.factionName) {
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

                baseProps.Add("Color", color);
            }

            return baseProps;
            //return base.Update();
        }
    }
}
