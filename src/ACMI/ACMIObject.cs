using System;
using System.Collections.Generic;
using System.Globalization;

namespace NOBlackBox
{
    abstract public class ACMIObject(long id)
    {
        public readonly long id = id;

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
