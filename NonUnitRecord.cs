using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace NOBlackBox
{
    public struct NonUnitRecord
    {
        public int id;
        public string typeName;
        public Vector3 pos;

        public NonUnitRecord(int _id, string _typeName, Vector3 _pos)
        {
            id = _id;
            typeName = _typeName;
            pos = _pos;
        }
    }
}
