using System;
using System.Collections.Generic;
using System.Threading;

namespace NOBlackBox
{
    abstract public class ACMIObject(long id)
    {
        private static long COMPRESSED_ID = 0;

        public readonly long id = Configuration.CompressIDs ? Interlocked.Increment(ref COMPRESSED_ID) - 1 : id;

        public event Action<string, long[], string>? OnEvent;

        public abstract Dictionary<string, string> Init();

        public abstract Dictionary<string, string> Update();

        protected void FireEvent(string key, long[] ids, string text)
        {
            OnEvent?.Invoke(key, ids, text);
        }
        public (float, float) CartesianToGeodetic(float U /* X */, float V /* Z */)
        {
            //Stupid simplification but it works.
            float longArc = (float)Math.PI * 6378137;
            float latArc = longArc / 2;

            float latitude = V * 90 / latArc;
            float longitude = U * 180 / longArc;

            return (latitude, longitude);
        }
    }
}
