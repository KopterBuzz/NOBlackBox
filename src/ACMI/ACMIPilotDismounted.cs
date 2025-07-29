using System.Collections.Generic;

namespace NOBlackBox
{
    public class ACMIPilotDismounted : ACMIUnit
    {
        public new readonly PilotDismounted unit;

        public ACMIPilotDismounted(PilotDismounted pilot) : base(pilot)
        {
            unit = pilot;

            pilot.onDisableUnit += (Unit _) =>
            {
                FireEvent("Destroyed", [id], "");
            };
        }

        public override Dictionary<string, string> Init()
        {
            Dictionary<string, string> props = base.Init();

            props.Add("Type", "Ground+Light+Human+Air+Parachutist");

            return props;
        }

        public override Dictionary<string, string> Update()
        {
            return base.Update();
        }
    }
}
