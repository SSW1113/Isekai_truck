using System.Collections.Generic;
using IsekaiTruck.Config;
using UnityEngine;

namespace IsekaiTruck.World
{
    [DisallowMultipleComponent]
    public sealed class WorldManager : MonoBehaviour
    {
        private sealed class ActiveChunk
        {
            public ActiveChunk(GameObject instance, int prefabIndex)
            {
                Instance = instance;
                PrefabIndex = prefabIndex;
            }

            public GameObject Instance { get; }
            public int PrefabIndex { get; }
        }

        private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");

        private readonly List<GameObject> groundTiles = new List<GameObject>();
        private readonly Dictionary<Vector2Int, ActiveChunk> activeChunks = new Dictionary<Vector2Int, ActiveChunk>();
        private readonly List<Vector2Int> chunksToRelease = new List<Vector2Int>();
        private readonly List<int> streetPrefabIndices = new List<int>();

        private GameConfig.WorldSettings settings;
        private Transform player;
        private UnityEngine.Camera targetCamera;
        private Mesh groundMesh;
        private Material groundMaterial;
        private Texture2D groundTexture;
        private WorldDefinition currentWorld;
        private ModernCityChunkPrototype[] chunkPrefabs;
        private Stack<GameObject>[] chunkPools;
        private MaterialPropertyBlock chunkGroundPropertyBlock;
        private Color currentGroundColor;
        private int crossroadPrefabIndex = -1;
        private int crossroadInterval = 4;
        private int currentTileX;
        private int currentTileZ;
        private int currentTileRadius = -1;
        private bool isInitialized;
        private bool usesChunkPrefabs;

        public WorldDefinition CurrentWorld => currentWorld;
        public int ActiveTileCount => usesChunkPrefabs ? activeChunks.Count : CountActiveGroundTiles();
        public bool UsesChunkPrefabs => usesChunkPrefabs;

        public void Initialize(GameConfig gameConfig, Transform playerTransform, UnityEngine.Camera worldCamera)
        {
            Initialize(gameConfig, playerTransform, worldCamera, null);
        }

        public void Initialize(GameConfig gameConfig, Transform playerTransform, UnityEngine.Camera worldCamera, WorldDefinition initialWorld)
        {
            settings = gameConfig.World;
            player = playerTransform;
            targetCamera = worldCamera;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = settings.BaseFogNear;
            RenderSettings.fogEndDistance = settings.BaseFogFar;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Color.white * 0.4f;

            ApplyWorld(initialWorld);
            isInitialized = true;
            UpdateGround(currentTileX, currentTileZ, settings.BaseTileRadius);
        }

