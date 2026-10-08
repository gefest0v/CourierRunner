using System;
using System.Collections.Generic;
using UnityEngine;

namespace CourierRunner
{
    /// <summary>One line of a dialogue. Stored in Resources/Dialogues/&lt;id&gt;.json.</summary>
    [Serializable]
    public sealed class DialogueLine
    {
        /// <summary>Character id from characters.json, or "system" for a narrator/system message.</summary>
        public string speaker;
        /// <summary>neutral, happy, surprised, sly or worried. Drives a small portrait animation.</summary>
        public string emotion = "neutral";
        /// <summary>"left" or "right": where the speaker's portrait stands.</summary>
        public string side = "left";
        public string text;
        /// <summary>Optional reward shown as a badge while this line is on screen, e.g. "wings".</summary>
        public string reward;
        /// <summary>Optional action run when the line is shown, e.g. "offer_wings". See <see cref="DialogueActions"/>.</summary>
        public string action;
    }

    [Serializable]
    public sealed class DialogueData
    {
        public string id;
        public DialogueLine[] lines;
    }

    [Serializable]
    public sealed class DialogueCharacter
    {
        public string id;
        public string name;
        /// <summary>Name plate color as #RRGGBB.</summary>
        public string color = "#FFFFFF";
        /// <summary>Optional 2D portrait (Resources path to a texture). Takes priority over <see cref="model"/>.</summary>
        public string portrait;
        /// <summary>3D model rendered live as a portrait when no 2D art exists: "crystal_maiden" or "courier".</summary>
        public string model;

        public Color NameColor => ColorUtility.TryParseHtmlString(color, out Color parsed) ? parsed : Color.white;
    }

    [Serializable]
    internal sealed class CharacterList
    {
        public DialogueCharacter[] characters;
    }

    /// <summary>Loads dialogues and characters from JSON in Resources/Dialogues.</summary>
    public static class DialogueDatabase
    {
        public const string SystemSpeaker = "system";

        private static Dictionary<string, DialogueCharacter> characters;

        public static DialogueData Load(string dialogueId)
        {
            TextAsset asset = Resources.Load<TextAsset>("Dialogues/" + dialogueId);
            if (asset == null)
            {
                Debug.LogWarning($"Dialogue '{dialogueId}' not found in Resources/Dialogues.");
                return null;
            }
            DialogueData data = JsonUtility.FromJson<DialogueData>(asset.text);
            if (data?.lines == null || data.lines.Length == 0) return null;
            data.id ??= dialogueId;
            return data;
        }

        public static DialogueCharacter Character(string id)
        {
            if (characters == null)
            {
                characters = new Dictionary<string, DialogueCharacter>();
                TextAsset asset = Resources.Load<TextAsset>("Dialogues/characters");
                CharacterList list = asset != null ? JsonUtility.FromJson<CharacterList>(asset.text) : null;
                if (list?.characters != null)
                    foreach (DialogueCharacter character in list.characters)
                        characters[character.id] = character;
            }
            if (id != null && characters.TryGetValue(id, out DialogueCharacter found)) return found;
            return new DialogueCharacter { id = id, name = id == SystemSpeaker ? "" : id };
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => characters = null;
    }
}
