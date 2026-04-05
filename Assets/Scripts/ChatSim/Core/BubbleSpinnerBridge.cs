// ════════════════════════════════════════════════════════════════════════
// Assets/Scripts/ChatSim/Core/BubbleSpinnerBridge.cs
// ════════════════════════════════════════════════════════════════════════

using UnityEngine;
using BubbleSpinner.Core;
using BubbleSpinner.Data;
using ChatSim.Data;

namespace ChatSim.Core
{
    /// <summary>
    /// Implements IBubbleSpinnerCallbacks to bridge BubbleSpinner's conversation system
    /// with ChatSim's persistence layer.
    ///
    /// Handles save, load, delete, and reset — anything that touches the save file goes here.
    /// Does NOT handle live UI events (messages, choices, pause) — those go directly from
    /// DialogueExecutor to ChatAppController because they are real-time and stateless.
    /// </summary>
    public class BubbleSpinnerBridge : IBubbleSpinnerCallbacks
    {
        private ConversationManager _conversationManager;
        private SaveData _cachedSaveData;

        public BubbleSpinnerBridge(ConversationManager conversationManager)
        {
            _conversationManager = conversationManager;
            GameEvents.OnCharacterStoryReset += OnCharacterStoryReset;
            GameEvents.OnAllStoriesReset += OnAllStoriesReset;
        }

        // ═══════════════════════════════════════════════════════════
        // ░ CACHE ACCESS
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Returns the cached SaveData, loading from disk on first access.
        /// All save/load operations go through this instead of calling LoadGame() directly.
        /// </summary>
        private SaveData GetSaveData()
        {
            if (_cachedSaveData == null)
            {
                _cachedSaveData = GameBootstrap.Save.LoadGame();

                if (_cachedSaveData == null)
                {
                    Debug.LogError("[BubbleSpinnerBridge] Failed to load save data — cache is empty. Save operations will be skipped until data is available.");
                    return null;
                }

                Debug.Log("[BubbleSpinnerBridge] SaveData loaded into cache");
            }

            return _cachedSaveData;
        }

        // ═══════════════════════════════════════════════════════════
        // ░ SAVE/LOAD CALLBACKS
        // ═══════════════════════════════════════════════════════════

        public bool SaveConversationState(ConversationState state)
        {
            if (state == null)
            {
                Debug.LogError("[BubbleSpinnerBridge] Cannot save null state");
                return false;
            }

            var saveData = GetSaveData();
            if (saveData == null) return false;

            var index = saveData.conversationStates.FindIndex(c => c.conversationId == state.conversationId);

            if (index >= 0)
                saveData.conversationStates[index] = state;
            else
                saveData.conversationStates.Add(state);

            bool success = GameBootstrap.Save.SaveGame(saveData);

            if (!success)
                Debug.LogError($"[BubbleSpinnerBridge] ✗ Failed to save: {state.conversationId}");

            return success;
        }

        public ConversationState LoadConversationState(string conversationId)
        {
            var saveData = GetSaveData();
            if (saveData == null) return null;

            return saveData.conversationStates.Find(c => c.conversationId == conversationId);
        }

        public void DeleteConversationState(string conversationId)
        {
            var saveData = GetSaveData();
            if (saveData == null) return;

            int removed = saveData.conversationStates.RemoveAll(c => c.conversationId == conversationId);

            if (removed > 0)
            {
                GameBootstrap.Save.SaveGame(saveData);
                Debug.Log($"[BubbleSpinnerBridge] ✓ Deleted conversation: {conversationId}");
            }
            else
            {
                Debug.LogWarning($"[BubbleSpinnerBridge] No state found to delete: {conversationId}");
            }
        }

        // ═══════════════════════════════════════════════════════════
        // ░ EVENT CALLBACKS (bridge to GameEvents)
        // ═══════════════════════════════════════════════════════════

        public void OnConversationStarted(string conversationId)
        {
            GameEvents.TriggerConversationStarted(conversationId);
        }

        public void OnCGUnlocked(string cgAddressableKey)
        {
            GameEvents.TriggerCGUnlocked(cgAddressableKey);
        }

        public void OnConversationEnded(string conversationId)
        {
            Debug.Log($"[BubbleSpinnerBridge] Conversation ended: {conversationId}");
        }

        public void OnChapterChanged(string conversationId, string chapterId, string chapterName)
        {
            Debug.Log($"[BubbleSpinnerBridge] Chapter changed: {conversationId} - {chapterId} ({chapterName})");
        }

        private void OnCharacterStoryReset(string conversationId)
        {
            Debug.Log($"[BubbleSpinnerBridge] Story reset — invalidating cache and evicting: {conversationId}");
            _cachedSaveData = null;
            _conversationManager?.EvictConversationCache(conversationId);
        }

        private void OnAllStoriesReset()
        {
            Debug.Log("[BubbleSpinnerBridge] All stories reset — invalidating cache");
            _cachedSaveData = null;
        }

        public void Cleanup()
        {
            GameEvents.OnCharacterStoryReset -= OnCharacterStoryReset;
            GameEvents.OnAllStoriesReset -= OnAllStoriesReset;
            _cachedSaveData = null;
            _conversationManager = null;
        }
    }
}