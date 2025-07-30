using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading;
using UnityEngine;

namespace NOBlackBox
{
    internal class ACMIShockwave : ACMIObject
    {
        private static int SHOCKWAVEID = 0;

        private static readonly FieldInfo propagation = typeof(Shockwave).GetField("blastPropagation", BindingFlags.NonPublic | BindingFlags.Instance);

        public readonly Shockwave shockwave;
        public ACMIShockwave(Shockwave shockwave) : base((long)(Interlocked.Increment(ref SHOCKWAVEID) - 1) | (1L << 34))
        {
            this.shockwave = shockwave;
        }

        public override Dictionary<string, string> Init()
        {
            Vector3 pos = shockwave.transform.position;
            (float lat, float lon) = CartesianToGeodetic(pos.GlobalX(), pos.GlobalZ());

            return new()
            {
                { "T", $"{
                    lon.ToString(CultureInfo.InvariantCulture)
                }|{
                    lat.ToString(CultureInfo.InvariantCulture)
                }|{
                    pos.GlobalY().ToString("0.##", CultureInfo.InvariantCulture)
                }|{
                    pos.GlobalX().ToString("0.##", CultureInfo.InvariantCulture)
                }|{
                    pos.GlobalZ().ToString("0.##", CultureInfo.InvariantCulture)
                }" },
                { "Type", "Misc+Explosion" }
            };
        }

        public override Dictionary<string, string> Update()
        {
            
            return new()
            {
                { "Radius", ((float)propagation.GetValue(shockwave)).ToString("0.##", CultureInfo.InvariantCulture) }
            };
        }
    }
}
