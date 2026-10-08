using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
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
        private static bool showDebug;
        private Text alertText;
        private GameObject flightBar;
        private Text distanceText;
        private RectTransform progressFill;
        private GameObject pauseOverlay;
        private float lastRunDistance;
        private bool lastRunRecord;
        private RectTransform flightFill;
        private PortraitStudio portraitStudio;
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
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.escapeKey.wasPressedThisFrame || keyboard.pKey.wasPressedThisFrame))
                SetPaused(pauseOverlay == null);
            if (keyboard != null && keyboard.f3Key.wasPressedThisFrame && debugText != null)
            {
                showDebug = !showDebug;
                debugText.gameObject.SetActive(showDebug);
            }
            UpdateDistance();
            coinText.text = runner.Coins.ToString();
            UpdateDebugText();
            UpdateAlert();
            UpdateFlightBar();
            if (runner.IsRunning || !runner.HasStarted) return;
            FinishRun(runner.HasDelivered);
        }

        private void FinishRun(bool delivered)
        {
            state = ScreenState.Result;
            GameWorld world = GameWorld.Current;
            lastRunDistance = world != null ? Mathf.Max(0f, world.RunnerTrackZ) : 0f;
            lastRunRecord = GameSession.Mode == RunMode.Endless && SaveSystem.RecordEndlessRun(lastRunDistance, runner.Coins);
            if (delivered) SaveSystem.MarkLevelCompleted(GameSession.LevelId);
            int earned = delivered || GameConfig.Instance.keepCoinsOnFail ? runner.Coins : 0;
            SaveSystem.AddCoins(earned);

            string dialogueId = delivered ? DeliveryDialogue(GameSession.LevelId) : null;
            if (dialogueId != null && !SaveSystem.HasSeenDialogue(dialogueId)) PlayDialogue(dialogueId, () => ShowResult(true, earned));
            else ShowResult(delivered, earned);
        }

        /// <summary>The scene played when a story delivery is completed, by level.</summary>
        private static string DeliveryDialogue(string levelId) => levelId switch
        {
            LevelIds.CrystalMaiden => "crystal_maiden_delivery",
            _ => null,
        };

        public void PlayDialogue(string dialogueId, System.Action onFinished)
        {
            DialogueData data = DialogueDatabase.Load(dialogueId);
            if (data == null)
            {
                onFinished?.Invoke();
                return;
            }
            ClearPage();
            GameWorld world = GameWorld.Current;
            if (portraitStudio == null && world != null) portraitStudio = PortraitStudio.Create(world.transform);
            DialogueView view = DialogueView.Show(phone, data, portraitStudio, world != null ? world.Library : null, onFinished);
            page = view.gameObject;
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

            // Same size as the settings button, stacked right above it.
            Button upgrades = CreateButton(page.transform, string.Empty, new Vector2(0.765f, 0.17f), new Vector2(0.955f, 0.28f), Color.white, 40);
            ApplyButtonArt(upgrades, "UI/UpgradesButton");
            upgrades.onClick.AddListener(ShowShop);
            if (SaveSystem.Data.wingsOffered && !SaveSystem.Data.wingsPurchased && SaveSystem.Coins >= GameConfig.Instance.wingsPrice)
                CreateCoinIcon(upgrades.transform, new Vector2(0.62f, 0.62f), new Vector2(0.98f, 0.98f));

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
            CreateCoinIcon(parent, new Vector2(0.66f, 0.948f), new Vector2(0.74f, 0.992f));
            CreateText(parent, SaveSystem.Coins.ToString(), 40, FontStyle.Bold, TextAnchor.MiddleLeft,
                new Vector2(0.75f, 0.945f), new Vector2(0.97f, 0.995f), Gold);
        }

        private void ShowMap()
        {
            state = ScreenState.Map;
            ClearPage();
            screenBackground.gameObject.SetActive(true);
            page = CreatePage("Level Map", Navy);
            CreatePlainText(page.transform, "КАРТА ДОСТАВОК", 58, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Vector2(0.06f, 0.84f), new Vector2(0.94f, 0.94f), Color.white);
            CreatePlainText(page.transform, "Выбери героя, которому нужна доставка", 27, FontStyle.Normal, TextAnchor.MiddleCenter,
                new Vector2(0.08f, 0.77f), new Vector2(0.92f, 0.84f), new Color(0.7f, 0.82f, 0.95f));

            bool completed = SaveSystem.IsLevelCompleted(LevelIds.CrystalMaiden);
            string icon = completed ? "✓" : "❄";
            Color cardColor = completed ? new Color(0.18f, 0.62f, 0.42f) : Violet;
            Button level = CreatePlainButton(page.transform, icon + "\nCRYSTAL MAIDEN\nУровень 1", new Vector2(0.14f, 0.5f), new Vector2(0.86f, 0.72f), cardColor, 36);
            level.onClick.AddListener(() => GameSession.StartRun(RunMode.Story, LevelIds.CrystalMaiden));
            CreatePlainText(page.transform, completed ? "Доставка выполнена" : "Доступно", 28, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Vector2(0.14f, 0.45f), new Vector2(0.86f, 0.5f), completed ? new Color(0.5f, 1f, 0.7f) : Ice);

            // Endless run: unlocked by the first delivery.
            bool endlessOpen = completed;
            float best = SaveSystem.Data.bestDistance;
            Button endless = CreatePlainButton(page.transform, "∞\nБЕСКОНЕЧНЫЙ ЗАБЕГ", new Vector2(0.14f, 0.2f), new Vector2(0.86f, 0.42f),
                endlessOpen ? new Color(0.36f, 0.23f, 0.67f) : new Color(0.2f, 0.2f, 0.26f), 36);
            endless.interactable = endlessOpen;
            endless.onClick.AddListener(() => GameSession.StartRun(RunMode.Endless));
            string endlessNote = !endlessOpen ? "Откроется после уровня 1" : best > 0f ? $"Рекорд: {best:0} м" : "Беги, сколько сможешь";
            CreatePlainText(page.transform, endlessNote, 28, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Vector2(0.14f, 0.15f), new Vector2(0.86f, 0.2f), endlessOpen ? Gold : new Color(0.7f, 0.7f, 0.75f));

            Button back = CreatePlainButton(page.transform, "‹  НАЗАД", new Vector2(0.08f, 0.04f), new Vector2(0.42f, 0.11f), Slate, 28);
            back.onClick.AddListener(ShowMainMenu);
        }

        private void ShowShop()
        {
            state = ScreenState.Menu;
            ClearPage();
            screenBackground.gameObject.SetActive(true);
            page = CreatePage("Upgrades", Navy, true);
            CreateBalancePlate(page.transform);
            GameWorld world = GameWorld.Current;
            Texture icon = null;
            if (world != null)
            {
                if (portraitStudio == null) portraitStudio = PortraitStudio.Create(world.transform);
                icon = portraitStudio.Portrait(new DialogueCharacter { id = "wings", model = "wings" }, world.Library);
                portraitStudio.SetRendering(true);
            }
            new UpgradeShop(page.transform, icon, ShowShop, ShowMainMenu, PlayDialogue).Build();
        }

        private void ShowControls()
        {
            state = ScreenState.Menu;
            ClearPage();
            screenBackground.gameObject.SetActive(true);
            page = CreatePage("Controls", Navy);
            CreatePlainText(page.transform, "НАСТРОЙКИ", 58, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Vector2(0.08f, 0.82f), new Vector2(0.92f, 0.93f), Color.white);
            CreatePlainText(page.transform, "УПРАВЛЕНИЕ", 31, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Vector2(0.15f, 0.73f), new Vector2(0.85f, 0.79f), Ice);

            CreateControlRow(page.transform, "A   или   ←", "Левая полоса", 0.64f);
            CreateControlRow(page.transform, "D   или   →", "Правая полоса", 0.49f);
            CreateControlRow(page.transform, "W   или   ↑", "Прыжок", 0.34f);
            CreatePlainText(page.transform, "НА ТЕЛЕФОНЕ", 27, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Vector2(0.15f, 0.22f), new Vector2(0.85f, 0.27f), Ice);
            CreatePlainText(page.transform, "Свайп влево / вправо — смена полосы\nСвайп вверх — прыжок", 25, FontStyle.Normal, TextAnchor.MiddleCenter,
                new Vector2(0.10f, 0.13f), new Vector2(0.90f, 0.22f), Color.white);

            Button back = CreatePlainButton(page.transform, "‹  НАЗАД", new Vector2(0.08f, 0.045f), new Vector2(0.42f, 0.105f), Slate, 27);
            back.onClick.AddListener(ShowMainMenu);
            Button reset = CreatePlainButton(page.transform, "СБРОСИТЬ ПРОГРЕСС", new Vector2(0.5f, 0.045f), new Vector2(0.92f, 0.105f),
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
            CreatePlainText(keyPlate.transform, keys, 31, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.92f), Color.white);
            CreatePlainText(parent, action, 29, FontStyle.Bold, TextAnchor.MiddleLeft,
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

            Image coinPlate = CreatePanel("Coin Plate", gameplayHud.transform, new Vector2(0.03f, 0.915f), new Vector2(0.4f, 0.975f), 30f);
            CreateCoinIcon(coinPlate.transform, new Vector2(0.09f, 0.16f), new Vector2(0.3f, 0.84f));
            coinText = CreateText(coinPlate.transform, "0", 38, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Vector2(0.3f, 0.12f), new Vector2(0.9f, 0.88f), Gold);
            Image distancePlate = CreatePanel("Distance Plate", gameplayHud.transform, new Vector2(0.6f, 0.915f), new Vector2(0.97f, 0.975f), 30f);
            distanceText = CreateText(distancePlate.transform, "0 м", 30, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Vector2(0.1f, 0.12f), new Vector2(0.9f, 0.88f), Parchment);
            if (GameSession.Mode == RunMode.Story)
            {
                // A gold progress bar under the distance plate, filling toward the recipient.
                Image progressBack = CreateImage("Delivery Progress", gameplayHud.transform, new Color(0.18f, 0.1f, 0.04f, 0.9f));
                SetAnchors(progressBack.rectTransform, new Vector2(0.63f, 0.902f), new Vector2(0.94f, 0.912f));
                progressBack.gameObject.AddComponent<Outline>().effectColor = new Color(0.1f, 0.05f, 0f, 0.9f);
                Image progress = CreateImage("Delivery Progress Fill", progressBack.transform, Gold);
                progressFill = progress.rectTransform;
                SetAnchors(progressFill, Vector2.zero, new Vector2(0f, 1f));
            }
            Button pause = CreateButton(gameplayHud.transform, string.Empty, new Vector2(0.435f, 0.912f), new Vector2(0.565f, 0.985f), Color.white, 28);
            ApplyButtonArt(pause, "UI/PauseButton");
            pause.GetComponent<Image>().preserveAspect = true;
            pause.onClick.AddListener(() => SetPaused(true));

            Image barBack = CreatePanel("Flight Bar", gameplayHud.transform, new Vector2(0.03f, 0.865f), new Vector2(0.4f, 0.91f), 22f);
            Image fill = CreateImage("Flight Fill", barBack.transform, new Color(0.5f, 0.85f, 1f, 0.9f));
            flightFill = fill.rectTransform;
            SetAnchors(flightFill, new Vector2(0.06f, 0.25f), new Vector2(0.94f, 0.75f));
            CreateText(barBack.transform, "КРЫЛЬЯ", 22, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0.06f, 0.15f), new Vector2(0.94f, 0.85f),
                Parchment);
            flightBar = barBack.gameObject;
            flightBar.SetActive(false);

            alertText = CreateText(gameplayHud.transform, string.Empty, 64, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Vector2(0.05f, 0.62f), new Vector2(0.95f, 0.74f), new Color(1f, 0.55f, 0.25f, 0f));
            alertText.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.6f);
            // Tester overlay: dev builds only and hidden until F3 is pressed.
            if (Debug.isDebugBuild)
            {
                debugText = CreateText(gameplayHud.transform, string.Empty, 20, FontStyle.Normal, TextAnchor.UpperLeft,
                    new Vector2(0.04f, 0.79f), new Vector2(0.96f, 0.86f), new Color(1f, 1f, 1f, 0.8f));
                debugText.gameObject.SetActive(showDebug);
            }

            runner.BeginRun();
        }

        /// <summary>Endless: meters run. Story: meters left to the recipient plus a progress bar.</summary>
        private void UpdateDistance()
        {
            GameWorld world = GameWorld.Current;
            if (distanceText == null || world == null) return;
            float distance = Mathf.Max(0f, world.RunnerTrackZ);
            if (GameSession.Mode == RunMode.Endless)
            {
                distanceText.text = $"{distance:0} м";
                return;
            }
            float goal = world.Segments.FinishZ;
            distanceText.text = $"до цели {Mathf.Max(0f, goal - distance):0} м";
            if (progressFill != null) progressFill.anchorMax = new Vector2(Mathf.Clamp01(distance / goal), 1f);
        }

        private void SetPaused(bool paused)
        {
            if (paused == (pauseOverlay != null) || state != ScreenState.Playing) return;
            Time.timeScale = paused ? 0f : 1f;
            if (!paused)
            {
                Destroy(pauseOverlay);
                pauseOverlay = null;
                return;
            }
            Image overlay = CreateImage("Pause", gameplayHud.transform, new Color(0.02f, 0.03f, 0.07f, 0.8f));
            Stretch(overlay.rectTransform);
            pauseOverlay = overlay.gameObject;
            CreateText(overlay.transform, "ПАУЗА", 72, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Vector2(0.1f, 0.6f), new Vector2(0.9f, 0.72f), Color.white);
            Button resume = CreateButton(overlay.transform, "▶  ПРОДОЛЖИТЬ", new Vector2(0.15f, 0.45f), new Vector2(0.85f, 0.54f), Ice, 34);
            resume.onClick.AddListener(() => SetPaused(false));
            Button menu = CreateButton(overlay.transform, "В МЕНЮ", new Vector2(0.15f, 0.33f), new Vector2(0.85f, 0.41f), Slate, 30);
            menu.onClick.AddListener(() =>
            {
                // Leaving a run counts like losing it: the coins are kept if the config says so.
                if (GameConfig.Instance.keepCoinsOnFail) SaveSystem.AddCoins(runner.Coins);
                Time.timeScale = 1f;
                UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
            });
        }

        /// <summary>Browser tab hidden or window unfocused: pause instead of running blind.</summary>
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && state == ScreenState.Playing && runner != null && runner.IsRunning) SetPaused(true);
        }

        private void UpdateFlightBar()
        {
            if (flightBar == null) return;
            bool flying = runner.IsFlying && runner.FlightDuration > 0f;
            if (flightBar.activeSelf != flying) flightBar.SetActive(flying);
            if (flying) flightFill.anchorMax = new Vector2(Mathf.Clamp01(runner.FlightRemaining / runner.FlightDuration), 1f);
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
            bool endless = GameSession.Mode == RunMode.Endless;
            page = CreatePage(won ? "Victory" : "Defeat", new Color(0.025f, 0.04f, 0.08f, 0.92f));
            string title = endless ? "ЗАБЕГ\nОКОНЧЕН" : won ? "ДОСТАВКА\nВЫПОЛНЕНА!" : "ДОСТАВКА\nСОРВАНА";
            CreateText(page.transform, title, 65, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Vector2(0.08f, 0.62f), new Vector2(0.92f, 0.8f), won || endless ? new Color(0.5f, 1f, 0.72f) : new Color(1f, 0.47f, 0.38f));
            if (endless)
            {
                CreateText(page.transform, $"Дистанция: {lastRunDistance:0} м    Рекорд: {SaveSystem.Data.bestDistance:0} м",
                    30, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0.06f, 0.565f), new Vector2(0.94f, 0.61f), Color.white);
                if (lastRunRecord)
                    CreateText(page.transform, "★ НОВЫЙ РЕКОРД! ★", 40, FontStyle.Bold, TextAnchor.MiddleCenter,
                        new Vector2(0.06f, 0.8f), new Vector2(0.94f, 0.86f), Gold);
            }
            CreateText(page.transform, $"Собрано золота: {runner.Coins}\nВ копилку: +{earned}   (всего {SaveSystem.Coins})",
                30, FontStyle.Normal, TextAnchor.MiddleCenter, new Vector2(0.08f, 0.45f), new Vector2(0.92f, 0.56f), Color.white);

            if (SaveSystem.Data.wingsOffered && !SaveSystem.Data.wingsPurchased)
            {
                int price = GameConfig.Instance.wingsPrice;
                string hint = SaveSystem.Coins >= price
                    ? "Хватает на крылья! Загляни в «Улучшения»."
                    : $"До крыльев осталось {price - SaveSystem.Coins} ●";
                CreateText(page.transform, hint, 26, FontStyle.Bold, TextAnchor.MiddleCenter,
                    new Vector2(0.08f, 0.41f), new Vector2(0.92f, 0.45f), new Color(0.7f, 0.9f, 1f));
            }

            Button retry = CreateButton(page.transform, won ? "↻  ПРОЙТИ ЕЩЁ РАЗ" : "↻  ПОПРОБОВАТЬ ЕЩЁ", new Vector2(0.13f, 0.31f), new Vector2(0.87f, 0.40f), Ice, 30);
            retry.onClick.AddListener(() => GameSession.StartRun(GameSession.Mode, GameSession.LevelId));
            Button map = CreateButton(page.transform, endless ? "В МЕНЮ" : "К КАРТЕ", new Vector2(0.13f, 0.20f), new Vector2(0.87f, 0.28f), new Color(0.18f, 0.22f, 0.34f), 30);
            map.onClick.AddListener(endless ? ShowMainMenu : ShowMap);
        }

        /// <summary>A full-screen page; with <paramref name="menuArt"/> the main menu art shows darkened behind it.</summary>
        private GameObject CreatePage(string name, Color color, bool menuArt = false)
        {
            Image image = CreateImage(name, phone, color);
            Stretch(image.rectTransform);
            if (menuArt)
            {
                image.color = Color.black;
                Image art = CreateTextureImage("Page Art", image.transform, "UI/MainMenuBackground");
                if (art != null)
                {
                    Stretch(art.rectTransform);
                    art.color = new Color(0.42f, 0.42f, 0.48f, 1f);
                }
                Image shade = CreateImage("Page Shade", image.transform, new Color(0.03f, 0.03f, 0.08f, 0.45f));
                Stretch(shade.rectTransform);
            }
            return image.gameObject;
        }

        private void ClearPage()
        {
            if (portraitStudio != null) portraitStudio.SetRendering(false);
            if (page != null) Destroy(page);
            if (gameplayHud != null) Destroy(gameplayHud);
            page = null;
            gameplayHud = null;
        }
    }
}
