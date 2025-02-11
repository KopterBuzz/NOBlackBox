using System;
using System.Collections.Generic;
using UnityEngine;

namespace NOBlackBox
{
    using ACMITransform = (double, double);

    public class ACMIUnit(Unit unit): ACMIObject(unit.persistentID)
    {
        private Vector3 lastPos = new(float.NaN, float.NaN, float.NaN);
        private Vector3 lastRot = new(float.NaN, float.NaN, float.NaN);
        public readonly Unit unit = unit;

        public override Dictionary<string, string> Init()
        {
            Faction? faction = unit.NetworkHQ?.faction;

            return new()
            {
                { "Name", unit.definition.code },
                { "Coalition", faction?.factionName ?? "Neutral" },
                { "Color", faction == null ? "Green" : (faction.factionName == "Boscali" ? "Blue" : "Red") }
            };
        }

        public override Dictionary<string, string> Update()
        {
            Dictionary<string, string> props = [];

            float fx = MathF.Round(unit.transform.position.GlobalX(), 2);
            float fy = MathF.Round(unit.transform.position.GlobalY(), 2);
            float fz = MathF.Round(unit.transform.position.GlobalZ(), 2);

            float fax = MathF.Round(unit.transform.eulerAngles.x, 2);
            float fay = MathF.Round(unit.transform.eulerAngles.y, 2);
            float faz = MathF.Round(unit.transform.eulerAngles.z, 2);

            Vector3 newPos = new(fx, fy, fz);
            Vector3 newRot = new(fax, fay, faz);

            if (newPos != lastPos || newRot != lastRot)
            {
                props.Add("T", UpdatePosition(newPos, newRot));

                lastPos = newPos;
                lastRot = newRot;
            }

            return props;
        }
        private string UpdatePosition(Vector3 newPos, Vector3 newRot)
        {
            string x = Mathf.Approximately(newPos.x, lastPos.x) ? "" : newPos.x.ToString();
            string y = Mathf.Approximately(newPos.y, lastPos.y) ? "" : newPos.y.ToString();
            string z = Mathf.Approximately(newPos.z, lastPos.z) ? "" : newPos.z.ToString();

            float adjusted_roll = newRot.z > 180.0f ? 360 - newRot.z : -newRot.z;
            float adjusted_pitch = newRot.x > 180.0f ? 360 - newRot.x : -newRot.x;
            float adjusted_yaw = newRot.y;

            string roll = Mathf.Approximately(newRot.z, lastRot.z) ? "" : adjusted_roll.ToString();
            string pitch = Mathf.Approximately(newRot.x, lastRot.x) ? "" : adjusted_pitch.ToString();
            string yaw = Mathf.Approximately(newRot.y, lastRot.y) ? "" : adjusted_yaw.ToString();

            /*ACMITransform lat_long = ConvertTransform(lastPos);
            string lat = z != "" ? lat_long.Item1.ToString() : "";
            string lon = x != "" ? lat_long.Item2.ToString() : "";*/

            (float latitude, float longitude) = CartesianToGeodetic(newPos.x, newPos.z);

            return $"{longitude}|{latitude}|{y}|{roll}|{pitch}|{yaw}|{x}|{z}|{yaw}";
        }

        private ACMITransform ConvertTransform(Vector3 pos)
        {
            double new_latitude = pos.z / 6378 * (180 / Math.PI);
            double new_longitude = pos.x / 6378 * (180 / Math.PI);
            return (new_latitude, new_longitude);
        }
    }
}
