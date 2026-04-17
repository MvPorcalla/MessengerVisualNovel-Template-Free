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
        
        // dead code
        // public static event Action OnNewGameStarted;
        public static event Action OnGameSaved;
        public static event Action OnGameLoaded;
        public static event Action OnSaveDeleted;
        
        // ════════════════════════════════════════════════════════════════
        // PHONE STATE EVENTS
        // ════════════════════════════════════════════════════════════════
        
        public static event Action OnPhoneLocked;
        public static event Action OnPhoneUnlocked;
        public static event Action<string> OnAppOpened;     // appId
        // dead code
        // public static event Action<string> OnAppClosed;     // appId
        
        // ════════════════════════════════════════════════════════════════
        // CONVERSATION EVENTS
        // ════════════════════════════════════════════════════════════════
        
        public static event Action<string> OnConversationStarted;       // conversationId
        // dead code
        // public static event Action<string> OnConversationUnlocked;      // conversationId
        // dead code
        // public static event Action<string, string> OnMessageReceived;   // conversationId, messageId
        // dead code
        // public static event Action<string, string> OnMessageSent;       // conversationId, messageId
        // dead code
        // public static event Action<string> OnConversationCompleted;     // conversationId
        
        // ════════════════════════════════════════════════════════════════
        // CG GALLERY EVENTS
        // ════════════════════════════════════════════════════════════════
        
        public static event Action<string> OnCGUnlocked;    // cgAddressableKey
        
        // ════════════════════════════════════════════════════════════════
        // NOTIFICATION EVENTS
        // ════════════════════════════════════════════════════════════════
        
        // dead code
        // public static event Action<string, string> OnNotificationReceived;  // appId, message
        // dead code
        // public static event Action OnNotificationCleared;
        
        // ════════════════════════════════════════════════════════════════
        // CONTACT EVENTS
        // ════════════════════════════════════════════════════════════════
        
        // dead code
        // public static event Action<string> OnContactAdded;      // contactId
        // dead code
        // public static event Action<string> OnContactUpdated;    // contactId
        public static event Action<string> OnCharacterStoryReset;   // conversationId

        // ════════════════════════════════════════════════════════════════════════
        // SETTINGS EVENTS
        // ════════════════════════════════════════════════════════════════════════

        public static event Action<float> OnTextSizeChanged;    // fontSize value
        public static event Action<bool> OnMessageSpeedChanged; // isFastMode
        public static event Action OnAllStoriesReset;
        
        // ════════════════════════════════════════════════════════════════
        // PROGRESSION EVENTS
        // ════════════════════════════════════════════════════════════════
        
        // dead code
        // public static event Action<string> OnStoryFlagSet;      // flagName
        // dead code
        // public static event Action<string> OnStoryFlagCleared;  // flagName
        // dead code
        // public static event Action<int> OnChapterUnlocked;      // chapterNumber
        
        // ════════════════════════════════════════════════════════════════
        // EVENT LIFECYCLE MANAGEMENT
        // ════════════════════════════════════════════════════════════════
        
        public static void ClearAllEvents()
        {
            // Scene events
            OnSceneLoaded = null;
            // dead code
            // OnSceneChanging = null;
            
            // Save/Load events
            // dead code
            // OnNewGameStarted = null;
            OnGameSaved = null;
            OnGameLoaded = null;
            OnSaveDeleted = null;
            
            // Phone state events
            OnPhoneLocked = null;
            OnPhoneUnlocked = null;
            OnAppOpened = null;
            // dead code
            // OnAppClosed = null;
            
            // Conversation events
            OnConversationStarted = null;
            // dead code
            // OnConversationUnlocked = null;
            // dead code
            // OnMessageReceived = null;
            // dead code
            // OnMessageSent = null;
            // dead code
            // OnConversationCompleted = null;
            
            // CG events
            OnCGUnlocked = null;
            
            // Notification events
            // dead code
            // OnNotificationReceived = null;
            // dead code
            // OnNotificationCleared = null;
            
            // Contact events
            // dead code
            // OnContactAdded = null;
            // dead code
            // OnContactUpdated = null;

            // Settings events
            OnTextSizeChanged = null;
            OnMessageSpeedChanged = null;
            OnAllStoriesReset = null;
            
            // Progression events
            // dead code
            // OnStoryFlagSet = null;
            // dead code
            // OnStoryFlagCleared = null;
            // dead code
            // OnChapterUnlocked = null;

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
        
        // dead code
        // public static void TriggerNewGameStarted()
        // {
        //     OnNewGameStarted?.Invoke();
        //     Log("New game started");
        // }
        
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
        
        // dead code
        // public static void TriggerAppClosed(string appId)
        // {
        //     OnAppClosed?.Invoke(appId);
        //     Log($"App closed: {appId}");
        // }
        
        // ════════════════════════════════════════════════════════════════
        // EVENT TRIGGERS - Conversations
        // ════════════════════════════════════════════════════════════════
        
        public static void TriggerConversationStarted(string conversationId)
        {
            OnConversationStarted?.Invoke(conversationId);
            Log($"Conversation started: {conversationId}");
        }
        
        // dead code
        // public static void TriggerConversationUnlocked(string conversationId)
        // {
        //     OnConversationUnlocked?.Invoke(conversationId);
        //     Log($"Conversation unlocked: {conversationId}");
        // }
        
        // dead code
        // public static void TriggerMessageReceived(string conversationId, string messageId)
        // {
        //     OnMessageReceived?.Invoke(conversationId, messageId);
        //     Log($"Message received: {conversationId}/{messageId}");
        // }
        
        // dead code
        // public static void TriggerMessageSent(string conversationId, string messageId)
        // {
        //     OnMessageSent?.Invoke(conversationId, messageId);
        //     Log($"Message sent: {conversationId}/{messageId}");
        // }
        
        // dead code
        // public static void TriggerConversationCompleted(string conversationId)
        // {
        //     OnConversationCompleted?.Invoke(conversationId);
        //     Log($"Conversation completed: {conversationId}");
        // }
        
        // ════════════════════════════════════════════════════════════════
        // EVENT TRIGGERS - CG Gallery
        // ════════════════════════════════════════════════════════════════
        
        public static void TriggerCGUnlocked(string cgAddressableKey)
        {
            OnCGUnlocked?.Invoke(cgAddressableKey);
            Log($"🎨 CG unlocked: {cgAddressableKey}");
        }
        
        // ════════════════════════════════════════════════════════════════
        // EVENT TRIGGERS - Notifications
        // ════════════════════════════════════════════════════════════════
        
        // dead code
        // public static void TriggerNotificationReceived(string appId, string message)
        // {
        //     OnNotificationReceived?.Invoke(appId, message);
        //     Log($"Notification: [{appId}] {message}");
        // }
        
        // dead code
        // public static void TriggerNotificationCleared()
        // {
        //     OnNotificationCleared?.Invoke();
        //     Log("Notifications cleared");
        // }
        
        // ════════════════════════════════════════════════════════════════
        // EVENT TRIGGERS - Contacts
        // ════════════════════════════════════════════════════════════════
        
        // dead code
        // public static void TriggerContactAdded(string contactId)
        // {
        //     OnContactAdded?.Invoke(contactId);
        //     Log($"Contact added: {contactId}");
        // }
        
        // dead code
        // public static void TriggerContactUpdated(string contactId)
        // {
        //     OnContactUpdated?.Invoke(contactId);
        //     Log($"Contact updated: {contactId}");
        // }

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
        
        // ════════════════════════════════════════════════════════════════
        // EVENT TRIGGERS - Progression
        // ════════════════════════════════════════════════════════════════
        
        // dead code
        // public static void TriggerStoryFlagSet(string flagName)
        // {
        //     OnStoryFlagSet?.Invoke(flagName);
        //     Log($"Story flag set: {flagName}");
        // }
        
        // dead code
        // public static void TriggerStoryFlagCleared(string flagName)
        // {
        //     OnStoryFlagCleared?.Invoke(flagName);
        //     Log($"Story flag cleared: {flagName}");
        // }
        
        // dead code
        // public static void TriggerChapterUnlocked(int chapterNumber)
        // {
        //     OnChapterUnlocked?.Invoke(chapterNumber);
        //     Log($"Chapter unlocked: {chapterNumber}");
        // }
    }
}
