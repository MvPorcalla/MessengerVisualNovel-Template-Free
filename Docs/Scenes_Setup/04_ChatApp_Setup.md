# 04_ChatApp — Scene Setup Guide

---

## Overview

This guide covers the complete setup for the `04_ChatApp` scene from scratch: hierarchy, prefabs, script attachment, Inspector wiring, and final checks.

**Scripts involved:**

| Script | Namespace |
|---|---|
| `ChatAppController.cs` | `ChatSim.UI.ChatApp.Controllers` |
| `ChatTimingController.cs` | `ChatSim.UI.ChatApp.Controllers` |
| `ChatAutoScroller.cs` | `ChatSim.UI.ChatApp.Controllers` |
| `ChatMessageSpawner.cs` | `ChatSim.UI.ChatApp.Controllers` |
| `ChatChoiceSpawner.cs` | `ChatSim.UI.ChatApp.Controllers` |
| `ContactListPanel.cs` | `ChatSim.UI.ChatApp.Panels` |
| `ContactListItem.cs` | `ChatSim.UI.ChatApp.Panels` |
| `TextMessageBubble.cs` | `ChatSim.UI.ChatApp.Components` |
| `ImageMessageBubble.cs` | `ChatSim.UI.ChatApp.Components` |
| `ChoiceButton.cs` | `ChatSim.UI.ChatApp.Components` |
| `FullscreenCGViewer.cs` | `ChatSim.UI.ChatApp` |
| `ChatAppNavButtons.cs` | `ChatSim.UI.HomeScreen` |
| `PoolingManager.cs` | `ChatSim.UI.Common.Pooling` |
| `PooledObject.cs` | `ChatSim.UI.Common.Pooling` |
| `AutoResizeText.cs` | `ChatSim.UI.Common.Components` |

---

## Part 1 — Scene Hierarchy

Create the following hierarchy under your Canvas. Names must match exactly — scripts reference these by component, not by name, but keeping names consistent avoids confusion.

```
Canvas
├── ChatAppController           ← ChatAppController, ChatTimingController,
│                                  ChatAutoScroller, PoolingManager
└── PhoneRoot
    ├── ContactListPanel        ← ContactListPanel
    │   ├── Header
    │   │   └── Title
    │   └── ContactScroll       (ScrollRect)
    │       └── Viewport
    │           └── Content     ← populated at runtime
    │
    └── ChatAppPanel
        ├── ChatHeader
        │   ├── ChatBackButton
        │   ├── ChatProfileContainer
        │   │   └── ChatProfileIMG
        │   ├── ChatProfileName
        │   └── ChatModeToggle
        │       └── Icon
        │
        ├── ChatPanel           ← ChatMessageSpawner, ScrollRect
        │   ├── Viewport
        │   │   └── Content
        │   │       └── TypingIndicator     ← INACTIVE by default
        │   │           └── TypingBubble
        │   │               └── TypingText
        │   └── NewMessageIndicator         ← INACTIVE by default
        │       ├── IndicatorBackground
        │       └── IndicatorText
        │
        └── ChatChoices         ← ChatChoiceSpawner
            (empty at runtime — choices spawn here)

NavigationBar
└── ActionButton
    ├── QuitButton
    ├── HomeButton
    └── BackButton
```

> **Note:** `TypingIndicator` and `NewMessageIndicator` must be set **inactive** in the Scene (not just in code). Select each and uncheck the active checkbox in the Inspector.

---

## Part 2 — Prefab Setup

Create each prefab in `Assets/Prefabs/ChatApp/`. Build them as independent GameObjects, then save as prefabs via drag to the Project window.

---

### 2.1 Message Bubble Prefabs

There are five message bubble prefabs. All five follow the same setup pattern — only the child layout differs.

**All five prefabs require on their root:**
- `MessageBubble` (i.e. `TextMessageBubble.cs` or `ImageMessageBubble.cs` — see table below)
- `Canvas Group` component

| Prefab | Root Name | Script on Root | Text Child | Image Child |
|---|---|---|---|---|
| `SystemContainer.prefab` | `SystemContainer` | `TextMessageBubble` | `SystemMessage` (TMP) | — |
| `NpcChatContainer.prefab` | `NpcChatContainer` | `TextMessageBubble` | `NpcMessage` (TMP) | — |
| `NpcCGContainer.prefab` | `NpcCGContainer` | `ImageMessageBubble` | — | `NpcImage` (Image) |
| `PlayerChatContainer.prefab` | `PlayerChatContainer` | `TextMessageBubble` | `PlayerMessage` (TMP) | — |
| `PlayerCGContainer.prefab` | `PlayerCGContainer` | `ImageMessageBubble` | — | `PlayerImage` (Image) |

**Steps (repeat for each prefab):**

