using System;
using System.IO;
using IsekaiTruck.Visuals;
using IsekaiTruck.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace IsekaiTruck.Editor
{
    public static class ModernCityChunkPrototypeSetup
    {
        private const string ArtRoot = "Assets/IsekaiTruck/Art/World/ModernCity";
        private const string MaterialRoot = ArtRoot + "/Materials";
        private const string PreviewRoot = ArtRoot + "/Previews";
        private const string PrefabRoot = "Assets/IsekaiTruck/Prefabs/World/ModernCity";
        private const string SceneRoot = "Assets/IsekaiTruck/Scenes/Prototypes";
        private const string PreviewScenePath = SceneRoot + "/ModernCityChunksPreview.unity";

        private const string TreeTexturePath = ArtRoot + "/ModernCityTree.png";
        private const string ChurchTexturePath = ArtRoot + "/ModernCityChurch.png";
        private const string BuildingTexturePath = ArtRoot + "/ModernCityBuilding.png";
        private const string SchoolTexturePath = ArtRoot + "/ModernCitySchool.png";
        private const string MartTexturePath = ArtRoot + "/ModernCityMart.png";

        private const string CrossroadPrefabPath = PrefabRoot + "/ModernCity_Crossroad.prefab";
        private const string MartStreetPrefabPath = PrefabRoot + "/ModernCity_MartStreet.prefab";
        private const string ResidentialPrefabPath = PrefabRoot + "/ModernCity_Residential.prefab";
        private const string SchoolZonePrefabPath = PrefabRoot + "/ModernCity_SchoolZone.prefab";
        private const string ChurchParkPrefabPath = PrefabRoot + "/ModernCity_ChurchPark.prefab";

        private const float ChunkSize = 50f;
        private const float RoadWidth = 12f;

        private static Material grassMaterial;
        private static Material roadMaterial;
        private static Material sidewalkMaterial;
        private static Material roadLineMaterial;
        private static Material poleMaterial;
        private static Material lampMaterial;
        private static Material benchMaterial;
        private static Material contactShadowMaterial;

        [MenuItem("Isekai Truck/Prototype/Build Modern City Chunks")]
        public static void Setup()
        {
            if (EditorApplication.isPlaying)
            {
                throw new InvalidOperationException("플레이 모드를 종료한 뒤 현대 도시 청크를 생성해주세요.");
            }

            EnsureFolders();
            DeleteObsoletePrototypeAssets();
            ConfigureSprite(TreeTexturePath);
            ConfigureSprite(ChurchTexturePath);
            ConfigureSprite(BuildingTexturePath);
            ConfigureSprite(SchoolTexturePath);
            ConfigureSprite(MartTexturePath);
            CreateMaterials();

            Sprite treeSprite = LoadSprite(TreeTexturePath);
            Sprite churchSprite = LoadSprite(ChurchTexturePath);
            Sprite buildingSprite = LoadSprite(BuildingTexturePath);
            Sprite schoolSprite = LoadSprite(SchoolTexturePath);
            Sprite martSprite = LoadSprite(MartTexturePath);

            GameObject crossroadPrefab = CreateCrossroadPrefab(treeSprite);
            GameObject martStreetPrefab = CreateMartStreetPrefab(martSprite, treeSprite);
            GameObject residentialPrefab = CreateResidentialPrefab(buildingSprite, treeSprite);
            GameObject schoolZonePrefab = CreateSchoolZonePrefab(schoolSprite, treeSprite);
            GameObject churchParkPrefab = CreateChurchParkPrefab(churchSprite, treeSprite);

            ModernCity3DModelSetup.ApplyToPrefabs();

            CreatePreviewScene(
                crossroadPrefab,
                martStreetPrefab,
                residentialPrefab,
                schoolZonePrefab,
                churchParkPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Verify();

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog(
                    "Isekai Truck",
                    "2.5D 현대 도시 프로토타입 청크 5개와 프리뷰를 생성했습니다.",
                    "확인");
            }
        }

        [MenuItem("Isekai Truck/Prototype/Verify Modern City Chunks")]
        public static void Verify()
        {
            VerifyPrefab(
                CrossroadPrefabPath,
                RoadConnection.North | RoadConnection.East | RoadConnection.South | RoadConnection.West);
            VerifyPrefab(
                MartStreetPrefabPath,
                RoadConnection.East | RoadConnection.West);
            VerifyPrefab(
                ResidentialPrefabPath,
                RoadConnection.East | RoadConnection.West);
            VerifyPrefab(
                SchoolZonePrefabPath,
                RoadConnection.East | RoadConnection.West);
            VerifyPrefab(
                ChurchParkPrefabPath,
                RoadConnection.East | RoadConnection.West);

            VerifyPreview(PreviewRoot + "/ModernCity_Crossroad_Preview.png");
            VerifyPreview(PreviewRoot + "/ModernCity_MartStreet_Preview.png");
            VerifyPreview(PreviewRoot + "/ModernCity_Residential_Preview.png");
            VerifyPreview(PreviewRoot + "/ModernCity_SchoolZone_Preview.png");
            VerifyPreview(PreviewRoot + "/ModernCity_ChurchPark_Preview.png");

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(PreviewScenePath) == null)
            {
                throw new InvalidOperationException("현대 도시 프리뷰 씬이 없습니다.");
            }

            Debug.Log("Modern city chunk prototype verification passed.");
        }

        private static GameObject CreateCrossroadPrefab(Sprite treeSprite)
        {
            GameObject root = CreateChunkRoot(
                "ModernCity_Crossroad",
                RoadConnection.North | RoadConnection.East | RoadConnection.South | RoadConnection.West);

            try
            {
                AddGround(root.transform);
                AddHorizontalRoad(root.transform, 0f);
                AddVerticalRoad(root.transform, 0f);
                AddCrossroadSidewalks(root.transform);
                AddHorizontalRoadDashes(root.transform, 0f, true);
                AddVerticalRoadDashes(root.transform, 0f, true);
                AddCrosswalk(root.transform, new Vector3(0f, 0.09f, 8.7f), false);
                AddCrosswalk(root.transform, new Vector3(0f, 0.09f, -8.7f), false);
                AddCrosswalk(root.transform, new Vector3(8.7f, 0.09f, 0f), true);
                AddCrosswalk(root.transform, new Vector3(-8.7f, 0.09f, 0f), true);

                AddTree(root.transform, treeSprite, new Vector3(-18f, 0.12f, -18f), 0.36f);
                AddTree(root.transform, treeSprite, new Vector3(18f, 0.12f, -18f), 0.34f);
                AddTree(root.transform, treeSprite, new Vector3(-18f, 0.12f, 18f), 0.32f);
                AddTree(root.transform, treeSprite, new Vector3(18f, 0.12f, 18f), 0.37f);

                AddStreetLamp(root.transform, new Vector3(-9f, 0f, -9f));
                AddStreetLamp(root.transform, new Vector3(9f, 0f, -9f));
                AddStreetLamp(root.transform, new Vector3(-9f, 0f, 9f));
                AddStreetLamp(root.transform, new Vector3(9f, 0f, 9f));

                return PrefabUtility.SaveAsPrefabAsset(root, CrossroadPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static GameObject CreateMartStreetPrefab(Sprite martSprite, Sprite treeSprite)
        {
            GameObject root = CreateChunkRoot(
                "ModernCity_MartStreet",
                RoadConnection.East | RoadConnection.West);

            try
            {
                AddGround(root.transform);
                AddHorizontalRoad(root.transform, 0f);
                AddHorizontalSidewalks(root.transform, 0f);
                AddHorizontalRoadDashes(root.transform, 0f, false);

                CreateBox(
                    root.transform,
                    "Mart Parking Lot",
                    new Vector3(12f, 0.045f, -17f),
                    new Vector3(20f, 0.05f, 11f),
                    sidewalkMaterial);
                AddParkingLines(root.transform, new Vector3(12f, 0.09f, -17f), 4);
                SpriteRenderer martRenderer = AddBillboard(
                    root.transform,
                    "Modern Mart",
                    martSprite,
                    new Vector3(-11f, 0.12f, -18.5f),
                    2.15f);
                martRenderer.flipX = true;

                AddTree(root.transform, treeSprite, new Vector3(-20f, 0.12f, 15f), 0.34f);
                AddTree(root.transform, treeSprite, new Vector3(-7f, 0.12f, 16f), 0.32f);
                AddTree(root.transform, treeSprite, new Vector3(7f, 0.12f, 15f), 0.36f);
                AddTree(root.transform, treeSprite, new Vector3(20f, 0.12f, 16f), 0.33f);

                AddStreetLamp(root.transform, new Vector3(-11f, 0f, -8f));
                AddStreetLamp(root.transform, new Vector3(11f, 0f, -8f));
                AddStreetLamp(root.transform, new Vector3(-11f, 0f, 8f));
                AddStreetLamp(root.transform, new Vector3(11f, 0f, 8f));

                AddBench(root.transform, new Vector3(-1f, 0.15f, 11f), 0f);
                AddBench(root.transform, new Vector3(14f, 0.15f, 11f), 180f);

                return PrefabUtility.SaveAsPrefabAsset(root, MartStreetPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static GameObject CreateResidentialPrefab(Sprite buildingSprite, Sprite treeSprite)
        {
            GameObject root = CreateChunkRoot(
                "ModernCity_Residential",
                RoadConnection.East | RoadConnection.West);

            try
            {
                AddGround(root.transform);
                AddHorizontalRoad(root.transform, 0f);
                AddHorizontalSidewalks(root.transform, 0f);
                AddHorizontalRoadDashes(root.transform, 0f, false);

                CreateBox(
                    root.transform,
                    "Residential Plaza",
                    new Vector3(0f, 0.045f, -18f),
                    new Vector3(40f, 0.05f, 11f),
                    sidewalkMaterial);

                AddBillboard(root.transform, "Apartment Left", buildingSprite, new Vector3(-12f, 0.12f, -18.5f), 2.05f);
                AddBillboard(root.transform, "Apartment Right", buildingSprite, new Vector3(12f, 0.12f, -18.5f), 1.9f);

                AddTree(root.transform, treeSprite, new Vector3(-20f, 0.12f, 15f), 0.36f);
                AddTree(root.transform, treeSprite, new Vector3(-7f, 0.12f, 17f), 0.32f);
                AddTree(root.transform, treeSprite, new Vector3(7f, 0.12f, 15f), 0.35f);
                AddTree(root.transform, treeSprite, new Vector3(20f, 0.12f, 17f), 0.32f);

                AddBench(root.transform, new Vector3(-13f, 0.15f, 10f), 0f);
                AddBench(root.transform, new Vector3(0f, 0.15f, 12f), 180f);
                AddBench(root.transform, new Vector3(13f, 0.15f, 10f), 0f);
                AddStreetLamp(root.transform, new Vector3(-18f, 0f, -8f));
                AddStreetLamp(root.transform, new Vector3(0f, 0f, -8f));
                AddStreetLamp(root.transform, new Vector3(18f, 0f, -8f));

                return PrefabUtility.SaveAsPrefabAsset(root, ResidentialPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static GameObject CreateSchoolZonePrefab(Sprite schoolSprite, Sprite treeSprite)
        {
            GameObject root = CreateChunkRoot(
                "ModernCity_SchoolZone",
                RoadConnection.East | RoadConnection.West);

            try
            {
                AddGround(root.transform);
                AddHorizontalRoad(root.transform, 0f);
                AddHorizontalSidewalks(root.transform, 0f);
                AddHorizontalRoadDashes(root.transform, 0f, false);
                AddCrosswalk(root.transform, new Vector3(14f, 0.09f, 0f), true);

                CreateBox(
                    root.transform,
                    "School Yard",
                    new Vector3(5f, 0.045f, -17f),
                    new Vector3(34f, 0.05f, 12f),
                    sidewalkMaterial);
                AddBillboard(root.transform, "Modern School", schoolSprite, new Vector3(-9f, 0.12f, -18f), 2.2f);

                AddTree(root.transform, treeSprite, new Vector3(-20f, 0.12f, 15f), 0.34f);
                AddTree(root.transform, treeSprite, new Vector3(-7f, 0.12f, 16f), 0.31f);
                AddTree(root.transform, treeSprite, new Vector3(7f, 0.12f, 15f), 0.35f);
                AddTree(root.transform, treeSprite, new Vector3(20f, 0.12f, 16f), 0.32f);

                AddStreetLamp(root.transform, new Vector3(-18f, 0f, -8f));
                AddStreetLamp(root.transform, new Vector3(0f, 0f, -8f));
                AddStreetLamp(root.transform, new Vector3(18f, 0f, -8f));
                AddBench(root.transform, new Vector3(-10f, 0.15f, 10f), 0f);
                AddBench(root.transform, new Vector3(10f, 0.15f, 10f), 180f);

                return PrefabUtility.SaveAsPrefabAsset(root, SchoolZonePrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static GameObject CreateChurchParkPrefab(Sprite churchSprite, Sprite treeSprite)
        {
            GameObject root = CreateChunkRoot(
                "ModernCity_ChurchPark",
                RoadConnection.East | RoadConnection.West);

            try
            {
                AddGround(root.transform);
                AddHorizontalRoad(root.transform, 0f);
                AddHorizontalSidewalks(root.transform, 0f);
                AddHorizontalRoadDashes(root.transform, 0f, false);

                CreateBox(
                    root.transform,
                    "Church Plaza",
                    new Vector3(-10f, 0.045f, -17f),
                    new Vector3(24f, 0.05f, 12f),
                    sidewalkMaterial);
                AddBillboard(root.transform, "Modern Church", churchSprite, new Vector3(-11f, 0.12f, -18f), 2.1f);

                AddTree(root.transform, treeSprite, new Vector3(13f, 0.12f, -17f), 0.36f);
                AddTree(root.transform, treeSprite, new Vector3(21f, 0.12f, -13f), 0.32f);
                AddTree(root.transform, treeSprite, new Vector3(-20f, 0.12f, 15f), 0.34f);
                AddTree(root.transform, treeSprite, new Vector3(-7f, 0.12f, 16f), 0.31f);
                AddTree(root.transform, treeSprite, new Vector3(7f, 0.12f, 15f), 0.35f);
                AddTree(root.transform, treeSprite, new Vector3(20f, 0.12f, 16f), 0.32f);

                AddBench(root.transform, new Vector3(12f, 0.15f, -10f), 0f);
                AddBench(root.transform, new Vector3(17f, 0.15f, 10f), 180f);
                AddStreetLamp(root.transform, new Vector3(-18f, 0f, -8f));
                AddStreetLamp(root.transform, new Vector3(0f, 0f, -8f));
                AddStreetLamp(root.transform, new Vector3(18f, 0f, -8f));

                return PrefabUtility.SaveAsPrefabAsset(root, ChurchParkPrefabPath);
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
                new Vector3(0f, -0.06f, 0f),
                new Vector3(ChunkSize, 0.1f, ChunkSize),
                grassMaterial);
        }

        private static void AddHorizontalRoad(Transform parent, float z)
        {
            CreateBox(
                parent,
                "Road Horizontal",
                new Vector3(0f, 0.01f, z),
                new Vector3(ChunkSize, 0.04f, RoadWidth),
                roadMaterial);
        }

        private static void AddVerticalRoad(Transform parent, float x)
        {
            CreateBox(
                parent,
                "Road Vertical",
                new Vector3(x, 0.015f, 0f),
                new Vector3(RoadWidth, 0.05f, ChunkSize),
                roadMaterial);
        }

        private static void AddHorizontalSidewalks(Transform parent, float roadZ)
        {
            CreateBox(parent, "Sidewalk North", new Vector3(0f, 0.055f, roadZ - 7.5f), new Vector3(ChunkSize, 0.07f, 3f), sidewalkMaterial);
            CreateBox(parent, "Sidewalk South", new Vector3(0f, 0.055f, roadZ + 7.5f), new Vector3(ChunkSize, 0.07f, 3f), sidewalkMaterial);
        }

        private static void AddCrossroadSidewalks(Transform parent)
        {
            const float segmentLength = 18.5f;
            const float segmentCenter = 15.75f;
            const float sidewalkOffset = 7.5f;

            float[] signs = { -1f, 1f };
            for (int xIndex = 0; xIndex < signs.Length; xIndex++)
            {
                for (int zIndex = 0; zIndex < signs.Length; zIndex++)
                {
                    float xSign = signs[xIndex];
                    float zSign = signs[zIndex];
                    CreateBox(
                        parent,
                        "Sidewalk Horizontal Segment",
                        new Vector3(xSign * segmentCenter, 0.055f, zSign * sidewalkOffset),
                        new Vector3(segmentLength, 0.07f, 3f),
                        sidewalkMaterial);
                    CreateBox(
                        parent,
                        "Sidewalk Vertical Segment",
                        new Vector3(xSign * sidewalkOffset, 0.06f, zSign * segmentCenter),
                        new Vector3(3f, 0.08f, segmentLength),
                        sidewalkMaterial);
                }
            }
        }

        private static void AddHorizontalRoadDashes(Transform parent, float z, bool skipCenter)
        {
            for (int x = -20; x <= 20; x += 10)
            {
                if (skipCenter && Mathf.Abs(x) < 8)
                {
                    continue;
                }

                CreateBox(
                    parent,
                    "Road Dash Horizontal",
                    new Vector3(x, 0.075f, z),
                    new Vector3(4f, 0.025f, 0.24f),
                    roadLineMaterial);
            }
        }

        private static void AddVerticalRoadDashes(Transform parent, float x, bool skipCenter)
        {
            for (int z = -20; z <= 20; z += 10)
            {
                if (skipCenter && Mathf.Abs(z) < 8)
                {
                    continue;
                }

                CreateBox(
                    parent,
                    "Road Dash Vertical",
                    new Vector3(x, 0.08f, z),
                    new Vector3(0.24f, 0.03f, 4f),
                    roadLineMaterial);
            }
        }

        private static void AddCrosswalk(Transform parent, Vector3 center, bool horizontalStripes)
        {
            for (int index = -2; index <= 2; index++)
            {
                Vector3 position = center;
                Vector3 size;

                if (horizontalStripes)
                {
                    position.z += index * 1.15f;
                    size = new Vector3(4.2f, 0.025f, 0.55f);
                }
                else
                {
                    position.x += index * 1.15f;
                    size = new Vector3(0.55f, 0.025f, 4.2f);
                }

                CreateBox(parent, "Crosswalk Stripe", position, size, roadLineMaterial);
            }
        }

        private static void AddParkingLines(Transform parent, Vector3 center, int spaceCount)
        {
            float startX = center.x - (spaceCount * 2f);
            for (int index = 0; index <= spaceCount; index++)
            {
                CreateBox(
                    parent,
                    "Parking Line",
                    new Vector3(startX + index * 4f, center.y, center.z),
                    new Vector3(0.16f, 0.025f, 8f),
                    roadLineMaterial);
            }
        }

        private static void AddTree(Transform parent, Sprite sprite, Vector3 position, float scale)
        {
            AddBillboard(parent, "Street Tree", sprite, position, scale * 1.3f);
        }

        private static SpriteRenderer AddBillboard(
            Transform parent,
            string name,
            Sprite sprite,
            Vector3 position,
            float scale)
        {
            GameObject anchorObject = new GameObject($"{name} Ground Anchor");
            anchorObject.transform.SetParent(parent, false);
            position.y = AssetDatabase.GetAssetPath(sprite) == TreeTexturePath ? 0.02f : 0.08f;
            anchorObject.transform.localPosition = position;

            GameObject billboardObject = new GameObject(name);
            billboardObject.transform.SetParent(anchorObject.transform, false);
            billboardObject.transform.localPosition = Vector3.zero;
            billboardObject.transform.localScale = Vector3.one * scale;

            SpriteRenderer spriteRenderer = billboardObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.sortingOrder = 0;
            spriteRenderer.spriteSortPoint = SpriteSortPoint.Pivot;

            billboardObject.AddComponent<BillboardSpriteView>();
            WorldSpriteDepthOrder depthOrder = anchorObject.AddComponent<WorldSpriteDepthOrder>();
            depthOrder.Configure(spriteRenderer, 0);
            ModernCityVisualGroundingSetup.CreateContactShadow(
                anchorObject.transform,
                spriteRenderer,
                billboardObject.transform.localScale,
                contactShadowMaterial);
            return spriteRenderer;
        }

        private static void AddStreetLamp(Transform parent, Vector3 position)
        {
            GameObject lampRoot = new GameObject("Street Lamp");
            lampRoot.transform.SetParent(parent, false);
            lampRoot.transform.localPosition = position;

            CreatePrimitive(
                lampRoot.transform,
                "Pole",
                PrimitiveType.Cylinder,
                new Vector3(0f, 2f, 0f),
                new Vector3(0.18f, 2f, 0.18f),
                poleMaterial);
            CreatePrimitive(
                lampRoot.transform,
                "Lamp",
                PrimitiveType.Sphere,
                new Vector3(0f, 4.25f, 0f),
                Vector3.one * 0.55f,
                lampMaterial);
        }

        private static void AddBench(Transform parent, Vector3 position, float yaw)
        {
            GameObject benchRoot = new GameObject("Park Bench");
            benchRoot.transform.SetParent(parent, false);
            benchRoot.transform.localPosition = position;
            benchRoot.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            CreateBox(benchRoot.transform, "Seat", new Vector3(0f, 0.55f, 0f), new Vector3(3.2f, 0.25f, 0.8f), benchMaterial);
            CreateBox(benchRoot.transform, "Back", new Vector3(0f, 1.15f, 0.32f), new Vector3(3.2f, 1f, 0.2f), benchMaterial);
            CreateBox(benchRoot.transform, "Leg Left", new Vector3(-1.1f, 0.25f, 0f), new Vector3(0.2f, 0.5f, 0.6f), poleMaterial);
            CreateBox(benchRoot.transform, "Leg Right", new Vector3(1.1f, 0.25f, 0f), new Vector3(0.2f, 0.5f, 0.6f), poleMaterial);
        }

        private static GameObject CreateBox(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 scale,
            Material material)
        {
            return CreatePrimitive(parent, name, PrimitiveType.Cube, position, scale, material);
        }

        private static GameObject CreatePrimitive(
            Transform parent,
            string name,
            PrimitiveType primitiveType,
            Vector3 position,
            Vector3 scale,
            Material material)
        {
            GameObject primitive = GameObject.CreatePrimitive(primitiveType);
            primitive.name = name;
            primitive.transform.SetParent(parent, false);
            primitive.transform.localPosition = position;
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

        private static void CreatePreviewScene(
            GameObject crossroadPrefab,
            GameObject martStreetPrefab,
            GameObject residentialPrefab,
            GameObject schoolZonePrefab,
            GameObject churchParkPrefab)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.fog = false;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.72f, 0.75f, 0.8f);

            UnityEngine.Camera camera = CreatePreviewCamera();
            CreatePreviewLight();

            GameObject[] prefabs =
            {
                crossroadPrefab,
                martStreetPrefab,
                residentialPrefab,
                schoolZonePrefab,
                churchParkPrefab
            };
            string[] previewNames =
            {
                "ModernCity_Crossroad_Preview.png",
                "ModernCity_MartStreet_Preview.png",
                "ModernCity_Residential_Preview.png",
                "ModernCity_SchoolZone_Preview.png",
                "ModernCity_ChurchPark_Preview.png"
            };
            Vector3[] positions =
            {
                new Vector3(-104f, 0f, 0f),
                new Vector3(-52f, 0f, 0f),
                Vector3.zero,
                new Vector3(52f, 0f, 0f),
                new Vector3(104f, 0f, 0f)
            };
            GameObject[] instances = new GameObject[prefabs.Length];

            for (int index = 0; index < prefabs.Length; index++)
            {
                instances[index] = PrefabUtility.InstantiatePrefab(prefabs[index]) as GameObject;
                instances[index].transform.position = positions[index];
            }

            for (int index = 0; index < instances.Length; index++)
            {
                for (int otherIndex = 0; otherIndex < instances.Length; otherIndex++)
                {
                    instances[otherIndex].SetActive(index == otherIndex);
                }

                SetBillboardCamera(instances[index], camera);
                RenderPreview(camera, positions[index], PreviewRoot + "/" + previewNames[index]);
            }

            for (int index = 0; index < instances.Length; index++)
            {
                instances[index].SetActive(true);
                SetBillboardCamera(instances[index], camera);
            }

            camera.transform.position = new Vector3(0f, 96f, 104f);
            camera.transform.LookAt(new Vector3(0f, 0f, -3f));
            SetAllBillboards(camera);

            EditorSceneManager.SaveScene(scene, PreviewScenePath);
        }

        private static UnityEngine.Camera CreatePreviewCamera()
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            UnityEngine.Camera camera = cameraObject.AddComponent<UnityEngine.Camera>();
            camera.fieldOfView = 48f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 400f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(0xb9, 0xd7, 0xe7, 0xff);
            return camera;
        }

        private static void CreatePreviewLight()
        {
            GameObject lightObject = new GameObject("Preview Sun");
            Light previewLight = lightObject.AddComponent<Light>();
            previewLight.type = LightType.Directional;
            previewLight.intensity = 1.05f;
            previewLight.color = new Color(1f, 0.95f, 0.86f);
            lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
        }

        private static void SetBillboardCamera(GameObject root, UnityEngine.Camera camera)
        {
            BillboardSpriteView[] billboards = root.GetComponentsInChildren<BillboardSpriteView>(true);
            for (int index = 0; index < billboards.Length; index++)
            {
                billboards[index].SetTargetCamera(camera);
                billboards[index].UpdateFacing();
            }
        }

        private static void SetAllBillboards(UnityEngine.Camera camera)
        {
            BillboardSpriteView[] billboards = Object.FindObjectsByType<BillboardSpriteView>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int index = 0; index < billboards.Length; index++)
            {
                billboards[index].SetTargetCamera(camera);
                billboards[index].UpdateFacing();
            }
        }

        private static void RenderPreview(UnityEngine.Camera camera, Vector3 center, string assetPath)
        {
            camera.transform.position = center + new Vector3(0f, 45f, 47f);
            camera.transform.LookAt(center + new Vector3(0f, 0f, -2f));
            SetAllBillboards(camera);

            const int width = 1280;
            const int height = 800;
            RenderTexture renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            Texture2D screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTarget = camera.targetTexture;

            try
            {
                camera.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;
                camera.Render();
                screenshot.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                screenshot.Apply(false, false);

                string absolutePath = GetAbsoluteAssetPath(assetPath);
                Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
                File.WriteAllBytes(absolutePath, screenshot.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                Object.DestroyImmediate(screenshot);
                renderTexture.Release();
                Object.DestroyImmediate(renderTexture);
            }

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        }

        private static void ConfigureSprite(string assetPath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"현대 도시 스프라이트를 찾지 못했습니다: {assetPath}");
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
            ModernCityVisualGroundingSetup.ConfigureSpriteBottomPivot(assetPath);
        }

        private static Sprite LoadSprite(string assetPath)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
            {
                throw new InvalidOperationException($"스프라이트 임포트에 실패했습니다: {assetPath}");
            }

            return sprite;
        }

        private static void CreateMaterials()
        {
            grassMaterial = CreateMaterial(MaterialRoot + "/ModernCity_Grass.mat", new Color32(0x78, 0xae, 0x62, 0xff));
            roadMaterial = CreateMaterial(MaterialRoot + "/ModernCity_Road.mat", new Color32(0x48, 0x50, 0x58, 0xff));
            sidewalkMaterial = CreateMaterial(MaterialRoot + "/ModernCity_Sidewalk.mat", new Color32(0xd3, 0xcb, 0xbd, 0xff));
            roadLineMaterial = CreateMaterial(MaterialRoot + "/ModernCity_RoadLine.mat", new Color32(0xf5, 0xe5, 0xa4, 0xff));
            poleMaterial = CreateMaterial(MaterialRoot + "/ModernCity_Metal.mat", new Color32(0x3f, 0x48, 0x53, 0xff));
            lampMaterial = CreateMaterial(MaterialRoot + "/ModernCity_Lamp.mat", new Color32(0xff, 0xd4, 0x73, 0xff));
            benchMaterial = CreateMaterial(MaterialRoot + "/ModernCity_Bench.mat", new Color32(0x9b, 0x65, 0x3f, 0xff));
            contactShadowMaterial = ModernCityVisualGroundingSetup.GetOrCreateContactShadowMaterial();
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
                throw new InvalidOperationException($"현대 도시 청크 구성을 확인하지 못했습니다: {assetPath}");
            }

            if (prefab.GetComponentsInChildren<Collider>(true).Length > 0)
            {
                throw new InvalidOperationException($"프로토타입 청크에 충돌체가 남아 있습니다: {assetPath}");
            }
        }

        private static void VerifyPreview(string assetPath)
        {
            string absolutePath = GetAbsoluteAssetPath(assetPath);
            if (!File.Exists(absolutePath))
            {
                throw new InvalidOperationException($"현대 도시 청크 프리뷰가 없습니다: {assetPath}");
            }

            Texture2D preview = new Texture2D(2, 2, TextureFormat.RGB24, false);
            try
            {
                byte[] pngData = File.ReadAllBytes(absolutePath);
                if (!ImageConversion.LoadImage(preview, pngData, false) ||
                    preview.width != 1280 ||
                    preview.height != 800)
                {
                    throw new InvalidOperationException($"현대 도시 청크 프리뷰 크기가 올바르지 않습니다: {assetPath}");
                }
            }
            finally
            {
                Object.DestroyImmediate(preview);
            }
        }

        private static string GetAbsoluteAssetPath(string assetPath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
        }

        private static void DeleteObsoletePrototypeAssets()
        {
            string[] obsoleteAssetPaths =
            {
                ArtRoot + "/ModernCityShop.png",
                ArtRoot + "/ModernCityApartment.png",
                PrefabRoot + "/ModernCity_ShoppingStreet.prefab",
                PrefabRoot + "/ModernCity_ResidentialPark.prefab",
                PreviewRoot + "/ModernCity_ShoppingStreet_Preview.png",
                PreviewRoot + "/ModernCity_ResidentialPark_Preview.png"
            };

            for (int index = 0; index < obsoleteAssetPaths.Length; index++)
            {
                if (AssetDatabase.LoadMainAssetAtPath(obsoleteAssetPaths[index]) != null)
                {
                    AssetDatabase.DeleteAsset(obsoleteAssetPaths[index]);
                }
            }
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/IsekaiTruck/Art", "World");
            EnsureFolder("Assets/IsekaiTruck/Art/World", "ModernCity");
            EnsureFolder(ArtRoot, "Materials");
            EnsureFolder(ArtRoot, "Previews");
            EnsureFolder("Assets/IsekaiTruck/Prefabs", "World");
            EnsureFolder("Assets/IsekaiTruck/Prefabs/World", "ModernCity");
            EnsureFolder("Assets/IsekaiTruck/Scenes", "Prototypes");
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
