// ════════════════════════════════════════════════════════════════════════
// Assets/Scripts/Core/GameEvents.cs
// ════════════════════════════════════════════════════════════════════════

using System;
using UnityEngine;

namespace ChatSim.Core
{
    /// <summary>
    /// Central event hub - all game state changes flow through here
    /// Provides decoupled communication between systems
    /// 
    /// [WARNING] MEMORY LEAK [WARNING]:
    /// Always unsubscribe from events in OnDestroy() or OnDisable()
    /// 
    /// Example:
    ///   void OnEnable() => GameEvents.OnSceneLoaded += HandleSceneLoaded;
    ///   void OnDestroy() => GameEvents.OnSceneLoaded -= HandleSceneLoaded;
    /// </summary>
    public static class GameEvents
    {
        #region Logging Control

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private static void Log(string message)
        {
            Debug.Log($"[GameEvents] {message}");
        }

        #endregion

        // ════════════════════════════════════════════════════════════════
        // SCENE EVENTS
        // ════════════════════════════════════════════════════════════════
        
        public static event Action<string> OnSceneLoaded;
        public static event Action<string> OnSceneChanging;
        
        // ════════════════════════════════════════════════════════════════
        // SAVE/LOAD EVENTS
        // ════════════════════════════════════════════════════════════════
        
        public static event Action OnGameSaved;
        public static event Action OnGameLoaded;
        public static event Action OnSaveDeleted;
        
        // ════════════════════════════════════════════════════════════════
        // PHONE STATE EVENTS
        // ════════════════════════════════════════════════════════════════
        
        public static event Action OnPhoneLocked;
        public static event Action OnPhoneUnlocked;
        public static event Action<string> OnAppOpened;     // appId
        
        // ════════════════════════════════════════════════════════════════
        // CONVERSATION EVENTS
        // ════════════════════════════════════════════════════════════════
        
        public static event Action<string> OnConversationStarted;       // conversationId
        
        // ════════════════════════════════════════════════════════════════
        // CG GALLERY EVENTS
        // ════════════════════════════════════════════════════════════════
        
        public static event Action<string> OnCGUnlocked;    // cgAddressableKey
        
        // ════════════════════════════════════════════════════════════════
        // CONTACT EVENTS
        // ════════════════════════════════════════════════════════════════

        public static event Action<string> OnCharacterStoryReset;   // conversationId

        // ════════════════════════════════════════════════════════════════════════
        // SETTINGS EVENTS
        // ════════════════════════════════════════════════════════════════════════

        public static event Action<float> OnTextSizeChanged;    // fontSize value
        public static event Action<bool> OnMessageSpeedChanged; // isFastMode
        public static event Action OnAllStoriesReset;
        
        // ════════════════════════════════════════════════════════════════
        // EVENT LIFECYCLE MANAGEMENT
        // ════════════════════════════════════════════════════════════════
        
        public static void ClearAllEvents()
        {
            // Scene events
            OnSceneLoaded = null;
            
            // Save/Load events
            OnGameSaved = null;
            OnGameLoaded = null;
            OnSaveDeleted = null;
            
            // Phone state events
            OnPhoneLocked = null;
            OnPhoneUnlocked = null;
            OnAppOpened = null;
            
            // Conversation events
            OnConversationStarted = null;
            
            // CG events
            OnCGUnlocked = null;

            // Settings events
            OnTextSizeChanged = null;
            OnMessageSpeedChanged = null;
            OnAllStoriesReset = null;

            // Custom events
            OnCharacterStoryReset = null;
            
            Log("All events cleared");
        }
        
        // ════════════════════════════════════════════════════════════════
        // EVENT TRIGGERS - Scene
        // ════════════════════════════════════════════════════════════════
        
        public static void TriggerSceneLoaded(string sceneName)
        {
            OnSceneLoaded?.Invoke(sceneName);
            Log($"Scene loaded: {sceneName}");
        }
        
        public static void TriggerSceneChanging(string sceneName)
        {
            OnSceneChanging?.Invoke(sceneName);
            Log($"Scene changing to: {sceneName}");
        }
        
        // ════════════════════════════════════════════════════════════════
        // EVENT TRIGGERS - Save/Load
        // ════════════════════════════════════════════════════════════════
        
        public static void TriggerGameSaved()
        {
            OnGameSaved?.Invoke();
            Log("Game saved");
        }
        
        public static void TriggerGameLoaded()
        {
            OnGameLoaded?.Invoke();
            Log("Game loaded");
        }
        
        public static void TriggerSaveDeleted()
        {
            OnSaveDeleted?.Invoke();
            Log("Save deleted");
        }
        
        // ════════════════════════════════════════════════════════════════
        // EVENT TRIGGERS - Phone State
        // ════════════════════════════════════════════════════════════════
        
        public static void TriggerPhoneLocked()
        {
            OnPhoneLocked?.Invoke();
            Log("Phone locked");
        }
        
        public static void TriggerPhoneUnlocked()
        {
            OnPhoneUnlocked?.Invoke();
            Log("Phone unlocked");
        }
        
        public static void TriggerAppOpened(string appId)
        {
            OnAppOpened?.Invoke(appId);
            Log($"App opened: {appId}");
        }
        
        // ════════════════════════════════════════════════════════════════
        // EVENT TRIGGERS - Conversations
        // ════════════════════════════════════════════════════════════════
        
        public static void TriggerConversationStarted(string conversationId)
        {
            OnConversationStarted?.Invoke(conversationId);
            Log($"Conversation started: {conversationId}");
        }
        
        // ════════════════════════════════════════════════════════════════
        // EVENT TRIGGERS - CG Gallery
        // ════════════════════════════════════════════════════════════════
        
        public static void TriggerCGUnlocked(string cgAddressableKey)
        {
            OnCGUnlocked?.Invoke(cgAddressableKey);
            Log($"CG unlocked: {cgAddressableKey}");
        }
        
        // ════════════════════════════════════════════════════════════════
        // EVENT TRIGGERS - Contacts
        // ════════════════════════════════════════════════════════════════

        public static void TriggerCharacterStoryReset(string conversationId)
        {
            OnCharacterStoryReset?.Invoke(conversationId);
            Log($"Character story reset: {conversationId}");
        }

        // ════════════════════════════════════════════════════════════════════════
        // EVENT TRIGGERS - Settings
        // ════════════════════════════════════════════════════════════════════════

        public static void TriggerTextSizeChanged(float fontSize)
        {
            OnTextSizeChanged?.Invoke(fontSize);
            Log($"Text size changed: {fontSize}");
        }

        public static void TriggerMessageSpeedChanged(bool isFastMode)
        {
            OnMessageSpeedChanged?.Invoke(isFastMode);
            Log($"Message speed changed: {(isFastMode ? "Fast" : "Normal")}");
        }

        public static void TriggerAllStoriesReset()
        {
            OnAllStoriesReset?.Invoke();
            Log("All stories reset");
        }
    }
}
