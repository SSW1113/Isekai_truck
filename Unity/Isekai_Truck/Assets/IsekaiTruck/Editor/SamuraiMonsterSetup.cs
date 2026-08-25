using System;
using System.Collections.Generic;
using IsekaiTruck.Monsters;
using IsekaiTruck.Visuals;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace IsekaiTruck.Editor
{
    public static class SamuraiMonsterSetup
    {
        private const string SpritePath = "Assets/IsekaiTruck/Art/Sprites/Samurai5DirectionWalk.png";
        private const string SamuraiPrefabPath = "Assets/IsekaiTruck/Prefabs/Monsters/Samurai.prefab";
        private const string CatalogPath = "Assets/IsekaiTruck/Config/MonsterPrefabCatalog.asset";
        private const int GridRows = 3;
        private const int SpritesPerRow = 20;
        private const int FramesPerSourceRow = 4;
        private const int MaximumRowGap = 2;
        private const int MaximumSpriteGap = 8;
        private const int SlicePadding = 4;
        private const byte AlphaThreshold = 8;
        private const float PixelsPerUnit = 112f;
        private const float VisualScale = 1.5f;
        private const float AnimationFramesPerSecond = 12f;
        private static readonly string[] SourceDirectionNames =
        {
            "Down",
            "DownRight",
            "Right",
            "UpRight",
            "Up"
        };

        [MenuItem("Isekai Truck/Setup Samurai Monster")]
        public static void Setup()
        {
            AssetDatabase.Refresh();
            ConfigureSpriteImporter();

            Sprite[] frames = LoadFrames();
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
                EditorUtility.DisplayDialog("Isekai Truck", "사무라이 주민과 돌진 애니메이션을 추가했습니다.", "확인");
            }
        }

        public static void Verify()
        {
            TextureImporter importer = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
            Sprite[] frames = LoadFrames();
            GameObject samuraiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SamuraiPrefabPath);
            MonsterPrefabCatalog catalog = AssetDatabase.LoadAssetAtPath<MonsterPrefabCatalog>(CatalogPath);
            if (importer == null || samuraiPrefab == null || catalog == null)
            {
                throw new InvalidOperationException("Samurai monster assets are missing.");
            }

            if (importer.textureType != TextureImporterType.Sprite ||
                importer.spriteImportMode != SpriteImportMode.Multiple ||
                !importer.alphaIsTransparency ||
                !Mathf.Approximately(importer.spritePixelsPerUnit, PixelsPerUnit))
            {
                throw new InvalidOperationException("Samurai walk sheet importer is not configured as expected.");
            }

            MonsterDefinition definition = samuraiPrefab.GetComponent<MonsterDefinition>();
            MonsterController controller = samuraiPrefab.GetComponent<MonsterController>();
            MonsterView monsterView = samuraiPrefab.GetComponent<MonsterView>();
            MonsterChargeBehavior chargeBehavior = samuraiPrefab.GetComponent<MonsterChargeBehavior>();
            Transform visual = samuraiPrefab.transform.Find("SpriteVisual");
            SpriteRenderer spriteRenderer = visual != null ? visual.GetComponent<SpriteRenderer>() : null;
            DirectionalSpriteAnimator directionalAnimator = visual != null ? visual.GetComponent<DirectionalSpriteAnimator>() : null;
            MeshRenderer legacyRenderer = samuraiPrefab.GetComponent<MeshRenderer>();
            if (definition == null || controller == null || monsterView == null || chargeBehavior == null ||
                visual == null || spriteRenderer == null || directionalAnimator == null || legacyRenderer == null)
            {
                throw new InvalidOperationException("Samurai prefab components are incomplete.");
            }

            if (definition.TypeId != "samurai" || definition.DisplayName != "사무라이")
            {
                throw new InvalidOperationException("Samurai prefab identity is incorrect.");
            }

            if (!Mathf.Approximately(definition.Size, MonsterDefinition.DefaultSize) ||
                !Mathf.Approximately(definition.Speed, MonsterDefinition.DefaultSpeed) ||
                !Mathf.Approximately(definition.FleeDistance, MonsterDefinition.DefaultFleeDistance) ||
                !Mathf.Approximately(definition.SpawnWeight, MonsterDefinition.DefaultSpawnWeight))
            {
                throw new InvalidOperationException("Samurai common stats are incorrect.");
            }

            if (legacyRenderer.enabled || spriteRenderer.sprite != frames[0] || monsterView.VisualRoot != visual ||
                visual.localPosition != new Vector3(0f, -0.5f, 0f) || visual.localRotation != Quaternion.identity ||
                visual.localScale != Vector3.one * VisualScale)
            {
                throw new InvalidOperationException("Samurai sprite visual is not configured as expected.");
            }

            VerifyDirectionalFrames(directionalAnimator, frames);
            VerifyCatalog(catalog, controller);
            VerifyChargeMovement(samuraiPrefab);
            VerifyChargeFrameRates(samuraiPrefab);
            Debug.Log("Samurai monster setup verification passed.");
        }

        private static void ConfigureSpriteImporter()
        {
            TextureImporter importer = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("Samurai walk sheet importer was not created.");
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.sRGBTexture = true;
            importer.maxTextureSize = 2048;
            importer.isReadable = true;
            importer.SaveAndReimport();

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(SpritePath);
            if (texture == null)
            {
                throw new InvalidOperationException("Samurai walk sheet texture could not be loaded.");
            }

            // Keep the current importer path without adding the optional 2D Sprite package.
#pragma warning disable CS0618
            importer.spritesheet = BuildSpriteMetadata(texture);
#pragma warning restore CS0618
            importer.isReadable = false;
            importer.SaveAndReimport();
        }

        private static SpriteMetaData[] BuildSpriteMetadata(Texture2D texture)
        {
            Color32[] pixels = texture.GetPixels32();
            SpriteMetaData[] metadata = new SpriteMetaData[DirectionalSpriteAnimator.ExpectedFrameCount];
            List<Vector2Int> sourceRows = FindOccupiedRows(pixels, texture.width, texture.height);
            if (sourceRows.Count != GridRows)
            {
                throw new InvalidOperationException($"Expected {GridRows} sprite rows, but found {sourceRows.Count}.");
            }

            List<Vector2Int>[] sourceColumns = new List<Vector2Int>[GridRows];
            for (int rowIndex = 0; rowIndex < GridRows; rowIndex++)
            {
                sourceColumns[rowIndex] = FindOccupiedColumns(
                    pixels,
                    texture.width,
                    sourceRows[rowIndex]
                );
                if (sourceColumns[rowIndex].Count != SpritesPerRow)
                {
                    throw new InvalidOperationException(
                        $"Expected {SpritesPerRow} sprites in row {rowIndex}, but found {sourceColumns[rowIndex].Count}."
                    );
                }
            }

            for (int directionIndex = 0; directionIndex < DirectionalSpriteAnimator.SourceDirectionCount; directionIndex++)
            {
                for (int frameIndex = 0; frameIndex < DirectionalSpriteAnimator.DefaultFramesPerDirection; frameIndex++)
                {
                    int rowFromTop = frameIndex / FramesPerSourceRow;
                    int sourceRowIndex = GridRows - rowFromTop - 1;
                    int groupColumn = frameIndex % FramesPerSourceRow;
                    int sourceColumn = groupColumn * DirectionalSpriteAnimator.SourceDirectionCount + directionIndex;
                    Vector2Int rowBounds = sourceRows[sourceRowIndex];
                    Vector2Int columnBounds = sourceColumns[sourceRowIndex][sourceColumn];
                    RectInt spriteRect = ExpandBounds(rowBounds, columnBounds, texture.width, texture.height);
                    int flattenedIndex = directionIndex * DirectionalSpriteAnimator.DefaultFramesPerDirection + frameIndex;
                    float pivotY = Mathf.Min(SlicePadding, rowBounds.x) / (float)spriteRect.height;

                    metadata[flattenedIndex] = new SpriteMetaData
                    {
                        name = GetSpriteName(directionIndex, frameIndex),
                        rect = new Rect(spriteRect.x, spriteRect.y, spriteRect.width, spriteRect.height),
                        alignment = (int)SpriteAlignment.Custom,
                        pivot = new Vector2(0.5f, pivotY),
                        border = Vector4.zero
                    };
                }
            }

            return metadata;
        }

        private static List<Vector2Int> FindOccupiedRows(Color32[] pixels, int textureWidth, int textureHeight)
        {
            bool[] occupiedRows = new bool[textureHeight];
            for (int y = 0; y < textureHeight; y++)
            {
                int rowOffset = y * textureWidth;
                for (int x = 0; x < textureWidth; x++)
                {
                    if (pixels[rowOffset + x].a <= AlphaThreshold)
                    {
                        continue;
                    }

                    occupiedRows[y] = true;
                    break;
                }
            }

            return FindOccupiedRuns(occupiedRows, MaximumRowGap);
        }

        private static List<Vector2Int> FindOccupiedColumns(Color32[] pixels, int textureWidth, Vector2Int rowBounds)
        {
            bool[] occupiedColumns = new bool[textureWidth];

            for (int y = rowBounds.x; y <= rowBounds.y; y++)
            {
                int rowOffset = y * textureWidth;
                for (int x = 0; x < textureWidth; x++)
                {
                    if (pixels[rowOffset + x].a > AlphaThreshold)
                    {
                        occupiedColumns[x] = true;
                    }
                }
            }

            return FindOccupiedRuns(occupiedColumns, MaximumSpriteGap);
        }

        private static List<Vector2Int> FindOccupiedRuns(bool[] occupied, int maximumGap)
        {
            List<Vector2Int> runs = new List<Vector2Int>();
            int runStart = -1;
            int lastOccupied = -1;

            for (int index = 0; index < occupied.Length; index++)
            {
                if (!occupied[index])
                {
                    continue;
                }

                if (runStart < 0)
                {
                    runStart = index;
                    lastOccupied = index;
                    continue;
                }

                if (index - lastOccupied - 1 <= maximumGap)
                {
                    lastOccupied = index;
                    continue;
                }

                runs.Add(new Vector2Int(runStart, lastOccupied));
                runStart = index;
                lastOccupied = index;
            }

            if (runStart >= 0)
            {
                runs.Add(new Vector2Int(runStart, lastOccupied));
            }

            return runs;
        }

        private static RectInt ExpandBounds(Vector2Int rowBounds, Vector2Int columnBounds, int textureWidth, int textureHeight)
        {
            int paddedMinX = Mathf.Max(0, columnBounds.x - SlicePadding);
            int paddedMinY = Mathf.Max(0, rowBounds.x - SlicePadding);
            int paddedMaxX = Mathf.Min(textureWidth - 1, columnBounds.y + SlicePadding);
            int paddedMaxY = Mathf.Min(textureHeight - 1, rowBounds.y + SlicePadding);
            return new RectInt(
                paddedMinX,
                paddedMinY,
                paddedMaxX - paddedMinX + 1,
                paddedMaxY - paddedMinY + 1
            );
        }

        private static void CreateOrUpdatePrefab(Sprite[] frames)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SamuraiPrefabPath);
            bool isNewPrefab = prefab == null;
            GameObject root = isNewPrefab
                ? GameObject.CreatePrimitive(PrimitiveType.Sphere)
                : PrefabUtility.LoadPrefabContents(SamuraiPrefabPath);

            try
            {
                root.name = "Samurai";
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

                if (definition.TypeId != "samurai")
                {
                    throw new InvalidOperationException("Existing Samurai prefab uses a different Type ID.");
                }

                GetOrAddComponent<MonsterController>(root);
                MonsterChargeBehavior chargeBehavior = GetOrAddComponent<MonsterChargeBehavior>(root);
                if (isNewPrefab)
                {
                    SerializedObject serializedCharge = new SerializedObject(chargeBehavior);
                    serializedCharge.FindProperty("chargeSpeedMultiplier").floatValue = 2f;
                    serializedCharge.ApplyModifiedPropertiesWithoutUndo();
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

                PrefabUtility.SaveAsPrefabAsset(root, SamuraiPrefabPath);
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
                "samurai",
                "사무라이",
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

        private static Sprite[] LoadFrames()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(SpritePath);
            Sprite[] frames = new Sprite[DirectionalSpriteAnimator.ExpectedFrameCount];

            for (int directionIndex = 0; directionIndex < DirectionalSpriteAnimator.SourceDirectionCount; directionIndex++)
            {
                for (int frameIndex = 0; frameIndex < DirectionalSpriteAnimator.DefaultFramesPerDirection; frameIndex++)
                {
                    int flattenedIndex = directionIndex * DirectionalSpriteAnimator.DefaultFramesPerDirection + frameIndex;
                    string spriteName = GetSpriteName(directionIndex, frameIndex);

                    for (int assetIndex = 0; assetIndex < assets.Length; assetIndex++)
                    {
                        Sprite sprite = assets[assetIndex] as Sprite;
                        if (sprite != null && sprite.name == spriteName)
                        {
                            frames[flattenedIndex] = sprite;
                            break;
                        }
                    }

                    if (frames[flattenedIndex] == null)
                    {
                        throw new InvalidOperationException($"Directional sprite frame is missing: {spriteName}");
                    }
                }
            }

            return frames;
        }

        private static string GetSpriteName(int directionIndex, int frameIndex)
        {
            return $"Samurai_{SourceDirectionNames[directionIndex]}_{frameIndex}";
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
                throw new InvalidOperationException("Samurai directional animation settings are incomplete.");
            }

            for (int frameIndex = 0; frameIndex < expectedFrames.Length; frameIndex++)
            {
                if (framesProperty.GetArrayElementAtIndex(frameIndex).objectReferenceValue != expectedFrames[frameIndex])
                {
                    throw new InvalidOperationException($"Samurai directional frame reference is incorrect: {frameIndex}");
                }
            }
        }

        private static void VerifyCatalog(MonsterPrefabCatalog catalog, MonsterController samuraiController)
        {
            for (int i = 0; i < catalog.MonsterPrefabs.Count; i++)
            {
                if (catalog.MonsterPrefabs[i] == samuraiController)
                {
                    return;
                }
            }

            throw new InvalidOperationException("Samurai prefab is not registered in the monster catalog.");
        }

        private static void VerifyChargeMovement(GameObject samuraiPrefab)
        {
            GameObject truckObject = new GameObject("Samurai Charge Verification Truck");
            GameObject instance = PrefabUtility.InstantiatePrefab(samuraiPrefab) as GameObject;
            try
            {
                instance.transform.position = new Vector3(5f, 0f, 0f);
                MonsterDefinition definition = instance.GetComponent<MonsterDefinition>();
                MonsterController controller = instance.GetComponent<MonsterController>();
                controller.Initialize(definition.CreateData(), truckObject.transform, 0f, 60f);

                controller.UpdateMonster(100f, 0f, 3.096f, 1f, 1f / 60f, 0f, 1f, false);
                if (instance.transform.position.x >= 5f)
                {
                    throw new InvalidOperationException("Samurai did not charge toward the truck.");
                }

                Vector3 pausedPosition = instance.transform.position;
                controller.UpdateMonster(200f, 0f, 3.096f, 1f, 1f / 60f, 0f, 1f, true);
                if (instance.transform.position != pausedPosition)
                {
                    throw new InvalidOperationException("Samurai moved while the world was paused.");
                }
            }
            finally
            {
                Object.DestroyImmediate(instance);
                Object.DestroyImmediate(truckObject);
            }
        }

        private static void VerifyChargeFrameRates(GameObject samuraiPrefab)
        {
            float distanceAt30Fps = MeasureChargeDistance(samuraiPrefab, 30);
            float distanceAt60Fps = MeasureChargeDistance(samuraiPrefab, 60);
            float distanceAt120Fps = MeasureChargeDistance(samuraiPrefab, 120);
            if (!Mathf.Approximately(distanceAt30Fps, distanceAt60Fps) ||
                !Mathf.Approximately(distanceAt60Fps, distanceAt120Fps))
            {
                throw new InvalidOperationException(
                    $"Samurai charge distance changed by frame rate: 30={distanceAt30Fps}, 60={distanceAt60Fps}, 120={distanceAt120Fps}"
                );
            }
        }

        private static float MeasureChargeDistance(GameObject samuraiPrefab, int frameRate)
        {
            GameObject truckObject = new GameObject($"Samurai {frameRate} FPS Verification Truck");
            GameObject instance = PrefabUtility.InstantiatePrefab(samuraiPrefab) as GameObject;
            try
            {
                const float startX = 20f;
                const float simulationDuration = 1f;
                float deltaTime = 1f / frameRate;
                float frameScale = deltaTime * 60f;
                int frameCount = Mathf.RoundToInt(simulationDuration * frameRate);
                instance.transform.position = new Vector3(startX, 0f, 0f);

                MonsterDefinition definition = instance.GetComponent<MonsterDefinition>();
                MonsterController controller = instance.GetComponent<MonsterController>();
                controller.Initialize(definition.CreateData(), truckObject.transform, 0f, 60f);

                for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
                {
                    float nowMilliseconds = (frameIndex + 1) * deltaTime * 1000f;
                    controller.UpdateMonster(nowMilliseconds, 0f, 3.096f, frameScale, deltaTime, 0f, 1f, false);
                }

                return startX - instance.transform.position.x;
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
