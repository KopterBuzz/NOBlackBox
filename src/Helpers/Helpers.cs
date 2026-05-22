using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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

        internal static ulong ComputeTacviewPasswordCrc64(string input)
        {
            const ulong poly = 0x42F0E1EBA9EA3693UL;
            byte[] bytes = Encoding.Unicode.GetBytes(input);
            ulong crc = 0xFFFFFFFFFFFFFFFFUL;

            foreach (byte b in bytes)
            {
                crc ^= (ulong)b << 56;
                for (int i = 0; i < 8; i++)
                {
                    crc = (crc & 0x8000000000000000UL) != 0
                        ? (crc << 1) ^ poly
                        : crc << 1;
                }
            }

            return crc ^ 0xFFFFFFFFFFFFFFFFUL;
        }

        internal static string ComputePasswordHash(string? password)
        {
            if (string.IsNullOrEmpty(password))
                return "0";
            return ComputeTacviewPasswordCrc64(password).ToString("x16");
        }
    }
}
