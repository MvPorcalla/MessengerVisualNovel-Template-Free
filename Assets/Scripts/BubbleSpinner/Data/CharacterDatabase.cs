// ════════════════════════════════════════════════════════════════════════
// Assets/Scripts/BubbleSpinner/Data/CharacterDatabase.cs
// ════════════════════════════════════════════════════════════════════════

using System.Collections.Generic;
using UnityEngine;

namespace BubbleSpinner.Data
{
    /// <summary>
    /// ScriptableObject that holds references to all ConversationAssets (characters) in the game.
    /// </summary>
    /// <remarks>
    /// Used by BubbleSpinner to look up character data when starting conversations.
    /// Create an instance via the Unity Editor and populate it with ConversationAssets.
    /// Includes an editor-only method to auto-find all ConversationAssets in the project.
    /// Note: This database is separate from the ConversationManager's runtime state management.
    /// </remarks>
    
    [CreateAssetMenu(fileName = "CharacterDatabase", menuName = "BubbleSpinner/Character Database")]
    public class CharacterDatabase : ScriptableObject
    {
        [Header("All Available Characters")]
        [Tooltip("Add all ConversationAssets here")]
        public List<ConversationAsset> allCharacters = new List<ConversationAsset>();

        // ═══════════════════════════════════════════════════════════
        // QUERY METHODS
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Get a character by conversation ID
        /// </summary>
        public ConversationAsset GetCharacterById(string conversationId)
        {
            return allCharacters.Find(c => c.ConversationId == conversationId);
        }

        public ConversationAsset GetCharacterByName(string characterName)
        {
            return allCharacters.Find(c => c.characterName == characterName);
        }

        public List<ConversationAsset> GetAllCharacters()
        {
            return new List<ConversationAsset>(allCharacters);
        }

        // ═══════════════════════════════════════════════════════════
        // VALIDATION (Editor Only)
        // ═══════════════════════════════════════════════════════════

        #if UNITY_EDITOR
        private void OnValidate()
        {
            // Remove null entries
            allCharacters.RemoveAll(c => c == null);

            // Check for duplicates
            var seen = new HashSet<string>();
            foreach (var character in allCharacters)
            {
                if (character == null) continue;

                string id = character.ConversationId;
                if (seen.Contains(id))
                {
                    Debug.LogWarning($"[CharacterDatabase] Duplicate conversation ID found: {id}");
                }
                seen.Add(id);
            }
        }

        [ContextMenu("Auto-Find All Characters")]
        private void AutoFindAllCharacters()
        {
            allCharacters.Clear();

            // Find all ConversationAsset files in the project
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ConversationAsset");

            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<ConversationAsset>(path);

                if (asset != null)
                {
                    allCharacters.Add(asset);
                }
            }

            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[CharacterDatabase] Found {allCharacters.Count} characters");
        }
        #endif
    }
}