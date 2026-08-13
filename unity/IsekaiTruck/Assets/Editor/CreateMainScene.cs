using System.IO;
using IsekaiTruck.Core;
using IsekaiTruck.Gameplay;
using IsekaiTruck.Items;
using IsekaiTruck.Monsters;
using IsekaiTruck.Player;
using IsekaiTruck.Spawning;
using IsekaiTruck.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace IsekaiTruck.Editor
{
    public static class CreateMainScene
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";
        private const string MaterialsPath = "Assets/Materials";
        private const string PrefabsPath = "Assets/Prefabs";
        private const string MonsterPrefabPath = PrefabsPath + "/Monster.prefab";
        private const string FuelPrefabPath = PrefabsPath + "/FuelPickup.prefab";
        private const string FontPath = "Assets/Fonts/WarsOfPrasia.ttf";

        private static readonly Color PanelColor = new Color(0.035f, 0.055f, 0.075f, 0.84f);
        private static readonly Color AccentColor = new Color(0.18f, 0.82f, 0.92f, 1f);
        private static readonly Color GoldColor = new Color(1f, 0.72f, 0.18f, 1f);

        [MenuItem("IsekaiTruck/Setup Main Scene")]
        public static void Setup()
        {
            EnsureFolder("Assets", "Materials");
            EnsureFolder("Assets", "Prefabs");

            Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (font == null)
            {
                throw new System.InvalidOperationException($"UI font was not found: {FontPath}");
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Main";

            CameraFollow cameraFollow = CreateCamera();
            CreateDirectionalLight();
            CreateGlobalVolume();

            GameObject environment = new GameObject("Environment");
            CreateGround(environment.transform);

            GameObject gameplay = new GameObject("Gameplay");
            TruckController truckController = CreateTruck(gameplay.transform);
            Transform truck = truckController.transform;

            GameObject monsters = new GameObject("Monsters");
            monsters.transform.SetParent(gameplay.transform);
            MonsterController monsterPrefab = CreateMonsterPrefab();

            GameObject pickups = new GameObject("Pickups");
            pickups.transform.SetParent(gameplay.transform);

            GameObject systems = new GameObject("Systems");
            PlayerProgress playerProgress = CreatePlayerProgress(systems.transform);
            DrivingTimeManager drivingTimeManager = CreateDrivingTimeManager(systems.transform);
            SpawnManager spawnManager = CreateSpawnManager(
                systems.transform,
                monsterPrefab,
                truck,
                monsters.transform,
                playerProgress);

            FuelPickup fuelPrefab = CreateFuelPrefab();
            CreateTestFuelPickups(fuelPrefab, pickups.transform, truck, drivingTimeManager);

            UiReferences ui = CreateUserInterface(font);
            HUDController hudController = ui.HudPanel.AddComponent<HUDController>();
            hudController.Configure(
                playerProgress,
                drivingTimeManager,
                truckController,
                ui.LevelText,
                ui.ExperienceText,
                ui.DrivingTimeText,
                ui.SoulText,
                ui.SpeedText,
                ui.ExperienceFill,
                ui.DrivingTimeArc,
                ui.SpeedArc,
                ui.SpeedNeedle);

            BottomMenuUI bottomMenu = ui.BottomMenu.AddComponent<BottomMenuUI>();
            bottomMenu.Configure(
                ui.RebirthButton,
                ui.TruckUpgradeButton,
                ui.DriveButton,
                ui.CollectionButton,
                ui.SettingsButton);

            ResultPanelUI resultPanelUI = ui.ResultPanel.AddComponent<ResultPanelUI>();
            resultPanelUI.Configure(
                playerProgress,
                ui.ResultLevelValue,
                ui.ResultDefeatedValue,
                ui.ResultSoulValue,
                ui.RestartButton);

            GameObject gameStateObject = new GameObject("GameStateManager");
            gameStateObject.transform.SetParent(systems.transform);
            GameStateManager gameState = gameStateObject.AddComponent<GameStateManager>();
            gameState.Configure(
                truckController,
                spawnManager,
                drivingTimeManager,
                monsters.transform,
                ui.StartPanel,
                ui.HudPanel,
                ui.ResultPanel,
                ui.StartButton,
                resultPanelUI);

            cameraFollow.Configure(
                truck,
                new Vector3(0f, 14f, -12f),
                new Vector3(45f, 0f, 0f));

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"IsekaiTruck main scene created: {ScenePath}");
        }

        private static CameraFollow CreateCamera()
        {
            GameObject cameraObject = new GameObject("Main Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            CameraFollow cameraFollow = cameraObject.AddComponent<CameraFollow>();
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetPositionAndRotation(
                new Vector3(0f, 14f, -12f),
                Quaternion.Euler(45f, 0f, 0f));
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.fieldOfView = 60f;
            return cameraFollow;
        }

        private static void CreateDirectionalLight()
        {
            GameObject lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.shadows = LightShadows.Soft;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static void CreateGlobalVolume()
        {
            GameObject volumeObject = new GameObject("Global Volume");
            Volume volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(
                "Assets/Settings/DefaultVolumeProfile.asset");
            if (profile != null)
            {
                volume.sharedProfile = profile;
            }
        }

        private static void CreateGround(Transform parent)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(parent);
            ground.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            ground.transform.localScale = new Vector3(50f, 1f, 50f);
            ground.GetComponent<MeshRenderer>().sharedMaterial = GetOrCreateMaterial(
                "Ground",
                new Color(0.32f, 0.35f, 0.38f));
        }

        private static TruckController CreateTruck(Transform parent)
        {
            GameObject truck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            truck.name = "Truck";
            truck.transform.SetParent(parent);
            truck.transform.SetPositionAndRotation(new Vector3(0f, 0.5f, 0f), Quaternion.identity);
            truck.transform.localScale = new Vector3(1.5f, 1f, 3f);
            truck.GetComponent<MeshRenderer>().sharedMaterial = GetOrCreateMaterial(
                "Truck",
                new Color(0.12f, 0.32f, 0.95f));

            Rigidbody body = truck.AddComponent<Rigidbody>();
            body.mass = 2f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            return truck.AddComponent<TruckController>();
        }

        private static MonsterController CreateMonsterPrefab()
        {
            GameObject monster = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            monster.name = "Monster";
            monster.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            monster.GetComponent<MeshRenderer>().sharedMaterial = GetOrCreateMaterial(
                "Monster",
                new Color(0.9f, 0.12f, 0.12f));
            monster.GetComponent<SphereCollider>().isTrigger = true;

            Rigidbody body = monster.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;

            MonsterController controller = monster.AddComponent<MonsterController>();
            controller.ConfigureRewards(10, 1);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(monster, MonsterPrefabPath);
            Object.DestroyImmediate(monster);
            return prefab.GetComponent<MonsterController>();
        }

        private static FuelPickup CreateFuelPrefab()
        {
            GameObject fuel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            fuel.name = "FuelPickup";
            fuel.transform.localScale = new Vector3(0.65f, 0.8f, 0.65f);
            fuel.GetComponent<MeshRenderer>().sharedMaterial = GetOrCreateMaterial(
                "FuelPickup",
                new Color(1f, 0.72f, 0.05f));
            fuel.GetComponent<Collider>().isTrigger = true;

            Rigidbody body = fuel.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            FuelPickup pickup = fuel.AddComponent<FuelPickup>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(fuel, FuelPrefabPath);
            Object.DestroyImmediate(fuel);
            return prefab.GetComponent<FuelPickup>();
        }

        private static void CreateTestFuelPickups(
            FuelPickup prefab,
            Transform parent,
            Transform truck,
            DrivingTimeManager timer)
        {
            Vector3[] positions =
            {
                new Vector3(0f, 0.8f, 12f),
                new Vector3(10f, 0.8f, 5f),
                new Vector3(-10f, 0.8f, 8f)
            };

            for (int index = 0; index < positions.Length; index++)
            {
                GameObject fuel = (GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject, parent);
                fuel.name = $"FuelPickup {index + 1}";
                fuel.transform.position = positions[index];
                fuel.GetComponent<FuelPickup>().Configure(truck, timer);
            }
        }

        private static PlayerProgress CreatePlayerProgress(Transform parent)
        {
            GameObject progressObject = new GameObject("PlayerProgress");
            progressObject.transform.SetParent(parent);
            return progressObject.AddComponent<PlayerProgress>();
        }

        private static DrivingTimeManager CreateDrivingTimeManager(Transform parent)
        {
            GameObject timerObject = new GameObject("DrivingTimeManager");
            timerObject.transform.SetParent(parent);
            return timerObject.AddComponent<DrivingTimeManager>();
        }

        private static SpawnManager CreateSpawnManager(
            Transform parent,
            MonsterController monsterPrefab,
            Transform truck,
            Transform monstersParent,
            PlayerProgress playerProgress)
        {
            GameObject spawnManagerObject = new GameObject("SpawnManager");
            spawnManagerObject.transform.SetParent(parent);
            SpawnManager spawnManager = spawnManagerObject.AddComponent<SpawnManager>();
            spawnManager.Configure(monsterPrefab, truck, monstersParent, playerProgress);
            return spawnManager;
        }

        private static UiReferences CreateUserInterface(Font font)
        {
            GameObject canvasObject = new GameObject("Canvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<InputSystemUIInputModule>();

            UiReferences ui = new UiReferences();
            ui.StartPanel = CreatePanel(
                "StartPanel",
                canvasObject.transform,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero,
                new Color(0.01f, 0.02f, 0.035f, 0.82f));

            Text title = CreateText("TitleText", ui.StartPanel.transform, font, "ISEKAI TRUCK", 72, TextAnchor.MiddleCenter, Color.white);
            SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-400f, 75f), new Vector2(400f, 185f));
            ui.StartButton = CreateButton("StartButton", ui.StartPanel.transform, font, "게임 시작하기", 34);
            SetRect((RectTransform)ui.StartButton.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-190f, -65f), new Vector2(190f, 15f));

            ui.HudPanel = CreateRectObject("HUDPanel", canvasObject.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            CreateHud(font, ui);
            CreateResultPanel(font, canvasObject.transform, ui);
            return ui;
        }

        private static void CreateResultPanel(Font font, Transform canvas, UiReferences ui)
        {
            ui.ResultPanel = CreatePanel(
                "ResultPanel",
                canvas,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero,
                new Color(0.01f, 0.02f, 0.035f, 0.86f));

            GameObject content = CreatePanel(
                "ResultContent",
                ui.ResultPanel.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(-280f, -300f),
                new Vector2(280f, 300f),
                new Color(0.035f, 0.07f, 0.095f, 0.98f));

            Text title = CreateText("ResultTitle", content.transform, font, "주행 종료", 52, TextAnchor.MiddleCenter, Color.white);
            SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(30f, -105f), new Vector2(-30f, -25f));

            Text levelLabel = CreateText("LevelLabel", content.transform, font, "도달 레벨", 25, TextAnchor.MiddleLeft, new Color(0.75f, 0.88f, 0.92f));
            SetRect(levelLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0.58f, 1f), new Vector2(65f, -205f), new Vector2(0f, -145f));
            ui.ResultLevelValue = CreateText("LevelValue", content.transform, font, "1", 36, TextAnchor.MiddleRight, GoldColor);
            SetRect(ui.ResultLevelValue.rectTransform, new Vector2(0.58f, 1f), new Vector2(1f, 1f), new Vector2(0f, -205f), new Vector2(-65f, -145f));

            Text defeatedLabel = CreateText("DefeatedLabel", content.transform, font, "처치한 몬스터", 25, TextAnchor.MiddleLeft, new Color(0.75f, 0.88f, 0.92f));
            SetRect(defeatedLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0.58f, 1f), new Vector2(65f, -285f), new Vector2(0f, -225f));
            ui.ResultDefeatedValue = CreateText("DefeatedValue", content.transform, font, "0", 36, TextAnchor.MiddleRight, AccentColor);
            SetRect(ui.ResultDefeatedValue.rectTransform, new Vector2(0.58f, 1f), new Vector2(1f, 1f), new Vector2(0f, -285f), new Vector2(-65f, -225f));

            Text soulLabel = CreateText("SoulLabel", content.transform, font, "획득한 영혼", 25, TextAnchor.MiddleLeft, new Color(0.75f, 0.88f, 0.92f));
            SetRect(soulLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0.58f, 1f), new Vector2(65f, -365f), new Vector2(0f, -305f));
            ui.ResultSoulValue = CreateText("SoulValue", content.transform, font, "0", 36, TextAnchor.MiddleRight, GoldColor);
            SetRect(ui.ResultSoulValue.rectTransform, new Vector2(0.58f, 1f), new Vector2(1f, 1f), new Vector2(0f, -365f), new Vector2(-65f, -305f));

            ui.RestartButton = CreateButton("RestartButton", content.transform, font, "다시 시작", 30);
            SetRect((RectTransform)ui.RestartButton.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-175f, 55f), new Vector2(175f, 135f));
        }

        private static void CreateHud(Font font, UiReferences ui)
        {
            GameObject left = CreatePanel(
                "LeftPanel",
                ui.HudPanel.transform,
                new Vector2(0f, 0f),
                new Vector2(0.18f, 1f),
                new Vector2(8f, 8f),
                new Vector2(-4f, -8f),
                PanelColor);

            GameObject center = CreateRectObject(
                "CenterPanel",
                ui.HudPanel.transform,
                new Vector2(0.18f, 0f),
                new Vector2(0.82f, 1f),
                new Vector2(4f, 8f),
                new Vector2(-4f, -8f));

            GameObject right = CreatePanel(
                "RightPanel",
                ui.HudPanel.transform,
                new Vector2(0.82f, 0f),
                new Vector2(1f, 1f),
                new Vector2(4f, 8f),
                new Vector2(-8f, -8f),
                PanelColor);

            CreateLeftPanel(font, left.transform, ui);
            CreateCenterPanel(font, center.transform, ui);
            CreateRightPanel(font, right.transform, ui);
        }

        private static void CreateLeftPanel(Font font, Transform parent, UiReferences ui)
        {
            GameObject logo = CreatePanel(
                "LogoArea",
                parent,
                new Vector2(0.08f, 0.76f),
                new Vector2(0.92f, 0.97f),
                Vector2.zero,
                Vector2.zero,
                new Color(0.08f, 0.19f, 0.25f, 0.95f));
            Text logoText = CreateText("LogoPlaceholder", logo.transform, font, "ISEKAI\nTRUCK", 32, TextAnchor.MiddleCenter, Color.white);
            Stretch(logoText.rectTransform, 12f);

            GameObject levelWidget = CreateRectObject(
                "LevelWidget",
                parent,
                new Vector2(0.07f, 0.43f),
                new Vector2(0.93f, 0.73f),
                Vector2.zero,
                Vector2.zero);
            Text levelTitle = CreateText("Title", levelWidget.transform, font, "레벨", 22, TextAnchor.MiddleCenter, new Color(0.78f, 0.9f, 0.95f));
            SetRect(levelTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -44f), Vector2.zero);
            CreateCircularRing("RingBackground", levelWidget.transform, new Color(0.2f, 0.28f, 0.33f, 1f), 1f);
            CreateCircularRing("RingFill", levelWidget.transform, AccentColor, 0.72f);
            ui.LevelText = CreateText("LevelLabel", levelWidget.transform, font, "LV. 1", 34, TextAnchor.MiddleCenter, Color.white);
            SetRect(ui.LevelText.rectTransform, new Vector2(0.15f, 0.08f), new Vector2(0.85f, 0.76f), Vector2.zero, Vector2.zero);

            GameObject timeWidget = CreateRectObject(
                "DrivingTimeWidget",
                parent,
                new Vector2(0.07f, 0.08f),
                new Vector2(0.93f, 0.4f),
                Vector2.zero,
                Vector2.zero);
            Text timeTitle = CreateText("DrivingTimeTitle", timeWidget.transform, font, "주행 가능 시간", 21, TextAnchor.MiddleCenter, new Color(0.78f, 0.9f, 0.95f));
            SetRect(timeTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -46f), Vector2.zero);
            CreateCircularRing("RingBackground", timeWidget.transform, new Color(0.2f, 0.28f, 0.33f, 1f), 1f);
            ui.DrivingTimeArc = CreateCircularRing("RingFill", timeWidget.transform, GoldColor, 1f);
            ui.DrivingTimeText = CreateText("DrivingTimeValue", timeWidget.transform, font, "01:30", 35, TextAnchor.MiddleCenter, Color.white);
            SetRect(ui.DrivingTimeText.rectTransform, new Vector2(0.12f, 0.08f), new Vector2(0.88f, 0.76f), Vector2.zero, Vector2.zero);
        }

        private static void CreateCenterPanel(Font font, Transform parent, UiReferences ui)
        {
            GameObject expArea = CreateRectObject(
                "ExpArea",
                parent,
                new Vector2(0f, 0.91f),
                new Vector2(1f, 1f),
                new Vector2(16f, 0f),
                new Vector2(-16f, 0f));

            GameObject exp = CreatePanel(
                "ExperienceBar",
                expArea.transform,
                new Vector2(0f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(10f, -20f),
                new Vector2(-10f, 20f),
                new Color(0.03f, 0.05f, 0.07f, 0.94f));
            GameObject expFillObject = CreatePanel("Fill", exp.transform, Vector2.zero, Vector2.one, new Vector2(4f, 4f), new Vector2(-4f, -4f), AccentColor);
            ui.ExperienceFill = expFillObject.GetComponent<Image>();
            ui.ExperienceFill.type = Image.Type.Filled;
            ui.ExperienceFill.fillMethod = Image.FillMethod.Horizontal;
            ui.ExperienceFill.fillOrigin = 0;
            ui.ExperienceFill.fillAmount = 0f;
            ui.ExperienceText = CreateText("ExperienceLabel", exp.transform, font, "0 / 100", 21, TextAnchor.MiddleCenter, Color.white);
            Stretch(ui.ExperienceText.rectTransform, 0f);

            CreatePanel(
                "GameViewArea",
                parent,
                new Vector2(0f, 0.11f),
                new Vector2(1f, 0.91f),
                new Vector2(8f, 6f),
                new Vector2(-8f, -6f),
                new Color(0.02f, 0.035f, 0.045f, 0.05f));

            ui.BottomMenu = CreatePanel(
                "BottomMenu",
                parent,
                new Vector2(0f, 0f),
                new Vector2(1f, 0.11f),
                new Vector2(8f, 0f),
                new Vector2(-8f, -4f),
                new Color(0.025f, 0.04f, 0.055f, 0.94f));

            HorizontalLayoutGroup layout = ui.BottomMenu.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 8, 8);
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            ui.RebirthButton = CreateMenuButton("RebirthButton", ui.BottomMenu.transform, font, "환생");
            ui.TruckUpgradeButton = CreateMenuButton("TruckUpgradeButton", ui.BottomMenu.transform, font, "트럭 업글");
            ui.DriveButton = CreateMenuButton("DriveButton", ui.BottomMenu.transform, font, "운전");
            ui.CollectionButton = CreateMenuButton("CollectionButton", ui.BottomMenu.transform, font, "도감");
            ui.SettingsButton = CreateMenuButton("SettingsButton", ui.BottomMenu.transform, font, "환경설정");
        }

        private static void CreateRightPanel(Font font, Transform parent, UiReferences ui)
        {
            GameObject goddess = CreatePanel(
                "GoddessArea",
                parent,
                new Vector2(0.08f, 0.6f),
                new Vector2(0.92f, 0.97f),
                Vector2.zero,
                Vector2.zero,
                new Color(0.18f, 0.22f, 0.32f, 1f));
            Text goddessText = CreateText("PlaceholderLabel", goddess.transform, font, "여신\nPLACEHOLDER", 24, TextAnchor.MiddleCenter, new Color(0.8f, 0.88f, 1f));
            Stretch(goddessText.rectTransform, 12f);

            GameObject soulWidget = CreateRectObject(
                "SoulWidget",
                parent,
                new Vector2(0.12f, 0.34f),
                new Vector2(0.88f, 0.58f),
                Vector2.zero,
                Vector2.zero);
            Text soulTitle = CreateText("SoulTitle", soulWidget.transform, font, "영혼", 22, TextAnchor.MiddleCenter, Color.white);
            SetRect(soulTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -42f), Vector2.zero);
            CreateCircularRing("RingBackground", soulWidget.transform, new Color(0.2f, 0.28f, 0.33f, 1f), 1f);
            CreateCircularRing("RingFill", soulWidget.transform, GoldColor, 0.82f);
            ui.SoulText = CreateText("SoulValue", soulWidget.transform, font, "0", 40, TextAnchor.MiddleCenter, Color.white);
            SetRect(ui.SoulText.rectTransform, new Vector2(0.18f, 0.05f), new Vector2(0.82f, 0.74f), Vector2.zero, Vector2.zero);

            CreateSpeedGauge(font, parent, ui);
        }

        private static void CreateSpeedGauge(Font font, Transform parent, UiReferences ui)
        {
            GameObject gauge = CreateRectObject(
                "SpeedGauge",
                parent,
                new Vector2(0.06f, 0.02f),
                new Vector2(0.94f, 0.32f),
                Vector2.zero,
                Vector2.zero);

            GameObject backgroundObject = CreateRectObject("ArcBackground", gauge.transform, new Vector2(0.5f, 0.54f), new Vector2(0.5f, 0.54f), new Vector2(-108f, -108f), new Vector2(108f, 108f));
            ArcGraphic background = backgroundObject.AddComponent<ArcGraphic>();
            background.color = new Color(0.25f, 0.3f, 0.34f, 1f);
            background.Configure(210f, 15f, 64, 195f);
            background.raycastTarget = false;

            GameObject fillObject = CreateRectObject("ArcFill", gauge.transform, new Vector2(0.5f, 0.54f), new Vector2(0.5f, 0.54f), new Vector2(-108f, -108f), new Vector2(108f, 108f));
            ui.SpeedArc = fillObject.AddComponent<ArcGraphic>();
            ui.SpeedArc.color = AccentColor;
            ui.SpeedArc.Configure(210f, 15f, 64, 195f);
            ui.SpeedArc.FillAmount = 0f;
            ui.SpeedArc.raycastTarget = false;

            GameObject needleObject = CreatePanel("Needle", gauge.transform, new Vector2(0.5f, 0.48f), new Vector2(0.5f, 0.48f), new Vector2(-2.5f, 0f), new Vector2(2.5f, 78f), GoldColor);
            ui.SpeedNeedle = (RectTransform)needleObject.transform;
            ui.SpeedNeedle.pivot = new Vector2(0.5f, 0f);
            ui.SpeedNeedle.localRotation = Quaternion.Euler(0f, 0f, 105f);

            ui.SpeedText = CreateText("SpeedValue", gauge.transform, font, "0", 42, TextAnchor.MiddleCenter, Color.white);
            SetRect(ui.SpeedText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(20f, 40f), new Vector2(-20f, 100f));
            Text unit = CreateText("SpeedUnit", gauge.transform, font, "km/h", 18, TextAnchor.MiddleCenter, new Color(0.75f, 0.85f, 0.9f));
            SetRect(unit.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(20f, 18f), new Vector2(-20f, 50f));
        }

        private static ArcGraphic CreateCircularRing(string name, Transform parent, Color color, float fill)
        {
            GameObject ringObject = CreateRectObject(
                name,
                parent,
                new Vector2(0.5f, 0.42f),
                new Vector2(0.5f, 0.42f),
                new Vector2(-72f, -72f),
                new Vector2(72f, 72f));
            ArcGraphic ring = ringObject.AddComponent<ArcGraphic>();
            ring.color = color;
            ring.Configure(360f, 10f, 64, 90f);
            ring.FillAmount = fill;
            ring.raycastTarget = false;
            return ring;
        }

        private static Button CreateMenuButton(string name, Transform parent, Font font, string label)
        {
            GameObject buttonObject = CreatePanel(
                name,
                parent,
                Vector2.zero,
                Vector2.zero,
                Vector2.zero,
                new Vector2(92f, 86f),
                new Color(0.1f, 0.34f, 0.43f, 0.96f));
            RectTransform rect = (RectTransform)buttonObject.transform;
            rect.sizeDelta = new Vector2(92f, 86f);
            LayoutElement element = buttonObject.AddComponent<LayoutElement>();
            element.preferredWidth = 92f;
            element.preferredHeight = 86f;

            Button button = buttonObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.82f, 1f, 1f);
            colors.pressedColor = new Color(0.62f, 0.85f, 0.9f);
            button.colors = colors;

            GameObject icon = CreatePanel(
                "Icon",
                buttonObject.transform,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(-17f, -43f),
                new Vector2(17f, -9f),
                new Color(0.58f, 0.88f, 0.92f, 0.9f));
            icon.GetComponent<Image>().raycastTarget = false;

            Text text = CreateText("Label", buttonObject.transform, font, label, 18, TextAnchor.MiddleCenter, Color.white);
            SetRect(text.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(4f, 5f), new Vector2(-4f, 39f));
            return button;
        }

        private static GameObject CreateRectObject(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            SetRect((RectTransform)gameObject.transform, anchorMin, anchorMax, offsetMin, offsetMax);
            return gameObject;
        }

        private static GameObject CreatePanel(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax,
            Color color)
        {
            GameObject panel = CreateRectObject(name, parent, anchorMin, anchorMax, offsetMin, offsetMax);
            Image image = panel.AddComponent<Image>();
            image.color = color;
            return panel;
        }

        private static Text CreateText(
            string name,
            Transform parent,
            Font font,
            string value,
            int fontSize,
            TextAnchor alignment,
            Color color)
        {
            GameObject textObject = CreateRectObject(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Text text = textObject.AddComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 12;
            text.resizeTextMaxSize = fontSize;
            return text;
        }

        private static Button CreateButton(string name, Transform parent, Font font, string label, int fontSize)
        {
            GameObject buttonObject = CreatePanel(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.1f, 0.34f, 0.43f, 0.96f));
            Button button = buttonObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.82f, 1f, 1f);
            colors.pressedColor = new Color(0.62f, 0.85f, 0.9f);
            button.colors = colors;
            Text text = CreateText("Label", buttonObject.transform, font, label, fontSize, TextAnchor.MiddleCenter, Color.white);
            Stretch(text.rectTransform, 8f);
            return button;
        }

        private static void SetRect(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.localScale = Vector3.one;
        }

        private static void Stretch(RectTransform rect, float padding)
        {
            SetRect(rect, Vector2.zero, Vector2.one, Vector2.one * padding, Vector2.one * -padding);
        }

        private static Material GetOrCreateMaterial(string name, Color color)
        {
            string path = $"{MaterialsPath}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                material.color = color;
                EditorUtility.SetDirty(material);
                return material;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new System.InvalidOperationException("URP Lit shader was not found.");
            }

            material = new Material(shader) { name = name, color = color };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = Path.Combine(parent, child).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private sealed class UiReferences
        {
            public GameObject StartPanel;
            public GameObject HudPanel;
            public GameObject ResultPanel;
            public GameObject BottomMenu;
            public Button StartButton;
            public Text LevelText;
            public Text ExperienceText;
            public Text DrivingTimeText;
            public Text SoulText;
            public Text SpeedText;
            public Image ExperienceFill;
            public ArcGraphic DrivingTimeArc;
            public ArcGraphic SpeedArc;
            public RectTransform SpeedNeedle;
            public Button RebirthButton;
            public Button TruckUpgradeButton;
            public Button DriveButton;
            public Button CollectionButton;
            public Button SettingsButton;
            public Text ResultLevelValue;
            public Text ResultDefeatedValue;
            public Text ResultSoulValue;
            public Button RestartButton;
        }
    }
}
