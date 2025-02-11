using System.Collections.Generic;

namespace NOBlackBox
{
    public class ACMIBuilding: ACMIUnit
    {
        public new readonly Building unit;

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

            props["Type"] = "Ground+Static+Building" + (unit.definition.code == "RDR" ? "+Sensor" : string.Empty);

            return props;
        }

        public override Dictionary<string, string> Update()
        {
            return base.Update();
        }
    }
}
