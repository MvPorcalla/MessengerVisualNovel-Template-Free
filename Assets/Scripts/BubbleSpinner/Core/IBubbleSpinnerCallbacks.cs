// ════════════════════════════════════════════════════════════════════════
// Assets/Scripts/BubbleSpinner/Core/IBubbleSpinnerCallbacks.cs
// ════════════════════════════════════════════════════════════════════════

using BubbleSpinner.Data;

namespace BubbleSpinner.Core
{
    /// <summary>
    /// Contract for your game code to handle BubbleSpinner save/load and conversation events.
    /// </summary>
    public interface IBubbleSpinnerCallbacks
    {
        // ═══════════════════════════════════════════════════════════
        // RESET PATTERN — READ THIS IF YOU IMPLEMENT A STORY RESET
        // ═══════════════════════════════════════════════════════════
        //
        // BubbleSpinner does NOT own the full reset flow.
        // Your game's save system owns the disk wipe.
        // BubbleSpinner only cleans up its in-memory session after.
        //
        // Correct reset pattern for your game:
        //
        //   Step 1 — Your save system wipes or zeroes the conversation state on disk
        //   Step 2 — Your save system fires an event or calls a method to notify BubbleSpinner
        //   Step 3 — Call ConversationManager.EvictConversationCache(conversationId)
        //
        // Example (ChatSim implementation):
        //   SaveManager.ResetCharacterStory()        ← Step 1 + fires event
        //       → BubbleSpinnerBridge catches event  ← Step 2
        //           → ConversationManager.EvictConversationCache()  ← Step 3
        //
        // WHY NOT use ConversationManager.ResetConversation()?
        //   That method calls DeleteConversationState() which fully removes
        //   the save entry. If your game needs to zero-out instead of delete
        //   (to preserve metadata like character name), handle it in your
        //   save system and use EvictConversationCache() afterward.
        // ═══════════════════════════════════════════════════════════

        // ═══════════════════════════════════════════════════════════
        // SAVE / LOAD
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Persist the given conversation state. Returns true on success.
        /// </summary>
        bool SaveConversationState(ConversationState state);

        /// <summary>
        /// Load a previously saved conversation state. Returns null if no save exists.
        /// </summary>
        ConversationState LoadConversationState(string conversationId);

        /// <summary>
        /// Permanently delete the saved state for the given conversation ID.
        /// </summary>
        void DeleteConversationState(string conversationId);

        // ═══════════════════════════════════════════════════════════
        // EVENTS
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Called when a conversation session begins.
        /// </summary>
        void OnConversationStarted(string conversationId);

        /// <summary>
        /// Called when a CG image is unlocked during dialogue.
        /// Use the key to update gallery state in your save system.
        /// </summary>
        void OnCGUnlocked(string cgAddressableKey);

        /// <summary>
        /// Called when a conversation session ends.
        /// </summary>
        void OnConversationEnded(string conversationId);

        /// <summary>
        /// Called when the executor advances to a new chapter.
        /// </summary>
        void OnChapterChanged(string conversationId, string chapterId, string chapterName);
    }
}