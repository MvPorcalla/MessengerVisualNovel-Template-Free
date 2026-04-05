// ════════════════════════════════════════════════════════════════════════
// Assets/Scripts/BubbleSpinner/Data/ConversationAsset.cs
// ════════════════════════════════════════════════════════════════════════

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace BubbleSpinner.Data
{
    /// <summary>
    /// Relationship status options for character profiles.
    /// </summary>
    public enum RelationshipStatus
    {
        Unknown,
        Single,
        InARelationship,
        Married,
        Divorced,
        Widowed,
        ItsComplicated
    }

    /// <summary>
    /// ScriptableObject that defines a character and their associated conversation data.
    /// Holds profile info, chapter list, CG keys, and an auto-generated unique ID.
    /// </summary>
    [CreateAssetMenu(fileName = "NewConversation", menuName = "BubbleSpinner/Conversation Asset")]
    public class ConversationAsset : ScriptableObject
    {
        // ═══════════════════════════════════════════════════════════
        // REQUIRED FIELDS
        // ═══════════════════════════════════════════════════════════

        [Header("Required")]
        [Tooltip("Character's display name. Must not be empty.")]
        public string characterName;

        [Tooltip("Profile image shown in contact list and chat header.")]
        public AssetReference profileImage;

        [Header("Dialogue Chapters")]
        [Tooltip("First entry is always the entry point. All others are reached via <<jump>>.")]
        public List<ChapterEntry> chapters = new List<ChapterEntry>();

        // ═══════════════════════════════════════════════════════════
        // UNIQUE IDENTIFIER
        // ═══════════════════════════════════════════════════════════

        [Header("Unique Identifier")]
        [Tooltip("Auto-generated unique ID. DO NOT MODIFY.")]
        [SerializeField] private string conversationId;

        // ═══════════════════════════════════════════════════════════
        // CHAPTER ENTRY
        // ═══════════════════════════════════════════════════════════

        [System.Serializable]
        public class ChapterEntry
        {
            [Tooltip("Auto-read from the first title: in the .bub file. Must match <<jump>> targets exactly.")]
            public string chapterId;

            [Tooltip("The .bub dialogue file for this chapter.")]
            public TextAsset file;
        }

        // ═══════════════════════════════════════════════════════════
        // OPTIONAL - BASIC PROFILE
        // ═══════════════════════════════════════════════════════════

        [Header("Optional - Basic Profile")]
        [Tooltip("Leave empty to display N/A")]
        public string characterAge = "N/A";

        [Tooltip("Leave empty to display N/A")]
        public string birthdate = "N/A";

        [Tooltip("Leave Unknown to display N/A")]
        public RelationshipStatus relationshipStatus = RelationshipStatus.Unknown;

        [Tooltip("Leave empty to display N/A")]
        public string occupation = "N/A";

        // ═══════════════════════════════════════════════════════════
        // OPTIONAL - TEXT PROFILE
        // ═══════════════════════════════════════════════════════════

        [Header("Optional - Text Profile")]
        [Tooltip("Short tagline shown in contact list preview. Leave empty to display N/A")]
        [TextArea(2, 4)]
        public string bio = "N/A";

        [Tooltip("Longer story background for detail panel. Leave empty to display N/A")]
        [TextArea(4, 8)]
        public string description = "N/A";

        [Tooltip("e.g. Introverted, caring, easily flustered. Leave empty to display N/A")]
        [TextArea(2, 4)]
        public string personalityTraits = "N/A";

        // ═══════════════════════════════════════════════════════════
        // CG GALLERY
        // ═══════════════════════════════════════════════════════════

        [Header("CG Gallery")]
        [Tooltip("All CGs for this character (Addressable keys)")]
        public List<string> cgAddressableKeys = new List<string>();

        // ═══════════════════════════════════════════════════════════
        // CONVERSATION ID
        // ═══════════════════════════════════════════════════════════

        public string ConversationId
        {
            get
            {
#if UNITY_EDITOR
                if (string.IsNullOrEmpty(conversationId))
                {
                    GenerateNewId();
                    UnityEditor.EditorUtility.SetDirty(this);
                }
#else
                if (string.IsNullOrEmpty(conversationId))
                {
                    Debug.LogError($"ConversationId is empty for {characterName}!");
                    return $"INVALID_{characterName}";
                }
#endif
                return conversationId;
            }
        }

        // ═══════════════════════════════════════════════════════════
        // CHAPTER REGISTRY LOOKUP
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Returns the TextAsset for the given chapterId, or null if not found.
        /// </summary>
        public TextAsset GetChapterById(string chapterId)
        {
            foreach (var entry in chapters)
            {
                if (string.Equals(entry.chapterId, chapterId, System.StringComparison.OrdinalIgnoreCase))
                    return entry.file;
            }
            return null;
        }

        /// <summary>
        /// Returns the entry point chapter file (always index 0).
        /// </summary>
        public TextAsset GetEntryPointChapter()
        {
            if (chapters == null || chapters.Count == 0) return null;
            return chapters[0].file;
        }

        // ═══════════════════════════════════════════════════════════
        // HELPER - DISPLAY VALUES WITH N/A FALLBACK
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Used by UI to display profile info with a consistent fallback for missing data.
        /// </summary>
        public string GetAge() => string.IsNullOrWhiteSpace(characterAge) || characterAge == "N/A" ? "N/A" : characterAge;
        public string GetBirthdate() => string.IsNullOrWhiteSpace(birthdate) || birthdate == "N/A" ? "N/A" : birthdate;
        public string GetOccupation() => string.IsNullOrWhiteSpace(occupation) || occupation == "N/A" ? "N/A" : occupation;
        public string GetBio() => string.IsNullOrWhiteSpace(bio) || bio == "N/A" ? "N/A" : bio;
        public string GetDescription() => string.IsNullOrWhiteSpace(description) || description == "N/A" ? "N/A" : description;
        public string GetPersonalityTraits() => string.IsNullOrWhiteSpace(personalityTraits) || personalityTraits == "N/A" ? "N/A" : personalityTraits;

        public string GetRelationshipStatus()
        {
            switch (relationshipStatus)
            {
                case RelationshipStatus.Single:          return "Single";
                case RelationshipStatus.InARelationship: return "In a Relationship";
                case RelationshipStatus.Married:         return "Married";
                case RelationshipStatus.Divorced:        return "Divorced";
                case RelationshipStatus.Widowed:         return "Widowed";
                case RelationshipStatus.ItsComplicated:  return "It's Complicated";
                default:                                 return "N/A";
            }
        }

        // ═══════════════════════════════════════════════════════════
        // VALIDATION
        // ═══════════════════════════════════════════════════════════

        private void GenerateNewId()
        {
            string guid = System.Guid.NewGuid().ToString("N");
            string shortId = guid.Substring(0, 6);
            conversationId = $"{shortId}_{characterName}";
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(conversationId))
            {
                GenerateNewId();
                UnityEditor.EditorUtility.SetDirty(this);
            }

            // Warn if required fields are missing
            if (string.IsNullOrWhiteSpace(characterName))
                Debug.LogWarning($"[ConversationAsset] characterName is empty on {name}!");

            if (profileImage == null || !profileImage.RuntimeKeyIsValid())
                Debug.LogWarning($"[ConversationAsset] profileImage is not assigned on {name}!");

            if (chapters == null || chapters.Count == 0)
                Debug.LogWarning($"[ConversationAsset] No chapters assigned on {name}!");
            else if (chapters[0].file == null)
                Debug.LogWarning($"[ConversationAsset] Entry point chapter (index 0) has no file assigned on {name}!");
        }
#endif
    }
}