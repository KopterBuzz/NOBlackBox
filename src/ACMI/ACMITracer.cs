using System;
using System.Collections.Generic;
using UnityEngine;

namespace NOBlackBox
{
    internal class ACMITracer(Tracer tracer) : ACMIObject(tracer.id)
    {
        private Vector3 lastPos = new(float.NaN, float.NaN, float.NaN);

        public override Dictionary<string, string> Init()
        {
            return new()
            {
                { "Type", "Projectile+Bullet" }
            };
        }
        public override Dictionary<string, string> Update()
        {
            Dictionary<string, string> props = [];

            float fx = MathF.Round(tracer.pos.x, 2);
            float fy = MathF.Round(tracer.pos.y, 2);
            float fz = MathF.Round(tracer.pos.z, 2);

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
            string x = Mathf.Approximately(newPos.x, lastPos.x) ? "" : newPos.x.ToString("0.##");
            string y = Mathf.Approximately(newPos.y, lastPos.y) ? "" : newPos.y.ToString("0.##");
            string z = Mathf.Approximately(newPos.z, lastPos.z) ? "" : newPos.z.ToString("0.##");

            (float latitude, float longitude) = CartesianToGeodetic(newPos.x, newPos.z);

            return $"{(newPos.x != lastPos.x ? longitude : string.Empty)}|{(newPos.z != lastPos.z ? latitude : string.Empty)}|{y}|{x}|{z}";
        }
    }
}
