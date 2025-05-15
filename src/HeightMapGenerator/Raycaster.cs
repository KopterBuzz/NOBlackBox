using Unity.Jobs;
using Unity.Collections;
using UnityEngine;
using System;
using System.Reflection;
using System.Linq;

namespace NOBlackBox.src.HeightMapGenerator
{
    internal class Raycaster: MonoBehaviour
    {
        private static readonly int STATICS = LayerMask.NameToLayer("Statics");

        private readonly static FieldInfo MapInScene = 
            typeof(MapSettingsManager).GetField("mapInScene", BindingFlags.Instance | BindingFlags.NonPublic);

        internal event Action<Texture2D>? OnFinished;

        private RayJob? job;
        private JobHandle? handle;
        private Texture2D? map;

        private void Update()
        {
            if (this.job == null)
                return;

            RayJob job = this.job!.Value;

            if (handle == null)
            {
                map = new(job.ArrayWidth, job.ArrayHeight, TextureFormat.RHalf, false);

                job.heightMap = map.GetPixelData<Color>(0);

                job.Run(job.heightMap.Length);

                handle = job.Schedule(job.heightMap.Length, 100);
            }
            else if (handle?.IsCompleted == true)
            {
                JobHandle handle = this.handle.Value;

                int width = job.ArrayWidth;

                handle.Complete();

                /*
                NativeArray<Color32> pixels = map!.GetRawTextureData<Color32>();

                int max = 0;
                foreach (var pixel in pixels)
                {
                    if (pixel.a > max)
                        max = pixel.a;
                }

                float scale = 255 / max;
                for (int i = 0; i < pixels.Length; i++)
                {
                    byte value = (byte)Math.Ceiling(pixels[i].a * scale);
                    pixels[i] = new(value, value, value, 255);
                }
                */

                map!.Apply(false);
                OnFinished?.Invoke(map!);

                Destroy(gameObject);
            }
        }

        private struct RayJob: IJobParallelFor
        {
            internal float width;
            internal float height;

            internal float resolution;

            internal float maxHeight;

            internal NativeArray<Color> heightMap;

            internal readonly int ArrayWidth
            {
                get
                {
                    return (int)Math.Ceiling(width / resolution);
                }
            }

            internal readonly int ArrayHeight
            {
                get
                {
                    return (int)Math.Ceiling(height / resolution);
                }
            }

            public void Execute(int index)
            {
                float indexMeters = index * resolution;

                float x0 = indexMeters % width;
                float y0 = (indexMeters - x0) / width;

                float x = x0 - (width / 2);
                float y = y0 - (height / 2);

                Vector3 target = new GlobalPosition(x, maxHeight + 1, y).ToLocalPosition();

                if (Physics.Raycast(target, Vector3.down, out RaycastHit hit)) //, maxHeight + 2, 1 << STATICS))
                {
                    Plugin.Logger!.LogInfo($"HIT: {hit.point.ToString()}");

                    float relative = (maxHeight + 1) / hit.point.GlobalY();

                    heightMap[index] = new(relative, relative, relative);
                }
            }
        }

        internal static Raycaster ScanMap()
        {
            float resolution = 4f;

            int size = (int)Math.Ceiling((TerrainGrid.terrainSize.y / resolution) * (TerrainGrid.terrainSize.x / resolution));
            while (size > (1 << 26))
            {
                resolution *= 2f;
                size = (int)Math.Ceiling((TerrainGrid.terrainSize.y / resolution) * (TerrainGrid.terrainSize.x / resolution));
            }

            int width = (int)Math.Ceiling(TerrainGrid.terrainSize.x / resolution);
            int height = (int)Math.Ceiling(TerrainGrid.terrainSize.y / resolution);

            MapSettings map = (MapSettings)MapInScene.GetValue(MapSettingsManager.i);
            GameObject mapHost = map.gameObject;

            float maxHeight =
                mapHost.GetComponentsInChildren<MeshCollider>()
                .Where(collider => collider.gameObject.layer == STATICS && collider.gameObject.GetComponents<Component>().Length == 4)
                .Select(collider => collider.bounds.max.y)
                .Max() - Datum.LocalSeaY;

            Plugin.Logger!.LogInfo($"MaxHeight: {maxHeight}");

            RayJob job = new()
            {
                width = width,
                height = height,

                resolution = resolution,
                maxHeight = maxHeight
            };

            GameObject host = new("Raycaster");
            Raycaster comp = host.AddComponent<Raycaster>();

            comp.job = job;

            return comp;
        }
    }
}
