using System.Collections.Generic;

namespace NOBlackBox
{
    public class ACMIContainer: ACMIUnit
    {
        public new readonly Container unit;

        public ACMIContainer(Container container): base(container)
        {
            unit = container;

            container.onDisableUnit += (Unit _) =>
            {
                FireEvent("Destroyed", [id], "");
            };
        }

        public override Dictionary<string, string> Init()
        {
            Dictionary<string, string> props = base.Init();

            props["Type"] = "Misc+Container";

            return props;
        }
    }
}
