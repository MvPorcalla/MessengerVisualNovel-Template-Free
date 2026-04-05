# 02_LockScreen — Scene Setup Guide

---

## Overview

First interactive scene after Bootstrap. Shows time, date, and notifications.
Player swipes up anywhere to unlock and navigate to PhoneScreen.

---

## Part 1 — Hierarchy

```
Canvas
└── LockScreen                           ← LockScreen.cs
    └── Panel                            ← CanvasGroup → contentGroup
        ├── TimeText          (TMP)      ← timeText
        ├── DateText          (TMP)      ← dateText
        └── NotificationContainer        ← notificationContainer
            └── MoreIndicator  (TMP)     ← moreIndicator (set inactive by default)
```

**Prefabs folder:**
```
Assets/Prefabs/
└── NotificationItem                     ← notificationItemPrefab
    ├── SenderText  (TMP)               ← must be named exactly "SenderText"
    └── PreviewText (TMP)               ← must be named exactly "PreviewText"
```

---

## Part 2 — Script Attachment

| GameObject | Script |
|---|---|
| `LockScreen` | `LockScreen.cs` |

---

## Part 3 — Components

| GameObject | Component | Purpose |
|---|---|---|
| `Panel` | `CanvasGroup` | Drives fade on swipe |
| `NotificationContainer` | `Vertical Layout Group` | Stacks notification items |
| `NotificationContainer` | `Content Size Fitter` → Vertical Fit: Preferred Size | Auto-sizes to content |
| `NotificationItem` (prefab) | `Layout Element` → Preferred Height: 80 | Controls item height |

---

## Part 4 — Inspector Wiring

### LockScreen.cs

```
[Config]
config                    → GameConfig.asset

[UI References]
timeText                  → TimeText
dateText                  → DateText
contentGroup              → Panel (CanvasGroup)

[Notifications]
notificationContainer     → NotificationContainer
notificationItemPrefab    → NotificationItem (Prefab)
moreIndicator             → MoreIndicator
```

---

## Part 5 — GameConfig.asset Settings

Create via: `Right-click in Project → Create → ChatSim → Game Config`
Save to: `Assets/Settings/GameConfig.asset`

| Field | Default | Description |
|---|---|---|
| `swipeThreshold` | 300 | Min pixels upward to trigger unlock |
| `fadeSwipeRange` | 400 | Pixels of swipe until content is fully faded |
| `maxIndividualNotifications` | 3 | Max notification items shown. 0 = none |
| `timeFormat` | HH:mm | Time display format dropdown |
| `dateFormat` | dddd, MMMM dd | Date display format dropdown |
| `lockScreenDebugLogs` | true | Enable console logs for LockScreen |

---

## Part 6 — Checklist

```
☐ LockScreen.cs attached to LockScreen GameObject
☐ Panel has CanvasGroup component
☐ timeText assigned
☐ dateText assigned
☐ contentGroup assigned → Panel
☐ notificationContainer assigned
☐ notificationItemPrefab assigned (from Prefabs folder)
☐ moreIndicator assigned and set inactive by default
☐ GameConfig.asset created and assigned to config field
☐ NotificationItem prefab has SenderText and PreviewText named exactly
☐ Scene added to Build Settings at index 2
```

---

## Common Mistakes

**Content fades but unlock never triggers**
`swipeThreshold` is too high. Lower it in `GameConfig.asset` or check console
for `Swipe delta:` logs to see actual values.

**Notifications not showing**
No conversations have been started and paused yet — notifications only appear
for conversations with `resumeTarget` of `Pause` or `Interrupted`.

**MoreIndicator disappears at runtime**
It was destroyed by the clear loop — make sure it is assigned to the
`moreIndicator` field so the script skips it during cleanup.

**Time or date showing wrong format**
Check `timeFormat` and `dateFormat` dropdowns in `GameConfig.asset`.

---

## Planned Features (TODO)

```
☐ Status bar (battery, signal, wifi)
☐ Fade out whole screen transition
```