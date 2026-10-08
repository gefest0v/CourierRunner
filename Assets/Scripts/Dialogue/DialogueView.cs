using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static CourierRunner.UIFactory;

namespace CourierRunner
{
    /// <summary>
    /// Visual-novel dialogue screen (GAME_DESIGN.md §9): dimmed level behind, large portraits, name plate,
    /// typewriter text. A tap finishes the current line instantly, the next tap advances.
    /// </summary>
    public sealed class DialogueView : MonoBehaviour, IPointerClickHandler
    {
        private const float CharactersPerSecond = 42f;

        private DialogueData dialogue;
        private Action onFinished;
        private PortraitStudio studio;
        private ModelLibrary library;

        private RawImage leftPortrait;
        private RawImage rightPortrait;
        private Image namePlate;
        private Text nameText;
        private Text bodyText;
        private Text continueMark;
        private GameObject rewardBadge;
        private Text rewardText;

        private int lineIndex = -1;
        private string fullText = string.Empty;
        private float typedCharacters;
        private bool lineComplete;
        private RectTransform activePortrait;
        private string activeEmotion;
        private float emotionTime;

        public static DialogueView Show(RectTransform parent, DialogueData data, PortraitStudio studio,
            ModelLibrary library, Action onFinished)
        {
            var root = new GameObject("Dialogue", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);
            Stretch(root.GetComponent<RectTransform>());
            DialogueView view = root.AddComponent<DialogueView>();
            view.dialogue = data;
            view.studio = studio;
            view.library = library;
            view.onFinished = onFinished;
            view.Build();
            view.studio.SetRendering(true);
            view.Next();
            return view;
        }

        private void Build()
        {
            // Dim the frozen level so the characters read clearly.
            Image dim = GetComponent<Image>();
            dim.color = new Color(0.02f, 0.03f, 0.07f, 0.72f);

            leftPortrait = CreatePortrait("Left Portrait", new Vector2(-0.08f, 0.26f), new Vector2(0.62f, 0.9f));
            rightPortrait = CreatePortrait("Right Portrait", new Vector2(0.38f, 0.26f), new Vector2(1.08f, 0.9f));

            Image panel = CreatePanel("Text Panel", transform, new Vector2(0.02f, 0.03f), new Vector2(0.98f, 0.3f), 70f);
            bodyText = CreateText(panel.transform, string.Empty, 36, FontStyle.Normal, TextAnchor.UpperLeft,
                new Vector2(0.1f, 0.2f), new Vector2(0.9f, 0.84f), Parchment);
            bodyText.resizeTextForBestFit = false;
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyText.verticalOverflow = VerticalWrapMode.Overflow;
            continueMark = CreateText(panel.transform, "▼", 30, FontStyle.Bold, TextAnchor.MiddleRight,
                new Vector2(0.78f, 0.06f), new Vector2(0.9f, 0.22f), Gold);

            namePlate = CreatePanel("Name Plate", transform, Vector2.zero, Vector2.one, 30f);
            nameText = CreateText(namePlate.transform, string.Empty, 32, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Vector2(0.1f, 0.12f), new Vector2(0.9f, 0.88f), Color.white);

            rewardBadge = CreateImage("Reward Badge", transform, new Color(0.95f, 0.72f, 0.12f, 0.96f)).gameObject;
            SetAnchors((RectTransform)rewardBadge.transform, new Vector2(0.1f, 0.9f), new Vector2(0.9f, 0.96f));
            rewardText = CreateText(rewardBadge.transform, string.Empty, 32, FontStyle.Bold, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Color(0.15f, 0.08f, 0.02f));
            rewardBadge.SetActive(false);

            Button skip = CreateButton(transform, "ПРОПУСТИТЬ ›", new Vector2(0.66f, 0.965f), new Vector2(0.97f, 0.995f),
                new Color(0.12f, 0.15f, 0.25f, 0.85f), 22);
            skip.onClick.AddListener(Finish);
        }

        private RawImage CreatePortrait(string portraitName, Vector2 min, Vector2 max)
        {
            var portraitObject = new GameObject(portraitName, typeof(RectTransform), typeof(RawImage));
            portraitObject.transform.SetParent(transform, false);
            SetAnchors(portraitObject.GetComponent<RectTransform>(), min, max);
            RawImage image = portraitObject.GetComponent<RawImage>();
            image.raycastTarget = false;
            image.gameObject.SetActive(false);
            return image;
        }

