using UnityEngine;

namespace NOBlackBox
{
    public class Tracer : Trackable
    {
        internal string typeName { get; set; }
        public Tracer(GameObject obj): base(obj)
        {
            this.typeName = TranslatedInstanceNames[obj.name];
        }
    }
}
