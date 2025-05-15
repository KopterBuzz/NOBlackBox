using Unity.Jobs;
using Unity.Collections;
using UnityEngine;
using System;
using System.Reflection;
using System.Linq;
using System.IO;

namespace NOBlackBox.src.HeightMapGenerator
{
    internal class RaycasterV2: MonoBehaviour
    {
        private static readonly int STATICS = LayerMask.NameToLayer("Statics");
        private const short SCALE = 1028; // 65535 * 4 / 255

        private readonly static FieldInfo MapInScene = 
            typeof(MapSettingsManager).GetField("mapInScene", BindingFlags.Instance | BindingFlags.NonPublic);

        internal event Action<byte[]>? OnFinished;

        private RayJob? job;
        private JobHandle? handle;

        private void Update()
        {
            if (this.job == null)
                return;

            RayJob job = this.job!.Value;

            if (handle == null)
                handle = job.Schedule(job.heightMap.Length, 100);
            else if (handle?.IsCompleted == true)
            {
                JobHandle handle = this.handle.Value;

                int width = job.ArrayWidth;

                handle.Complete();

                byte[] bytes = new byte[job.heightMap.Length * 2];
                MemoryStream str = new(bytes, true);
                BinaryWriter writer = new(str);

                foreach (short s in job.heightMap)
                    writer.Write(s);

                writer.Close();
                str.Close();

                job.heightMap.Dispose();

                OnFinished?.Invoke(bytes);

                Destroy(gameObject);
            }
        }

        private struct RayJob: IJobParallelFor
        {
            internal float width;
            internal float height;

            internal float resolution;

            internal float maxHeight;

            internal NativeArray<short> heightMap;

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

                    float scaled = hit.point.GlobalY() * SCALE;

                    heightMap[index] = (short)Math.Floor(scaled);
                }
                else
                    heightMap[index] = 0;
            }
        }

        internal static RaycasterV2 ScanMap()
        {
            float resolution = 4f;

            int size = (int)Math.Ceiling((TerrainGrid.terrainSize.y / resolution) * (TerrainGrid.terrainSize.x / resolution));
            /*while (size * 2 > (1 << 26))
            {
                resolution *= 2f;
                size = (int)Math.Ceiling((TerrainGrid.terrainSize.y / resolution) * (TerrainGrid.terrainSize.x / resolution));
            }*/

            int width = (int)Math.Ceiling(TerrainGrid.terrainSize.x / resolution);
            int height = (int)Math.Ceiling(TerrainGrid.terrainSize.y / resolution);

            MapSettings map = (MapSettings)MapInScene.GetValue(MapSettingsManager.i);
            GameObject mapHost = map.gameObject;

            float maxHeight =
                mapHost.GetComponentsInChildren<MeshCollider>()
                .Where(collider => collider.gameObject.layer == STATICS && collider.gameObject.GetComponents<Component>().Length == 4)
                .Select(collider => collider.bounds.max.y)
                .Max() - Datum.originPosition.y;

            Plugin.Logger!.LogInfo($"MaxHeight: {maxHeight}");

            RayJob job = new()
            {
                width = width,
                height = height,

                resolution = resolution,
                maxHeight = maxHeight,

                heightMap = new(size, Allocator.TempJob)
            };

            GameObject host = new("Raycaster");
            RaycasterV2 comp = host.AddComponent<RaycasterV2>();

            comp.job = job;

            return comp;
        }
    }
}
