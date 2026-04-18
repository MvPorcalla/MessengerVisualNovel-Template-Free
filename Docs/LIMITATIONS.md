# BubbleSpinner — Known Limitations

This document tracks confirmed limitations of the BubbleSpinner dialogue engine and the ChatSim UI layer.
Each entry includes a symptom, root cause, and suggested fix where applicable.

---

## Parser Limitations

### No Audio or Video Media Support
**Symptom:** `>> media` command only supports images. Sending audio or video files is not possible.
**Root cause:** `TryParseMediaCommand` only handles `type:image`. No other media types are parsed.
**Fix:** Extend `MessageData.MessageType` to include `Audio` and `Video`, and add type dispatch in `TryParseMediaCommand` and `ChatMessageSpawner`.

---

### No Conditional Logic (`<<if>>` / `<<else>>` / `<<endif>>`)
**Symptom:** Branching based on game state (e.g. `hasMet == true`) is not possible in `.bub` files.
**Root cause:** The parser is a flat state machine with no scope stack. Nested conditional blocks are not supported.
**Fix:** Replace flat indent checks with a `BlockScope` stack. Add `TryParseIfBlock`, `TryParseElseBlock`, `TryParseEndIf`. Requires a new `ConditionData.cs` and `IConditionEvaluator` interface. See TODO block in `BubbleSpinnerParser.cs`.

---

### No Variables or State Flags
**Symptom:** Cannot read or write game state variables from `.bub` files (e.g. set a flag when a choice is picked).
**Root cause:** No variable system exists in the parser or executor.
**Fix:** Requires a `DialogueVariables` store and expression evaluator, likely tied to the `<<if>>` implementation above.

---

### No Nested Choice Blocks
**Symptom:** A `>> choice` block inside another `>> choice` block is not supported.
**Root cause:** The parser's indent system only supports 3 levels (0, 1, 2). A nested choice would require level 3+.
**Fix:** Blocked until the scope stack refactor (see conditional logic above) is implemented.

---

### No Inline Text Formatting
**Symptom:** Bold, color, or sized text inside dialogue strings is not supported.
**Root cause:** Message content is stored as a plain string. No markup is parsed or passed to TMP.
**Fix:** Parse TMP-compatible rich text tags inside dialogue content strings and pass them through `MessageData.content` as-is.

---

### Indent System Hard-Capped at 3 Levels
**Symptom:** Content deeper than indent level 2 is either ignored or produces a warning.
**Root cause:** `MeasureIndent` is correct, but all dispatch handlers explicitly check for levels 0, 1, and 2 only.
**Fix:** Implement the scope stack. Until then, do not author `.bub` files with content deeper than indent 2.

---

## Save System Limitations

### Pending Display Buffer Is Not Serialized
**Symptom:** If the game is quit mid-animation (NPC typing in progress), the exact in-flight message batch is lost on cold restart. The executor recovers by re-collecting unread messages from `currentMessageIndex`, but the animation restarts from the beginning of the batch, not the exact frame it was interrupted.
**Root cause:** `pendingDisplayMessages` is a runtime-only list and is not written to `ConversationState` or disk.
**Fix:** Serialize `pendingDisplayMessages` and `pendingNextMessageIndex` into `ConversationState`. Bump `CURRENT_VERSION`.

---

### No Save Migration
**Symptom:** If `ConversationState.CURRENT_VERSION` is bumped without a migration path, old saves loaded from disk may silently produce incorrect state or missing fields.
**Root cause:** `LoadGame` deserializes the save directly with no version check or field migration step.
**Fix:** Add a `MigrateSaveData(SaveData data, int fromVersion)` method in `SaveManager` that upgrades old saves field by field before use.

---

### Single Save Slot Only
**Symptom:** There is one save file per installation. Multiple playthroughs, profiles, or cloud sync are not supported.
**Root cause:** `SaveFilePath` is a single hardcoded path. `SaveData` is a single object.
**Fix:** Parameterize the save path by slot index or user profile ID.

---

## Conversation System Limitations

