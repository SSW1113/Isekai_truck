using IsekaiTruck.Camera;
using IsekaiTruck.Truck;
using UnityEngine;

namespace IsekaiTruck.Diagnostics
{
    [DisallowMultipleComponent]
    public sealed class TruckSpeedDebugUI : MonoBehaviour
    {
        [SerializeField] private TruckController truckController;
        [SerializeField] private CameraController cameraController;
        [SerializeField] private bool isVisible = true;

        private GUIStyle boxStyle;
        private GUIStyle labelStyle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateRuntimeOverlay()
        {
            if (FindFirstObjectByType<TruckSpeedDebugUI>() != null)
            {
                return;
            }

            TruckController truck = FindFirstObjectByType<TruckController>();
            CameraController camera = FindFirstObjectByType<CameraController>();

            if (truck == null || camera == null)
            {
                return;
            }

            GameObject debugObject = new GameObject("Speed Debug UI");
            TruckSpeedDebugUI debugUI = debugObject.AddComponent<TruckSpeedDebugUI>();
            debugUI.SetTargets(truck, camera);
        }

        private void OnGUI()
        {
            if (!isVisible || truckController == null || cameraController == null)
            {
                return;
            }

            CreateStyles();

            Rect viewport = cameraController.ViewportRect;
            float scale = Mathf.Clamp(Screen.height / 900f, 0.75f, 1.5f);
            float width = 210f * scale;
            float height = 100f * scale;
            float margin = 12f * scale;
            float x = viewport.xMin * Screen.width + margin;
            float y = (1f - viewport.yMax) * Screen.height + margin;
            Rect panel = new Rect(x, y, width, height);

            Color previousColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.7f);
            GUI.Box(panel, GUIContent.none, boxStyle);
            GUI.color = previousColor;

            string text =
                $"Speed/frame  {truckController.CurrentSpeed:F4}\n" +
                $"Moved/frame  {truckController.CurrentFrameDistance:F4}\n" +
                $"Moved/sec    {truckController.CurrentSpeedPerSecond:F2}\n" +
                $"Input        {truckController.CurrentInputMagnitude:F3}";

            GUI.Label(
                new Rect(panel.x + margin, panel.y + margin * 0.65f, panel.width - margin * 2f, panel.height - margin),
                text,
                labelStyle
            );
        }

        private void CreateStyles()
        {
            if (boxStyle != null)
            {
                return;
            }

            boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.normal.background = Texture2D.whiteTexture;
            boxStyle.normal.textColor = Color.white;

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(16f * Mathf.Clamp(Screen.height / 900f, 0.75f, 1.5f)),
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }

        public void SetTargets(TruckController truck, CameraController camera)
        {
            truckController = truck;
            cameraController = camera;
        }
    }
}
