# 00_Disclaimer — Scene Setup Guide

---

## Overview

Optional first scene. Shows a content disclaimer before allowing progression.
On confirm, navigates to Bootstrap. On exit, quits the application.

To skip this scene entirely, set `01_Bootstrap` as index 0 in Build Settings.

---

## Part 1 — Hierarchy

```
Canvas
└── DisclaimerScreen                 ← DisclaimerScreen.cs
    └── DisclaimerPanel
        ├── Title          (TMP)
        ├── Content
        │   └── text       (TMP)
        ├── ConfirmButton  (Button)
        └── ExitButton     (Button)
```

---

## Part 2 — Script Attachment

| GameObject | Script |
|---|---|
| `DisclaimerScreen` | `DisclaimerScreen.cs` |

---

## Part 3 — Inspector Wiring

### DisclaimerScreen.cs

```
[UI References]
confirmButton  → ConfirmButton (Button)
exitButton     → ExitButton (Button)

[Debug]
skipForTesting   → ☐ false  ← NEVER true in release builds
enableDebugLogs  → ☑ true
```

---

## Part 4 — Checklist

```
☐ DisclaimerScreen.cs attached to DisclaimerScreen GameObject
☐ confirmButton assigned → ConfirmButton
☐ exitButton assigned → ExitButton
☐ Title and text populated in scene
☐ skipForTesting = ☐ false before any build
```

---

## Common Mistakes

**Disclaimer shows every launch**
`PlayerPrefs` key was cleared or never saved. Check that `MarkAccepted()`
is being called on confirm — use F9 in editor to manually reset if needed.

**Game skips disclaimer in release build**
`skipForTesting` was left checked. Always verify it is false before building.

**Confirm button does nothing**
`confirmButton` is not assigned in the Inspector.

---

## TODO

Multi-panel support (Disclaimer ↔ TOS switching) is stubbed out in
`DisclaimerScreen.cs` as a TODO comment. When needed, uncomment the
panel fields and add `TOSPanel` to the hierarchy under `DisclaimerScreen`.