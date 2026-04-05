# Quick Start — Adding a Character

This guide assumes the template is already set up and running.
Follow these steps to add a new character and test them in Play Mode.

---

## Before You Start

CG images use Addressables. If you haven't set that up yet, do that first:
→ [Addressables Setup Guide](Addressables_Setup.md)

---

## Step 1 — Create a Conversation Asset

Right-click in the Project window:
**Create → BubbleSpinner → Conversation Asset**

Fill in the Inspector:

```
[Required]
characterName  → "Sofia"
profileImage   → Addressable sprite reference
chapters       → drag your .bub file here

[CG Gallery]
cgAddressableKeys → "Sofia/CG1", "Sofia/CG2"  (Addressable keys for each CG)

[Optional Profile]
characterAge       → "24"
birthdate          → "March 3"
relationshipStatus → Single
occupation         → "Barista"
bio                → short tagline shown in contact list
description        → longer background for detail panel
personalityTraits  → "Introverted, caring, easily flustered"
```

> `conversationId` is auto-generated — do not modify it.

---

## Step 2 — Write a Dialogue File

Create a new text file, give it a `.bub` extension, and assign it to the `chapters` list on the `ConversationAsset`.

Minimal working example:

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

===

title: Node_Dismiss
---
Sofia: "Fine."

<<jump EndNode>>

===

title: EndNode
---
Sofia: "Talk later."
```

See [.bub Format Reference](../Assets/Scripts/BubbleSpinner/Docs/FORMAT.md) for the full syntax guide.

---

## Step 3 — Add to Character Database

Open `CharacterDatabase.asset` in the Inspector.

Either:
- Drag the `ConversationAsset` into the `allCharacters` list manually
- Or right-click the asset → **Auto-Find All Characters** to populate all assets in the project at once

---

## Step 4 — Press Play and Test

Press Play from any scene. The game always starts at `00_Disclaimer`.

**Expected flow:**
```
00_Disclaimer → 01_Bootstrap → 02_Lockscreen → 03_PhoneScreen → 04_ChatApp
```

Open the Chat app and tap your character's contact to start the conversation.

---

## Testing Tips

**Skip the disclaimer on repeat plays**
The disclaimer is only shown once (saved to PlayerPrefs). To reset it in the Editor:

- Press **F9** during Play Mode to reset the disclaimer flag
- Or right-click `DisclaimerScreen` on the Canvas → **Reset Disclaimer**

Next time you press Play, the TOS screen will appear again.

---

**Validate the Bootstrap**
Right-click `GameBootstrap` in the Hierarchy → **Validate Bootstrap**

Checks that all manager references are wired. Run this if the game quits immediately on Play.

---

**Validate scene Build Settings**
Right-click `SceneFlowManager` → **Validate All Scenes**

Prints a ✓/✗ list of all five scenes. If any are missing from Build Settings the game will fail to transition between scenes.

---

**Refresh the Gallery manually**
Right-click `GalleryController` → **Debug/Refresh Gallery**

Forces the gallery to rebuild from save data without restarting Play Mode. Useful when testing CG unlocks.

---

**Print Gallery stats**
Right-click `GalleryController` → **Debug/Print Gallery Stats**

Prints unlocked/total CG counts per character to the Console.

---

**Validate Gallery references**
Right-click `GalleryController` → **Debug/Validate References**

Checks that all prefabs, containers, and the database are assigned. Run this if the gallery spawns nothing.