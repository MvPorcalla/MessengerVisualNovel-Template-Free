# BubbleSpinner — Quick Start Guide

> Get BubbleSpinner running in a new Unity project in under 30 minutes.

---

## What is BubbleSpinner?

BubbleSpinner is a standalone dialogue engine for chat-style visual novels.
It reads `.bub` script files and tells your UI what to show and when.

**BubbleSpinner handles:**
- Parsing `.bub` dialogue files
- Executing nodes, branches, choices, and chapter jumps
- Managing conversation save state
- Unlocking CG gallery images

**BubbleSpinner does NOT handle:**
- Rendering message bubbles
- UI layout or animations
- Asset loading
- Phone/app navigation

Your game handles all of that. BubbleSpinner just tells you what to show.

---

## Requirements

- Unity 2021.3+
- TextMeshPro
- Newtonsoft.Json
- Unity Addressables

---

## Folder Structure

```
Assets/Scripts/BubbleSpinner/
├── Core/
│   ├── IBubbleSpinnerCallbacks.cs   ← contract your game implements
│   ├── BubbleSpinnerParser.cs       ← reads .bub files
│   ├── DialogueExecutor.cs          ← runs dialogue flow
│   └── ConversationManager.cs       ← manages sessions
│
└── Data/
    ├── MessageData.cs               ← message, choice, node data types
    ├── ConversationAsset.cs         ← ScriptableObject per character
    └── CharacterDatabase.cs         ← ScriptableObject listing all characters
```

---

## Step 1 — Create a ConversationAsset

For each character in your game, create a `ConversationAsset` ScriptableObject.

In Unity: **Right-click in Project → BubbleSpinner → Conversation Asset**

Fill in:
- `characterName` — display name (e.g. "Sofia")
- `profileImage` — Addressables reference to their profile picture
- `chapters` — list of `.bub` TextAsset files in order

> The `conversationId` is auto-generated. Do not modify it.

---

## Step 2 — Create a CharacterDatabase

Create one `CharacterDatabase` ScriptableObject for your project.

In Unity: **Right-click in Project → BubbleSpinner → Character Database**

Add all your `ConversationAsset` files to the `allCharacters` list.
You can also use the **Auto-Find All Characters** context menu button.

---

## Step 3 — Add ConversationManager to your scene

Add a `ConversationManager` MonoBehaviour to a persistent GameObject in your bootstrap scene.
This GameObject should survive scene loads (`DontDestroyOnLoad`).

---

## Step 4 — Implement IBubbleSpinnerCallbacks

Create a bridge class that implements `IBubbleSpinnerCallbacks`.
This is how BubbleSpinner talks to your save system.

Copy `BRIDGE_TEMPLATE.cs` from the Docs folder and fill in the three save/load methods.

```csharp
// Minimum you need to implement:
bool SaveConversationState(ConversationState state);
ConversationState LoadConversationState(string conversationId);
void DeleteConversationState(string conversationId);
```

See `BRIDGE_TEMPLATE.cs` for a complete copy-paste starting point.

---

## Step 5 — Initialize ConversationManager

Wire up your bridge and initialize the manager at game start:

```csharp
var bridge = new YourBridge();
conversationManager.Initialize(bridge);
```

---

## Step 6 — Start a Conversation

```csharp
// Get the executor back — subscribe to its events for UI
DialogueExecutor executor = conversationManager.StartConversation(conversationAsset);

// Subscribe to events BEFORE calling ContinueFromCurrentState
executor.OnMessagesReady  += HandleMessages;
executor.OnChoicesReady   += HandleChoices;
executor.OnPauseReached   += HandlePause;
executor.OnConversationEnd += HandleEnd;

// Begin dialogue flow
executor.ContinueFromCurrentState();
```

---

## Step 7 — Handle Events in Your UI

