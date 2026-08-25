using System;
using IsekaiTruck.Enemies;
using IsekaiTruck.Visuals;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace IsekaiTruck.Editor
{
    public static class PoliceCarEnemySetup
    {
        private const string SpritePath = "Assets/IsekaiTruck/Art/Sprites/PoliceCar5Direction.png";
        private const string PrefabPath = "Assets/IsekaiTruck/Prefabs/Enemies/BasicEnemy.prefab";
        private const float PixelsPerUnit = 100f;
        private const float PoliceCarSize = 2f;
        private const int FramesPerDirection = 1;

        private static readonly string[] SourceDirectionNames =
        {
            "Down",
            "DownRight",
            "Right",
            "UpRight",
            "Up"
        };

        private static readonly Rect[] SourceRects =
        {
            new Rect(15f, 90f, 101f, 113f),
            new Rect(170f, 92f, 149f, 112f),
            new Rect(332f, 97f, 195f, 94f),
            new Rect(546f, 96f, 148f, 107f),
            new Rect(732f, 94f, 94f, 109f)
        };

        [MenuItem("Isekai Truck/Setup Police Car Enemy")]
        public static void Setup()
        {
            AssetDatabase.Refresh();
            ConfigureSpriteImporter();
            Sprite[] frames = LoadFrames();
            ApplyToPrefab(frames);
            AssetDatabase.SaveAssets();
            Verify();

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("Isekai Truck", "추적 적을 경찰차 스프라이트로 교체했습니다.", "확인");
            }
        }

        public static void Verify()
        {
            TextureImporter importer = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Sprite[] frames = LoadFrames();
            if (importer == null || prefab == null)
            {
                throw new InvalidOperationException("Police car enemy assets are missing.");
            }

            EnemyDefinition definition = prefab.GetComponent<EnemyDefinition>();
            EnemyController controller = prefab.GetComponent<EnemyController>();
            EnemyView enemyView = prefab.GetComponent<EnemyView>();
            Transform visual = prefab.transform.Find("SpriteVisual");
            SpriteRenderer spriteRenderer = visual != null ? visual.GetComponent<SpriteRenderer>() : null;
            BillboardSpriteView billboard = visual != null ? visual.GetComponent<BillboardSpriteView>() : null;
            DirectionalSpriteAnimator directionalAnimator =
                visual != null ? visual.GetComponent<DirectionalSpriteAnimator>() : null;
            MeshRenderer legacyRenderer = prefab.GetComponentInChildren<MeshRenderer>(true);

            if (definition == null || controller == null || enemyView == null || visual == null ||
                spriteRenderer == null || billboard == null || directionalAnimator == null || legacyRenderer == null)
            {
                throw new InvalidOperationException("Police car enemy prefab components are incomplete.");
            }

            if (definition.TypeId != "basic_enemy" || definition.DisplayName != "경찰차" ||
                !Mathf.Approximately(definition.Size, PoliceCarSize) ||
                !Mathf.Approximately(definition.CollisionRadius, 0.5f) ||
                !Mathf.Approximately(definition.MoveSpeed, 6f) || definition.ContactDamage != 1 ||
                !Mathf.Approximately(definition.SpawnWeight, 1f))
            {
                throw new InvalidOperationException("Police car enemy gameplay settings changed unexpectedly.");
            }

            if (importer.textureType != TextureImporterType.Sprite ||
                importer.spriteImportMode != SpriteImportMode.Multiple || !importer.alphaIsTransparency ||
                !Mathf.Approximately(importer.spritePixelsPerUnit, PixelsPerUnit) ||
                legacyRenderer.enabled || spriteRenderer.sprite != frames[0])
            {
                throw new InvalidOperationException("Police car enemy sprite settings are incorrect.");
            }

            VerifyDirectionalFrames(directionalAnimator, frames);
            VerifyViewReference(enemyView, directionalAnimator);
            VerifyDirectionSelection(prefab, frames);
            VerifyPausedMovement(prefab);
            Debug.Log("Police car enemy verification passed.");
        }

        private static void ConfigureSpriteImporter()
        {
            TextureImporter importer = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("Police car sprite sheet was not imported.");
            }

            SpriteMetaData[] metadata = new SpriteMetaData[SourceDirectionNames.Length];
            for (int directionIndex = 0; directionIndex < SourceDirectionNames.Length; directionIndex++)
            {
                metadata[directionIndex] = new SpriteMetaData
                {
                    name = $"PoliceCar_{SourceDirectionNames[directionIndex]}",
                    rect = SourceRects[directionIndex],
                    alignment = (int)SpriteAlignment.Custom,
                    pivot = new Vector2(0.5f, 0f)
                };
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
            importer.maxTextureSize = 1024;
            importer.spritesheet = metadata;
            importer.SaveAndReimport();
        }

        private static Sprite[] LoadFrames()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(SpritePath);
            Sprite[] frames = new Sprite[SourceDirectionNames.Length];
            for (int directionIndex = 0; directionIndex < SourceDirectionNames.Length; directionIndex++)
            {
                string spriteName = $"PoliceCar_{SourceDirectionNames[directionIndex]}";
                for (int assetIndex = 0; assetIndex < assets.Length; assetIndex++)
                {
                    Sprite sprite = assets[assetIndex] as Sprite;
                    if (sprite != null && sprite.name == spriteName)
                    {
                        frames[directionIndex] = sprite;
                        break;
                    }
                }

                if (frames[directionIndex] == null)
                {
                    throw new InvalidOperationException($"Police car directional sprite is missing: {spriteName}");
                }
            }

            return frames;
        }

        private static void ApplyToPrefab(Sprite[] frames)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                root.name = "경찰차";
                EnemyDefinition definition = root.GetComponent<EnemyDefinition>();
                if (definition == null || definition.TypeId != "basic_enemy")
                {
                    throw new InvalidOperationException("Existing enemy prefab identity is invalid.");
                }

                definition.Configure(
                    definition.TypeId,
                    "경찰차",
                    PoliceCarSize,
                    definition.CollisionRadius,
                    definition.MoveSpeed,
                    definition.ContactDamage,
                    definition.SpawnWeight);

                MeshRenderer[] legacyRenderers = root.GetComponentsInChildren<MeshRenderer>(true);
                for (int rendererIndex = 0; rendererIndex < legacyRenderers.Length; rendererIndex++)
                {
                    legacyRenderers[rendererIndex].enabled = false;
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
                visual.localScale = Vector3.one;

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
                directionalAnimator.Configure(spriteRenderer, frames, FramesPerDirection, 0f);

                EnemyView enemyView = GetOrAddComponent<EnemyView>(root);
                enemyView.Configure(directionalAnimator);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void VerifyDirectionalFrames(
            DirectionalSpriteAnimator directionalAnimator,
            Sprite[] expectedFrames)
        {
            SerializedObject serializedAnimator = new SerializedObject(directionalAnimator);
            SerializedProperty framesProperty = serializedAnimator.FindProperty("directionFrames");
            SerializedProperty framesPerDirectionProperty = serializedAnimator.FindProperty("framesPerDirection");
            SerializedProperty framesPerSecondProperty = serializedAnimator.FindProperty("framesPerSecond");
            if (framesProperty == null || framesPerDirectionProperty == null || framesPerSecondProperty == null ||
                framesProperty.arraySize != expectedFrames.Length ||
                framesPerDirectionProperty.intValue != FramesPerDirection ||
                !Mathf.Approximately(framesPerSecondProperty.floatValue, 0f))
            {
                throw new InvalidOperationException("Police car directional sprite settings are incomplete.");
            }

            for (int frameIndex = 0; frameIndex < expectedFrames.Length; frameIndex++)
            {
                if (framesProperty.GetArrayElementAtIndex(frameIndex).objectReferenceValue !=
                    expectedFrames[frameIndex])
                {
                    throw new InvalidOperationException(
                        $"Police car directional sprite reference is incorrect: {frameIndex}");
                }
            }
        }

        private static void VerifyViewReference(
            EnemyView enemyView,
            DirectionalSpriteAnimator directionalSpriteAnimator)
        {
            SerializedObject serializedView = new SerializedObject(enemyView);
            if (serializedView.FindProperty("directionalSpriteAnimator").objectReferenceValue !=
                directionalSpriteAnimator)
            {
                throw new InvalidOperationException("Police car EnemyView reference is incomplete.");
            }
        }

        private static void VerifyDirectionSelection(GameObject prefab, Sprite[] frames)
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            GameObject cameraObject = new GameObject("Police Car Direction Verification Camera", typeof(UnityEngine.Camera));
            try
            {
                UnityEngine.Camera targetCamera = cameraObject.GetComponent<UnityEngine.Camera>();
                targetCamera.transform.rotation = Quaternion.Euler(35f, 0f, 0f);
                DirectionalSpriteAnimator animator = instance.GetComponentInChildren<DirectionalSpriteAnimator>(true);
                SpriteRenderer spriteRenderer = animator.GetComponent<SpriteRenderer>();
                animator.SetTargetCamera(targetCamera);
                animator.Initialize();

                Vector3[] directions =
                {
                    Vector3.back,
                    new Vector3(1f, 0f, -1f),
                    Vector3.right,
                    new Vector3(1f, 0f, 1f),
                    Vector3.forward
                };

                for (int directionIndex = 0; directionIndex < directions.Length; directionIndex++)
                {
                    animator.SetMovement(directions[directionIndex], 1f);
                    if (spriteRenderer.sprite != frames[directionIndex] || spriteRenderer.flipX)
                    {
                        throw new InvalidOperationException(
                            $"Police car direction selection failed: {directionIndex}");
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(instance);
                Object.DestroyImmediate(cameraObject);
            }
        }

        private static void VerifyPausedMovement(GameObject prefab)
        {
            GameObject truck = new GameObject("Police Car Pause Verification Truck");
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            try
            {
                EnemyDefinition definition = instance.GetComponent<EnemyDefinition>();
                EnemyController controller = instance.GetComponent<EnemyController>();
                controller.Initialize(definition.CreateData(), truck.transform);
                instance.transform.position = Vector3.right * 4f;
                Vector3 pausedPosition = instance.transform.position;
                controller.UpdateEnemy(1f, true);
                if (instance.transform.position != pausedPosition)
                {
                    throw new InvalidOperationException("Police car moved while the world was paused.");
                }
            }
            finally
            {
                Object.DestroyImmediate(instance);
                Object.DestroyImmediate(truck);
            }
        }

        private static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }
    }
}
