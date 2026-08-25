using System;
using System.Collections.Generic;
using System.Text;
using IsekaiTruck.Blessings;
using IsekaiTruck.Camera;
using IsekaiTruck.Collection;
using IsekaiTruck.Config;
using IsekaiTruck.Core;
using IsekaiTruck.Input;
using IsekaiTruck.Monsters;
using IsekaiTruck.Player;
using IsekaiTruck.Rebirth;
using IsekaiTruck.Save;
using IsekaiTruck.Truck;
using IsekaiTruck.UI;
using IsekaiTruck.Upgrades;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace IsekaiTruck.Editor
{
    public static class MonsterCollectionFeatureSetup
    {
        private const string ScenePath = "Assets/IsekaiTruck/Scenes/Main.unity";
        private const string CatalogFolder = "Assets/IsekaiTruck/Collection";
        private const string CatalogPath = CatalogFolder + "/MonsterCollectionCatalog.asset";
        private const string MonsterPrefabCatalogPath = "Assets/IsekaiTruck/Config/MonsterPrefabCatalog.asset";
        private const string GameConfigPath = "Assets/IsekaiTruck/Config/GameConfig.asset";
        private const string BlessingCatalogPath = "Assets/IsekaiTruck/Blessings/BlessingCatalog.asset";
        private const string FontSourcePath = "Assets/IsekaiTruck/Fonts/CartoonHUD.ttf";
        private const string FontPath = CatalogFolder + "/MonsterCollectionFont.asset";
        private const string VerificationSaveKey = "IsekaiTruck.MonsterCollection.Verification";

        private static readonly CollectionSource[] Sources =
        {
            new CollectionSource(
                "Assets/IsekaiTruck/Prefabs/Monsters/Man.prefab",
                "Assets/IsekaiTruck/Art/Sprites/OrdinaryPerson5DirectionWalk.png",
                "Man_Down_0",
                "트럭을 발견하면 반대 방향으로 도망칩니다. 멀리서는 천천히 배회하며 특별한 회피 능력은 없습니다.",
                "도망갈 방향을 예상해 트럭으로 한 번 접촉하면 이세계 전송이 완료됩니다."
            ),
            new CollectionSource(
                "Assets/IsekaiTruck/Prefabs/Monsters/Salesman.prefab",
                "Assets/IsekaiTruck/Art/Sprites/Salesman5DirectionWalk.png",
                "Salesman_Down_0",
                "평소에는 배회하다가 트럭이 가까워지면 빠르게 반대편으로 도망칩니다. 일반 주민보다 전송 보상이 조금 높습니다.",
                "이동 경로를 미리 예상하거나, 가속한 상태로 따라가 접촉하세요."
            ),
            new CollectionSource(
                "Assets/IsekaiTruck/Prefabs/Monsters/Policeman.prefab",
                "Assets/IsekaiTruck/Art/Sprites/Unemployed5DirectionWalk.png",
                "Unemployed_Down_0",
                "트럭을 보면 반대 방향으로 달아나는 평범한 주민이지만, 전송 시 많은 EXP와 영혼을 줍니다.",
                "특별한 회피 능력은 없으므로 한 번 접촉하면 전송됩니다. 보상이 큰 만큼 보이면 우선 찾아보세요."
            ),
            new CollectionSource(
                "Assets/IsekaiTruck/Prefabs/Monsters/Samurai.prefab",
                "Assets/IsekaiTruck/Art/Sprites/Samurai5DirectionWalk.png",
                "Samurai_Down_0",
                "트럭을 인식하면 도망치지 않고 트럭 방향으로 평소 속도의 2배로 빠르게 달려옵니다.",
                "다가오는 경로를 예상해 정면에서 접촉하거나, 측면으로 비켜 지나간 뒤 방향을 바꿔 접촉하세요."
            ),
            new CollectionSource(
                "Assets/IsekaiTruck/Prefabs/Monsters/Ninja.prefab",
                "Assets/IsekaiTruck/Art/Sprites/Ninja5DirectionWalk.png",
                "Ninja_Down_0",
                "첫 번째 접촉을 허수아비 분신으로 피하고, 도망 거리의 2배만큼 멀리 순간이동합니다.",
                "첫 접촉에서는 분신술만 발동합니다. 순간이동한 닌자를 다시 따라가 두 번째로 접촉하면 전송됩니다."
            ),
            new CollectionSource(
                "Assets/IsekaiTruck/Prefabs/Monsters/JeonWoochi.prefab",
                "Assets/IsekaiTruck/Art/Sprites/JeonWoochi5DirectionWalk.png",
                "JeonWoochi_Down_0",
                "3초마다 자신이 지나간 자리에 끈적한 안개를 남깁니다. 안개를 밟은 트럭은 일시적으로 느려집니다.",
                "안개 자국을 그대로 따라가지 말고 측면에서 접근해 접촉하세요. 감속 영역에서는 먼저 빠져나온 뒤 다시 가속하는 것이 안전합니다."
            ),
            new CollectionSource(
                "Assets/IsekaiTruck/Prefabs/Monsters/Mascot.prefab",
                "Assets/IsekaiTruck/Art/Sprites/Mascot5DirectionWalk.png",
                "Mascot_Down_0",
                "트럭과 접촉하면 이세계로 전송되지만, 전단지가 날아와 잠시 화면을 가리는 장난을 남깁니다.",
                "한 번 접촉하면 전송됩니다. 전송 직후 화면이 가려질 수 있으므로 장애물과 다른 주민이 적은 곳에서 접근하세요."
            ),
            new CollectionSource(
                "Assets/IsekaiTruck/Prefabs/Monsters/Turtle.prefab",
                "Assets/IsekaiTruck/Art/Sprites/Turtle5DirectionWalk.png",
                "Turtle_Down_0",
                "트럭과 최소 5의 거리를 강제로 유지해 작은 트럭의 일반적인 접근을 막습니다.",
                "트럭 크기를 키워 접촉 범위를 늘리거나, 멈춤 효과로 거리 유지를 잠시 끊은 뒤 접촉하세요."
            ),
            new CollectionSource(
                "Assets/IsekaiTruck/Prefabs/Monsters/Wizard.prefab",
                "Assets/IsekaiTruck/Art/Sprites/Wizard5DirectionWalk.png",
                "Wizard_Down_0",
                "트럭이 가까워지면 3초 간격으로 도망 방향으로 큰 거리를 순간이동합니다.",
                "순간이동 직후의 재사용 대기 시간에 빠르게 거리를 좁히거나, 멈춤 효과로 순간이동을 막고 접촉하세요."
            )
        };

        [MenuItem("Isekai Truck/Setup Monster Collection")]
        public static void Setup()
        {
            EnsureFolder(CatalogFolder);
            MonsterCollectionCatalog collectionCatalog = CreateCatalog();
            GetOrCreateCollectionFont(collectionCatalog);

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
            GameUIController gameUI = Object.FindFirstObjectByType<GameUIController>();
            JoystickInput joystickInput = Object.FindFirstObjectByType<JoystickInput>();
            CameraController cameraController = Object.FindFirstObjectByType<CameraController>();
            Canvas canvas = GameObject.Find("Game Canvas")?.GetComponent<Canvas>();
            if (gameManager == null || gameUI == null || joystickInput == null || cameraController == null || canvas == null)
            {
                throw new InvalidOperationException("도감 UI 생성에 필요한 Main 씬 참조를 찾지 못했습니다.");
            }

            DestroyExisting(canvas.transform.Find("Monster Collection UI"));
            MonsterCollectionSystem existingSystem = Object.FindFirstObjectByType<MonsterCollectionSystem>();
            if (existingSystem != null)
            {
                Object.DestroyImmediate(existingSystem.gameObject);
            }

            GameObject systemObject = new GameObject("Monster Collection System");
            MonsterCollectionSystem collectionSystem = systemObject.AddComponent<MonsterCollectionSystem>();
            collectionSystem.SetCatalog(collectionCatalog);

            Button collectionButton = (Button)new SerializedObject(gameUI).FindProperty("collectionButton").objectReferenceValue;
            MonsterCollectionUIController collectionUI = CreateUI(canvas.transform, collectionCatalog, collectionButton);
            gameManager.SetMonsterCollectionSystems(collectionSystem, collectionUI);

            EditorUtility.SetDirty(collectionSystem);
            EditorUtility.SetDirty(collectionUI);
            EditorUtility.SetDirty(gameManager);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Verify();

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("Isekai Truck", "처치 기반 몬스터 도감을 Main 씬에 연결했습니다.", "확인");
            }
        }

        public static void Verify()
        {
            MonsterCollectionCatalog catalog = AssetDatabase.LoadAssetAtPath<MonsterCollectionCatalog>(CatalogPath);
            MonsterPrefabCatalog prefabCatalog = AssetDatabase.LoadAssetAtPath<MonsterPrefabCatalog>(MonsterPrefabCatalogPath);
            if (catalog == null || prefabCatalog == null || catalog.Entries.Count != prefabCatalog.MonsterPrefabs.Count || catalog.Entries.Count != Sources.Length)
            {
                throw new InvalidOperationException("도감 카탈로그가 현재 구현된 몬스터 카탈로그와 일치하지 않습니다.");
            }

            HashSet<string> ids = new HashSet<string>();
            for (int i = 0; i < catalog.Entries.Count; i++)
            {
                MonsterCollectionEntry entry = catalog.Entries[i];
                if (entry.Definition == null || entry.Portrait == null || !entry.Portrait.name.EndsWith("Down_0") ||
                    string.IsNullOrWhiteSpace(entry.BehaviorDescription) || string.IsNullOrWhiteSpace(entry.DefeatDescription) ||
                    !ids.Add(entry.TypeId))
                {
                    throw new InvalidOperationException($"도감 {i + 1}번 항목의 데이터가 올바르지 않습니다.");
                }
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
            GameUIController gameUI = Object.FindFirstObjectByType<GameUIController>();
            MonsterCollectionSystem collectionSystem = Object.FindFirstObjectByType<MonsterCollectionSystem>();
            MonsterCollectionUIController collectionUI = Object.FindFirstObjectByType<MonsterCollectionUIController>(FindObjectsInactive.Include);
            if (gameManager == null || gameUI == null || collectionSystem == null || collectionUI == null)
            {
                throw new InvalidOperationException("도감 런타임 시스템이 Main 씬에 없습니다.");
            }

            SerializedObject serializedGameManager = new SerializedObject(gameManager);
            if (serializedGameManager.FindProperty("monsterCollectionSystem").objectReferenceValue != collectionSystem ||
                serializedGameManager.FindProperty("monsterCollectionUIController").objectReferenceValue != collectionUI)
            {
                throw new InvalidOperationException("GameManager의 도감 참조가 연결되지 않았습니다.");
            }

            SerializedObject serializedUI = new SerializedObject(collectionUI);
            Button gameCollectionButton = (Button)new SerializedObject(gameUI).FindProperty("collectionButton").objectReferenceValue;
            SerializedProperty cardsProperty = serializedUI.FindProperty("cards");
            GameObject overlay = (GameObject)serializedUI.FindProperty("overlay").objectReferenceValue;
            RectTransform gameArea = (RectTransform)serializedUI.FindProperty("gameArea").objectReferenceValue;
            MonsterCollectionBookView bookView = (MonsterCollectionBookView)serializedUI.FindProperty("bookView").objectReferenceValue;
            if (serializedUI.FindProperty("openButton").objectReferenceValue != gameCollectionButton ||
                bookView == null || cardsProperty.arraySize != Sources.Length ||
                overlay == null || overlay.activeSelf || overlay.transform.parent != collectionUI.transform ||
                gameArea == null || gameArea.parent != overlay.transform)
            {
                throw new InvalidOperationException("도감 버튼, 책, 카드 또는 초기 상태가 올바르지 않습니다.");
            }

            SerializedObject serializedBookView = new SerializedObject(bookView);
            RectTransform pagesRoot = (RectTransform)serializedBookView.FindProperty("pagesRoot").objectReferenceValue;
            RectTransform cardGridRoot = (RectTransform)serializedBookView.FindProperty("cardGridRoot").objectReferenceValue;
            TMP_Text detailBodyText = (TMP_Text)serializedBookView.FindProperty("detailBodyText").objectReferenceValue;
            Transform collectionBackground = pagesRoot != null ? pagesRoot.Find("Collection Background") : null;
            if (pagesRoot == null || cardGridRoot == null || cardGridRoot.GetComponent<GridLayoutGroup>() == null ||
                collectionBackground == null || pagesRoot.Find("Left Page") != null || pagesRoot.Find("Right Page") != null ||
                detailBodyText == null || detailBodyText.textWrappingMode != TextWrappingModes.Normal ||
                detailBodyText.overflowMode != TextOverflowModes.Ellipsis)
            {
                throw new InvalidOperationException("도감 단일 배경, 카드 그리드 또는 설명 줄바꿈 설정이 올바르지 않습니다.");
            }

            bool overlayWasActive = overlay.activeSelf;
            bool pagesWereActive = pagesRoot.gameObject.activeSelf;
            try
            {
                overlay.SetActive(true);
                pagesRoot.gameObject.SetActive(true);
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(cardGridRoot);

                HashSet<Vector2> cardPositions = new HashSet<Vector2>();
                for (int i = 0; i < cardsProperty.arraySize; i++)
                {
                    MonsterCollectionCardView card = (MonsterCollectionCardView)cardsProperty.GetArrayElementAtIndex(i).objectReferenceValue;
                    CartoonButtonPressEffect pressEffect = card != null ? card.GetComponent<CartoonButtonPressEffect>() : null;
                    SerializedProperty shouldAnimatePosition = pressEffect != null
                        ? new SerializedObject(pressEffect).FindProperty("shouldAnimatePosition")
                        : null;
                    if (card == null || shouldAnimatePosition == null || shouldAnimatePosition.boolValue ||
                        !cardPositions.Add(card.CardRect.anchoredPosition))
                    {
                        throw new InvalidOperationException($"도감 {i + 1}번 카드의 그리드 배치 또는 버튼 효과 설정이 올바르지 않습니다.");
                    }
                }
            }
            finally
            {
                pagesRoot.gameObject.SetActive(pagesWereActive);
                overlay.SetActive(overlayWasActive);
            }

            GameConfig sceneConfig = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath);
            VerifyViewport(collectionUI, gameArea, sceneConfig, 16f / 9f, "1920x1080");
            VerifyViewport(collectionUI, gameArea, sceneConfig, 960f / 600f, "960x600");
            VerifyViewport(collectionUI, gameArea, sceneConfig, 4f / 3f, "4:3");
            VerifyViewport(collectionUI, gameArea, sceneConfig, 16f / 9f, "1920x1080 restore");

            GameObject verificationObject = new GameObject("Monster Collection Verification");
            try
            {
                MonsterCollectionSystem verificationSystem = verificationObject.AddComponent<MonsterCollectionSystem>();
                verificationSystem.SetCatalog(catalog);
                verificationSystem.Initialize();
                string firstId = catalog.Entries[0].TypeId;
                if (verificationSystem.IsUnlocked(firstId) || !verificationSystem.Unlock(firstId) ||
                    verificationSystem.Unlock(firstId) || !verificationSystem.IsUnlocked(firstId))
                {
                    throw new InvalidOperationException("최초 처치 해금 규칙이 올바르지 않습니다.");
                }

                verificationSystem.RestoreState(new List<string> { firstId, "not_implemented" });
                if (!verificationSystem.IsUnlocked(firstId) || verificationSystem.GetUnlockedSnapshot().Count != 1)
                {
                    throw new InvalidOperationException("도감 해금 저장 복원이 구현된 몬스터만 유지하지 않습니다.");
                }

            }
            finally
            {
                Object.DestroyImmediate(verificationObject);
            }

            VerifySaveRoundTrip(catalog);

            Debug.Log("Monster collection feature verification passed.");
        }

        private static void VerifyViewport(
            MonsterCollectionUIController collectionUI,
            RectTransform gameArea,
            GameConfig config,
            float screenAspect,
            string resolutionLabel)
        {
            Rect viewport = CameraController.CalculateViewportRect(
                screenAspect,
                config.Camera.ViewportAspect,
                config.Camera.ViewportHorizontalCenter
            );
            collectionUI.SetViewport(viewport);
            if (gameArea.anchorMin != viewport.min || gameArea.anchorMax != viewport.max ||
                gameArea.offsetMin != Vector2.zero || gameArea.offsetMax != Vector2.zero)
            {
                throw new InvalidOperationException($"{resolutionLabel} 도감 책 영역이 카메라 Viewport와 일치하지 않습니다.");
            }
        }

        private static void VerifySaveRoundTrip(MonsterCollectionCatalog catalog)
        {
            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath);
            BlessingCatalog blessingCatalog = AssetDatabase.LoadAssetAtPath<BlessingCatalog>(BlessingCatalogPath);
            PlayerProgressSaveSystem.DeleteSaveForVerification(VerificationSaveKey);
            GameObject source = new GameObject("Collection Save Verification Source");
            GameObject destination = new GameObject("Collection Save Verification Destination");

            try
            {
                CollectionSaveSystems first = CreateSaveSystems(source, config, blessingCatalog, catalog);
                string unlockedId = catalog.Entries[0].TypeId;
                first.Collection.Unlock(unlockedId);
                first.Save.Save();
                Object.DestroyImmediate(first.Save);

                CollectionSaveSystems restored = CreateSaveSystems(destination, config, blessingCatalog, catalog);
                if (!restored.Collection.IsUnlocked(unlockedId) || restored.Collection.GetUnlockedSnapshot().Count != 1)
                {
                    throw new InvalidOperationException("도감 해금 상태가 PlayerProgressSaveSystem에서 복원되지 않았습니다.");
                }

                Object.DestroyImmediate(restored.Save);
            }
            finally
            {
                Object.DestroyImmediate(destination);
                Object.DestroyImmediate(source);
                PlayerProgressSaveSystem.DeleteSaveForVerification(VerificationSaveKey);
            }
        }

        private static CollectionSaveSystems CreateSaveSystems(
            GameObject root,
            GameConfig config,
            BlessingCatalog blessingCatalog,
            MonsterCollectionCatalog collectionCatalog)
        {
            TruckController truck = root.AddComponent<TruckController>();
            PlayerState player = root.AddComponent<PlayerState>();
            BlessingSystem blessings = root.AddComponent<BlessingSystem>();
            RebirthSystem rebirth = root.AddComponent<RebirthSystem>();
            MonsterCollectionSystem collection = root.AddComponent<MonsterCollectionSystem>();
            TruckUpgradeSystem upgrades = root.AddComponent<TruckUpgradeSystem>();
            PlayerProgressSaveSystem save = root.AddComponent<PlayerProgressSaveSystem>();

            truck.Initialize(config);
            player.Initialize(config);
            blessings.SetCatalog(blessingCatalog);
            blessings.Initialize();
            rebirth.Initialize(config, player, truck, blessings);
            collection.SetCatalog(collectionCatalog);
            collection.Initialize();
            upgrades.Initialize(player, truck);
            save.SetSaveKeyForVerification(VerificationSaveKey);
            save.Initialize(player, truck, rebirth, blessings, null, null, null, null, collection, upgrades);
            return new CollectionSaveSystems(collection, save);
        }

        private static MonsterCollectionCatalog CreateCatalog()
        {
            MonsterCollectionCatalog catalog = AssetDatabase.LoadAssetAtPath<MonsterCollectionCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<MonsterCollectionCatalog>();
                catalog.name = "MonsterCollectionCatalog";
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            List<MonsterCollectionEntry> entries = new List<MonsterCollectionEntry>(Sources.Length);
            for (int i = 0; i < Sources.Length; i++)
            {
                CollectionSource source = Sources[i];
                MonsterController prefab = AssetDatabase.LoadAssetAtPath<MonsterController>(source.PrefabPath);
                MonsterDefinition definition = prefab != null ? prefab.GetComponent<MonsterDefinition>() : null;
                Sprite portrait = LoadSprite(source.SpritePath, source.SpriteName);
                if (definition == null || portrait == null)
                {
                    throw new InvalidOperationException($"도감 원본 데이터를 찾지 못했습니다: {source.PrefabPath}");
                }

                entries.Add(new MonsterCollectionEntry(definition, portrait, source.Behavior, source.Defeat));
            }

            catalog.SetEntries(entries);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return catalog;
        }

        private static MonsterCollectionUIController CreateUI(
            Transform canvas,
            MonsterCollectionCatalog catalog,
            Button collectionButton)
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font == null || collectionButton == null)
            {
                throw new InvalidOperationException("도감 UI 폰트 또는 열기 버튼을 찾지 못했습니다.");
            }

            GameObject root = CreateUIObject("Monster Collection UI", canvas);
            Stretch(root.GetComponent<RectTransform>());
            MonsterCollectionUIController controller = root.AddComponent<MonsterCollectionUIController>();

            GameObject overlay = CreatePanel("Collection Overlay", root.transform, new Color(0.19f, 0.13f, 0.20f, 0.84f));
            Stretch(overlay.GetComponent<RectTransform>());
            CartoonUIStyle.StyleScrim(overlay);
            CanvasGroup overlayCanvasGroup = overlay.AddComponent<CanvasGroup>();

            GameObject gameAreaObject = CreateUIObject("Collection Game Area", overlay.transform);
            RectTransform gameArea = gameAreaObject.GetComponent<RectTransform>();
            SetRect(gameArea, new Vector2(0.19f, 0f), new Vector2(0.79f, 1f));

            GameObject bookRootObject = CreateUIObject("Book Root", gameArea);
            RectTransform bookRoot = bookRootObject.GetComponent<RectTransform>();
            CenterWithSize(bookRoot, new Vector2(1060f, 720f));
            ResponsivePanelFitter fitter = bookRootObject.AddComponent<ResponsivePanelFitter>();
            fitter.Configure(bookRoot.sizeDelta, 22f, 22f);
            MonsterCollectionBookView bookView = bookRootObject.AddComponent<MonsterCollectionBookView>();

            RectTransform coverRoot = CreateCover(bookRoot, font);
            RectTransform pagesRoot = CreatePages(bookRoot, font, out CanvasGroup pagesCanvasGroup);
            Button closeButton = CreateButton("Close Collection Button", pagesRoot, font, "닫기", HudColorPalette.Level, HudColorPalette.LevelDepth, HudColorPalette.DarkInk);
            RectTransform closeRect = closeButton.GetComponent<RectTransform>();
            SetRect(closeRect, new Vector2(0.84f, 0.88f), new Vector2(0.96f, 0.96f));

            GameObject viewportObject = CreateUIObject("Card Viewport", pagesRoot);
            RectTransform viewport = viewportObject.GetComponent<RectTransform>();
            SetRect(viewport, new Vector2(0.07f, 0.10f), new Vector2(0.93f, 0.84f));
            viewportObject.AddComponent<RectMask2D>();
            CanvasGroup cardGridCanvasGroup = viewportObject.AddComponent<CanvasGroup>();

            GameObject cardContentObject = CreateUIObject("Card Grid", viewport);
            RectTransform cardContent = cardContentObject.GetComponent<RectTransform>();
            cardContent.anchorMin = new Vector2(0f, 1f);
            cardContent.anchorMax = new Vector2(1f, 1f);
            cardContent.pivot = new Vector2(0.5f, 1f);
            cardContent.anchoredPosition = Vector2.zero;
            cardContent.sizeDelta = new Vector2(0f, 500f);
            GridLayoutGroup grid = cardContentObject.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(32, 32, 10, 10);
            grid.cellSize = new Vector2(250f, 148f);
            grid.spacing = new Vector2(24f, 18f);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;

            ScrollRect scrollRect = viewportObject.AddComponent<ScrollRect>();
            scrollRect.viewport = viewport;
            scrollRect.content = cardContent;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 28f;

            MonsterCollectionCardView[] cards = new MonsterCollectionCardView[catalog.Entries.Count];
            for (int i = 0; i < cards.Length; i++)
            {
                cards[i] = CreateCard(cardContent, font, catalog.Entries[i], i);
            }

            GameObject animationLayerObject = CreateUIObject("Card Detail Animation Layer", bookRoot);
            RectTransform animationLayer = animationLayerObject.GetComponent<RectTransform>();
            Stretch(animationLayer);

            CreatePreviewCard(animationLayer, font, out RectTransform previewCard, out Image previewPortrait, out TMP_Text previewNameText);
            CreateDetailScroll(animationLayer, font, out RectTransform detailScroll, out CanvasGroup detailScrollCanvasGroup, out TMP_Text detailNameText, out TMP_Text detailBodyText);

            bookView.SetReferences(
                overlayCanvasGroup,
                coverRoot,
                pagesRoot,
                pagesCanvasGroup,
                cardGridCanvasGroup,
                cardContent,
                animationLayer,
                previewCard,
                previewPortrait,
                previewNameText,
                detailScroll,
                detailScrollCanvasGroup,
                detailNameText,
                detailBodyText
            );
            controller.SetReferences(gameArea, overlay, collectionButton, closeButton, bookView, cards);
            overlay.SetActive(false);
            root.transform.SetAsLastSibling();
            return controller;
        }

        private static RectTransform CreateCover(Transform parent, TMP_FontAsset font)
        {
            GameObject cover = CreatePanel("Closed Book Cover", parent, HudColorPalette.LevelDepth);
            RectTransform rect = cover.GetComponent<RectTransform>();
            CenterWithSize(rect, new Vector2(370f, 590f));
            CartoonUIStyle.StylePanel(cover, HudColorPalette.LevelDepth, new Color(0.30f, 0.16f, 0.22f, 1f));

            GameObject inset = CreatePanel("Cover Inset", cover.transform, HudColorPalette.Level);
            SetRect(inset.GetComponent<RectTransform>(), new Vector2(0.09f, 0.08f), new Vector2(0.91f, 0.92f));
            CartoonUIStyle.StylePanel(inset, HudColorPalette.Level, HudColorPalette.LevelDepth);
            TMP_Text title = CreateText("Cover Title", inset.transform, font, "주민\n도감", 42, TextAlignmentOptions.Center, HudColorPalette.DarkInk, true);
            SetRect(title.rectTransform, new Vector2(0.12f, 0.34f), new Vector2(0.88f, 0.72f));
            TMP_Text mark = CreateText("Cover Mark", inset.transform, font, "?", 82, TextAlignmentOptions.Center, HudColorPalette.SoftWhite, true);
            SetRect(mark.rectTransform, new Vector2(0.32f, 0.12f), new Vector2(0.68f, 0.34f));
            return rect;
        }

        private static RectTransform CreatePages(Transform parent, TMP_FontAsset font, out CanvasGroup canvasGroup)
        {
            GameObject pages = CreateUIObject("Open Book Pages", parent);
            RectTransform pagesRect = pages.GetComponent<RectTransform>();
            StretchWithOffsets(pagesRect, 8f, 8f, 8f, 8f);
            canvasGroup = pages.AddComponent<CanvasGroup>();

            GameObject background = CreatePanel("Collection Background", pages.transform, HudColorPalette.Cream);
            SetRect(background.GetComponent<RectTransform>(), new Vector2(0.01f, 0.02f), new Vector2(0.99f, 0.98f));
            CartoonUIStyle.StylePanel(background, HudColorPalette.Cream, HudColorPalette.UpgradeDepth);

            TMP_Text title = CreateText("Book Title", pages.transform, font, "주민 도감", 36, TextAlignmentOptions.Center, HudColorPalette.DarkInk, true);
            SetRect(title.rectTransform, new Vector2(0.26f, 0.87f), new Vector2(0.74f, 0.97f));
            TMP_Text hint = CreateText("Book Hint", pages.transform, font, "전송 기록이 있는 주민 카드를 눌러 정보를 확인하세요", 18, TextAlignmentOptions.Center, HudColorPalette.DarkInk, false);
            SetRect(hint.rectTransform, new Vector2(0.22f, 0.82f), new Vector2(0.78f, 0.87f));
            return pagesRect;
        }

        private static MonsterCollectionCardView CreateCard(Transform parent, TMP_FontAsset font, MonsterCollectionEntry entry, int index)
        {
            Color faceColor = index % 2 == 0 ? HudColorPalette.ModalFace : HudColorPalette.ModalInset;
            GameObject card = CreatePanel($"Monster Card {index + 1}", parent, faceColor);
            CartoonUIStyle.StylePanel(card, faceColor, HudColorPalette.LevelDepth);
            RectTransform cardRect = card.GetComponent<RectTransform>();
            CanvasGroup canvasGroup = card.AddComponent<CanvasGroup>();
            Button button = card.AddComponent<Button>();
            button.targetGraphic = card.GetComponent<Image>();
            CartoonUIStyle.StyleButton(button, faceColor, HudColorPalette.LevelDepth, HudColorPalette.DarkInk, false);

            GameObject portraitObject = CreateUIObject("Portrait", card.transform);
            Image portrait = portraitObject.AddComponent<Image>();
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            SetRect(portrait.rectTransform, new Vector2(0.12f, 0.24f), new Vector2(0.88f, 0.93f));

            TMP_Text question = CreateText("Question", card.transform, font, "?", 58, TextAlignmentOptions.Center, HudColorPalette.LevelDepth, true);
            SetRect(question.rectTransform, new Vector2(0.20f, 0.25f), new Vector2(0.80f, 0.90f));
            TMP_Text name = CreateText("Name", card.transform, font, entry.DisplayName, 20, TextAlignmentOptions.Center, HudColorPalette.DarkInk, true);
            SetRect(name.rectTransform, new Vector2(0.08f, 0.03f), new Vector2(0.92f, 0.25f));

            GameObject selectionFrame = CreateUIObject("Selection Frame", card.transform);
            Image frameImage = selectionFrame.AddComponent<Image>();
            frameImage.sprite = GetRoundedSprite();
            frameImage.type = Image.Type.Sliced;
            frameImage.color = new Color(1f, 1f, 1f, 0.01f);
            frameImage.raycastTarget = false;
            Stretch(selectionFrame.GetComponent<RectTransform>());
            Outline outline = selectionFrame.AddComponent<Outline>();
            outline.effectColor = HudColorPalette.Upgrade;
            outline.effectDistance = new Vector2(4f, -4f);
            selectionFrame.SetActive(false);

            MonsterCollectionCardView view = card.AddComponent<MonsterCollectionCardView>();
            view.SetReferences(cardRect, button, portrait, name, question, canvasGroup, selectionFrame);
            return view;
        }

        private static void CreatePreviewCard(
            Transform parent,
            TMP_FontAsset font,
            out RectTransform cardRect,
            out Image portrait,
            out TMP_Text nameText)
        {
            GameObject card = CreatePanel("Selected Monster Card", parent, HudColorPalette.ModalFace);
            CartoonUIStyle.StylePanel(card, HudColorPalette.ModalFace, HudColorPalette.LevelDepth);
            cardRect = card.GetComponent<RectTransform>();
            CenterWithSize(cardRect, new Vector2(250f, 320f));

            GameObject portraitObject = CreateUIObject("Portrait", card.transform);
            portrait = portraitObject.AddComponent<Image>();
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            SetRect(portrait.rectTransform, new Vector2(0.08f, 0.20f), new Vector2(0.92f, 0.94f));
            nameText = CreateText("Name", card.transform, font, string.Empty, 25, TextAlignmentOptions.Center, HudColorPalette.DarkInk, true);
            SetRect(nameText.rectTransform, new Vector2(0.08f, 0.03f), new Vector2(0.92f, 0.20f));
            card.SetActive(false);
        }

        private static void CreateDetailScroll(
            Transform parent,
            TMP_FontAsset font,
            out RectTransform scrollRect,
            out CanvasGroup canvasGroup,
            out TMP_Text nameText,
            out TMP_Text bodyText)
        {
            GameObject scroll = CreatePanel("Monster Detail Scroll", parent, HudColorPalette.ModalInset);
            CartoonUIStyle.StylePanel(scroll, HudColorPalette.ModalInset, HudColorPalette.UpgradeDepth);
            scrollRect = scroll.GetComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.5f, 0.5f);
            scrollRect.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRect.pivot = new Vector2(0f, 0.5f);
            scrollRect.anchoredPosition = new Vector2(-230f, -18f);
            scrollRect.sizeDelta = new Vector2(650f, 440f);
            canvasGroup = scroll.AddComponent<CanvasGroup>();

            CreateScrollRoll("Left Roll", scroll.transform, new Vector2(0f, 0.5f));
            CreateScrollRoll("Right Roll", scroll.transform, new Vector2(1f, 0.5f));
            nameText = CreateText("Detail Name", scroll.transform, font, string.Empty, 31, TextAlignmentOptions.Center, HudColorPalette.LevelDepth, true);
            SetRect(nameText.rectTransform, new Vector2(0.08f, 0.79f), new Vector2(0.92f, 0.94f));
            bodyText = CreateText("Detail Body", scroll.transform, font, string.Empty, 21, TextAlignmentOptions.TopLeft, HudColorPalette.DarkInk, false);
            SetRect(bodyText.rectTransform, new Vector2(0.10f, 0.10f), new Vector2(0.90f, 0.79f));
            bodyText.enableAutoSizing = true;
            bodyText.fontSizeMin = 15f;
            bodyText.fontSizeMax = 21f;
            bodyText.textWrappingMode = TextWrappingModes.Normal;
            bodyText.overflowMode = TextOverflowModes.Ellipsis;
            scroll.SetActive(false);
        }

        private static void CreateScrollRoll(string name, Transform parent, Vector2 anchor)
        {
            GameObject roll = CreatePanel(name, parent, HudColorPalette.Upgrade);
            RectTransform rect = roll.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor.x < 0.5f ? new Vector2(0.5f, 0.5f) : new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(26f, 474f);
            rect.anchoredPosition = Vector2.zero;
            roll.GetComponent<Image>().raycastTarget = false;
        }

        private static Button CreateButton(
            string name,
            Transform parent,
            TMP_FontAsset font,
            string label,
            Color faceColor,
            Color depthColor,
            Color textColor)
        {
            GameObject buttonObject = CreatePanel(name, parent, faceColor);
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonObject.GetComponent<Image>();
            TMP_Text text = CreateText("Label", buttonObject.transform, font, label, 20, TextAlignmentOptions.Center, textColor, true);
            StretchWithOffsets(text.rectTransform, 8f, 8f, 4f, 4f);
            CartoonUIStyle.StyleButton(button, faceColor, depthColor, textColor);
            return button;
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(parent, false);
            Image image = panel.GetComponent<Image>();
            image.sprite = GetRoundedSprite();
            image.type = Image.Type.Sliced;
            image.color = color;
            return panel;
        }

        private static TMP_Text CreateText(
            string name,
            Transform parent,
            TMP_FontAsset font,
            string value,
            float fontSize,
            TextAlignmentOptions alignment,
            Color color,
            bool bold)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            TMP_Text text = textObject.GetComponent<TMP_Text>();
            text.font = font;
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            text.raycastTarget = false;
            return text;
        }

        private static Sprite LoadSprite(string path, string spriteName)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite && sprite.name == spriteName)
                {
                    return sprite;
                }
            }

            return null;
        }

        private static TMP_FontAsset GetOrCreateCollectionFont(MonsterCollectionCatalog catalog)
        {
            string characters = "주민도감전송기록이있는카드를눌러정보를확인하세요닫기특징보상팁EXP+영혼???";
            for (int i = 0; i < catalog.Entries.Count; i++)
            {
                MonsterCollectionEntry entry = catalog.Entries[i];
                characters += entry.DisplayName + entry.BehaviorDescription + entry.DefeatDescription;
            }

            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font != null)
            {
                SerializedObject serializedFont = new SerializedObject(font);
                if (serializedFont.FindProperty("m_AtlasWidth").intValue != 1024 ||
                    !string.IsNullOrEmpty(FindMissingCharacters(font, characters)))
                {
                    AssetDatabase.DeleteAsset(FontPath);
                    font = null;
                }
                else
                {
                    return font;
                }
            }

            if (font == null)
            {
                Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(FontSourcePath);
                if (sourceFont == null)
                {
                    throw new InvalidOperationException("도감에 사용할 카툰 HUD 원본 폰트를 찾지 못했습니다.");
                }

                font = TMP_FontAsset.CreateFontAsset(
                    sourceFont,
                    42,
                    5,
                    GlyphRenderMode.SDFAA,
                    1024,
                    1024,
                    AtlasPopulationMode.Dynamic,
                    false
                );
                if (font == null)
                {
                    throw new InvalidOperationException("도감용 TMP 폰트 생성에 실패했습니다.");
                }

                font.name = "MonsterCollectionFont";
            }

            if (!font.TryAddCharacters(characters, out string missingCharacters))
            {
                throw new InvalidOperationException($"도감 폰트에 추가하지 못한 문자가 있습니다: {missingCharacters}");
            }

            font.atlasPopulationMode = AtlasPopulationMode.Static;
            AssetDatabase.CreateAsset(font, FontPath);
            Texture2D atlasTexture = font.atlasTexture;
            atlasTexture.name = "MonsterCollectionFont Atlas";
            atlasTexture.filterMode = FilterMode.Bilinear;
            atlasTexture.anisoLevel = 0;
            AssetDatabase.AddObjectToAsset(atlasTexture, font);

            Material fontMaterial = font.material;
            fontMaterial.name = "MonsterCollectionFont Material";
            AssetDatabase.AddObjectToAsset(fontMaterial, font);

            EditorUtility.SetDirty(font);
            EditorUtility.SetDirty(font.atlasTexture);
            AssetDatabase.SaveAssets();
            return font;
        }

        private static string FindMissingCharacters(TMP_FontAsset font, string characters)
        {
            HashSet<uint> availableCharacters = new HashSet<uint>();
            for (int i = 0; i < font.characterTable.Count; i++)
            {
                availableCharacters.Add(font.characterTable[i].unicode);
            }

            HashSet<char> missingCharacters = new HashSet<char>();
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < characters.Length; i++)
            {
                char character = characters[i];
                if (char.IsWhiteSpace(character) || availableCharacters.Contains(character) || !missingCharacters.Add(character))
                {
                    continue;
                }

                result.Append(character);
            }

            return result.ToString();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            int separator = path.LastIndexOf('/');
            string parent = path.Substring(0, separator);
            string folder = path.Substring(separator + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folder);
        }

        private static void DestroyExisting(Transform target)
        {
            if (target != null)
            {
                Object.DestroyImmediate(target.gameObject);
            }
        }

        private static Sprite GetRoundedSprite()
        {
            return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        }

        private static void Stretch(RectTransform rect)
        {
            SetRect(rect, Vector2.zero, Vector2.one);
        }

        private static void StretchWithOffsets(RectTransform rect, float left, float right, float bottom, float top)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void CenterWithSize(RectTransform rect, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
        }

        private readonly struct CollectionSource
        {
            public CollectionSource(string prefabPath, string spritePath, string spriteName, string behavior, string defeat)
            {
                PrefabPath = prefabPath;
                SpritePath = spritePath;
                SpriteName = spriteName;
                Behavior = behavior;
                Defeat = defeat;
            }

            public string PrefabPath { get; }
            public string SpritePath { get; }
            public string SpriteName { get; }
            public string Behavior { get; }
            public string Defeat { get; }
        }

        private readonly struct CollectionSaveSystems
        {
            public CollectionSaveSystems(MonsterCollectionSystem collection, PlayerProgressSaveSystem save)
            {
                Collection = collection;
                Save = save;
            }

            public MonsterCollectionSystem Collection { get; }
            public PlayerProgressSaveSystem Save { get; }
        }
    }
}
