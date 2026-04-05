# 01_Bootstrap — Scene Setup Guide

---

## Overview

Bootstrap is a persistent scene loaded once at game start and never unloaded.
It holds all core managers that must survive across scene transitions.

> No Camera. No Canvas. No EventSystem.

---

## Part 1 — Hierarchy

```
GameBootstrap           ← GameBootstrap.cs
├── SaveManager         ← SaveManager.cs
├── SceneFlowManager    ← SceneFlowManager.cs
└── ConversationManager ← ConversationManager.cs
```

---

## Part 2 — Script Attachment

| GameObject | Script |
|---|---|
| `GameBootstrap` | `GameBootstrap.cs` |
| `SaveManager` | `SaveManager.cs` |
| `SceneFlowManager` | `SceneFlowManager.cs` |
| `ConversationManager` | `ConversationManager.cs` |

---

## Part 3 — Inspector Wiring

### GameBootstrap.cs

```
[Core Managers]
saveManager          → SaveManager (child GameObject)
sceneFlowManager     → SceneFlowManager (child GameObject)

[Game Systems]
conversationManager  → ConversationManager (child GameObject)

[Config]
gameConfig           → GameConfig.asset (from Assets/Settings/)

[Debug]
enableDebugLogs      → ☑ true
```

### SaveManager.cs

```
[Debug]
enableDebugLogs  → ☑ true

[Save Settings]
prettyPrintJson  → ☑ true  (disable for release builds)
```

### SceneFlowManager.cs
No serialized fields.

### ConversationManager.cs
No serialized fields.

---

## Part 4 — GameConfig.asset

GameConfig is a ScriptableObject that holds global settings for all systems.
It is assigned once in Bootstrap and accessed everywhere via `GameBootstrap.Config`.

**Create the asset:**
Right-click in Project → `Create → ChatSim → Game Config`
Save to: `Assets/Settings/GameConfig.asset`

**Assign it:**
Select `GameBootstrap` in hierarchy → drag `GameConfig.asset` into the `gameConfig` field.

**Access it from any script:**
```csharp
GameBootstrap.Config.lockScreenDebugLogs
GameBootstrap.Config.swipeThreshold
// etc.
```

No need to assign `GameConfig` in any other script's Inspector.

---

## Part 5 — Checklist

```
GameBootstrap
☐ saveManager assigned
☐ sceneFlowManager assigned
☐ conversationManager assigned
☐ gameConfig assigned → GameConfig.asset

SaveManager
☐ prettyPrintJson set appropriately for build type

GameConfig.asset
☐ Created at Assets/Settings/GameConfig.asset
☐ Assigned to GameBootstrap → gameConfig field
```