using UnityEngine;

namespace IsekaiTruck.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class ResponsivePanelFitter : MonoBehaviour
    {
        [SerializeField] private Vector2 preferredSize = new Vector2(620f, 900f);
        [SerializeField, Min(0f)] private float horizontalMargin = 28f;
        [SerializeField, Min(0f)] private float verticalMargin = 28f;

        private RectTransform panelRect;
        private RectTransform parentRect;
        private Vector2 renderedParentSize = new Vector2(-1f, -1f);

        private void Awake()
        {
            CacheReferences();
            Refresh();
        }

        private void OnEnable()
        {
            Refresh();
        }

        private void Update()
        {
            if (parentRect == null || parentRect.rect.size == renderedParentSize)
            {
                return;
            }

            Refresh();
        }

        public void Configure(Vector2 targetPreferredSize, float targetHorizontalMargin, float targetVerticalMargin)
        {
            preferredSize = targetPreferredSize;
            horizontalMargin = Mathf.Max(0f, targetHorizontalMargin);
            verticalMargin = Mathf.Max(0f, targetVerticalMargin);
            Refresh();
        }

        public void Refresh()
        {
            CacheReferences();
            if (panelRect == null || parentRect == null)
            {
                return;
            }

            Vector2 parentSize = parentRect.rect.size;
            if (parentSize.x <= 0f || parentSize.y <= 0f)
            {
                return;
            }

            float availableWidth = Mathf.Max(1f, parentSize.x - horizontalMargin * 2f);
            float availableHeight = Mathf.Max(1f, parentSize.y - verticalMargin * 2f);
            float scale = Mathf.Min(1f, availableWidth / preferredSize.x, availableHeight / preferredSize.y);
            panelRect.sizeDelta = preferredSize;
            panelRect.localScale = Vector3.one * scale;
            renderedParentSize = parentSize;
        }

        private void CacheReferences()
        {
            if (panelRect == null)
            {
                panelRect = GetComponent<RectTransform>();
            }

            if (parentRect == null)
            {
                parentRect = panelRect.parent as RectTransform;
            }
        }
    }
}