```csharp
private void HandleMessages(List<MessageData> messages)
{
    foreach (var msg in messages)
    {
        // spawn a message bubble for msg.content
        // msg.speaker tells you who sent it
        // msg.type is Text, Image, or System
    }

    // IMPORTANT: Tell BubbleSpinner when your UI is done displaying
    executor.OnMessagesDisplayComplete();
}

private void HandleChoices(List<ChoiceData> choices)
{
    foreach (var choice in choices)
    {
        // spawn a choice button for choice.choiceText
        // on click: executor.OnChoiceSelected(choice)
    }
}

private void HandlePause()
{
    // show a continue/tap button
    // on click: executor.OnPauseButtonClicked()
}

private void HandleEnd()
{
    if (executor.HasMoreChapters)
    {
        // show "Next Chapter" button
        // on click: executor.AdvanceToNextChapter()
    }
    else
    {
        // show "Return" button or end screen
    }
}
```

---

## Step 8 — Handle Player Exiting

When the player leaves the chat (back button, scene change, etc.):

```csharp
// If player exits mid-message sequence:
executor.NotifyInterrupted();

// Save current state
conversationManager.ForceSaveCurrentConversation();

// End the session
conversationManager.EndCurrentConversation();
```

---

## Step 9 — Load Conversation History (Resume)

When a player reopens a conversation they were in before,
load and display their history before starting:

```csharp
var state = executor.GetState();

if (state.messageHistory != null && state.messageHistory.Count > 0)
{
    foreach (var msg in state.messageHistory)
    {
        // display instantly, no animation
        DisplayMessageInstant(msg);
    }
}

// Then start normally
executor.ContinueFromCurrentState();
```

`ContinueFromCurrentState()` automatically handles resuming at the correct point
(pause, choices, end, or interrupted mid-sequence).

---

## Reset Pattern

BubbleSpinner does not own the reset flow. Your save system does.

```
Step 1 — Your save system wipes/zeroes the conversation state on disk
Step 2 — Call ConversationManager.EvictConversationCache(conversationId)
```

`EvictConversationCache()` clears BubbleSpinner's in-memory session so the
next `StartConversation()` loads fresh from disk instead of the stale cache.

> Do NOT use `ConversationManager.ResetConversation()` if your save system
> zeroes state instead of deleting it. That method calls `DeleteConversationState()`
> which removes the entry entirely. Use `EvictConversationCache()` instead
> after your save system has already handled disk.

---

## CG Gallery

When a message has `shouldUnlockCG = true`, BubbleSpinner automatically:
1. Adds the image path to `ConversationState.unlockedCGs`
2. Calls `IBubbleSpinnerCallbacks.OnCGUnlocked(cgKey)` on your bridge

Handle `OnCGUnlocked` in your bridge to update your gallery save data.

---

## Event Reference

| Event | When it fires | What to do |
|---|---|---|
| `OnMessagesReady` | New messages to display | Spawn bubbles, then call `OnMessagesDisplayComplete()` |
| `OnChoicesReady` | Player needs to pick | Spawn choice buttons |
| `OnPauseReached` | Tap-to-continue point | Show continue button |
| `OnConversationEnd` | No more content | Show end or next chapter button |
| `OnChapterChange` | Chapter advanced | Optional chapter title UI |

---

## ConversationManager Method Reference

| Method | When to call |
|---|---|
| `Initialize(bridge)` | Once at game start |
| `StartConversation(asset)` | When player opens a chat |
| `SaveCurrentConversation()` | Throttled auto-save |
| `ForceSaveCurrentConversation()` | On exit, pause, chapter end |
| `EndCurrentConversation()` | When player leaves chat |
| `EvictConversationCache(id)` | After your save system resets a story |

---

## Common Mistakes

**Forgetting to call `OnMessagesDisplayComplete()`**
BubbleSpinner waits for this before deciding what happens next.
If you never call it, dialogue will freeze after every message batch.

**Calling `ContinueFromCurrentState()` before subscribing to events**
Subscribe first, then call `ContinueFromCurrentState()`.
Events fired before subscription are lost.

**Not saving on exit**
Always call `ForceSaveCurrentConversation()` before the player leaves the chat.
The throttled save may not have flushed yet.

**Using `ResetConversation()` when you should use `EvictConversationCache()`**
See the Reset Pattern section above.