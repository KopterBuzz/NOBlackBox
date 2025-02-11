using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace NOBlackBox
{
    public class Trackable
    {
        internal Dictionary<string, string> TranslatedInstanceNames = new Dictionary<string, string>()
        {
            ["tracer(Clone)"] = "Bullet",
            ["IRFlare(Clone)"] = "Flare"
        };
        internal int id { get; set; }
        internal Vector3 pos { get; set; }

        public Trackable(GameObject obj)
        {
            this.id = Mathf.Abs(obj.GetInstanceID());
            this.pos = obj.transform.GlobalPosition().AsVector3();
        }
    }
}
