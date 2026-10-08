using System;
using UnityEngine;
using UnityEngine.UI;
using static CourierRunner.UIFactory;

namespace CourierRunner
{
    /// <summary>
    /// "Улучшения" screen (GAME_DESIGN.md §11): the wings card with its lock / buy / owned states,
    /// a purchase confirmation, and the flight duration upgrade once the wings are owned.
    /// </summary>
    public sealed class UpgradeShop
    {
        public const string WingsBoughtDialogue = "crystal_maiden_wings_bought";

        private readonly Transform page;
        private readonly Texture wingsIcon;
        private readonly Action refresh;
        private readonly Action back;
        private readonly Action<string, Action> playDialogue;

        public UpgradeShop(Transform page, Texture wingsIcon, Action refresh, Action back, Action<string, Action> playDialogue)
        {
            this.page = page;
            this.wingsIcon = wingsIcon;
            this.refresh = refresh;
            this.back = back;
            this.playDialogue = playDialogue;
        }

        public void Build()
        {
            GameConfig config = GameConfig.Instance;
            SaveData save = SaveSystem.Data;

            CreateText(page, "УЛУЧШЕНИЯ", 58, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Vector2(0.06f, 0.86f), new Vector2(0.94f, 0.94f), Color.white);

            Image card = CreateImage("Wings Card", page, new Color(0.1f, 0.14f, 0.27f, 0.97f));
            SetAnchors(card.rectTransform, new Vector2(0.06f, 0.43f), new Vector2(0.94f, 0.84f));
            card.gameObject.AddComponent<Outline>().effectColor = new Color(0.45f, 0.75f, 1f, 0.45f);

            var icon = new GameObject("Wings Icon", typeof(RectTransform), typeof(RawImage));
            icon.transform.SetParent(card.transform, false);
            SetAnchors(icon.GetComponent<RectTransform>(), new Vector2(0.18f, 0.45f), new Vector2(0.82f, 0.98f));
            RawImage iconImage = icon.GetComponent<RawImage>();
            iconImage.texture = wingsIcon;
            iconImage.raycastTarget = false;
            iconImage.enabled = wingsIcon != null;

            CreateText(card.transform, "КРЫЛЬЯ", 44, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Vector2(0.05f, 0.37f), new Vector2(0.95f, 0.47f), new Color(0.7f, 0.9f, 1f));
            float seconds = WingsPickup.FlightDuration;
            CreateText(card.transform,
                $"Поднимают курьера выше башен на {seconds:0.#} с. Наземные препятствия не страшны, в воздухе — своя дорожка монет.",
                26, FontStyle.Normal, TextAnchor.MiddleCenter, new Vector2(0.06f, 0.2f), new Vector2(0.94f, 0.37f),
                new Color(0.82f, 0.88f, 0.98f));

            if (!save.wingsOffered)
            {
                // Hidden until Crystal Maiden offers them.
                iconImage.color = new Color(0.15f, 0.18f, 0.28f, 1f);
                CreateText(card.transform, "ЗАКРЫТО — пройдите первый уровень", 28, FontStyle.Bold, TextAnchor.MiddleCenter,
                    new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.17f), new Color(1f, 0.6f, 0.45f));
            }
            else if (save.wingsPurchased)
            {
                CreateText(card.transform, "КУПЛЕНО ✓  появляются на уровнях", 28, FontStyle.Bold, TextAnchor.MiddleCenter,
                    new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.17f), new Color(0.5f, 1f, 0.7f));
            }
            else
            {
                int price = config.wingsPrice;
                bool affordable = SaveSystem.Coins >= price;
                string label = affordable ? $"КУПИТЬ ЗА {price} ●" : $"НУЖНО ЕЩЁ {price - SaveSystem.Coins} ●";
                Button buy = CreateButton(card.transform, label, new Vector2(0.12f, 0.04f), new Vector2(0.88f, 0.17f),
                    affordable ? new Color(0.95f, 0.66f, 0.12f) : Slate, 30);
                buy.interactable = affordable;
                buy.onClick.AddListener(() => Confirm($"Купить крылья за {price} монет?", () => BuyWings(price)));
            }

            if (save.wingsPurchased) BuildFlightUpgrade(config, save);

            Button backButton = CreateButton(page, "‹  НАЗАД", new Vector2(0.08f, 0.05f), new Vector2(0.42f, 0.12f), Slate, 28);
            backButton.onClick.AddListener(() => back());

