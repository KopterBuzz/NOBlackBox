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

            props["Type"] = "Ground+Static+Building" + (unit.definition.code == "RDR" ? "+Sensor" : string.Empty);

            return props;
        }

        public override Dictionary<string, string> Update()
        {
            Dictionary<string, string> baseProps = base.Update();

            if (unit.NetworkHQ?.faction.factionName != coalition)
            {
                try {
                    baseProps.Add("Coalition", unit.NetworkHQ?.faction.factionName ?? "Neutral");
                } catch
                {
                    baseProps["Coalition"] = unit.NetworkHQ?.faction.factionName ?? "Neutral";
                }
                
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
