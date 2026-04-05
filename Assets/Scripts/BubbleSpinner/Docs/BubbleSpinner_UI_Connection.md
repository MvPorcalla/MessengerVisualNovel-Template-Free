# BubbleSpinner ↔ UI Connection Notes

### How BubbleSpinner Connects to the UI

## Overview

BubbleSpinner communicates with the rest of the game through two separate channels, depending on the type of event. 
Each channel handles a different category of interaction.

---

## Channel 1 — Live Dialogue Events → Directly to ChatAppController

These are real-time events that only matter while the chat UI is open. No middleman.

```
DialogueExecutor
    ↓ (C# events)
ChatAppController
    ├── OnMessagesReady    → queues bubbles via ChatTimingController
    ├── OnChoicesReady     → spawns choice buttons via ChatChoiceSpawner
    ├── OnPauseReached     → shows continue button
    ├── OnConversationEnd  → shows end/next chapter button
    └── OnChapterChange    → (optional UI transition)
```

`ChatAppController` subscribes directly to `DialogueExecutor` events when a conversation starts and unsubscribes when it ends. These events are stateless and UI-only — no reason to route them through a bridge.

---

## Channel 2 — Save/Load/Reset → Through BubbleSpinnerBridge

These are persistence events that need to survive between sessions.

```
ConversationManager
    ↓ (calls IBubbleSpinnerCallbacks)
BubbleSpinnerBridge
    ↓
SaveManager (reads/writes disk)
```

`BubbleSpinnerBridge` implements `IBubbleSpinnerCallbacks` — an interface BubbleSpinner defines but never implements itself. This keeps BubbleSpinner completely decoupled from Unity, SaveManager, and GameEvents.

**The bridge has three jobs:**

```
SaveConversationState(state)
    → finds or adds the state in _cachedSaveData
    → calls SaveManager.SaveGame() to write to disk

LoadConversationState(conversationId)
    → reads from _cachedSaveData (loads from disk on first access)
    → returns the ConversationState for that conversation

DeleteConversationState(conversationId)
    → removes the state from _cachedSaveData
    → calls SaveManager.SaveGame() to write the deletion to disk
```

---

## How GameBootstrap Wires It All Together

```
GameBootstrap.Awake()
    → creates BubbleSpinnerBridge(conversationManager)
    → calls Conversation.Initialize(bubbleSpinnerBridge)

ConversationManager
    → holds reference to IBubbleSpinnerCallbacks (the bridge)
    → calls bridge.SaveConversationState() on every auto-save
    → calls bridge.LoadConversationState() on conversation start

ChatAppController
    → calls GameBootstrap.Conversation.StartConversation()
    → receives DialogueExecutor back
    → subscribes directly to executor events
```

**The key design rule:** BubbleSpinner never knows about Unity UI, SaveManager, or GameEvents. It only knows about `IBubbleSpinnerCallbacks`. Everything else is the bridge's job.

---

## Two Caches — Why They Exist and Why They Must Be Cleared on Reset

There are two separate caches that both hold state in memory:

```
BubbleSpinnerBridge._cachedSaveData     ← full SaveData object (all conversations)
ConversationManager.activeSessions      ← live executor + state per conversation
```

Both exist for performance. Both can hold stale data after a reset. Both must be invalidated.

---

### The Reset Bug — What Goes Wrong Without Cache Invalidation

**Individual character reset without eviction:**

```
ResetCharacterStory(conversationId)
    → wipes that conversation's state on disk ✓

BubbleSpinnerBridge._cachedSaveData
    → still holds the OLD data in memory ✗

ConversationManager.activeSessions
    → still has the old executor + state for that conversation ✗

Player opens that conversation
    → StartConversation finds session in activeSessions
    → reuses old executor with old state
    → story continues from old position instead of beginning ✗

Next SaveConversationState() call
    → GetSaveData() returns _cachedSaveData (old data)
    → old data written back to disk — reset undone ✗
```

**Reset All without cache invalidation — same problem at scale:**

```
ResetAllData()
    → all states wiped on disk ✓

BubbleSpinnerBridge._cachedSaveData
    → still holds ALL old data in memory ✗
    → next save rewrites old data back to disk ✗
```

---

### The Fix — Invalidate Both Caches After Every Reset

**Individual reset fix:**

```
ResetCharacterStory()          → disk wiped
OnCharacterStoryReset event
    → _cachedSaveData = null   → bridge cache cleared ✓
EvictConversationCache(id)     → activeSessions entry removed ✓

Player opens conversation
    → no session in activeSessions
    → LoadOrCreateState loads from disk (zeroed) ✓
    → fresh start ✓
```

**Reset All fix:**

```
ResetAllData()                    → all states wiped on disk
OnAllStoriesReset event
    → _cachedSaveData = null      → bridge cache cleared ✓
EvictAllConversationCaches()      → ALL activeSessions entries removed ✓

Player opens any conversation
    → no session in activeSessions
    → loads from disk (zeroed) ✓
    → fresh start ✓
```

---

### Two Evict Methods — Same Logic, Different Scope

```
EvictConversationCache(id)    → single conversation — used by ContactsAppItem
EvictAllConversationCaches()  → all conversations  — used by SettingsPanel
```

---

### The Rule — Always Invalidate Both Caches After a Disk Wipe

| Action | Disk | Bridge cache | Session cache |
|---|---|---|---|
| `ResetCharacterStory` | ✓ wiped | ✓ nulled via `OnCharacterStoryReset` event | ✓ evicted via `EvictConversationCache` |
| `ResetAllData` | ✓ wiped | ✓ nulled via `OnAllStoriesReset` event | ✓ evicted via `EvictAllConversationCaches` |

If either cache is left stale after a disk wipe, the old data gets written back to disk on the next save — undoing the reset silently with no error.