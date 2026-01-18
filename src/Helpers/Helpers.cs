using System;
using System.Collections.Generic;
using System.IO;

namespace NOBlackBox
{
    public static class Helpers
    {
        public static (bool isFolder, bool success) IsFileOrFolder(string path)
        {
            try
            {
                var attr = File.GetAttributes(path);
                return attr.HasFlag(FileAttributes.Directory) ? (true, true)! : (false, true)!;
            }
            catch (FileNotFoundException)
            {
                return (false, false);
            }
        }

        public static (float, float) CartesianToGeodetic(float U /* X */, float V /* Z */)
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
