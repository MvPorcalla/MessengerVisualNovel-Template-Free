# AI Project Instructions — Phone Chat Simulation Game (Unity)

## Important Rule (Strict)

Before refactoring, reviewing, or suggesting changes:

* Ask for the full script and any related or dependent scripts required to understand the full context.
* Do **not assume, infer, or invent missing code**.
* Only use **code explicitly provided**.
* Keep explanations **minimal and concise**.
* Provide **only the code snippet of the fix with a short guide**.
* Output a **full updated script only if explicitly requested**.

---

Build Setting:

00_Disclaimer
01_Bootstrap
02_Custscene
03_Phonescreen
04_ChatApp

---

# BubbleSpinner Dialogue Engine

**BubbleSpinner** is a standalone, UI-agnostic dialogue engine responsible for parsing and executing branching chat conversations.

Supported dialogue elements may include:

* Text messages
* Dialogue choices
* Media attachments
* Conditional logic
* Node jumps
* State-driven branching

BubbleSpinner is designed to be **fully independent of Unity UI and scene logic**.

The engine must remain **data-driven and reusable across multiple projects**.

---

# Engine Responsibilities

BubbleSpinner is responsible for:

* Parsing dialogue data
* Executing dialogue nodes
* Handling branching logic
* Managing message flow
* Triggering dialogue events

BubbleSpinner is **not responsible for**:

* Rendering UI
* Message layout
* Scroll behavior
* Phone app navigation
* Asset loading for UI presentation

---

# UI Communication Layer

All communication between the dialogue engine and the UI layer occurs through:

**Interface**

```
IBubbleSpinnerCallbacks
```

**Bridge**

```
BubbleSpinnerBridge
```

The bridge translates engine events into UI actions.

This ensures the dialogue engine remains **fully decoupled from UI implementation**.

---

# Game Architecture Overview

The project follows a **UI-first, system-driven architecture** designed for narrative-driven chat gameplay.

The core template must remain **modular and reusable**, supporting multiple story projects built on the same system.

Placeholder sections below will define project structure and implementation rules.


[Folder Structure]

```
Assets/Scripts/
├── BubbleSpinner/                        # Standalone dialogue engine — no Unity UI dependencies
│   ├── Core/
│   │   ├── IBubbleSpinnerCallbacks.cs    # Interface — save, load, events contract for the bridge
│   │   ├── BubbleSpinnerParser.cs        # Parses .bub text files into structured dialogue data
│   │   ├── DialogueExecutor.cs           # Executes parsed nodes — fires message, choice, media events
│   │   └── ConversationManager.cs        # Manages active conversations, executor lifecycle, cache
│   └── Data/
│       ├── MessageData.cs                # Data model for a single message (sender, text, type)
│       ├── ConversationAsset.cs          # ScriptableObject — character data, chapters, CG keys
│       └── CharacterDatabase.cs          # ScriptableObject — registry of all ConversationAssets
│
└── ChatSim/
    ├── Core/
    │   ├── AddressablesImageLoader.cs    # Async CG image loader via Addressables
    │   ├── BubbleSpinnerBridge.cs        # Implements IBubbleSpinnerCallbacks — save/load/reset
    │   ├── GameBootstrap.cs              # Entry point — initializes all managers, loads first scene
    │   ├── GameEvents.cs                 # Static event bus — decoupled cross-system communication
    │   ├── SaveManager.cs                # Reads and writes save data to disk with backup recovery
    │   ├── SceneFlowManager.cs           # Handles scene transitions and load validation
    │   └── SceneNames.cs                 # String constants for all scene names
    │
    ├── Data/
    │   └── SaveData.cs                   # Serializable save data model — conversation states, CG unlocks
    │
    └── UI/
        ├── ChatApp/
        │   ├── Components/
        │   │   ├── TextMessageBubble.cs          # Text bubble — auto-resizes to content
        │   │   ├── ImageMessageBubble.cs          # CG image bubble — tap to open fullscreen viewer
        │   │   └── ChoiceButton.cs                # Individual choice button — fires selection event on click
        │   ├── Controllers/
        │   │   ├── ChatAppController.cs           # Main controller — receives engine events, drives UI
        │   │   ├── ChatAutoScroller.cs            # Scrolls chat to latest message automatically
        │   │   ├── ChatChoiceSpawner.cs           # Spawns and clears choice buttons from pool
        │   │   ├── ChatMessageSpawner.cs          # Spawns message bubbles from pool into chat content
        │   │   └── ChatTimingController.cs        # Queues messages with typing indicator delays
        │   ├── Panels/
        │   │   ├── ContactListPanel.cs            # Populates and displays the contact list
        │   │   └── ContactListItem.cs             # Individual contact row — opens conversation on tap
        │   ├── ChatAppNavButtons.cs               # Navigation bar buttons for the chat app scene
        │   └── FullscreenCGViewer.cs              # Fullscreen overlay for tapped CG images
        │
        ├── Common/
        │   └── Components/
        │       ├── AutoResizeText.cs              # Resizes TMP bubble width to fit text content
        │       ├── PooledObject.cs                # Marks a GameObject as poolable — handles return logic
        │       └── PoolingManager.cs              # Object pool — spawns and recycles UI prefabs
        │
        ├── HomeScreen/
        │   ├── Contacts/
        │   │   ├── ContactsAppDetails.cs          # Character detail view — profile, bio, traits (future)
        │   │   ├── ContactsAppItems.cs            # Individual contact row in the contacts app
        │   │   ├── ContactsAppPanels.cs           # Manages contacts panel display and population
        │   │   └── ResetConfirmationDialog.cs     # Confirmation dialog before resetting a character story
        │   ├── Gallery/
        │   │   ├── GalleryController.cs           # Populates gallery grid from save data CG unlocks
        │   │   ├── GalleryFullscreenViewer.cs     # Fullscreen CG viewer for the gallery panel
        │   │   └── GalleryThumbnailItems.cs       # Individual CG thumbnail — locked/unlocked state
        │   ├── HomeScreenController.cs            # Home screen app launcher — opens scenes or panels
        │   └── HomeScreenNavButtons.cs            # Navigation bar buttons for the phone home screen
        │
        └── Screens/
            ├── DisclaimerScreen.cs                # First-launch TOS screen — skipped after acceptance
            └── LockScreen.cs                      # Lock screen entry point — tap to unlock
```

