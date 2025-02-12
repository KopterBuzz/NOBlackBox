using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace NOBlackBox
{
    internal class ACMITracer: ACMIObject
    {
        private static int BULLETID = 0;

        private Vector3 lastPos = new(float.NaN, float.NaN, float.NaN);

        public readonly BulletSim sim;
        public readonly BulletSim.Bullet bullet;

        public ACMITracer(BulletSim sim, BulletSim.Bullet bullet): base((long)(Interlocked.Increment(ref BULLETID) - 1) | (1L << 33))
        {
            this.sim = sim;
            this.bullet = bullet;
        }

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
