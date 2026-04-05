// ════════════════════════════════════════════════════════════════════════
// BubbleSpinner/Docs/BRIDGE_TEMPLATE.cs
//
// COPY THIS FILE into your game project and rename it to match your game.
// Example: MyGameBridge.cs
//
// WHAT TO DO:
//   1. Rename the class from "MyGameBridge" to something that fits your project
//   2. Fill in the three save/load methods with your own save system
//   3. Handle OnCGUnlocked if your game has a CG gallery
//   4. Wire it up in your bootstrap (see bottom of this file)
//
// WHAT NOT TO TOUCH:
//   - The method signatures (they are defined by IBubbleSpinnerCallbacks)
//   - The constructor subscription pattern
// ════════════════════════════════════════════════════════════════════════

using UnityEngine;
using BubbleSpinner.Core;
using BubbleSpinner.Data;

/// <summary>
/// Bridge between BubbleSpinner and your game's save system.
/// Implements IBubbleSpinnerCallbacks so BubbleSpinner can save/load
/// conversation state without knowing anything about your game.
///
/// HOW IT WORKS:
///   BubbleSpinner calls these methods when it needs to persist state.
///   You decide how and where to store the data.
///
/// RESET PATTERN:
///   If your game has a "Reset Story" feature:
///     Step 1 — Your save system wipes the conversation state on disk
///     Step 2 — Call ConversationManager.EvictConversationCache(conversationId)
///   Do NOT call ConversationManager.ResetConversation() if your save system
///   zeroes state instead of deleting it — ResetConversation() calls
///   DeleteConversationState() which removes the entry entirely.
/// </summary>
public class MyGameBridge : IBubbleSpinnerCallbacks
{
    // ═══════════════════════════════════════════════════════════
    // DEPENDENCIES
    // ═══════════════════════════════════════════════════════════

    private ConversationManager _conversationManager;

    // ═══════════════════════════════════════════════════════════
    // CONSTRUCTOR
    // ═══════════════════════════════════════════════════════════

    public MyGameBridge(ConversationManager conversationManager)
    {
        _conversationManager = conversationManager;

        // Subscribe to any game events that should notify BubbleSpinner here.
        // Example: MyGameEvents.OnStoryReset += OnStoryReset;
    }

    // ═══════════════════════════════════════════════════════════
    // SAVE / LOAD — Fill these in with your save system
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// BubbleSpinner calls this when it wants to save conversation progress.
    /// Persist the state object however your game handles saves.
    /// Return true if the save succeeded, false if it failed.
    /// </summary>
    public bool SaveConversationState(ConversationState state)
    {
        if (state == null)
        {
            Debug.LogError("[MyGameBridge] Cannot save null state");
            return false;
        }

        // ── REPLACE THIS WITH YOUR SAVE SYSTEM ──────────────────
        // Example using PlayerPrefs (simple, not recommended for production):
        //
        //   string json = JsonUtility.ToJson(state);
        //   PlayerPrefs.SetString($"conv_{state.conversationId}", json);
        //   PlayerPrefs.Save();
        //   return true;
        //
        // Example using your own SaveManager:
        //
        //   return MyGame.SaveManager.SaveConversation(state);
        // ────────────────────────────────────────────────────────

        Debug.LogWarning("[MyGameBridge] SaveConversationState not implemented!");
        return false;
    }

    /// <summary>
    /// BubbleSpinner calls this when it wants to load a saved conversation.
    /// Return the saved ConversationState, or null if no save exists for this ID.
    /// </summary>
    public ConversationState LoadConversationState(string conversationId)
    {
        // ── REPLACE THIS WITH YOUR LOAD SYSTEM ──────────────────
        // Example using PlayerPrefs:
        //
        //   string key = $"conv_{conversationId}";
        //   if (!PlayerPrefs.HasKey(key)) return null;
        //   string json = PlayerPrefs.GetString(key);
        //   return JsonUtility.FromJson<ConversationState>(json);
        //
        // Example using your own SaveManager:
        //
        //   return MyGame.SaveManager.LoadConversation(conversationId);
        // ────────────────────────────────────────────────────────

        Debug.LogWarning("[MyGameBridge] LoadConversationState not implemented!");
        return null;
    }

    /// <summary>
    /// BubbleSpinner calls this when a conversation state should be permanently deleted.
    /// Remove the saved data for this conversation ID from your save system.
    ///
    /// NOTE: In most games you want to ZERO OUT state rather than delete it,
    /// so you preserve the characterName and other metadata. If that is the case,
    /// handle the wipe in your own save system and call
    /// ConversationManager.EvictConversationCache() instead of routing through here.
    /// </summary>
    public void DeleteConversationState(string conversationId)
    {
        // ── REPLACE THIS WITH YOUR DELETE LOGIC ─────────────────
        // Example using PlayerPrefs:
        //
        //   PlayerPrefs.DeleteKey($"conv_{conversationId}");
        //   PlayerPrefs.Save();
        //
        // Example using your own SaveManager:
        //
        //   MyGame.SaveManager.DeleteConversation(conversationId);
        // ────────────────────────────────────────────────────────

        Debug.LogWarning("[MyGameBridge] DeleteConversationState not implemented!");
    }

