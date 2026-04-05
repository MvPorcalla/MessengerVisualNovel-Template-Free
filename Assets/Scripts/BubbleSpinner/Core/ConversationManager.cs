// ════════════════════════════════════════════════════════════════════════
// Assets/Scripts/BubbleSpinner/Core/ConversationManager.cs
// ════════════════════════════════════════════════════════════════════════

using System.Collections.Generic;
using UnityEngine;
using BubbleSpinner.Data;

namespace BubbleSpinner.Core
{
    /// <summary>
    /// Manages the lifecycle of conversations, including starting, saving, loading, and ending conversations.
    /// It interacts with DialogueExecutor to run conversations 
    /// and uses IBubbleSpinnerCallbacks to communicate with external systems for saving/loading and event notifications.
    /// This class is designed to be the central hub for all conversation-related logic in BubbleSpinner.
    /// </summary>
    public class ConversationManager : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════
        // PRIVATE TYPES
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Encapsulates the state of an active conversation session, including its DialogueExecutor and ConversationState.
        /// </summary>
        private class ConversationSession
        {
            public DialogueExecutor executor;
            public ConversationState state;
        }

        // ═══════════════════════════════════════════════════════════
        // CONSTANTS
        // ═══════════════════════════════════════════════════════════

        private const float SAVE_THROTTLE_DELAY = 0.5f;

        // ═══════════════════════════════════════════════════════════
        // DEPENDENCIES (injected via Initialize)
        // ═══════════════════════════════════════════════════════════

        private IBubbleSpinnerCallbacks callbacks;

        // ═══════════════════════════════════════════════════════════
        // STATE
        // ═══════════════════════════════════════════════════════════

        // Single dictionary replaces the previous activeExecutors + activeStates pair
        private Dictionary<string, ConversationSession> activeSessions = new Dictionary<string, ConversationSession>();

        private string currentConversationId;

        // Save throttling
        private float lastSaveTime = -999f;
        private bool hasPendingSave = false;

        // ═══════════════════════════════════════════════════════════
        // PROPERTIES
        // ═══════════════════════════════════════════════════════════

        public DialogueExecutor CurrentExecutor =>
            currentConversationId != null && activeSessions.TryGetValue(currentConversationId, out var s)
                ? s.executor
                : null;

        public string CurrentConversationId => currentConversationId;
        public bool HasActiveConversation => CurrentExecutor != null;

        // ═══════════════════════════════════════════════════════════
        // INITIALIZATION
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Initialize ConversationManager with external callbacks.
        /// </summary>
        public void Initialize(IBubbleSpinnerCallbacks externalCallbacks)
        {
            callbacks = externalCallbacks ?? throw new System.ArgumentNullException(nameof(externalCallbacks));
        }

        // ═══════════════════════════════════════════════════════════
        // PUBLIC API - CONVERSATION CONTROL
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Starts or resumes a conversation.
        /// Returns the DialogueExecutor for UI to subscribe to.
        /// </summary>
        public DialogueExecutor StartConversation(ConversationAsset asset)
        {
            if (callbacks == null)
            {
                BSDebug.Error("[ConversationManager] Not initialized! Call Initialize() first.");
                return null;
            }

            if (asset == null)
            {
                BSDebug.Error("[ConversationManager] Cannot start null conversation");
                return null;
            }

            string convId = asset.ConversationId;
            BSDebug.Info($"[ConversationManager] Starting conversation: {asset.characterName} (ID: {convId})");

            if (!activeSessions.ContainsKey(convId))
            {
                // First time starting this conversation - load or create state and initialize executor
                ConversationState state = LoadOrCreateState(convId, asset.characterName);

                var executor = new DialogueExecutor();
                executor.Initialize(asset, state, callbacks);
                SubscribeToExecutorEvents(executor);

                activeSessions[convId] = new ConversationSession
                {
                    executor = executor,
                    state = state
                };

                BSDebug.Info($"[ConversationManager] Created new session for {convId}");

                // Save after executor initialized state properly
                ForceSaveGame();
            }

            currentConversationId = convId;
            callbacks.OnConversationStarted(convId);

            return activeSessions[convId].executor;
        }

        /// <summary>
        /// Saves the current conversation state (throttled).
        /// </summary>
        public void SaveCurrentConversation()
        {
            if (string.IsNullOrEmpty(currentConversationId) || CurrentExecutor == null)
            {
                BSDebug.Warn("[ConversationManager] No active conversation to save");
                return;
            }

            SaveConversationState(throttle: true);
        }

