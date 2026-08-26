using System;
using System.Collections.Generic;
using IsekaiTruck.Config;
using IsekaiTruck.World;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace IsekaiTruck.Editor
{
    public static class OldJapanWorldSetup
    {
        private const float ChunkSize = 50f;
        private const float RoadWidth = 12f;
        private const int CrossroadInterval = 4;
        private const string ConfigPath = "Assets/IsekaiTruck/Config/GameConfig.asset";
        private const string WorldPath = "Assets/IsekaiTruck/Worlds/Definitions/OldJapanWorld.asset";
        private const string ModernCityWorldPath = "Assets/IsekaiTruck/Worlds/Definitions/ModernCityWorld.asset";
        private const string ArtRoot = "Assets/IsekaiTruck/Art/World/OldJapan";
        private const string MaterialRoot = ArtRoot + "/Materials";
        private const string PrefabRoot = "Assets/IsekaiTruck/Prefabs/World/OldJapan";

        private static readonly string[] PrefabPaths =
        {
            PrefabRoot + "/OldJapan_Crossroad.prefab",
            PrefabRoot + "/OldJapan_SakuraAvenue.prefab",
            PrefabRoot + "/OldJapan_CastleTown.prefab",
            PrefabRoot + "/OldJapan_ShrineRoad.prefab",
            PrefabRoot + "/OldJapan_FestivalStreet.prefab"
        };

        private static Material grassMaterial;
        private static Material earthMaterial;
        private static Material stoneMaterial;
        private static Material paleStoneMaterial;
        private static Material woodMaterial;
        private static Material darkWoodMaterial;
        private static Material plasterMaterial;
        private static Material roofMaterial;
        private static Material vermilionMaterial;
        private static Material indigoMaterial;
        private static Material goldMaterial;
        private static Material trunkMaterial;
        private static Material blossomMaterial;
        private static Material lightBlossomMaterial;
        private static Material blossomShadowMaterial;
        private static Material lanternMaterial;

        [MenuItem("Isekai Truck/World/Build Old Japan World")]
        public static void Setup()
        {
            if (EditorApplication.isPlaying)
            {
                throw new InvalidOperationException("플레이 모드를 종료한 뒤 옛 일본 월드를 생성해주세요.");
            }

            EnsureFolders();
            CreateMaterials();

            CreateCrossroadPrefab();
            CreateSakuraAvenuePrefab();
            CreateCastleTownPrefab();
            CreateShrineRoadPrefab();
            CreateFestivalStreetPrefab();

            ModernCityChunkPrototype[] prefabs = LoadPrefabs();
            ApplyToSecondWorld(prefabs);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Verify();

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog(
                    "Isekai Truck",
                    "3D 옛 일본 청크 5종을 생성하고 두 번째 월드에 연결했습니다.",
                    "확인");
            }
        }

        [MenuItem("Isekai Truck/World/Verify Old Japan World")]
        public static void Verify()
        {
            ModernCityChunkPrototype[] prefabs = LoadPrefabs();
            RoadConnection crossroadConnections =
                RoadConnection.North |
                RoadConnection.East |
                RoadConnection.South |
                RoadConnection.West;
            RoadConnection streetConnections = RoadConnection.East | RoadConnection.West;

            VerifyPrefab(PrefabPaths[0], crossroadConnections);
            for (int index = 1; index < PrefabPaths.Length; index++)
            {
                VerifyPrefab(PrefabPaths[index], streetConnections);
            }

            WorldDefinition oldJapanWorld = VerifyWorldDefinition(prefabs);
            VerifyModernCityWorldIsIndependent(prefabs);
            VerifyRuntimeLayout(oldJapanWorld);
            Debug.Log("Old Japan world verification passed.");
        }

        private static void CreateCrossroadPrefab()
        {
            GameObject root = CreateChunkRoot(
                "OldJapan_Crossroad",
                RoadConnection.North | RoadConnection.East | RoadConnection.South | RoadConnection.West);

            try
            {
                AddGround(root.transform);
                AddHorizontalRoad(root.transform);
                AddVerticalRoad(root.transform);
                AddCrossroadStoneCorners(root.transform);

                AddCherryTree(root.transform, new Vector3(-13f, 0f, -13f), 0.95f, 18f);
                AddCherryTree(root.transform, new Vector3(13f, 0f, 13f), 1.05f, -24f);
                AddWarBanner(root.transform, new Vector3(13f, 0f, -12f), 180f);
                AddWarBanner(root.transform, new Vector3(-13f, 0f, 12f), 0f);
                AddStoneLantern(root.transform, new Vector3(-10f, 0f, -10f), 0.9f);
                AddStoneLantern(root.transform, new Vector3(10f, 0f, 10f), 0.9f);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPaths[0]);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void CreateSakuraAvenuePrefab()
        {
            GameObject root = CreateChunkRoot("OldJapan_SakuraAvenue", RoadConnection.East | RoadConnection.West);

            try
            {
                AddGround(root.transform);
                AddHorizontalRoad(root.transform);
                AddRoadShoulders(root.transform);

                float[] treePositions = { -19f, -7f, 7f, 19f };
                for (int index = 0; index < treePositions.Length; index++)
                {
                    float z = index % 2 == 0 ? -12f : 12f;
                    AddCherryTree(root.transform, new Vector3(treePositions[index], 0f, z), 0.9f + index * 0.04f, index * 23f);
                }

                AddWarBanner(root.transform, new Vector3(-13f, 0f, 10f), 0f);
                AddWarBanner(root.transform, new Vector3(13f, 0f, -10f), 180f);
                AddStoneLantern(root.transform, new Vector3(-1.5f, 0f, -10f), 0.8f);
                AddStoneLantern(root.transform, new Vector3(1.5f, 0f, 10f), 0.8f);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPaths[1]);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void CreateCastleTownPrefab()
        {
            GameObject root = CreateChunkRoot("OldJapan_CastleTown", RoadConnection.East | RoadConnection.West);

            try
            {
                AddGround(root.transform);
                AddHorizontalRoad(root.transform);
                AddRoadShoulders(root.transform);

                AddMachiya(root.transform, new Vector3(-13f, 0f, -17f), 0f, 9f, 6f);
                AddMachiya(root.transform, new Vector3(12f, 0f, 17f), 180f, 10f, 6f);
                AddWoodFence(root.transform, new Vector3(9f, 0f, -14f), 0f, 11f);
                AddWoodFence(root.transform, new Vector3(-9f, 0f, 14f), 180f, 11f);
                AddCherryTree(root.transform, new Vector3(19f, 0f, -16f), 0.86f, 15f);
                AddCherryTree(root.transform, new Vector3(-19f, 0f, 16f), 0.9f, -18f);
                AddWarBanner(root.transform, new Vector3(-5f, 0f, -10f), 180f);
                AddWarBanner(root.transform, new Vector3(5f, 0f, 10f), 0f);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPaths[2]);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void CreateShrineRoadPrefab()
        {
            GameObject root = CreateChunkRoot("OldJapan_ShrineRoad", RoadConnection.East | RoadConnection.West);

            try
            {
                AddGround(root.transform);
                AddHorizontalRoad(root.transform);
                AddRoadShoulders(root.transform);

                AddShrine(root.transform, new Vector3(13f, 0f, -17f), 0f);
                AddTorii(root.transform, new Vector3(13f, 0f, -9.5f), 0f);
                AddStonePath(root.transform, new Vector3(13f, 0f, -13f), 0f, 6f);
                AddCherryTree(root.transform, new Vector3(-16f, 0f, -15f), 1.08f, 22f);
                AddCherryTree(root.transform, new Vector3(-17f, 0f, 15f), 0.9f, -12f);
                AddWarBanner(root.transform, new Vector3(-7f, 0f, -10f), 180f);
                AddWarBanner(root.transform, new Vector3(7f, 0f, 10f), 0f);
                AddStoneLantern(root.transform, new Vector3(8f, 0f, -10f), 0.9f);
                AddStoneLantern(root.transform, new Vector3(18f, 0f, -10f), 0.9f);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPaths[3]);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void CreateFestivalStreetPrefab()
        {
            GameObject root = CreateChunkRoot("OldJapan_FestivalStreet", RoadConnection.East | RoadConnection.West);

            try
            {
                AddGround(root.transform);
                AddHorizontalRoad(root.transform);
                AddRoadShoulders(root.transform);

                AddMarketStall(root.transform, new Vector3(-13f, 0f, -13f), 0f, vermilionMaterial);
                AddMarketStall(root.transform, new Vector3(2f, 0f, -13f), 0f, indigoMaterial);
                AddMarketStall(root.transform, new Vector3(-2f, 0f, 13f), 180f, indigoMaterial);
                AddMarketStall(root.transform, new Vector3(13f, 0f, 13f), 180f, vermilionMaterial);
                AddCherryTree(root.transform, new Vector3(20f, 0f, -16f), 0.92f, 10f);
                AddCherryTree(root.transform, new Vector3(-20f, 0f, 16f), 0.92f, -10f);
                AddWarBanner(root.transform, new Vector3(-6f, 0f, -10f), 180f);
                AddWarBanner(root.transform, new Vector3(6f, 0f, 10f), 0f);
                AddLanternString(root.transform, new Vector3(0f, 0f, 0f));

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPaths[4]);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static GameObject CreateChunkRoot(string name, RoadConnection connections)
        {
            GameObject root = new GameObject(name);
            ModernCityChunkPrototype chunk = root.AddComponent<ModernCityChunkPrototype>();
            chunk.Configure(new Vector2(ChunkSize, ChunkSize), connections);
            return root;
        }

        private static void AddGround(Transform parent)
        {
            CreateBox(
                parent,
                "Grass Ground",
                new Vector3(0f, -0.07f, 0f),
                new Vector3(ChunkSize, 0.1f, ChunkSize),
                grassMaterial);
        }

        private static void AddHorizontalRoad(Transform parent)
        {
            CreateBox(
                parent,
                "Old Highway Horizontal",
                new Vector3(0f, 0f, 0f),
                new Vector3(ChunkSize, 0.04f, RoadWidth),
                earthMaterial);
        }

        private static void AddVerticalRoad(Transform parent)
        {
            CreateBox(
                parent,
                "Old Highway Vertical",
                new Vector3(0f, 0.005f, 0f),
                new Vector3(RoadWidth, 0.05f, ChunkSize),
                earthMaterial);
        }

        private static void AddRoadShoulders(Transform parent)
        {
            CreateBox(parent, "Stone Shoulder North", new Vector3(0f, 0.045f, -6.5f), new Vector3(ChunkSize, 0.08f, 1f), stoneMaterial);
            CreateBox(parent, "Stone Shoulder South", new Vector3(0f, 0.045f, 6.5f), new Vector3(ChunkSize, 0.08f, 1f), stoneMaterial);
        }

        private static void AddCrossroadStoneCorners(Transform parent)
        {
            float[] signs = { -1f, 1f };
            for (int xIndex = 0; xIndex < signs.Length; xIndex++)
            {
                for (int zIndex = 0; zIndex < signs.Length; zIndex++)
                {
                    CreateBox(
                        parent,
                        "Crossroad Stone Corner",
                        new Vector3(signs[xIndex] * 9.25f, 0.04f, signs[zIndex] * 9.25f),
                        new Vector3(6.5f, 0.08f, 6.5f),
                        paleStoneMaterial);
                }
            }
        }

        private static void AddCherryTree(Transform parent, Vector3 position, float scale, float yaw)
        {
            GameObject treeRoot = new GameObject("Cherry Blossom Tree");
            treeRoot.transform.SetParent(parent, false);
            treeRoot.transform.localPosition = position;
            treeRoot.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            treeRoot.transform.localScale = Vector3.one * scale;

            CreatePrimitive(
                treeRoot.transform,
                "Tree Bed",
                PrimitiveType.Cylinder,
                new Vector3(0f, 0.08f, 0f),
                new Vector3(1.15f, 0.08f, 1.15f),
                paleStoneMaterial);
            CreatePrimitive(
                treeRoot.transform,
                "Trunk",
                PrimitiveType.Cylinder,
                new Vector3(0f, 2.35f, 0f),
                new Vector3(0.44f, 2.35f, 0.44f),
                trunkMaterial);
            CreateCylinderBetween(treeRoot.transform, "Branch Left", new Vector3(0f, 3.4f, 0f), new Vector3(-1.45f, 5f, 0.3f), 0.23f, trunkMaterial);
            CreateCylinderBetween(treeRoot.transform, "Branch Right", new Vector3(0f, 3.65f, 0f), new Vector3(1.55f, 5.2f, -0.25f), 0.21f, trunkMaterial);

            CreatePrimitive(treeRoot.transform, "Blossom Crown Center", PrimitiveType.Sphere, new Vector3(0f, 5.55f, 0f), new Vector3(4.5f, 2.8f, 3.7f), blossomMaterial);
            CreatePrimitive(treeRoot.transform, "Blossom Crown Left", PrimitiveType.Sphere, new Vector3(-1.8f, 5.1f, 0.25f), new Vector3(3.3f, 2.45f, 3f), lightBlossomMaterial);
            CreatePrimitive(treeRoot.transform, "Blossom Crown Right", PrimitiveType.Sphere, new Vector3(1.85f, 5.25f, -0.2f), new Vector3(3.4f, 2.5f, 3.1f), blossomMaterial);
            CreatePrimitive(treeRoot.transform, "Blossom Crown Back", PrimitiveType.Sphere, new Vector3(0.2f, 5.05f, 1.4f), new Vector3(3.5f, 2.2f, 2.8f), blossomShadowMaterial);
            AddFadeVolume(
                treeRoot,
                new Vector3(0f, 5.2f, 0.35f),
                new Vector3(6.2f, 4.1f, 5.8f),
                0.3f,
                GetNamedRenderers(treeRoot.transform, "Blossom Crown"));
        }

        private static void AddWarBanner(Transform parent, Vector3 position, float yaw)
        {
            GameObject bannerRoot = new GameObject("War Banner");
            bannerRoot.transform.SetParent(parent, false);
            bannerRoot.transform.localPosition = position;
            bannerRoot.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            CreatePrimitive(bannerRoot.transform, "Wood Pole", PrimitiveType.Cylinder, new Vector3(0f, 3.15f, 0f), new Vector3(0.14f, 3.15f, 0.14f), darkWoodMaterial);
            CreatePrimitive(bannerRoot.transform, "Pole Finial", PrimitiveType.Sphere, new Vector3(0f, 6.45f, 0f), Vector3.one * 0.34f, goldMaterial);
            CreateBox(bannerRoot.transform, "Banner Arm", new Vector3(0.9f, 5.75f, 0f), new Vector3(1.9f, 0.14f, 0.14f), darkWoodMaterial);
            CreateBox(bannerRoot.transform, "Indigo Cloth", new Vector3(1.05f, 4.1f, 0f), new Vector3(1.65f, 3.1f, 0.12f), indigoMaterial);
            CreateBox(bannerRoot.transform, "Gold Top Trim", new Vector3(1.05f, 5.55f, -0.08f), new Vector3(1.65f, 0.12f, 0.05f), goldMaterial);
            CreateBox(bannerRoot.transform, "Gold Bottom Trim", new Vector3(1.05f, 2.65f, -0.08f), new Vector3(1.65f, 0.12f, 0.05f), goldMaterial);
            CreatePrimitive(
                bannerRoot.transform,
                "Gold Crest",
                PrimitiveType.Cylinder,
                new Vector3(1.05f, 4.1f, -0.11f),
                new Vector3(0.52f, 0.05f, 0.52f),
                Quaternion.Euler(90f, 0f, 0f),
                goldMaterial);
            CreatePrimitive(
                bannerRoot.transform,
                "Crest Center",
                PrimitiveType.Cylinder,
                new Vector3(1.05f, 4.1f, -0.17f),
                new Vector3(0.23f, 0.035f, 0.23f),
                Quaternion.Euler(90f, 0f, 0f),
                indigoMaterial);
        }

        private static void AddMachiya(Transform parent, Vector3 position, float yaw, float width, float depth)
        {
            GameObject buildingRoot = new GameObject("Traditional Machiya");
            buildingRoot.transform.SetParent(parent, false);
            buildingRoot.transform.localPosition = position;
            buildingRoot.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            CreateBox(buildingRoot.transform, "Stone Foundation", new Vector3(0f, 0.25f, 0f), new Vector3(width + 0.6f, 0.5f, depth + 0.6f), stoneMaterial);
            CreateBox(buildingRoot.transform, "Plaster Walls", new Vector3(0f, 2.35f, 0f), new Vector3(width, 4.2f, depth), plasterMaterial);
            CreateBox(buildingRoot.transform, "Front Beam", new Vector3(0f, 3.8f, -depth * 0.51f), new Vector3(width, 0.28f, 0.25f), darkWoodMaterial);
            CreateBox(buildingRoot.transform, "Left Pillar", new Vector3(-width * 0.42f, 2.1f, -depth * 0.52f), new Vector3(0.28f, 3.6f, 0.28f), darkWoodMaterial);
            CreateBox(buildingRoot.transform, "Right Pillar", new Vector3(width * 0.42f, 2.1f, -depth * 0.52f), new Vector3(0.28f, 3.6f, 0.28f), darkWoodMaterial);
            CreateBox(buildingRoot.transform, "Sliding Door Left", new Vector3(-1.3f, 1.65f, -depth * 0.53f), new Vector3(2.2f, 2.8f, 0.16f), woodMaterial);
            CreateBox(buildingRoot.transform, "Sliding Door Right", new Vector3(1.3f, 1.65f, -depth * 0.53f), new Vector3(2.2f, 2.8f, 0.16f), woodMaterial);
            AddLattice(buildingRoot.transform, new Vector3(-1.3f, 1.75f, -depth * 0.63f), 2.2f, 2.5f);
            AddLattice(buildingRoot.transform, new Vector3(1.3f, 1.75f, -depth * 0.63f), 2.2f, 2.5f);
            AddGabledRoof(buildingRoot.transform, new Vector3(0f, 4.8f, 0f), width + 1.4f, depth + 1.5f, roofMaterial);
            AddFadeVolume(buildingRoot, new Vector3(0f, 2.8f, 0f), new Vector3(width + 1.5f, 6.2f, depth + 1.5f), 0.28f);
        }

        private static void AddShrine(Transform parent, Vector3 position, float yaw)
        {
            GameObject shrineRoot = new GameObject("Roadside Shrine");
            shrineRoot.transform.SetParent(parent, false);
            shrineRoot.transform.localPosition = position;
            shrineRoot.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            CreateBox(shrineRoot.transform, "Stone Foundation", new Vector3(0f, 0.25f, 0f), new Vector3(9f, 0.5f, 7f), paleStoneMaterial);
            CreateBox(shrineRoot.transform, "Shrine Walls", new Vector3(0f, 2.2f, 0f), new Vector3(7.5f, 3.9f, 5.5f), plasterMaterial);
            CreateBox(shrineRoot.transform, "Shrine Front", new Vector3(0f, 2.2f, -2.85f), new Vector3(6.8f, 3.4f, 0.3f), vermilionMaterial);
            CreateBox(shrineRoot.transform, "Shrine Door", new Vector3(0f, 1.7f, -3.05f), new Vector3(2.7f, 2.8f, 0.18f), darkWoodMaterial);
            CreateBox(shrineRoot.transform, "Shrine Step", new Vector3(0f, 0.45f, -3.8f), new Vector3(4.2f, 0.35f, 1.8f), woodMaterial);
            AddGabledRoof(shrineRoot.transform, new Vector3(0f, 4.55f, 0f), 9.4f, 7.5f, roofMaterial);
            AddFadeVolume(shrineRoot, new Vector3(0f, 2.8f, 0f), new Vector3(10f, 6.1f, 8f), 0.28f);
        }

        private static void AddTorii(Transform parent, Vector3 position, float yaw)
        {
            GameObject toriiRoot = new GameObject("Torii Gate");
            toriiRoot.transform.SetParent(parent, false);
            toriiRoot.transform.localPosition = position;
            toriiRoot.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            CreatePrimitive(toriiRoot.transform, "Left Pillar", PrimitiveType.Cylinder, new Vector3(-2.25f, 2.8f, 0f), new Vector3(0.3f, 2.8f, 0.3f), vermilionMaterial);
            CreatePrimitive(toriiRoot.transform, "Right Pillar", PrimitiveType.Cylinder, new Vector3(2.25f, 2.8f, 0f), new Vector3(0.3f, 2.8f, 0.3f), vermilionMaterial);
            CreateBox(toriiRoot.transform, "Upper Beam", new Vector3(0f, 5.35f, 0f), new Vector3(6.4f, 0.38f, 0.5f), vermilionMaterial);
            CreateBox(toriiRoot.transform, "Top Cap", new Vector3(0f, 5.8f, 0f), new Vector3(7.1f, 0.3f, 0.65f), darkWoodMaterial);
            CreateBox(toriiRoot.transform, "Center Plaque", new Vector3(0f, 4.75f, -0.3f), new Vector3(1.2f, 0.8f, 0.18f), goldMaterial);
            AddFadeVolume(toriiRoot, new Vector3(0f, 3f, 0f), new Vector3(7.5f, 6.2f, 1.5f), 0.32f);
        }

        private static void AddMarketStall(Transform parent, Vector3 position, float yaw, Material awningMaterial)
        {
            GameObject stallRoot = new GameObject("Festival Market Stall");
            stallRoot.transform.SetParent(parent, false);
            stallRoot.transform.localPosition = position;
            stallRoot.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            CreateBox(stallRoot.transform, "Counter", new Vector3(0f, 1.25f, 0f), new Vector3(5f, 0.55f, 2.4f), woodMaterial);
            CreateBox(stallRoot.transform, "Left Post", new Vector3(-2.1f, 2.6f, 0f), new Vector3(0.22f, 3.8f, 0.22f), darkWoodMaterial);
            CreateBox(stallRoot.transform, "Right Post", new Vector3(2.1f, 2.6f, 0f), new Vector3(0.22f, 3.8f, 0.22f), darkWoodMaterial);
            CreateBox(stallRoot.transform, "Awning", new Vector3(0f, 4.35f, 0f), new Vector3(5.6f, 0.35f, 3.2f), awningMaterial);

            for (int index = -2; index <= 2; index++)
            {
                CreatePrimitive(
                    stallRoot.transform,
                    "Paper Lantern",
                    PrimitiveType.Sphere,
                    new Vector3(index * 1.05f, 3.65f, -1.35f),
                    new Vector3(0.5f, 0.65f, 0.5f),
                    lanternMaterial);
            }

            AddFadeVolume(stallRoot, new Vector3(0f, 2.3f, 0f), new Vector3(6f, 4.8f, 3.8f), 0.32f);
        }

        private static void AddStoneLantern(Transform parent, Vector3 position, float scale)
        {
            GameObject lanternRoot = new GameObject("Stone Lantern");
            lanternRoot.transform.SetParent(parent, false);
            lanternRoot.transform.localPosition = position;
            lanternRoot.transform.localScale = Vector3.one * scale;

            CreateBox(lanternRoot.transform, "Base", new Vector3(0f, 0.15f, 0f), new Vector3(1.4f, 0.3f, 1.4f), stoneMaterial);
            CreateBox(lanternRoot.transform, "Post", new Vector3(0f, 1.15f, 0f), new Vector3(0.55f, 1.8f, 0.55f), paleStoneMaterial);
            CreateBox(lanternRoot.transform, "Light Box", new Vector3(0f, 2.25f, 0f), new Vector3(1.1f, 0.85f, 1.1f), stoneMaterial);
            CreateBox(lanternRoot.transform, "Light", new Vector3(0f, 2.25f, -0.56f), new Vector3(0.55f, 0.42f, 0.08f), lanternMaterial);
            CreateBox(lanternRoot.transform, "Roof", new Vector3(0f, 2.85f, 0f), new Vector3(1.75f, 0.25f, 1.75f), roofMaterial);
            CreatePrimitive(lanternRoot.transform, "Top", PrimitiveType.Sphere, new Vector3(0f, 3.15f, 0f), Vector3.one * 0.42f, stoneMaterial);
        }

        private static void AddWoodFence(Transform parent, Vector3 position, float yaw, float length)
        {
            GameObject fenceRoot = new GameObject("Wood Fence");
            fenceRoot.transform.SetParent(parent, false);
            fenceRoot.transform.localPosition = position;
            fenceRoot.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            int postCount = Mathf.CeilToInt(length / 2.5f) + 1;
            float spacing = length / (postCount - 1);
            for (int index = 0; index < postCount; index++)
            {
                float x = -length * 0.5f + spacing * index;
                CreateBox(fenceRoot.transform, "Fence Post", new Vector3(x, 1f, 0f), new Vector3(0.22f, 2f, 0.22f), darkWoodMaterial);
            }

            CreateBox(fenceRoot.transform, "Fence Rail Upper", new Vector3(0f, 1.45f, 0f), new Vector3(length, 0.18f, 0.18f), woodMaterial);
            CreateBox(fenceRoot.transform, "Fence Rail Lower", new Vector3(0f, 0.65f, 0f), new Vector3(length, 0.18f, 0.18f), woodMaterial);
        }

        private static void AddStonePath(Transform parent, Vector3 position, float yaw, float length)
        {
            GameObject pathRoot = new GameObject("Shrine Stone Path");
            pathRoot.transform.SetParent(parent, false);
            pathRoot.transform.localPosition = position;
            pathRoot.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            for (float z = -length * 0.5f; z <= length * 0.5f; z += 1.5f)
            {
                CreateBox(pathRoot.transform, "Stepping Stone", new Vector3(0f, 0.06f, z), new Vector3(2.3f, 0.12f, 1.1f), paleStoneMaterial);
            }
        }

        private static void AddLanternString(Transform parent, Vector3 position)
        {
            GameObject lanternRoot = new GameObject("Festival Lantern String");
            lanternRoot.transform.SetParent(parent, false);
            lanternRoot.transform.localPosition = position;

            CreateBox(lanternRoot.transform, "Left Support", new Vector3(-10f, 3f, 0f), new Vector3(0.18f, 6f, 0.18f), darkWoodMaterial);
            CreateBox(lanternRoot.transform, "Right Support", new Vector3(10f, 3f, 0f), new Vector3(0.18f, 6f, 0.18f), darkWoodMaterial);
            CreateBox(lanternRoot.transform, "Lantern Rope", new Vector3(0f, 5.65f, 0f), new Vector3(20f, 0.08f, 0.08f), darkWoodMaterial);

            for (int index = -4; index <= 4; index++)
            {
                Material material = index % 2 == 0 ? lanternMaterial : lightBlossomMaterial;
                CreatePrimitive(
                    lanternRoot.transform,
                    "Hanging Lantern",
                    PrimitiveType.Sphere,
                    new Vector3(index * 2f, 5.2f, 0f),
                    new Vector3(0.65f, 0.85f, 0.65f),
                    material);
            }
        }

        private static void AddGabledRoof(Transform parent, Vector3 center, float width, float depth, Material material)
        {
            float halfDepth = depth * 0.5f;
            float panelDepth = halfDepth + 0.8f;
            CreatePrimitive(
                parent,
                "Roof Front Slope",
                PrimitiveType.Cube,
                center + new Vector3(0f, 0f, -halfDepth * 0.48f),
                new Vector3(width, 0.28f, panelDepth),
                Quaternion.Euler(-20f, 0f, 0f),
                material);
            CreatePrimitive(
                parent,
                "Roof Back Slope",
                PrimitiveType.Cube,
                center + new Vector3(0f, 0f, halfDepth * 0.48f),
                new Vector3(width, 0.28f, panelDepth),
                Quaternion.Euler(20f, 0f, 0f),
                material);
            CreateBox(parent, "Roof Ridge", center + new Vector3(0f, 0.55f, 0f), new Vector3(width + 0.25f, 0.35f, 0.38f), darkWoodMaterial);
        }

        private static void AddLattice(Transform parent, Vector3 center, float width, float height)
        {
            for (int index = -2; index <= 2; index++)
            {
                CreateBox(
                    parent,
                    "Door Lattice Vertical",
                    center + new Vector3(index * width * 0.2f, 0f, 0f),
                    new Vector3(0.08f, height, 0.08f),
                    darkWoodMaterial);
            }

            for (int index = -1; index <= 1; index++)
            {
                CreateBox(
                    parent,
                    "Door Lattice Horizontal",
                    center + new Vector3(0f, index * height * 0.3f, 0f),
                    new Vector3(width, 0.08f, 0.08f),
                    darkWoodMaterial);
            }
        }

        private static void CreateCylinderBetween(
            Transform parent,
            string name,
            Vector3 start,
            Vector3 end,
            float radius,
            Material material)
        {
            Vector3 direction = end - start;
            CreatePrimitive(
                parent,
                name,
                PrimitiveType.Cylinder,
                (start + end) * 0.5f,
                new Vector3(radius, direction.magnitude * 0.5f, radius),
                Quaternion.FromToRotation(Vector3.up, direction.normalized),
                material);
        }

        private static void AddFadeVolume(
            GameObject target,
            Vector3 localCenter,
            Vector3 localSize,
            float fadedAlpha,
            Renderer[] targetRenderers = null)
        {
            WorldModelFadeVolume fadeVolume = target.AddComponent<WorldModelFadeVolume>();
            Renderer[] renderers = targetRenderers ?? target.GetComponentsInChildren<Renderer>(true);
            fadeVolume.Configure(localCenter, localSize, fadedAlpha, 0.2f, renderers);
        }

        private static Renderer[] GetNamedRenderers(Transform root, string namePrefix)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            List<Renderer> matches = new List<Renderer>();
            for (int index = 0; index < renderers.Length; index++)
            {
                if (renderers[index].name.StartsWith(namePrefix, StringComparison.Ordinal))
                {
                    matches.Add(renderers[index]);
                }
            }

            return matches.ToArray();
        }

        private static GameObject CreateBox(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 scale,
            Material material)
        {
            return CreatePrimitive(parent, name, PrimitiveType.Cube, position, scale, Quaternion.identity, material);
        }

        private static GameObject CreatePrimitive(
            Transform parent,
            string name,
            PrimitiveType primitiveType,
            Vector3 position,
            Vector3 scale,
            Material material)
        {
            return CreatePrimitive(parent, name, primitiveType, position, scale, Quaternion.identity, material);
        }

        private static GameObject CreatePrimitive(
            Transform parent,
            string name,
            PrimitiveType primitiveType,
            Vector3 position,
            Vector3 scale,
            Quaternion rotation,
            Material material)
        {
            GameObject primitive = GameObject.CreatePrimitive(primitiveType);
            primitive.name = name;
            primitive.transform.SetParent(parent, false);
            primitive.transform.localPosition = position;
            primitive.transform.localRotation = rotation;
            primitive.transform.localScale = scale;

            Renderer renderer = primitive.GetComponent<Renderer>();
            renderer.sharedMaterial = material;

            Collider collider = primitive.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            return primitive;
        }

        private static void CreateMaterials()
        {
            grassMaterial = CreateMaterial(MaterialRoot + "/OldJapan_Grass.mat", new Color32(0x78, 0x93, 0x5c, 0xff));
            earthMaterial = CreateMaterial(MaterialRoot + "/OldJapan_EarthRoad.mat", new Color32(0xb8, 0x95, 0x6b, 0xff));
            stoneMaterial = CreateMaterial(MaterialRoot + "/OldJapan_Stone.mat", new Color32(0x73, 0x77, 0x72, 0xff));
            paleStoneMaterial = CreateMaterial(MaterialRoot + "/OldJapan_PaleStone.mat", new Color32(0xb8, 0xb5, 0xaa, 0xff));
            woodMaterial = CreateMaterial(MaterialRoot + "/OldJapan_Wood.mat", new Color32(0x89, 0x5a, 0x36, 0xff));
            darkWoodMaterial = CreateMaterial(MaterialRoot + "/OldJapan_DarkWood.mat", new Color32(0x3f, 0x2b, 0x25, 0xff));
            plasterMaterial = CreateMaterial(MaterialRoot + "/OldJapan_Plaster.mat", new Color32(0xe2, 0xd8, 0xc5, 0xff));
            roofMaterial = CreateMaterial(MaterialRoot + "/OldJapan_Roof.mat", new Color32(0x2f, 0x38, 0x3a, 0xff));
            vermilionMaterial = CreateMaterial(MaterialRoot + "/OldJapan_Vermilion.mat", new Color32(0xb8, 0x35, 0x2f, 0xff));
            indigoMaterial = CreateMaterial(MaterialRoot + "/OldJapan_Indigo.mat", new Color32(0x27, 0x2b, 0x58, 0xff));
            goldMaterial = CreateMaterial(MaterialRoot + "/OldJapan_Gold.mat", new Color32(0xd7, 0xb8, 0x69, 0xff));
            trunkMaterial = CreateMaterial(MaterialRoot + "/OldJapan_CherryTrunk.mat", new Color32(0x48, 0x32, 0x35, 0xff));
            blossomMaterial = CreateMaterial(MaterialRoot + "/OldJapan_Blossom.mat", new Color32(0xf3, 0xb7, 0xcb, 0xff));
            lightBlossomMaterial = CreateMaterial(MaterialRoot + "/OldJapan_LightBlossom.mat", new Color32(0xff, 0xd8, 0xe3, 0xff));
            blossomShadowMaterial = CreateMaterial(MaterialRoot + "/OldJapan_BlossomShadow.mat", new Color32(0xd9, 0x91, 0xae, 0xff));
            lanternMaterial = CreateMaterial(MaterialRoot + "/OldJapan_Lantern.mat", new Color32(0xff, 0xd0, 0x8a, 0xff));
        }

        private static Material CreateMaterial(string assetPath, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                material = new Material(shader);
                AssetDatabase.CreateAsset(material, assetPath);
            }

            material.color = color;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", 0.05f);
            }

            EditorUtility.SetDirty(material);
            return material;
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
                    throw new MissingReferenceException($"옛 일본 청크 프리팹을 찾지 못했습니다: {PrefabPaths[index]}");
                }

                prefabs[index] = prefab;
            }

            return prefabs;
        }

        private static void ApplyToSecondWorld(ModernCityChunkPrototype[] prefabs)
        {
            WorldDefinition definition = AssetDatabase.LoadAssetAtPath<WorldDefinition>(WorldPath);
            if (definition == null)
            {
                throw new MissingReferenceException($"두 번째 세계 정의를 찾지 못했습니다: {WorldPath}");
            }

            definition.SetEditorValues(
                "old_japan",
                "에도의 세계",
                new Color32(0xb8, 0xd3, 0xdf, 0xff),
                new Color32(0xd7, 0xc5, 0xce, 0xff),
                new Color32(0x78, 0x93, 0x5c, 0xff),
                new Color32(0x62, 0x7b, 0x4b, 0xff));
            definition.SetEditorChunkLayout(prefabs, CrossroadInterval);
            EditorUtility.SetDirty(definition);
        }

        private static void VerifyPrefab(string assetPath, RoadConnection expectedConnections)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            ModernCityChunkPrototype chunk = prefab != null
                ? prefab.GetComponent<ModernCityChunkPrototype>()
                : null;
            if (chunk == null ||
                chunk.Size != new Vector2(ChunkSize, ChunkSize) ||
                chunk.RoadConnections != expectedConnections)
            {
                throw new InvalidOperationException($"옛 일본 청크 구성을 확인하지 못했습니다: {assetPath}");
            }

            if (prefab.GetComponentsInChildren<SpriteRenderer>(true).Length > 0)
            {
                throw new InvalidOperationException($"옛 일본 청크에 2D 스프라이트가 남아 있습니다: {assetPath}");
            }

            if (prefab.GetComponentsInChildren<Collider>(true).Length > 0)
            {
                throw new InvalidOperationException($"옛 일본 청크에 충돌체가 남아 있습니다: {assetPath}");
            }

            bool hasCherryTree = false;
            bool hasWarBanner = false;
            Transform[] transforms = prefab.GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < transforms.Length; index++)
            {
                hasCherryTree |= transforms[index].name == "Cherry Blossom Tree";
                hasWarBanner |= transforms[index].name == "War Banner";
            }

            if (!hasCherryTree || !hasWarBanner)
            {
                throw new InvalidOperationException($"벚꽃나무 또는 깃발이 없는 옛 일본 청크입니다: {assetPath}");
            }

            WorldModelFadeVolume[] fadeVolumes = prefab.GetComponentsInChildren<WorldModelFadeVolume>(true);
            if (fadeVolumes.Length == 0)
            {
                throw new InvalidOperationException($"옛 일본 청크에 투명화 영역이 없습니다: {assetPath}");
            }

            for (int index = 0; index < fadeVolumes.Length; index++)
            {
                if (fadeVolumes[index].FadeRendererCount == 0)
                {
                    throw new InvalidOperationException($"옛 일본 투명화 대상 렌더러가 없습니다: {assetPath}");
                }
            }
        }

        private static WorldDefinition VerifyWorldDefinition(ModernCityChunkPrototype[] expectedPrefabs)
        {
            WorldDefinition definition = AssetDatabase.LoadAssetAtPath<WorldDefinition>(WorldPath);
            if (definition == null ||
                definition.Id != "old_japan" ||
                definition.DisplayName != "에도의 세계" ||
                definition.CrossroadInterval != CrossroadInterval ||
                definition.ChunkPrefabs.Count != expectedPrefabs.Length)
            {
                throw new InvalidOperationException("두 번째 세계의 옛 일본 청크 설정이 올바르지 않습니다.");
            }

            for (int index = 0; index < expectedPrefabs.Length; index++)
            {
                if (definition.ChunkPrefabs[index] != expectedPrefabs[index])
                {
                    throw new InvalidOperationException("두 번째 세계의 옛 일본 청크 순서가 올바르지 않습니다.");
                }
            }

            return definition;
        }

        private static void VerifyModernCityWorldIsIndependent(ModernCityChunkPrototype[] oldJapanPrefabs)
        {
            WorldDefinition modernCityWorld = AssetDatabase.LoadAssetAtPath<WorldDefinition>(ModernCityWorldPath);
            if (modernCityWorld == null)
            {
                throw new MissingReferenceException("현대 도시 세계 정의를 찾지 못했습니다.");
            }

            for (int index = 0; index < modernCityWorld.ChunkPrefabs.Count; index++)
            {
                for (int oldJapanIndex = 0; oldJapanIndex < oldJapanPrefabs.Length; oldJapanIndex++)
                {
                    if (modernCityWorld.ChunkPrefabs[index] == oldJapanPrefabs[oldJapanIndex])
                    {
                        throw new InvalidOperationException("현대 도시 세계에 옛 일본 청크가 잘못 연결되었습니다.");
                    }
                }
            }
        }

        private static void VerifyRuntimeLayout(WorldDefinition worldDefinition)
        {
            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            if (config == null)
            {
                throw new MissingReferenceException("GameConfig를 찾지 못했습니다.");
            }

            GameObject worldObject = new GameObject("Old Japan Verification World");
            GameObject playerObject = new GameObject("Old Japan Verification Player");
            GameObject cameraObject = new GameObject("Old Japan Verification Camera");

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
                        $"초기 옛 일본 청크 수가 올바르지 않습니다: expected {expectedTileCount}, actual {worldManager.ActiveTileCount}");
                }

                VerifyActiveChunkTypes(worldObject, expectedTileCount, sideLength);

                playerObject.transform.position = Vector3.right * config.World.TileSize;
                cameraObject.transform.position = playerObject.transform.position;
                worldManager.UpdateWorld(1f);
                if (worldManager.ActiveTileCount != expectedTileCount)
                {
                    throw new InvalidOperationException("플레이어 이동 후 옛 일본 활성 청크 수가 유지되지 않았습니다.");
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
                throw new InvalidOperationException("활성 옛 일본 청크 오브젝트 수가 월드 상태와 일치하지 않습니다.");
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
                    continue;
                }

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

            if (crossroadCount != sideLength || streetTypes.Count != PrefabPaths.Length - 1)
            {
                throw new InvalidOperationException(
                    $"초기 옛 일본 청크 종류 배치가 올바르지 않습니다: crossroads {crossroadCount}, streets {streetTypes.Count}");
            }
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/IsekaiTruck/Art", "World");
            EnsureFolder("Assets/IsekaiTruck/Art/World", "OldJapan");
            EnsureFolder(ArtRoot, "Materials");
            EnsureFolder("Assets/IsekaiTruck/Prefabs", "World");
            EnsureFolder("Assets/IsekaiTruck/Prefabs/World", "OldJapan");
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}