    // ═══════════════════════════════════════════════════════════
    // EVENT CALLBACKS — React to BubbleSpinner events here
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Called when a conversation session begins.
    /// Use this to update UI state, analytics, or unlock notifications.
    /// Safe to leave empty if you don't need it.
    /// </summary>
    public void OnConversationStarted(string conversationId)
    {
        Debug.Log($"[MyGameBridge] Conversation started: {conversationId}");

        // Optional: fire your own game event
        // MyGameEvents.TriggerConversationStarted(conversationId);
    }

    /// <summary>
    /// Called when a CG image is unlocked during dialogue.
    /// Save the key to your gallery system so it appears in the gallery UI.
    /// </summary>
    public void OnCGUnlocked(string cgAddressableKey)
    {
        Debug.Log($"[MyGameBridge] CG unlocked: {cgAddressableKey}");

        // ── HANDLE CG UNLOCK ────────────────────────────────────
        // Example: add to a gallery save list
        //
        //   MyGame.GalleryManager.UnlockCG(cgAddressableKey);
        //
        // Or fire a game event:
        //
        //   MyGameEvents.TriggerCGUnlocked(cgAddressableKey);
        // ────────────────────────────────────────────────────────
    }

    /// <summary>
    /// Called when a conversation session ends normally.
    /// Safe to leave empty if you don't need it.
    /// </summary>
    public void OnConversationEnded(string conversationId)
    {
        Debug.Log($"[MyGameBridge] Conversation ended: {conversationId}");
    }

    /// <summary>
    /// Called when the executor advances to a new chapter.
    /// Safe to leave empty if you don't need it.
    /// </summary>
    public void OnChapterChanged(string conversationId, string chapterId, string chapterName)
    {
        Debug.Log($"[MyGameBridge] Chapter changed: {chapterName} (id: {chapterId})");
    }

    // ═══════════════════════════════════════════════════════════
    // RESET HELPER — Call this when your save system resets a story
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Call this from your reset event handler after your save system
    /// has already wiped the conversation data on disk.
    /// This clears BubbleSpinner's in-memory session cache.
    /// </summary>
    private void OnStoryReset(string conversationId)
    {
        Debug.Log($"[MyGameBridge] Story reset — evicting cache: {conversationId}");
        _conversationManager?.EvictConversationCache(conversationId);
    }

    // ═══════════════════════════════════════════════════════════
    // CLEANUP
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Call this when your bootstrap GameObject is destroyed.
    /// Unsubscribes from any events to prevent memory leaks.
    /// </summary>
    public void Cleanup()
    {
        // Unsubscribe from any events you subscribed to in the constructor.
        // Example: MyGameEvents.OnStoryReset -= OnStoryReset;

        _conversationManager = null;
    }
}

// ════════════════════════════════════════════════════════════════════════
// HOW TO WIRE THIS UP IN YOUR BOOTSTRAP
// ════════════════════════════════════════════════════════════════════════
//
// In your game's bootstrap MonoBehaviour (the one that sets up core systems):
//
//   private MyGameBridge _bridge;
//
//   private void InitializeSystems()
//   {
//       // 1. Create the bridge, pass in the ConversationManager
//       _bridge = new MyGameBridge(conversationManager);
//
//       // 2. Initialize ConversationManager with the bridge
//       conversationManager.Initialize(_bridge);
//   }
//
//   private void OnDestroy()
//   {
//       // 3. Clean up on destroy to prevent event leaks
//       _bridge?.Cleanup();
//   }
//
// ════════════════════════════════════════════════════════════════════════
// HOW TO START A CONVERSATION IN YOUR UI
// ════════════════════════════════════════════════════════════════════════
//
//   // Get the executor
//   var executor = conversationManager.StartConversation(conversationAsset);
//
//   // Subscribe to events BEFORE calling ContinueFromCurrentState
//   executor.OnMessagesReady   += HandleMessages;
//   executor.OnChoicesReady    += HandleChoices;
//   executor.OnPauseReached    += HandlePause;
//   executor.OnConversationEnd += HandleEnd;
//
//   // Load history if resuming (display instantly, no animation)
//   var state = executor.GetState();
//   foreach (var msg in state.messageHistory)
//       DisplayInstant(msg);
//
//   // Start or resume dialogue
//   executor.ContinueFromCurrentState();
//
// ════════════════════════════════════════════════════════════════════════
// PLAYER INPUT → EXECUTOR CALLS
// ════════════════════════════════════════════════════════════════════════
//
//   Continue button tapped:   executor.OnPauseButtonClicked()
//   Choice button tapped:     executor.OnChoiceSelected(choice)
//   Messages finished showing: executor.OnMessagesDisplayComplete()
//   Next chapter button:      executor.AdvanceToNextChapter()
//   Player exits chat:        executor.NotifyInterrupted()
//                             conversationManager.ForceSaveCurrentConversation()
//                             conversationManager.EndCurrentConversation()
//
// ════════════════════════════════════════════════════════════════════════