using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using UnityEngine;

namespace NOBlackBox
{ 
    internal class ACMIFlare: ACMIObject
    {
        private static int FLAREID = 0;

        private Vector3 lastPos = new(float.NaN, float.NaN, float.NaN);

        public readonly IRFlare flare;

        public ACMIFlare(IRSource source) : base((long)(Interlocked.Increment(ref FLAREID) - 1) | (1L << 32))
        {
            if (source.flare == false)
                throw new ArgumentException("IRSource is not flare");

            flare = source.transform.gameObject.GetComponent<IRFlare>();
            if (flare == null)
                throw new InvalidOperationException("Failed to acquire IRFlare");
        }
        public override Dictionary<string, string> Init()
        {
            return new()
            {
                { "Type", "Misc+Decoy+Flare" }
            };
        }
        public override Dictionary<string, string> Update()
        {
            Dictionary<string, string> props = [];

            float fx = MathF.Round(flare.transform.position.GlobalX(), 2);
            float fy = MathF.Round(flare.transform.position.GlobalY(), 2);
            float fz = MathF.Round(flare.transform.position.GlobalZ(), 2);

            Vector3 newPos = new(fx, fy, fz);

            if (newPos != lastPos)
            {
                props.Add("T", UpdatePosition(newPos));

                lastPos = newPos;
            }

            return props;
        }
        private string UpdatePosition(Vector3 newPos)
        {
            string x = Mathf.Approximately(newPos.x, lastPos.x) ? "" : newPos.x.ToString(CultureInfo.InvariantCulture);
            string y = Mathf.Approximately(newPos.y, lastPos.y) ? "" : newPos.y.ToString(CultureInfo.InvariantCulture);
            string z = Mathf.Approximately(newPos.z, lastPos.z) ? "" : newPos.z.ToString(CultureInfo.InvariantCulture);

            (float latitude, float longitude) = CartesianToGeodetic(newPos.x, newPos.z);

            return $"{(newPos.x != lastPos.x ? longitude : string.Empty)}|{(newPos.z != lastPos.z ? latitude : string.Empty)}|{y}|{x}|{z}";
        }
    }
}
