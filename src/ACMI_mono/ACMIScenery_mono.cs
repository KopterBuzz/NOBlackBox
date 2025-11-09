using System;
using System.Collections.Generic;
using System.Text;

namespace NOBlackBox.src.ACMI_mono
{
    internal class ACMIScenery_mono : ACMIUnit_mono
    {
        public Scenery scenery;
        public virtual void Init(Scenery scenery)
        {
            base.unit = scenery;
            this.scenery = (Scenery)base.unit;
            base.unitId = unit.persistentID.Id;
            base.tacviewId = unit.persistentID.Id + 1;
            base.destroyedEvent = true;
            lastState = unit.unitState;
            //Faction? faction = this.unit.NetworkHQ?.faction;
            props = new Dictionary<string, string>()
            {
                { "Name", this.unit.definition.unitName },
                { "CallSign", $"{scenery.definition.code} {tacviewId:X}"},
                { "Coalition", "Neutral" },
                { "Color", "Green" },
                { "Type","Ground+Static+Building"},
                { "Debug", lastState.ToString()}
            };
            UpdatePose();
            UpdateState();
            Plugin.recorderMono.GetComponent<Recorder_mono>().invokeWriterUpdate(this);
            props = [];
            this.enabled = true;
            base.enabled = true;
        }
    }
}
