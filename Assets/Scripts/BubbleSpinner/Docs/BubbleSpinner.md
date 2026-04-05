TODO: Update this Docs its Outdated

# BubbleSpinner — Code Documentation

> Full reference for every script in the BubbleSpinner module.

---

## Table of Contents

1. [BubFileImporter](#bubfileimporter)
2. [IBubbleSpinnerCallbacks](#ibubblespiinnercallbacks)
3. [BubbleSpinnerParser](#bubblespinnerparser)
4. [DialogueExecutor](#dialogueexecutor)
5. [ConversationManager](#conversationmanager)
6. [MessageData](#messagedata)
7. [ChoiceData](#choicedata)
8. [DialogueNode](#dialoguenode)
9. [PausePoint](#pausepoint)
10. [ResumeTarget](#resumetarget)
11. [ConversationState](#conversationstate)
12. [ConversationAsset](#conversationasset)
13. [CharacterDatabase](#characterdatabase)

---

## BubFileImporter

**File:** `BubbleSpinner/Editor/BubFileImporter.cs`
**Namespace:** none (Editor script)
**Type:** `ScriptedImporter`

### Purpose
Registers `.bub` as a recognized Unity asset type.
Without this importer, Unity does not know how to handle `.bub` files and
they cannot be assigned to `ConversationAsset.chapters` in the Inspector.

### How it works
When Unity imports a `.bub` file, this importer reads it as plain text and
wraps it in a `TextAsset`. The result is identical to a `.txt` file in Unity —
a `TextAsset` whose `.text` property contains the raw `.bub` content.
`BubbleSpinnerParser.Parse()` then reads that text at runtime.

### Setup
Place this file in any `Editor/` folder in your project.
Unity auto-discovers `ScriptedImporter` classes — no manual registration needed.
Once the importer exists, all `.bub` files in the project are re-imported automatically.

```csharp
[ScriptedImporter(1, "bub")]
public class BubFileImporter : ScriptedImporter
{
    public override void OnImportAsset(AssetImportContext ctx)
    {
        var text = System.IO.File.ReadAllText(ctx.assetPath);
        var asset = new UnityEngine.TextAsset(text);
        ctx.AddObjectToAsset("text", asset);
        ctx.SetMainObject(asset);
    }
}
```

### Parameters
| Parameter | Value | Meaning |
|---|---|---|
| version | `1` | Importer version. Increment if import logic changes to force re-import. |
| ext | `"bub"` | File extension this importer handles. |

### Notes
- This is an Editor-only script. It does not exist at runtime.
- The importer does not parse or validate `.bub` content — it only wraps it as a `TextAsset`.
- Parsing happens at runtime inside `BubbleSpinnerParser.Parse()`.

---

## IBubbleSpinnerCallbacks

**File:** `BubbleSpinner/Core/IBubbleSpinnerCallbacks.cs`
**Namespace:** `BubbleSpinner.Core`
**Type:** `interface`

### Purpose
The contract between BubbleSpinner and your game.
BubbleSpinner calls these methods when it needs to persist state or notify
your game of conversation events. Your game implements this interface in a
bridge class.

### Reset Pattern
BubbleSpinner does not own the reset flow. Your save system owns the disk wipe.

```
Step 1 — Your save system wipes or zeroes the conversation state on disk
Step 2 — Your save system fires an event to notify BubbleSpinner
Step 3 — Call ConversationManager.EvictConversationCache(conversationId)
```

Do NOT use `ConversationManager.ResetConversation()` if your save system
zeroes state instead of deleting it. That method calls `DeleteConversationState()`
which removes the entry entirely.

### Methods

#### `bool SaveConversationState(ConversationState state)`
Called by `ConversationManager` when it wants to persist conversation progress.
Implement this to write the state to your save file.
Return `true` on success, `false` on failure.

#### `ConversationState LoadConversationState(string conversationId)`
Called by `ConversationManager` when starting a conversation to check for existing progress.
Return the saved `ConversationState` for this ID, or `null` if no save exists.
Returning `null` causes BubbleSpinner to create a fresh state from the beginning.

#### `void DeleteConversationState(string conversationId)`
Called by `ConversationManager.ResetConversation()` to permanently remove a save entry.
Implement this to delete the state from your save file.
Only called when using the BubbleSpinner-owned reset path — not called in the ChatSim
pattern where `SaveManager` owns the reset.

#### `void OnConversationStarted(string conversationId)`
Called when a conversation session begins via `ConversationManager.StartConversation()`.
Safe to leave empty if your game does not need to react to this.

#### `void OnCGUnlocked(string cgAddressableKey)`
Called when a message with `shouldUnlockCG = true` is processed.
Implement this to add the CG key to your gallery save data.

#### `void OnConversationEnded(string conversationId)`
Called when `ConversationManager.EndCurrentConversation()` runs.
Safe to leave empty if your game does not need to react to this.

#### `void OnChapterChanged(string conversationId, int chapterIndex, string chapterName)`
Called when `DialogueExecutor` advances to a new chapter file.
Safe to leave empty if your game does not need chapter transition UI.

---

## BubbleSpinnerParser

**File:** `BubbleSpinner/Core/BubbleSpinnerParser.cs`
**Namespace:** `BubbleSpinner.Core`
**Type:** `static class`

### Purpose
Reads a `.bub` `TextAsset` and produces a `Dictionary<string, DialogueNode>`
that `DialogueExecutor` uses to run conversations.
Runs entirely at parse time — no state is retained between calls.

### Public API

#### `Dictionary<string, DialogueNode> Parse(TextAsset bubbleFile, string expectedCharacterName = "")`
Parses a `.bub` file into a node dictionary.

| Parameter | Description |
|---|---|
| `bubbleFile` | The `.bub` TextAsset to parse. |
| `expectedCharacterName` | Optional. If provided, validates against the `contact:` header and logs a warning on mismatch. |

Returns an empty dictionary if `bubbleFile` is null or parsing fails entirely.
Individual line errors are caught and logged without stopping the parse.

### Parse Pipeline
1. Split file into lines
2. Strip inline comments (`//`)
3. For each line, try each parser method in order until one matches
4. `FinalizeParser()` — closes any open node or choice block
5. `ValidateDialogueGraph()` — checks all jump targets exist
6. `AssignNodeMessageIds()` — assigns deterministic IDs to all messages

### Message ID Format
Assigned after full parse so IDs are stable regardless of parse order.

| Message type | ID format |
|---|---|
| Node message | `{nodeName}_{messageIndex}` |
| Choice player message | `{nodeName}_choice{choiceIndex}_player{messageIndex}` |

### Cross-Chapter Jump Detection
Node names matching `_ch\d+` (case-insensitive) are treated as cross-chapter
targets. The validator suppresses "missing node" warnings for these names.

Examples that match: `Start_Ch2`, `Node_Ch12`, `End_CH3`
Examples that do not: `Fetch_ChocolateCake`, `ChapterIntro`, `StartCh2`

### Warnings vs Errors
The parser never throws on bad content — it logs a warning and continues.
This means a broken `.bub` file produces partial output rather than crashing.
Check the console for `[BubbleSpinner]` warnings when dialogue behaves unexpectedly.

---

## DialogueExecutor

**File:** `BubbleSpinner/Core/DialogueExecutor.cs`
**Namespace:** `BubbleSpinner.Core`
**Type:** `class` (not a MonoBehaviour)

### Purpose
Executes a parsed dialogue graph, managing message flow, pause points,
choices, node jumps, and chapter advances.
Communicates with UI exclusively through events — no direct UI references.

### Initialization

#### `void Initialize(ConversationAsset asset, ConversationState state, IBubbleSpinnerCallbacks callbacks = null)`
Must be called before any other method.
- Validates chapter index and resets to 0 if out of range
- Parses the current chapter via `BubbleSpinnerParser`
- Validates current node name and message index against parsed data
- Auto-corrects invalid state values with warnings rather than crashing

### Events

| Event | Signature | When fired |
|---|---|---|
| `OnMessagesReady` | `Action<List<MessageData>>` | A batch of messages is ready to display |
| `OnChoicesReady` | `Action<List<ChoiceData>>` | Choice buttons should be shown |
| `OnPauseReached` | `Action` | A tap-to-continue pause point was hit |
| `OnConversationEnd` | `Action` | No more content in current chapter or all chapters done |
| `OnChapterChange` | `Action<string>` | Executor advanced to a new chapter file |

> Always subscribe to events BEFORE calling `ContinueFromCurrentState()`.
> Events fired before subscription are lost.

### Public API — Flow Control

#### `void ContinueFromCurrentState()`
Single entry point for starting or resuming dialogue.
Reads `state.resumeTarget` to determine where to resume:

| ResumeTarget | Behavior |
|---|---|
| `None` | Fresh start — calls `ProcessCurrentNode()` |
| `Pause` | Restores pause button directly |
| `Interrupted` | Shows continue button if unread messages exist, otherwise skips to next action |
| `Choices` | Restores choice buttons directly |
| `End` | Restores end/next-chapter button directly |

#### `void OnPauseButtonClicked()`
Call when the player taps the continue/pause button.
If the pause point has a paired player message, emits it first then
continues. Otherwise resumes the next message batch directly.

#### `void OnChoiceSelected(ChoiceData choice)`
Call when the player selects a choice button.
Emits the choice's player messages (if any), then jumps to `choice.targetNode`.

#### `void OnMessagesDisplayComplete()`
Call when your UI finishes displaying a message batch (animations done).
BubbleSpinner uses this to determine what happens next — choices, pause,
jump, or end. Forgetting to call this freezes dialogue.

#### `void NotifyInterrupted()`
Call when the player exits the chat mid-message sequence (back button).
Sets `resumeTarget = Interrupted` so re-entry shows the continue button.

#### `void AdvanceToNextChapter()`
Call when the player taps the "Next Chapter" button.
Increments chapter index and loads the next `.bub` file.

### Properties

| Property | Type | Description |
|---|---|---|
| `IsInPauseState` | `bool` | Whether the executor is currently at a pause point |
| `CurrentNodeName` | `string` | Name of the currently executing node |
| `CurrentMessageIndex` | `int` | Index of the next unread message |
| `HasMoreChapters` | `bool` | True if more chapter files exist after the current one |
| `GetState()` | `ConversationState` | Returns the live state object for history loading |

### Next Action Priority
After each message batch, `DetermineNextAction()` checks in this order:
1. Pause point at current index → fire `OnPauseReached`
2. Choices on current node → fire `OnChoicesReady`
3. Auto-jump (`nextNode`) → call `JumpToNode()`
4. Nothing → fire `OnConversationEnd`

---

## ConversationManager

**File:** `BubbleSpinner/Core/ConversationManager.cs`
**Namespace:** `BubbleSpinner.Core`
**Type:** `MonoBehaviour`

### Purpose
Manages the full lifecycle of conversation sessions.
Owns session creation, save throttling, auto-save triggers, and cache eviction.
Acts as the main entry point your game code calls into BubbleSpinner.

### Initialization

#### `void Initialize(IBubbleSpinnerCallbacks callbacks)`
Must be called once at game start before any conversation is started.
Throws `ArgumentNullException` if callbacks is null.

### Public API

#### `DialogueExecutor StartConversation(ConversationAsset asset)`
Starts or resumes a conversation for the given asset.
- If no session exists: loads or creates `ConversationState`, creates `DialogueExecutor`, subscribes to executor events for auto-save
- If session already exists in cache: returns the existing executor
- Always sets `currentConversationId` and fires `OnConversationStarted`
- Returns the `DialogueExecutor` — subscribe to its events before calling `ContinueFromCurrentState()`

#### `void SaveCurrentConversation()`
Throttled save — skips if last save was less than 0.5 seconds ago.
Use during normal dialogue flow.

#### `void ForceSaveCurrentConversation()`
Immediate save — bypasses throttle.
Use on exit, app pause, chapter end, or any critical save point.

#### `void EndCurrentConversation()`
Force saves, unsubscribes from executor events, fires `OnConversationEnded`,
clears `currentConversationId`. Call when player leaves the chat.

#### `void EvictConversationCache(string conversationId)`
Clears the in-memory session for this conversation ID without touching disk.
Use this AFTER your save system has already wiped the disk data.
The next `StartConversation()` will load fresh from disk.

#### `void ResetConversation(string conversationId)`
Clears memory AND calls `DeleteConversationState()` on disk.
Use this only if BubbleSpinner fully owns persistence in your game.
Do NOT use if your save system zeroes state instead of deleting it —
use `EvictConversationCache()` instead.

### CG Gallery API

#### `List<string> GetUnlockedCGs(string conversationId)`
Returns unlocked CG paths for a specific conversation from the active session cache.
Returns empty list if no active session exists for this ID.

#### `List<string> GetAllUnlockedCGs()`
Returns all unlocked CGs across all active sessions.

#### `bool IsCGUnlocked(string conversationId, string cgPath)`
Returns true if the given CG path is unlocked in the active session for this conversation.

### Properties

| Property | Type | Description |
|---|---|---|
| `CurrentExecutor` | `DialogueExecutor` | The executor for the current active conversation, or null |
| `CurrentConversationId` | `string` | ID of the currently active conversation |
| `HasActiveConversation` | `bool` | True if there is a current active executor |

### Auto-Save Triggers
ConversationManager subscribes to executor events and auto-saves on:

| Executor event | Save type |
|---|---|
| `OnMessagesReady` | Throttled |
| `OnChoicesReady` | Throttled |
| `OnPauseReached` | Throttled |
| `OnConversationEnd` | Force |
| `OnChapterChange` | Force |

### Lifecycle Auto-Save
Also force-saves on:
- `OnApplicationPause(true)`
- `OnApplicationFocus(false)`
- `OnApplicationQuit()`

### Reset vs Evict

```
ResetConversation()      → clears memory + calls DeleteConversationState() on disk
EvictConversationCache() → clears memory only, disk already handled externally
```

Using the wrong one causes either a stale cache (served old progress after reset)
or a double-delete attempt (deleting data that is already gone).

---

## MessageData

**File:** `BubbleSpinner/Data/MessageData.cs`
**Namespace:** `BubbleSpinner.Data`
**Type:** `[Serializable] class`

### Purpose
Represents a single message in a conversation — text, image, or system.
Stored in `DialogueNode.messages`, `ChoiceData.playerMessages`, and `ConversationState.messageHistory`.

### Fields

| Field | Type | Description |
|---|---|---|
| `type` | `MessageType` | Text, Image, or System |
| `speaker` | `string` | Speaker name. Determines bubble alignment in UI. |
| `content` | `string` | Text content. Empty for Image type. |
| `imagePath` | `string` | Addressables key for Image type. Empty for Text/System. |
| `timestamp` | `string` | Assigned at parse time using `DateTime.Now`. Format: `HH:mm`. |
| `messageId` | `string` | Deterministic ID assigned by parser after full parse. Empty until then. |
| `shouldUnlockCG` | `bool` | If true, this image unlocks a CG when displayed. |

### MessageType Enum

| Value | Description |
|---|---|
| `Text` | Standard chat bubble |
| `Image` | Image bubble loaded via Addressables |
| `System` | Non-chat system message (timestamp, scene break) |

### Notes
- `messageId` is empty at construction and assigned by `AssignNodeMessageIds()` after the full parse completes.
- `timestamp` is assigned at parse time, not display time. Restored conversations show the load time.

---

## ChoiceData

**File:** `BubbleSpinner/Data/MessageData.cs`
**Namespace:** `BubbleSpinner.Data`
**Type:** `[Serializable] class`

### Purpose
Represents one player choice option — the button text, jump target,
and any player messages to emit when the choice is selected.

### Fields

| Field | Type | Description |
|---|---|---|
| `choiceText` | `string` | Text shown on the choice button |
| `targetNode` | `string` | Node name to jump to when this choice is selected |
| `playerMessages` | `List<MessageData>` | Messages emitted as the player after selecting this choice |

### Notes
- `playerMessages` is populated from `# Speaker: "text"` lines in the `.bub` choice block.
- If `targetNode` is empty, a warning is logged at parse time.
- If `playerMessages` is empty, `DialogueExecutor` jumps to `targetNode` immediately on selection.

---

## DialogueNode

**File:** `BubbleSpinner/Data/MessageData.cs`
**Namespace:** `BubbleSpinner.Data`
**Type:** `[Serializable] class`

### Purpose
Represents one named node in a `.bub` file — a flat list of messages,
optional choices, pause points, and an optional auto-jump target.

### Fields

| Field | Type | Description |
|---|---|---|
| `nodeName` | `string` | Unique name within the chapter file |
| `messages` | `List<MessageData>` | All messages in this node in order |
| `choices` | `List<ChoiceData>` | Choice options shown at end of node. Empty if no choices. |
| `pausePoints` | `List<PausePoint>` | All pause points in this node |
| `nextNode` | `string` | Auto-jump target after all messages. Empty if node ends with choices or conversation end. |

### Methods

#### `bool ShouldPauseAfter(int messageIndex)`
Returns true if a pause point exists with `stopIndex == messageIndex`.

#### `PausePoint GetPauseAt(int messageIndex)`
Returns the `PausePoint` at the given index, or null if none exists.

---

## PausePoint

**File:** `BubbleSpinner/Data/MessageData.cs`
**Namespace:** `BubbleSpinner.Data`
**Type:** `[Serializable] class`

### Purpose
Marks a position in a node's message list where the player must tap to continue.
Optionally pairs with a player message to emit when the player taps.

### Fields

| Field | Type | Description |
|---|---|---|
| `stopIndex` | `int` | Message index at which to pause. Flow stops before this index. |
| `playerMessageIndex` | `int` | Index of the player message to emit on continue. `-1` if none. |
| `HasPlayerMessage` | `bool` (property) | True if `playerMessageIndex >= 0` |

### Player-Turn Pause
When `-> ...` is followed by a `Player:` line in the `.bub` file, the parser
sets `playerMessageIndex` to the index where that player message lands.
Tapping continue emits the player message first, then resumes NPC dialogue.

### Pure Pacing Pause
When `-> ...` is not followed by a `Player:` line, `playerMessageIndex = -1`.
Tapping continue resumes the next NPC batch directly.

---

## ResumeTarget

**File:** `BubbleSpinner/Data/MessageData.cs`
**Namespace:** `BubbleSpinner.Data`
**Type:** `enum`

### Purpose
Records what UI state the conversation was in when the player exited.
Used by `DialogueExecutor.ContinueFromCurrentState()` to restore the
correct UI on re-entry without replaying messages.

### Values

| Value | Meaning | UI restored on re-entry |
|---|---|---|
| `None` | Fresh start or fully reset | Runs `ProcessCurrentNode()` from beginning |
| `Pause` | Exited at a tap-to-continue point | Restores pause/continue button |
| `Choices` | Exited at choice selection | Restores choice buttons |
| `End` | Exited at end or next-chapter button | Restores end/next-chapter button |
| `Interrupted` | Exited mid-message sequence | Shows continue button if unread messages remain |

---

## ConversationState

**File:** `BubbleSpinner/Data/MessageData.cs`
**Namespace:** `BubbleSpinner.Data`
**Type:** `[Serializable] class`

### Purpose
The full serializable save state for one conversation.
Stored in your save file and loaded by `IBubbleSpinnerCallbacks.LoadConversationState()`.

### Fields

| Field | Type | Description |
|---|---|---|
| `version` | `int` | Save format version. Current: `2`. |
| `conversationId` | `string` | Unique ID from `ConversationAsset.ConversationId` |
| `characterName` | `string` | Character display name. Preserved across resets. |
| `currentChapterIndex` | `int` | Index into `ConversationAsset.chapters` |
| `currentNodeName` | `string` | Name of the current node |
| `currentMessageIndex` | `int` | Next unread message index in the current node |
| `isInPauseState` | `bool` | Whether the conversation is currently paused |
| `resumeTarget` | `ResumeTarget` | UI state to restore on re-entry |
| `readMessageIds` | `List<string>` | IDs of all messages already displayed — prevents duplicates on resume |
| `messageHistory` | `List<MessageData>` | Full ordered history for UI reload on re-entry |
| `unlockedCGs` | `List<string>` | Addressables keys of all CGs unlocked in this conversation |

### Version History
| Version | Change |
|---|---|
| `1` | Initial format |
| `2` | Added `resumeTarget` for precise resume behavior |

### Notes
- `messageHistory` grows indefinitely. Save file size increases with long playthroughs.
- On reset, `characterName` should be preserved. Zero out all other fields rather than deleting the entry.

---

## ConversationAsset

**File:** `BubbleSpinner/Data/ConversationAsset.cs`
**Namespace:** `BubbleSpinner.Data`
**Type:** `ScriptableObject`

### Purpose
Defines one character and all their dialogue data.
Created in the Unity Editor and assigned to `CharacterDatabase`.
Passed into `ConversationManager.StartConversation()` to begin a conversation.

**Create via:** Right-click in Project → BubbleSpinner → Conversation Asset

### Fields

| Field | Type | Required | Description |
|---|---|---|---|
| `characterName` | `string` | ✓ | Display name. Must not be empty. |
| `profileImage` | `AssetReference` | ✓ | Addressables reference to profile picture. |
| `chapters` | `List<TextAsset>` | ✓ | `.bub` files in chapter order. At least one required. |
| `characterAge` | `string` | — | Shown in contact detail panel. Default: `"N/A"` |
| `birthdate` | `string` | — | Shown in contact detail panel. Default: `"N/A"` |
| `relationshipStatus` | `RelationshipStatus` | — | Enum. Default: `Unknown` (displays as N/A) |
| `occupation` | `string` | — | Shown in contact detail panel. Default: `"N/A"` |
| `bio` | `string` | — | Short tagline for contact list preview. Default: `"N/A"` |
| `description` | `string` | — | Longer background text. Default: `"N/A"` |
| `personalityTraits` | `string` | — | e.g. "Introverted, caring". Default: `"N/A"` |
| `cgAddressableKeys` | `List<string>` | — | All CG Addressables keys for this character |

### ConversationId
Auto-generated GUID-based ID in the format `{6-char-guid}_{characterName}`.
Generated once in the Editor on first access. **Do not modify.**
Used as the key for save state lookup across all systems.

### Profile Helper Methods
All return `"N/A"` if the field is empty or already set to `"N/A"`.

```csharp
GetAge()
GetBirthdate()
GetOccupation()
GetBio()
GetDescription()
GetPersonalityTraits()
GetRelationshipStatus()   // converts enum to display string
```

### RelationshipStatus Enum

| Value | Display string |
|---|---|
| `Unknown` | N/A |
| `Single` | Single |
| `InARelationship` | In a Relationship |
| `Married` | Married |
| `Divorced` | Divorced |
| `Widowed` | Widowed |
| `ItsComplicated` | It's Complicated |

---

## CharacterDatabase

**File:** `BubbleSpinner/Data/CharacterDatabase.cs`
**Namespace:** `BubbleSpinner.Data`
**Type:** `ScriptableObject`

### Purpose
A single ScriptableObject that holds references to all `ConversationAsset` files in the project.
Used by UI systems to populate contact lists and look up characters by ID or name.

**Create via:** Right-click in Project → BubbleSpinner → Character Database

### Fields

| Field | Type | Description |
|---|---|---|
| `allCharacters` | `List<ConversationAsset>` | All characters in the game |

### Query Methods

#### `ConversationAsset GetCharacterById(string conversationId)`
Returns the character with the matching `ConversationId`, or null if not found.

#### `ConversationAsset GetCharacterByName(string characterName)`
Returns the first character with the matching `characterName`, or null if not found.

#### `List<ConversationAsset> GetAllCharacters()`
Returns a shallow copy of `allCharacters`.

### Editor Tools

#### `[ContextMenu] Auto-Find All Characters`
Scans the entire project for `ConversationAsset` files using `AssetDatabase`
and populates `allCharacters` automatically.
Use this after adding new characters instead of manually dragging assets in.

### Validation (Editor Only)
`OnValidate()` runs in the Editor whenever the asset changes:
- Removes null entries from `allCharacters`
- Logs a warning for any duplicate `ConversationId` values

---

## Data Flow Summary

```
.bub file (TextAsset)
    ↓  BubbleSpinnerParser.Parse()
Dictionary<string, DialogueNode>
    ↓  DialogueExecutor.Initialize()
DialogueExecutor (runs the graph)
    ↓  fires events
Your UI (subscribes to executor events)
    ↓  calls back
DialogueExecutor.OnPauseButtonClicked()
DialogueExecutor.OnChoiceSelected()
DialogueExecutor.OnMessagesDisplayComplete()
    ↓  auto-save on each event
ConversationManager → IBubbleSpinnerCallbacks.SaveConversationState()
    ↓
Your save system (disk)
```