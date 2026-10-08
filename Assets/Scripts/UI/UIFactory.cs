using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CourierRunner
{
    /// <summary>
    /// Helpers for building the code-driven uGUI screens in the game's art style (gold stone frames,
    /// wood planks, vines). Positions use normalized anchors inside the 9:16 phone frame (1080x1920).
    /// </summary>
    public static class UIFactory
    {
        public static readonly Color Navy = new(0.035f, 0.055f, 0.11f, 0.98f);
        public static readonly Color Ice = new(0.25f, 0.72f, 0.95f, 1f);
        public static readonly Color Violet = new(0.36f, 0.23f, 0.67f, 1f);
        public static readonly Color Gold = new(1f, 0.84f, 0.3f, 1f);
        public static readonly Color Slate = new(0.16f, 0.2f, 0.3f, 1f);
        /// <summary>Warm off-white used for text on wood.</summary>
        public static readonly Color Parchment = new(1f, 0.95f, 0.82f, 1f);

        private const float ReferenceWidth = 1080f;
        private const float ReferenceHeight = 1920f;
        /// <summary>Border of the 512 px panel sprite that must not stretch (frame and corner vines).</summary>
        private const float PanelBorderPixels = 112f;

        private static Font font;
        private static readonly Dictionary<string, Sprite> sprites = new();

        public static Font Font
        {
            get
            {
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        /// <summary>
        /// A button in the menu style: wide buttons use the wooden sign plank, compact ones the framed panel.
        /// <paramref name="color"/> only tints the wood slightly, so buttons stay distinguishable.
        /// </summary>
        public static Button CreateButton(Transform parent, string label, Vector2 min, Vector2 max, Color color, int size = 38)
        {
            Image image = CreateImage(label + " Button", parent, Color.white);
            SetAnchors(image.rectTransform, min, max);
            float aspect = (max.x - min.x) * ReferenceWidth / Mathf.Max(1f, (max.y - min.y) * ReferenceHeight);
            Sprite art = aspect >= 3.2f ? LoadSprite("UI/PlayButton", 0f) : LoadSprite("UI/Panel", PanelBorderPixels);
            Color tint = Color.Lerp(Color.white, new Color(color.r, color.g, color.b, 1f), 0.3f);
            if (art != null)
            {
                image.sprite = art;
                image.type = art.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
                image.pixelsPerUnitMultiplier = PanelBorderPixels / BorderSizeFor(min, max);
                image.color = tint;
            }
            else
            {
                image.color = color;
            }
            Button button = image.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 0.88f, 1f);
            colors.pressedColor = new Color(0.75f, 0.72f, 0.68f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.45f, 0.45f, 0.48f, 0.85f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            Vector2 inset = aspect >= 3.2f ? new Vector2(0.16f, 0.2f) : new Vector2(0.1f, 0.14f);
            CreateText(image.transform, label, size, FontStyle.Bold, TextAnchor.MiddleCenter,
                inset, Vector2.one - inset, Parchment);
            if (button.GetComponent<MenuButtonMotion>() == null) button.gameObject.AddComponent<MenuButtonMotion>();
            return button;
        }

        /// <summary>Replaces a button's art with a full image (no slicing), e.g. the illustrated Play button.</summary>
        public static void ApplyButtonArt(Button button, string resourcePath)
        {
            Sprite art = LoadSprite(resourcePath, 0f);
            if (art == null) return;
            Image image = button.GetComponent<Image>();
            image.sprite = art;
            image.type = Image.Type.Simple;
            image.color = Color.white;
        }

        /// <summary>A framed wooden panel (9-sliced) for plates, cards and dialogs.</summary>
        public static Image CreatePanel(string name, Transform parent, Vector2 min, Vector2 max, float borderSize = 0f)
        {
            Image image = CreateImage(name, parent, Color.white);
            SetAnchors(image.rectTransform, min, max);
            Sprite panel = LoadSprite("UI/Panel", PanelBorderPixels);
            if (panel == null)
            {
                image.color = new Color(0.12f, 0.08f, 0.05f, 0.92f);
                return image;
            }
            image.sprite = panel;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = PanelBorderPixels / (borderSize > 0f ? borderSize : BorderSizeFor(min, max));
            image.raycastTarget = false;
            return image;
        }

        /// <summary>Frame thickness that suits the element: about a third of its shorter side, capped.</summary>
        private static float BorderSizeFor(Vector2 min, Vector2 max)
        {
            float shortSide = Mathf.Min((max.x - min.x) * ReferenceWidth, (max.y - min.y) * ReferenceHeight);
            return Mathf.Clamp(shortSide * 0.32f, 18f, 90f);
        }

        /// <summary>The gold coin icon, kept square inside the given area.</summary>
        public static Image CreateCoinIcon(Transform parent, Vector2 min, Vector2 max)
        {
            Image image = CreateImage("Coin Icon", parent, Color.white);
            SetAnchors(image.rectTransform, min, max);
            Sprite coin = LoadSprite("UI/CoinIcon", 0f);
            if (coin != null) image.sprite = coin;
            else image.color = Gold;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
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
            // A dark outline keeps text readable on wood, art and the 3D scene alike.
            Outline outline = textObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.12f, 0.06f, 0.02f, 0.85f);
            outline.effectDistance = new Vector2(2f, -2f);
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
            Sprite sprite = LoadSprite(resourcePath, 0f);
            if (sprite == null) return null;
            Image image = CreateImage(name, parent, Color.white);
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            return image;
        }

        /// <summary>Loads a texture from Resources as a sprite, optionally with a 9-slice border (in pixels).</summary>
        public static Sprite LoadSprite(string resourcePath, float border)
        {
            string key = resourcePath + "#" + border;
            if (sprites.TryGetValue(key, out Sprite cached) && cached != null) return cached;
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null) return null;
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect, Vector4.one * border);
            sprites[key] = sprite;
            return sprite;
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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => sprites.Clear();
    }
}
