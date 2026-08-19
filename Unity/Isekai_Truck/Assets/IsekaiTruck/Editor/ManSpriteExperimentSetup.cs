using System;
using IsekaiTruck.Monsters;
using IsekaiTruck.Visuals;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace IsekaiTruck.Editor
{
    public static class ManSpriteExperimentSetup
    {
        private const string SpritePath = "Assets/IsekaiTruck/Art/Sprites/OrdinaryPerson5DirectionWalk.png";
        private const string ManPrefabPath = "Assets/IsekaiTruck/Prefabs/Monsters/Man.prefab";
        private const float PixelsPerUnit = 270f;
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

        [MenuItem("Isekai Truck/Setup Man Sprite Experiment")]
        public static void Setup()
        {
            ConfigureSpriteImporter();

            Sprite[] frames = LoadFrames();
            ApplySpriteToManPrefab(frames);
            AssetDatabase.SaveAssets();
            Verify();

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("Isekai Truck", "평범한 사람의 8방향 걷기 애니메이션을 적용했습니다.", "확인");
            }
        }

        public static void Verify()
        {
            TextureImporter importer = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
            Sprite[] frames = LoadFrames();
            GameObject manPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ManPrefabPath);
            if (importer == null || manPrefab == null)
            {
                throw new InvalidOperationException("Man directional sprite assets are missing.");
            }

            if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Multiple || !importer.alphaIsTransparency || !Mathf.Approximately(importer.spritePixelsPerUnit, PixelsPerUnit))
            {
                throw new InvalidOperationException("Ordinary person walk sheet importer is not configured as expected.");
            }

            Transform visual = manPrefab.transform.Find("SpriteVisual");
            SpriteRenderer spriteRenderer = visual != null ? visual.GetComponent<SpriteRenderer>() : null;
            BillboardSpriteView billboard = visual != null ? visual.GetComponent<BillboardSpriteView>() : null;
            DirectionalSpriteAnimator directionalAnimator = visual != null ? visual.GetComponent<DirectionalSpriteAnimator>() : null;
            MonsterView monsterView = manPrefab.GetComponent<MonsterView>();
            MeshRenderer legacyRenderer = manPrefab.GetComponent<MeshRenderer>();
            if (visual == null || spriteRenderer == null || billboard == null || directionalAnimator == null || monsterView == null || spriteRenderer.sprite != frames[0] || monsterView.VisualRoot != visual)
            {
                throw new InvalidOperationException("Man prefab directional sprite references are incomplete.");
            }

            VerifyDirectionalFrames(directionalAnimator, frames);
            SerializedObject serializedView = new SerializedObject(monsterView);
            if (serializedView.FindProperty("directionalSpriteAnimator").objectReferenceValue != directionalAnimator)
            {
                throw new InvalidOperationException("Man prefab directional sprite animator is not assigned to MonsterView.");
            }

            if (legacyRenderer == null || legacyRenderer.enabled)
            {
                throw new InvalidOperationException("Man prefab legacy mesh renderer was not disabled.");
            }

            if (visual.localPosition != new Vector3(0f, -0.5f, 0f) || visual.localRotation != Quaternion.identity || visual.localScale != Vector3.one * VisualScale)
            {
                throw new InvalidOperationException("Man sprite visual transform is incorrect.");
            }

            VerifyBillboardFacing(manPrefab);
            VerifyDirectionalFacing(manPrefab, frames);
            Debug.Log("Man 5-direction sheet verification passed.");
        }

        private static void ConfigureSpriteImporter()
        {
            TextureImporter importer = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("Ordinary person walk sheet importer was not created.");
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
            importer.maxTextureSize = 4096;
            importer.SaveAndReimport();
        }

        private static void ApplySpriteToManPrefab(Sprite[] frames)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(ManPrefabPath);
            try
            {
                MeshRenderer legacyRenderer = root.GetComponent<MeshRenderer>();
                MonsterView monsterView = root.GetComponent<MonsterView>();
                if (legacyRenderer == null || monsterView == null)
                {
                    throw new InvalidOperationException("Man prefab legacy visual components were not found.");
                }

                legacyRenderer.enabled = false;
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

                PrefabUtility.SaveAsPrefabAsset(root, ManPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
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
            return $"Man_{SourceDirectionNames[directionIndex]}_{frameIndex}";
        }

        private static void VerifyDirectionalFrames(DirectionalSpriteAnimator directionalAnimator, Sprite[] expectedFrames)
        {
            SerializedObject serializedAnimator = new SerializedObject(directionalAnimator);
            SerializedProperty framesProperty = serializedAnimator.FindProperty("directionFrames");
            SerializedProperty framesPerDirectionProperty = serializedAnimator.FindProperty("framesPerDirection");
            SerializedProperty framesPerSecondProperty = serializedAnimator.FindProperty("framesPerSecond");
            if (framesProperty == null || framesPerDirectionProperty == null || framesPerSecondProperty == null || framesProperty.arraySize != expectedFrames.Length)
            {
                throw new InvalidOperationException("Man directional sprite animation settings are incomplete.");
            }

            if (framesPerDirectionProperty.intValue != DirectionalSpriteAnimator.DefaultFramesPerDirection)
            {
                throw new InvalidOperationException("Man directional sprite frame count is incorrect.");
            }

            for (int frameIndex = 0; frameIndex < expectedFrames.Length; frameIndex++)
            {
                if (framesProperty.GetArrayElementAtIndex(frameIndex).objectReferenceValue != expectedFrames[frameIndex])
                {
                    throw new InvalidOperationException($"Man directional sprite frame reference is incorrect: {frameIndex}");
                }
            }

            if (!Mathf.Approximately(framesPerSecondProperty.floatValue, AnimationFramesPerSecond))
            {
                throw new InvalidOperationException("Man directional sprite animation speed is incorrect.");
            }
        }

        private static void VerifyBillboardFacing(GameObject manPrefab)
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(manPrefab) as GameObject;
            GameObject cameraObject = new GameObject("Man Billboard Verification Camera", typeof(UnityEngine.Camera));
            try
            {
                UnityEngine.Camera targetCamera = cameraObject.GetComponent<UnityEngine.Camera>();
                targetCamera.transform.rotation = Quaternion.Euler(32f, 18f, 0f);
                BillboardSpriteView billboard = instance.GetComponentInChildren<BillboardSpriteView>(true);
                billboard.SetTargetCamera(targetCamera);
                billboard.UpdateFacing();

                if (Quaternion.Angle(billboard.transform.rotation, targetCamera.transform.rotation) > 0.01f)
                {
                    throw new InvalidOperationException("Man sprite did not face the camera.");
                }
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(instance);
            }
        }

        private static void VerifyDirectionalFacing(GameObject manPrefab, Sprite[] frames)
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(manPrefab) as GameObject;
            GameObject cameraObject = new GameObject("Man Direction Verification Camera", typeof(UnityEngine.Camera));
            try
            {
                UnityEngine.Camera targetCamera = cameraObject.GetComponent<UnityEngine.Camera>();
                targetCamera.transform.rotation = Quaternion.Euler(32f, 18f, 0f);

                DirectionalSpriteAnimator directionalAnimator = instance.GetComponentInChildren<DirectionalSpriteAnimator>(true);
                SpriteRenderer spriteRenderer = directionalAnimator.GetComponent<SpriteRenderer>();
                directionalAnimator.SetTargetCamera(targetCamera);

                Vector3 screenRight = Vector3.ProjectOnPlane(targetCamera.transform.right, Vector3.up).normalized;
                Vector3 screenUp = Vector3.ProjectOnPlane(targetCamera.transform.up, Vector3.up).normalized;
                Vector3[] directions =
                {
                    -screenUp,
                    (screenRight - screenUp).normalized,
                    screenRight,
                    (screenRight + screenUp).normalized,
                    screenUp,
                    (-screenRight + screenUp).normalized,
                    -screenRight,
                    (-screenRight - screenUp).normalized
                };
                int[] sourceDirectionIndices = { 0, 1, 2, 3, 4, 3, 2, 1 };
                bool[] expectedFlipX = { false, false, false, false, false, true, true, true };

                for (int directionIndex = 0; directionIndex < directions.Length; directionIndex++)
                {
                    directionalAnimator.SetMovement(directions[directionIndex], 1f);
                    Sprite expectedSprite = frames[sourceDirectionIndices[directionIndex] * DirectionalSpriteAnimator.DefaultFramesPerDirection];
                    if (spriteRenderer.sprite != expectedSprite || spriteRenderer.flipX != expectedFlipX[directionIndex])
                    {
                        throw new InvalidOperationException($"Man directional sprite selection is incorrect: {directionIndex}");
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(instance);
            }
        }

        private static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }
    }
}
