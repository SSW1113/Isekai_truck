using UnityEngine;
using UnityEngine.UI;

namespace IsekaiTruck.UI
{
    public sealed class ArcGraphic : MaskableGraphic
    {
        [SerializeField, Range(0f, 1f)] private float fillAmount = 1f;
        [SerializeField, Range(1f, 360f)] private float arcDegrees = 210f;
        [SerializeField, Min(1f)] private float thickness = 12f;
        [SerializeField, Range(4, 128)] private int segments = 48;
        [SerializeField] private float startAngle = 195f;

        public float FillAmount
        {
            get => fillAmount;
            set
            {
                float next = Mathf.Clamp01(value);
                if (Mathf.Approximately(fillAmount, next))
                {
                    return;
                }

                fillAmount = next;
                SetVerticesDirty();
            }
        }

        public void Configure(float degrees, float lineThickness, int segmentCount, float angle)
        {
            arcDegrees = Mathf.Clamp(degrees, 1f, 360f);
            thickness = Mathf.Max(1f, lineThickness);
            segments = Mathf.Clamp(segmentCount, 4, 128);
            startAngle = angle;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            if (fillAmount <= 0f)
            {
                return;
            }

            Rect rect = rectTransform.rect;
            float outerRadius = Mathf.Min(rect.width, rect.height) * 0.5f;
            float innerRadius = Mathf.Max(0f, outerRadius - thickness);
            int usedSegments = Mathf.Max(1, Mathf.CeilToInt(segments * fillAmount));
            float usedArc = arcDegrees * fillAmount;

            for (int index = 0; index <= usedSegments; index++)
            {
                float t = index / (float)usedSegments;
                float radians = (startAngle - usedArc * t) * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
                vertexHelper.AddVert(direction * outerRadius, color, Vector2.zero);
                vertexHelper.AddVert(direction * innerRadius, color, Vector2.zero);
            }

            for (int index = 0; index < usedSegments; index++)
            {
                int vertex = index * 2;
                vertexHelper.AddTriangle(vertex, vertex + 2, vertex + 1);
                vertexHelper.AddTriangle(vertex + 2, vertex + 3, vertex + 1);
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            fillAmount = Mathf.Clamp01(fillAmount);
            thickness = Mathf.Max(1f, thickness);
            segments = Mathf.Clamp(segments, 4, 128);
            SetVerticesDirty();
        }
#endif
    }
}
