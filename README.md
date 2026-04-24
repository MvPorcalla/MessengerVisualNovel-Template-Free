# Messenger Style Visual Novel Template for Unity

![Unity](https://img.shields.io/badge/Unity-2022.3.62f2_LTS-black?logo=unity) ![Platform](https://img.shields.io/badge/Platform-Mobile-brightgreen) ![License](https://img.shields.io/badge/ChatSim-Free_to_Use-brightgreen) ![License](https://img.shields.io/badge/BubbleSpinner-Restricted-red) ![Status](https://img.shields.io/badge/Status-Active_Development-orange) ![Version](https://img.shields.io/badge/Version-v1-blue)

A modular **Unity template** for building **narrative-driven mobile games** with a **phone chat messenger interface**.

Built on **BubbleSpinner** — a standalone, data-driven dialogue engine designed for branching conversations, media messages, and reliable save/resume systems.

---

## ✨ Features

### 🧠 BubbleSpinner (Dialogue Engine)

* Custom `.bub` dialogue parser (lightweight, deterministic)
* Branching dialogue with player choices
* Node-based navigation (`<<jump>>`)
* Pause/resume with deterministic message IDs
* Media / CG message support
* Fully decoupled from Unity UI
* Event-driven via `IBubbleSpinnerCallbacks`

---

### 📱 ChatSim (Game Layer)

* Messenger-style chat interface
* Typing indicator system (delayed message simulation)
* Contact list with per-character conversations
* Homescreen with app navigation
* Lockscreen → Phone → Chat flow
* Scene-based lifecycle management

---

### 🖼️ Gallery System

* CG unlock tracking
* Persistent gallery state
* Addressables-based image loading
* Dialogue-integrated unlock triggers

---

### 💾 Save System

* Atomic save with backup recovery
* Resume mid-conversation
* Deterministic state reconstruction
* Per-contact progress reset

---

### 🎮 UI Systems

* Chat bubbles (player vs character styling)
* Scrollable message history
* Contact UI
* Settings UI (base structure)
* Modular and extensible UI architecture

---

### ⚙️ Architecture

```
┌─────────────────────────────────────┐
│           BubbleSpinner             │
│  Parser → Executor → Manager        │
└────────────────┬────────────────────┘
                 │ IBubbleSpinnerCallbacks
┌────────────────▼────────────────────┐
│         BubbleSpinnerBridge         │
│        Save / Load / Reset          │
└────────────────┬────────────────────┘
                 │ GameEvents
┌────────────────▼────────────────────┐
│             ChatSim                 │
│   Bootstrap → SaveManager → UI      │
└─────────────────────────────────────┘
```

BubbleSpinner is fully decoupled from Unity UI and communicates through callbacks and events, making it portable and reusable across projects.

---

## ⚡ Quick Start

1. Open the project in Unity
2. Create a `ConversationAsset` ScriptableObject
3. Write a `.bub` dialogue file and assign it
4. Add it to `CharacterDatabase`
5. Press Play

→ [Full Quick Start Guide](Docs/QuickStart.md)

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

→ [Full Format Reference](Docs/FORMAT.md)

---

## ⚠️ Current Limitations

This is the **free version** of the template. Some advanced systems are not yet implemented:

* No variables or conditional logic (`<<if>>`, etc.)
* Single save slot only
* No nested choice blocks
* Limited UI features (no rich text, no unread badges)
* Each ConversationAsset supports only one primary character
* Side-character / multi-speaker support is not yet implemented (planned feature)

→ [Full Limitations & Technical Details](Docs/LIMITATIONS.md)

---

### 🐛 Stability Notes
- The project is under active development and may contain edge cases
- Some updates are iterative and may temporarily affect related systems
- Stability improvements are prioritized over full refactoring in early iterations

### 📌 Internal Notes
- Legacy patterns may still exist from rapid iteration phases
- Some systems are intentionally deferred for refactor in later milestones

---

## 📚 Documentation

| Doc                                              | Description                 |
| ------------------------------------------------ | --------------------------- |
| [Quick Start](Docs/QuickStart.md)                | Setup and first run         |
| [.bub Format](Docs/FORMAT.md)                    | Dialogue syntax reference   |
| [Addressables Setup](Docs/Addressables_Setup.md) | CG image setup              |
| [Project Structure](Docs/Project_Structure.md)   | Folder and architecture map |
| [Limitations](Docs/LIMITATIONS.md)               | Full technical limitations  |

---

## 📦 Requirements

|                     |                                            |
| ------------------- | ------------------------------------------ |
| **Engine**          | Unity 2022.3.62f2 LTS (2D)                 |
| **Platform**        | Mobile (primary)                           |
| **Version Control** | Git                                        |
| **Packages**        | TextMeshPro, Addressables, Newtonsoft.Json |

---

## 📄 License

MIT License — see [LICENSE](LICENSE)

---

## 👤 Contact

**Melvin Porcalla**
GitHub: [https://github.com/MvPorcalla](https://github.com/MvPorcalla)
Email: [scryptid1@gmail.com](mailto:scryptid1@gmail.com)

---

**Built as a reusable foundation for narrative-first developers.**

---