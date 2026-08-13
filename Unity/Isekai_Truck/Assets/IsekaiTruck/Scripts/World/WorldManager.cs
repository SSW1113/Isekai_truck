using System.Collections.Generic;
using IsekaiTruck.Config;
using UnityEngine;

namespace IsekaiTruck.World
{
    [DisallowMultipleComponent]
    public sealed class WorldManager : MonoBehaviour
    {
        private readonly List<GameObject> groundTiles = new List<GameObject>();

        private GameConfig.WorldSettings settings;
        private Transform player;
        private UnityEngine.Camera targetCamera;
        private Mesh groundMesh;
        private Material groundMaterial;
        private Texture2D groundTexture;
        private int currentTileX;
        private int currentTileZ;
        private int currentTileRadius = -1;

        public void Initialize(GameConfig gameConfig, Transform playerTransform, UnityEngine.Camera worldCamera)
        {
            settings = gameConfig.World;
            player = playerTransform;
            targetCamera = worldCamera;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = settings.FogColor;
            RenderSettings.fogStartDistance = settings.BaseFogNear;
            RenderSettings.fogEndDistance = settings.BaseFogFar;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Color.white * 0.4f;

            CreateGroundResources();
            UpdateGround(currentTileX, currentTileZ, settings.BaseTileRadius);
        }

        public void UpdateWorld(float zoomMultiplier)
        {
            float fogMultiplier = 1f + (zoomMultiplier - 1f) * settings.FogScaleStrength;
            RenderSettings.fogStartDistance = settings.BaseFogNear * fogMultiplier;
            RenderSettings.fogEndDistance = settings.BaseFogFar * fogMultiplier;

            int newTileX = RoundLikeJavaScript(player.position.x / settings.TileSize);
            int newTileZ = RoundLikeJavaScript(player.position.z / settings.TileSize);

            float cameraDistanceX = targetCamera.transform.position.x - player.position.x;
            float cameraDistanceZ = targetCamera.transform.position.z - player.position.z;
            float cameraDistance = Mathf.Sqrt(cameraDistanceX * cameraDistanceX + cameraDistanceZ * cameraDistanceZ);
            float requiredDistance = RenderSettings.fogEndDistance + cameraDistance;
            int requiredRadius = Mathf.CeilToInt((requiredDistance - settings.TileSize / 2f) / settings.TileSize);

            requiredRadius = Mathf.Max(requiredRadius, settings.BaseTileRadius);
            requiredRadius = Mathf.Min(requiredRadius, settings.MaxTileRadius);

            if (newTileX == currentTileX && newTileZ == currentTileZ && requiredRadius == currentTileRadius) return;

            currentTileX = newTileX;
            currentTileZ = newTileZ;
            currentTileRadius = requiredRadius;

            UpdateGround(currentTileX, currentTileZ, currentTileRadius);
        }

        private void CreateGroundResources()
        {
            float halfSize = settings.TileSize / 2f;

            groundMesh = new Mesh { name = "Ground Tile Mesh" };
            groundMesh.vertices = new[]
            {
                new Vector3(-halfSize, 0f, -halfSize),
                new Vector3(-halfSize, 0f, halfSize),
                new Vector3(halfSize, 0f, halfSize),
                new Vector3(halfSize, 0f, -halfSize)
            };
            groundMesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            groundMesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 0f)
            };
            groundMesh.RecalculateNormals();
            groundMesh.RecalculateBounds();

            groundTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                name = "Ground Pattern Texture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat
            };
            groundTexture.SetPixels(new[]
            {
                settings.GroundColor,
                settings.GroundPatternColor,
                settings.GroundPatternColor,
                settings.GroundColor
            });
            groundTexture.Apply(false, false);

            Shader groundShader = Shader.Find("Universal Render Pipeline/Lit");
            if (groundShader == null)
            {
                groundShader = Shader.Find("Standard");
            }

            groundMaterial = new Material(groundShader)
            {
                name = "Ground Material",
                color = Color.white,
                mainTexture = groundTexture
            };

            float patternSize = Mathf.Max(settings.GroundPatternSize, 0.1f);
            float patternRepeat = Mathf.Max(1f, Mathf.Round(settings.TileSize / (patternSize * 2f)));
            groundMaterial.mainTextureScale = Vector2.one * patternRepeat;
        }

        private void EnsureTileCount(int count)
        {
            while (groundTiles.Count < count)
            {
                GameObject tile = new GameObject($"Ground Tile {groundTiles.Count}");
                tile.transform.SetParent(transform, false);

                MeshFilter meshFilter = tile.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = groundMesh;

                MeshRenderer meshRenderer = tile.AddComponent<MeshRenderer>();
                meshRenderer.sharedMaterial = groundMaterial;

                groundTiles.Add(tile);
            }
        }

        private void UpdateGround(int tileX, int tileZ, int radius)
        {
            int sideLength = radius * 2 + 1;
            int requiredCount = sideLength * sideLength;
            EnsureTileCount(requiredCount);

            int index = 0;

            for (int x = -radius; x <= radius; x++)
            {
                for (int z = -radius; z <= radius; z++)
                {
                    GameObject tile = groundTiles[index++];
                    tile.SetActive(true);
                    tile.transform.position = new Vector3((tileX + x) * settings.TileSize, 0f, (tileZ + z) * settings.TileSize);
                }
            }

            for (int i = index; i < groundTiles.Count; i++)
            {
                groundTiles[i].SetActive(false);
            }
        }

        private static int RoundLikeJavaScript(float value)
        {
            return Mathf.FloorToInt(value + 0.5f);
        }

        private void OnDestroy()
        {
            if (groundMesh != null) DestroyRuntimeObject(groundMesh);
            if (groundMaterial != null) DestroyRuntimeObject(groundMaterial);
            if (groundTexture != null) DestroyRuntimeObject(groundTexture);
        }

        private static void DestroyRuntimeObject(Object target)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(target);
                return;
            }
#endif
            Destroy(target);
        }
    }
}
