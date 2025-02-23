using System.Collections.Generic;

namespace NOBlackBox
{
    internal class ACMIPilot(PilotDismounted pilot) : ACMIUnit(pilot)
    {
        public new readonly PilotDismounted unit = pilot;

        public override Dictionary<string, string> Init()
        {
            Dictionary<string, string> props = base.Init();

            props.Add("Type", "Ground+Light+Human+Air+Parachutist");
            props.Add("Parent", unit.NetworkparentUnit != 0 ? unit.NetworkparentUnit.ToString("X") : int.MaxValue.ToString("X"));

            props["Name"] = unit.Networkplayer != null ? unit.Networkplayer.PlayerName : "Pilot";

            return props;
        }
    }
}
