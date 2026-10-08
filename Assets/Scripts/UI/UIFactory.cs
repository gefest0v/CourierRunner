using UnityEngine;
using UnityEngine.UI;

namespace CourierRunner
{
    /// <summary>Helpers for building the code-driven uGUI screens. Positions use normalized anchors.</summary>
    public static class UIFactory
    {
        public static readonly Color Navy = new(0.035f, 0.055f, 0.11f, 0.98f);
        public static readonly Color Ice = new(0.25f, 0.72f, 0.95f, 1f);
        public static readonly Color Violet = new(0.36f, 0.23f, 0.67f, 1f);
        public static readonly Color Gold = new(1f, 0.82f, 0.18f, 1f);
        public static readonly Color Slate = new(0.16f, 0.2f, 0.3f, 1f);

        private static Font font;

        public static Font Font
        {
            get
            {
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        public static Button CreateButton(Transform parent, string label, Vector2 min, Vector2 max, Color color, int size = 38)
        {
            Image image = CreateImage(label + " Button", parent, color);
            SetAnchors(image.rectTransform, min, max);
            Button button = image.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.18f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.18f);
            colors.disabledColor = Color.Lerp(color, Color.gray, 0.6f);
            button.colors = colors;
            CreateText(image.transform, label, size, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.94f), Color.white);
            return button;
        }

        public static void ApplyButtonArt(Button button, string resourcePath)
        {
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null) return;

            Image image = button.GetComponent<Image>();
            image.sprite = CreateSprite(texture);
            image.type = Image.Type.Simple;
            image.color = Color.white;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 0.9f, 1f);
            colors.pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.75f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            if (button.GetComponent<MenuButtonMotion>() == null)
                button.gameObject.AddComponent<MenuButtonMotion>();
        }

        public static Text CreateText(Transform parent, string value, int size, FontStyle style, TextAnchor alignment,
            Vector2 min, Vector2 max, Color color)
        {
            GameObject textObject = new(value + " Text", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            SetAnchors(textObject.GetComponent<RectTransform>(), min, max);
            Text text = textObject.GetComponent<Text>();
            text.font = Font;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 16;
            text.resizeTextMaxSize = size;
            text.raycastTarget = false;
            return text;
        }

        public static Image CreateImage(string name, Transform parent, Color color)
        {
            GameObject imageObject = new(name, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        public static Image CreateTextureImage(string name, Transform parent, string resourcePath)
        {
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null) return null;
            Image image = CreateImage(name, parent, Color.white);
            image.sprite = CreateSprite(texture);
            image.type = Image.Type.Simple;
            return image;
        }

        public static Sprite CreateSprite(Texture2D texture) =>
            Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
