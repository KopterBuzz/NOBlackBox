using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Threading;
using UnityEngine;

namespace NOBlackBox
{
    internal class ACMIShockwave_mono : ACMIObject_mono
    {
        private static int SHOCKWAVEID = 0;
        private static readonly FieldInfo propagation = typeof(Shockwave).GetField("blastPropagation", BindingFlags.NonPublic | BindingFlags.Instance);

        public Shockwave shockwave;
        private float lastRadius = 0f;

        public virtual void Init(Shockwave shockwave)
        {
            Vector3 pos = shockwave.transform.position;
            this.shockwave = shockwave;
            base.unitId = (long)(Interlocked.Increment(ref SHOCKWAVEID) - 1) | (1L << 34);
            base.tacviewId = unitId;
            (float lat, float lon) = Helpers.CartesianToGeodetic(pos.GlobalX(), pos.GlobalZ());
            props = new Dictionary<string, string>()
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
            Plugin.recorderMono.GetComponent<Recorder_mono>().invokeWriterUpdate(this);
            props = [];
        }

        public override void Update()
        {
            try
            {
                timer += Time.deltaTime;
                if (timer < Plugin.shockwaveUpdateDelta)
                {
                    return;
                }
                float radius = (float)propagation.GetValue(shockwave) / 10f;
                if (radius <= lastRadius)
                {
                    DisableShockWave();
                }
                props.Add("Radius", radius.ToString("0.##", CultureInfo.InvariantCulture));
                Plugin.recorderMono.GetComponent<Recorder_mono>().invokeWriterUpdate(this);
                props = [];
                timer = 0f;
            } catch
            {
                DisableShockWave();
            }

        }

        private void DisableShockWave()
        {
            this.enabled = false;
            base.enabled = false;
            Plugin.recorderMono.GetComponent<Recorder_mono>().invokeWriterRemove(this);
            Plugin.Logger?.LogDebug($"DISABLING SHOCKWAVE {unitId.ToString(CultureInfo.InvariantCulture)}");
            props = [];
            GameObject.Destroy(this);
        }
    }
}
