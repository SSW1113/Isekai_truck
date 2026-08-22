using System;
using System.Collections.Generic;
using IsekaiTruck.UI.Entry;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace IsekaiTruck.Editor
{
    public static class EntrySceneSetup
    {
        private const string ScenePath = "Assets/IsekaiTruck/Scenes/Entry.unity";
        private const string MainScenePath = "Assets/IsekaiTruck/Scenes/Main.unity";
        private const string TruckMaterialPath = "Assets/IsekaiTruck/Materials/EntryTruck.mat";
        private const string RetroFontAssetPath = "Assets/IsekaiTruck/Fonts/RetroHUD.asset";

        [MenuItem("Isekai Truck/Setup Entry Scene")]
        public static void Setup()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Isekai Truck", "플레이 모드를 종료한 뒤 실행해주세요.", "확인");
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                CreateScene();
            }
            else
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            ApplyEntryBackground();
            ConfigureBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Verify();

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("Isekai Truck", "Entry 씬을 생성하고 첫 시작 씬으로 등록했습니다.", "확인");
            }
        }

        [MenuItem("Isekai Truck/Rebuild Entry Scene")]
        public static void Rebuild()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Isekai Truck", "플레이 모드를 종료한 뒤 실행해주세요.", "확인");
                return;
            }

            if (!Application.isBatchMode && !EditorUtility.DisplayDialog("Isekai Truck", "현재 Entry 씬을 새 시작 연출로 다시 구성할까요?", "다시 구성", "취소"))
            {
                return;
            }

            CreateScene();
            ApplyEntryBackground();
            ConfigureBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Verify();

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("Isekai Truck", "Entry 씬의 원근 트럭 연출을 구성했습니다.", "확인");
            }
        }

        public static void Verify()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EntrySceneController controller = Object.FindFirstObjectByType<EntrySceneController>();
            TitleTruckEntrance truckEntrance = Object.FindFirstObjectByType<TitleTruckEntrance>();
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
            UnityEngine.Camera targetCamera = Object.FindFirstObjectByType<UnityEngine.Camera>();
            GameObject truckRoot = GameObject.Find("Truck Presentation Root");
            GameObject startPoint = GameObject.Find("Truck Start Point");
            GameObject endPoint = GameObject.Find("Truck End Point");
            GameObject title = GameObject.Find("Title");
            GameObject guide = GameObject.Find("Input Guide");

            if (!scene.IsValid() || controller == null || truckEntrance == null || canvas == null || eventSystem == null || targetCamera == null || truckRoot == null || startPoint == null || endPoint == null || title == null || guide == null)
            {
                throw new InvalidOperationException("Entry 씬의 필수 오브젝트가 구성되지 않았습니다.");
            }

            if (targetCamera.orthographic)
            {
                throw new InvalidOperationException("Entry 카메라는 Perspective로 설정되어야 합니다.");
            }

            if (!HudColorPalette.Matches(targetCamera.backgroundColor, HudColorPalette.EntryBackground))
            {
                throw new InvalidOperationException("Entry 씬 배경색이 지정된 파스텔 옐로가 아닙니다.");
            }

            TMP_FontAsset retroFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RetroFontAssetPath);
            TMP_Text titleText = title.GetComponent<TMP_Text>();
            TMP_Text guideText = guide.GetComponent<TMP_Text>();
            if (retroFont == null || titleText == null || guideText == null || titleText.font != retroFont || guideText.font != retroFont)
            {
                throw new InvalidOperationException("Entry 씬 텍스트에 RetroHUD TMP 폰트가 적용되지 않았습니다.");
            }

            Vector3 startViewportPosition = targetCamera.WorldToViewportPoint(startPoint.transform.position);
            Vector3 endViewportPosition = targetCamera.WorldToViewportPoint(endPoint.transform.position);
            bool isStartVisible = startViewportPosition.z > 0f && startViewportPosition.x >= 0f && startViewportPosition.x <= 1f && startViewportPosition.y >= 0f && startViewportPosition.y <= 1f;
            bool isEndVisible = endViewportPosition.z > 0f && endViewportPosition.x >= 0f && endViewportPosition.x <= 1f && endViewportPosition.y >= 0f && endViewportPosition.y <= 1f;
            if (isStartVisible || !isEndVisible)
            {
                throw new InvalidOperationException("트럭 시작점은 화면 밖에, 종료점은 화면 안에 있어야 합니다.");
            }

            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.Update();
            string[] requiredProperties = { "truckEntrance", "inputGuideText", "inputSurface" };
            for (int i = 0; i < requiredProperties.Length; i++)
            {
                if (serializedController.FindProperty(requiredProperties[i]).objectReferenceValue == null)
                {
                    throw new InvalidOperationException($"EntrySceneController 참조가 비어 있습니다: {requiredProperties[i]}");
                }
            }

            SerializedObject serializedEntrance = new SerializedObject(truckEntrance);
            serializedEntrance.Update();
            string[] entranceProperties = { "truck", "startPoint", "endPoint", "animationCurve" };
            for (int i = 0; i < entranceProperties.Length; i++)
            {
                SerializedProperty property = serializedEntrance.FindProperty(entranceProperties[i]);
                if (property == null || (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null))
                {
                    throw new InvalidOperationException($"TitleTruckEntrance 설정이 비어 있습니다: {entranceProperties[i]}");
                }
            }

            EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
            if (buildScenes.Length < 2 || buildScenes[0].path != ScenePath || !buildScenes[0].enabled || buildScenes[1].path != MainScenePath || !buildScenes[1].enabled)
            {
                throw new InvalidOperationException("Entry와 Main 씬의 빌드 순서가 올바르지 않습니다.");
            }

            Debug.Log("Entry scene verification passed.");
        }

        private static void CreateScene()
        {
            Material truckMaterial = GetOrCreateTruckMaterial();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            UnityEngine.Camera targetCamera = cameraObject.AddComponent<UnityEngine.Camera>();
            targetCamera.clearFlags = CameraClearFlags.SolidColor;
            targetCamera.backgroundColor = HudColorPalette.EntryBackground;
            targetCamera.orthographic = false;
            targetCamera.fieldOfView = 48f;
            targetCamera.nearClipPlane = 0.3f;
            targetCamera.farClipPlane = 100f;
            cameraObject.AddComponent<AudioListener>();

            GameObject lightObject = new GameObject("Directional Light");
            lightObject.transform.rotation = Quaternion.Euler(35f, -30f, 0f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = Color.white;
            light.intensity = 1.2f;
            light.shadows = LightShadows.None;

            GameObject entranceObject = new GameObject("Truck Entrance");

            GameObject startPointObject = new GameObject("Truck Start Point");
            startPointObject.transform.SetParent(entranceObject.transform, false);
            startPointObject.transform.position = new Vector3(24f, -30f, 45f);

            GameObject endPointObject = new GameObject("Truck End Point");
            endPointObject.transform.SetParent(entranceObject.transform, false);
            endPointObject.transform.position = new Vector3(1.55f, -1.75f, 3.5f);

            Vector3 entranceDirection = endPointObject.transform.position - startPointObject.transform.position;
            startPointObject.transform.rotation = Quaternion.LookRotation(entranceDirection, Vector3.up);
            endPointObject.transform.rotation = startPointObject.transform.rotation;

            GameObject truckRoot = new GameObject("Truck Presentation Root");
            truckRoot.transform.SetParent(entranceObject.transform, false);
            truckRoot.transform.localScale = Vector3.one * (2f / 3f);
            GameObject truckVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            truckVisual.name = "Visual";
            truckVisual.transform.SetParent(truckRoot.transform, false);
            truckVisual.transform.localScale = new Vector3(5.6f, 4.4f, 15.2f);
            Object.DestroyImmediate(truckVisual.GetComponent<BoxCollider>());
            truckVisual.GetComponent<MeshRenderer>().sharedMaterial = truckMaterial;

            TitleTruckEntrance truckEntrance = entranceObject.AddComponent<TitleTruckEntrance>();
            truckEntrance.SetReferences(truckRoot.transform, startPointObject.transform, endPointObject.transform);

            GameObject canvasObject = new GameObject("Entry Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RetroFontAssetPath);
            if (font == null)
            {
                throw new InvalidOperationException("Entry 씬에 사용할 RetroHUD TMP 폰트를 찾지 못했습니다.");
            }

            TMP_Text title = CreateText("Title", canvasObject.transform, font, "ISEKAI TRUCK", 84);
            SetCenteredRect(title.rectTransform, new Vector2(0f, 90f), new Vector2(900f, 150f));

            TMP_Text inputGuide = CreateText("Input Guide", canvasObject.transform, font, "PRESS ENTER OR CLICK ANYWHERE TO START", 27);
            SetCenteredRect(inputGuide.rectTransform, new Vector2(0f, -55f), new Vector2(850f, 80f));

            GameObject inputObject = new GameObject("Input Surface", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            inputObject.transform.SetParent(canvasObject.transform, false);
            RectTransform inputRect = inputObject.GetComponent<RectTransform>();
            Stretch(inputRect);
            Image inputImage = inputObject.GetComponent<Image>();
            inputImage.color = Color.clear;
            Button inputButton = inputObject.GetComponent<Button>();
            inputButton.transition = Selectable.Transition.None;

            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            GameObject controllerObject = new GameObject("Entry Scene Controller");
            EntrySceneController controller = controllerObject.AddComponent<EntrySceneController>();
            controller.SetReferences(truckEntrance, inputGuide, inputButton);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static void ApplyEntryBackground()
        {
            UnityEngine.Camera targetCamera = Object.FindFirstObjectByType<UnityEngine.Camera>();
            if (targetCamera == null)
            {
                throw new InvalidOperationException("Entry 씬의 Main Camera를 찾지 못했습니다.");
            }

            targetCamera.clearFlags = CameraClearFlags.SolidColor;
            targetCamera.backgroundColor = HudColorPalette.EntryBackground;
            EditorUtility.SetDirty(targetCamera);
            EditorSceneManager.MarkSceneDirty(targetCamera.gameObject.scene);
            EditorSceneManager.SaveScene(targetCamera.gameObject.scene, ScenePath);
        }

        private static Material GetOrCreateTruckMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(TruckMaterialPath);
            if (material != null)
            {
                return material;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            material = new Material(shader)
            {
                name = "Entry Truck",
                color = new Color(0.12f, 0.14f, 0.17f, 1f)
            };
            AssetDatabase.CreateAsset(material, TruckMaterialPath);
            return material;
        }

        private static TMP_Text CreateText(string name, Transform parent, TMP_FontAsset font, string value, int fontSize)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            TMP_Text text = textObject.GetComponent<TMP_Text>();
            text.font = font;
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Normal;
            text.alignment = TextAlignmentOptions.Center;
            text.characterSpacing = 1.5f;
            text.enableAutoSizing = false;
            text.color = Color.black;
            text.raycastTarget = false;
            return text;
        }

        private static void SetCenteredRect(RectTransform rectTransform, Vector2 anchoredPosition, Vector2 size)
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;
        }

        private static void Stretch(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private static void ConfigureBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene(MainScenePath, true)
            };

            EditorBuildSettingsScene[] existingScenes = EditorBuildSettings.scenes;
            for (int i = 0; i < existingScenes.Length; i++)
            {
                string path = existingScenes[i].path;
                if (path == ScenePath || path == MainScenePath)
                {
                    continue;
                }

                scenes.Add(existingScenes[i]);
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
