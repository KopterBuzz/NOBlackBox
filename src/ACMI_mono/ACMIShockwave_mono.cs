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
        private static readonly FieldInfo blastRadiusField = typeof(Shockwave).GetField("blastRadius", BindingFlags.NonPublic | BindingFlags.Instance);

        private float blastRadius = 0f;

        public Shockwave shockwave;
        private float lastRadius = 0f;
        private float waveSpeed = 0f;

        private float shockWaveTimer = 0f;

        public virtual void Init(Shockwave shockwave)
        {
            Vector3 pos = shockwave.transform.position;
            this.shockwave = shockwave;
            blastRadius = (float)blastRadiusField.GetValue(shockwave);
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
                shockWaveTimer += Time.deltaTime;
                timer += Time.deltaTime;
                if (timer < Configuration.shockwaveUpdateDelta.Value)
                {
                    return;
                }
                float radius = MathF.Round(((float)propagation.GetValue(shockwave)), 2);
                waveSpeed = (radius - lastRadius) / timer;
                
                if (radius == lastRadius || !shockwave.enabled || !shockwave || waveSpeed <= 0f || radius >= blastRadius || (blastRadius <= 1000 && shockWaveTimer > 3f))
                {
                    DisableShockWave();
                }
                props.Add("Radius", radius.ToString("0.##", CultureInfo.InvariantCulture));
                Plugin.recorderMono.GetComponent<Recorder_mono>().invokeWriterUpdate(this);
                props = [];
                timer = 0f;
                lastRadius = radius;
                Plugin.Logger?.LogDebug($"Shockwave {SHOCKWAVEID.ToString(CultureInfo.InvariantCulture)}Blast Radius: {blastRadius.ToString(CultureInfo.InvariantCulture)}, Current Radius: {radius.ToString(CultureInfo.InvariantCulture)}, Speed: {waveSpeed.ToString(CultureInfo.InvariantCulture)}");
            } catch
            {
                DisableShockWave();
            }

        }

        private void DisableShockWave()
        {
            Plugin.recorderMono.GetComponent<Recorder_mono>().invokeWriterRemove(this);
            Plugin.Logger?.LogDebug($"DISABLING SHOCKWAVE {unitId.ToString(CultureInfo.InvariantCulture)}");
            this.enabled = false;
            base.enabled = false;
            props = [];
            GameObject.Destroy(this);
        }
    }
}