1. Create a new GameObject with the name matching the **Root Name** above.
2. Add the appropriate child GameObject(s) (`NpcMessage`, `NpcImage`, etc.).
3. On the root, add:
   - **Add Component → `TextMessageBubble`** (or `ImageMessageBubble` for CG prefabs)
   - **Add Component → `Canvas Group`**
4. On each **TMP child** (`NpcMessage`, `PlayerMessage`, `SystemMessage`), add:
   - **Add Component → `AutoResizeText`**
   - **Add Component → `Layout Element`** (required by `AutoResizeText`)

   `AutoResizeText` Inspector settings:
   ```
   maxWidth               → 650
   minWidth               → 40
   widthChangeThreshold   → 0.1
   ```
5. Wire Inspector fields on `TextMessageBubble`:
   ```
   messageText  → TMP child (e.g. NpcMessage)  — leave empty for image prefabs
   canvasGroup  → Canvas Group on this root
   ```
6. Wire Inspector fields on `ImageMessageBubble` (CG prefabs only):
   ```
   cgImage          → Image child (e.g. NpcImage)
   fullscreenViewer → None  (found automatically at runtime)
   ```
   Also on the root — set the **Button** component:
   ```
   Navigation  → None
   Transition  → None
   ```
7. Drag from Hierarchy into Project to save as prefab.
8. **Delete the instance from the scene.** `Content` must be empty (except `TypingIndicator`).

> **`PooledObject` on message bubble prefabs:** Do not add it manually. `PoolingManager` attaches it automatically on first use with `PreserveContent = false`, which is the correct behavior — their text and button listeners are cleared via `ResetForPool()` on recycle.

---

### 2.2 TypingIndicator Prefab

Create a separate prefab for the pooled typing indicator:

```
TypingIndicatorPrefab
└── TypingBubble
    └── TypingText  (TMP)
```

**Setup:**
1. On the root, add **`PooledObject.cs`**
2. Set `PreserveContent = ☑ true`
3. Save as `TypingIndicatorPrefab.prefab`

`ChatTimingController` pools and positions it via `PoolingManager`. `PreserveContent = true` tells the pool not to wipe its text/listeners on recycle — important since the typing indicator has no `ResetForPool()` method.

> All other prefabs (`NpcChatContainer`, `ChoiceButton`, etc.) do **not** need `PooledObject` pre-attached. `PoolingManager` adds it automatically on first use with `PreserveContent = false`.

Assign to `ChatTimingController → typingIndicatorPrefab` in the Inspector.

---

### 2.3 ChoiceButton Prefab

```
ChoiceButton (root)
└── ButtonText  (TMP)
```

**Setup:**
1. Add a **Button** component to root.
2. Add **`ChoiceButton.cs`** to root.
3. Wire Inspector:
   ```
   button      → Button on root
   buttonText  → ButtonText (TMP)
   ```
4. Save as `ChoiceButton.prefab`.

> A separate `ContinueButton.prefab` is optional — if left unassigned, `ChatChoiceSpawner` reuses `ChoiceButton.prefab` for pause/end buttons.

---

### 2.4 ContactListItem Prefab

```
ContactListItem (root)   ← Button component here
├── ProfileIMG           (Image)
├── InfoGroup            (GameObject)
│   ├── ProfileName      (TMP)
│   └── LastMessageText  (TMP — smaller font, grey color)
└── Badge                (GameObject — child with any visual)
```

**Setup:**
1. Add **Button** to root.
2. Add **`ContactListItem.cs`** to root.
3. Wire Inspector:
   ```
   button          → Button on root
   profileIMG      → ProfileIMG (Image)
   profileName     → InfoGroup/ProfileName (TMP)
   lastMessageText → InfoGroup/LastMessageText (TMP)
   badge           → Badge (GameObject)
   ```
4. Save as `ContactListItem.prefab`.

> `lastMessageText` displays the last message sent in the conversation — text messages truncate at 40 characters with ellipsis, image messages show `"Sent an image."` or `"You sent an image."`, and blank if the conversation has not started. If `lastMessageText` is left unassigned in the Inspector, the preview silently does nothing — no error.

---

## Part 3 — Script Attachment

Attach scripts to scene GameObjects (not prefabs).

| GameObject | Scripts to Attach |
|---|---|
| `ChatAppController` | `ChatAppController`, `ChatTimingController`, `ChatAutoScroller`, `PoolingManager` |
| `ChatPanel` | `ChatMessageSpawner` |
| `ChatChoices` | `ChatChoiceSpawner` |
| `ContactListPanel` | `ContactListPanel` |
| `NavigationBar` | `ChatAppNavButtons` |

> All four scripts on `ChatAppController` must be on the **same** GameObject. `ChatTimingController` and `ChatAutoScroller` use `GetComponent<ChatAppController>()` internally to find each other.

