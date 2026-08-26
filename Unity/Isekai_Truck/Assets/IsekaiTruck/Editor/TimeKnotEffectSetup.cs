using System;
using IsekaiTruck.Monsters;
using IsekaiTruck.Visuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace IsekaiTruck.Editor
{
    public static class TimeKnotEffectSetup
    {
        private const string ScenePath = "Assets/IsekaiTruck/Scenes/Main.unity";
        private const string SpritePath = "Assets/IsekaiTruck/Art/Sprites/TimeKnotStunEffect.png";
        private const string EffectPrefabPath = "Assets/IsekaiTruck/Prefabs/Effects/TimeKnotStunEffect.prefab";
        private const string SpritePrefix = "TimeKnotStunEffect";
        private const int Columns = 3;
        private const int Rows = 3;
        private const int FrameCount = Columns * Rows;
        private const float PixelsPerUnit = 100f;
        private const float PreviewFramesPerSecond = 9f;
        private const float VisualScale = 1.2f;
        private const float EffectHeight = 0.15f;

        [MenuItem("Isekai Truck/Setup Time Knot Effect")]
        public static void Setup()
        {
            ConfigureImporter();
            Sprite[] frames = LoadFrames();
            SpriteSequenceEffect effectPrefab = CreateOrUpdateEffectPrefab(frames);

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            MonsterManager monsterManager = Object.FindFirstObjectByType<MonsterManager>();
            if (monsterManager == null)
            {
                throw new InvalidOperationException("Main scene MonsterManager was not found.");
            }

            monsterManager.SetStunEffect(effectPrefab, EffectHeight);
            EditorUtility.SetDirty(monsterManager);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Verify();

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("Isekai Truck", "시간 매듭 기절 이펙트를 주민에게 연결했습니다.", "확인");
            }
        }

        public static void Verify()
        {
            TextureImporter importer = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
            Sprite[] frames = LoadFrames();
            SpriteSequenceEffect effectPrefab = AssetDatabase.LoadAssetAtPath<SpriteSequenceEffect>(EffectPrefabPath);
            if (importer == null || effectPrefab == null)
            {
                throw new InvalidOperationException("Time Knot effect assets are missing.");
            }

            if (importer.textureType != TextureImporterType.Sprite ||
                importer.spriteImportMode != SpriteImportMode.Multiple ||
                !importer.alphaIsTransparency ||
                !Mathf.Approximately(importer.spritePixelsPerUnit, PixelsPerUnit))
            {
                throw new InvalidOperationException("Time Knot effect importer is not configured as expected.");
            }

            SpriteRenderer spriteRenderer = effectPrefab.GetComponent<SpriteRenderer>();
            BillboardSpriteView billboard = effectPrefab.GetComponent<BillboardSpriteView>();
            if (spriteRenderer == null || billboard == null || effectPrefab.FrameCount != FrameCount ||
                effectPrefab.transform.localScale != Vector3.one * VisualScale ||
                spriteRenderer.sprite != frames[0])
            {
                throw new InvalidOperationException("Time Knot effect prefab is not configured as expected.");
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            MonsterManager monsterManager = Object.FindFirstObjectByType<MonsterManager>();
            if (monsterManager == null)
            {
                throw new InvalidOperationException("Main scene MonsterManager was not found.");
            }

            SerializedObject serializedManager = new SerializedObject(monsterManager);
            if (serializedManager.FindProperty("stunEffectPrefab").objectReferenceValue != effectPrefab ||
                !Mathf.Approximately(serializedManager.FindProperty("stunEffectHeight").floatValue, EffectHeight))
            {
                throw new InvalidOperationException("Time Knot effect is not assigned to MonsterManager.");
            }

            Debug.Log("Time Knot effect setup verification passed.");
        }

        private static void ConfigureImporter()
        {
            TextureImporter importer = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("Time Knot effect sprite sheet was not found.");
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.sRGBTexture = true;
            importer.maxTextureSize = 1024;
            importer.SaveAndReimport();

            importer = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("Time Knot effect sprite sheet importer was not created.");
            }

            importer.spriteImportMode = SpriteImportMode.Multiple;

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(SpritePath);
            if (texture == null)
            {
                throw new InvalidOperationException("Time Knot effect sprite sheet could not be loaded.");
            }

#pragma warning disable CS0618
            importer.spritesheet = BuildMetadata(texture);
#pragma warning restore CS0618
            importer.SaveAndReimport();
        }

        private static SpriteMetaData[] BuildMetadata(Texture2D texture)
        {
            SpriteMetaData[] metadata = new SpriteMetaData[FrameCount];
            for (int frameIndex = 0; frameIndex < metadata.Length; frameIndex++)
            {
                int column = frameIndex % Columns;
                int rowFromTop = frameIndex / Columns;
                int rowFromBottom = Rows - rowFromTop - 1;
                int minX = Mathf.FloorToInt(column * texture.width / (float)Columns);
                int maxX = Mathf.FloorToInt((column + 1) * texture.width / (float)Columns);
                int minY = Mathf.FloorToInt(rowFromBottom * texture.height / (float)Rows);
                int maxY = Mathf.FloorToInt((rowFromBottom + 1) * texture.height / (float)Rows);

                metadata[frameIndex] = new SpriteMetaData
                {
                    name = GetSpriteName(frameIndex),
                    rect = new Rect(minX, minY, maxX - minX, maxY - minY),
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                    border = Vector4.zero
                };
            }

            return metadata;
        }

        private static Sprite[] LoadFrames()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(SpritePath);
            Sprite[] frames = new Sprite[FrameCount];
            for (int frameIndex = 0; frameIndex < frames.Length; frameIndex++)
            {
                string spriteName = GetSpriteName(frameIndex);
                for (int assetIndex = 0; assetIndex < assets.Length; assetIndex++)
                {
                    if (assets[assetIndex] is Sprite sprite && sprite.name == spriteName)
                    {
                        frames[frameIndex] = sprite;
                        break;
                    }
                }

                if (frames[frameIndex] == null)
                {
                    throw new InvalidOperationException($"Time Knot effect frame is missing: {spriteName}");
                }
            }

            return frames;
        }

        private static string GetSpriteName(int frameIndex)
        {
            return $"{SpritePrefix}_{frameIndex}";
        }

        private static SpriteSequenceEffect CreateOrUpdateEffectPrefab(Sprite[] frames)
        {
            GameObject root = new GameObject("TimeKnotStunEffect");
            try
            {
                root.transform.localScale = Vector3.one * VisualScale;
                SpriteRenderer spriteRenderer = root.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = frames[0];
                spriteRenderer.color = Color.white;
                spriteRenderer.spriteSortPoint = SpriteSortPoint.Pivot;
                spriteRenderer.sortingOrder = 3;
                spriteRenderer.shadowCastingMode = ShadowCastingMode.Off;
                spriteRenderer.receiveShadows = false;
                root.AddComponent<BillboardSpriteView>();
                SpriteSequenceEffect sequenceEffect = root.AddComponent<SpriteSequenceEffect>();
                sequenceEffect.Configure(spriteRenderer, frames, PreviewFramesPerSecond, true);

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, EffectPrefabPath);
                SpriteSequenceEffect prefabEffect = prefab != null
                    ? prefab.GetComponent<SpriteSequenceEffect>()
                    : null;
                if (prefabEffect == null)
                {
                    throw new InvalidOperationException("Time Knot effect prefab could not be saved.");
                }

                return prefabEffect;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
