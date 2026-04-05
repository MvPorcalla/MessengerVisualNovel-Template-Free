# ChatSim — Scene & Architecture Notes

---

## 1. Build Settings

`File → Build Settings` — add scenes in this exact order:

| Index | Scene | Role |
|---|---|---|
| 0 | `00_Disclaimer` | Optional TOS gate |
| 1 | `01_Bootstrap` | System initializer |
| 2 | `02_LockScreen` | Entry point after boot |
| 3 | `03_PhoneScreen` | Home screen / app launcher |
| 4 | `04_ChatApp` | Chat conversation screen |

> **Tip:** If you want to skip Disclaimer, move `01_Bootstrap` to index 0. No code changes needed.

---

## 2. Scene Flow

```
00_Disclaimer ──(first launch)──► 01_Bootstrap ──► 02_LockScreen
      │                                                    │
      └──(already accepted)────────────────────────────────┘
                                                           │
                                                    (tap to unlock)
                                                           │
                                                           ▼
                                                   03_PhoneScreen
                                                           │
                                                  (open chat app)
                                                           │
                                                           ▼
                                                    04_ChatApp
```

---

## 3. Per-Scene Summary

### `00_Disclaimer`
- Attach: `DisclaimerScreen.cs` → Canvas
- Shows TOS panel before first boot
- On agree → loads `01_Bootstrap`
- Skipped automatically if already accepted (stored in `PlayerPrefs`)
- `skipForTesting = true` bypasses it in editor without clearing PlayerPrefs

### `01_Bootstrap`
- No Camera, Canvas, or EventSystem needed
- Attach: `GameBootstrap.cs`, `SaveManager.cs`, `SceneFlowManager.cs`, `ConversationManager.cs`
- Persists for the entire session (`DontDestroyOnLoad`)
- Always navigates to `02_LockScreen` after initialization

### `02_LockScreen`
- Attach: `LockScreen.cs` → LockScreen GameObject
- Shows time, date, and unlock button
- On tap → calls `GameBootstrap.SceneFlow.GoToPhoneScreen()`

### `03_PhoneScreen`
- Home screen and in-scene apps (Gallery, Contacts)
- → See `03_PhoneScreen_Setup.md` for full hierarchy, prefabs, and wiring

### `04_ChatApp`
- Chat messaging interface and contact list
- → See `04_ChatApp_Setup.md` for full hierarchy, prefabs, and wiring

---

## 4. Architecture

### Core Principle
> Engine and UI are fully decoupled. Bootstrap owns initialization. SceneFlow owns transitions.

### System Layers

```
┌──────────────────────────────────────────────────┐
│                    UI Layer                      │
│   DisclaimerScreen, LockScreen,                  │
│   ChatAppController, HomeScreenController        │
│   (scene-local — destroyed on scene change)      │
└─────────────────────┬────────────────────────────┘
                      │
               GameEvents (static event bus)
                      │
┌─────────────────────▼────────────────────────────┐
│               Core Systems Layer                 │
│   GameBootstrap [DontDestroyOnLoad]              │
│     ├── SaveManager       → GameBootstrap.Save   │
│     ├── SceneFlowManager  → GameBootstrap.SceneFlow │
│     └── ConversationManager → GameBootstrap.Conversation │
└─────────────────────┬────────────────────────────┘
                      │
┌─────────────────────▼────────────────────────────┐
│             BubbleSpinner Engine                 │
│   BubbleSpinnerParser  → DialogueExecutor        │
│   ConversationManager  → BubbleSpinnerBridge     │
│   (no Unity UI dependencies)                     │
└──────────────────────────────────────────────────┘
```

### Bootstrap Initialization Order

```
Awake()
  ├── Singleton setup
  ├── DontDestroyOnLoad
  └── GameEvents.ClearAllEvents()

Start()
  ├── ValidateManagerReferences()     ← throws if any Inspector ref is missing
  ├── AssignStaticReferences()        ← exposes Save, SceneFlow, Conversation
  ├── InitializeManagers()
  │     ├── Save.Init()
  │     ├── SceneFlow.Init()
  │     ├── BubbleSpinnerBridge setup
  │     ├── Conversation.Initialize()
  │     └── EnsureSaveDataExists()
  └── DetermineNextScene()            ← always returns LockScreen
        └── SceneFlow.LoadScene(LockScreen)
```

---

## 5. Script Reference

| Script | Scene | Responsibility |
|---|---|---|
| `DisclaimerScreen.cs` | `00_Disclaimer` | TOS gate — loads Bootstrap on accept |
| `GameBootstrap.cs` | `01_Bootstrap` | Initializes all managers, navigates to LockScreen |
| `SceneFlowManager.cs` | `01_Bootstrap` | All scene transitions go through here |
| `SceneNames.cs` | `01_Bootstrap` | String constants for all scene names |
| `GameEvents.cs` | persistent | Static event bus for cross-system communication |
| `SaveManager.cs` | persistent | Read/write save data to disk |
| `LockScreen.cs` | `02_LockScreen` | Tap to unlock, navigates to PhoneScreen |

---

## 6. How to Add or Remove a Scene

**Adding a scene:**
1. Create the scene file in Unity
2. Add it to `File > Build Settings`
3. Add a constant to `SceneNames.cs`
4. Add a convenience method to `SceneFlowManager.cs` (e.g. `GoToNewScene()`)

**Removing Disclaimer:**
1. Move `01_Bootstrap` to Build Index 0 in Build Settings
2. Remove `00_Disclaimer` from Build Settings (keep the file if needed later)
3. No code changes required