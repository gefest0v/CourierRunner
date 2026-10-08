using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using static CourierRunner.UIFactory;

namespace CourierRunner
{
    /// <summary>Main menu, level map, settings, in-run HUD and result screens.</summary>
    public sealed class GameFlowUI : MonoBehaviour
    {
        public RunnerController runner;

        private enum ScreenState { Menu, Map, Playing, Result }
        private ScreenState state;
        private RectTransform phone;
        private Image screenBackground;
        private GameObject page;
        private GameObject gameplayHud;
        private Text coinText;
        private Text debugText;
        private Text alertText;
        private float alertUntil;

        private void Start()
        {
            BuildCanvas();
            GameWorld world = GameWorld.Current;
            if (world != null && world.Treants != null) world.Treants.Warning += text => ShowAlert(text, 2.2f);
            if (world != null && world.AutoStart) StartGameplay();
            else ShowMainMenu();
        }

        private void Update()
        {
            if (state != ScreenState.Playing || runner == null) return;
            coinText.text = $"ЗОЛОТО  {runner.Coins}";
            UpdateDebugText();
            UpdateAlert();
            if (runner.IsRunning || !runner.HasStarted) return;
            FinishRun(runner.HasDelivered);
        }

        private void FinishRun(bool delivered)
        {
            if (delivered) SaveSystem.MarkLevelCompleted(GameSession.LevelId);
            int earned = delivered || GameConfig.Instance.keepCoinsOnFail ? runner.Coins : 0;
            SaveSystem.AddCoins(earned);
            ShowResult(delivered, earned);
        }

        private void BuildCanvas()
        {
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                GameObject eventObject = new("UI EventSystem");
                eventObject.AddComponent<EventSystem>();
                eventObject.AddComponent<InputSystemUIInputModule>();
            }

            GameObject canvasObject = new("Game UI");
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            screenBackground = CreateImage("Screen Background", canvasObject.transform, new Color(0.008f, 0.012f, 0.025f, 1f));
            Stretch(screenBackground.rectTransform);

            GameObject phoneObject = new("9x16 Safe Area", typeof(RectTransform), typeof(AspectRatioFitter));
            phoneObject.transform.SetParent(canvasObject.transform, false);
            phone = phoneObject.GetComponent<RectTransform>();
            Stretch(phone);
            AspectRatioFitter fitter = phoneObject.GetComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 9f / 16f;
        }

        private void ShowMainMenu()
        {
            state = ScreenState.Menu;
            ClearPage();
            screenBackground.gameObject.SetActive(true);
            page = CreatePage("Main Menu", Color.black);

            Image background = CreateTextureImage("Menu Background", page.transform, "UI/MainMenuBackground");
            if (background != null) Stretch(background.rectTransform);

            Image floatingTitle = CreateTextureImage("Floating Title", page.transform, "UI/MainMenuTitle");
            if (floatingTitle != null)
            {
                floatingTitle.preserveAspect = true;
                SetAnchors(floatingTitle.rectTransform, new Vector2(0.01f, 0.665f), new Vector2(0.99f, 0.94f));
                floatingTitle.gameObject.AddComponent<MenuArtFloat>();
            }

            CreateBalancePlate(page.transform);

            Button play = CreateButton(page.transform, "▶   ИГРАТЬ", new Vector2(0.045f, 0.045f), new Vector2(0.72f, 0.155f), Ice, 42);
            ApplyButtonArt(play, "UI/PlayButton");
            play.onClick.AddListener(ShowMap);
            // Normalized X and Y ranges are different in a 9:16 canvas. A wider X span is
            // required for the settings control to be physically square and match Play's height.
            Button settings = CreateButton(page.transform, string.Empty, new Vector2(0.765f, 0.045f), new Vector2(0.955f, 0.155f), Color.white, 40);
            ApplyButtonArt(settings, "UI/SettingsButton");
            settings.onClick.AddListener(ShowControls);
        }

        private void CreateBalancePlate(Transform parent)
        {
            Image plate = CreateImage("Balance Plate", parent, new Color(0.03f, 0.05f, 0.1f, 0.78f));
            SetAnchors(plate.rectTransform, new Vector2(0.56f, 0.955f), new Vector2(0.97f, 0.99f));
            CreateText(plate.transform, $"● {SaveSystem.Coins}", 30, FontStyle.Bold, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, Gold);
        }

