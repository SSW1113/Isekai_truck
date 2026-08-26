using System;
using IsekaiTruck.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace IsekaiTruck.Editor
{
    internal static class CartoonUIStyle
    {
        private const string CartoonFontPath = "Assets/IsekaiTruck/Fonts/CartoonHUD.ttf";

        public static Font LoadFont()
        {
            Font font = AssetDatabase.LoadAssetAtPath<Font>(CartoonFontPath);
            if (font == null)
            {
                throw new InvalidOperationException($"Cartoon HUD font was not found: {CartoonFontPath}");
            }

            return font;
        }

        public static void StylePanel(GameObject panel, Color faceColor, Color depthColor, bool addDepth = true)
        {
            Image image = panel.GetComponent<Image>();
            if (image == null)
            {
                image = panel.AddComponent<Image>();
            }

            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;
            image.color = faceColor;

            if (!addDepth)
            {
                return;
            }

            Outline outline = panel.GetComponent<Outline>();
            if (outline == null)
            {
                outline = panel.AddComponent<Outline>();
            }
            outline.effectColor = new Color(depthColor.r, depthColor.g, depthColor.b, 0.22f);
            outline.effectDistance = new Vector2(1f, -1f);

            Shadow shadow = FindShadow(panel);
            if (shadow == null)
            {
                shadow = panel.AddComponent<Shadow>();
            }
            shadow.effectColor = new Color(depthColor.r, depthColor.g, depthColor.b, 0.16f);
            shadow.effectDistance = new Vector2(0f, -3f);
        }

        public static void StyleScrim(GameObject panel, float alpha = 0.64f)
        {
            Image image = panel.GetComponent<Image>();
            image.color = new Color(HudColorPalette.DarkInk.r, HudColorPalette.DarkInk.g, HudColorPalette.DarkInk.b, alpha);
            image.sprite = null;
            image.type = Image.Type.Simple;
        }

        public static void StyleButton(
            Button button,
            Color faceColor,
            Color depthColor,
            Color textColor,
            bool animatePosition = true)
        {
            StylePanel(button.gameObject, faceColor, depthColor);
            button.targetGraphic = button.GetComponent<Image>();

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.04f, 1.04f, 1.04f, 1f);
            colors.pressedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.62f, 0.59f, 0.60f, 0.68f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            Text[] labels = button.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                labels[i].font = LoadFont();
                labels[i].fontStyle = FontStyle.Bold;
                labels[i].color = textColor;
                labels[i].alignment = TextAnchor.MiddleCenter;
                labels[i].resizeTextForBestFit = true;
                labels[i].resizeTextMinSize = 12;
                labels[i].resizeTextMaxSize = Mathf.Max(labels[i].fontSize, 18);
            }

            CartoonButtonPressEffect pressEffect = button.GetComponent<CartoonButtonPressEffect>();
            if (pressEffect == null)
            {
                pressEffect = button.gameObject.AddComponent<CartoonButtonPressEffect>();
            }
            pressEffect.Configure(
                (RectTransform)button.transform,
                null,
                1.025f,
                0.975f,
                0.7f,
                animateTargetPosition: animatePosition
            );
        }

        public static void StyleText(Text text, Color color, bool isBold = false)
        {
            text.font = LoadFont();
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontStyle = isBold ? FontStyle.Bold : FontStyle.Normal;
        }

        private static Shadow FindShadow(GameObject target)
        {
            Shadow[] shadows = target.GetComponents<Shadow>();
            for (int i = 0; i < shadows.Length; i++)
            {
                if (shadows[i].GetType() == typeof(Shadow))
                {
                    return shadows[i];
                }
            }

            return null;
        }
    }
}