            if (Debug.isDebugBuild)
            {
                Button cheat = CreateButton(page, "+100 ● (тест)", new Vector2(0.58f, 0.05f), new Vector2(0.92f, 0.12f),
                    new Color(0.3f, 0.3f, 0.3f, 0.9f), 24);
                cheat.onClick.AddListener(() =>
                {
                    SaveSystem.AddCoins(100);
                    refresh();
                });
            }
        }

        private void BuildFlightUpgrade(GameConfig config, SaveData save)
        {
            Image card = CreateImage("Flight Upgrade Card", page, new Color(0.1f, 0.14f, 0.27f, 0.97f));
            SetAnchors(card.rectTransform, new Vector2(0.06f, 0.16f), new Vector2(0.94f, 0.4f));
            int level = save.flightLevel;
            int max = config.flightUpgradeMaxLevel;
            CreateText(card.transform, $"ДЛИТЕЛЬНОСТЬ ПОЛЁТА   {level}/{max}", 34, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Vector2(0.05f, 0.68f), new Vector2(0.95f, 0.95f), new Color(0.7f, 0.9f, 1f));
            CreateText(card.transform, $"+{config.flightUpgradeSeconds:0.#} с к полёту за каждый уровень", 26, FontStyle.Normal,
                TextAnchor.MiddleCenter, new Vector2(0.05f, 0.45f), new Vector2(0.95f, 0.68f), new Color(0.82f, 0.88f, 0.98f));

            if (level >= max)
            {
                CreateText(card.transform, "МАКСИМУМ ✓", 30, FontStyle.Bold, TextAnchor.MiddleCenter,
                    new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.4f), new Color(0.5f, 1f, 0.7f));
                return;
            }
            int cost = config.flightUpgradeBaseCost << level;
            bool affordable = SaveSystem.Coins >= cost;
            Button upgrade = CreateButton(card.transform, affordable ? $"УЛУЧШИТЬ ЗА {cost} ●" : $"НУЖНО ЕЩЁ {cost - SaveSystem.Coins} ●",
                new Vector2(0.12f, 0.08f), new Vector2(0.88f, 0.4f), affordable ? new Color(0.95f, 0.66f, 0.12f) : Slate, 28);
            upgrade.interactable = affordable;
            upgrade.onClick.AddListener(() => Confirm($"Улучшить полёт за {cost} монет?", () =>
            {
                if (!SaveSystem.TrySpendCoins(cost)) return;
                SaveSystem.Data.flightLevel++;
                SaveSystem.Save();
                refresh();
            }));
        }

        private void BuyWings(int price)
        {
            if (!SaveSystem.TrySpendCoins(price)) return;
            SaveSystem.Data.wingsPurchased = true;
            SaveSystem.Save();
            playDialogue(WingsBoughtDialogue, refresh);
        }

        /// <summary>Modal yes/no confirmation over the shop.</summary>
        private void Confirm(string question, Action onYes)
        {
            Image overlay = CreateImage("Confirm", page, new Color(0f, 0f, 0f, 0.7f));
            Stretch(overlay.rectTransform);
            Image box = CreateImage("Confirm Box", overlay.transform, new Color(0.08f, 0.11f, 0.22f, 1f));
            SetAnchors(box.rectTransform, new Vector2(0.08f, 0.38f), new Vector2(0.92f, 0.62f));
            box.gameObject.AddComponent<Outline>().effectColor = new Color(0.95f, 0.7f, 0.2f, 0.6f);
            CreateText(box.transform, question, 36, FontStyle.Bold, TextAnchor.MiddleCenter,
                new Vector2(0.06f, 0.48f), new Vector2(0.94f, 0.92f), Color.white);
            Button yes = CreateButton(box.transform, "ДА", new Vector2(0.08f, 0.1f), new Vector2(0.46f, 0.4f), new Color(0.2f, 0.65f, 0.4f), 32);
            Button no = CreateButton(box.transform, "НЕТ", new Vector2(0.54f, 0.1f), new Vector2(0.92f, 0.4f), Slate, 32);
            yes.onClick.AddListener(() =>
            {
                UnityEngine.Object.Destroy(overlay.gameObject);
                onYes();
            });
            no.onClick.AddListener(() => UnityEngine.Object.Destroy(overlay.gameObject));
        }
    }
}