        private void ShowMap()
        {
            state = ScreenState.Map;
            ClearPage();
            screenBackground.gameObject.SetActive(true);
            page = CreatePage("Level Map", Navy);
            CreateBalancePlate(page.transform);
            CreateText(page.transform, "КАРТА ДОСТАВОК", 58, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Vector2(0.06f, 0.84f), new Vector2(0.94f, 0.94f), Color.white);
            CreateText(page.transform, "Выбери героя, которому нужна доставка", 27, FontStyle.Normal, TextAnchor.MiddleCenter,
                new Vector2(0.08f, 0.77f), new Vector2(0.92f, 0.84f), new Color(0.7f, 0.82f, 0.95f));

            bool completed = SaveSystem.IsLevelCompleted(LevelIds.CrystalMaiden);
            string icon = completed ? "✓" : "❄";
            Color cardColor = completed ? new Color(0.18f, 0.62f, 0.42f) : Violet;
            Button level = CreateButton(page.transform, icon + "\nCRYSTAL MAIDEN\nУровень 1", new Vector2(0.18f, 0.42f), new Vector2(0.82f, 0.68f), cardColor, 36);
            level.onClick.AddListener(() => GameSession.StartRun(RunMode.Story, LevelIds.CrystalMaiden));
            CreateText(page.transform, completed ? "Доставка выполнена" : "Доступно", 28, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Vector2(0.2f, 0.35f), new Vector2(0.8f, 0.41f), completed ? new Color(0.5f, 1f, 0.7f) : Ice);

            Button back = CreateButton(page.transform, "‹  НАЗАД", new Vector2(0.08f, 0.07f), new Vector2(0.42f, 0.14f), Slate, 28);
            back.onClick.AddListener(ShowMainMenu);
        }

        private void ShowControls()
        {
            state = ScreenState.Menu;
            ClearPage();
            screenBackground.gameObject.SetActive(true);
            page = CreatePage("Controls", Navy);
            CreateText(page.transform, "НАСТРОЙКИ", 58, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Vector2(0.08f, 0.82f), new Vector2(0.92f, 0.93f), Color.white);
            CreateText(page.transform, "УПРАВЛЕНИЕ", 31, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Vector2(0.15f, 0.73f), new Vector2(0.85f, 0.79f), Ice);

            CreateControlRow(page.transform, "A   или   ←", "Левая полоса", 0.64f);
            CreateControlRow(page.transform, "D   или   →", "Правая полоса", 0.49f);
            CreateControlRow(page.transform, "W   или   ↑", "Прыжок", 0.34f);
            CreateText(page.transform, "НА ТЕЛЕФОНЕ", 27, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Vector2(0.15f, 0.22f), new Vector2(0.85f, 0.27f), Ice);
            CreateText(page.transform, "Свайп влево / вправо — смена полосы\nСвайп вверх — прыжок", 25, FontStyle.Normal, TextAnchor.MiddleCenter,
                new Vector2(0.10f, 0.13f), new Vector2(0.90f, 0.22f), Color.white);

            Button back = CreateButton(page.transform, "‹  НАЗАД", new Vector2(0.08f, 0.045f), new Vector2(0.42f, 0.105f), Slate, 27);
            back.onClick.AddListener(ShowMainMenu);
            Button reset = CreateButton(page.transform, "СБРОСИТЬ ПРОГРЕСС", new Vector2(0.5f, 0.045f), new Vector2(0.92f, 0.105f),
                new Color(0.55f, 0.16f, 0.16f), 24);
            bool armed = false;
            reset.onClick.AddListener(() =>
            {
                if (!armed)
                {
                    armed = true;
                    reset.GetComponentInChildren<Text>().text = "ТОЧНО? ЖМИ ЕЩЁ";
                    return;
                }
                SaveSystem.ResetProgress();
                ShowMainMenu();
            });
        }