---

## Part 4 — Inspector Wiring

### ChatAppController

```
[Panels]
contactListPanel     → ContactListPanel (GameObject)
chatAppPanel         → ChatAppPanel (GameObject)

[Chat Header]
chatBackButton       → ChatBackButton (Button)
chatProfileIMG       → ChatProfileIMG (Image)
chatProfileName      → ChatProfileName (TMP)

[Chat Mode Toggle]
chatModeButton       → ChatModeToggle (Button)
chatModeIcon         → Icon (Image, child of ChatModeToggle)
fastModeSprite       → FastMode sprite (Project)
normalModeSprite     → NormalMode sprite (Project)

[Chat Display]
messageDisplay       → ChatPanel (drag ChatPanel — Unity finds ChatMessageSpawner on it)
choiceDisplay        → ChatChoices (drag ChatChoices — Unity finds ChatChoiceSpawner on it)

[Timing Controller]
timingController     → ChatAppController (drag ChatAppController — Unity finds ChatTimingController on it)

[Auto Scroll]
autoScroll           → ChatAppController (drag ChatAppController — Unity finds ChatAutoScroller on it)

[New Message Indicator]
newMessageIndicator  → NewMessageIndicator (GameObject)
newMessageButton     → NewMessageIndicator (Button on it)
newMessageText       → IndicatorText (TMP)
```

---

### ChatTimingController

```
[Timing Settings]
messageDelay              → 1.2
typingIndicatorDuration   → 1.5
playerMessageDelay        → 0.3
finalDelayBeforeChoices   → 0.2

[Fast Mode]
isFastMode                → ☐ (false)
fastModeSpeed             → 0.1

[References]
messageDisplay            → ChatPanel (drag ChatPanel)
typingIndicatorPrefab     → TypingIndicatorPrefab (from Project)
poolingManager            → ChatAppController (drag ChatAppController)
```

---

### ChatAutoScroller

```
[References]
chatScrollRect     → ChatPanel (drag ChatPanel — Unity finds ScrollRect on it)

[Settings]
autoScrollEnabled  → ☑ (true)
bottomThreshold    → 0.01
```

---

### ChatMessageSpawner

```
[Message Prefabs]
systemBubblePrefab       → SystemContainer.prefab
npcTextBubblePrefab      → NpcChatContainer.prefab
npcImageBubblePrefab     → NpcCGContainer.prefab
playerTextBubblePrefab   → PlayerChatContainer.prefab
playerImageBubblePrefab  → PlayerCGContainer.prefab

[Content Container]
chatContent              → Content (ChatPanel > Viewport > Content)

[Pooling]
poolingManager           → ChatAppController (drag ChatAppController)
prewarmCount             → 10
```

> `chatContent` must be the **Content** GameObject inside `Viewport` — not `Viewport` and not `ChatPanel`.

---

### ChatChoiceSpawner

```
[Prefabs]
choiceButtonPrefab    → ChoiceButton.prefab
continueButtonPrefab  → (optional) ContinueButton.prefab or leave None

[Container]
choiceContainer       → ChatChoices (this same GameObject)

[Pooling]
poolingManager        → ChatAppController (drag ChatAppController)
prewarmCount          → 4

[Auto Scroll]
autoScroller          → ChatAppController (drag — Unity finds ChatAutoScroller on it)
```

---

### ContactListPanel

```
[Database]
characterDatabase      → CharacterDatabase asset (from Project)

[UI References]
contactContainer       → Content (ContactScroll > Viewport > Content)
ContactListItemPrefab  → ContactListItem.prefab

[Controller Reference]
chatController         → ChatAppPanel (drag — Unity finds ChatAppController on it)
```

---

### ChatAppNavButtons

```
[Navigation Buttons]
homeButton              → HomeButton (Button)
backButton              → BackButton (Button)
quitButton              → QuitButton (Button)

[Quit Confirmation]
quitConfirmationPanel   → QuitConfirmationPanel (GameObject)
yesQuitButton           → YesQuitButton (Button)
noQuitButton            → NoQuitButton (Button)

[Chat App]
chatAppController       → ChatAppController (drag — Unity finds ChatAppController on it)
```

---

## Part 5 — Final Checklist

Run through this before entering Play Mode.