        /// <summary>
        /// Forces an immediate save (bypasses throttle).
        /// Use for critical save points (quit, pause, chapter end).
        /// </summary>
        public void ForceSaveCurrentConversation()
        {
            if (string.IsNullOrEmpty(currentConversationId) || CurrentExecutor == null)
            {
                BSDebug.Warn("[ConversationManager] No active conversation to force save");
                return;
            }

            SaveConversationState(throttle: false);
        }

        /// <summary>
        /// Ends the current conversation and saves state.
        /// </summary>
        public void EndCurrentConversation()
        {
            if (CurrentExecutor == null)
            {
                BSDebug.Warn("[ConversationManager] No active conversation to end");
                return;
            }

            BSDebug.Info($"[ConversationManager] Ending conversation: {currentConversationId}");

            ForceSaveCurrentConversation();

            if (activeSessions.TryGetValue(currentConversationId, out var session))
            {
                UnsubscribeFromExecutorEvents(session.executor);
            }

            callbacks?.OnConversationEnded(currentConversationId);
            currentConversationId = null;
        }

        // ═══════════════════════════════════════════════════════════
        // RESET vs EVICT — READ BEFORE CALLING
        // ═══════════════════════════════════════════════════════════
        //
        // ResetConversation()     → wipes memory + disk (full story reset)
        // EvictConversationCache() → wipes memory only (disk already cleared)
        //
        // Wrong choice = either stale cache or double-delete attempt.
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Resets a conversation to its initial state AND wipes the save file.
        /// Use this when you want a full story reset — clears both memory and disk.
        /// </summary>
        public void ResetConversation(string conversationId)
        {
            BSDebug.Info($"[ConversationManager] Resetting conversation: {conversationId}");

            if (activeSessions.TryGetValue(conversationId, out var session))
            {
                UnsubscribeFromExecutorEvents(session.executor);
                activeSessions.Remove(conversationId);
            }

            if (currentConversationId == conversationId)
            {
                currentConversationId = null;
            }

            callbacks?.DeleteConversationState(conversationId);

            BSDebug.Info($"[ConversationManager] Conversation reset complete: {conversationId}");
        }

        /// <summary>
        /// Evicts the in-memory session cache WITHOUT touching the save file on disk.
        /// Use this ONLY when the disk data has already been wiped externally
        /// (e.g. after SaveManager.ResetCharacterStory() has run).
        /// </summary>
        public void EvictConversationCache(string conversationId)
        {
            BSDebug.Info($"[ConversationManager] Evicting cache for: {conversationId}");

            if (activeSessions.TryGetValue(conversationId, out var session))
            {
                UnsubscribeFromExecutorEvents(session.executor);
                activeSessions.Remove(conversationId);
            }

            if (currentConversationId == conversationId)
            {
                currentConversationId = null;
            }

            BSDebug.Info($"[ConversationManager] ✓ Cache evicted for: {conversationId}");
        }

        /// <summary>
        /// Evicts ALL in-memory session caches without touching disk.
        /// Use after SaveManager.ResetAllData() has wiped the disk.
        /// </summary>
        public void EvictAllConversationCaches()
        {
            BSDebug.Info($"[ConversationManager] Evicting all session caches ({activeSessions.Count} sessions)");

            foreach (var session in activeSessions.Values)
            {
                if (session?.executor != null)
                    UnsubscribeFromExecutorEvents(session.executor);
            }

            activeSessions.Clear();
            currentConversationId = null;

            BSDebug.Info("[ConversationManager] ✓ All session caches evicted");
        }

        // ═══════════════════════════════════════════════════════════
        // CG GALLERY API
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Gets all unlocked CGs for a specific conversation.
        /// </summary>
        public List<string> GetUnlockedCGs(string conversationId)
        {
            if (activeSessions.TryGetValue(conversationId, out var session))
                return session.state?.unlockedCGs ?? new List<string>();

            return new List<string>();
        }

        /// <summary>
        /// Gets all unlocked CGs across all conversations.
        /// </summary>
        public List<string> GetAllUnlockedCGs()
        {
            var allCGs = new List<string>();

            foreach (var kvp in activeSessions)
            {
                if (kvp.Value.state?.unlockedCGs != null)
                    allCGs.AddRange(kvp.Value.state.unlockedCGs);
            }

            return allCGs;
        }

        /// <summary>
        /// Checks if a specific CG is unlocked in a conversation.
        /// </summary>
        public bool IsCGUnlocked(string conversationId, string cgPath)
        {
            if (activeSessions.TryGetValue(conversationId, out var session))
                return session.state?.unlockedCGs?.Contains(cgPath) ?? false;

            return false;
        }

        // ═══════════════════════════════════════════════════════════
        // EVENT SUBSCRIPTIONS (for auto-save)
        // ═══════════════════════════════════════════════════════════

