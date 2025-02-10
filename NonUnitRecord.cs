using UnityEngine;

namespace NOBlackBox
{
    public struct NonUnitRecord
    {
        public int id { get; set; }
        public string typeName;
        public Vector3 pos;

        public NonUnitRecord(int _id, string _typeName, Vector3 _pos)
        {
            this.id = _id;
            this.typeName = _typeName;
            this.pos = _pos;
        }
    }
}