        private static void CreateControlRow(Transform parent, string keys, string action, float bottom)
        {
            Image keyPlate = CreateImage(keys + " Keys", parent, new Color(0.12f, 0.16f, 0.25f));
            SetAnchors(keyPlate.rectTransform, new Vector2(0.12f, bottom), new Vector2(0.48f, bottom + 0.105f));
            CreateText(keyPlate.transform, keys, 31, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.92f), Color.white);
            CreateText(parent, action, 29, FontStyle.Bold, TextAnchor.MiddleLeft,
                new Vector2(0.54f, bottom), new Vector2(0.91f, bottom + 0.105f), new Color(0.75f, 0.88f, 1f));
        }

        private void StartGameplay()
        {
            state = ScreenState.Playing;
            ClearPage();
            screenBackground.gameObject.SetActive(false);
            gameplayHud = new GameObject("Gameplay HUD", typeof(RectTransform));
            gameplayHud.transform.SetParent(phone, false);
            Stretch(gameplayHud.GetComponent<RectTransform>());

            Image coinPlate = CreateImage("Coin Plate", gameplayHud.transform, new Color(0.03f, 0.05f, 0.1f, 0.72f));
            SetAnchors(coinPlate.rectTransform, new Vector2(0.04f, 0.91f), new Vector2(0.40f, 0.97f));
            coinText = CreateText(coinPlate.transform, "ЗОЛОТО  0", 28, FontStyle.Bold, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Gold);
            alertText = CreateText(gameplayHud.transform, string.Empty, 64, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Vector2(0.05f, 0.62f), new Vector2(0.95f, 0.74f), new Color(1f, 0.55f, 0.25f, 0f));
            alertText.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.6f);
            if (Debug.isDebugBuild)
                debugText = CreateText(gameplayHud.transform, string.Empty, 20, FontStyle.Normal, TextAnchor.UpperLeft,
                    new Vector2(0.04f, 0.80f), new Vector2(0.96f, 0.90f), new Color(1f, 1f, 1f, 0.75f));

            runner.BeginRun();
        }

        /// <summary>Big centered warning for events such as the treant charge.</summary>
        public void ShowAlert(string text, float duration)
        {
            if (alertText == null) return;
            alertText.text = text;
            alertUntil = Time.time + duration;
        }

        private void UpdateAlert()
        {
            if (alertText == null) return;
            float remaining = alertUntil - Time.time;
            // Pulses while visible, then fades out over the last half second.
            float alpha = Mathf.Clamp01(remaining / 0.5f) * (0.75f + 0.25f * Mathf.Sin(Time.time * 14f));
            Color color = alertText.color;
            color.a = Mathf.Max(0f, alpha);
            alertText.color = color;
        }

        /// <summary>Tester overlay (editor and development builds only).</summary>
        private void UpdateDebugText()
        {
            GameWorld world = GameWorld.Current;
            if (debugText == null || world == null) return;
            float distance = world.RunnerTrackZ;
            debugText.text = $"{distance:0} м   скорость {runner.forwardSpeed:0.0}   сложность {world.Difficulty.DifficultyAt(distance):0.0}\n" +
                             $"паттерн: {world.Generator.LastPattern ?? "—"}";
        }

        private void ShowResult(bool won, int earned)
        {
            state = ScreenState.Result;
            ClearPage();
            screenBackground.gameObject.SetActive(false);
            page = CreatePage(won ? "Victory" : "Defeat", new Color(0.025f, 0.04f, 0.08f, 0.92f));
            CreateText(page.transform, won ? "ДОСТАВКА\nВЫПОЛНЕНА!" : "ДОСТАВКА\nСОРВАНА", 65, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Vector2(0.08f, 0.58f), new Vector2(0.92f, 0.78f), won ? new Color(0.5f, 1f, 0.72f) : new Color(1f, 0.47f, 0.38f));
            CreateText(page.transform, $"Собрано золота: {runner.Coins}\nВ копилку: +{earned}   (всего {SaveSystem.Coins})",
                30, FontStyle.Normal, TextAnchor.MiddleCenter, new Vector2(0.08f, 0.45f), new Vector2(0.92f, 0.56f), Color.white);

            Button retry = CreateButton(page.transform, won ? "↻  ПРОЙТИ ЕЩЁ РАЗ" : "↻  ПОПРОБОВАТЬ ЕЩЁ", new Vector2(0.13f, 0.31f), new Vector2(0.87f, 0.40f), Ice, 30);
            retry.onClick.AddListener(() => GameSession.StartRun(GameSession.Mode, GameSession.LevelId));
            Button map = CreateButton(page.transform, "К КАРТЕ", new Vector2(0.13f, 0.20f), new Vector2(0.87f, 0.28f), new Color(0.18f, 0.22f, 0.34f), 30);
            map.onClick.AddListener(ShowMap);
        }

        private GameObject CreatePage(string name, Color color)
        {
            Image image = CreateImage(name, phone, color);
            Stretch(image.rectTransform);
            return image.gameObject;
        }

        private void ClearPage()
        {
            if (page != null) Destroy(page);
            if (gameplayHud != null) Destroy(gameplayHud);
            page = null;
            gameplayHud = null;
        }
    }
}
