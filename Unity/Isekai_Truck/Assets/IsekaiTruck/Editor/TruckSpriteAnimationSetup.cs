using System;
using IsekaiTruck.Truck;
using IsekaiTruck.Visuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace IsekaiTruck.Editor
{
    public static class TruckSpriteAnimationSetup
    {
        private const string SpritePath = "Assets/IsekaiTruck/Art/Sprites/Truck5DirectionDrive.png";
        private const string ScenePath = "Assets/IsekaiTruck/Scenes/Main.unity";
        private const int FramesPerDirection = 36;
        private const float PixelsPerUnit = 28f;
        private const float VisualScale = 1.5f;
        private const float AnimationFramesPerSecond = 18f;
        private static readonly string[] SourceDirectionNames =
        {
            "Down",
            "DownRight",
            "Right",
            "UpRight",
            "Up"
        };

        [MenuItem("Isekai Truck/Setup Truck Sprite Animation")]
        public static void Setup()
        {
            ConfigureSpriteImporter();

            Sprite[] frames = LoadFrames();
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ApplySpriteToTruck(frames);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Verify();

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("Isekai Truck", "트럭의 8방향 스프라이트 애니메이션을 적용했습니다.", "확인");
            }
        }

        public static void Verify()
        {
            TextureImporter importer = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
            Sprite[] frames = LoadFrames();
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            TruckController truckController = Object.FindFirstObjectByType<TruckController>();
            if (!scene.IsValid() || importer == null || truckController == null)
            {
                throw new InvalidOperationException("Truck directional sprite assets are missing.");
            }

            if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Multiple || !importer.alphaIsTransparency || !Mathf.Approximately(importer.spritePixelsPerUnit, PixelsPerUnit))
            {
                throw new InvalidOperationException("Truck walk sheet importer is not configured as expected.");
            }

            Transform legacyVisual = truckController.transform.Find("Visual");
            MeshRenderer legacyRenderer = legacyVisual != null ? legacyVisual.GetComponent<MeshRenderer>() : null;
            Transform spriteVisual = truckController.transform.Find("SpriteVisual");
            SpriteRenderer spriteRenderer = spriteVisual != null ? spriteVisual.GetComponent<SpriteRenderer>() : null;
            BillboardSpriteView billboard = spriteVisual != null ? spriteVisual.GetComponent<BillboardSpriteView>() : null;
            DirectionalSpriteAnimator directionalAnimator = spriteVisual != null ? spriteVisual.GetComponent<DirectionalSpriteAnimator>() : null;
            TruckSpriteView truckSpriteView = truckController.GetComponent<TruckSpriteView>();
            TruckDamageFlash damageFlash = truckController.GetComponent<TruckDamageFlash>();
            if (legacyRenderer == null || legacyRenderer.enabled || spriteVisual == null || spriteRenderer == null || billboard == null || directionalAnimator == null || truckSpriteView == null || damageFlash == null || spriteRenderer.sprite != frames[0])
            {
                throw new InvalidOperationException("Truck directional sprite scene references are incomplete.");
            }

            if (spriteVisual.localPosition != new Vector3(0f, -0.5f, 0f) || spriteVisual.localRotation != Quaternion.identity || spriteVisual.localScale != Vector3.one * VisualScale)
            {
                throw new InvalidOperationException("Truck sprite visual transform is incorrect.");
            }

            VerifyDirectionalFrames(directionalAnimator, frames);
            VerifyTruckSpriteView(truckSpriteView, truckController, directionalAnimator);
            VerifyBillboardFacing(truckController.gameObject);
            VerifyDirectionalFacing(truckController.gameObject, frames);
            Debug.Log("Truck 5-direction sheet verification passed.");
        }

        private static void ConfigureSpriteImporter()
        {
            TextureImporter importer = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("Truck sprite sheet importer was not created.");
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
            importer.SaveAndReimport();
        }

        private static void ApplySpriteToTruck(Sprite[] frames)
        {
            TruckController truckController = Object.FindFirstObjectByType<TruckController>();
            if (truckController == null)
            {
                throw new InvalidOperationException("TruckController was not found in the main scene.");
            }

            Transform legacyVisual = truckController.transform.Find("Visual");
            MeshRenderer legacyRenderer = legacyVisual != null ? legacyVisual.GetComponent<MeshRenderer>() : null;
            if (legacyRenderer == null)
            {
                throw new InvalidOperationException("Truck legacy visual was not found.");
            }

            legacyRenderer.enabled = false;
            Transform spriteVisual = truckController.transform.Find("SpriteVisual");
            if (spriteVisual == null)
            {
                GameObject visualObject = new GameObject("SpriteVisual");
                visualObject.transform.SetParent(truckController.transform, false);
                spriteVisual = visualObject.transform;
            }

            spriteVisual.localPosition = new Vector3(0f, -0.5f, 0f);
            spriteVisual.localRotation = Quaternion.identity;
            spriteVisual.localScale = Vector3.one * VisualScale;

            SpriteRenderer spriteRenderer = GetOrAddComponent<SpriteRenderer>(spriteVisual.gameObject);
            spriteRenderer.sprite = frames[0];
            spriteRenderer.flipX = false;
            spriteRenderer.color = Color.white;
            spriteRenderer.spriteSortPoint = SpriteSortPoint.Pivot;
            spriteRenderer.shadowCastingMode = ShadowCastingMode.Off;
            spriteRenderer.receiveShadows = false;
            GetOrAddComponent<BillboardSpriteView>(spriteVisual.gameObject);
            DirectionalSpriteAnimator directionalAnimator = GetOrAddComponent<DirectionalSpriteAnimator>(spriteVisual.gameObject);
            directionalAnimator.Configure(spriteRenderer, frames, FramesPerDirection, AnimationFramesPerSecond);

            TruckSpriteView truckSpriteView = GetOrAddComponent<TruckSpriteView>(truckController.gameObject);
            truckSpriteView.Configure(truckController, directionalAnimator);
        }

        private static Sprite[] LoadFrames()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(SpritePath);
            Sprite[] frames = new Sprite[DirectionalSpriteAnimator.SourceDirectionCount * FramesPerDirection];

            for (int directionIndex = 0; directionIndex < DirectionalSpriteAnimator.SourceDirectionCount; directionIndex++)
            {
                for (int frameIndex = 0; frameIndex < FramesPerDirection; frameIndex++)
                {
                    int flattenedIndex = directionIndex * FramesPerDirection + frameIndex;
                    string spriteName = $"Truck_{SourceDirectionNames[directionIndex]}_{frameIndex}";

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
                        throw new InvalidOperationException($"Truck directional sprite frame is missing: {spriteName}");
                    }
                }
            }

            return frames;
        }

        private static void VerifyDirectionalFrames(DirectionalSpriteAnimator directionalAnimator, Sprite[] expectedFrames)
        {
            SerializedObject serializedAnimator = new SerializedObject(directionalAnimator);
            SerializedProperty framesProperty = serializedAnimator.FindProperty("directionFrames");
            SerializedProperty framesPerDirectionProperty = serializedAnimator.FindProperty("framesPerDirection");
            SerializedProperty framesPerSecondProperty = serializedAnimator.FindProperty("framesPerSecond");
            if (framesProperty == null || framesPerDirectionProperty == null || framesPerSecondProperty == null || framesProperty.arraySize != expectedFrames.Length)
            {
                throw new InvalidOperationException("Truck directional sprite animation settings are incomplete.");
            }

            if (framesPerDirectionProperty.intValue != FramesPerDirection || !Mathf.Approximately(framesPerSecondProperty.floatValue, AnimationFramesPerSecond))
            {
                throw new InvalidOperationException("Truck directional sprite timing is incorrect.");
            }

            for (int frameIndex = 0; frameIndex < expectedFrames.Length; frameIndex++)
            {
                if (framesProperty.GetArrayElementAtIndex(frameIndex).objectReferenceValue != expectedFrames[frameIndex])
                {
                    throw new InvalidOperationException($"Truck directional sprite frame reference is incorrect: {frameIndex}");
                }
            }
        }

        private static void VerifyTruckSpriteView(TruckSpriteView truckSpriteView, TruckController truckController, DirectionalSpriteAnimator directionalAnimator)
        {
            SerializedObject serializedView = new SerializedObject(truckSpriteView);
            if (serializedView.FindProperty("truckController").objectReferenceValue != truckController || serializedView.FindProperty("directionalSpriteAnimator").objectReferenceValue != directionalAnimator)
            {
                throw new InvalidOperationException("TruckSpriteView references are incomplete.");
            }
        }

        private static void VerifyBillboardFacing(GameObject truck)
        {
            GameObject cameraObject = new GameObject("Truck Billboard Verification Camera", typeof(UnityEngine.Camera));
            try
            {
                UnityEngine.Camera targetCamera = cameraObject.GetComponent<UnityEngine.Camera>();
                targetCamera.transform.rotation = Quaternion.Euler(32f, 18f, 0f);
                BillboardSpriteView billboard = truck.GetComponentInChildren<BillboardSpriteView>(true);
                billboard.SetTargetCamera(targetCamera);
                billboard.UpdateFacing();

                if (Quaternion.Angle(billboard.transform.rotation, targetCamera.transform.rotation) > 0.01f)
                {
                    throw new InvalidOperationException("Truck sprite did not face the camera.");
                }
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
            }
        }

        private static void VerifyDirectionalFacing(GameObject truck, Sprite[] frames)
        {
            GameObject cameraObject = new GameObject("Truck Direction Verification Camera", typeof(UnityEngine.Camera));
            try
            {
                UnityEngine.Camera targetCamera = cameraObject.GetComponent<UnityEngine.Camera>();
                targetCamera.transform.rotation = Quaternion.Euler(32f, 18f, 0f);

                DirectionalSpriteAnimator directionalAnimator = truck.GetComponentInChildren<DirectionalSpriteAnimator>(true);
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
                    Sprite expectedSprite = frames[sourceDirectionIndices[directionIndex] * FramesPerDirection];
                    if (spriteRenderer.sprite != expectedSprite || spriteRenderer.flipX != expectedFlipX[directionIndex])
                    {
                        throw new InvalidOperationException($"Truck directional sprite selection is incorrect: {directionIndex}");
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
            }
        }

        private static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }
    }
}