```
SCENE OBJECTS
☐ TypingIndicator         — inactive in scene
☐ NewMessageIndicator     — inactive in scene
☐ Content (ChatPanel)     — empty (no bubble instances left over)
☐ Content (ContactScroll) — empty (populated at runtime by ContactListPanel)

ChatAppController
☐ contactListPanel
☐ chatAppPanel
☐ chatBackButton
☐ chatProfileIMG
☐ chatProfileName
☐ chatModeButton
☐ chatModeIcon
☐ fastModeSprite
☐ normalModeSprite
☐ messageDisplay
☐ choiceDisplay
☐ timingController
☐ autoScroll
☐ newMessageIndicator
☐ newMessageButton
☐ newMessageText

ChatTimingController
☐ messageDisplay
☐ typingIndicatorPrefab
☐ poolingManager

Prefabs
☐ TypingIndicatorPrefab  — PooledObject attached, PreserveContent = ☑ true
☐ All message bubbles    — NO PooledObject pre-attached (added automatically at runtime)

ChatAutoScroller
☐ chatScrollRect

ChatMessageSpawner
☐ systemBubblePrefab
☐ npcTextBubblePrefab
☐ npcImageBubblePrefab
☐ playerTextBubblePrefab
☐ playerImageBubblePrefab
☐ chatContent

ChatChoiceSpawner
☐ choiceButtonPrefab
☐ choiceContainer
☐ autoScroller

ContactListPanel
☐ characterDatabase
☐ contactContainer
☐ ContactListItemPrefab
☐ chatController

ContactListItem.prefab
☐ Button on root
☐ ContactListItem.cs attached
☐ button assigned
☐ profileIMG assigned
☐ profileName assigned (InfoGroup/ProfileName)
☐ lastMessageText assigned (InfoGroup/LastMessageText)
☐ badge assigned
☐ Prefab saved

ChatAppNavButtons
☐ homeButton
☐ backButton
☐ quitButton
☐ quitConfirmationPanel
☐ chatAppController
```

---

## Part 6 — Common Mistakes

**`messageDisplay` / `choiceDisplay` shows None after drag**
Drag the **GameObject** (`ChatPanel`, `ChatChoices`) — not a script file from the Project window. Unity automatically finds the component attached to that GameObject.

**`timingController` / `autoScroll` shows None**
Both scripts are on `ChatAppController`. Drag the `ChatAppController` **GameObject** into both fields — Unity resolves the component from the same object.

**`chatContent` assigned but messages spawn in wrong place**
`chatContent` must point to the `Content` child inside `ChatPanel > Viewport > Content`. Do not assign `Viewport` or `ChatPanel` itself.

**`choiceContainer` shows None**
Drag `ChatChoices` itself (the same GameObject `ChatChoiceSpawner` is on) into the field.

**Stale message bubbles visible on scene open**
Any bubble instances left in `Content` from prefab testing will persist at runtime. Delete all children of `Content` except `TypingIndicator` before saving the scene.

**Contacts don't populate**
`characterDatabase` must be assigned on `ContactListPanel`. The list is built in `OnEnable()` — if the database is null when the panel activates, nothing spawns.

**Last message preview doesn't update after returning from chat**
`ContactListPanel` rebuilds the list in `OnEnable()` — this fires automatically when `SwitchToContactList()` calls `contactListPanel.SetActive(true)`. If the preview is stale, confirm `OnEnable()` is not being suppressed by the panel already being active when the scene loads.

**Last message preview shows blank for all contacts**
`lastMessageText` on `ContactListItem` is not assigned in the prefab Inspector. Open the prefab, find `InfoGroup/LastMessageText`, and wire it to the `lastMessageText` field on `ContactListItem`.

**Last message preview shows wrong text**
`GetLastMessagePreview()` walks `messageHistory` backwards and skips system messages and image messages. If the preview looks wrong, check the `messageHistory` list in save data — open the save file at `Application.persistentDataPath/Saves/ChatSimData/game_save.json` and inspect the `messageHistory` array for that conversation.

**CG images show blank on click**
`ImageMessageBubble` needs a valid Addressable key set in the dialogue data (`path:` field). If the key is missing or misspelled, the load fails silently. Check the Console for `[ImageMessageBubble] ✗ Load failed`.

**Typing indicator content gets wiped on recycle**
`PooledObject` on `TypingIndicatorPrefab` must have `PreserveContent = ☑ true`. If it's false (or `PooledObject` is missing), `PoolingManager` clears all TMP text on recycle, causing the indicator to appear blank on reuse.

**Message bubbles show wrong width on pool reuse**
`AutoResizeText` is required on the TMP child of each text bubble prefab (`NpcMessage`, `PlayerMessage`, `SystemMessage`). It needs both a `TextMeshProUGUI` and a `LayoutElement` on the same GameObject. If either is missing, `[AutoResize]` errors will appear in the Console and bubbles will render at incorrect widths.

**Choices overlap last message bubble in normal mode**
`autoScroller` must be assigned on `ChatChoiceSpawner`. Without it, the spawner 
cannot force scroll to bottom after choices appear, causing overlap. 
Drag the `ChatAppController` GameObject into the `autoScroller` field — 
Unity resolves the `ChatAutoScroller` component from it.