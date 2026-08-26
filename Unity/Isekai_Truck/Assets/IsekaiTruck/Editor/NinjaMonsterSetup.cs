using System;
using IsekaiTruck.Monsters;
using IsekaiTruck.Visuals;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace IsekaiTruck.Editor
{
    public static class NinjaMonsterSetup
    {
        private const string SpritePath = "Assets/IsekaiTruck/Art/Sprites/Ninja5DirectionWalk.png";
        private const string SpritePrefix = "Ninja";
        private const string SubstitutionSpritePath =
            "Assets/IsekaiTruck/Art/Sprites/NinjaSubstitutionEffect.png";
        private const string SubstitutionSpritePrefix = "NinjaSubstitution";
        private const string SubstitutionEffectPrefabPath =
            "Assets/IsekaiTruck/Prefabs/Effects/NinjaSubstitutionEffect.prefab";
        private const string TeleportSmokeSpritePath =
            "Assets/IsekaiTruck/Art/Sprites/NinjaTeleportSmoke.png";
        private const string TeleportSmokeSpritePrefix = "NinjaTeleportSmoke";
        private const string TeleportSmokeEffectPrefabPath =
            "Assets/IsekaiTruck/Prefabs/Effects/NinjaTeleportSmokeEffect.prefab";
        private const string NinjaPrefabPath = "Assets/IsekaiTruck/Prefabs/Monsters/Ninja.prefab";
        private const string CatalogPath = "Assets/IsekaiTruck/Config/MonsterPrefabCatalog.asset";
        private const float PixelsPerUnit = 112f;
        private const float SubstitutionPixelsPerUnit = 128f;
        private const float TeleportSmokePixelsPerUnit = 100f;
        private const float VisualScale = 1.5f;
        private const float SubstitutionVisualScale = 4f;
        private const float TeleportSmokeVisualScale = 2f;
        private const float AnimationFramesPerSecond = 12f;
        private const float SubstitutionFramesPerSecond = 12f;
        private const float TeleportSmokeFramesPerSecond = 12f;
        private const float TeleportDistanceMultiplier = 2f;
        private const int SubstitutionColumns = 4;
        private const int SubstitutionRows = 4;
        private const int SubstitutionFrameCount = 8;
        private const int SubstitutionSlicePadding = 4;
        private const byte SubstitutionAlphaThreshold = 8;
        private const int TeleportSmokeColumns = 3;
        private const int TeleportSmokeRows = 3;
        private const int TeleportSmokeFrameCount = 9;

        [MenuItem("Isekai Truck/Setup Ninja Monster")]
        public static void Setup()
        {
            AssetDatabase.Refresh();
            DirectionalMonsterSpriteSheetUtility.ConfigureImporter(SpritePath, SpritePrefix, PixelsPerUnit);
            ConfigureSubstitutionImporter();
            ConfigureTeleportSmokeImporter();

            Sprite[] frames = DirectionalMonsterSpriteSheetUtility.LoadFrames(SpritePath, SpritePrefix);
            Sprite[] substitutionFrames = LoadSubstitutionFrames();
            Sprite[] teleportSmokeFrames = LoadTeleportSmokeFrames();
            SpriteSequenceEffect substitutionEffectPrefab =
                CreateOrUpdateSubstitutionEffectPrefab(substitutionFrames);
            SpriteSequenceEffect teleportSmokeEffectPrefab =
                CreateOrUpdateTeleportSmokeEffectPrefab(teleportSmokeFrames);
            CreateOrUpdatePrefab(frames, substitutionEffectPrefab, teleportSmokeEffectPrefab);

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
                EditorUtility.DisplayDialog(
                    "Isekai Truck",
                    "닌자의 한 번 생존과 순간이동 연막 이펙트를 추가했습니다.",
                    "확인");
            }
        }

        public static void Verify()
        {
            TextureImporter importer = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
            TextureImporter substitutionImporter =
                AssetImporter.GetAtPath(SubstitutionSpritePath) as TextureImporter;
            TextureImporter teleportSmokeImporter =
                AssetImporter.GetAtPath(TeleportSmokeSpritePath) as TextureImporter;
            Sprite[] frames = DirectionalMonsterSpriteSheetUtility.LoadFrames(SpritePath, SpritePrefix);
            Sprite[] substitutionFrames = LoadSubstitutionFrames();
            Sprite[] teleportSmokeFrames = LoadTeleportSmokeFrames();
            SpriteSequenceEffect substitutionEffectPrefab =
                AssetDatabase.LoadAssetAtPath<SpriteSequenceEffect>(SubstitutionEffectPrefabPath);
            SpriteSequenceEffect teleportSmokeEffectPrefab =
                AssetDatabase.LoadAssetAtPath<SpriteSequenceEffect>(TeleportSmokeEffectPrefabPath);
            GameObject ninjaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(NinjaPrefabPath);
            MonsterPrefabCatalog catalog = AssetDatabase.LoadAssetAtPath<MonsterPrefabCatalog>(CatalogPath);
            if (importer == null || substitutionImporter == null || teleportSmokeImporter == null ||
                substitutionEffectPrefab == null || teleportSmokeEffectPrefab == null ||
                ninjaPrefab == null || catalog == null)
            {
                throw new InvalidOperationException("Ninja monster assets are missing.");
            }

            if (importer.textureType != TextureImporterType.Sprite ||
                importer.spriteImportMode != SpriteImportMode.Multiple ||
                !importer.alphaIsTransparency ||
                !Mathf.Approximately(importer.spritePixelsPerUnit, PixelsPerUnit))
            {
                throw new InvalidOperationException("Ninja walk sheet importer is not configured as expected.");
            }

            if (substitutionImporter.textureType != TextureImporterType.Sprite ||
                substitutionImporter.spriteImportMode != SpriteImportMode.Multiple ||
                !substitutionImporter.alphaIsTransparency ||
                !Mathf.Approximately(
                    substitutionImporter.spritePixelsPerUnit,
                    SubstitutionPixelsPerUnit))
            {
                throw new InvalidOperationException(
                    "Ninja substitution sheet importer is not configured as expected.");
            }

            if (teleportSmokeImporter.textureType != TextureImporterType.Sprite ||
                teleportSmokeImporter.spriteImportMode != SpriteImportMode.Multiple ||
                !teleportSmokeImporter.alphaIsTransparency ||
                !Mathf.Approximately(
                    teleportSmokeImporter.spritePixelsPerUnit,
                    TeleportSmokePixelsPerUnit))
            {
                throw new InvalidOperationException(
                    "Ninja teleport smoke sheet importer is not configured as expected.");
            }

            MonsterDefinition definition = ninjaPrefab.GetComponent<MonsterDefinition>();
            MonsterController controller = ninjaPrefab.GetComponent<MonsterController>();
            MonsterView monsterView = ninjaPrefab.GetComponent<MonsterView>();
            MonsterOneTimeSurvivalBehavior survivalBehavior =
                ninjaPrefab.GetComponent<MonsterOneTimeSurvivalBehavior>();
            Transform visual = ninjaPrefab.transform.Find("SpriteVisual");
            SpriteRenderer spriteRenderer = visual != null ? visual.GetComponent<SpriteRenderer>() : null;
            DirectionalSpriteAnimator directionalAnimator =
                visual != null ? visual.GetComponent<DirectionalSpriteAnimator>() : null;
            MeshRenderer legacyRenderer = ninjaPrefab.GetComponent<MeshRenderer>();
            if (definition == null || controller == null || monsterView == null || survivalBehavior == null ||
                visual == null || spriteRenderer == null || directionalAnimator == null || legacyRenderer == null)
            {
                throw new InvalidOperationException("Ninja prefab components are incomplete.");
            }

            if (definition.TypeId != "ninja" || definition.DisplayName != "닌자")
            {
                throw new InvalidOperationException("Ninja identity settings are incorrect.");
            }

            if (!Mathf.Approximately(definition.Size, MonsterDefinition.DefaultSize) ||
                !Mathf.Approximately(definition.Speed, MonsterDefinition.DefaultSpeed) ||
                !Mathf.Approximately(definition.FleeDistance, MonsterDefinition.DefaultFleeDistance) ||
                !Mathf.Approximately(definition.SpawnWeight, MonsterDefinition.DefaultSpawnWeight))
            {
                throw new InvalidOperationException("Ninja stat settings are incorrect.");
            }

            if (!Mathf.Approximately(
                    survivalBehavior.TeleportDistanceMultiplier,
                    TeleportDistanceMultiplier) ||
                AssetDatabase.GetAssetPath(survivalBehavior.SubstitutionEffectPrefab) !=
                    SubstitutionEffectPrefabPath ||
                AssetDatabase.GetAssetPath(survivalBehavior.TeleportSmokeEffectPrefab) !=
                    TeleportSmokeEffectPrefabPath)
            {
                throw new InvalidOperationException("Ninja survival settings are incorrect.");
            }

            if (legacyRenderer.enabled || spriteRenderer.sprite != frames[0] || monsterView.VisualRoot != visual ||
                visual.localPosition != new Vector3(0f, -0.5f, 0f) || visual.localRotation != Quaternion.identity ||
                visual.localScale != Vector3.one * VisualScale)
            {
                throw new InvalidOperationException("Ninja sprite visual is not configured as expected.");
            }

            SerializedObject serializedView = new SerializedObject(monsterView);
            if (serializedView.FindProperty("directionalSpriteAnimator").objectReferenceValue != directionalAnimator)
            {
                throw new InvalidOperationException("Ninja directional animator is not assigned to MonsterView.");
            }

            VerifyDirectionalFrames(directionalAnimator, frames);
            VerifySubstitutionEffect(substitutionEffectPrefab, substitutionFrames);
            VerifyTeleportSmokeEffect(teleportSmokeEffectPrefab, teleportSmokeFrames);
            VerifyCatalog(catalog, controller);
            VerifyOneTimeSurvival(ninjaPrefab, substitutionEffectPrefab, teleportSmokeEffectPrefab);
            Debug.Log("Ninja monster setup verification passed.");
        }

        private static void ConfigureSubstitutionImporter()
        {
            TextureImporter importer = AssetImporter.GetAtPath(SubstitutionSpritePath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("Ninja substitution sheet importer was not created.");
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = SubstitutionPixelsPerUnit;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.sRGBTexture = true;
            importer.maxTextureSize = 1024;
            importer.isReadable = true;
            importer.SaveAndReimport();

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(SubstitutionSpritePath);
            if (texture == null)
            {
                throw new InvalidOperationException("Ninja substitution sheet could not be loaded.");
            }

#pragma warning disable CS0618
            importer.spritesheet = BuildSubstitutionMetadata(texture);
#pragma warning restore CS0618
            importer.isReadable = false;
            importer.SaveAndReimport();
        }

        private static void ConfigureTeleportSmokeImporter()
        {
            TextureImporter importer = AssetImporter.GetAtPath(TeleportSmokeSpritePath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("Ninja teleport smoke sheet importer was not created.");
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = TeleportSmokePixelsPerUnit;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.sRGBTexture = true;
            importer.maxTextureSize = 1024;

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TeleportSmokeSpritePath);
            if (texture == null)
            {
                throw new InvalidOperationException("Ninja teleport smoke sheet could not be loaded.");
            }

#pragma warning disable CS0618
            importer.spritesheet = BuildTeleportSmokeMetadata(texture);
#pragma warning restore CS0618
            importer.SaveAndReimport();
        }

        private static SpriteMetaData[] BuildTeleportSmokeMetadata(Texture2D texture)
        {
            SpriteMetaData[] metadata = new SpriteMetaData[TeleportSmokeFrameCount];
            for (int frameIndex = 0; frameIndex < metadata.Length; frameIndex++)
            {
                int column = frameIndex % TeleportSmokeColumns;
                int rowFromTop = frameIndex / TeleportSmokeColumns;
                int rowFromBottom = TeleportSmokeRows - rowFromTop - 1;
                int minX = Mathf.FloorToInt(column * texture.width / (float)TeleportSmokeColumns);
                int maxX = Mathf.FloorToInt((column + 1) * texture.width / (float)TeleportSmokeColumns);
                int minY = Mathf.FloorToInt(rowFromBottom * texture.height / (float)TeleportSmokeRows);
                int maxY = Mathf.FloorToInt((rowFromBottom + 1) * texture.height / (float)TeleportSmokeRows);

                metadata[frameIndex] = new SpriteMetaData
                {
                    name = GetTeleportSmokeSpriteName(frameIndex),
                    rect = new Rect(minX, minY, maxX - minX, maxY - minY),
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                    border = Vector4.zero
                };
            }

            return metadata;
        }

        private static SpriteMetaData[] BuildSubstitutionMetadata(Texture2D texture)
        {
            if (texture.width % SubstitutionColumns != 0 || texture.height % SubstitutionRows != 0)
            {
                throw new InvalidOperationException("Ninja substitution sheet is not a 4x4 grid.");
            }

            int cellWidth = texture.width / SubstitutionColumns;
            int cellHeight = texture.height / SubstitutionRows;
            Color32[] pixels = texture.GetPixels32();
            SpriteMetaData[] metadata = new SpriteMetaData[SubstitutionFrameCount];

            for (int frameIndex = 0; frameIndex < SubstitutionFrameCount; frameIndex++)
            {
                int column = frameIndex % SubstitutionColumns;
                int rowFromTop = frameIndex / SubstitutionColumns;
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
                int paddedMinX = Mathf.Max(cellMinX, opaqueBounds.xMin - SubstitutionSlicePadding);
                int paddedMinY = Mathf.Max(cellMinY, opaqueBounds.yMin - SubstitutionSlicePadding);
                int paddedMaxX = Mathf.Min(
                    cellMinX + cellWidth - 1,
                    opaqueBounds.xMax - 1 + SubstitutionSlicePadding);
                int paddedMaxY = Mathf.Min(
                    cellMinY + cellHeight - 1,
                    opaqueBounds.yMax - 1 + SubstitutionSlicePadding);
                int spriteWidth = paddedMaxX - paddedMinX + 1;
                int spriteHeight = paddedMaxY - paddedMinY + 1;
                float pivotY = (opaqueBounds.yMin - paddedMinY) / (float)spriteHeight;

                metadata[frameIndex] = new SpriteMetaData
                {
                    name = GetSubstitutionSpriteName(frameIndex),
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

            for (int y = cellMinY; y < cellMinY + cellHeight; y++)
            {
                int rowOffset = y * textureWidth;
                for (int x = cellMinX; x < cellMinX + cellWidth; x++)
                {
                    if (pixels[rowOffset + x].a <= SubstitutionAlphaThreshold)
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
                throw new InvalidOperationException(
                    $"Ninja substitution frame is empty: {frameIndex}");
            }

            return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        private static Sprite[] LoadSubstitutionFrames()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(SubstitutionSpritePath);
            Sprite[] frames = new Sprite[SubstitutionFrameCount];

            for (int frameIndex = 0; frameIndex < frames.Length; frameIndex++)
            {
                string spriteName = GetSubstitutionSpriteName(frameIndex);
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
                    throw new InvalidOperationException(
                        $"Ninja substitution frame is missing: {spriteName}");
                }
            }

            return frames;
        }

        private static string GetSubstitutionSpriteName(int frameIndex)
        {
            return $"{SubstitutionSpritePrefix}_{frameIndex}";
        }

        private static Sprite[] LoadTeleportSmokeFrames()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(TeleportSmokeSpritePath);
            Sprite[] frames = new Sprite[TeleportSmokeFrameCount];

            for (int frameIndex = 0; frameIndex < frames.Length; frameIndex++)
            {
                string spriteName = GetTeleportSmokeSpriteName(frameIndex);
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
                    throw new InvalidOperationException(
                        $"Ninja teleport smoke frame is missing: {spriteName}");
                }
            }

            return frames;
        }

        private static string GetTeleportSmokeSpriteName(int frameIndex)
        {
            return $"{TeleportSmokeSpritePrefix}_{frameIndex}";
        }

        private static SpriteSequenceEffect CreateOrUpdateSubstitutionEffectPrefab(Sprite[] frames)
        {
            GameObject root = new GameObject("NinjaSubstitutionEffect");
            try
            {
                root.transform.localScale = Vector3.one * SubstitutionVisualScale;
                SpriteRenderer spriteRenderer = root.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = frames[0];
                spriteRenderer.color = Color.white;
                spriteRenderer.spriteSortPoint = SpriteSortPoint.Pivot;
                spriteRenderer.sortingOrder = 2;
                spriteRenderer.shadowCastingMode = ShadowCastingMode.Off;
                spriteRenderer.receiveShadows = false;
                root.AddComponent<BillboardSpriteView>();
                SpriteSequenceEffect sequenceEffect = root.AddComponent<SpriteSequenceEffect>();
                sequenceEffect.Configure(
                    spriteRenderer,
                    frames,
                    SubstitutionFramesPerSecond,
                    true);

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, SubstitutionEffectPrefabPath);
                SpriteSequenceEffect prefabEffect = prefab != null
                    ? prefab.GetComponent<SpriteSequenceEffect>()
                    : null;
                if (prefabEffect == null)
                {
                    throw new InvalidOperationException("Ninja substitution effect prefab could not be saved.");
                }

                return prefabEffect;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static SpriteSequenceEffect CreateOrUpdateTeleportSmokeEffectPrefab(Sprite[] frames)
        {
            GameObject root = new GameObject("NinjaTeleportSmokeEffect");
            try
            {
                root.transform.localScale = Vector3.one * TeleportSmokeVisualScale;
                SpriteRenderer spriteRenderer = root.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = frames[0];
                spriteRenderer.color = Color.white;
                spriteRenderer.spriteSortPoint = SpriteSortPoint.Pivot;
                spriteRenderer.sortingOrder = 3;
                spriteRenderer.shadowCastingMode = ShadowCastingMode.Off;
                spriteRenderer.receiveShadows = false;
                root.AddComponent<BillboardSpriteView>();
                SpriteSequenceEffect sequenceEffect = root.AddComponent<SpriteSequenceEffect>();
                sequenceEffect.Configure(
                    spriteRenderer,
                    frames,
                    TeleportSmokeFramesPerSecond,
                    true);

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, TeleportSmokeEffectPrefabPath);
                SpriteSequenceEffect prefabEffect = prefab != null
                    ? prefab.GetComponent<SpriteSequenceEffect>()
                    : null;
                if (prefabEffect == null)
                {
                    throw new InvalidOperationException("Ninja teleport smoke effect prefab could not be saved.");
                }

                return prefabEffect;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void CreateOrUpdatePrefab(
            Sprite[] frames,
            SpriteSequenceEffect substitutionEffectPrefab,
            SpriteSequenceEffect teleportSmokeEffectPrefab)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(NinjaPrefabPath);
            bool isNewPrefab = prefab == null;
            GameObject root = isNewPrefab
                ? GameObject.CreatePrimitive(PrimitiveType.Sphere)
                : PrefabUtility.LoadPrefabContents(NinjaPrefabPath);

            try
            {
                root.name = "Ninja";
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

                if (definition.TypeId != "ninja")
                {
                    throw new InvalidOperationException("Existing Ninja prefab uses a different Type ID.");
                }

                GetOrAddComponent<MonsterController>(root);
                MonsterOneTimeSurvivalBehavior survivalBehavior =
                    GetOrAddComponent<MonsterOneTimeSurvivalBehavior>(root);
                SerializedObject serializedSurvival = new SerializedObject(survivalBehavior);
                if (isNewPrefab)
                {
                    serializedSurvival.FindProperty("teleportDistanceMultiplier").floatValue =
                        TeleportDistanceMultiplier;
                }

                serializedSurvival.FindProperty("substitutionEffectPrefab").objectReferenceValue =
                    substitutionEffectPrefab;
                serializedSurvival.FindProperty("teleportSmokeEffectPrefab").objectReferenceValue =
                    teleportSmokeEffectPrefab;
                serializedSurvival.ApplyModifiedPropertiesWithoutUndo();

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

                PrefabUtility.SaveAsPrefabAsset(root, NinjaPrefabPath);
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
                "ninja",
                "닌자",
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
                throw new InvalidOperationException("Ninja directional animation settings are incomplete.");
            }

            for (int frameIndex = 0; frameIndex < expectedFrames.Length; frameIndex++)
            {
                if (framesProperty.GetArrayElementAtIndex(frameIndex).objectReferenceValue !=
                    expectedFrames[frameIndex])
                {
                    throw new InvalidOperationException(
                        $"Ninja directional frame reference is incorrect: {frameIndex}");
                }
            }
        }

        private static void VerifyCatalog(MonsterPrefabCatalog catalog, MonsterController ninjaController)
        {
            for (int i = 0; i < catalog.MonsterPrefabs.Count; i++)
            {
                if (catalog.MonsterPrefabs[i] == ninjaController)
                {
                    return;
                }
            }

            throw new InvalidOperationException("Ninja prefab is not registered in the monster catalog.");
        }

        private static void VerifySubstitutionEffect(
            SpriteSequenceEffect effectPrefab,
            Sprite[] expectedFrames)
        {
            SpriteRenderer spriteRenderer = effectPrefab.GetComponent<SpriteRenderer>();
            BillboardSpriteView billboard = effectPrefab.GetComponent<BillboardSpriteView>();
            if (spriteRenderer == null || billboard == null ||
                effectPrefab.FrameCount != SubstitutionFrameCount ||
                !Mathf.Approximately(effectPrefab.FramesPerSecond, SubstitutionFramesPerSecond) ||
                !Mathf.Approximately(
                    effectPrefab.Duration,
                    SubstitutionFrameCount / SubstitutionFramesPerSecond) ||
                !effectPrefab.DestroyOnComplete ||
                effectPrefab.transform.localScale != Vector3.one * SubstitutionVisualScale ||
                spriteRenderer.sprite != expectedFrames[0] ||
                spriteRenderer.sortingOrder != 2)
            {
                throw new InvalidOperationException("Ninja substitution effect prefab is incomplete.");
            }

            SerializedObject serializedEffect = new SerializedObject(effectPrefab);
            SerializedProperty framesProperty = serializedEffect.FindProperty("frames");
            if (framesProperty == null || framesProperty.arraySize != expectedFrames.Length)
            {
                throw new InvalidOperationException("Ninja substitution effect frames are incomplete.");
            }

            for (int frameIndex = 0; frameIndex < expectedFrames.Length; frameIndex++)
            {
                if (framesProperty.GetArrayElementAtIndex(frameIndex).objectReferenceValue !=
                    expectedFrames[frameIndex])
                {
                    throw new InvalidOperationException(
                        $"Ninja substitution frame reference is incorrect: {frameIndex}");
                }
            }
        }

        private static void VerifyTeleportSmokeEffect(
            SpriteSequenceEffect effectPrefab,
            Sprite[] expectedFrames)
        {
            SpriteRenderer spriteRenderer = effectPrefab.GetComponent<SpriteRenderer>();
            BillboardSpriteView billboard = effectPrefab.GetComponent<BillboardSpriteView>();
            if (spriteRenderer == null || billboard == null ||
                effectPrefab.FrameCount != TeleportSmokeFrameCount ||
                !Mathf.Approximately(effectPrefab.FramesPerSecond, TeleportSmokeFramesPerSecond) ||
                !Mathf.Approximately(
                    effectPrefab.Duration,
                    TeleportSmokeFrameCount / TeleportSmokeFramesPerSecond) ||
                !effectPrefab.DestroyOnComplete ||
                effectPrefab.transform.localScale != Vector3.one * TeleportSmokeVisualScale ||
                spriteRenderer.sprite != expectedFrames[0] ||
                spriteRenderer.sortingOrder != 3)
            {
                throw new InvalidOperationException("Ninja teleport smoke effect prefab is incomplete.");
            }

            SerializedObject serializedEffect = new SerializedObject(effectPrefab);
            SerializedProperty framesProperty = serializedEffect.FindProperty("frames");
            if (framesProperty == null || framesProperty.arraySize != expectedFrames.Length)
            {
                throw new InvalidOperationException("Ninja teleport smoke effect frames are incomplete.");
            }

            for (int frameIndex = 0; frameIndex < expectedFrames.Length; frameIndex++)
            {
                if (framesProperty.GetArrayElementAtIndex(frameIndex).objectReferenceValue !=
                    expectedFrames[frameIndex])
                {
                    throw new InvalidOperationException(
                        $"Ninja teleport smoke frame reference is incorrect: {frameIndex}");
                }
            }
        }

        private static void VerifyOneTimeSurvival(
            GameObject ninjaPrefab,
            SpriteSequenceEffect substitutionEffectPrefab,
            SpriteSequenceEffect teleportSmokeEffectPrefab)
        {
            GameObject truckObject = new GameObject("Ninja Survival Verification Truck");
            GameObject instance = PrefabUtility.InstantiatePrefab(ninjaPrefab) as GameObject;
            SpriteSequenceEffect[] spawnedEffects = null;
            try
            {
                instance.transform.position = Vector3.right;
                MonsterDefinition definition = instance.GetComponent<MonsterDefinition>();
                MonsterController controller = instance.GetComponent<MonsterController>();
                MonsterOneTimeSurvivalBehavior survivalBehavior =
                    instance.GetComponent<MonsterOneTimeSurvivalBehavior>();
                controller.Initialize(definition.CreateData(), truckObject.transform, 0f, 60f);

                Vector3 positionBeforeFirstContact = instance.transform.position;
                MonsterContactResult firstResult = controller.ResolveContact(CreateContactContext(truckObject));
                float expectedTeleportDistance = definition.FleeDistance * TeleportDistanceMultiplier;
                float actualTeleportDistance =
                    Vector3.Distance(positionBeforeFirstContact, instance.transform.position);
                spawnedEffects = Object.FindObjectsByType<SpriteSequenceEffect>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
                SpriteSequenceEffect spawnedSubstitution = null;
                SpriteSequenceEffect spawnedTeleportSmoke = null;
                for (int effectIndex = 0; effectIndex < spawnedEffects.Length; effectIndex++)
                {
                    SpriteSequenceEffect effect = spawnedEffects[effectIndex];
                    if (!effect.gameObject.scene.IsValid())
                    {
                        continue;
                    }

                    if (effect.name.StartsWith(substitutionEffectPrefab.name, StringComparison.Ordinal))
                    {
                        spawnedSubstitution = effect;
                    }
                    else if (effect.name.StartsWith(teleportSmokeEffectPrefab.name, StringComparison.Ordinal))
                    {
                        spawnedTeleportSmoke = effect;
                    }
                }

                if (firstResult.Outcome != MonsterContactOutcome.Survived ||
                    !survivalBehavior.HasSurvived ||
                    !Mathf.Approximately(actualTeleportDistance, expectedTeleportDistance) ||
                    instance.transform.position.x <= positionBeforeFirstContact.x ||
                    survivalBehavior.SubstitutionEffectPrefab != substitutionEffectPrefab ||
                    survivalBehavior.TeleportSmokeEffectPrefab != teleportSmokeEffectPrefab ||
                    spawnedSubstitution == null ||
                    spawnedSubstitution.transform.position != positionBeforeFirstContact ||
                    spawnedTeleportSmoke == null ||
                    spawnedTeleportSmoke.transform.position != instance.transform.position ||
                    spawnedTeleportSmoke.transform.position == positionBeforeFirstContact)
                {
                    throw new InvalidOperationException(
                        "Ninja teleport effects were not placed at the expected positions.");
                }

                instance.transform.position = Vector3.right;
                MonsterContactResult secondResult = controller.ResolveContact(CreateContactContext(truckObject));
                if (secondResult.Outcome != MonsterContactOutcome.Defeated ||
                    instance.transform.position != Vector3.right)
                {
                    throw new InvalidOperationException("Ninja did not become defeatable after surviving once.");
                }
            }
            finally
            {
                if (spawnedEffects != null)
                {
                    for (int effectIndex = 0; effectIndex < spawnedEffects.Length; effectIndex++)
                    {
                        SpriteSequenceEffect effect = spawnedEffects[effectIndex];
                        if (effect != null && effect.gameObject.scene.IsValid())
                        {
                            Object.DestroyImmediate(effect.gameObject);
                        }
                    }
                }

                Object.DestroyImmediate(instance);
                Object.DestroyImmediate(truckObject);
            }
        }

        private static MonsterContactContext CreateContactContext(GameObject truckObject)
        {
            return new MonsterContactContext(
                truckObject.transform,
                null,
                1f,
                1.8f,
                Vector3.left,
                Vector3.forward
            );
        }

        private static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }
    }
}
