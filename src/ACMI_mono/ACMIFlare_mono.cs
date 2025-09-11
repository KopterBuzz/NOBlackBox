using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using UnityEngine;
using static BulletSim;

namespace NOBlackBox
{
    internal class ACMIFlare_mono : ACMIObject_mono
    {
        private static int FLAREID = 0;

        private Vector3 lastPos = new(float.NaN, float.NaN, float.NaN);

        public IRFlare flare;

        public virtual void Init(IRSource source)
        {
            this.flare = source.transform.gameObject.GetComponent<IRFlare>();
            base.unitId = (long)(Interlocked.Increment(ref FLAREID) - 1) | (1L << 32);
            base.tacviewId = base.unitId;
            props = new Dictionary<string, string>()
            {
                {"Type","Misc+Decoy+Flare" }
            };
            Plugin.recorderMono.GetComponent<Recorder_mono>().invokeWriterUpdate(this);
            props = [];
        }

        public override void Update()
        {
            timer += Time.deltaTime;
            if (timer < Plugin.flareUpdateDelta)
            {
                return;
            }
            UpdatePose();
            Plugin.recorderMono.GetComponent<Recorder_mono>().invokeWriterUpdate(this);
            props = [];
            timer = 0;
        }

        public virtual void LateUpdate()
        {
            if  (flare == null || !flare.enabled || null == flare.transform.position)
            {
                DisableFlare();
            }
        }

        public void UpdatePose()
        {
            try
            {
                float fx = MathF.Round(flare.transform.position.GlobalX(), 2);
                float fy = MathF.Round(flare.transform.position.GlobalY(), 2);
                float fz = MathF.Round(flare.transform.position.GlobalZ(), 2);

                Vector3 newPos = new(fx, fy, fz);

                if (newPos != lastPos)
                {
                    props.Add("T", UpdatePosition(newPos).ToString(CultureInfo.InvariantCulture));

                    lastPos = newPos;
                }
            } catch
            {
                DisableFlare();
            }

        }

        private string UpdatePosition(Vector3 newPos)
        {
            string x = Mathf.Approximately(newPos.x, lastPos.x) ? "" : newPos.x.ToString(CultureInfo.InvariantCulture);
            string y = Mathf.Approximately(newPos.y, lastPos.y) ? "" : newPos.y.ToString(CultureInfo.InvariantCulture);
            string z = Mathf.Approximately(newPos.z, lastPos.z) ? "" : newPos.z.ToString(CultureInfo.InvariantCulture);

            (float latitude, float longitude) = Helpers.CartesianToGeodetic(newPos.x, newPos.z);

            return $"{(newPos.x != lastPos.x ? longitude.ToString(CultureInfo.InvariantCulture) : string.Empty)}|{(newPos.z != lastPos.z ? latitude.ToString(CultureInfo.InvariantCulture) : string.Empty)}|{y}|{x}|{z}";
        }

        private void DisableFlare()
        {
            this.enabled = false;
            base.enabled = false;
            Plugin.recorderMono.GetComponent<Recorder_mono>().invokeWriterRemove(this);
            Plugin.Logger?.LogDebug($"DISABLING FLARE {unitId.ToString(CultureInfo.InvariantCulture)}");
            props = [];
            GameObject.Destroy(this);
        }
    }
}
