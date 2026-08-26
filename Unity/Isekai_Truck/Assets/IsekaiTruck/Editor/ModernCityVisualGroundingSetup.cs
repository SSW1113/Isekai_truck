using System;
using IsekaiTruck.Visuals;
using IsekaiTruck.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace IsekaiTruck.Editor
{
    public static class ModernCityVisualGroundingSetup
    {
        private const string ArtRoot = "Assets/IsekaiTruck/Art/World/ModernCity";
        private const string MaterialPath = ArtRoot + "/Materials/ModernCity_ContactShadow.mat";
        private const string TreeTexturePath = ArtRoot + "/ModernCityTree.png";
        private const float TreeGroundHeight = 0.02f;
        private const float BuildingGroundHeight = 0.08f;

        private static readonly string[] TexturePaths =
        {
            TreeTexturePath,
            ArtRoot + "/ModernCityChurch.png",
            ArtRoot + "/ModernCityBuilding.png",
            ArtRoot + "/ModernCitySchool.png",
            ArtRoot + "/ModernCityMart.png"
        };

        private static readonly string[] PrefabPaths =
        {
            "Assets/IsekaiTruck/Prefabs/World/ModernCity/ModernCity_Crossroad.prefab",
            "Assets/IsekaiTruck/Prefabs/World/ModernCity/ModernCity_MartStreet.prefab",
            "Assets/IsekaiTruck/Prefabs/World/ModernCity/ModernCity_Residential.prefab",
            "Assets/IsekaiTruck/Prefabs/World/ModernCity/ModernCity_SchoolZone.prefab",
            "Assets/IsekaiTruck/Prefabs/World/ModernCity/ModernCity_ChurchPark.prefab"
        };

        [MenuItem("Isekai Truck/World/Fix Modern City Grounding And Depth")]
        public static void Setup()
        {
            if (EditorApplication.isPlaying)
            {
                throw new InvalidOperationException("플레이 모드를 종료한 뒤 현대 도시 접지 보정을 실행해주세요.");
            }

            for (int index = 0; index < TexturePaths.Length; index++)
            {
                ConfigureSpriteBottomPivot(TexturePaths[index]);
            }

            Material contactShadowMaterial = GetOrCreateContactShadowMaterial();
            for (int index = 0; index < PrefabPaths.Length; index++)
            {
                ApplyToPrefab(PrefabPaths[index], contactShadowMaterial);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Verify();

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog(
                    "Isekai Truck",
                    "현대 도시 건물과 가로수의 접지 및 깊이 정렬을 보정했습니다.",
                    "확인");
            }
        }

        [MenuItem("Isekai Truck/World/Verify Modern City Grounding And Depth")]
        public static void Verify()
        {
            for (int index = 0; index < PrefabPaths.Length; index++)
            {
                VerifyPrefab(PrefabPaths[index]);
            }

            VerifyDepthOrdering();
            Debug.Log("Modern city grounding and depth verification passed.");
        }

        internal static void ConfigureSpriteBottomPivot(string assetPath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"현대 도시 스프라이트를 찾지 못했습니다: {assetPath}");
            }

            bool wasReadable = importer.isReadable;
            if (!wasReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
            }

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (texture == null)
            {
                throw new InvalidOperationException($"현대 도시 텍스처를 읽지 못했습니다: {assetPath}");
            }

            Color32[] pixels = texture.GetPixels32();
            int minimumOpaqueY = FindMinimumOpaqueY(pixels, texture.width, texture.height);
            float pivotY = (minimumOpaqueY + 0.5f) / texture.height;

            TextureImporterSettings textureSettings = new TextureImporterSettings();
            importer.ReadTextureSettings(textureSettings);
            textureSettings.spriteAlignment = (int)SpriteAlignment.Custom;
            textureSettings.spritePivot = new Vector2(0.5f, pivotY);
            importer.SetTextureSettings(textureSettings);
            importer.isReadable = wasReadable;
            importer.SaveAndReimport();
        }

        internal static Material GetOrCreateContactShadowMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null)
                {
                    shader = Shader.Find("Sprites/Default");
                }

                material = new Material(shader);
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            Color color = new Color(0.08f, 0.07f, 0.09f, 0.17f);
            material.color = color;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
            }

            if (material.HasProperty("_SrcBlend"))
            {
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            }

            if (material.HasProperty("_DstBlend"))
            {
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            }

            if (material.HasProperty("_ZWrite"))
            {
                material.SetFloat("_ZWrite", 0f);
            }

            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            return material;
        }

        internal static void CreateContactShadow(
            Transform groundAnchor,
            SpriteRenderer spriteRenderer,
            Vector3 visualScale,
            Material contactShadowMaterial)
        {
            Transform existingShadow = groundAnchor.Find("Contact Shadow");
            if (existingShadow != null)
            {
                return;
            }

            string spritePath = AssetDatabase.GetAssetPath(spriteRenderer.sprite);
            bool isTree = spritePath == TreeTexturePath;
            float worldWidth = spriteRenderer.sprite.bounds.size.x * Mathf.Abs(visualScale.x);
            float shadowWidth = worldWidth * (isTree ? 0.42f : 0.72f);
            float shadowDepth = worldWidth * (isTree ? 0.14f : 0.2f);

            GameObject shadow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shadow.name = "Contact Shadow";
            shadow.layer = groundAnchor.gameObject.layer;
            shadow.transform.SetParent(groundAnchor, false);
            shadow.transform.SetAsFirstSibling();
            shadow.transform.localPosition = new Vector3(0f, -0.005f, 0f);
            shadow.transform.localScale = new Vector3(shadowWidth * 0.5f, 0.003f, shadowDepth * 0.5f);

            Collider collider = shadow.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            MeshRenderer shadowRenderer = shadow.GetComponent<MeshRenderer>();
            shadowRenderer.sharedMaterial = contactShadowMaterial;
            shadowRenderer.shadowCastingMode = ShadowCastingMode.Off;
            shadowRenderer.receiveShadows = false;
        }

        private static void ApplyToPrefab(string prefabPath, Material contactShadowMaterial)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                BillboardSpriteView[] billboardViews = root.GetComponentsInChildren<BillboardSpriteView>(true);
                for (int index = 0; index < billboardViews.Length; index++)
                {
                    BillboardSpriteView billboardView = billboardViews[index];
                    SpriteRenderer spriteRenderer = billboardView.GetComponent<SpriteRenderer>();
                    if (spriteRenderer == null || spriteRenderer.sprite == null)
                    {
                        continue;
                    }

                    string spritePath = AssetDatabase.GetAssetPath(spriteRenderer.sprite);
                    bool isTree = spritePath == TreeTexturePath;
                    Transform visual = billboardView.transform;
                    Vector3 visualScale = visual.localScale;
                    WorldSpriteDepthOrder depthOrder = visual.parent != null
                        ? visual.parent.GetComponent<WorldSpriteDepthOrder>()
                        : null;

                    if (depthOrder == null)
                    {
                        Transform originalParent = visual.parent;
                        int originalSiblingIndex = visual.GetSiblingIndex();
                        Vector3 originalPosition = visual.localPosition;

                        GameObject anchorObject = new GameObject($"{visual.name} Ground Anchor");
                        anchorObject.layer = visual.gameObject.layer;
                        Transform groundAnchor = anchorObject.transform;
                        groundAnchor.SetParent(originalParent, false);
                        groundAnchor.SetSiblingIndex(originalSiblingIndex);
                        groundAnchor.localPosition = new Vector3(
                            originalPosition.x,
                            isTree ? TreeGroundHeight : BuildingGroundHeight,
                            originalPosition.z);
                        groundAnchor.localRotation = Quaternion.identity;
                        groundAnchor.localScale = Vector3.one;

                        visual.SetParent(groundAnchor, false);
                        visual.localPosition = Vector3.zero;
                        visual.localRotation = Quaternion.identity;
                        visual.localScale = visualScale;

                        depthOrder = anchorObject.AddComponent<WorldSpriteDepthOrder>();
                    }

                    spriteRenderer.sortingOrder = 0;
                    spriteRenderer.spriteSortPoint = SpriteSortPoint.Pivot;
                    depthOrder.Configure(spriteRenderer, 0);
                    CreateContactShadow(depthOrder.transform, spriteRenderer, visualScale, contactShadowMaterial);
                }

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void VerifyPrefab(string prefabPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            BillboardSpriteView[] billboardViews = prefab != null
                ? prefab.GetComponentsInChildren<BillboardSpriteView>(true)
                : Array.Empty<BillboardSpriteView>();
            if (billboardViews.Length == 0)
            {
                throw new InvalidOperationException($"현대 도시 빌보드가 없습니다: {prefabPath}");
            }

            for (int index = 0; index < billboardViews.Length; index++)
            {
                BillboardSpriteView billboardView = billboardViews[index];
                SpriteRenderer spriteRenderer = billboardView.GetComponent<SpriteRenderer>();
                Transform groundAnchor = billboardView.transform.parent;
                if (spriteRenderer == null ||
                    spriteRenderer.sortingOrder != 0 ||
                    groundAnchor == null ||
                    groundAnchor.GetComponent<WorldSpriteDepthOrder>() == null ||
                    groundAnchor.Find("Contact Shadow") == null ||
                    billboardView.transform.localPosition != Vector3.zero)
                {
                    throw new InvalidOperationException($"현대 도시 접지 구성이 올바르지 않습니다: {prefabPath}");
                }
            }

            if (prefab.GetComponentsInChildren<Collider>(true).Length > 0)
            {
                throw new InvalidOperationException($"현대 도시 접촉 그림자에 충돌체가 남아 있습니다: {prefabPath}");
            }
        }

        private static int FindMinimumOpaqueY(Color32[] pixels, int width, int height)
        {
            const byte alphaThreshold = 8;
            for (int y = 0; y < height; y++)
            {
                int rowStart = y * width;
                for (int x = 0; x < width; x++)
                {
                    if (pixels[rowStart + x].a > alphaThreshold)
                    {
                        return y;
                    }
                }
            }

            throw new InvalidOperationException("현대 도시 스프라이트에서 불투명 픽셀을 찾지 못했습니다.");
        }

        private static void VerifyDepthOrdering()
        {
            GameObject cameraObject = new GameObject("Modern City Depth Verification Camera");
            GameObject nearObject = new GameObject("Modern City Near Verification Sprite");
            GameObject farObject = new GameObject("Modern City Far Verification Sprite");

            try
            {
                UnityEngine.Camera targetCamera = cameraObject.AddComponent<UnityEngine.Camera>();
                cameraObject.transform.position = new Vector3(0f, 12f, 6f);
                cameraObject.transform.LookAt(new Vector3(0f, 0f, -5f));

                nearObject.transform.position = Vector3.zero;
                farObject.transform.position = new Vector3(0f, 0f, -10f);
                SpriteRenderer nearRenderer = nearObject.AddComponent<SpriteRenderer>();
                SpriteRenderer farRenderer = farObject.AddComponent<SpriteRenderer>();
                WorldSpriteDepthOrder nearOrder = nearObject.AddComponent<WorldSpriteDepthOrder>();
                WorldSpriteDepthOrder farOrder = farObject.AddComponent<WorldSpriteDepthOrder>();
                nearOrder.Configure(nearRenderer, 0);
                farOrder.Configure(farRenderer, 0);
                nearOrder.Refresh(targetCamera);
                farOrder.Refresh(targetCamera);

                if (nearRenderer.sortingOrder <= farRenderer.sortingOrder)
                {
                    throw new InvalidOperationException("카메라에 가까운 월드 스프라이트의 정렬 순서가 올바르지 않습니다.");
                }
            }
            finally
            {
                Object.DestroyImmediate(farObject);
                Object.DestroyImmediate(nearObject);
                Object.DestroyImmediate(cameraObject);
            }
        }
    }
}
