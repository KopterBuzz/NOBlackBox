using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using UnityEngine;

namespace NOBlackBox
{
    internal class ACMITracer_mono : ACMIObject_mono
    {
        private static int BULLETID = 0;

        public BulletSim sim;
        public BulletSim.Bullet bullet;

        private Vector3 lastPos = new(float.NaN, float.NaN, float.NaN);

        public virtual void Init(BulletSim sim, BulletSim.Bullet bullet)
        {
            this.sim = sim;
            this.bullet = bullet;
            base.unitId = (long)(Interlocked.Increment(ref BULLETID) - 1) | (1L << 33);
            base.tacviewId = base.unitId;
            props = new Dictionary<string, string>()
            {
                { "Type", "Misc+Projectile+Shell" }
            };

            Plugin.recorderMono.GetComponent<Recorder_mono>().invokeWriterUpdate(this);
        }

        public override void Update()
        {
            timer += Time.deltaTime;
            if (timer < Plugin.tracerUpdateDelta)
            {
                return;
            }
            UpdatePose();
            Plugin.recorderMono.GetComponent<Recorder_mono>().invokeWriterUpdate(this);
            props = [];
        }

        public override void LateUpdate()
        {
            if (bullet.tracer == null || !bullet.tracer.activeSelf)
            {
                this.enabled = false;
                base.enabled = false;
                Plugin.recorderMono.GetComponent<Recorder_mono>().invokeWriterRemove(this);
                Plugin.Logger?.LogDebug($"DISABLING TRACER {unitId.ToString(CultureInfo.InvariantCulture)}");
                props = [];
                GameObject.Destroy(this);
            }
        }

        public void UpdatePose()
        {

            float fx = MathF.Round(bullet.position.x, 2);
            float fy = MathF.Round(bullet.position.y, 2);
            float fz = MathF.Round(bullet.position.z, 2);

            Vector3 newPos = new(fx, fy, fz);

            if (newPos != lastPos)
            {
                props.Add("T", UpdatePosition(newPos).ToString(CultureInfo.InvariantCulture));

                lastPos = newPos;
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
    }
}