        public void ApplyWorld(WorldDefinition worldDefinition)
        {
            bool layoutChanged = currentWorld != worldDefinition || !isInitialized;
            currentWorld = worldDefinition;
            Color fogColor = currentWorld != null ? currentWorld.FogColor : settings.FogColor;
            Color skyColor = currentWorld != null ? currentWorld.SkyColor : settings.FogColor;
            Color groundColor = currentWorld != null ? currentWorld.GroundColor : settings.GroundColor;
            Color groundPatternColor = currentWorld != null ? currentWorld.GroundPatternColor : settings.GroundPatternColor;
            currentGroundColor = groundColor;

            if (layoutChanged)
            {
                ConfigureChunkLayout(currentWorld);
            }

            RenderSettings.fogColor = fogColor;
            if (targetCamera != null)
            {
                targetCamera.backgroundColor = skyColor;
            }

            if (groundTexture != null)
            {
                groundTexture.SetPixels(new[]
                {
                    groundColor,
                    groundPatternColor,
                    groundPatternColor,
                    groundColor
                });
                groundTexture.Apply(false, false);
            }

            ApplyActiveChunkGroundColor();

            if (layoutChanged && isInitialized)
            {
                int radius = Mathf.Max(currentTileRadius, settings.BaseTileRadius);
                UpdateGround(currentTileX, currentTileZ, radius);
            }
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

        private void ConfigureChunkLayout(WorldDefinition worldDefinition)
        {
            ClearChunkInstances();
            streetPrefabIndices.Clear();
            crossroadPrefabIndex = -1;
            usesChunkPrefabs = false;
            chunkPrefabs = null;
            chunkPools = null;

            IReadOnlyList<ModernCityChunkPrototype> sourcePrefabs = worldDefinition != null
                ? worldDefinition.ChunkPrefabs
                : null;
            if (sourcePrefabs == null || sourcePrefabs.Count == 0)
            {
                EnsureGroundResources();
                SetGroundTilesActive(true);
                return;
            }

            chunkPrefabs = new ModernCityChunkPrototype[sourcePrefabs.Count];
            chunkPools = new Stack<GameObject>[sourcePrefabs.Count];
            RoadConnection crossroadConnections =
                RoadConnection.North |
                RoadConnection.East |
                RoadConnection.South |
                RoadConnection.West;
            RoadConnection streetConnections = RoadConnection.East | RoadConnection.West;

            for (int index = 0; index < sourcePrefabs.Count; index++)
            {
                ModernCityChunkPrototype prefab = sourcePrefabs[index];
                chunkPrefabs[index] = prefab;
                chunkPools[index] = new Stack<GameObject>();

                if (prefab == null ||
                    Mathf.Abs(prefab.Size.x - settings.TileSize) > 0.01f ||
                    Mathf.Abs(prefab.Size.y - settings.TileSize) > 0.01f)
                {
                    continue;
                }

                if ((prefab.RoadConnections & crossroadConnections) == crossroadConnections && crossroadPrefabIndex < 0)
                {
                    crossroadPrefabIndex = index;
                }
                else if (prefab.RoadConnections == streetConnections)
                {
                    streetPrefabIndices.Add(index);
                }
            }

            usesChunkPrefabs = crossroadPrefabIndex >= 0 && streetPrefabIndices.Count > 0;
            if (!usesChunkPrefabs)
            {
                Debug.LogWarning($"{worldDefinition.name}에 교차로와 직선 도로 청크가 모두 필요합니다. 기존 바닥 타일을 사용합니다.", worldDefinition);
                chunkPrefabs = null;
                chunkPools = null;
                EnsureGroundResources();
                SetGroundTilesActive(true);
                return;
            }

            crossroadInterval = Mathf.Max(1, worldDefinition.CrossroadInterval);
            SetGroundTilesActive(false);
        }

        private void ClearChunkInstances()
        {
            foreach (KeyValuePair<Vector2Int, ActiveChunk> pair in activeChunks)
            {
                pair.Value.Instance.SetActive(false);
                DestroyRuntimeObject(pair.Value.Instance);
            }

            activeChunks.Clear();
            chunksToRelease.Clear();

            if (chunkPools == null)
            {
                return;
            }

            for (int poolIndex = 0; poolIndex < chunkPools.Length; poolIndex++)
            {
                Stack<GameObject> pool = chunkPools[poolIndex];
                while (pool != null && pool.Count > 0)
                {
                    DestroyRuntimeObject(pool.Pop());
                }
            }
        }

        private void EnsureGroundResources()
        {
            if (groundMesh == null || groundMaterial == null || groundTexture == null)
            {
                CreateGroundResources();
            }
        }

        private void SetGroundTilesActive(bool isActive)
        {
            for (int index = 0; index < groundTiles.Count; index++)
            {
                groundTiles[index].SetActive(isActive);
            }
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
                Color.white,
                Color.white,
                Color.white,
                Color.white
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
            if (usesChunkPrefabs)
            {
                UpdateChunkGround(tileX, tileZ, radius);
                return;
            }

            UpdatePatternGround(tileX, tileZ, radius);
        }

        private void UpdatePatternGround(int tileX, int tileZ, int radius)
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

        private void UpdateChunkGround(int tileX, int tileZ, int radius)
        {
            chunksToRelease.Clear();
            foreach (KeyValuePair<Vector2Int, ActiveChunk> pair in activeChunks)
            {
                Vector2Int coordinates = pair.Key;
                if (Mathf.Abs(coordinates.x - tileX) > radius || Mathf.Abs(coordinates.y - tileZ) > radius)
                {
                    chunksToRelease.Add(coordinates);
                }
            }

            for (int index = 0; index < chunksToRelease.Count; index++)
            {
                ReleaseChunk(chunksToRelease[index]);
            }

            for (int x = -radius; x <= radius; x++)
            {
                for (int z = -radius; z <= radius; z++)
                {
                    Vector2Int coordinates = new Vector2Int(tileX + x, tileZ + z);
                    if (!activeChunks.ContainsKey(coordinates))
                    {
                        AcquireChunk(coordinates);
                    }
                }
            }
        }

        private void AcquireChunk(Vector2Int coordinates)
        {
            int prefabIndex = SelectChunkPrefabIndex(coordinates.x, coordinates.y);
            Stack<GameObject> pool = chunkPools[prefabIndex];
            GameObject instance = pool.Count > 0
                ? pool.Pop()
                : Instantiate(chunkPrefabs[prefabIndex].gameObject, transform, false);

            instance.name = $"World Chunk [{coordinates.x}, {coordinates.y}] {chunkPrefabs[prefabIndex].name}";
            instance.transform.SetPositionAndRotation(
                new Vector3(coordinates.x * settings.TileSize, 0f, coordinates.y * settings.TileSize),
                Quaternion.identity);
            ApplyChunkGroundColor(instance);
            instance.SetActive(true);
            activeChunks.Add(coordinates, new ActiveChunk(instance, prefabIndex));
        }

        private void ReleaseChunk(Vector2Int coordinates)
        {
            ActiveChunk chunk = activeChunks[coordinates];
            activeChunks.Remove(coordinates);
            chunk.Instance.SetActive(false);
            chunkPools[chunk.PrefabIndex].Push(chunk.Instance);
        }

        private int SelectChunkPrefabIndex(int tileX, int tileZ)
        {
            if (PositiveModulo(tileX, crossroadInterval) == 0)
            {
                return crossroadPrefabIndex;
            }

            int hash = unchecked(tileX * 73856093 ^ tileZ * 19349663);
            hash ^= hash >> 13;
            int selection = PositiveModulo(hash, streetPrefabIndices.Count);
            return streetPrefabIndices[selection];
        }

        private void ApplyActiveChunkGroundColor()
        {
            foreach (KeyValuePair<Vector2Int, ActiveChunk> pair in activeChunks)
            {
                ApplyChunkGroundColor(pair.Value.Instance);
            }
        }

        private void ApplyChunkGroundColor(GameObject chunk)
        {
            if (chunkGroundPropertyBlock == null)
            {
                chunkGroundPropertyBlock = new MaterialPropertyBlock();
            }

            MeshRenderer[] renderers = chunk.GetComponentsInChildren<MeshRenderer>(true);
            for (int index = 0; index < renderers.Length; index++)
            {
                MeshRenderer renderer = renderers[index];
                if (renderer.gameObject.name != "Grass Ground")
                {
                    continue;
                }

                renderer.GetPropertyBlock(chunkGroundPropertyBlock);
                chunkGroundPropertyBlock.SetColor(BaseColorProperty, currentGroundColor);
                chunkGroundPropertyBlock.SetColor(ColorProperty, currentGroundColor);
                renderer.SetPropertyBlock(chunkGroundPropertyBlock);
                chunkGroundPropertyBlock.Clear();
            }
        }

        private int CountActiveGroundTiles()
        {
            int count = 0;
            for (int index = 0; index < groundTiles.Count; index++)
            {
                if (groundTiles[index].activeSelf)
                {
                    count++;
                }
            }

            return count;
        }

        private static int PositiveModulo(int value, int divisor)
        {
            int remainder = value % divisor;
            return remainder < 0 ? remainder + divisor : remainder;
        }

        private static int RoundLikeJavaScript(float value)
        {
            return Mathf.FloorToInt(value + 0.5f);
        }

        private void OnDestroy()
        {
            ClearChunkInstances();
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
