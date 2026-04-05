# Messenger Style Visual Novel Template for Unity

## Free Version (Limited Features)

A modular **Unity template** for building **narrative-driven mobile games** with a **phone chat messenger interface.** Built on the **BubbleSpinner** dialogue engine — a standalone, data-driven conversation system that handles branching dialogue, media messages, choices, and save/resume state.

It powers messenger-style storytelling with branching dialogue, CG unlocks, and persistent save states—designed specifically for mobile-first visual novels.

---

## 📦 Requirements

## Unity Project Requirements

* **Engine:** Unity 2022.3.62f2 LTS (2D)
* **Target Platform:** Mobile (primary), PC support may come later
* **Version Control:** GitHub (Git)

## Packages:
* **TextMeshPro**
* **Addressables**
* **Newtonsoft.Json**

---

## What's Included

### BubbleSpinner — Dialogue Engine
A standalone, UI-agnostic dialogue engine. Parses `.bub` dialogue files and executes branching conversations with full save/resume support.

- Text messages, player choices, media/CG images
- Pause points, node jumps, cross-chapter navigation
- Deterministic message IDs for reliable save state
- Fully decoupled from Unity UI

### ChatSim — Game Layer
The full phone simulation built on top of BubbleSpinner.

- Animated chat message display with typing indicators
- Contact list with conversation selection
- CG gallery with unlock tracking
- Contacts app with per-character story reset
- Atomic save system with backup recovery
- Scene flow management across 5 scenes

---

## Scene Structure

```
00_Disclaimer    → Terms of service (first launch only)
01_Bootstrap     → Manager initialization (persistent)
02_Lockscreen    → Entry point after bootstrap
03_PhoneScreen   → Home screen and app launcher
04_ChatApp       → Chat interface
```

---

## Quick Start

Create a `ConversationAsset`, write a `.bub` dialogue file, add it to the `CharacterDatabase`, and press Play.

→ [Full Quick Start Guide](Docs/QuickStart.md)

---

## Architecture Overview

```
┌─────────────────────────────────────┐
│           BubbleSpinner             │  Standalone dialogue engine
│  Parser → Executor → Manager        │  No Unity UI dependencies
└────────────────┬────────────────────┘
                 │ IBubbleSpinnerCallbacks
┌────────────────▼────────────────────┐
│         BubbleSpinnerBridge         │  Persistence layer
│  Save / Load / Delete / Reset       │  Connects engine to ChatSim
└────────────────┬────────────────────┘
                 │ GameEvents
┌────────────────▼────────────────────┐
│             ChatSim                 │  Game layer
│  Bootstrap → SaveManager → UI       │  Scene flow, phone UI, gallery
└─────────────────────────────────────┘
```

---

## Dialogue Format (.bub)

```
contact: Sofia

title: Start
---
Sofia: "Hey, are you there?"
Sofia: "I need to talk to you."

-> ...

Player: "..."

>> choice
    -> "What's wrong?"
        # Player: "What's wrong? You sound worried."
        <<jump Node_Concern>>

    -> "Not now"
        # Player: "Can't talk right now."
        <<jump Node_Dismiss>>

===

title: Node_Concern
---
Sofia: "It's nothing. Never mind."

<<jump EndNode>>
```

See [.bub Format Reference](Assets/Scripts/BubbleSpinner/Docs/FORMAT.md) for the full syntax guide.

---

## Documentation

### Getting Started
- [Quick Start](Docs/QuickStart.md) — Add a character and test it in Play Mode
- [Addressables Setup](Docs/Addressables_Setup.md) — Setting up CG images with Addressables

### Project
- [Project Structure](Docs/Project_Structure.md) — Full folder and file map

### Scene Setup
- [Scene Overview](Docs/Scenes_Setup/Scene_Overview.md) — Build settings, scene order, and per-scene summary
- [00_Disclaimer Setup](Docs/Scenes_Setup/00_Disclaimer_Setup.md) — First-launch TOS flow
- [01_Bootstrap Setup](Docs/Scenes_Setup/01_Bootstrap_Setup.md) — Core manager initialization
- [02_Lockscreen Setup](Docs/Scenes_Setup/02_Lockscreen_Setup.md) — Lock screen entry point
- [03_PhoneScreen Setup](Docs/Scenes_Setup/03_PhoneScreen_Setup.md) — Gallery, contacts, navigation, home screen
- [04_ChatApp Setup](Docs/Scenes_Setup/04_ChatApp_Setup.md) — Chat UI, message spawning, timing, scroll

### BubbleSpinner
- [BubbleSpinner Code Reference](Assets/Scripts/BubbleSpinner/Docs/BubbleSpinner.md) — Full script documentation
- [.bub Format Reference](Assets/Scripts/BubbleSpinner/Docs/FORMAT.md) — Complete `.bub` syntax guide

---

## Goals

- Rapid narrative prototyping
- Scalable multi-character visual novel architecture
- Reusable dialogue engine with zero game-specific dependencies
- Clean separation between engine, UI, and game logic

Built as a **foundation**, not a one-off game.

---

## License

MIT License — see [LICENSE](LICENSE) for details.

---

## Contact

**Melvin Porcalla**
GitHub: [MvPorcalla](https://github.com/MvPorcalla)
Email: scryptid1@gmail.com

---

*Built for narrative-first developers who care about structure, performance, and clean systems.*