        private void SubscribeToExecutorEvents(DialogueExecutor executor)
        {
            executor.OnMessagesReady += OnExecutorMessagesReady;
            executor.OnChoicesReady += OnExecutorChoicesReady;
            executor.OnPauseReached += OnExecutorPauseReached;
            executor.OnConversationEnd += OnExecutorConversationEnd;
            executor.OnChapterChange += OnExecutorChapterChange;
        }

        private void UnsubscribeFromExecutorEvents(DialogueExecutor executor)
        {
            if (executor == null) return;

            executor.OnMessagesReady -= OnExecutorMessagesReady;
            executor.OnChoicesReady -= OnExecutorChoicesReady;
            executor.OnPauseReached -= OnExecutorPauseReached;
            executor.OnConversationEnd -= OnExecutorConversationEnd;
            executor.OnChapterChange -= OnExecutorChapterChange;
        }

        // ═══════════════════════════════════════════════════════════
        // EXECUTOR EVENT HANDLERS (auto-save triggers)
        // ═══════════════════════════════════════════════════════════

        private void OnExecutorMessagesReady(List<MessageData> messages)
        {
            SaveCurrentConversation();
        }

        private void OnExecutorChoicesReady(List<ChoiceData> choices)
        {
            SaveCurrentConversation();
        }

        private void OnExecutorPauseReached()
        {
            SaveCurrentConversation();
        }

        private void OnExecutorConversationEnd()
        {
            ForceSaveCurrentConversation();
        }

        private void OnExecutorChapterChange(string chapterName)
        {
            BSDebug.Info($"[ConversationManager] Chapter changed: {chapterName}");
            ForceSaveCurrentConversation();
        }

        // ═══════════════════════════════════════════════════════════
        // SAVE/LOAD LOGIC
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Loads existing ConversationState from save, or creates a fresh one.
        /// Result is always immediately stored in activeSessions by the caller —
        /// it never floats unowned as it did with the old GetOrCreateState pattern.
        /// </summary>
        private ConversationState LoadOrCreateState(string conversationId, string characterName)
        {
            var existingState = callbacks?.LoadConversationState(conversationId);

            if (existingState != null)
            {
                BSDebug.Info($"[ConversationManager] Loaded existing state: {conversationId} " +
                        $"(Chapter: '{existingState.currentChapterId}', Node: '{existingState.currentNodeName}')");
                return existingState;
            }

            var newState = new ConversationState(conversationId)
            {
                characterName = characterName
            };

            return newState;
        }

        /// <summary>
        /// Save conversation state with optional throttling.
        /// </summary>
        private void SaveConversationState(bool throttle)
        {
            if (throttle)
            {
                float timeSinceLastSave = Time.realtimeSinceStartup - lastSaveTime;

                if (timeSinceLastSave < SAVE_THROTTLE_DELAY)
                {
                    hasPendingSave = true;
                    return;
                }
            }

            ForceSaveGame();
        }

        /// <summary>
        /// Performs the actual save operation.
        /// </summary>
        private void ForceSaveGame()
        {
            if (string.IsNullOrEmpty(currentConversationId) ||
                !activeSessions.TryGetValue(currentConversationId, out var session))
                return;

            bool success = callbacks?.SaveConversationState(session.state) ?? false;

            if (success)
            {
                lastSaveTime = Time.realtimeSinceStartup;
                hasPendingSave = false;

                BSDebug.Info($"[ConversationManager] ✓ Saved: {currentConversationId} " +
                        $"(Node: '{session.state.currentNodeName}', Chapter: '{session.state.currentChapterId}')");
            }
            else
            {
                BSDebug.Error($"[ConversationManager] ✗ Save failed for: {currentConversationId}");
            }
        }

        // ═══════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════

        private void Update()
        {
            if (hasPendingSave)
            {
                float timeSinceLastSave = Time.realtimeSinceStartup - lastSaveTime;

                if (timeSinceLastSave >= SAVE_THROTTLE_DELAY)
                {
                    if (!string.IsNullOrEmpty(currentConversationId))
                    {
                        SaveConversationState(throttle: false);
                    }
                }
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && CurrentExecutor != null && !string.IsNullOrEmpty(currentConversationId))
            {
                BSDebug.Info("[ConversationManager] App paused - force saving conversation");
                ForceSaveCurrentConversation();
            }
        }

        private void OnApplicationQuit()
        {
            if (CurrentExecutor != null && !string.IsNullOrEmpty(currentConversationId))
            {
                BSDebug.Info("[ConversationManager] App quitting - force saving conversation");
                ForceSaveCurrentConversation();
            }
        }
    }
}