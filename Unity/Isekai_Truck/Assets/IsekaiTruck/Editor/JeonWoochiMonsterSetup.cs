using System;
using IsekaiTruck.Config;
using IsekaiTruck.Monsters;
using IsekaiTruck.Truck;
using IsekaiTruck.Visuals;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace IsekaiTruck.Editor
{
    public static class JeonWoochiMonsterSetup
    {
        private const string SpritePath = "Assets/IsekaiTruck/Art/Sprites/JeonWoochi5DirectionWalk.png";
        private const string SpritePrefix = "JeonWoochi";
        private const string StickySpritePath = "Assets/IsekaiTruck/Art/Sprites/JeonWoochiStickyMist.png";
        private const string StickySpritePrefix = "JeonWoochiSticky";
        private const string StickyPrefabPath = "Assets/IsekaiTruck/Prefabs/Effects/JeonWoochiStickyMist.prefab";
        private const string MonsterPrefabPath = "Assets/IsekaiTruck/Prefabs/Monsters/JeonWoochi.prefab";
        private const string CatalogPath = "Assets/IsekaiTruck/Config/MonsterPrefabCatalog.asset";
        private const string ConfigPath = "Assets/IsekaiTruck/Config/GameConfig.asset";
        private const float PixelsPerUnit = 112f;
        private const float StickyPixelsPerUnit = 125f;
        private const float VisualScale = 1.5f;
        private const float StickyVisualScale = 4f;
        private const float AnimationFramesPerSecond = 12f;
        private const float StickyFramesPerSecond = 4f;
        private const float StickyAlpha = 0.55f;
        private const float DropInterval = 3f;
        private const float StickyRadius = 4f;
        private const float StickySpeedMultiplier = 0.5f;
        private const int StickyColumns = 4;
        private const int StickyRows = 4;
        private const int StickyFrameCount = 16;
        private const int StickyCellInset = 2;
        private const int StickySlicePadding = 4;
        private const byte AlphaThreshold = 8;

        [MenuItem("Isekai Truck/Setup Jeon Woochi Monster")]
        public static void Setup()
        {
            bool isNewMonster = AssetDatabase.LoadAssetAtPath<GameObject>(MonsterPrefabPath) == null;
            bool isNewSticky = AssetDatabase.LoadAssetAtPath<GameObject>(StickyPrefabPath) == null;
            AssetDatabase.Refresh();
            DirectionalMonsterSpriteSheetUtility.ConfigureImporter(SpritePath, SpritePrefix, PixelsPerUnit);
            ConfigureStickyImporter();

            Sprite[] frames = DirectionalMonsterSpriteSheetUtility.LoadFrames(SpritePath, SpritePrefix);
            Sprite[] stickyFrames = LoadStickyFrames();
            StickySlowZone stickyPrefab = CreateOrUpdateStickyPrefab(stickyFrames);
            CreateOrUpdateMonsterPrefab(frames, stickyPrefab);

            MonsterPrefabCatalog catalog = AssetDatabase.LoadAssetAtPath<MonsterPrefabCatalog>(CatalogPath);
            if (catalog == null)
            {
                throw new InvalidOperationException("Monster prefab catalog is missing.");
            }

            MonsterPrefabSetup.RefreshCatalog(catalog);
            AssetDatabase.SaveAssets();
            Verify();
            if (isNewMonster || isNewSticky)
            {
                VerifyRequestedDefaults();
            }

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("Isekai Truck", "끈끈이를 남기는 전우치 주민을 추가했습니다.", "확인");
            }
        }

        public static void Verify()
        {
            TextureImporter importer = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
            TextureImporter stickyImporter = AssetImporter.GetAtPath(StickySpritePath) as TextureImporter;
            Sprite[] frames = DirectionalMonsterSpriteSheetUtility.LoadFrames(SpritePath, SpritePrefix);
            Sprite[] stickyFrames = LoadStickyFrames();
            GameObject stickyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StickyPrefabPath);
            GameObject monsterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MonsterPrefabPath);
            MonsterPrefabCatalog catalog = AssetDatabase.LoadAssetAtPath<MonsterPrefabCatalog>(CatalogPath);
            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            if (importer == null || stickyImporter == null || stickyPrefab == null || monsterPrefab == null ||
                catalog == null || config == null)
            {
                throw new InvalidOperationException("Jeon Woochi monster assets are missing.");
            }

            VerifyImporter(importer, PixelsPerUnit, 2048, "Jeon Woochi walk sheet");
            VerifyImporter(stickyImporter, StickyPixelsPerUnit, 512, "Jeon Woochi sticky sheet");
            VerifyStickyPrefab(stickyPrefab, stickyFrames);
            VerifyMonsterPrefab(monsterPrefab, frames, catalog);
            VerifyStickySlow(stickyPrefab, config);
            VerifyStickyDropFrameRates(monsterPrefab, config);
            VerifyPausedDrop(monsterPrefab, config);
            Debug.Log("Jeon Woochi monster setup verification passed.");
        }

        private static void ConfigureStickyImporter()
        {
            TextureImporter importer = AssetImporter.GetAtPath(StickySpritePath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("Jeon Woochi sticky sheet importer was not created.");
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = StickyPixelsPerUnit;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.sRGBTexture = true;
            importer.maxTextureSize = 512;
            importer.isReadable = true;
            importer.SaveAndReimport();

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(StickySpritePath);
            if (texture == null)
            {
                throw new InvalidOperationException("Jeon Woochi sticky sheet could not be loaded.");
            }

#pragma warning disable CS0618
            importer.spritesheet = BuildStickyMetadata(texture);
#pragma warning restore CS0618
            importer.isReadable = false;
            importer.SaveAndReimport();
        }

        private static SpriteMetaData[] BuildStickyMetadata(Texture2D texture)
        {
            if (texture.width % StickyColumns != 0 || texture.height % StickyRows != 0)
            {
                throw new InvalidOperationException("Jeon Woochi sticky sheet is not a 4x4 grid.");
            }

            int cellWidth = texture.width / StickyColumns;
            int cellHeight = texture.height / StickyRows;
            Color32[] pixels = texture.GetPixels32();
            SpriteMetaData[] metadata = new SpriteMetaData[StickyFrameCount];

            for (int frameIndex = 0; frameIndex < StickyFrameCount; frameIndex++)
            {
                int column = frameIndex % StickyColumns;
                int rowFromTop = frameIndex / StickyColumns;
                int cellMinX = column * cellWidth;
                int cellMinY = texture.height - (rowFromTop + 1) * cellHeight;
                RectInt opaqueBounds = FindOpaqueBounds(
                    pixels,
                    texture.width,
                    cellMinX,
                    cellMinY,
                    cellWidth,
                    cellHeight,
                    frameIndex);
                int safeMinX = cellMinX + StickyCellInset;
                int safeMinY = cellMinY + StickyCellInset;
                int safeMaxX = cellMinX + cellWidth - StickyCellInset - 1;
                int safeMaxY = cellMinY + cellHeight - StickyCellInset - 1;
                int paddedMinX = Mathf.Max(safeMinX, opaqueBounds.xMin - StickySlicePadding);
                int paddedMinY = Mathf.Max(safeMinY, opaqueBounds.yMin - StickySlicePadding);
                int paddedMaxX = Mathf.Min(safeMaxX, opaqueBounds.xMax - 1 + StickySlicePadding);
                int paddedMaxY = Mathf.Min(safeMaxY, opaqueBounds.yMax - 1 + StickySlicePadding);
                int spriteWidth = paddedMaxX - paddedMinX + 1;
                int spriteHeight = paddedMaxY - paddedMinY + 1;
                float pivotY = (opaqueBounds.yMin - paddedMinY) / (float)spriteHeight;

                metadata[frameIndex] = new SpriteMetaData
                {
                    name = GetStickySpriteName(frameIndex),
                    rect = new Rect(paddedMinX, paddedMinY, spriteWidth, spriteHeight),
                    alignment = (int)SpriteAlignment.Custom,
                    pivot = new Vector2(0.5f, pivotY),
                    border = Vector4.zero
                };
            }

            return metadata;
        }

        private static RectInt FindOpaqueBounds(
            Color32[] pixels,
            int textureWidth,
            int cellMinX,
            int cellMinY,
            int cellWidth,
            int cellHeight,
            int frameIndex)
        {
            int minX = cellMinX + cellWidth;
            int minY = cellMinY + cellHeight;
            int maxX = -1;
            int maxY = -1;
            int startX = cellMinX + StickyCellInset;
            int startY = cellMinY + StickyCellInset;
            int endX = cellMinX + cellWidth - StickyCellInset;
            int endY = cellMinY + cellHeight - StickyCellInset;

            for (int y = startY; y < endY; y++)
            {
                int rowOffset = y * textureWidth;
                for (int x = startX; x < endX; x++)
                {
                    if (pixels[rowOffset + x].a <= AlphaThreshold)
                    {
                        continue;
                    }

                    minX = Mathf.Min(minX, x);
                    minY = Mathf.Min(minY, y);
                    maxX = Mathf.Max(maxX, x);
                    maxY = Mathf.Max(maxY, y);
                }
            }

            if (maxX < minX || maxY < minY)
            {
                throw new InvalidOperationException($"Jeon Woochi sticky frame is empty: {frameIndex}");
            }

            return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        private static Sprite[] LoadStickyFrames()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(StickySpritePath);
            Sprite[] frames = new Sprite[StickyFrameCount];

            for (int frameIndex = 0; frameIndex < frames.Length; frameIndex++)
            {
                string spriteName = GetStickySpriteName(frameIndex);
                for (int assetIndex = 0; assetIndex < assets.Length; assetIndex++)
                {
                    Sprite sprite = assets[assetIndex] as Sprite;
                    if (sprite != null && sprite.name == spriteName)
                    {
                        frames[frameIndex] = sprite;
                        break;
                    }
                }

                if (frames[frameIndex] == null)
                {
                    throw new InvalidOperationException($"Jeon Woochi sticky frame is missing: {spriteName}");
                }
            }

            return frames;
        }

        private static string GetStickySpriteName(int frameIndex)
        {
            return $"{StickySpritePrefix}_{frameIndex}";
        }

        private static StickySlowZone CreateOrUpdateStickyPrefab(Sprite[] frames)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(StickyPrefabPath);
            bool isNewPrefab = prefab == null;
            GameObject root = isNewPrefab
                ? new GameObject("JeonWoochiStickyMist")
                : PrefabUtility.LoadPrefabContents(StickyPrefabPath);

            try
            {
                root.name = "JeonWoochiStickyMist";
                root.transform.localScale = Vector3.one * StickyVisualScale;
                SpriteRenderer spriteRenderer = GetOrAddComponent<SpriteRenderer>(root);
                spriteRenderer.sprite = frames[0];
                spriteRenderer.color = new Color(1f, 1f, 1f, StickyAlpha);
                spriteRenderer.spriteSortPoint = SpriteSortPoint.Pivot;
                spriteRenderer.sortingOrder = 1;
                spriteRenderer.shadowCastingMode = ShadowCastingMode.Off;
                spriteRenderer.receiveShadows = false;
                GetOrAddComponent<BillboardSpriteView>(root);
                SpriteSequenceEffect sequenceEffect = GetOrAddComponent<SpriteSequenceEffect>(root);
                sequenceEffect.Configure(spriteRenderer, frames, StickyFramesPerSecond, true);
                StickySlowZone stickyZone = GetOrAddComponent<StickySlowZone>(root);
                SerializedObject serializedZone = new SerializedObject(stickyZone);
                serializedZone.FindProperty("radius").floatValue = StickyRadius;
                serializedZone.FindProperty("speedMultiplier").floatValue = StickySpeedMultiplier;
                serializedZone.ApplyModifiedPropertiesWithoutUndo();

                GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, StickyPrefabPath);
                StickySlowZone savedZone = savedPrefab != null ? savedPrefab.GetComponent<StickySlowZone>() : null;
                if (savedZone == null)
                {
                    throw new InvalidOperationException("Jeon Woochi sticky prefab could not be saved.");
                }

                return savedZone;
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

        private static void CreateOrUpdateMonsterPrefab(Sprite[] frames, StickySlowZone stickyPrefab)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MonsterPrefabPath);
            bool isNewPrefab = prefab == null;
            GameObject root = isNewPrefab
                ? GameObject.CreatePrimitive(PrimitiveType.Sphere)
                : PrefabUtility.LoadPrefabContents(MonsterPrefabPath);

            try
            {
                root.name = "JeonWoochi";
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

                if (definition.TypeId != "jeon_woochi")
                {
                    throw new InvalidOperationException("Existing JeonWoochi prefab uses a different Type ID.");
                }

                GetOrAddComponent<MonsterController>(root);
                MonsterStickyTrailBehavior stickyBehavior = GetOrAddComponent<MonsterStickyTrailBehavior>(root);
                SerializedObject serializedBehavior = new SerializedObject(stickyBehavior);
                serializedBehavior.FindProperty("stickyZonePrefab").objectReferenceValue = stickyPrefab;
                if (isNewPrefab)
                {
                    serializedBehavior.FindProperty("dropInterval").floatValue = DropInterval;
                }

                serializedBehavior.ApplyModifiedPropertiesWithoutUndo();

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

                PrefabUtility.SaveAsPrefabAsset(root, MonsterPrefabPath);
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
                "jeon_woochi",
                "전우치",
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

        private static void VerifyImporter(
            TextureImporter importer,
            float pixelsPerUnit,
            int maxTextureSize,
            string label)
        {
            if (importer.textureType != TextureImporterType.Sprite ||
                importer.spriteImportMode != SpriteImportMode.Multiple ||
                !importer.alphaIsTransparency ||
                !Mathf.Approximately(importer.spritePixelsPerUnit, pixelsPerUnit) ||
                importer.maxTextureSize != maxTextureSize)
            {
                throw new InvalidOperationException($"{label} importer is not configured as expected.");
            }
        }

        private static void VerifyStickyPrefab(GameObject stickyPrefab, Sprite[] expectedFrames)
        {
            StickySlowZone zone = stickyPrefab.GetComponent<StickySlowZone>();
            SpriteRenderer spriteRenderer = stickyPrefab.GetComponent<SpriteRenderer>();
            BillboardSpriteView billboard = stickyPrefab.GetComponent<BillboardSpriteView>();
            SpriteSequenceEffect sequenceEffect = stickyPrefab.GetComponent<SpriteSequenceEffect>();
            if (zone == null || spriteRenderer == null || billboard == null || sequenceEffect == null)
            {
                throw new InvalidOperationException("Jeon Woochi sticky prefab components are incomplete.");
            }

            if (stickyPrefab.transform.localScale != Vector3.one * StickyVisualScale ||
                spriteRenderer.sprite != expectedFrames[0] ||
                !Mathf.Approximately(spriteRenderer.color.a, StickyAlpha) ||
                sequenceEffect.FrameCount != StickyFrameCount ||
                !Mathf.Approximately(sequenceEffect.FramesPerSecond, StickyFramesPerSecond) ||
                !sequenceEffect.DestroyOnComplete || !Mathf.Approximately(zone.Radius, StickyRadius) ||
                !Mathf.Approximately(zone.SpeedMultiplier, StickySpeedMultiplier))
            {
                throw new InvalidOperationException("Jeon Woochi sticky prefab settings are incorrect.");
            }

            SerializedObject serializedSequence = new SerializedObject(sequenceEffect);
            SerializedProperty framesProperty = serializedSequence.FindProperty("frames");
            for (int frameIndex = 0; frameIndex < expectedFrames.Length; frameIndex++)
            {
                if (framesProperty.GetArrayElementAtIndex(frameIndex).objectReferenceValue != expectedFrames[frameIndex])
                {
                    throw new InvalidOperationException($"Sticky frame reference is incorrect: {frameIndex}");
                }
            }
        }

        private static void VerifyMonsterPrefab(
            GameObject monsterPrefab,
            Sprite[] expectedFrames,
            MonsterPrefabCatalog catalog)
        {
            MonsterDefinition definition = monsterPrefab.GetComponent<MonsterDefinition>();
            MonsterController controller = monsterPrefab.GetComponent<MonsterController>();
            MonsterView monsterView = monsterPrefab.GetComponent<MonsterView>();
            MonsterStickyTrailBehavior stickyBehavior = monsterPrefab.GetComponent<MonsterStickyTrailBehavior>();
            Transform visual = monsterPrefab.transform.Find("SpriteVisual");
            SpriteRenderer spriteRenderer = visual != null ? visual.GetComponent<SpriteRenderer>() : null;
            DirectionalSpriteAnimator animator = visual != null ? visual.GetComponent<DirectionalSpriteAnimator>() : null;
            MeshRenderer legacyRenderer = monsterPrefab.GetComponent<MeshRenderer>();
            if (definition == null || controller == null || monsterView == null || stickyBehavior == null ||
                visual == null || spriteRenderer == null || animator == null || legacyRenderer == null)
            {
                throw new InvalidOperationException("Jeon Woochi monster prefab components are incomplete.");
            }

            if (definition.TypeId != "jeon_woochi" || definition.DisplayName != "전우치" ||
                definition.Size <= 0f || definition.Speed < 0f || definition.SpawnWeight < 0f ||
                stickyBehavior.StickyZonePrefab == null || stickyBehavior.DropInterval <= 0f ||
                legacyRenderer.enabled || spriteRenderer.sprite != expectedFrames[0] ||
                monsterView.VisualRoot != visual || visual.localPosition != new Vector3(0f, -0.5f, 0f) ||
                visual.localScale != Vector3.one * VisualScale)
            {
                throw new InvalidOperationException("Jeon Woochi monster prefab settings are incorrect.");
            }

            SerializedObject serializedAnimator = new SerializedObject(animator);
            SerializedProperty directionFrames = serializedAnimator.FindProperty("directionFrames");
            if (directionFrames.arraySize != expectedFrames.Length)
            {
                throw new InvalidOperationException("Jeon Woochi directional frame count is incorrect.");
            }

            for (int frameIndex = 0; frameIndex < expectedFrames.Length; frameIndex++)
            {
                if (directionFrames.GetArrayElementAtIndex(frameIndex).objectReferenceValue != expectedFrames[frameIndex])
                {
                    throw new InvalidOperationException($"Jeon Woochi frame reference is incorrect: {frameIndex}");
                }
            }

            for (int prefabIndex = 0; prefabIndex < catalog.MonsterPrefabs.Count; prefabIndex++)
            {
                if (catalog.MonsterPrefabs[prefabIndex] == controller)
                {
                    return;
                }
            }

            throw new InvalidOperationException("Jeon Woochi prefab is not registered in the monster catalog.");
        }

        private static void VerifyRequestedDefaults()
        {
            GameObject monsterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MonsterPrefabPath);
            GameObject stickyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StickyPrefabPath);
            MonsterDefinition definition = monsterPrefab.GetComponent<MonsterDefinition>();
            MonsterStickyTrailBehavior behavior = monsterPrefab.GetComponent<MonsterStickyTrailBehavior>();
            StickySlowZone zone = stickyPrefab.GetComponent<StickySlowZone>();
            if (!Mathf.Approximately(definition.Size, MonsterDefinition.DefaultSize) ||
                !Mathf.Approximately(definition.Speed, MonsterDefinition.DefaultSpeed) ||
                !Mathf.Approximately(definition.FleeDistance, MonsterDefinition.DefaultFleeDistance) ||
                !Mathf.Approximately(definition.SpawnWeight, MonsterDefinition.DefaultSpawnWeight) ||
                !Mathf.Approximately(behavior.DropInterval, DropInterval) ||
                !Mathf.Approximately(zone.Radius, StickyRadius) ||
                !Mathf.Approximately(zone.SpeedMultiplier, StickySpeedMultiplier))
            {
                throw new InvalidOperationException("Jeon Woochi requested defaults were not applied.");
            }
        }

        private static void VerifyStickySlow(GameObject stickyPrefab, GameConfig config)
        {
            GameObject truckObject = new GameObject("Sticky Slow Verification Truck");
            StickySlowZone firstZone = null;
            StickySlowZone secondZone = null;
            try
            {
                TruckController truck = truckObject.AddComponent<TruckController>();
                truck.Initialize(config);
                TruckStickySlowController slowController = truckObject.AddComponent<TruckStickySlowController>();
                firstZone = (PrefabUtility.InstantiatePrefab(stickyPrefab) as GameObject)?.GetComponent<StickySlowZone>();
                secondZone = (PrefabUtility.InstantiatePrefab(stickyPrefab) as GameObject)?.GetComponent<StickySlowZone>();
                if (firstZone == null || secondZone == null)
                {
                    throw new InvalidOperationException("Sticky slow verification zones could not be created.");
                }

                firstZone.transform.position = truckObject.transform.position;
                secondZone.transform.position = truckObject.transform.position;
                firstZone.Initialize(slowController);
                secondZone.Initialize(slowController);
                truck.UpdateTruck(Vector2.up, 1f / config.ReferenceFrameRate);
                float unslowedCurrentSpeed = truck.CurrentSpeed;
                float unslowedMaxSpeed = truck.GetStats().MaxSpeed;
                slowController.UpdateSlowState();
                if (!Mathf.Approximately(truck.EnvironmentSpeedMultiplier, firstZone.SpeedMultiplier) ||
                    !Mathf.Approximately(
                        truck.CurrentSpeed,
                        unslowedCurrentSpeed * firstZone.SpeedMultiplier) ||
                    !Mathf.Approximately(truck.GetStats().MaxSpeed, unslowedMaxSpeed) ||
                    slowController.ActiveZoneCount != 2)
                {
                    throw new InvalidOperationException("Truck did not slow inside overlapping sticky zones.");
                }

                Object.DestroyImmediate(firstZone.gameObject);
                firstZone = null;
                slowController.UpdateSlowState();
                if (!Mathf.Approximately(truck.EnvironmentSpeedMultiplier, secondZone.SpeedMultiplier))
                {
                    throw new InvalidOperationException("Overlapping sticky zone was cleared too early.");
                }

                secondZone.transform.position = Vector3.right * (secondZone.Radius + 1f);
                slowController.UpdateSlowState();
                if (!Mathf.Approximately(truck.EnvironmentSpeedMultiplier, 1f) ||
                    !Mathf.Approximately(truck.CurrentSpeed, unslowedCurrentSpeed))
                {
                    throw new InvalidOperationException("Truck sticky slow did not clear outside the zone.");
                }
            }
            finally
            {
                if (firstZone != null)
                {
                    Object.DestroyImmediate(firstZone.gameObject);
                }

                if (secondZone != null)
                {
                    Object.DestroyImmediate(secondZone.gameObject);
                }

                Object.DestroyImmediate(truckObject);
            }
        }

        private static void VerifyStickyDropFrameRates(GameObject monsterPrefab, GameConfig config)
        {
            VerifyStickyDrop(monsterPrefab, config, 30);
            VerifyStickyDrop(monsterPrefab, config, 60);
            VerifyStickyDrop(monsterPrefab, config, 120);
        }

        private static void VerifyStickyDrop(GameObject monsterPrefab, GameConfig config, int frameRate)
        {
            GameObject truckObject = new GameObject($"Sticky Drop {frameRate} FPS Verification Truck");
            GameObject instance = PrefabUtility.InstantiatePrefab(monsterPrefab) as GameObject;
            try
            {
                TruckController truck = truckObject.AddComponent<TruckController>();
                truck.Initialize(config);
                truckObject.transform.position = Vector3.one * 100f;
                MonsterDefinition definition = instance.GetComponent<MonsterDefinition>();
                MonsterController controller = instance.GetComponent<MonsterController>();
                MonsterStickyTrailBehavior behavior = instance.GetComponent<MonsterStickyTrailBehavior>();
                controller.Initialize(definition.CreateData(), truckObject.transform, 0f, config.ReferenceFrameRate);
                int initialZoneCount = CountSceneStickyZones();
                float deltaTime = 1f / frameRate;
                int frameCount = Mathf.CeilToInt(behavior.DropInterval * frameRate) + 1;
                for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
                {
                    controller.UpdateMonster(
                        (frameIndex + 1) * deltaTime * 1000f,
                        0f,
                        3.096f,
                        deltaTime * config.ReferenceFrameRate,
                        deltaTime,
                        0f,
                        1f,
                        false);
                }

                if (CountSceneStickyZones() != initialZoneCount + 1)
                {
                    throw new InvalidOperationException($"Sticky drop timing failed at {frameRate} FPS.");
                }
            }
            finally
            {
                DestroySceneStickyZones();
                Object.DestroyImmediate(instance);
                Object.DestroyImmediate(truckObject);
            }
        }

        private static void VerifyPausedDrop(GameObject monsterPrefab, GameConfig config)
        {
            GameObject truckObject = new GameObject("Paused Sticky Drop Verification Truck");
            GameObject instance = PrefabUtility.InstantiatePrefab(monsterPrefab) as GameObject;
            try
            {
                TruckController truck = truckObject.AddComponent<TruckController>();
                truck.Initialize(config);
                truckObject.transform.position = Vector3.one * 100f;
                MonsterDefinition definition = instance.GetComponent<MonsterDefinition>();
                MonsterController controller = instance.GetComponent<MonsterController>();
                controller.Initialize(definition.CreateData(), truckObject.transform, 0f, config.ReferenceFrameRate);

                for (int frameIndex = 0; frameIndex < 240; frameIndex++)
                {
                    controller.UpdateMonster(
                        (frameIndex + 1) * 1000f / 60f,
                        0f,
                        3.096f,
                        1f,
                        1f / 60f,
                        0f,
                        1f,
                        true);
                }

                if (CountSceneStickyZones() != 0)
                {
                    throw new InvalidOperationException("Jeon Woochi dropped sticky mist while time was paused.");
                }
            }
            finally
            {
                DestroySceneStickyZones();
                Object.DestroyImmediate(instance);
                Object.DestroyImmediate(truckObject);
            }
        }

        private static int CountSceneStickyZones()
        {
            StickySlowZone[] zones = Object.FindObjectsByType<StickySlowZone>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            int count = 0;
            for (int zoneIndex = 0; zoneIndex < zones.Length; zoneIndex++)
            {
                if (zones[zoneIndex].gameObject.scene.IsValid())
                {
                    count++;
                }
            }

            return count;
        }

        private static void DestroySceneStickyZones()
        {
            StickySlowZone[] zones = Object.FindObjectsByType<StickySlowZone>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int zoneIndex = 0; zoneIndex < zones.Length; zoneIndex++)
            {
                if (zones[zoneIndex].gameObject.scene.IsValid())
                {
                    Object.DestroyImmediate(zones[zoneIndex].gameObject);
                }
            }
        }

        private static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }
    }
}
