using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace NOBlackBox
{
    internal class ACMITracer(BulletSim sim, BulletSim.Bullet bullet) : ACMINotUnit
    {
        private Vector3 lastPos = new(float.NaN, float.NaN, float.NaN);

        public readonly BulletSim sim = sim;
        public readonly BulletSim.Bullet bullet = bullet;

        public override Dictionary<string, string> Init()
        {
            return new()
            {
                { "Type", "Misc+Projectile+Shell" }
            };
        }
        public override Dictionary<string, string> Update()
        {
            Dictionary<string, string> props = [];

            float fx = MathF.Round(bullet.position.x, 2);
            float fy = MathF.Round(bullet.position.y, 2);
            float fz = MathF.Round(bullet.position.z, 2);

            Vector3 newPos = new(fx, fy, fz);

            if (newPos != lastPos)
            {
                props.Add("T", UpdatePosition(newPos).ToString(CultureInfo.InvariantCulture));

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

            return $"{(newPos.x != lastPos.x ? longitude.ToString(CultureInfo.InvariantCulture) : string.Empty)}|{(newPos.z != lastPos.z ? latitude.ToString(CultureInfo.InvariantCulture) : string.Empty)}|{y}|{x}|{z}";
        }
    }
}
