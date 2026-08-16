using UnityEngine;
using UnityEngine.UI;

namespace IsekaiTruck.UI
{
    [DisallowMultipleComponent]
    public sealed class EnemyWarningIconGraphic : MaskableGraphic
    {
        private static readonly Color32 OutlineColor = new Color32(0x4f, 0x18, 0x0d, 0xff);

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            Rect rect = GetPixelAdjustedRect();
            Vector2 center = rect.center;
            float width = rect.width;
            float height = rect.height;

            Vector2 outerTop = center + new Vector2(0f, height * 0.48f);
            Vector2 outerLeft = center + new Vector2(-width * 0.46f, -height * 0.42f);
            Vector2 outerRight = center + new Vector2(width * 0.46f, -height * 0.42f);
            AddTriangle(vertexHelper, outerTop, outerLeft, outerRight, OutlineColor);

            Vector2 innerTop = center + new Vector2(0f, height * 0.35f);
            Vector2 innerLeft = center + new Vector2(-width * 0.34f, -height * 0.31f);
            Vector2 innerRight = center + new Vector2(width * 0.34f, -height * 0.31f);
            AddTriangle(vertexHelper, innerTop, innerLeft, innerRight, color);

            AddQuad(
                vertexHelper,
                center + new Vector2(-width * 0.055f, -height * 0.02f),
                center + new Vector2(width * 0.055f, height * 0.22f),
                OutlineColor
            );
            AddQuad(
                vertexHelper,
                center + new Vector2(-width * 0.06f, -height * 0.22f),
                center + new Vector2(width * 0.06f, -height * 0.1f),
                OutlineColor
            );
        }

        private static void AddTriangle(VertexHelper vertexHelper, Vector2 top, Vector2 left, Vector2 right, Color32 targetColor)
        {
            int startIndex = vertexHelper.currentVertCount;
            vertexHelper.AddVert(top, targetColor, Vector2.zero);
            vertexHelper.AddVert(left, targetColor, Vector2.zero);
            vertexHelper.AddVert(right, targetColor, Vector2.zero);
            vertexHelper.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
        }

        private static void AddQuad(VertexHelper vertexHelper, Vector2 min, Vector2 max, Color32 targetColor)
        {
            int startIndex = vertexHelper.currentVertCount;
            vertexHelper.AddVert(new Vector2(min.x, min.y), targetColor, Vector2.zero);
            vertexHelper.AddVert(new Vector2(min.x, max.y), targetColor, Vector2.zero);
            vertexHelper.AddVert(new Vector2(max.x, max.y), targetColor, Vector2.zero);
            vertexHelper.AddVert(new Vector2(max.x, min.y), targetColor, Vector2.zero);
            vertexHelper.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
            vertexHelper.AddTriangle(startIndex, startIndex + 2, startIndex + 3);
        }
    }
}