---

# Game Type Constraints

Platform: **Mobile**

Engine: **Unity**

Gameplay is **entirely UI-driven**.

The game does **not include**:

* Character movement
* Physics gameplay
* World exploration

Player interaction occurs through:

* Chat messages
* Dialogue choices
* Phone app navigation
* State-based story progression

---

# Dialogue Data

Dialogue content must be **data-driven**.

The dialogue engine reads structured dialogue files which define:

* Messages
* Choices
* Branching paths
* Conditional events

---

## Syntax Legend

| Symbol | Meaning |
|--------|---------|
| `contact:`                      | Optional file metadata — declares the character name                   |
| `chapter:`                      | Required file metadata — declares the chapter ID for cross-chapter jumps |
| `title:`                        | Declares a new node — must be unique within the file                   |
| `---`                           | Opens or closes a node — first `---` opens, second `---` closes        |
| `...`                           | Pure pacing pause — tap to continue, nothing sent                      |
| `Speaker: "text"`               | NPC message bubble                                                     |
| `Player: "text"`                | Player message — implicit pause point, tap sends then NPC continues    |
| `System: "text"`                | Non-chat system message (timestamps, scene breaks)                     |
| `>> media`                      | Image bubble command                                                   |
| `>> choice`                     | Opens a choice block — must be at indent 0                             |
| `>> endchoice`                  | Closes a choice block — required, must be at indent 0                  |
| `-> "text"`                     | Choice button — must be inside `>> choice` at indent 1                 |
| `Speaker: "text"`               | Pre-jump dialogue inside a choice option — indent 2, before `<<jump>>` |
| `>> media`                      | Pre-jump media inside a choice option — indent 2, before `<<jump>>`    |
| `<<jump NodeName>>`             | Local jump — stays within the current chapter file                     |
| `<<jump chapter:ChapterId>>`    | Chapter jump — loads a new chapter file, enters at Start node          |
| `<<jump chapter:ChapterId node:NodeName>>` | Chapter jump — loads a new chapter file, enters at specific node |
| `//`                            | Comment — inline or full line, stripped by parser                      |
| `>> END`                        | Marks a node as an intentional conversation end — no jump required, suppresses missing-jump warning |

---

## File Structure
```
contact: Fern                                                   // optional — validated against ConversationAsset.characterName
chapter: Ch1                                                    // required — must match ChapterEntry.chapterId in ConversationAsset

title: Start                                                    // declares node named "Start"
---                                                             // opens node content
System: "7:15 AM"                                               // system message — timestamp, scene label, etc.

Fern: "Good morning."                                           // NPC message bubble
Fern: "I hope I'm not disturbing you this early."

>> media npc type:image unlock:true path:Fern/CG1               // image bubble — unlock:true adds to gallery

...                                                             // pure pacing pause — tap to continue, nothing sent

Fern: "Still, there are moments when I feel..."

Player: "I'm sure she notices"                                  // implicit pause point — tap sends this message, then NPC continues

Fern: "...Perhaps you're right."

>> choice                                                       // opens choice block — must be at indent 0
    -> "Lonely?"                                                // choice button — indent 1
        Player: "Test Choice Dialogue"                          // pre-jump dialogue — indent 2, shows before jumping
        <<jump Node_Loneliness>>                                // local block jump — indent 2
    -> "Unappreciated?"                                         // second choice option — indent 1
        Player: "Test Choice Dialogue"
        <<jump Node_Appreciation>>
>> endchoice                                                    // closes choice block — required, must be at indent 0

---                                                             // closes node "Start"


title: Node_Loneliness                                          // declares next node
---                                                             // opens node content
Fern: "..."
<<jump chapter:Ch2>>                                            // chapter jump — loads Ch2, enters at Start
---                                                             // closes node


title: Node_Appreciation
---
Fern: "..."
<<jump chapter:Ch2 node:Branch_A>>                              // chapter jump — loads Ch2, enters at Branch_A
---

```

---

# Unity Packages

The project depends on the following Unity packages:

* **TextMeshPro (TMP)** — text rendering
* **Newtonsoft.Json** — dialogue parsing
* **Addressables** — asset loading and content management

---

# Design Principles

The system must follow these core principles:

* **Engine and UI must remain decoupled**
* **Dialogue must remain data-driven**
* **Systems must remain modular and reusable**
* **Chat UI must support scalable conversation sizes**