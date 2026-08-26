using System;
using System.Collections.Generic;
using IsekaiTruck.Config;
using IsekaiTruck.World;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace IsekaiTruck.Editor
{
    public static class ModernCityWorldIntegrationSetup
    {
        private const string ConfigPath = "Assets/IsekaiTruck/Config/GameConfig.asset";
        private const string OriginalWorldPath = "Assets/IsekaiTruck/Worlds/Definitions/OriginalWorld.asset";
        private const string DarkWorldPath = "Assets/IsekaiTruck/Worlds/Definitions/DarkWorld.asset";
        private const int CrossroadInterval = 4;

        private static readonly string[] PrefabPaths =
        {
            "Assets/IsekaiTruck/Prefabs/World/ModernCity/ModernCity_Crossroad.prefab",
            "Assets/IsekaiTruck/Prefabs/World/ModernCity/ModernCity_MartStreet.prefab",
            "Assets/IsekaiTruck/Prefabs/World/ModernCity/ModernCity_Residential.prefab",
            "Assets/IsekaiTruck/Prefabs/World/ModernCity/ModernCity_SchoolZone.prefab",
            "Assets/IsekaiTruck/Prefabs/World/ModernCity/ModernCity_ChurchPark.prefab"
        };

        [MenuItem("Isekai Truck/World/Apply Modern City Chunks")]
        public static void Setup()
        {
            ModernCityChunkPrototype[] prefabs = LoadPrefabs();
            ApplyToWorld(OriginalWorldPath, prefabs);
            ApplyToWorld(DarkWorldPath, prefabs);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Verify();

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog(
                    "Isekai Truck",
                    "현재 현대 도시 프리팹 5종을 월드 생성 시스템에 연결했습니다.",
                    "확인");
            }
        }

        [MenuItem("Isekai Truck/World/Verify Modern City World Integration")]
        public static void Verify()
        {
            ModernCityChunkPrototype[] prefabs = LoadPrefabs();
            WorldDefinition originalWorld = VerifyWorldDefinition(OriginalWorldPath, prefabs);
            VerifyWorldDefinition(DarkWorldPath, prefabs);
            VerifyRuntimeLayout(originalWorld);
            Debug.Log("Modern city world integration verification passed.");
        }

        private static ModernCityChunkPrototype[] LoadPrefabs()
        {
            ModernCityChunkPrototype[] prefabs = new ModernCityChunkPrototype[PrefabPaths.Length];
            for (int index = 0; index < PrefabPaths.Length; index++)
            {
                GameObject prefabObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPaths[index]);
                ModernCityChunkPrototype prefab = prefabObject != null
                    ? prefabObject.GetComponent<ModernCityChunkPrototype>()
                    : null;
                if (prefab == null)
                {
                    throw new MissingReferenceException($"현대 도시 청크 프리팹을 찾지 못했습니다: {PrefabPaths[index]}");
                }

                prefabs[index] = prefab;
            }

            return prefabs;
        }

        private static void ApplyToWorld(string assetPath, ModernCityChunkPrototype[] prefabs)
        {
            WorldDefinition definition = AssetDatabase.LoadAssetAtPath<WorldDefinition>(assetPath);
            if (definition == null)
            {
                throw new MissingReferenceException($"세계 정의를 찾지 못했습니다: {assetPath}");
            }

            definition.SetEditorChunkLayout(prefabs, CrossroadInterval);
            EditorUtility.SetDirty(definition);
        }

        private static WorldDefinition VerifyWorldDefinition(
            string assetPath,
            ModernCityChunkPrototype[] expectedPrefabs)
        {
            WorldDefinition definition = AssetDatabase.LoadAssetAtPath<WorldDefinition>(assetPath);
            if (definition == null ||
                definition.CrossroadInterval != CrossroadInterval ||
                definition.ChunkPrefabs.Count != expectedPrefabs.Length)
            {
                throw new InvalidOperationException($"세계 청크 설정이 올바르지 않습니다: {assetPath}");
            }

            for (int index = 0; index < expectedPrefabs.Length; index++)
            {
                if (definition.ChunkPrefabs[index] != expectedPrefabs[index])
                {
                    throw new InvalidOperationException($"세계 청크 순서가 올바르지 않습니다: {assetPath}");
                }
            }

            return definition;
        }

        private static void VerifyRuntimeLayout(WorldDefinition worldDefinition)
        {
            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            if (config == null)
            {
                throw new MissingReferenceException("GameConfig를 찾지 못했습니다.");
            }

            GameObject worldObject = new GameObject("Modern City Integration Verification World");
            GameObject playerObject = new GameObject("Modern City Integration Verification Player");
            GameObject cameraObject = new GameObject("Modern City Integration Verification Camera");

            try
            {
                UnityEngine.Camera targetCamera = cameraObject.AddComponent<UnityEngine.Camera>();
                WorldManager worldManager = worldObject.AddComponent<WorldManager>();
                worldManager.Initialize(config, playerObject.transform, targetCamera, worldDefinition);

                int sideLength = config.World.BaseTileRadius * 2 + 1;
                int expectedTileCount = sideLength * sideLength;
                if (!worldManager.UsesChunkPrefabs || worldManager.ActiveTileCount != expectedTileCount)
                {
                    throw new InvalidOperationException(
                        $"초기 현대 도시 청크 수가 올바르지 않습니다: expected {expectedTileCount}, actual {worldManager.ActiveTileCount}");
                }

                VerifyActiveChunkTypes(worldObject, expectedTileCount, sideLength);

                playerObject.transform.position = Vector3.right * config.World.TileSize;
                cameraObject.transform.position = playerObject.transform.position;
                worldManager.UpdateWorld(1f);
                if (worldManager.ActiveTileCount != expectedTileCount)
                {
                    throw new InvalidOperationException("플레이어 이동 후 활성 청크 수가 유지되지 않았습니다.");
                }
            }
            finally
            {
                Object.DestroyImmediate(worldObject);
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(playerObject);
            }
        }

        private static void VerifyActiveChunkTypes(GameObject worldObject, int expectedTileCount, int sideLength)
        {
            ModernCityChunkPrototype[] chunks = worldObject.GetComponentsInChildren<ModernCityChunkPrototype>(false);
            if (chunks.Length != expectedTileCount)
            {
                throw new InvalidOperationException("활성 청크 오브젝트 수가 월드 상태와 일치하지 않습니다.");
            }

            RoadConnection crossroadConnections =
                RoadConnection.North |
                RoadConnection.East |
                RoadConnection.South |
                RoadConnection.West;
            int crossroadCount = 0;
            HashSet<string> streetTypes = new HashSet<string>();

            for (int index = 0; index < chunks.Length; index++)
            {
                ModernCityChunkPrototype chunk = chunks[index];
                if (chunk.RoadConnections == crossroadConnections)
                {
                    crossroadCount++;
                }
                else
                {
                    for (int prefabIndex = 1; prefabIndex < PrefabPaths.Length; prefabIndex++)
                    {
                        string prefabName = System.IO.Path.GetFileNameWithoutExtension(PrefabPaths[prefabIndex]);
                        if (chunk.gameObject.name.Contains(prefabName))
                        {
                            streetTypes.Add(prefabName);
                            break;
                        }
                    }
                }
            }

            if (crossroadCount != sideLength || streetTypes.Count != PrefabPaths.Length - 1)
            {
                throw new InvalidOperationException(
                    $"초기 청크 종류 배치가 올바르지 않습니다: crossroads {crossroadCount}, streets {streetTypes.Count}");
            }
        }
    }
}
