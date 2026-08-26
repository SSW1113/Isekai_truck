using System;
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
    public static class StoryIntroFeatureSetup
    {
        private const string ScenePath = "Assets/IsekaiTruck/Scenes/Main.unity";
        private const string PrefabFolder = "Assets/IsekaiTruck/Prefabs/UI";
        private const string PrefabPath = PrefabFolder + "/StoryIntro.prefab";
        private const string PanelFolder = "Assets/IsekaiTruck/Art/StoryIntro/Panels";

        private static readonly string[] PanelPaths =
        {
            PanelFolder + "/panel01.png",
            PanelFolder + "/panel02.png",
            PanelFolder + "/panel03.png",
            PanelFolder + "/panel04.png",
            PanelFolder + "/panel05.png",
            PanelFolder + "/panel06.png"
        };

        private static readonly Vector2[] PanelPositions =
        {
            new Vector2(-466f, 233f),
            new Vector2(0f, 233f),
            new Vector2(466f, 233f),
            new Vector2(-466f, -233f),
            new Vector2(0f, -233f),
            new Vector2(466f, -233f)
        };

        [MenuItem("Isekai Truck/Setup Story Intro")]
        public static void Setup()
        {
            ConfigurePanelImporters();
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
            Canvas canvas = GameObject.Find("Game Canvas")?.GetComponent<Canvas>();
            if (gameManager == null || canvas == null)
            {
                throw new InvalidOperationException("스토리 인트로 생성에 필요한 Main 씬 참조를 찾지 못했습니다.");
            }

            EnsureFolder(PrefabFolder);
            DestroyExisting(canvas.transform.Find("Story Intro UI"));
            StoryIntroController introController = CreateUI(canvas.transform, CartoonUIStyle.LoadFont());
            GameObject sceneRoot = introController.gameObject;
            PrefabUtility.SaveAsPrefabAssetAndConnect(
                sceneRoot,
                PrefabPath,
                InteractionMode.AutomatedAction
            );
            introController = sceneRoot.GetComponent<StoryIntroController>();
            gameManager.SetStoryIntroController(introController);

            EditorUtility.SetDirty(introController);
            EditorUtility.SetDirty(gameManager);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Verify();

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("Isekai Truck", "게임 시작 스토리 인트로를 Main 씬에 연결했습니다.", "확인");
            }
        }

        public static void Verify()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
            StoryIntroController introController = Object.FindFirstObjectByType<StoryIntroController>(FindObjectsInactive.Include);
            StoryIntroController prefab = AssetDatabase.LoadAssetAtPath<StoryIntroController>(PrefabPath);
            if (gameManager == null || introController == null || prefab == null || !PrefabUtility.IsPartOfPrefabInstance(introController))
            {
                throw new InvalidOperationException("스토리 인트로 프리팹 또는 Main 씬 인스턴스가 없습니다.");
            }

            SerializedObject serializedGameManager = new SerializedObject(gameManager);
            SerializedObject serializedIntro = new SerializedObject(introController);
            GameObject overlay = (GameObject)serializedIntro.FindProperty("overlay").objectReferenceValue;
            RectTransform comicRoot = (RectTransform)serializedIntro.FindProperty("comicRoot").objectReferenceValue;
            Image impactFlash = (Image)serializedIntro.FindProperty("impactFlash").objectReferenceValue;
            Button inputButton = (Button)serializedIntro.FindProperty("inputButton").objectReferenceValue;
            Text promptText = (Text)serializedIntro.FindProperty("promptText").objectReferenceValue;
            SerializedProperty panels = serializedIntro.FindProperty("panels");

            if (serializedGameManager.FindProperty("storyIntroController").objectReferenceValue != introController ||
                overlay == null || overlay.activeSelf || comicRoot == null || impactFlash == null ||
                inputButton == null || promptText == null || panels.arraySize != PanelPaths.Length ||
                introController.transform.parent == null || introController.transform.parent.name != "Game Canvas")
            {
                throw new InvalidOperationException("스토리 인트로의 씬 연결 또는 기본 상태가 올바르지 않습니다.");
            }

            for (int i = 0; i < panels.arraySize; i++)
            {
                SerializedProperty panel = panels.GetArrayElementAtIndex(i);
                RectTransform panelRoot = (RectTransform)panel.FindPropertyRelative("panelRoot").objectReferenceValue;
                CanvasGroup canvasGroup = (CanvasGroup)panel.FindPropertyRelative("canvasGroup").objectReferenceValue;
                int entrance = panel.FindPropertyRelative("entrance").enumValueIndex;
                Sprite expectedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PanelPaths[i]);
                Image panelImage = panelRoot != null ? panelRoot.GetComponent<Image>() : null;
                int expectedEntrance = i == 1 || i == 4
                    ? (int)StoryIntroController.PanelEntrance.Impact
                    : i == 2
                        ? (int)StoryIntroController.PanelEntrance.Fade
                        : (int)StoryIntroController.PanelEntrance.Slide;

                if (panelRoot == null || canvasGroup == null || panelImage == null || panelImage.sprite != expectedSprite ||
                    entrance != expectedEntrance || panelRoot.anchoredPosition != PanelPositions[i])
                {
                    throw new InvalidOperationException($"스토리 인트로 {i + 1}번 패널 설정이 올바르지 않습니다.");
                }
            }

            if (impactFlash.raycastTarget || impactFlash.color.a > 0f || inputButton.transition != Selectable.Transition.None)
            {
                throw new InvalidOperationException("스토리 인트로 입력 또는 충격 플래시 설정이 올바르지 않습니다.");
            }

            Debug.Log("Story intro feature verification passed.");
        }

        private static StoryIntroController CreateUI(Transform canvas, Font font)
        {
            GameObject root = CreateUIObject("Story Intro UI", canvas);
            Stretch(root.GetComponent<RectTransform>());
            StoryIntroController introController = root.AddComponent<StoryIntroController>();

            GameObject overlay = CreatePanel("Story Intro Overlay", root.transform, new Color(0.025f, 0.018f, 0.03f, 0.985f));
            Stretch(overlay.GetComponent<RectTransform>());
            CanvasGroup overlayCanvasGroup = overlay.AddComponent<CanvasGroup>();

            GameObject comicObject = CreateUIObject("Comic Page", overlay.transform);
            RectTransform comicRoot = comicObject.GetComponent<RectTransform>();
            CenterWithSize(comicRoot, new Vector2(1382f, 916f));
            comicRoot.anchoredPosition = new Vector2(0f, 18f);
            ResponsivePanelFitter responsiveFitter = comicObject.AddComponent<ResponsivePanelFitter>();
            responsiveFitter.Configure(comicRoot.sizeDelta, 34f, 82f);

            StoryIntroController.PanelEntrance[] entrances =
            {
                StoryIntroController.PanelEntrance.Slide,
                StoryIntroController.PanelEntrance.Impact,
                StoryIntroController.PanelEntrance.Fade,
                StoryIntroController.PanelEntrance.Slide,
                StoryIntroController.PanelEntrance.Impact,
                StoryIntroController.PanelEntrance.Slide
            };
            Vector2[] startOffsets =
            {
                new Vector2(-620f, 0f),
                Vector2.zero,
                Vector2.zero,
                new Vector2(0f, -620f),
                Vector2.zero,
                new Vector2(620f, 0f)
            };
            float[] durations = { 0.28f, 0.22f, 0.26f, 0.28f, 0.24f, 0.28f };
            StoryIntroController.StoryPanel[] storyPanels = new StoryIntroController.StoryPanel[PanelPaths.Length];
            for (int i = 0; i < storyPanels.Length; i++)
            {
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PanelPaths[i]);
                if (sprite == null)
                {
                    throw new InvalidOperationException($"스토리 인트로 이미지를 찾지 못했습니다: {PanelPaths[i]}");
                }

                GameObject panelObject = CreatePanel($"Story Panel {i + 1}", comicRoot, Color.white);
                RectTransform panelRoot = panelObject.GetComponent<RectTransform>();
                CenterWithSize(panelRoot, new Vector2(450f, 450f));
                panelRoot.anchoredPosition = PanelPositions[i];
                Image panelImage = panelObject.GetComponent<Image>();
                panelImage.sprite = sprite;
                panelImage.type = Image.Type.Simple;
                panelImage.preserveAspect = true;
                panelImage.raycastTarget = false;
                CanvasGroup panelCanvasGroup = panelObject.AddComponent<CanvasGroup>();
                storyPanels[i] = new StoryIntroController.StoryPanel(
                    panelRoot,
                    panelCanvasGroup,
                    entrances[i],
                    startOffsets[i],
                    durations[i]
                );
            }

            GameObject flashObject = CreatePanel("Impact Flash", overlay.transform, new Color(1f, 0.77f, 0.38f, 0f));
            Stretch(flashObject.GetComponent<RectTransform>());
            Image impactFlash = flashObject.GetComponent<Image>();
            impactFlash.sprite = null;
            impactFlash.type = Image.Type.Simple;
            impactFlash.raycastTarget = false;

            GameObject promptBackground = CreatePanel("Story Prompt Background", overlay.transform, new Color(0f, 0f, 0f, 0.74f));
            RectTransform promptBackgroundRect = promptBackground.GetComponent<RectTransform>();
            promptBackgroundRect.anchorMin = new Vector2(0.5f, 0f);
            promptBackgroundRect.anchorMax = new Vector2(0.5f, 0f);
            promptBackgroundRect.pivot = new Vector2(0.5f, 0.5f);
            promptBackgroundRect.anchoredPosition = new Vector2(0f, 30f);
            promptBackgroundRect.sizeDelta = new Vector2(720f, 44f);
            promptBackground.GetComponent<Image>().raycastTarget = false;
            Text promptText = CreateText("Story Prompt", promptBackground.transform, font, "클릭하여 이야기를 시작하세요", 20);
            StretchWithOffsets(promptText.rectTransform, 18f, 18f, 4f, 4f);

            GameObject inputObject = CreatePanel("Story Input Surface", overlay.transform, Color.clear);
            Stretch(inputObject.GetComponent<RectTransform>());
            Image inputImage = inputObject.GetComponent<Image>();
            inputImage.sprite = null;
            inputImage.type = Image.Type.Simple;
            Button inputButton = inputObject.AddComponent<Button>();
            inputButton.transition = Selectable.Transition.None;
            inputButton.targetGraphic = inputImage;

            introController.SetReferences(
                overlay,
                overlayCanvasGroup,
                comicRoot,
                impactFlash,
                inputButton,
                promptText,
                storyPanels
            );

            overlay.SetActive(false);
            root.transform.SetAsLastSibling();
            return introController;
        }

        private static void ConfigurePanelImporters()
        {
            AssetDatabase.Refresh();
            for (int i = 0; i < PanelPaths.Length; i++)
            {
                TextureImporter importer = AssetImporter.GetAtPath(PanelPaths[i]) as TextureImporter;
                if (importer == null)
                {
                    throw new InvalidOperationException($"스토리 인트로 이미지 임포터를 찾지 못했습니다: {PanelPaths[i]}");
                }

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = false;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.maxTextureSize = 2048;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.spritePixelsPerUnit = 100f;
                importer.SaveAndReimport();
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
            image.color = color;
            return panel;
        }

        private static Text CreateText(string name, Transform parent, Font font, string value, int fontSize)
        {
            GameObject textObject = CreateUIObject(name, parent);
            Text text = textObject.AddComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
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
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private static void StretchWithOffsets(RectTransform rectTransform, float left, float right, float bottom, float top)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(left, bottom);
            rectTransform.offsetMax = new Vector2(-right, -top);
        }
    }
}
