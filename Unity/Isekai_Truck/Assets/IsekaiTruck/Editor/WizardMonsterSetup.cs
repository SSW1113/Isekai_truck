using System;
using IsekaiTruck.Monsters;
using IsekaiTruck.Visuals;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace IsekaiTruck.Editor
{
    public static class WizardMonsterSetup
    {
        private const string SpritePath = "Assets/IsekaiTruck/Art/Sprites/Wizard5DirectionWalk.png";
        private const string SpritePrefix = "Wizard";
        private const string WizardPrefabPath = "Assets/IsekaiTruck/Prefabs/Monsters/Wizard.prefab";
        private const string CatalogPath = "Assets/IsekaiTruck/Config/MonsterPrefabCatalog.asset";
        private const float PixelsPerUnit = 112f;
        private const float VisualScale = 1.5f;
        private const float AnimationFramesPerSecond = 12f;
        private const float TeleportInterval = 3f;

        [MenuItem("Isekai Truck/Setup Wizard Monster")]
        public static void Setup()
        {
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

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("Isekai Truck", "마법사 주민과 쿨타임 텔레포트를 추가했습니다.", "확인");
            }
        }

        public static void Verify()
        {
            TextureImporter importer = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
            Sprite[] frames = DirectionalMonsterSpriteSheetUtility.LoadFrames(SpritePath, SpritePrefix);
            GameObject wizardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WizardPrefabPath);
            MonsterPrefabCatalog catalog = AssetDatabase.LoadAssetAtPath<MonsterPrefabCatalog>(CatalogPath);
            if (importer == null || wizardPrefab == null || catalog == null)
            {
                throw new InvalidOperationException("Wizard monster assets are missing.");
            }

            if (importer.textureType != TextureImporterType.Sprite ||
                importer.spriteImportMode != SpriteImportMode.Multiple ||
                !importer.alphaIsTransparency ||
                !Mathf.Approximately(importer.spritePixelsPerUnit, PixelsPerUnit))
            {
                throw new InvalidOperationException("Wizard walk sheet importer is not configured as expected.");
            }

            MonsterDefinition definition = wizardPrefab.GetComponent<MonsterDefinition>();
            MonsterController controller = wizardPrefab.GetComponent<MonsterController>();
            MonsterView monsterView = wizardPrefab.GetComponent<MonsterView>();
            MonsterFleeTeleportBehavior teleportBehavior = wizardPrefab.GetComponent<MonsterFleeTeleportBehavior>();
            Transform visual = wizardPrefab.transform.Find("SpriteVisual");
            SpriteRenderer spriteRenderer = visual != null ? visual.GetComponent<SpriteRenderer>() : null;
            DirectionalSpriteAnimator directionalAnimator = visual != null ? visual.GetComponent<DirectionalSpriteAnimator>() : null;
            MeshRenderer legacyRenderer = wizardPrefab.GetComponent<MeshRenderer>();
            if (definition == null || controller == null || monsterView == null || teleportBehavior == null ||
                visual == null || spriteRenderer == null || directionalAnimator == null || legacyRenderer == null)
            {
                throw new InvalidOperationException("Wizard prefab components are incomplete.");
            }

            if (definition.TypeId != "wizard" || definition.DisplayName != "마법사" ||
                !Mathf.Approximately(definition.Size, MonsterDefinition.DefaultSize) ||
                !Mathf.Approximately(definition.Speed, MonsterDefinition.DefaultSpeed) ||
                !Mathf.Approximately(definition.FleeDistance, MonsterDefinition.DefaultFleeDistance) ||
                !Mathf.Approximately(definition.SpawnWeight, MonsterDefinition.DefaultSpawnWeight) ||
                !Mathf.Approximately(teleportBehavior.TeleportInterval, TeleportInterval))
            {
                throw new InvalidOperationException("Wizard gameplay settings are incorrect.");
            }

            if (legacyRenderer.enabled || spriteRenderer.sprite != frames[0] || monsterView.VisualRoot != visual ||
                visual.localPosition != new Vector3(0f, -0.5f, 0f) || visual.localRotation != Quaternion.identity ||
                visual.localScale != Vector3.one * VisualScale)
            {
                throw new InvalidOperationException("Wizard sprite visual is not configured as expected.");
            }

            SerializedObject serializedView = new SerializedObject(monsterView);
            if (serializedView.FindProperty("directionalSpriteAnimator").objectReferenceValue != directionalAnimator)
            {
                throw new InvalidOperationException("Wizard directional animator is not assigned to MonsterView.");
            }

            VerifyDirectionalFrames(directionalAnimator, frames);
            VerifyCatalog(catalog, controller);
            VerifyTeleportFrameRates(wizardPrefab);
            VerifyReadyCooldownFrameRates(wizardPrefab);
            VerifyPausedTeleportTimer(wizardPrefab);
            Debug.Log("Wizard monster setup verification passed.");
        }

        private static void CreateOrUpdatePrefab(Sprite[] frames)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(WizardPrefabPath);
            bool isNewPrefab = prefab == null;
            GameObject root = isNewPrefab
                ? GameObject.CreatePrimitive(PrimitiveType.Sphere)
                : PrefabUtility.LoadPrefabContents(WizardPrefabPath);

            try
            {
                root.name = "Wizard";
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

                if (definition.TypeId != "wizard")
                {
                    throw new InvalidOperationException("Existing Wizard prefab uses a different Type ID.");
                }

                GetOrAddComponent<MonsterController>(root);
                MonsterFleeTeleportBehavior teleportBehavior = GetOrAddComponent<MonsterFleeTeleportBehavior>(root);
                if (isNewPrefab)
                {
                    SerializedObject serializedTeleport = new SerializedObject(teleportBehavior);
                    serializedTeleport.FindProperty("teleportInterval").floatValue = TeleportInterval;
                    serializedTeleport.FindProperty("teleportDistanceMultiplier").floatValue = 1f;
                    serializedTeleport.ApplyModifiedPropertiesWithoutUndo();
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
                DirectionalSpriteAnimator directionalAnimator = GetOrAddComponent<DirectionalSpriteAnimator>(visual.gameObject);
                directionalAnimator.Configure(spriteRenderer, frames, AnimationFramesPerSecond);

                monsterView.SetVisualRoot(visual);
                SerializedObject serializedView = new SerializedObject(monsterView);
                serializedView.FindProperty("faceMoveDirection").boolValue = false;
                serializedView.FindProperty("applyDefinitionColor").boolValue = false;
                serializedView.FindProperty("directionalSpriteAnimator").objectReferenceValue = directionalAnimator;
                serializedView.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, WizardPrefabPath);
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
                "wizard",
                "마법사",
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

        private static void VerifyDirectionalFrames(DirectionalSpriteAnimator animator, Sprite[] expectedFrames)
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
                throw new InvalidOperationException("Wizard directional animation settings are incomplete.");
            }

            for (int frameIndex = 0; frameIndex < expectedFrames.Length; frameIndex++)
            {
                if (framesProperty.GetArrayElementAtIndex(frameIndex).objectReferenceValue != expectedFrames[frameIndex])
                {
                    throw new InvalidOperationException($"Wizard directional frame reference is incorrect: {frameIndex}");
                }
            }
        }

        private static void VerifyCatalog(MonsterPrefabCatalog catalog, MonsterController wizardController)
        {
            for (int i = 0; i < catalog.MonsterPrefabs.Count; i++)
            {
                if (catalog.MonsterPrefabs[i] == wizardController)
                {
                    return;
                }
            }

            throw new InvalidOperationException("Wizard prefab is not registered in the monster catalog.");
        }

        private static void VerifyTeleportFrameRates(GameObject wizardPrefab)
        {
            float teleportTimeAt30Fps = MeasureTeleportTime(wizardPrefab, 30);
            float teleportTimeAt60Fps = MeasureTeleportTime(wizardPrefab, 60);
            float teleportTimeAt120Fps = MeasureTeleportTime(wizardPrefab, 120);
            AssertTeleportTime(teleportTimeAt30Fps, 30);
            AssertTeleportTime(teleportTimeAt60Fps, 60);
            AssertTeleportTime(teleportTimeAt120Fps, 120);
        }

        private static float MeasureTeleportTime(GameObject wizardPrefab, int frameRate)
        {
            GameObject truckObject = new GameObject($"Wizard {frameRate} FPS Verification Truck");
            GameObject instance = PrefabUtility.InstantiatePrefab(wizardPrefab) as GameObject;
            try
            {
                float deltaTime = 1f / frameRate;
                float frameScale = deltaTime * 60f;
                int frameCount = Mathf.CeilToInt((TeleportInterval + 1f) * frameRate);
                instance.transform.position = new Vector3(5f, 0f, 0f);

                MonsterDefinition definition = instance.GetComponent<MonsterDefinition>();
                MonsterController controller = instance.GetComponent<MonsterController>();
                controller.Initialize(definition.CreateData(), truckObject.transform, 0f, 60f);

                for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
                {
                    truckObject.transform.position = instance.transform.position - Vector3.right * 5f;
                    Vector3 previousPosition = instance.transform.position;
                    float nowMilliseconds = (frameIndex + 1) * deltaTime * 1000f;
                    controller.UpdateMonster(nowMilliseconds, 0f, 3.096f, frameScale, deltaTime, 0f, 1f, false);

                    float frameDistance = Vector3.Distance(previousPosition, instance.transform.position);
                    if (frameDistance > 1f)
                    {
                        return (frameIndex + 1) * deltaTime;
                    }
                }

                throw new InvalidOperationException($"Wizard did not teleport at {frameRate} FPS.");
            }
            finally
            {
                Object.DestroyImmediate(instance);
                Object.DestroyImmediate(truckObject);
            }
        }

        private static void AssertTeleportTime(float teleportTime, int frameRate)
        {
            float tolerance = 1f / frameRate + 0.001f;
            if (teleportTime < TeleportInterval - tolerance || teleportTime > TeleportInterval + tolerance)
            {
                throw new InvalidOperationException(
                    $"Wizard teleport interval changed at {frameRate} FPS: {teleportTime} seconds"
                );
            }
        }

        private static void VerifyReadyCooldownFrameRates(GameObject wizardPrefab)
        {
            VerifyReadyCooldown(wizardPrefab, 30);
            VerifyReadyCooldown(wizardPrefab, 60);
            VerifyReadyCooldown(wizardPrefab, 120);
        }

        private static void VerifyReadyCooldown(GameObject wizardPrefab, int frameRate)
        {
            GameObject truckObject = new GameObject($"Wizard Ready Cooldown {frameRate} FPS Verification Truck");
            GameObject instance = PrefabUtility.InstantiatePrefab(wizardPrefab) as GameObject;
            try
            {
                float deltaTime = 1f / frameRate;
                float frameScale = deltaTime * 60f;
                int cooldownFrameCount = Mathf.CeilToInt(TeleportInterval * frameRate);
                truckObject.transform.position = new Vector3(1000f, 0f, 0f);

                MonsterDefinition definition = instance.GetComponent<MonsterDefinition>();
                MonsterController controller = instance.GetComponent<MonsterController>();
                controller.Initialize(definition.CreateData(), truckObject.transform, 0f, 60f);

                for (int frameIndex = 0; frameIndex < cooldownFrameCount; frameIndex++)
                {
                    controller.UpdateMonster(
                        (frameIndex + 1) * deltaTime * 1000f,
                        0f,
                        3.096f,
                        frameScale,
                        deltaTime,
                        0f,
                        1f,
                        false
                    );
                }

                truckObject.transform.position = instance.transform.position - Vector3.right * 5f;
                Vector3 positionBeforeRecognition = instance.transform.position;
                controller.UpdateMonster(
                    (cooldownFrameCount + 1) * deltaTime * 1000f,
                    0f,
                    3.096f,
                    frameScale,
                    deltaTime,
                    0f,
                    1f,
                    false
                );

                if (Vector3.Distance(positionBeforeRecognition, instance.transform.position) <= 1f)
                {
                    throw new InvalidOperationException(
                        $"Wizard did not teleport immediately after recognition at {frameRate} FPS."
                    );
                }

                truckObject.transform.position = instance.transform.position - Vector3.right * 5f;
                Vector3 positionAfterTeleport = instance.transform.position;
                controller.UpdateMonster(
                    (cooldownFrameCount + 2) * deltaTime * 1000f,
                    0f,
                    3.096f,
                    frameScale,
                    deltaTime,
                    0f,
                    1f,
                    false
                );

                if (Vector3.Distance(positionAfterTeleport, instance.transform.position) > 1f)
                {
                    throw new InvalidOperationException(
                        $"Wizard teleported again before the cooldown reset at {frameRate} FPS."
                    );
                }
            }
            finally
            {
                Object.DestroyImmediate(instance);
                Object.DestroyImmediate(truckObject);
            }
        }

        private static void VerifyPausedTeleportTimer(GameObject wizardPrefab)
        {
            GameObject truckObject = new GameObject("Wizard Pause Verification Truck");
            GameObject instance = PrefabUtility.InstantiatePrefab(wizardPrefab) as GameObject;
            try
            {
                instance.transform.position = new Vector3(5f, 0f, 0f);
                MonsterDefinition definition = instance.GetComponent<MonsterDefinition>();
                MonsterController controller = instance.GetComponent<MonsterController>();
                controller.Initialize(definition.CreateData(), truckObject.transform, 0f, 60f);

                for (int frameIndex = 0; frameIndex < 240; frameIndex++)
                {
                    truckObject.transform.position = instance.transform.position - Vector3.right * 5f;
                    controller.UpdateMonster((frameIndex + 1) * 1000f / 60f, 0f, 3.096f, 1f, 1f / 60f, 0f, 1f, false);
                }

                Vector3 pausedPosition = instance.transform.position;
                for (int frameIndex = 0; frameIndex < 120; frameIndex++)
                {
                    controller.UpdateMonster((frameIndex + 241) * 1000f / 60f, 0f, 3.096f, 1f, 1f / 60f, 0f, 1f, true);
                }

                if (instance.transform.position != pausedPosition)
                {
                    throw new InvalidOperationException("Wizard moved or teleported while the world was paused.");
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
