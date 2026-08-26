using System;
using IsekaiTruck.Camera;
using IsekaiTruck.Config;
using IsekaiTruck.Core;
using IsekaiTruck.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace IsekaiTruck.Editor
{
    public static class SystemGuideFeatureSetup
    {
        private const string ScenePath = "Assets/IsekaiTruck/Scenes/Main.unity";
        private const string GameConfigPath = "Assets/IsekaiTruck/Config/GameConfig.asset";
        private const string PrefabFolder = "Assets/IsekaiTruck/Prefabs/UI";
        private const string PrefabPath = PrefabFolder + "/SystemGuidePopup.prefab";

        [MenuItem("Isekai Truck/Setup System Guide")]
        public static void Setup()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
            Canvas canvas = GameObject.Find("Game Canvas")?.GetComponent<Canvas>();
            if (gameManager == null || canvas == null)
            {
                throw new InvalidOperationException("시스템 안내 UI 생성에 필요한 Main 씬 참조를 찾지 못했습니다.");
            }

            EnsureFolder(PrefabFolder);
            DestroyExisting(canvas.transform.Find("System Guide UI"));
            SystemGuidePopup popup = CreateUI(canvas.transform, CartoonUIStyle.LoadFont());
            GameObject sceneRoot = popup.gameObject;
            PrefabUtility.SaveAsPrefabAssetAndConnect(
                popup.gameObject,
                PrefabPath,
                InteractionMode.AutomatedAction
            );
            popup = sceneRoot.GetComponent<SystemGuidePopup>();
            gameManager.SetSystemGuidePopup(popup);

            EditorUtility.SetDirty(popup);
            EditorUtility.SetDirty(gameManager);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Verify();

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("Isekai Truck", "게임 시작 전 시스템 안내 팝업을 Main 씬에 연결했습니다.", "확인");
            }
        }

        public static void Verify()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
            SystemGuidePopup popup = Object.FindFirstObjectByType<SystemGuidePopup>(FindObjectsInactive.Include);
            SystemGuidePopup prefab = AssetDatabase.LoadAssetAtPath<SystemGuidePopup>(PrefabPath);
            if (gameManager == null || popup == null || prefab == null || !PrefabUtility.IsPartOfPrefabInstance(popup))
            {
                throw new InvalidOperationException("시스템 안내 프리팹 또는 Main 씬 인스턴스가 없습니다.");
            }

            SerializedObject serializedGameManager = new SerializedObject(gameManager);
            SerializedObject serializedPopup = new SerializedObject(popup);
            GameObject overlay = (GameObject)serializedPopup.FindProperty("overlay").objectReferenceValue;
            RectTransform gameArea = (RectTransform)serializedPopup.FindProperty("gameArea").objectReferenceValue;
            RectTransform panelRoot = (RectTransform)serializedPopup.FindProperty("panelRoot").objectReferenceValue;
            Button previousButton = (Button)serializedPopup.FindProperty("previousButton").objectReferenceValue;
            Button nextButton = (Button)serializedPopup.FindProperty("nextButton").objectReferenceValue;
            Text nextButtonText = (Text)serializedPopup.FindProperty("nextButtonText").objectReferenceValue;
            SerializedProperty pages = serializedPopup.FindProperty("pages");

            if (serializedGameManager.FindProperty("systemGuidePopup").objectReferenceValue != popup ||
                overlay == null || overlay.activeSelf || gameArea == null || panelRoot == null ||
                previousButton == null || nextButton == null || nextButtonText == null || pages.arraySize != 5 ||
                popup.transform.parent == null || popup.transform.parent.name != "Game Canvas" ||
                popup.transform.parent.Find("System Guide UI/Collection Guide Skip Button") != null)
            {
                throw new InvalidOperationException("시스템 안내 팝업의 씬 연결 또는 기본 상태가 올바르지 않습니다.");
            }

            Image overlayImage = overlay.GetComponent<Image>();
            Image panelImage = panelRoot.GetComponent<Image>();
            if (overlayImage == null || overlayImage.color.a <= 0f || overlayImage.color.a >= 0.5f ||
                panelImage == null || panelImage.color.a < 0.85f || panelImage.color.a >= 1f)
            {
                throw new InvalidOperationException("시스템 안내 팝업의 반투명 배경 스타일이 올바르지 않습니다.");
            }

            for (int i = 0; i < pages.arraySize; i++)
            {
                SerializedProperty page = pages.GetArrayElementAtIndex(i);
                SerializedProperty tips = page.FindPropertyRelative("tips");
                if (string.IsNullOrWhiteSpace(page.FindPropertyRelative("category").stringValue) ||
                    string.IsNullOrWhiteSpace(page.FindPropertyRelative("title").stringValue) ||
                    string.IsNullOrWhiteSpace(page.FindPropertyRelative("summary").stringValue) || tips.arraySize != 3)
                {
                    throw new InvalidOperationException($"시스템 안내 {i + 1}페이지가 간결한 3개 항목 구조로 구성되지 않았습니다.");
                }
            }

            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath);
            VerifyViewport(popup, gameArea, config, 16f / 9f, "1920x1080");
            VerifyViewport(popup, gameArea, config, 960f / 600f, "960x600");
            VerifyViewport(popup, gameArea, config, 4f / 3f, "4:3");
            VerifyViewport(popup, gameArea, config, 16f / 9f, "1920x1080 restore");

            Debug.Log("System guide feature verification passed.");
        }

        private static SystemGuidePopup CreateUI(Transform canvas, Font font)
        {
            GameObject root = CreateUIObject("System Guide UI", canvas);
            Stretch(root.GetComponent<RectTransform>());
            SystemGuidePopup popup = root.AddComponent<SystemGuidePopup>();

            GameObject overlay = CreatePanel("System Guide Overlay", root.transform, new Color(0.16f, 0.11f, 0.16f, 0.30f));
            Stretch(overlay.GetComponent<RectTransform>());
            overlay.GetComponent<Image>().sprite = null;
            overlay.GetComponent<Image>().type = Image.Type.Simple;
            CanvasGroup overlayCanvasGroup = overlay.AddComponent<CanvasGroup>();

            GameObject gameAreaObject = CreateUIObject("System Guide Game Area", overlay.transform);
            RectTransform gameArea = gameAreaObject.GetComponent<RectTransform>();
            Stretch(gameArea);

            Color panelColor = new Color(1f, 0.99f, 0.97f, 0.94f);
            GameObject panel = CreatePanel("System Guide Panel", gameArea, panelColor);
            RectTransform panelRoot = panel.GetComponent<RectTransform>();
            CenterWithSize(panelRoot, new Vector2(980f, 650f));
            CartoonUIStyle.StylePanel(panel, panelColor, HudColorPalette.LevelDepth);
            CanvasGroup panelCanvasGroup = panel.AddComponent<CanvasGroup>();
            ResponsivePanelFitter fitter = panel.AddComponent<ResponsivePanelFitter>();
            fitter.Configure(panelRoot.sizeDelta, 24f, 24f);

            GameObject topAccent = CreatePanel("Top Accent", panel.transform, HudColorPalette.Level);
            SetRect(topAccent.GetComponent<RectTransform>(), new Vector2(0f, 0.975f), Vector2.one);
            topAccent.GetComponent<Image>().raycastTarget = false;

            GameObject contentObject = CreateUIObject("Guide Page Content", panel.transform);
            RectTransform contentRoot = contentObject.GetComponent<RectTransform>();
            Stretch(contentRoot);
            CanvasGroup contentCanvasGroup = contentObject.AddComponent<CanvasGroup>();

            GameObject categoryPanel = CreatePanel("Guide Category", contentRoot, HudColorPalette.LevelFill);
            SetRect(categoryPanel.GetComponent<RectTransform>(), new Vector2(0.07f, 0.865f), new Vector2(0.30f, 0.935f));
            CartoonUIStyle.StylePanel(categoryPanel, HudColorPalette.LevelFill, HudColorPalette.LevelDepth, false);
            Text categoryText = CreateText("Category Text", categoryPanel.transform, font, string.Empty, 18, TextAnchor.MiddleCenter, true);
            StretchWithOffsets(categoryText.rectTransform, 10f, 10f, 3f, 3f);

            Text titleText = CreateText("Guide Title", contentRoot, font, string.Empty, 35, TextAnchor.MiddleLeft, true);
            SetRect(titleText.rectTransform, new Vector2(0.07f, 0.745f), new Vector2(0.93f, 0.855f));
            Text summaryText = CreateText("Guide Summary", contentRoot, font, string.Empty, 20, TextAnchor.UpperLeft, false);
            SetRect(summaryText.rectTransform, new Vector2(0.07f, 0.635f), new Vector2(0.93f, 0.745f));
            summaryText.horizontalOverflow = HorizontalWrapMode.Wrap;
            summaryText.verticalOverflow = VerticalWrapMode.Truncate;

            GameObject[] tipCards = new GameObject[3];
            Text[] tipNumberTexts = new Text[3];
            Text[] tipTitleTexts = new Text[3];
            Text[] tipBodyTexts = new Text[3];
            for (int i = 0; i < tipCards.Length; i++)
            {
                float left = 0.06f + i * 0.30f;
                float right = left + 0.28f;
                Color cardColor = new Color(1f, 0.96f, 0.87f, 0.90f);
                GameObject card = CreatePanel($"Guide Tip {i + 1}", contentRoot, cardColor);
                SetRect(card.GetComponent<RectTransform>(), new Vector2(left, 0.245f), new Vector2(right, 0.605f));
                CartoonUIStyle.StylePanel(card, cardColor, HudColorPalette.UpgradeDepth);

                GameObject numberBadge = CreatePanel("Number Badge", card.transform, HudColorPalette.Upgrade);
                SetRect(numberBadge.GetComponent<RectTransform>(), new Vector2(0.07f, 0.75f), new Vector2(0.25f, 0.93f));
                CartoonUIStyle.StylePanel(numberBadge, numberBadge.GetComponent<Image>().color, HudColorPalette.LevelDepth, false);
                tipNumberTexts[i] = CreateText("Number", numberBadge.transform, font, $"0{i + 1}", 17, TextAnchor.MiddleCenter, true);
                Stretch(tipNumberTexts[i].rectTransform);

                tipTitleTexts[i] = CreateText("Tip Title", card.transform, font, string.Empty, 22, TextAnchor.MiddleLeft, true);
                SetRect(tipTitleTexts[i].rectTransform, new Vector2(0.29f, 0.72f), new Vector2(0.93f, 0.94f));
                tipBodyTexts[i] = CreateText("Tip Body", card.transform, font, string.Empty, 17, TextAnchor.UpperLeft, false);
                SetRect(tipBodyTexts[i].rectTransform, new Vector2(0.08f, 0.10f), new Vector2(0.92f, 0.70f));
                tipBodyTexts[i].horizontalOverflow = HorizontalWrapMode.Wrap;
                tipBodyTexts[i].verticalOverflow = VerticalWrapMode.Truncate;
                tipBodyTexts[i].lineSpacing = 1.08f;

                tipCards[i] = card;
            }

            GameObject divider = CreatePanel("Footer Divider", panel.transform, new Color(HudColorPalette.LevelDepth.r, HudColorPalette.LevelDepth.g, HudColorPalette.LevelDepth.b, 0.22f));
            SetRect(divider.GetComponent<RectTransform>(), new Vector2(0.06f, 0.205f), new Vector2(0.94f, 0.208f));
            divider.GetComponent<Image>().raycastTarget = false;

            Button previousButton = CreateButton("Previous Guide Page Button", panel.transform, font, "← 이전", 20, HudColorPalette.Cream, HudColorPalette.UpgradeDepth);
            SetRect(previousButton.GetComponent<RectTransform>(), new Vector2(0.06f, 0.065f), new Vector2(0.26f, 0.165f));
            Text pageIndicatorText = CreateText("Guide Page Indicator", panel.transform, font, "1 / 5", 19, TextAnchor.MiddleCenter, true);
            SetRect(pageIndicatorText.rectTransform, new Vector2(0.39f, 0.065f), new Vector2(0.61f, 0.165f));
            Button nextButton = CreateButton("Next Guide Page Button", panel.transform, font, "다음 →", 20, HudColorPalette.Level, HudColorPalette.LevelDepth);
            SetRect(nextButton.GetComponent<RectTransform>(), new Vector2(0.74f, 0.065f), new Vector2(0.94f, 0.165f));
            Text nextButtonText = nextButton.GetComponentInChildren<Text>();

            popup.SetReferences(
                overlay,
                gameArea,
                panelRoot,
                overlayCanvasGroup,
                panelCanvasGroup,
                contentRoot,
                contentCanvasGroup,
                categoryText,
                titleText,
                summaryText,
                tipCards,
                tipNumberTexts,
                tipTitleTexts,
                tipBodyTexts,
                pageIndicatorText,
                previousButton,
                nextButton,
                nextButtonText,
                CreatePages()
            );

            overlay.SetActive(false);
            root.transform.SetAsLastSibling();
            return popup;
        }

        private static SystemGuidePopup.GuidePage[] CreatePages()
        {
            return new[]
            {
                CreatePage(
                    "기본 목표와 성장",
                    "달리고, 전송하고, 더 강해지세요",
                    "트럭으로 맵을 누비며 주민을 이세계로 전송하고 EXP와 영혼을 모으는 것이 기본 흐름입니다.",
                    "주민 전송", "주민과 트럭이 접촉하면 이세계 전송이 완료됩니다. 획득한 보상은 트럭 근처에 표시됩니다.",
                    "레벨 업", "EXP가 차면 레벨이 오르고, 레벨마다 트럭을 강화할 업그레이드 포인트를 얻습니다.",
                    "장기 성장", "영혼과 획득한 축복은 유지됩니다. 도감에서는 지금까지 전송한 주민 기록을 볼 수 있습니다."
                ),
                CreatePage(
                    "조작법",
                    "트럭을 원하는 방향으로 이끄세요",
                    "키보드와 화면 조이스틱 중 편한 방식을 사용하세요. 입력하는 동안 트럭은 점점 가속합니다.",
                    "이동", "키보드는 W·A·S·D를 사용합니다. 화면에서는 중앙 게임 영역을 누른 채 원하는 방향으로 끌어보세요.",
                    "가속과 관성", "입력을 유지하면 속도가 오릅니다. 손을 놓아도 마지막 진행 방향으로 잠시 관성이 남습니다.",
                    "축복 단축키", "장착한 액티브 축복은 숫자 1·2·3으로 사용합니다. 패시브 축복은 항상 효과를 냅니다."
                ),
                CreatePage(
                    "트럭 증강",
                    "성장 포인트를 속도와 크기에 투자하세요",
                    "왼쪽 패널의 업그레이드 버튼에서 현재 포인트를 원하는 능력치에 사용할 수 있습니다.",
                    "포인트 획득", "레벨이 한 단계 오를 때마다 업그레이드 포인트를 1개 얻습니다.",
                    "속도 업그레이드", "최대 속도가 올라가 더 빠르게 추격할 수 있습니다. 우측 하단 속도계로 현재 속도를 확인하세요.",
                    "크기 업그레이드", "트럭이 커지면 실제 접촉 범위도 함께 넓어집니다. 카메라는 커진 트럭에 맞춰 줌아웃합니다."
                ),
                CreatePage(
                    "지명수배 시스템",
                    "많이 전송할수록 추격이 거세집니다",
                    "주민 전송 횟수가 쌓이면 화면 상단의 지명수배 별이 늘고, 트럭을 뒤쫓는 추격자도 많아집니다.",
                    "수배 단계", "주민 5명을 전송할 때마다 지명수배 레벨이 1단계 오르며, 최대 10단계까지 상승합니다.",
                    "추격자 경계", "수배 레벨이 높아질수록 추격자가 늘어납니다. 체력이 모두 떨어지면 현재 EXP를 잃고 리스폰합니다.",
                    "세계 이동", "지명수배 Lv.5부터 우측 패널의 세계 이동을 사용할 수 있습니다. 이동하면 수배 단계가 초기화됩니다."
                ),
                CreatePage(
                    "환생",
                    "처음으로 돌아가 더 큰 성장을 준비하세요",
                    "요구 레벨에 도달하면 환생할 수 있습니다. 환생은 일부 진행도를 초기화하는 대신 축복과 장기 보너스를 남깁니다.",
                    "환생 조건", "첫 환생은 Lv.10부터 가능합니다. 성장할수록 더 높은 환생 단계가 차례로 열립니다.",
                    "축복 선택", "환생할 때 제시되는 축복 3개 중 하나를 선택합니다. 획득한 축복은 축복 메뉴에서 장착할 수 있습니다.",
                    "다음 성장", "레벨과 트럭 업그레이드는 초기화되지만 영혼과 축복은 유지됩니다. 최고 단계 환생은 보상 배율도 높입니다."
                )
            };
        }

        private static SystemGuidePopup.GuidePage CreatePage(
            string category,
            string title,
            string summary,
            string firstTitle,
            string firstBody,
            string secondTitle,
            string secondBody,
            string thirdTitle,
            string thirdBody)
        {
            return new SystemGuidePopup.GuidePage(
                category,
                title,
                summary,
                new[]
                {
                    new SystemGuidePopup.GuideTip(firstTitle, firstBody),
                    new SystemGuidePopup.GuideTip(secondTitle, secondBody),
                    new SystemGuidePopup.GuideTip(thirdTitle, thirdBody)
                }
            );
        }

        private static void VerifyViewport(SystemGuidePopup popup, RectTransform gameArea, GameConfig config, float screenAspect, string label)
        {
            Rect viewport = CameraController.CalculateViewportRect(
                screenAspect,
                config.Camera.ViewportAspect,
                config.Camera.ViewportHorizontalCenter
            );
            popup.SetViewport(viewport);
            if (gameArea.anchorMin != viewport.min || gameArea.anchorMax != viewport.max ||
                gameArea.offsetMin != Vector2.zero || gameArea.offsetMax != Vector2.zero)
            {
                throw new InvalidOperationException($"{label} 시스템 안내 영역이 카메라 Viewport와 일치하지 않습니다.");
            }
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
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;
            image.color = color;
            return panel;
        }

        private static Text CreateText(string name, Transform parent, Font font, string value, int fontSize, TextAnchor alignment, bool isBold)
        {
            GameObject textObject = CreateUIObject(name, parent);
            Text text = textObject.AddComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = HudColorPalette.DarkInk;
            text.fontStyle = isBold ? FontStyle.Bold : FontStyle.Normal;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(
            string name,
            Transform parent,
            Font font,
            string label,
            int fontSize,
            Color faceColor,
            Color depthColor)
        {
            GameObject buttonObject = CreatePanel(name, parent, faceColor);
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonObject.GetComponent<Image>();
            Text text = CreateText("Label", buttonObject.transform, font, label, fontSize, TextAnchor.MiddleCenter, true);
            StretchWithOffsets(text.rectTransform, 8f, 8f, 4f, 4f);
            CartoonUIStyle.StyleButton(button, faceColor, depthColor, HudColorPalette.DarkInk);
            return button;
        }

        private static void DestroyExisting(Transform target)
        {
            if (target != null)
            {
                Object.DestroyImmediate(target.gameObject);
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            int separator = path.LastIndexOf('/');
            string parent = path.Substring(0, separator);
            string name = path.Substring(separator + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static void CenterWithSize(RectTransform rectTransform, Vector2 size)
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = size;
        }

        private static void Stretch(RectTransform rectTransform)
        {
            SetRect(rectTransform, Vector2.zero, Vector2.one);
        }

        private static void StretchWithOffsets(RectTransform rectTransform, float left, float right, float bottom, float top)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(left, bottom);
            rectTransform.offsetMax = new Vector2(-right, -top);
        }

        private static void SetRect(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}
