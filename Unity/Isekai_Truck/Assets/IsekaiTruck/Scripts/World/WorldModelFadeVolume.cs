using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace IsekaiTruck.World
{
    [DisallowMultipleComponent]
    public sealed class WorldModelFadeVolume : MonoBehaviour
    {
        private const float MinimumAlpha = 0.05f;
        private const float MinimumFadeDuration = 0.05f;
        private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");
        private static readonly Dictionary<Material, Material> TransparentMaterialCache = new Dictionary<Material, Material>();

        [SerializeField] private Vector3 localCenter = Vector3.up;
        [SerializeField] private Vector3 localSize = Vector3.one * 2f;
        [SerializeField, Range(MinimumAlpha, 1f)] private float fadedAlpha = 0.28f;
        [SerializeField, Min(MinimumFadeDuration)] private float fadeDuration = 0.2f;
        [SerializeField] private Renderer[] fadeRenderers = new Renderer[0];

        private Material[][] originalMaterials;
        private Material[][] transparentMaterials;
        private Color[] originalColors;
        private ShadowCastingMode[] originalShadowModes;
        private MaterialPropertyBlock propertyBlock;
        private float currentAlpha = 1f;
        private bool usesTransparentMaterials;
        private bool isInitialized;

        public Vector3 LocalCenter => localCenter;
        public Vector3 LocalSize => localSize;
        public float FadedAlpha => fadedAlpha;
        public float CurrentAlpha => currentAlpha;
        public int FadeRendererCount => fadeRenderers != null ? fadeRenderers.Length : 0;

        public void UpdateFade(Vector3 truckPosition, Vector3 cameraPosition, float deltaTime)
        {
            EnsureInitialized();
            if (fadeRenderers.Length == 0)
            {
                return;
            }

            Vector3 localTruckPosition = transform.InverseTransformPoint(truckPosition);
            Vector3 localCameraPosition = transform.InverseTransformPoint(cameraPosition);
            Bounds fadeBounds = new Bounds(localCenter, localSize);
            bool shouldFade = fadeBounds.Contains(localTruckPosition) ||
                SegmentIntersectsBounds(localCameraPosition, localTruckPosition, fadeBounds);
            float targetAlpha = shouldFade ? fadedAlpha : 1f;
            float fadeSpeed = (1f - fadedAlpha) / Mathf.Max(fadeDuration, MinimumFadeDuration);
            float nextAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, fadeSpeed * Mathf.Max(0f, deltaTime));
            if (Mathf.Approximately(nextAlpha, currentAlpha))
            {
                return;
            }

            currentAlpha = nextAlpha;
            ApplyCurrentAlpha();
        }

        public void RestoreImmediate()
        {
            RestoreOpaqueMaterials();
        }

        private void OnDisable()
        {
            RestoreImmediate();
        }

        private void EnsureInitialized()
        {
            if (isInitialized)
            {
                return;
            }

            if (fadeRenderers == null || fadeRenderers.Length == 0)
            {
                fadeRenderers = GetComponentsInChildren<Renderer>(true);
            }

            originalMaterials = new Material[fadeRenderers.Length][];
            transparentMaterials = new Material[fadeRenderers.Length][];
            originalColors = new Color[fadeRenderers.Length];
            originalShadowModes = new ShadowCastingMode[fadeRenderers.Length];
            propertyBlock = new MaterialPropertyBlock();

            for (int index = 0; index < fadeRenderers.Length; index++)
            {
                Renderer targetRenderer = fadeRenderers[index];
                if (targetRenderer == null)
                {
                    originalMaterials[index] = new Material[0];
                    originalColors[index] = Color.white;
                    continue;
                }

                originalMaterials[index] = targetRenderer.sharedMaterials;
                originalColors[index] = GetRendererColor(targetRenderer);
                originalShadowModes[index] = targetRenderer.shadowCastingMode;
            }

            isInitialized = true;
        }

        private void ApplyCurrentAlpha()
        {
            if (currentAlpha < 0.999f && !usesTransparentMaterials)
            {
                ApplyTransparentMaterials();
            }

            for (int index = 0; index < fadeRenderers.Length; index++)
            {
                Renderer targetRenderer = fadeRenderers[index];
                if (targetRenderer == null)
                {
                    continue;
                }

                Color color = originalColors[index];
                color.a *= currentAlpha;
                propertyBlock.Clear();
                propertyBlock.SetColor(BaseColorProperty, color);
                propertyBlock.SetColor(ColorProperty, color);
                targetRenderer.SetPropertyBlock(propertyBlock);
            }

            if (currentAlpha >= 0.999f)
            {
                RestoreOpaqueMaterials();
            }
        }

        private void ApplyTransparentMaterials()
        {
            for (int rendererIndex = 0; rendererIndex < fadeRenderers.Length; rendererIndex++)
            {
                Renderer targetRenderer = fadeRenderers[rendererIndex];
                if (targetRenderer == null)
                {
                    continue;
                }

                Material[] sourceMaterials = originalMaterials[rendererIndex];
                Material[] rendererTransparentMaterials = transparentMaterials[rendererIndex];
                if (rendererTransparentMaterials == null)
                {
                    rendererTransparentMaterials = new Material[sourceMaterials.Length];
                    for (int materialIndex = 0; materialIndex < sourceMaterials.Length; materialIndex++)
                    {
                        rendererTransparentMaterials[materialIndex] = GetTransparentMaterial(sourceMaterials[materialIndex]);
                    }

                    transparentMaterials[rendererIndex] = rendererTransparentMaterials;
                }

                targetRenderer.sharedMaterials = rendererTransparentMaterials;
                targetRenderer.shadowCastingMode = ShadowCastingMode.Off;
            }

            usesTransparentMaterials = true;
        }

        private void RestoreOpaqueMaterials()
        {
            if (!isInitialized)
            {
                return;
            }

            for (int index = 0; index < fadeRenderers.Length; index++)
            {
                Renderer targetRenderer = fadeRenderers[index];
                if (targetRenderer == null)
                {
                    continue;
                }

                targetRenderer.sharedMaterials = originalMaterials[index];
                targetRenderer.shadowCastingMode = originalShadowModes[index];
                targetRenderer.SetPropertyBlock(null);
            }

            currentAlpha = 1f;
            usesTransparentMaterials = false;
        }

        private static Material GetTransparentMaterial(Material sourceMaterial)
        {
            if (sourceMaterial == null)
            {
                return null;
            }

            if (TransparentMaterialCache.TryGetValue(sourceMaterial, out Material transparentMaterial))
            {
                return transparentMaterial;
            }

            transparentMaterial = new Material(sourceMaterial)
            {
                name = sourceMaterial.name + " (World Fade)",
                hideFlags = HideFlags.DontSave,
                renderQueue = (int)RenderQueue.Transparent
            };
            transparentMaterial.SetOverrideTag("RenderType", "Transparent");

            if (transparentMaterial.HasProperty("_Surface"))
            {
                transparentMaterial.SetFloat("_Surface", 1f);
                transparentMaterial.SetFloat("_Blend", 0f);
                transparentMaterial.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                transparentMaterial.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                transparentMaterial.SetFloat("_ZWrite", 0f);
                transparentMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                transparentMaterial.DisableKeyword("_SURFACE_TYPE_OPAQUE");
            }
            else if (transparentMaterial.HasProperty("_Mode"))
            {
                transparentMaterial.SetFloat("_Mode", 2f);
                transparentMaterial.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                transparentMaterial.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                transparentMaterial.SetFloat("_ZWrite", 0f);
                transparentMaterial.EnableKeyword("_ALPHABLEND_ON");
                transparentMaterial.DisableKeyword("_ALPHATEST_ON");
                transparentMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            }

            TransparentMaterialCache.Add(sourceMaterial, transparentMaterial);
            return transparentMaterial;
        }

        private static Color GetRendererColor(Renderer targetRenderer)
        {
            Material material = targetRenderer.sharedMaterial;
            if (material == null)
            {
                return Color.white;
            }

            if (material.HasProperty(BaseColorProperty))
            {
                return material.GetColor(BaseColorProperty);
            }

            if (material.HasProperty(ColorProperty))
            {
                return material.GetColor(ColorProperty);
            }

            return Color.white;
        }

        private static bool SegmentIntersectsBounds(Vector3 start, Vector3 end, Bounds bounds)
        {
            Vector3 direction = end - start;
            float distance = direction.magnitude;
            if (distance <= Mathf.Epsilon)
            {
                return bounds.Contains(start);
            }

            Ray ray = new Ray(start, direction / distance);
            return bounds.IntersectRay(ray, out float hitDistance) && hitDistance <= distance;
        }

#if UNITY_EDITOR
        public void Configure(
            Vector3 volumeCenter,
            Vector3 volumeSize,
            float transparentAlpha,
            float transitionDuration,
            Renderer[] targetRenderers)
        {
            localCenter = volumeCenter;
            localSize = new Vector3(
                Mathf.Max(0.1f, Mathf.Abs(volumeSize.x)),
                Mathf.Max(0.1f, Mathf.Abs(volumeSize.y)),
                Mathf.Max(0.1f, Mathf.Abs(volumeSize.z)));
            fadedAlpha = Mathf.Clamp(transparentAlpha, MinimumAlpha, 1f);
            fadeDuration = Mathf.Max(MinimumFadeDuration, transitionDuration);
            fadeRenderers = targetRenderers ?? new Renderer[0];
        }
#endif
    }
}
