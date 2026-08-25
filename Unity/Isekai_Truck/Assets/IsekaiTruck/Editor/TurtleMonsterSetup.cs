using System;
using IsekaiTruck.Monsters;
using IsekaiTruck.Visuals;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace IsekaiTruck.Editor
{
    public static class TurtleMonsterSetup
    {
        private const string SpritePath = "Assets/IsekaiTruck/Art/Sprites/Turtle5DirectionWalk.png";
        private const string SpritePrefix = "Turtle";
        private const string TurtlePrefabPath = "Assets/IsekaiTruck/Prefabs/Monsters/Turtle.prefab";
        private const string CatalogPath = "Assets/IsekaiTruck/Config/MonsterPrefabCatalog.asset";
        private const float PixelsPerUnit = 112f;
        private const float VisualScale = 1.5f;
        private const float AnimationFramesPerSecond = 12f;
        private const float MinimumDistance = 5f;

        [MenuItem("Isekai Truck/Setup Turtle Monster")]
        public static void Setup()
        {
            bool isNewPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TurtlePrefabPath) == null;
            AssetDatabase.Refresh();
            DirectionalMonsterSpriteSheetUtility.ConfigureImporter(SpritePath, SpritePrefix, PixelsPerUnit);

            Sprite[] frames = DirectionalMonsterSpriteSheetUtility.LoadFrames(SpritePath, SpritePrefix);
            CreateOrUpdatePrefab(frames);

            MonsterPrefabCatalog catalog = AssetDatabase.LoadAssetAtPath<MonsterPrefabCatalog>(CatalogPath);
            if (catalog == null)
            {
                throw new InvalidOperationException("Monster prefab catalog is missing.");
            }

            MonsterPrefabSetup.RefreshCatalog(catalog);
            AssetDatabase.SaveAssets();
            Verify();
            if (isNewPrefab)
            {
                VerifyRequestedDefaults();
            }

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("Isekai Truck", "최소 거리를 유지하는 거북이 주민을 추가했습니다.", "확인");
            }
        }

        public static void Verify()
        {
            TextureImporter importer = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
            Sprite[] frames = DirectionalMonsterSpriteSheetUtility.LoadFrames(SpritePath, SpritePrefix);
            GameObject turtlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TurtlePrefabPath);
            MonsterPrefabCatalog catalog = AssetDatabase.LoadAssetAtPath<MonsterPrefabCatalog>(CatalogPath);
            if (importer == null || turtlePrefab == null || catalog == null)
            {
                throw new InvalidOperationException("Turtle monster assets are missing.");
            }

            if (importer.textureType != TextureImporterType.Sprite ||
                importer.spriteImportMode != SpriteImportMode.Multiple ||
                !importer.alphaIsTransparency ||
                !Mathf.Approximately(importer.spritePixelsPerUnit, PixelsPerUnit))
            {
                throw new InvalidOperationException("Turtle walk sheet importer is not configured as expected.");
            }

            MonsterDefinition definition = turtlePrefab.GetComponent<MonsterDefinition>();
            MonsterController controller = turtlePrefab.GetComponent<MonsterController>();
            MonsterView monsterView = turtlePrefab.GetComponent<MonsterView>();
            MonsterMinimumDistanceBehavior distanceBehavior =
                turtlePrefab.GetComponent<MonsterMinimumDistanceBehavior>();
            Transform visual = turtlePrefab.transform.Find("SpriteVisual");
            SpriteRenderer spriteRenderer = visual != null ? visual.GetComponent<SpriteRenderer>() : null;
            DirectionalSpriteAnimator directionalAnimator =
                visual != null ? visual.GetComponent<DirectionalSpriteAnimator>() : null;
            MeshRenderer legacyRenderer = turtlePrefab.GetComponent<MeshRenderer>();
            if (definition == null || controller == null || monsterView == null || distanceBehavior == null ||
                visual == null || spriteRenderer == null || directionalAnimator == null || legacyRenderer == null)
            {
                throw new InvalidOperationException("Turtle prefab components are incomplete.");
            }

            if (definition.TypeId != "turtle" || definition.DisplayName != "거북이")
            {
                throw new InvalidOperationException("Turtle prefab identity is incorrect.");
            }

            if (!Mathf.Approximately(definition.Size, MonsterDefinition.DefaultSize) ||
                !Mathf.Approximately(definition.Speed, MonsterDefinition.DefaultSpeed) ||
                !Mathf.Approximately(definition.FleeDistance, MonsterDefinition.DefaultFleeDistance) ||
                !Mathf.Approximately(definition.SpawnWeight, MonsterDefinition.DefaultSpawnWeight) ||
                distanceBehavior.MinimumDistance <= 0f)
            {
                throw new InvalidOperationException("Turtle gameplay settings are invalid.");
            }

            if (legacyRenderer.enabled || spriteRenderer.sprite != frames[0] || monsterView.VisualRoot != visual ||
                visual.localPosition != new Vector3(0f, -0.5f, 0f) || visual.localRotation != Quaternion.identity ||
                visual.localScale != Vector3.one * VisualScale)
            {
                throw new InvalidOperationException("Turtle sprite visual is not configured as expected.");
            }

            SerializedObject serializedView = new SerializedObject(monsterView);
            if (serializedView.FindProperty("directionalSpriteAnimator").objectReferenceValue != directionalAnimator)
            {
                throw new InvalidOperationException("Turtle directional animator is not assigned to MonsterView.");
            }

            VerifyDirectionalFrames(directionalAnimator, frames);
            VerifyCatalog(catalog, controller);
            VerifyOrdinaryFleeBehavior(turtlePrefab);
            VerifyMinimumDistanceFrameRates(turtlePrefab, distanceBehavior.MinimumDistance);
            VerifyPausedBehavior(turtlePrefab);
            Debug.Log("Turtle monster setup verification passed.");
        }

        private static void CreateOrUpdatePrefab(Sprite[] frames)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TurtlePrefabPath);
            bool isNewPrefab = prefab == null;
            GameObject root = isNewPrefab
                ? GameObject.CreatePrimitive(PrimitiveType.Sphere)
                : PrefabUtility.LoadPrefabContents(TurtlePrefabPath);

            try
            {
                root.name = "Turtle";
                Collider collider = root.GetComponent<Collider>();
                if (collider != null)
                {
                    Object.DestroyImmediate(collider);
                }

                MonsterDefinition definition = GetOrAddComponent<MonsterDefinition>(root);
                if (isNewPrefab)
                {
                    definition.Configure(CreateDefaultType());
                }

                if (definition.TypeId != "turtle")
                {
                    throw new InvalidOperationException("Existing Turtle prefab uses a different Type ID.");
                }

                GetOrAddComponent<MonsterController>(root);
                MonsterMinimumDistanceBehavior distanceBehavior =
                    GetOrAddComponent<MonsterMinimumDistanceBehavior>(root);
                if (isNewPrefab)
                {
                    SerializedObject serializedDistance = new SerializedObject(distanceBehavior);
                    serializedDistance.FindProperty("minimumDistance").floatValue = MinimumDistance;
                    serializedDistance.ApplyModifiedPropertiesWithoutUndo();
                }

                MonsterView monsterView = GetOrAddComponent<MonsterView>(root);
                MeshRenderer legacyRenderer = root.GetComponent<MeshRenderer>();
                if (legacyRenderer != null)
                {
                    legacyRenderer.enabled = false;
                }

                Transform visual = root.transform.Find("SpriteVisual");
                if (visual == null)
                {
                    GameObject visualObject = new GameObject("SpriteVisual");
                    visualObject.transform.SetParent(root.transform, false);
                    visual = visualObject.transform;
                }

                visual.localPosition = new Vector3(0f, -0.5f, 0f);
                visual.localRotation = Quaternion.identity;
                visual.localScale = Vector3.one * VisualScale;

                SpriteRenderer spriteRenderer = GetOrAddComponent<SpriteRenderer>(visual.gameObject);
                spriteRenderer.sprite = frames[0];
                spriteRenderer.flipX = false;
                spriteRenderer.color = Color.white;
                spriteRenderer.spriteSortPoint = SpriteSortPoint.Pivot;
                spriteRenderer.shadowCastingMode = ShadowCastingMode.Off;
                spriteRenderer.receiveShadows = false;
                GetOrAddComponent<BillboardSpriteView>(visual.gameObject);
                DirectionalSpriteAnimator directionalAnimator =
                    GetOrAddComponent<DirectionalSpriteAnimator>(visual.gameObject);
                directionalAnimator.Configure(spriteRenderer, frames, AnimationFramesPerSecond);

                monsterView.SetVisualRoot(visual);
                SerializedObject serializedView = new SerializedObject(monsterView);
                serializedView.FindProperty("faceMoveDirection").boolValue = false;
                serializedView.FindProperty("applyDefinitionColor").boolValue = false;
                serializedView.FindProperty("directionalSpriteAnimator").objectReferenceValue = directionalAnimator;
                serializedView.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, TurtlePrefabPath);
            }
            finally
            {
                if (isNewPrefab)
                {
                    Object.DestroyImmediate(root);
                }
                else
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
        }

        private static MonsterData CreateDefaultType()
        {
            return new MonsterData(
                "turtle",
                "거북이",
                "#FFFFFF",
                Color.white,
                MonsterDefinition.DefaultSize,
                MonsterDefinition.DefaultSpeed,
                MonsterDefinition.DefaultFleeDistance,
                50,
                2,
                MonsterDefinition.DefaultSpawnWeight
            );
        }

        private static void VerifyRequestedDefaults()
        {
            GameObject turtlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TurtlePrefabPath);
            MonsterDefinition definition = turtlePrefab.GetComponent<MonsterDefinition>();
            MonsterMinimumDistanceBehavior distanceBehavior =
                turtlePrefab.GetComponent<MonsterMinimumDistanceBehavior>();
            if (!Mathf.Approximately(definition.Size, MonsterDefinition.DefaultSize) ||
                !Mathf.Approximately(definition.Speed, MonsterDefinition.DefaultSpeed) ||
                !Mathf.Approximately(definition.FleeDistance, MonsterDefinition.DefaultFleeDistance) ||
                !Mathf.Approximately(definition.SpawnWeight, MonsterDefinition.DefaultSpawnWeight) ||
                !Mathf.Approximately(distanceBehavior.MinimumDistance, MinimumDistance))
            {
                throw new InvalidOperationException("Turtle requested defaults were not applied.");
            }
        }

        private static void VerifyDirectionalFrames(
            DirectionalSpriteAnimator animator,
            Sprite[] expectedFrames)
        {
            SerializedObject serializedAnimator = new SerializedObject(animator);
            SerializedProperty framesProperty = serializedAnimator.FindProperty("directionFrames");
            SerializedProperty framesPerDirectionProperty = serializedAnimator.FindProperty("framesPerDirection");
            SerializedProperty framesPerSecondProperty = serializedAnimator.FindProperty("framesPerSecond");
            if (framesProperty == null || framesPerDirectionProperty == null || framesPerSecondProperty == null ||
                framesProperty.arraySize != expectedFrames.Length ||
                framesPerDirectionProperty.intValue != DirectionalSpriteAnimator.DefaultFramesPerDirection ||
                !Mathf.Approximately(framesPerSecondProperty.floatValue, AnimationFramesPerSecond))
            {
                throw new InvalidOperationException("Turtle directional animation settings are incomplete.");
            }

            for (int frameIndex = 0; frameIndex < expectedFrames.Length; frameIndex++)
            {
                if (framesProperty.GetArrayElementAtIndex(frameIndex).objectReferenceValue !=
                    expectedFrames[frameIndex])
                {
                    throw new InvalidOperationException(
                        $"Turtle directional frame reference is incorrect: {frameIndex}");
                }
            }
        }

        private static void VerifyCatalog(MonsterPrefabCatalog catalog, MonsterController turtleController)
        {
            for (int i = 0; i < catalog.MonsterPrefabs.Count; i++)
            {
                if (catalog.MonsterPrefabs[i] == turtleController)
                {
                    return;
                }
            }

            throw new InvalidOperationException("Turtle prefab is not registered in the monster catalog.");
        }

        private static void VerifyOrdinaryFleeBehavior(GameObject turtlePrefab)
        {
            GameObject truckObject = new GameObject("Turtle Ordinary Flee Verification Truck");
            GameObject instance = PrefabUtility.InstantiatePrefab(turtlePrefab) as GameObject;
            try
            {
                instance.transform.position = Vector3.right * 6f;
                MonsterDefinition definition = instance.GetComponent<MonsterDefinition>();
                MonsterController controller = instance.GetComponent<MonsterController>();
                controller.Initialize(definition.CreateData(), truckObject.transform, 0f, 60f);
                controller.UpdateMonster(100f, 0f, 3.096f, 1f, 1f / 60f, 0f, 1f, false);

                if (instance.transform.position.x <= 6f)
                {
                    throw new InvalidOperationException("Turtle no longer uses the ordinary flee behavior.");
                }
            }
            finally
            {
                Object.DestroyImmediate(instance);
                Object.DestroyImmediate(truckObject);
            }
        }

        private static void VerifyMinimumDistanceFrameRates(
            GameObject turtlePrefab,
            float minimumDistance)
        {
            VerifyMinimumDistance(turtlePrefab, minimumDistance, 30);
            VerifyMinimumDistance(turtlePrefab, minimumDistance, 60);
            VerifyMinimumDistance(turtlePrefab, minimumDistance, 120);
        }

        private static void VerifyMinimumDistance(
            GameObject turtlePrefab,
            float minimumDistance,
            int frameRate)
        {
            GameObject truckObject = new GameObject($"Turtle {frameRate} FPS Verification Truck");
            GameObject instance = PrefabUtility.InstantiatePrefab(turtlePrefab) as GameObject;
            try
            {
                float deltaTime = 1f / frameRate;
                float frameScale = deltaTime * 60f;
                MonsterDefinition definition = instance.GetComponent<MonsterDefinition>();
                MonsterController controller = instance.GetComponent<MonsterController>();
                controller.Initialize(definition.CreateData(), truckObject.transform, 0f, 60f);

                for (int frameIndex = 0; frameIndex < frameRate; frameIndex++)
                {
                    truckObject.transform.position = instance.transform.position - Vector3.right * 2f;
                    controller.UpdateMonster(
                        (frameIndex + 1) * deltaTime * 1000f,
                        0f,
                        3.096f,
                        frameScale,
                        deltaTime,
                        0f,
                        1f,
                        false);

                    float distance = Vector3.Distance(
                        Vector3.ProjectOnPlane(instance.transform.position, Vector3.up),
                        Vector3.ProjectOnPlane(truckObject.transform.position, Vector3.up));
                    if (distance < minimumDistance - 0.0001f)
                    {
                        throw new InvalidOperationException(
                            $"Turtle minimum distance failed at {frameRate} FPS: {distance}");
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(instance);
                Object.DestroyImmediate(truckObject);
            }
        }

        private static void VerifyPausedBehavior(GameObject turtlePrefab)
        {
            GameObject truckObject = new GameObject("Turtle Pause Verification Truck");
            GameObject instance = PrefabUtility.InstantiatePrefab(turtlePrefab) as GameObject;
            try
            {
                instance.transform.position = Vector3.right * 2f;
                MonsterDefinition definition = instance.GetComponent<MonsterDefinition>();
                MonsterController controller = instance.GetComponent<MonsterController>();
                controller.Initialize(definition.CreateData(), truckObject.transform, 0f, 60f);
                Vector3 pausedPosition = instance.transform.position;
                controller.UpdateMonster(100f, 0f, 3.096f, 1f, 1f / 60f, 0f, 1f, true);

                if (instance.transform.position != pausedPosition)
                {
                    throw new InvalidOperationException("Turtle moved while the world was paused.");
                }
            }
            finally
            {
                Object.DestroyImmediate(instance);
                Object.DestroyImmediate(truckObject);
            }
        }

        private static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }
    }
}