### Manual Executor Wiring
**Symptom:** `ChatAppController` must subscribe and unsubscribe from `DialogueExecutor` events manually every time a conversation starts or ends. Adding a second UI layer or porting to a new project requires repeating this wiring.
**Root cause:** `ConversationManager.StartConversation()` returns the raw `DialogueExecutor` rather than exposing passthrough events.
**Fix:** Move `OnMessagesReady`, `OnChoicesReady`, `OnPauseReached`, and `OnConversationEnd` onto `ConversationManager` as passthrough events. Outside code subscribes to the manager, never to the executor directly.

---

### No Cross-Chapter Message Deduplication
**Symptom:** When a chapter jump occurs, `readMessageIds` is cleared. If a node's messages happen to share an ID with a previous chapter (unlikely but possible with deterministic ID assignment), they could be skipped or double-shown.
**Root cause:** `LoadChapterById` calls `state.readMessageIds.Clear()` unconditionally.
**Fix:** Prefix message IDs with the chapter ID: `{chapterId}_{nodeName}_{index}`. This makes IDs globally unique across chapters.

---

### No Branching History
**Symptom:** There is no way to query which choices the player previously made, or replay a summary of past decisions.
**Root cause:** `resolvedChoiceBlockIds` only tracks whether a block was resolved, not which option was selected.
**Fix:** Store `selectedChoiceText` or a choice index alongside each resolved block ID in `ConversationState`.

---

### No Node Skip or Fast-Forward
**Symptom:** There is no way to jump the player directly to a specific node from outside the engine (e.g. debug tools, chapter select).
**Root cause:** `ContinueFromCurrentState` always resumes from the saved state. There is no public API to override the current node.
**Fix:** Add a `JumpToNode(string nodeId)` method on `DialogueExecutor` that sets `state.currentNodeName` and calls `ProcessCurrentNode()`. Guard with a dev-only flag if needed.

---

## UI Limitations

### FPS Drop on Bulk Spawn
**Symptom:** FPS drops to ~20 when loading 600+ chat bubbles on conversation resume. Recovers after spawn completes.
**Root cause:** All bubbles instantiate in a single frame during `LoadConversationHistory`, triggering a mass layout rebuild.
**Fix:** Spread instantiation across frames using a coroutine with a `yield` per batch in `ChatMessageSpawner`. Touch `ChatTimingController.cs` and `ChatMessageSpawner.cs`.

---

### No Unread Badge Count on Contact List
**Symptom:** The contact list has no visual indicator showing how many new messages are available per character.
**Root cause:** `ContactListPanel` reads character data from `ConversationAsset` but has no access to unread message counts.
**Fix:** Compare `state.messageHistory.Count` against a stored "last seen" count in `ConversationState` and surface the delta as a badge.

---

### No Rich Text or Emoji Sizing in Bubbles
**Symptom:** TMP rich text tags (`<b>`, `<color>`, `<size>`) inside dialogue strings render as literal text, not formatting.
**Root cause:** `AutoResizeText` and `TextMessageBubble` pass content strings directly to TMP without enabling rich text on the component.
**Fix:** Enable `richText` on the TMP component in `TextMessageBubble`. Ensure `AutoResizeText` measures text width using the rendered output, not the raw string length.

---

### Missing Emoji Glyph Support (Font Limitation)
**Symptom:** Some emojis do not render in chat bubbles and appear as missing characters (□ or ?).
**Root cause:** The current TextMeshPro font asset does not include full Unicode emoji glyph coverage. TMP only renders characters present in the assigned font or its fallback chain.
**Fix:** Use a font asset with broader Unicode/emoji support (e.g. Noto Color Emoji or equivalent) and configure it as a fallback in TMP. Alternatively, create a TMP fallback font asset that includes emoji ranges and assign it in the TMP Font Asset fallback list.

---

### Addressables Setup Required for CG Images
**Symptom:** CG images do not load unless the Addressables system is configured and groups are built. Missing setup produces silent failures with no fallback image.
**Root cause:** `AddressablesImageLoader` and `ChatAppController.LoadChatProfileImage` use Addressables handles with no fallback path.
**Fix:** Add a fallback sprite reference on `ConversationAsset` that is used when the Addressables load fails or the key is invalid.

---

*Last updated: based on codebase snapshot provided. Bump this document when limitations are resolved or new ones are confirmed.*