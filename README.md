# BubbleSpinner — Messenger Style Visual Novel Template for Unity

![Unity](https://img.shields.io/badge/Unity-2022.3.62f2_LTS-black?logo=unity) ![Platform](https://img.shields.io/badge/Platform-Mobile-brightgreen) ![License](https://img.shields.io/badge/License-MIT-blue) ![Status](https://img.shields.io/badge/Status-Active_Development-orange)

A modular **Unity template** for building **narrative-driven mobile games** with a **phone chat messenger interface.**

Built on **BubbleSpinner** — a standalone, data-driven dialogue engine that handles branching dialogue, media messages, choices, and save/resume state. It powers messenger-style storytelling with CG unlocks and persistent save states, designed specifically for mobile-first visual novels.

---

## ⚠️ Current Limitations

### 📌 Note: BubbleSpinner is currently in active development. The architecture prioritizes stability of dialogue flow and resume correctness over feature completeness. Systems such as variables, conditional logic, and advanced save features are planned for future iterations.

> This is the **free version** of the template. The features below are known limitations of the current release. Most are planned to be addressed in a future or upcoming updates.

### 🧠 BubbleSpinner (Core Engine)
- No Variables or State System
- No Conditional Logic in `.bub` Files (`<<if>>` / `<<else>>` / `<<endif>>`)
- No Nested Choice Blocks
- No Cross-Chapter History Tracking
- No External Debug Control API

### 💾 Save System
- No Save Migration System
- Single Save Slot Only
- Pending Message State Not Fully Persistent

### 🎮 UI
- Manual UI Wiring Required
- No Rich Text or Emoji Formatting in Bubbles
- FPS Drops on Large Message History Loads
- No Unread Message Badge on Contact List
- Addressables Setup Required for CG Images — No Fallback on Load Failure

→ [Full Limitations Reference with root causes and fixes](Docs/LIMITATIONS.md)

---

### 🐛 Known Bugs & Stability Notes

- Some edge cases or bugs may still exist that have not yet been discovered or resolved.  
- Due to ongoing development, certain fixes may introduce unintended side effects in other flows.  
- If you encounter any issues, feedback or reports are highly appreciated and help improve system stability.

---

## 📦 Requirements

| | |
|---|---|
| **Engine** | Unity 2022.3.62f2 LTS (2D) |
| **Platform** | Mobile (primary) |
| **Version Control** | GitHub (Git) |
| **Packages** | TextMeshPro, Addressables, Newtonsoft.Json |

---

## ⚡ Quick Start

1. Open the project in Unity
2. Create a `ConversationAsset` ScriptableObject
3. Write a `.bub` dialogue file and assign it to the asset
4. Add the asset to `CharacterDatabase`
5. Press Play

→ [Full Quick Start Guide](Docs/QuickStart.md)

---

## 🏗️ Architecture Overview

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

BubbleSpinner has **zero dependencies on Unity UI or game logic.** It communicates outward only through `IBubbleSpinnerCallbacks` and events. This makes it portable to any project.

---

## 📂 Scene Structure

```
00_Disclaimer    → Terms of service (first launch only)
01_Bootstrap     → Manager initialization (persistent)
02_Lockscreen    → Entry point after bootstrap
03_PhoneScreen   → Home screen and app launcher
04_ChatApp       → Chat interface
```

---

## 🧩 What's Included

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
- Scene flow management

---

## ✍️ Dialogue Format (.bub)

```
contact: Fern
chapter: Ch1

title: Start
---
System: "7:15 AM"
Fern: "Good morning."
...
Player: "Hey, what's up?"
Fern: "Not much."

>> choice
    -> "Ask about her day" <<jump Node_Day>>
    -> "Stay quiet"
        Player: "..."
        <<jump Node_Quiet>>
>> endchoice
---
```

→ [Full .bub Format Reference](Docs/FORMAT.md)

---



## 📚 Documentation

| Doc | Description |
|---|---|
| [Quick Start](Docs/QuickStart.md) | Add a character and test in Play Mode |
| [.bub Format](Docs/FORMAT.md) | Full dialogue file syntax reference |
| [Addressables Setup](Docs/Addressables_Setup.md) | Setting up CG images |
| [Project Structure](Docs/Project_Structure.md) | Full folder and file map |
| [Limitations](Docs/LIMITATIONS.md) | Known issues with root causes and fixes |

---

## 🎯 Goals

- Rapid narrative prototyping on mobile
- Scalable multi-character visual novel architecture
- Reusable dialogue engine with zero game-specific dependencies
- Clean separation between engine, UI, and game logic

Built as a **foundation**, not a one-off game.

---

## 📄 License

MIT License — see [LICENSE](LICENSE) for details.

---

## 👤 Contact

**Melvin Porcalla**
GitHub: [MvPorcalla](https://github.com/MvPorcalla)
Email: scryptid1@gmail.com

---

*Built for narrative-first developers who care about structure, performance, and clean systems.*