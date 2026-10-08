using UnityEngine;

namespace CourierRunner
{
    /// <summary>
    /// Side effects a dialogue line can trigger through its "action" field.
    /// Add new cases here when a story beat needs to change the save or unlock something.
    /// </summary>
    public static class DialogueActions
    {
        public const string OfferWings = "offer_wings";

        public static void Run(string action)
        {
            if (string.IsNullOrEmpty(action)) return;
            switch (action)
            {
                case OfferWings:
                    if (!SaveSystem.Data.wingsOffered)
                    {
                        SaveSystem.Data.wingsOffered = true;
                        SaveSystem.Save();
                    }
                    break;
                default:
                    Debug.LogWarning($"Unknown dialogue action '{action}'.");
                    break;
            }
        }

        /// <summary>Text for the reward badge shown next to a line.</summary>
        public static string RewardTitle(string reward) => reward switch
        {
            "wings" => "НОВАЯ НАГРАДА: КРЫЛЬЯ",
            _ => "НОВАЯ НАГРАДА",
        };
    }
}
