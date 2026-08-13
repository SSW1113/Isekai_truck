using System;
using IsekaiTruck.Camera;
using IsekaiTruck.Config;
using IsekaiTruck.Core;
using IsekaiTruck.Input;
using IsekaiTruck.Truck;
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
    public static class SecondStageSetup
    {
        private const string ConfigPath = "Assets/IsekaiTruck/Config/GameConfig.asset";
        private const string ScenePath = "Assets/IsekaiTruck/Scenes/Main.unity";

        [MenuItem("Isekai Truck/Setup Truck Movement Stage")]
        public static void Setup()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Isekai Truck", "플레이 모드를 종료한 뒤 실행해주세요.", "확인");
                return;
            }

            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            if (config == null)
            {
                throw new InvalidOperationException($"GameConfig를 불러오지 못했습니다: {ConfigPath}");
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
            GameObject truckObject = GameObject.Find("Truck");

            if (gameManager == null || truckObject == null)
            {
                throw new InvalidOperationException("Main 씬의 GameManager 또는 Truck을 찾지 못했습니다.");
            }

            TruckController truckController = truckObject.GetComponent<TruckController>();
            if (truckController == null)
            {
                truckController = truckObject.AddComponent<TruckController>();
            }

            JoystickInput joystickInput = GetOrCreateJoystick();
            GetOrCreateEventSystem();

            gameManager.SetTruckSystems(joystickInput, truckController);
            EditorUtility.SetDirty(gameManager);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();

            ValidateSceneReferences();

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog(
                    "Isekai Truck",
                    "가상 조이스틱과 트럭 이동 시스템을 Main 씬에 연결했습니다.",
                    "확인"
                );
            }
        }

        public static void Verify()
        {
            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            if (config == null)
            {
                throw new InvalidOperationException("GameConfig 검증에 실패했습니다.");
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ValidateSceneReferences();

            GameObject testObject = new GameObject("Truck Movement Verification");
            testObject.transform.position = new Vector3(0f, 0.5f, 0f);
            TruckController controller = testObject.AddComponent<TruckController>();
            controller.Initialize(config);
            float referenceDeltaTime = 1f / config.ReferenceFrameRate;

            float firstFrameSpeed = Mathf.Min(config.Truck.Acceleration, config.Truck.BaseMaxSpeed);
            controller.UpdateTruck(new Vector2(0f, 1f), referenceDeltaTime);
            AssertApproximately(testObject.transform.position.z, firstFrameSpeed, "첫 프레임 가속");
            AssertApproximately(controller.CurrentSpeed, firstFrameSpeed, "첫 프레임 표시 속도");
            AssertApproximately(controller.CurrentFrameDistance, firstFrameSpeed, "첫 프레임 실제 이동거리");
            AssertApproximately(controller.CurrentInputMagnitude, 1f, "첫 프레임 입력 크기");

            float frictionSpeed = firstFrameSpeed * config.Truck.Friction;
            float expectedStoppedSpeed = frictionSpeed < 0.001f ? 0f : frictionSpeed;
            controller.UpdateTruck(Vector2.zero, referenceDeltaTime);
            AssertApproximately(testObject.transform.position.z, firstFrameSpeed + frictionSpeed, "조이스틱 해제 관성");
            AssertApproximately(controller.CurrentSpeed, expectedStoppedSpeed, "정지 임계값 적용 후 표시 속도");
            AssertApproximately(controller.CurrentFrameDistance, frictionSpeed, "관성 실제 이동거리");
            AssertApproximately(controller.CurrentInputMagnitude, 0f, "조이스틱 해제 입력 크기");

            testObject.transform.position = new Vector3(0f, 0.5f, 0f);
            testObject.transform.rotation = Quaternion.identity;
            controller = ResetController(testObject, config);
            controller.UpdateTruck(new Vector2(1f, 0f), referenceDeltaTime);
            AssertApproximately(Mathf.DeltaAngle(0f, testObject.transform.eulerAngles.y), -90f * config.Truck.TurnSpeed, "오른쪽 회전 보간");

            testObject.transform.position = new Vector3(0f, 0.5f, 0f);
            testObject.transform.rotation = Quaternion.identity;
            controller = ResetController(testObject, config);
            controller.UpdateTruck(new Vector2(-1f, 0f), referenceDeltaTime);
            AssertApproximately(Mathf.DeltaAngle(0f, testObject.transform.eulerAngles.y), 90f * config.Truck.TurnSpeed, "왼쪽 회전 보간");

            controller.UpgradeSpeed();
            AssertApproximately(controller.GetStats().MaxSpeed, config.Truck.BaseMaxSpeed + config.Truck.SpeedPerUpgrade, "속도 업그레이드");

            controller.UpgradeSize();
            float expectedScale = 1f + config.Truck.SizePerUpgrade;
            AssertApproximately(testObject.transform.localScale.x, expectedScale, "크기 업그레이드");
            AssertApproximately(testObject.transform.position.y, 0.5f * expectedScale, "크기 업그레이드 높이");

            Rect landscapeViewport = CameraController.CalculateViewportRect(16f / 9f, 10f / 16f);
            AssertApproximately(landscapeViewport.width, 0.3515625f, "16:9 화면의 10:16 뷰포트 너비");
            AssertApproximately(landscapeViewport.x, 0.32421875f, "10:16 뷰포트 중앙 정렬");

            Object.DestroyImmediate(testObject);
            Debug.Log("Truck movement stage verification passed.");
        }

        private static TruckController ResetController(GameObject testObject, GameConfig config)
        {
            Object.DestroyImmediate(testObject.GetComponent<TruckController>());
            TruckController controller = testObject.AddComponent<TruckController>();
            controller.Initialize(config);
            return controller;
        }

        private static JoystickInput GetOrCreateJoystick()
        {
            JoystickInput existingInput = Object.FindFirstObjectByType<JoystickInput>();
            if (existingInput != null)
            {
                return existingInput;
            }

            GameObject canvasObject = new GameObject("Game Canvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();

            GameObject inputSurfaceObject = CreateUiObject("Input Surface", canvasObject.transform);
            RectTransform inputSurface = inputSurfaceObject.GetComponent<RectTransform>();
            inputSurface.anchorMin = Vector2.zero;
            inputSurface.anchorMax = Vector2.one;
            inputSurface.offsetMin = Vector2.zero;
            inputSurface.offsetMax = Vector2.zero;

            Image inputSurfaceImage = inputSurfaceObject.AddComponent<Image>();
            inputSurfaceImage.color = Color.clear;
            inputSurfaceImage.raycastTarget = true;

            GameObject joystickBaseObject = CreateUiObject("Joystick", canvasObject.transform);
            RectTransform joystickBase = joystickBaseObject.GetComponent<RectTransform>();
            joystickBase.anchorMin = new Vector2(0.5f, 0.5f);
            joystickBase.anchorMax = new Vector2(0.5f, 0.5f);
            joystickBase.pivot = new Vector2(0.5f, 0.5f);
            joystickBase.sizeDelta = new Vector2(120f, 120f);

            Image joystickImage = joystickBaseObject.AddComponent<Image>();
            joystickImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            joystickImage.color = new Color(1f, 1f, 1f, 0.15f);
            joystickImage.raycastTarget = false;

            Outline outline = joystickBaseObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.35f);
            outline.effectDistance = new Vector2(2f, -2f);

            GameObject stickObject = CreateUiObject("Stick", joystickBaseObject.transform);
            RectTransform stick = stickObject.GetComponent<RectTransform>();
            stick.anchorMin = new Vector2(0.5f, 0.5f);
            stick.anchorMax = new Vector2(0.5f, 0.5f);
            stick.pivot = new Vector2(0.5f, 0.5f);
            stick.sizeDelta = new Vector2(50f, 50f);
            stick.anchoredPosition = Vector2.zero;

            Image stickImage = stickObject.AddComponent<Image>();
            stickImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            stickImage.color = new Color(1f, 1f, 1f, 0.85f);
            stickImage.raycastTarget = false;

            JoystickInput joystickInput = inputSurfaceObject.AddComponent<JoystickInput>();
            SerializedObject inputSerializedObject = new SerializedObject(joystickInput);
            inputSerializedObject.FindProperty("joystickBase").objectReferenceValue = joystickBase;
            inputSerializedObject.FindProperty("stick").objectReferenceValue = stick;
            inputSerializedObject.ApplyModifiedPropertiesWithoutUndo();

            joystickBaseObject.SetActive(false);
            return joystickInput;
        }

        private static void GetOrCreateEventSystem()
        {
            EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject eventSystemObject = new GameObject("EventSystem");
                eventSystem = eventSystemObject.AddComponent<EventSystem>();
            }

            InputSystemUIInputModule inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (inputModule == null)
            {
                inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
                inputModule.AssignDefaultActions();
            }
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static void ValidateSceneReferences()
        {
            GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
            JoystickInput joystickInput = Object.FindFirstObjectByType<JoystickInput>();
            TruckController truckController = Object.FindFirstObjectByType<TruckController>();

            if (gameManager == null || joystickInput == null || truckController == null)
            {
                throw new InvalidOperationException("트럭 이동 시스템 씬 연결을 확인하지 못했습니다.");
            }

            SerializedObject gameManagerSerializedObject = new SerializedObject(gameManager);
            gameManagerSerializedObject.Update();

            if (gameManagerSerializedObject.FindProperty("joystickInput").objectReferenceValue == null ||
                gameManagerSerializedObject.FindProperty("truckController").objectReferenceValue == null)
            {
                throw new InvalidOperationException("GameManager의 트럭 이동 참조가 비어 있습니다.");
            }
        }

        private static void AssertApproximately(float actual, float expected, string label)
        {
            if (Mathf.Abs(actual - expected) <= 0.0001f)
            {
                return;
            }

            throw new InvalidOperationException($"{label} 검증 실패: expected {expected}, actual {actual}");
        }
    }
}
