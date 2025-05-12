using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace NOBlackBox
{
    internal class ComputeShaderHeightmapGenerator : MonoBehaviour
    {
        [Header("Tile Mesh Settings")]
        public GameObject tileMeshPrefab; // Prefab with MeshRenderer and MeshFilter
        public Material heightEncodeMaterial; // Material using the HeightEncode shader
        public float minHeight = 0f;
        public float maxHeight = 10f;

        [Header("Camera Settings")]
        public Vector3 cameraOffset = new Vector3(0, 10, 0);
        public float orthographicSize = 5f;
        public LayerMask renderLayer;

        [Header("Output")]
        public int resolution = 512;
        public RenderTexture heightmapTexture;

        private Camera renderCam;

        void Start()
        {
            SetupRenderTexture();
            SetupCamera();
            GenerateHeightmap();
        }

        void SetupRenderTexture()
        {
            if (heightmapTexture != null)
            {
                heightmapTexture.Release();
            }

            heightmapTexture = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.RFloat);
            heightmapTexture.enableRandomWrite = true;
            heightmapTexture.Create();
        }

        void SetupCamera()
        {
            GameObject camGO = new GameObject("HeightmapCamera");
            camGO.hideFlags = HideFlags.HideAndDontSave;
            renderCam = camGO.AddComponent<Camera>();
            renderCam.orthographic = true;
            renderCam.orthographicSize = orthographicSize;
            renderCam.clearFlags = CameraClearFlags.SolidColor;
            renderCam.backgroundColor = Color.black;
            renderCam.cullingMask = renderLayer;
            renderCam.targetTexture = heightmapTexture;
            renderCam.allowHDR = false;
            renderCam.enabled = false;
        }

        public void GenerateHeightmap()
        {
            if (tileMeshPrefab == null || heightEncodeMaterial == null)
            {
                Debug.LogError("Missing required references.");
                return;
            }

            // Instantiate a temporary mesh instance
            GameObject tempTile = Instantiate(tileMeshPrefab);
            tempTile.layer = Mathf.RoundToInt(Mathf.Log(renderLayer.value, 2)); // assign layer

            // Assign height encode material
            MeshRenderer mr = tempTile.GetComponent<MeshRenderer>();
            if (mr == null)
            {
                Debug.LogError("Prefab must have a MeshRenderer.");
                DestroyImmediate(tempTile);
                return;
            }

            Material matInstance = new Material(heightEncodeMaterial);
            matInstance.SetFloat("_MinHeight", minHeight);
            matInstance.SetFloat("_MaxHeight", maxHeight);
            mr.material = matInstance;

            // Position camera above tile
            Bounds bounds = mr.bounds;
            Vector3 center = bounds.center;
            renderCam.transform.position = center + cameraOffset;
            renderCam.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // look down

            // Render to texture
            renderCam.Render();

            // Cleanup
            DestroyImmediate(tempTile);
        }

        // Optional: Save heightmap as PNG (debugging)
        [ContextMenu("Save Heightmap PNG")]
        public void SaveHeightmapAsPNG()
        {
            Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RFloat, false);
            RenderTexture.active = heightmapTexture;
            tex.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
            tex.Apply();
            RenderTexture.active = null;

            byte[] pngData = tex.EncodeToPNG();
            System.IO.File.WriteAllBytes(Application.dataPath + "/HeightmapOutput.png", pngData);
            Debug.Log("Saved heightmap to HeightmapOutput.png");
            DestroyImmediate(tex);
        }
    }
}
