using System;
using System.Collections.Generic;
using IsekaiTruck.World;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace IsekaiTruck.Editor
{
    public static class ModernCity3DModelSetup
    {
        private const string MaterialRoot = "Assets/IsekaiTruck/Art/World/ModernCity/Materials";

        private static readonly string[] PrefabPaths =
        {
            "Assets/IsekaiTruck/Prefabs/World/ModernCity/ModernCity_Crossroad.prefab",
            "Assets/IsekaiTruck/Prefabs/World/ModernCity/ModernCity_MartStreet.prefab",
            "Assets/IsekaiTruck/Prefabs/World/ModernCity/ModernCity_Residential.prefab",
            "Assets/IsekaiTruck/Prefabs/World/ModernCity/ModernCity_SchoolZone.prefab",
            "Assets/IsekaiTruck/Prefabs/World/ModernCity/ModernCity_ChurchPark.prefab"
        };

        private static readonly string[] RequiredModelNames =
        {
            "City Bus Stop",
            "3D Modern Mart",
            "3D Apartment Left",
            "3D Modern School",
            "3D Modern Church"
        };

        private static Material concreteMaterial;
        private static Material whiteMaterial;
        private static Material darkMaterial;
        private static Material glassMaterial;
        private static Material redMaterial;
        private static Material blueMaterial;
        private static Material yellowMaterial;
        private static Material brickMaterial;
        private static Material lightGreenMaterial;
        private static Material darkGreenMaterial;
        private static Material trunkMaterial;
        private static Material waterMaterial;
        private static Material rubberMaterial;
        private static Material metalMaterial;
        private static Material woodMaterial;
        private static Material lampMaterial;

        [MenuItem("Isekai Truck/World/Add 3D Models To Modern City")]
        public static void Setup()
        {
            if (EditorApplication.isPlaying)
            {
                throw new InvalidOperationException("플레이 모드를 종료한 뒤 기본 월드 3D 모델을 생성해주세요.");
            }

            ApplyToPrefabs();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Verify();

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog(
                    "Isekai Truck",
                    "기본 월드 청크 5종에 3D 도시 모델을 배치했습니다.",
                    "확인");
            }
        }

        public static void ApplyToPrefabs()
        {
            CreateMaterials();

            for (int index = 0; index < PrefabPaths.Length; index++)
            {
                GameObject root = PrefabUtility.LoadPrefabContents(PrefabPaths[index]);
                if (root == null)
                {
                    throw new MissingReferenceException($"현대 도시 청크 프리팹을 찾지 못했습니다: {PrefabPaths[index]}");
                }

                try
                {
                    RemoveSpriteHierarchy(root);
                    RemovePreviousGeneratedModels(root.transform);

                    GameObject modelRoot = new GameObject("Modern 3D Decorations");
                    modelRoot.transform.SetParent(root.transform, false);
                    AddModelsForChunk(index, modelRoot.transform);

                    PrefabUtility.SaveAsPrefabAsset(root, PrefabPaths[index]);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
        }

        [MenuItem("Isekai Truck/World/Verify Modern City 3D Models")]
        public static void Verify()
        {
            for (int index = 0; index < PrefabPaths.Length; index++)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPaths[index]);
                ModernCityChunkPrototype chunk = prefab != null
                    ? prefab.GetComponent<ModernCityChunkPrototype>()
                    : null;
                if (chunk == null)
                {
                    throw new MissingReferenceException($"현대 도시 청크 프리팹을 찾지 못했습니다: {PrefabPaths[index]}");
                }

                if (prefab.GetComponentsInChildren<SpriteRenderer>(true).Length > 0)
                {
                    throw new InvalidOperationException($"기본 월드 청크에 2D 스프라이트가 남아 있습니다: {PrefabPaths[index]}");
                }

                if (prefab.GetComponentsInChildren<Collider>(true).Length > 0)
                {
                    throw new InvalidOperationException($"기본 월드 청크에 장식 충돌체가 남아 있습니다: {PrefabPaths[index]}");
                }

                if (!ContainsTransformName(prefab.transform, "Modern 3D Decorations") ||
                    !ContainsTransformName(prefab.transform, RequiredModelNames[index]))
                {
                    throw new InvalidOperationException($"기본 월드 3D 모델 배치를 확인하지 못했습니다: {PrefabPaths[index]}");
                }

                WorldModelFadeVolume[] fadeVolumes = prefab.GetComponentsInChildren<WorldModelFadeVolume>(true);
                if (fadeVolumes.Length == 0)
                {
                    throw new InvalidOperationException($"기본 월드에 투명화 영역이 없습니다: {PrefabPaths[index]}");
                }

                for (int volumeIndex = 0; volumeIndex < fadeVolumes.Length; volumeIndex++)
                {
                    if (fadeVolumes[volumeIndex].FadeRendererCount == 0 ||
                        fadeVolumes[volumeIndex].LocalSize.x <= 0f ||
                        fadeVolumes[volumeIndex].LocalSize.y <= 0f ||
                        fadeVolumes[volumeIndex].LocalSize.z <= 0f)
                    {
                        throw new InvalidOperationException($"기본 월드 투명화 영역 구성이 올바르지 않습니다: {PrefabPaths[index]}");
                    }
                }
            }

            VerifyFadeBehavior();
            ModernCityWorldIntegrationSetup.Verify();
            Debug.Log("Modern city 3D model verification passed.");
        }

        private static void VerifyFadeBehavior()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPaths[0]);
            GameObject instance = Object.Instantiate(prefab);
            try
            {
                WorldModelFadeVolume fadeVolume = instance.GetComponentInChildren<WorldModelFadeVolume>(true);
                if (fadeVolume == null)
                {
                    throw new InvalidOperationException("투명화 동작 검증 대상을 찾지 못했습니다.");
                }

                Vector3 insidePosition = fadeVolume.transform.TransformPoint(fadeVolume.LocalCenter);
                fadeVolume.UpdateFade(insidePosition, insidePosition, 1f);
                if (fadeVolume.CurrentAlpha > fadeVolume.FadedAlpha + 0.01f)
                {
                    throw new InvalidOperationException("트럭 진입 시 오브젝트가 투명해지지 않았습니다.");
                }

                Vector3 outsidePosition = fadeVolume.transform.TransformPoint(
                    fadeVolume.LocalCenter + Vector3.one * 100f);
                fadeVolume.UpdateFade(outsidePosition, outsidePosition, 1f);
                if (fadeVolume.CurrentAlpha < 0.99f)
                {
                    throw new InvalidOperationException("트럭 이탈 시 오브젝트 투명도가 복구되지 않았습니다.");
                }

                Vector3 cameraPosition = fadeVolume.transform.TransformPoint(
                    fadeVolume.LocalCenter + Vector3.forward * (fadeVolume.LocalSize.z + 5f));
                Vector3 truckPosition = fadeVolume.transform.TransformPoint(
                    fadeVolume.LocalCenter - Vector3.forward * (fadeVolume.LocalSize.z + 5f));
                fadeVolume.UpdateFade(truckPosition, cameraPosition, 1f);
                if (fadeVolume.CurrentAlpha > fadeVolume.FadedAlpha + 0.01f)
                {
                    throw new InvalidOperationException("카메라 시야 차단 시 오브젝트가 투명해지지 않았습니다.");
                }

                fadeVolume.RestoreImmediate();
                instance.SetActive(false);
                if (fadeVolume.CurrentAlpha < 0.99f)
                {
                    throw new InvalidOperationException("청크 비활성화 시 오브젝트 투명도가 복구되지 않았습니다.");
                }
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static void AddModelsForChunk(int index, Transform parent)
        {
            switch (index)
            {
                case 0:
                    AddCrossroadModels(parent);
                    break;
                case 1:
                    AddMartStreetModels(parent);
                    break;
                case 2:
                    AddResidentialModels(parent);
                    break;
                case 3:
                    AddSchoolZoneModels(parent);
                    break;
                case 4:
                    AddChurchParkModels(parent);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        private static void AddCrossroadModels(Transform parent)
        {
            AddTrafficLight(parent, new Vector3(-8.5f, 0f, -8.5f), 0f);
            AddTrafficLight(parent, new Vector3(8.5f, 0f, 8.5f), 180f);
            AddTrafficLight(parent, new Vector3(8.5f, 0f, -8.5f), 90f);
            AddTrafficLight(parent, new Vector3(-8.5f, 0f, 8.5f), -90f);

            AddBusStop(parent, new Vector3(16f, 0f, 11.5f), 180f);
            AddPlanter(parent, new Vector3(-16f, 0f, -13f), 0f);
            AddPlanter(parent, new Vector3(16f, 0f, -13f), 0f);
            AddLowPolyTree(parent, new Vector3(-18f, 0f, -18f), 0.95f, 12f);
            AddLowPolyTree(parent, new Vector3(18f, 0f, -18f), 0.88f, -18f);
            AddLowPolyTree(parent, new Vector3(-18f, 0f, 18f), 0.9f, 24f);
            AddLowPolyTree(parent, new Vector3(18f, 0f, 18f), 1f, -8f);
        }

        private static void AddMartStreetModels(Transform parent)
        {
            AddModernMart(parent, new Vector3(-11f, 0f, -18.5f), 0f);
            AddVendingMachine(parent, new Vector3(-1.5f, 0f, -12f), 0f);
            AddParkedCar(parent, new Vector3(9f, 0f, -17f), 0f, yellowMaterial);
            AddParkedCar(parent, new Vector3(16f, 0f, -17f), 0f, blueMaterial);
            AddBikeRack(parent, new Vector3(21f, 0f, -11.5f), 0f);

            AddLowPolyTree(parent, new Vector3(-20f, 0f, 15f), 0.9f, 12f);
            AddLowPolyTree(parent, new Vector3(-7f, 0f, 16f), 0.82f, -20f);
            AddLowPolyTree(parent, new Vector3(7f, 0f, 15f), 0.95f, 18f);
            AddLowPolyTree(parent, new Vector3(20f, 0f, 16f), 0.86f, -8f);
        }

        private static void AddResidentialModels(Transform parent)
        {
            AddApartment(parent, "3D Apartment Left", new Vector3(-12f, 0f, -18f), 0f, 14f, 9f, 12f, brickMaterial);
            AddApartment(parent, "3D Apartment Right", new Vector3(12f, 0f, -18f), 0f, 14f, 9f, 10f, whiteMaterial);
            AddPlayground(parent, new Vector3(0f, 0f, 16f));
            AddPlanter(parent, new Vector3(-16f, 0f, 12f), 0f);
            AddPlanter(parent, new Vector3(16f, 0f, 12f), 0f);
            AddLowPolyTree(parent, new Vector3(-21f, 0f, 17f), 0.92f, 10f);
            AddLowPolyTree(parent, new Vector3(21f, 0f, 17f), 0.88f, -10f);
        }

        private static void AddSchoolZoneModels(Transform parent)
        {
            AddModernSchool(parent, new Vector3(-8f, 0f, -18f), 0f);
            AddSchoolGate(parent, new Vector3(13f, 0f, -10.5f), 0f);
            AddBikeRack(parent, new Vector3(18f, 0f, -14f), 90f);
            AddBasketballGoal(parent, new Vector3(16f, 0f, -20f), 180f);
            AddLowPolyTree(parent, new Vector3(-20f, 0f, 15f), 0.88f, 12f);
            AddLowPolyTree(parent, new Vector3(-7f, 0f, 16f), 0.8f, -16f);
            AddLowPolyTree(parent, new Vector3(7f, 0f, 15f), 0.92f, 20f);
            AddLowPolyTree(parent, new Vector3(20f, 0f, 16f), 0.84f, -12f);
        }

        private static void AddChurchParkModels(Transform parent)
        {
            AddModernChurch(parent, new Vector3(-11f, 0f, -18f), 0f);
            AddFountain(parent, new Vector3(13f, 0f, -17f));
            AddGazebo(parent, new Vector3(0f, 0f, 16f));
            AddLowPolyTree(parent, new Vector3(21f, 0f, -14f), 0.86f, 16f);
            AddLowPolyTree(parent, new Vector3(-20f, 0f, 15f), 0.92f, -12f);
            AddLowPolyTree(parent, new Vector3(-8f, 0f, 18f), 0.82f, 22f);
            AddLowPolyTree(parent, new Vector3(19f, 0f, 17f), 0.9f, -20f);
        }

        private static void AddModernMart(Transform parent, Vector3 position, float yaw)
        {
            GameObject root = CreateModelRoot(parent, "3D Modern Mart", position, yaw);
            CreateBox(root.transform, "Foundation", new Vector3(0f, 0.25f, 0f), new Vector3(17f, 0.5f, 9f), concreteMaterial);
            CreateBox(root.transform, "Main Building", new Vector3(0f, 3.25f, 0f), new Vector3(16f, 6f, 8f), whiteMaterial);
            CreateBox(root.transform, "Roof Band", new Vector3(0f, 6.35f, 0f), new Vector3(17f, 0.65f, 9f), blueMaterial);
            CreateBox(root.transform, "Red Accent", new Vector3(0f, 4.8f, -4.08f), new Vector3(16f, 0.55f, 0.2f), redMaterial);
            CreateBox(root.transform, "Store Sign", new Vector3(0f, 6.2f, -4.55f), new Vector3(10f, 1.8f, 0.35f), blueMaterial);
            CreateBox(root.transform, "Left Window", new Vector3(-5f, 2.7f, -4.15f), new Vector3(4.3f, 3.4f, 0.18f), glassMaterial);
            CreateBox(root.transform, "Right Window", new Vector3(5f, 2.7f, -4.15f), new Vector3(4.3f, 3.4f, 0.18f), glassMaterial);
            CreateBox(root.transform, "Door", new Vector3(0f, 2.25f, -4.25f), new Vector3(2.8f, 4.2f, 0.22f), darkMaterial);
            CreateBox(root.transform, "Door Glass", new Vector3(0f, 2.45f, -4.4f), new Vector3(2.2f, 3.3f, 0.08f), glassMaterial);
            AddRoofUnit(root.transform, new Vector3(4f, 7.2f, 1f));
            AddFadeVolume(root, new Vector3(0f, 3.8f, 0f), new Vector3(18f, 8.5f, 10f), 0.28f);
        }

        private static void AddApartment(
            Transform parent,
            string name,
            Vector3 position,
            float yaw,
            float width,
            float depth,
            float height,
            Material wallMaterial)
        {
            GameObject root = CreateModelRoot(parent, name, position, yaw);
            CreateBox(root.transform, "Foundation", new Vector3(0f, 0.3f, 0f), new Vector3(width + 0.8f, 0.6f, depth + 0.8f), concreteMaterial);
            CreateBox(root.transform, "Apartment Body", new Vector3(0f, height * 0.5f, 0f), new Vector3(width, height, depth), wallMaterial);
            CreateBox(root.transform, "Roof", new Vector3(0f, height + 0.35f, 0f), new Vector3(width + 0.8f, 0.7f, depth + 0.8f), darkMaterial);
            CreateBox(root.transform, "Entrance", new Vector3(0f, 1.5f, -depth * 0.52f), new Vector3(2.8f, 3f, 0.25f), darkMaterial);

            int floorCount = Mathf.Max(2, Mathf.FloorToInt(height / 3f));
            for (int floor = 0; floor < floorCount; floor++)
            {
                float y = 2f + floor * 2.7f;
                for (int column = -2; column <= 2; column++)
                {
                    CreateBox(
                        root.transform,
                        "Apartment Window",
                        new Vector3(column * width * 0.17f, y, -depth * 0.51f),
                        new Vector3(1.5f, 1.25f, 0.18f),
                        glassMaterial);
                }

                CreateBox(
                    root.transform,
                    "Balcony",
                    new Vector3(0f, y - 0.85f, -depth * 0.58f),
                    new Vector3(width * 0.88f, 0.18f, 1.2f),
                    concreteMaterial);
            }

            AddRoofUnit(root.transform, new Vector3(width * 0.25f, height + 1.2f, 0f));
            AddFadeVolume(root, new Vector3(0f, height * 0.5f, 0f), new Vector3(width + 1.5f, height + 2.5f, depth + 1.5f), 0.25f);
        }

        private static void AddModernSchool(Transform parent, Vector3 position, float yaw)
        {
            GameObject root = CreateModelRoot(parent, "3D Modern School", position, yaw);
            CreateBox(root.transform, "Foundation", new Vector3(0f, 0.25f, 0f), new Vector3(22f, 0.5f, 10f), concreteMaterial);
            CreateBox(root.transform, "School Body", new Vector3(0f, 5f, 0f), new Vector3(21f, 10f, 9f), whiteMaterial);
            CreateBox(root.transform, "Blue Floor Band", new Vector3(0f, 3.5f, -4.6f), new Vector3(21f, 0.45f, 0.25f), blueMaterial);
            CreateBox(root.transform, "Yellow Floor Band", new Vector3(0f, 6.5f, -4.62f), new Vector3(21f, 0.45f, 0.25f), yellowMaterial);
            CreateBox(root.transform, "Roof", new Vector3(0f, 10.35f, 0f), new Vector3(22f, 0.7f, 10f), darkMaterial);

            for (int floor = 0; floor < 3; floor++)
            {
                for (int column = -3; column <= 3; column++)
                {
                    CreateBox(
                        root.transform,
                        "Classroom Window",
                        new Vector3(column * 2.7f, 2.2f + floor * 3f, -4.68f),
                        new Vector3(1.9f, 1.45f, 0.18f),
                        glassMaterial);
                }
            }

            CreateBox(root.transform, "Entrance Canopy", new Vector3(0f, 3.2f, -5.5f), new Vector3(5.5f, 0.35f, 2f), yellowMaterial);
            CreateBox(root.transform, "Entrance", new Vector3(0f, 1.8f, -4.8f), new Vector3(3.8f, 3.6f, 0.25f), blueMaterial);
            AddFlagPole(root.transform, new Vector3(7.8f, 0f, -5.7f));
            AddFadeVolume(root, new Vector3(0f, 5.3f, 0f), new Vector3(23f, 12f, 11f), 0.25f);
        }

        private static void AddModernChurch(Transform parent, Vector3 position, float yaw)
        {
            GameObject root = CreateModelRoot(parent, "3D Modern Church", position, yaw);
            CreateBox(root.transform, "Foundation", new Vector3(0f, 0.25f, 0f), new Vector3(15f, 0.5f, 10f), concreteMaterial);
            CreateBox(root.transform, "Church Body", new Vector3(0f, 4f, 0f), new Vector3(13.5f, 8f, 8.5f), whiteMaterial);
            AddGabledRoof(root.transform, new Vector3(0f, 8.5f, 0f), 15f, 10f, blueMaterial);
            CreateBox(root.transform, "Front Tower", new Vector3(0f, 7.2f, -4.5f), new Vector3(4.5f, 8.5f, 3.5f), whiteMaterial);
            AddGabledRoof(root.transform, new Vector3(0f, 11.7f, -4.5f), 5.2f, 4.6f, darkMaterial);
            CreateBox(root.transform, "Church Door", new Vector3(0f, 2.2f, -6.3f), new Vector3(2.8f, 4.2f, 0.28f), woodMaterial);
            CreatePrimitive(root.transform, "Round Window", PrimitiveType.Cylinder, new Vector3(0f, 7.2f, -6.25f), new Vector3(1.15f, 0.12f, 1.15f), Quaternion.Euler(90f, 0f, 0f), glassMaterial);
            AddCross(root.transform, new Vector3(0f, 14.1f, -4.5f));
            AddFadeVolume(root, new Vector3(0f, 7f, -1f), new Vector3(16f, 15.5f, 13f), 0.25f);
        }

        private static void AddTrafficLight(Transform parent, Vector3 position, float yaw)
        {
            GameObject root = CreateModelRoot(parent, "Traffic Light", position, yaw);
            CreatePrimitive(root.transform, "Pole", PrimitiveType.Cylinder, new Vector3(0f, 2.7f, 0f), new Vector3(0.16f, 2.7f, 0.16f), metalMaterial);
            CreateBox(root.transform, "Arm", new Vector3(1.2f, 5.2f, 0f), new Vector3(2.5f, 0.16f, 0.16f), metalMaterial);
            CreateBox(root.transform, "Signal Box", new Vector3(2.2f, 4.45f, 0f), new Vector3(0.75f, 1.8f, 0.7f), darkMaterial);
            CreatePrimitive(root.transform, "Red Light", PrimitiveType.Sphere, new Vector3(2.2f, 5.02f, -0.36f), Vector3.one * 0.38f, redMaterial);
            CreatePrimitive(root.transform, "Yellow Light", PrimitiveType.Sphere, new Vector3(2.2f, 4.45f, -0.36f), Vector3.one * 0.38f, yellowMaterial);
            CreatePrimitive(root.transform, "Green Light", PrimitiveType.Sphere, new Vector3(2.2f, 3.88f, -0.36f), Vector3.one * 0.38f, lightGreenMaterial);
        }

        private static void AddBusStop(Transform parent, Vector3 position, float yaw)
        {
            GameObject root = CreateModelRoot(parent, "City Bus Stop", position, yaw);
            CreateBox(root.transform, "Shelter Floor", new Vector3(0f, 0.12f, 0f), new Vector3(7f, 0.24f, 2.8f), concreteMaterial);
            CreateBox(root.transform, "Left Post", new Vector3(-3f, 2f, 0.8f), new Vector3(0.18f, 4f, 0.18f), metalMaterial);
            CreateBox(root.transform, "Right Post", new Vector3(3f, 2f, 0.8f), new Vector3(0.18f, 4f, 0.18f), metalMaterial);
            CreateBox(root.transform, "Back Glass", new Vector3(0f, 2f, 1.15f), new Vector3(6f, 3.7f, 0.12f), glassMaterial);
            CreateBox(root.transform, "Shelter Roof", new Vector3(0f, 4.15f, 0f), new Vector3(7f, 0.25f, 3.2f), blueMaterial);
            CreateBox(root.transform, "Bench Seat", new Vector3(0f, 0.8f, 0.35f), new Vector3(4.5f, 0.28f, 0.8f), woodMaterial);
            CreateBox(root.transform, "Bus Sign Pole", new Vector3(-4f, 1.8f, 0f), new Vector3(0.14f, 3.6f, 0.14f), metalMaterial);
            CreatePrimitive(root.transform, "Bus Sign", PrimitiveType.Cylinder, new Vector3(-4f, 3.45f, 0f), new Vector3(0.65f, 0.08f, 0.65f), Quaternion.Euler(90f, 0f, 0f), blueMaterial);
            AddFadeVolume(root, new Vector3(0f, 2.1f, 0.25f), new Vector3(8.5f, 4.8f, 3.6f), 0.32f);
        }

        private static void AddVendingMachine(Transform parent, Vector3 position, float yaw)
        {
            GameObject root = CreateModelRoot(parent, "Vending Machine", position, yaw);
            CreateBox(root.transform, "Machine Body", new Vector3(0f, 1.5f, 0f), new Vector3(1.8f, 3f, 1.2f), redMaterial);
            CreateBox(root.transform, "Product Window", new Vector3(0f, 2f, -0.63f), new Vector3(1.45f, 1.3f, 0.08f), glassMaterial);
            CreateBox(root.transform, "Payment Panel", new Vector3(0.48f, 1f, -0.65f), new Vector3(0.35f, 0.65f, 0.08f), darkMaterial);
            CreateBox(root.transform, "Product Slot", new Vector3(-0.25f, 0.45f, -0.65f), new Vector3(0.9f, 0.32f, 0.08f), darkMaterial);
        }

        private static void AddParkedCar(Transform parent, Vector3 position, float yaw, Material bodyMaterial)
        {
            GameObject root = CreateModelRoot(parent, "Parked Car", position, yaw);
            CreateBox(root.transform, "Car Body", new Vector3(0f, 0.75f, 0f), new Vector3(4.8f, 1.2f, 2.2f), bodyMaterial);
            CreateBox(root.transform, "Car Cabin", new Vector3(0.25f, 1.65f, 0f), new Vector3(2.7f, 1.1f, 1.85f), glassMaterial);
            CreateBox(root.transform, "Front Bumper", new Vector3(2.5f, 0.55f, 0f), new Vector3(0.2f, 0.4f, 2f), darkMaterial);

            float[] xPositions = { -1.55f, 1.55f };
            float[] zPositions = { -1.08f, 1.08f };
            for (int xIndex = 0; xIndex < xPositions.Length; xIndex++)
            {
                for (int zIndex = 0; zIndex < zPositions.Length; zIndex++)
                {
                    CreatePrimitive(
                        root.transform,
                        "Car Wheel",
                        PrimitiveType.Cylinder,
                        new Vector3(xPositions[xIndex], 0.48f, zPositions[zIndex]),
                        new Vector3(0.48f, 0.18f, 0.48f),
                        Quaternion.Euler(90f, 0f, 0f),
                        rubberMaterial);
                }
            }
        }

        private static void AddLowPolyTree(Transform parent, Vector3 position, float scale, float yaw)
        {
            GameObject root = CreateModelRoot(parent, "3D Street Tree", position, yaw);
            root.transform.localScale = Vector3.one * scale;
            CreatePrimitive(root.transform, "Tree Bed", PrimitiveType.Cylinder, new Vector3(0f, 0.12f, 0f), new Vector3(1.15f, 0.12f, 1.15f), concreteMaterial);
            CreatePrimitive(root.transform, "Trunk", PrimitiveType.Cylinder, new Vector3(0f, 2.3f, 0f), new Vector3(0.42f, 2.3f, 0.42f), trunkMaterial);
            CreatePrimitive(root.transform, "Crown Center", PrimitiveType.Sphere, new Vector3(0f, 5f, 0f), new Vector3(4.2f, 3.2f, 3.8f), darkGreenMaterial);
            CreatePrimitive(root.transform, "Crown Left", PrimitiveType.Sphere, new Vector3(-1.4f, 4.6f, 0.2f), new Vector3(2.8f, 2.6f, 2.7f), lightGreenMaterial);
            CreatePrimitive(root.transform, "Crown Right", PrimitiveType.Sphere, new Vector3(1.35f, 4.7f, -0.2f), new Vector3(2.9f, 2.5f, 2.8f), lightGreenMaterial);
            AddFadeVolume(
                root,
                new Vector3(0f, 4.9f, 0f),
                new Vector3(5.8f, 4.2f, 5.3f),
                0.3f,
                GetNamedRenderers(root.transform, "Crown"));
        }

        private static void AddPlanter(Transform parent, Vector3 position, float yaw)
        {
            GameObject root = CreateModelRoot(parent, "City Planter", position, yaw);
            CreateBox(root.transform, "Planter Box", new Vector3(0f, 0.45f, 0f), new Vector3(5f, 0.9f, 1.8f), concreteMaterial);
            for (int index = -2; index <= 2; index++)
            {
                CreatePrimitive(root.transform, "Planter Shrub", PrimitiveType.Sphere, new Vector3(index * 0.95f, 1.25f, 0f), new Vector3(1.2f, 1.1f, 1.2f), index % 2 == 0 ? darkGreenMaterial : lightGreenMaterial);
            }
        }

        private static void AddPlayground(Transform parent, Vector3 position)
        {
            GameObject root = CreateModelRoot(parent, "Residential Playground", position, 0f);
            CreateBox(root.transform, "Playground Mat", new Vector3(0f, 0.06f, 0f), new Vector3(16f, 0.12f, 8f), yellowMaterial);
            AddSwing(root.transform, new Vector3(-4f, 0f, 0f));
            AddSlide(root.transform, new Vector3(4f, 0f, 0f));
        }

        private static void AddSwing(Transform parent, Vector3 position)
        {
            GameObject root = CreateModelRoot(parent, "Swing Set", position, 0f);
            CreateBox(root.transform, "Left Frame", new Vector3(-2f, 2f, 0f), new Vector3(0.2f, 4f, 0.2f), blueMaterial);
            CreateBox(root.transform, "Right Frame", new Vector3(2f, 2f, 0f), new Vector3(0.2f, 4f, 0.2f), blueMaterial);
            CreateBox(root.transform, "Top Beam", new Vector3(0f, 3.9f, 0f), new Vector3(4.4f, 0.22f, 0.22f), blueMaterial);
            CreateBox(root.transform, "Left Rope", new Vector3(-0.8f, 2.6f, 0f), new Vector3(0.06f, 2.4f, 0.06f), metalMaterial);
            CreateBox(root.transform, "Right Rope", new Vector3(0.8f, 2.6f, 0f), new Vector3(0.06f, 2.4f, 0.06f), metalMaterial);
            CreateBox(root.transform, "Swing Seat", new Vector3(0f, 1.4f, 0f), new Vector3(1.9f, 0.18f, 0.8f), redMaterial);
        }

        private static void AddSlide(Transform parent, Vector3 position)
        {
            GameObject root = CreateModelRoot(parent, "Playground Slide", position, 0f);
            CreateBox(root.transform, "Platform", new Vector3(-1f, 2.5f, 0f), new Vector3(2f, 0.3f, 2f), blueMaterial);
            CreateBox(root.transform, "Left Leg", new Vector3(-1.6f, 1.25f, 0.6f), new Vector3(0.2f, 2.5f, 0.2f), metalMaterial);
            CreateBox(root.transform, "Right Leg", new Vector3(-0.4f, 1.25f, 0.6f), new Vector3(0.2f, 2.5f, 0.2f), metalMaterial);
            CreatePrimitive(root.transform, "Slide Ramp", PrimitiveType.Cube, new Vector3(1f, 1.4f, 0f), new Vector3(4.5f, 0.25f, 1.5f), Quaternion.Euler(0f, 0f, -28f), redMaterial);
        }

        private static void AddSchoolGate(Transform parent, Vector3 position, float yaw)
        {
            GameObject root = CreateModelRoot(parent, "School Gate", position, yaw);
            CreateBox(root.transform, "Left Gate Post", new Vector3(-2.8f, 1.5f, 0f), new Vector3(0.8f, 3f, 0.8f), concreteMaterial);
            CreateBox(root.transform, "Right Gate Post", new Vector3(2.8f, 1.5f, 0f), new Vector3(0.8f, 3f, 0.8f), concreteMaterial);
            CreateBox(root.transform, "Gate Sign", new Vector3(0f, 3.25f, 0f), new Vector3(6.4f, 0.7f, 0.5f), blueMaterial);
        }

        private static void AddBikeRack(Transform parent, Vector3 position, float yaw)
        {
            GameObject root = CreateModelRoot(parent, "Bike Rack", position, yaw);
            CreateBox(root.transform, "Rack Base", new Vector3(0f, 0.15f, 0f), new Vector3(5f, 0.3f, 1f), concreteMaterial);
            for (int index = -2; index <= 2; index++)
            {
                CreateBox(root.transform, "Rack Bar", new Vector3(index, 0.75f, 0f), new Vector3(0.1f, 1.3f, 0.8f), metalMaterial);
            }
        }

        private static void AddBasketballGoal(Transform parent, Vector3 position, float yaw)
        {
            GameObject root = CreateModelRoot(parent, "Basketball Goal", position, yaw);
            CreateBox(root.transform, "Goal Pole", new Vector3(0f, 2.2f, 0f), new Vector3(0.22f, 4.4f, 0.22f), metalMaterial);
            CreateBox(root.transform, "Backboard", new Vector3(0f, 4.3f, -0.5f), new Vector3(2.4f, 1.5f, 0.14f), whiteMaterial);
            CreatePrimitive(
                root.transform,
                "Basket Ring",
                PrimitiveType.Cylinder,
                new Vector3(0f, 3.8f, -1.15f),
                new Vector3(0.65f, 0.06f, 0.65f),
                Quaternion.Euler(90f, 0f, 0f),
                redMaterial);
        }

        private static void AddFountain(Transform parent, Vector3 position)
        {
            GameObject root = CreateModelRoot(parent, "Park Fountain", position, 0f);
            CreatePrimitive(root.transform, "Fountain Base", PrimitiveType.Cylinder, new Vector3(0f, 0.35f, 0f), new Vector3(4f, 0.35f, 4f), concreteMaterial);
            CreatePrimitive(root.transform, "Fountain Water", PrimitiveType.Cylinder, new Vector3(0f, 0.72f, 0f), new Vector3(3.4f, 0.08f, 3.4f), waterMaterial);
            CreatePrimitive(root.transform, "Center Pillar", PrimitiveType.Cylinder, new Vector3(0f, 1.6f, 0f), new Vector3(0.55f, 1.2f, 0.55f), concreteMaterial);
            CreatePrimitive(root.transform, "Fountain Top", PrimitiveType.Sphere, new Vector3(0f, 2.85f, 0f), Vector3.one * 0.9f, waterMaterial);
        }

        private static void AddGazebo(Transform parent, Vector3 position)
        {
            GameObject root = CreateModelRoot(parent, "Park Gazebo", position, 0f);
            CreatePrimitive(root.transform, "Gazebo Floor", PrimitiveType.Cylinder, new Vector3(0f, 0.2f, 0f), new Vector3(4.5f, 0.2f, 4.5f), concreteMaterial);
            for (int index = 0; index < 6; index++)
            {
                float angle = index * Mathf.PI * 2f / 6f;
                Vector3 postPosition = new Vector3(Mathf.Cos(angle) * 3.5f, 2.25f, Mathf.Sin(angle) * 3.5f);
                CreatePrimitive(root.transform, "Gazebo Post", PrimitiveType.Cylinder, postPosition, new Vector3(0.16f, 2.2f, 0.16f), whiteMaterial);
            }

            CreatePrimitive(root.transform, "Gazebo Roof", PrimitiveType.Cylinder, new Vector3(0f, 4.65f, 0f), new Vector3(5f, 0.3f, 5f), blueMaterial);
            CreatePrimitive(root.transform, "Gazebo Table", PrimitiveType.Cylinder, new Vector3(0f, 1f, 0f), new Vector3(1.6f, 0.15f, 1.6f), woodMaterial);
            AddFadeVolume(root, new Vector3(0f, 2.5f, 0f), new Vector3(10f, 5.3f, 10f), 0.32f);
        }

        private static void AddRoofUnit(Transform parent, Vector3 position)
        {
            CreateBox(parent, "Roof Utility Unit", position, new Vector3(3f, 1.4f, 2f), concreteMaterial);
            CreatePrimitive(parent, "Roof Unit Fan", PrimitiveType.Cylinder, position + new Vector3(0f, 0.75f, 0f), new Vector3(0.7f, 0.08f, 0.7f), darkMaterial);
        }

        private static void AddFlagPole(Transform parent, Vector3 position)
        {
            GameObject root = CreateModelRoot(parent, "School Flag Pole", position, 0f);
            CreatePrimitive(root.transform, "Pole", PrimitiveType.Cylinder, new Vector3(0f, 4f, 0f), new Vector3(0.1f, 4f, 0.1f), metalMaterial);
            CreateBox(root.transform, "Flag", new Vector3(1f, 7.1f, 0f), new Vector3(2f, 1.2f, 0.08f), blueMaterial);
            CreatePrimitive(root.transform, "Finial", PrimitiveType.Sphere, new Vector3(0f, 8.1f, 0f), Vector3.one * 0.28f, yellowMaterial);
        }

        private static void AddCross(Transform parent, Vector3 position)
        {
            GameObject root = CreateModelRoot(parent, "Church Cross", position, 0f);
            CreateBox(root.transform, "Cross Vertical", Vector3.zero, new Vector3(0.35f, 3f, 0.35f), yellowMaterial);
            CreateBox(root.transform, "Cross Horizontal", new Vector3(0f, 0.35f, 0f), new Vector3(1.8f, 0.35f, 0.35f), yellowMaterial);
        }

        private static void AddGabledRoof(Transform parent, Vector3 center, float width, float depth, Material material)
        {
            float halfDepth = depth * 0.5f;
            float panelDepth = halfDepth + 0.8f;
            CreatePrimitive(parent, "Roof Front", PrimitiveType.Cube, center + new Vector3(0f, 0f, -halfDepth * 0.48f), new Vector3(width, 0.35f, panelDepth), Quaternion.Euler(-22f, 0f, 0f), material);
            CreatePrimitive(parent, "Roof Back", PrimitiveType.Cube, center + new Vector3(0f, 0f, halfDepth * 0.48f), new Vector3(width, 0.35f, panelDepth), Quaternion.Euler(22f, 0f, 0f), material);
        }

        private static GameObject CreateModelRoot(Transform parent, string name, Vector3 position, float yaw)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = position;
            root.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            return root;
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

        private static GameObject CreateBox(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
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

        private static void RemoveSpriteHierarchy(GameObject prefabRoot)
        {
            SpriteRenderer[] renderers = prefabRoot.GetComponentsInChildren<SpriteRenderer>(true);
            HashSet<GameObject> rootsToRemove = new HashSet<GameObject>();

            for (int index = 0; index < renderers.Length; index++)
            {
                Transform topLevel = renderers[index].transform;
                while (topLevel.parent != null && topLevel.parent != prefabRoot.transform)
                {
                    topLevel = topLevel.parent;
                }

                if (topLevel != prefabRoot.transform)
                {
                    rootsToRemove.Add(topLevel.gameObject);
                }
            }

            foreach (GameObject objectToRemove in rootsToRemove)
            {
                Object.DestroyImmediate(objectToRemove);
            }
        }

        private static void RemovePreviousGeneratedModels(Transform prefabRoot)
        {
            Transform previousRoot = prefabRoot.Find("Modern 3D Decorations");
            if (previousRoot != null)
            {
                Object.DestroyImmediate(previousRoot.gameObject);
            }
        }

        private static bool ContainsTransformName(Transform root, string name)
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < transforms.Length; index++)
            {
                if (transforms[index].name == name)
                {
                    return true;
                }
            }

            return false;
        }

        private static void CreateMaterials()
        {
            concreteMaterial = CreateMaterial("ModernCity3D_Concrete.mat", new Color32(0xb8, 0xb8, 0xb3, 0xff));
            whiteMaterial = CreateMaterial("ModernCity3D_White.mat", new Color32(0xe9, 0xe9, 0xe6, 0xff));
            darkMaterial = CreateMaterial("ModernCity3D_Dark.mat", new Color32(0x35, 0x3b, 0x43, 0xff));
            glassMaterial = CreateMaterial("ModernCity3D_Glass.mat", new Color32(0x78, 0xb7, 0xd3, 0xff));
            redMaterial = CreateMaterial("ModernCity3D_Red.mat", new Color32(0xd8, 0x4c, 0x4c, 0xff));
            blueMaterial = CreateMaterial("ModernCity3D_Blue.mat", new Color32(0x3d, 0x83, 0xb8, 0xff));
            yellowMaterial = CreateMaterial("ModernCity3D_Yellow.mat", new Color32(0xf0, 0xc4, 0x55, 0xff));
            brickMaterial = CreateMaterial("ModernCity3D_Brick.mat", new Color32(0xb8, 0x78, 0x62, 0xff));
            lightGreenMaterial = CreateMaterial("ModernCity3D_FoliageLight.mat", new Color32(0x72, 0xa9, 0x4b, 0xff));
            darkGreenMaterial = CreateMaterial("ModernCity3D_FoliageDark.mat", new Color32(0x3f, 0x7f, 0x42, 0xff));
            trunkMaterial = CreateMaterial("ModernCity3D_Trunk.mat", new Color32(0x58, 0x3c, 0x31, 0xff));
            waterMaterial = CreateMaterial("ModernCity3D_Water.mat", new Color32(0x55, 0xb7, 0xd3, 0xff));
            rubberMaterial = CreateMaterial("ModernCity3D_Rubber.mat", new Color32(0x20, 0x23, 0x26, 0xff));
            metalMaterial = LoadOrCreateMaterial("ModernCity_Metal.mat", new Color32(0x3f, 0x48, 0x53, 0xff));
            woodMaterial = LoadOrCreateMaterial("ModernCity_Bench.mat", new Color32(0x9b, 0x65, 0x3f, 0xff));
            lampMaterial = LoadOrCreateMaterial("ModernCity_Lamp.mat", new Color32(0xff, 0xd4, 0x73, 0xff));
        }

        private static Material CreateMaterial(string fileName, Color color)
        {
            return LoadOrCreateMaterial(fileName, color);
        }

        private static Material LoadOrCreateMaterial(string fileName, Color color)
        {
            string assetPath = MaterialRoot + "/" + fileName;
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
                float smoothness = fileName.Contains("Glass") || fileName.Contains("Water") ? 0.65f : 0.08f;
                material.SetFloat("_Smoothness", smoothness);
            }

            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