        public void OnPointerClick(PointerEventData eventData) => Advance();

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame))
                Advance();

            if (!lineComplete)
            {
                typedCharacters += CharactersPerSecond * Time.unscaledDeltaTime;
                int count = Mathf.Min(fullText.Length, Mathf.FloorToInt(typedCharacters));
                bodyText.text = fullText.Substring(0, count);
                if (count >= fullText.Length) lineComplete = true;
            }
            continueMark.enabled = lineComplete && Mathf.Repeat(Time.unscaledTime, 0.9f) < 0.6f;
            AnimateEmotion();
        }

        private void Advance()
        {
            if (!lineComplete)
            {
                // First tap reveals the whole line.
                typedCharacters = fullText.Length;
                bodyText.text = fullText;
                lineComplete = true;
                return;
            }
            Next();
        }

        private void Next()
        {
            lineIndex++;
            if (lineIndex >= dialogue.lines.Length)
            {
                Finish();
                return;
            }
            DialogueLine line = dialogue.lines[lineIndex];
            DialogueCharacter character = DialogueDatabase.Character(line.speaker);
            bool system = line.speaker == DialogueDatabase.SystemSpeaker;
            bool right = line.side == "right";

            fullText = (line.text ?? string.Empty).Replace("{wings_price}", GameConfig.Instance.wingsPrice.ToString());
            typedCharacters = 0f;
            lineComplete = false;
            bodyText.text = string.Empty;
            bodyText.fontStyle = system ? FontStyle.Italic : FontStyle.Normal;
            bodyText.color = system ? Gold : Parchment;

            namePlate.gameObject.SetActive(!system && !string.IsNullOrEmpty(character.name));
            nameText.text = character.name;
            nameText.color = character.NameColor;
            SetAnchors(namePlate.rectTransform, right ? new Vector2(0.52f, 0.285f) : new Vector2(0.04f, 0.285f),
                right ? new Vector2(0.96f, 0.35f) : new Vector2(0.48f, 0.35f));

            if (!system)
            {
                RawImage portrait = right ? rightPortrait : leftPortrait;
                Texture texture = LoadPortrait(character);
                portrait.texture = texture;
                portrait.gameObject.SetActive(texture != null);
                activePortrait = texture != null ? portrait.rectTransform : null;
                activeEmotion = line.emotion;
                emotionTime = 0f;
            }
            // The speaker is bright, the listener is dimmed, as in visual novels.
            Highlight(leftPortrait, !system && !right);
            Highlight(rightPortrait, !system && right);

            rewardBadge.SetActive(!string.IsNullOrEmpty(line.reward));
            if (rewardBadge.activeSelf) rewardText.text = "★ " + DialogueActions.RewardTitle(line.reward) + " ★";
            DialogueActions.Run(line.action);
        }

        private Texture LoadPortrait(DialogueCharacter character)
        {
            if (!string.IsNullOrEmpty(character.portrait))
            {
                Texture2D art = Resources.Load<Texture2D>(character.portrait);
                if (art != null) return art;
            }
            return studio != null ? studio.Portrait(character, library) : null;
        }

        private static void Highlight(RawImage portrait, bool speaking)
        {
            portrait.color = speaking ? Color.white : new Color(0.45f, 0.48f, 0.58f, 1f);
            if (speaking) return;
            RectTransform rect = portrait.rectTransform;
            rect.localScale = Vector3.one * 0.94f;
            rect.localRotation = Quaternion.identity;
            rect.anchoredPosition = Vector2.zero;
        }

        /// <summary>Small motion per emotion on the speaking portrait.</summary>
        private void AnimateEmotion()
        {
            if (activePortrait == null) return;
            emotionTime += Time.unscaledDeltaTime;
            float t = emotionTime;
            float scale = 1f;
            float angle = 0f;
            Vector2 shift = Vector2.zero;
            switch (activeEmotion)
            {
                case "happy":
                    shift.y = Mathf.Abs(Mathf.Sin(t * 9f)) * 14f * Mathf.Clamp01(1f - t * 1.5f);
                    break;
                case "surprised":
                    scale = 1f + 0.06f * Mathf.Clamp01(1f - t * 2f);
                    shift.x = Mathf.Sin(t * 40f) * 6f * Mathf.Clamp01(1f - t * 2.5f);
                    break;
                case "sly":
                    angle = Mathf.Lerp(0f, 4f, Mathf.Clamp01(t * 3f));
                    break;
                case "worried":
                    shift.x = Mathf.Sin(t * 18f) * 3f * Mathf.Clamp01(1.2f - t);
                    break;
            }
            activePortrait.localScale = Vector3.one * scale;
            activePortrait.localRotation = Quaternion.Euler(0f, 0f, angle);
            activePortrait.anchoredPosition = shift;
        }

        private void Finish()
        {
            if (dialogue != null) SaveSystem.MarkDialogueSeen(dialogue.id);
            // A skipped dialogue still grants what its lines would have.
            for (int i = Mathf.Max(lineIndex + 1, 0); dialogue != null && i < dialogue.lines.Length; i++)
                DialogueActions.Run(dialogue.lines[i].action);
            dialogue = null;
            if (studio != null) studio.SetRendering(false);
            Action callback = onFinished;
            onFinished = null;
            Destroy(gameObject);
            callback?.Invoke();
        }
    }
}
