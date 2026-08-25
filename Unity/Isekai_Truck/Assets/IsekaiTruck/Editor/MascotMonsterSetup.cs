using System;
using IsekaiTruck.Monsters;
using IsekaiTruck.UI;
using IsekaiTruck.Visuals;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace IsekaiTruck.Editor
{
    public static class MascotMonsterSetup
    {
        private const string SpritePath = "Assets/IsekaiTruck/Art/Sprites/Mascot5DirectionWalk.png";
        private const string SpritePrefix = "Mascot";
        private const string FlyerSpritePath = "Assets/IsekaiTruck/Art/Sprites/MascotFlyerScreenEffect.png";
        private const string FlyerSpritePrefix = "MascotFlyer";
        private const string OverlayPrefabPath = "Assets/IsekaiTruck/Prefabs/Effects/MascotFlyerScreenOverlay.prefab";
        private const string MonsterPrefabPath = "Assets/IsekaiTruck/Prefabs/Monsters/Mascot.prefab";
        private const string CatalogPath = "Assets/IsekaiTruck/Config/MonsterPrefabCatalog.asset";
        private const float PixelsPerUnit = 112f;
        private const float FlyerPixelsPerUnit = 100f;
        private const float VisualScale = 1.5f;
        private const float AnimationFramesPerSecond = 12f;
        private const float OverlayDuration = 5f;
        private const float BuildupDuration = 0.8f;
        private const float FadeDuration = 1.5f;
        private const float FlyerOpacity = 0.6f;
        private const int OverlaySortingOrder = -10;
        private const int FlyerColumns = 3;
        private const int FlyerRows = 2;
        private const int FlyerFrameCount = 6;
        private const int SlicePadding = 4;
        private const byte AlphaThreshold = 8;

        [MenuItem("Isekai Truck/Setup Mascot Monster")]
        public static void Setup()
        {
            bool isNewMonster = AssetDatabase.LoadAssetAtPath<GameObject>(MonsterPrefabPath) == null;
            AssetDatabase.Refresh();
            DirectionalMonsterSpriteSheetUtility.ConfigureImporter(SpritePath, SpritePrefix, PixelsPerUnit);
            ConfigureFlyerImporter();

            Sprite[] frames = DirectionalMonsterSpriteSheetUtility.LoadFrames(SpritePath, SpritePrefix);
            Sprite[] flyerFrames = LoadFlyerFrames();
            FlyerScreenOverlay overlayPrefab = CreateOrUpdateOverlayPrefab(flyerFrames);
            CreateOrUpdateMonsterPrefab(frames, overlayPrefab);

            MonsterPrefabCatalog catalog = AssetDatabase.LoadAssetAtPath<MonsterPrefabCatalog>(CatalogPath);
            if (catalog == null)
            {
                throw new InvalidOperationException("Monster prefab catalog is missing.");
            }

            MonsterPrefabSetup.RefreshCatalog(catalog);
            AssetDatabase.SaveAssets();
            Verify();
            if (isNewMonster)
            {
                VerifyRequestedDefaults();
            }

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("Isekai Truck", "처치 시 전단지로 화면을 가리는 인형탈 주민을 추가했습니다.", "확인");
            }
        }

        public static void Verify()
        {
            TextureImporter importer = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
            TextureImporter flyerImporter = AssetImporter.GetAtPath(FlyerSpritePath) as TextureImporter;
            Sprite[] frames = DirectionalMonsterSpriteSheetUtility.LoadFrames(SpritePath, SpritePrefix);
            Sprite[] flyerFrames = LoadFlyerFrames();
            GameObject overlayPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(OverlayPrefabPath);
            GameObject monsterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MonsterPrefabPath);
            MonsterPrefabCatalog catalog = AssetDatabase.LoadAssetAtPath<MonsterPrefabCatalog>(CatalogPath);
            if (importer == null || flyerImporter == null || overlayPrefab == null || monsterPrefab == null ||
                catalog == null)
            {
                throw new InvalidOperationException("Mascot monster assets are missing.");
            }

            VerifyImporter(importer, PixelsPerUnit, 2048, "Mascot walk sheet");
            VerifyImporter(flyerImporter, FlyerPixelsPerUnit, 1024, "Mascot flyer sheet");
            VerifyOverlayPrefab(overlayPrefab, flyerFrames);
            VerifyMonsterPrefab(monsterPrefab, frames, catalog);
            VerifyDefeatEffect(monsterPrefab);
            Debug.Log("Mascot monster setup verification passed.");
        }

        private static void ConfigureFlyerImporter()
        {
            TextureImporter importer = AssetImporter.GetAtPath(FlyerSpritePath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("Mascot flyer sheet importer was not created.");
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = FlyerPixelsPerUnit;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.sRGBTexture = true;
            importer.maxTextureSize = 1024;
            importer.isReadable = true;
            importer.SaveAndReimport();

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(FlyerSpritePath);
            if (texture == null)
            {
                throw new InvalidOperationException("Mascot flyer sheet could not be loaded.");
            }

#pragma warning disable CS0618
            importer.spritesheet = BuildFlyerMetadata(texture);
#pragma warning restore CS0618
            importer.isReadable = false;
            importer.SaveAndReimport();
        }

        private static SpriteMetaData[] BuildFlyerMetadata(Texture2D texture)
        {
            if (texture.width % FlyerColumns > 1 || texture.height % FlyerRows > 1)
            {
                throw new InvalidOperationException("Mascot flyer sheet is not a 3x2 grid.");
            }

            int cellWidth = texture.width / FlyerColumns;
            int cellHeight = texture.height / FlyerRows;
            Color32[] pixels = texture.GetPixels32();
            SpriteMetaData[] metadata = new SpriteMetaData[FlyerFrameCount];

            for (int frameIndex = 0; frameIndex < FlyerFrameCount; frameIndex++)
            {
                int column = frameIndex % FlyerColumns;
                int rowFromTop = frameIndex / FlyerColumns;
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
                int paddedMinX = Mathf.Max(cellMinX, opaqueBounds.xMin - SlicePadding);
                int paddedMinY = Mathf.Max(cellMinY, opaqueBounds.yMin - SlicePadding);
                int paddedMaxX = Mathf.Min(cellMinX + cellWidth - 1, opaqueBounds.xMax - 1 + SlicePadding);
                int paddedMaxY = Mathf.Min(cellMinY + cellHeight - 1, opaqueBounds.yMax - 1 + SlicePadding);

                metadata[frameIndex] = new SpriteMetaData
                {
                    name = GetFlyerSpriteName(frameIndex),
                    rect = new Rect(
                        paddedMinX,
                        paddedMinY,
                        paddedMaxX - paddedMinX + 1,
                        paddedMaxY - paddedMinY + 1),
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
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
                throw new InvalidOperationException($"Mascot flyer frame is empty: {frameIndex}");
            }

            return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        private static Sprite[] LoadFlyerFrames()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(FlyerSpritePath);
            Sprite[] frames = new Sprite[FlyerFrameCount];

            for (int frameIndex = 0; frameIndex < frames.Length; frameIndex++)
            {
                string spriteName = GetFlyerSpriteName(frameIndex);
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
                    throw new InvalidOperationException($"Mascot flyer frame is missing: {spriteName}");
                }
            }

            return frames;
        }

        private static string GetFlyerSpriteName(int frameIndex)
        {
            return $"{FlyerSpritePrefix}_{frameIndex}";
        }

        private static FlyerScreenOverlay CreateOrUpdateOverlayPrefab(Sprite[] frames)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(OverlayPrefabPath);
            bool isNewPrefab = prefab == null;
            GameObject root = isNewPrefab
                ? new GameObject("MascotFlyerScreenOverlay", typeof(RectTransform))
                : PrefabUtility.LoadPrefabContents(OverlayPrefabPath);

            try
            {
                root.name = "MascotFlyerScreenOverlay";
                RectTransform rootTransform = root.GetComponent<RectTransform>();
                rootTransform.localScale = Vector3.one;

                Canvas canvas = GetOrAddComponent<Canvas>(root);
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = OverlaySortingOrder;

                CanvasScaler scaler = GetOrAddComponent<CanvasScaler>(root);
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                CanvasGroup canvasGroup = GetOrAddComponent<CanvasGroup>(root);
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;

                RectTransform viewportRoot = GetOrCreateRect(root.transform, "ViewportRoot");
                Stretch(viewportRoot);
                RectTransform imageTransform = GetOrCreateRect(viewportRoot, "FlyerImage");
                Stretch(imageTransform);
                Image image = GetOrAddComponent<Image>(imageTransform.gameObject);
                image.sprite = frames[0];
                image.color = new Color(1f, 1f, 1f, FlyerOpacity);
                image.preserveAspect = false;
                image.raycastTarget = false;

                FlyerScreenOverlay overlay = GetOrAddComponent<FlyerScreenOverlay>(root);
                overlay.Configure(
                    viewportRoot,
                    image,
                    canvasGroup,
                    frames,
                    OverlayDuration,
                    BuildupDuration,
                    FadeDuration);

                GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, OverlayPrefabPath);
                FlyerScreenOverlay savedOverlay = savedPrefab != null
                    ? savedPrefab.GetComponent<FlyerScreenOverlay>()
                    : null;
                if (savedOverlay == null)
                {
                    throw new InvalidOperationException("Mascot flyer overlay prefab could not be saved.");
                }

                return savedOverlay;
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

        private static void CreateOrUpdateMonsterPrefab(
            Sprite[] frames,
            FlyerScreenOverlay overlayPrefab)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MonsterPrefabPath);
            bool isNewPrefab = prefab == null;
            GameObject root = isNewPrefab
                ? GameObject.CreatePrimitive(PrimitiveType.Sphere)
                : PrefabUtility.LoadPrefabContents(MonsterPrefabPath);

            try
            {
                root.name = "Mascot";
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

                if (definition.TypeId != "mascot")
                {
                    throw new InvalidOperationException("Existing Mascot prefab uses a different Type ID.");
                }

                GetOrAddComponent<MonsterController>(root);
                MonsterFlyerDeathBehavior flyerBehavior = GetOrAddComponent<MonsterFlyerDeathBehavior>(root);
                SerializedObject serializedBehavior = new SerializedObject(flyerBehavior);
                serializedBehavior.FindProperty("screenOverlayPrefab").objectReferenceValue = overlayPrefab;
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
                DirectionalSpriteAnimator animator = GetOrAddComponent<DirectionalSpriteAnimator>(visual.gameObject);
                animator.Configure(spriteRenderer, frames, AnimationFramesPerSecond);

                monsterView.SetVisualRoot(visual);
                SerializedObject serializedView = new SerializedObject(monsterView);
                serializedView.FindProperty("faceMoveDirection").boolValue = false;
                serializedView.FindProperty("applyDefinitionColor").boolValue = false;
                serializedView.FindProperty("directionalSpriteAnimator").objectReferenceValue = animator;
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
                "mascot",
                "인형탈",
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

        private static void VerifyOverlayPrefab(GameObject overlayPrefab, Sprite[] expectedFrames)
        {
            Canvas canvas = overlayPrefab.GetComponent<Canvas>();
            CanvasGroup canvasGroup = overlayPrefab.GetComponent<CanvasGroup>();
            FlyerScreenOverlay overlay = overlayPrefab.GetComponent<FlyerScreenOverlay>();
            Transform viewportRoot = overlayPrefab.transform.Find("ViewportRoot");
            Image image = viewportRoot != null
                ? viewportRoot.Find("FlyerImage")?.GetComponent<Image>()
                : null;
            if (canvas == null || canvasGroup == null || overlay == null || viewportRoot == null || image == null)
            {
                throw new InvalidOperationException("Mascot flyer overlay prefab components are incomplete.");
            }

            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay || canvas.sortingOrder != OverlaySortingOrder ||
                overlayPrefab.GetComponent<RectTransform>().localScale != Vector3.one ||
                canvasGroup.blocksRaycasts || image.raycastTarget ||
                image.preserveAspect || image.sprite != expectedFrames[0] ||
                !Mathf.Approximately(image.color.a, FlyerOpacity) ||
                overlay.FrameCount != FlyerFrameCount ||
                !Mathf.Approximately(overlay.TotalDuration, OverlayDuration) ||
                !Mathf.Approximately(overlay.BuildupDuration, BuildupDuration) ||
                !Mathf.Approximately(overlay.FadeDuration, FadeDuration))
            {
                throw new InvalidOperationException("Mascot flyer overlay settings are incorrect.");
            }

            SerializedObject serializedOverlay = new SerializedObject(overlay);
            SerializedProperty framesProperty = serializedOverlay.FindProperty("frames");
            for (int frameIndex = 0; frameIndex < expectedFrames.Length; frameIndex++)
            {
                if (framesProperty.GetArrayElementAtIndex(frameIndex).objectReferenceValue != expectedFrames[frameIndex])
                {
                    throw new InvalidOperationException($"Mascot flyer frame reference is incorrect: {frameIndex}");
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
            MonsterFlyerDeathBehavior flyerBehavior = monsterPrefab.GetComponent<MonsterFlyerDeathBehavior>();
            Transform visual = monsterPrefab.transform.Find("SpriteVisual");
            SpriteRenderer spriteRenderer = visual != null ? visual.GetComponent<SpriteRenderer>() : null;
            DirectionalSpriteAnimator animator = visual != null ? visual.GetComponent<DirectionalSpriteAnimator>() : null;
            MeshRenderer legacyRenderer = monsterPrefab.GetComponent<MeshRenderer>();
            if (definition == null || controller == null || monsterView == null || flyerBehavior == null ||
                visual == null || spriteRenderer == null || animator == null || legacyRenderer == null)
            {
                throw new InvalidOperationException("Mascot monster prefab components are incomplete.");
            }

            if (definition.TypeId != "mascot" || definition.DisplayName != "인형탈" ||
                definition.Size <= 0f || definition.Speed < 0f || definition.SpawnWeight < 0f ||
                flyerBehavior.ScreenOverlayPrefab == null || legacyRenderer.enabled ||
                spriteRenderer.sprite != expectedFrames[0] || monsterView.VisualRoot != visual ||
                visual.localPosition != new Vector3(0f, -0.5f, 0f) ||
                visual.localScale != Vector3.one * VisualScale)
            {
                throw new InvalidOperationException("Mascot monster prefab settings are incorrect.");
            }

            SerializedObject serializedAnimator = new SerializedObject(animator);
            SerializedProperty directionFrames = serializedAnimator.FindProperty("directionFrames");
            if (directionFrames.arraySize != expectedFrames.Length)
            {
                throw new InvalidOperationException("Mascot directional frame count is incorrect.");
            }

            for (int frameIndex = 0; frameIndex < expectedFrames.Length; frameIndex++)
            {
                if (directionFrames.GetArrayElementAtIndex(frameIndex).objectReferenceValue != expectedFrames[frameIndex])
                {
                    throw new InvalidOperationException($"Mascot frame reference is incorrect: {frameIndex}");
                }
            }

            for (int prefabIndex = 0; prefabIndex < catalog.MonsterPrefabs.Count; prefabIndex++)
            {
                if (catalog.MonsterPrefabs[prefabIndex] == controller)
                {
                    return;
                }
            }

            throw new InvalidOperationException("Mascot prefab is not registered in the monster catalog.");
        }

        private static void VerifyRequestedDefaults()
        {
            GameObject monsterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MonsterPrefabPath);
            MonsterDefinition definition = monsterPrefab.GetComponent<MonsterDefinition>();
            if (!Mathf.Approximately(definition.Size, MonsterDefinition.DefaultSize) ||
                !Mathf.Approximately(definition.Speed, MonsterDefinition.DefaultSpeed) ||
                !Mathf.Approximately(definition.FleeDistance, MonsterDefinition.DefaultFleeDistance) ||
                !Mathf.Approximately(definition.SpawnWeight, MonsterDefinition.DefaultSpawnWeight))
            {
                throw new InvalidOperationException("Mascot requested defaults were not applied.");
            }
        }

        private static void VerifyDefeatEffect(GameObject monsterPrefab)
        {
            GameObject truckObject = new GameObject("Mascot Defeat Verification Truck");
            GameObject instance = PrefabUtility.InstantiatePrefab(monsterPrefab) as GameObject;
            FlyerScreenOverlay spawnedOverlay = null;
            try
            {
                MonsterDefinition definition = instance.GetComponent<MonsterDefinition>();
                MonsterController controller = instance.GetComponent<MonsterController>();
                controller.Initialize(definition.CreateData(), truckObject.transform, 0f, 60f);
                MonsterContactResult result = controller.ResolveContact(new MonsterContactContext(
                    truckObject.transform,
                    null,
                    1f,
                    1.8f,
                    Vector3.back,
                    Vector3.forward));
                spawnedOverlay = FindSceneOverlay();
                if (result.Outcome != MonsterContactOutcome.Defeated || spawnedOverlay == null)
                {
                    throw new InvalidOperationException("Mascot defeat did not create the flyer screen overlay.");
                }

                controller.ResolveContact(new MonsterContactContext(
                    truckObject.transform,
                    null,
                    1f,
                    1.8f,
                    Vector3.back,
                    Vector3.forward));
                if (CountSceneOverlays() != 1)
                {
                    throw new InvalidOperationException("Mascot defeat created duplicate flyer overlays.");
                }

                spawnedOverlay.Advance(spawnedOverlay.TotalDuration, true);
                if (spawnedOverlay.CurrentFrameIndex != 0 ||
                    !Mathf.Approximately(spawnedOverlay.CurrentAlpha, 1f))
                {
                    throw new InvalidOperationException("Mascot flyer timer advanced while the menu was paused.");
                }

                spawnedOverlay.Advance(spawnedOverlay.BuildupDuration);
                if (spawnedOverlay.CurrentFrameIndex != spawnedOverlay.FrameCount - 1 ||
                    !Mathf.Approximately(spawnedOverlay.CurrentAlpha, 1f))
                {
                    throw new InvalidOperationException("Mascot flyer buildup animation is incorrect.");
                }

                spawnedOverlay.Advance(
                    spawnedOverlay.TotalDuration - spawnedOverlay.BuildupDuration -
                    spawnedOverlay.FadeDuration * 0.5f);
                if (!Mathf.Approximately(spawnedOverlay.CurrentAlpha, 0.5f))
                {
                    throw new InvalidOperationException("Mascot flyer fade timing is incorrect.");
                }

                spawnedOverlay.Advance(spawnedOverlay.FadeDuration * 0.5f);
                if (spawnedOverlay != null)
                {
                    throw new InvalidOperationException("Mascot flyer overlay was not removed after five seconds.");
                }
            }
            finally
            {
                if (spawnedOverlay != null)
                {
                    Object.DestroyImmediate(spawnedOverlay.gameObject);
                }

                DestroySceneOverlays();
                Object.DestroyImmediate(instance);
                Object.DestroyImmediate(truckObject);
            }
        }

        private static FlyerScreenOverlay FindSceneOverlay()
        {
            FlyerScreenOverlay[] overlays = Object.FindObjectsByType<FlyerScreenOverlay>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int overlayIndex = 0; overlayIndex < overlays.Length; overlayIndex++)
            {
                if (overlays[overlayIndex].gameObject.scene.IsValid())
                {
                    return overlays[overlayIndex];
                }
            }

            return null;
        }

        private static int CountSceneOverlays()
        {
            FlyerScreenOverlay[] overlays = Object.FindObjectsByType<FlyerScreenOverlay>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            int count = 0;
            for (int overlayIndex = 0; overlayIndex < overlays.Length; overlayIndex++)
            {
                if (overlays[overlayIndex].gameObject.scene.IsValid())
                {
                    count++;
                }
            }

            return count;
        }

        private static void DestroySceneOverlays()
        {
            FlyerScreenOverlay[] overlays = Object.FindObjectsByType<FlyerScreenOverlay>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int overlayIndex = 0; overlayIndex < overlays.Length; overlayIndex++)
            {
                if (overlays[overlayIndex].gameObject.scene.IsValid())
                {
                    Object.DestroyImmediate(overlays[overlayIndex].gameObject);
                }
            }
        }

        private static RectTransform GetOrCreateRect(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                RectTransform existingRect = existing as RectTransform;
                if (existingRect == null)
                {
                    throw new InvalidOperationException($"{name} must use a RectTransform.");
                }

                return existingRect;
            }

            GameObject child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return child.GetComponent<RectTransform>();
        }

        private static void Stretch(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.localScale = Vector3.one;
        }

        private static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }
    }
}
