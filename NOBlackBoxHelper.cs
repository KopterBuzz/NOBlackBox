using UnityEngine;


namespace NOBlackBox
{
    internal static class NOBlackBoxHelper
    {
        private const float minLatitude = -90.0f;
        private const float maxLatitude = 90.0f;
        private const float minLongitude = -180.0f;
        private const float maxLongitude = 180.0f;

        private const float earthCircumference = 40075000f; // 40,075 kilometers
        private const float poleToPoleDistance = 20037500f; // 20,037.5 kilometers

        public static float[] ConvertUnityToLatLong(float x, float y, float z)
        {
            // Normalize the Unity coordinates to the range [0, 1]
            float normalizedX = (x + earthCircumference / 2) / earthCircumference;
            float normalizedY = (z + poleToPoleDistance / 2) / poleToPoleDistance;

            // Convert the normalized coordinates to latitude and longitude
            float latitude = Mathf.Lerp(minLatitude, maxLatitude, normalizedY);
            float longitude = Mathf.Lerp(minLongitude, maxLongitude, normalizedX);

            float[] output = new float[2];
            output[0] = latitude;
            output[1] = longitude;
            return output;
        }
        public static string TimeOfDay(float time)
        {
            int hourInt = Mathf.FloorToInt(LevelInfo.i.timeOfDay);
            string hourString = hourInt.ToString("F0");
            if (hourInt < 10)
            {
                hourString = "0" + hourString;
            }
            float minutesSeconds = (LevelInfo.i.timeOfDay - (float)hourInt) * 60f;
            int minutesInt = Mathf.FloorToInt(minutesSeconds);
            string minutesString = minutesInt.ToString("F0");
            if (minutesInt < 10)
            {
                minutesString = "0" + minutesString;
            }
            int secondsInt = Mathf.FloorToInt((minutesSeconds - (float)minutesInt) * 60f);
            string secondsString = secondsInt.ToString("F0");
            if (secondsInt < 10)
            {
                secondsString = "0" + secondsString;
            }
            return hourString+":"+minutesString+":"+secondsString;
        }
    }
}
