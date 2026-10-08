using System;
using System.Collections.Generic;
using UnityEngine;

namespace CourierRunner
{
    [Serializable]
    public sealed class SaveData
    {
        public int version = 1;
        public int coins;
        public List<string> completedLevels = new();
        public List<string> seenDialogues = new();
        public bool wingsOffered;
        public bool wingsPurchased;
        /// <summary>Purchased flight-duration upgrades.</summary>
        public int flightLevel;
        public float bestDistance;
        public int bestRunCoins;
    }

    /// <summary>
    /// Persistent profile stored as JSON in PlayerPrefs (IndexedDB in a WebGL build).
    /// Every change is written immediately so closing the browser tab never loses progress.
    /// </summary>
    public static class SaveSystem
    {
        private const string SaveKey = "CourierRunner.Save";
        private const string LegacyCompletionKey = "CourierRunner.CrystalMaiden.Completed";

        private static SaveData data;

        public static event Action Changed;

        public static SaveData Data
        {
            get
            {
                if (data == null) Load();
                return data;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            // Needed when "Enter Play Mode Options" skips the domain reload.
            data = null;
            Changed = null;
        }

        public static void Load()
        {
            data = null;
            string json = PlayerPrefs.GetString(SaveKey, string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                try { data = JsonUtility.FromJson<SaveData>(json); }
                catch (ArgumentException exception) { Debug.LogWarning($"Save data is corrupt, starting fresh: {exception.Message}"); }
            }
            data ??= new SaveData();
            data.completedLevels ??= new List<string>();
            data.seenDialogues ??= new List<string>();

            if (PlayerPrefs.GetInt(LegacyCompletionKey, 0) == 1)
            {
                if (!data.completedLevels.Contains(LevelIds.CrystalMaiden)) data.completedLevels.Add(LevelIds.CrystalMaiden);
                PlayerPrefs.DeleteKey(LegacyCompletionKey);
                Save();
            }
        }

        public static void Save()
        {
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(Data));
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        public static int Coins => Data.coins;

        public static void AddCoins(int amount)
        {
            if (amount <= 0) return;
            Data.coins += amount;
            Save();
        }

        public static bool TrySpendCoins(int amount)
        {
            if (amount < 0 || Data.coins < amount) return false;
            Data.coins -= amount;
            Save();
            return true;
        }

        public static bool IsLevelCompleted(string levelId) => Data.completedLevels.Contains(levelId);

        public static void MarkLevelCompleted(string levelId)
        {
            if (IsLevelCompleted(levelId)) return;
            Data.completedLevels.Add(levelId);
            Save();
        }

        public static bool HasSeenDialogue(string dialogueId) => Data.seenDialogues.Contains(dialogueId);

        public static void MarkDialogueSeen(string dialogueId)
        {
            if (HasSeenDialogue(dialogueId)) return;
            Data.seenDialogues.Add(dialogueId);
            Save();
        }

        /// <summary>Stores an endless run's result. Returns true if the distance is a new record.</summary>
        public static bool RecordEndlessRun(float distance, int coins)
        {
            bool record = distance > Data.bestDistance;
            if (record) Data.bestDistance = distance;
            if (coins > Data.bestRunCoins) Data.bestRunCoins = coins;
            Save();
            return record;
        }

        public static void ResetProgress()
        {
            data = new SaveData();
            Save();
        }
    }
}
