# Addressables Setup Guide

A step-by-step guide for setting up Addressables for CG images for the first time.

---

## Part 1 — Install Addressables Package

1. Open **Window → Package Manager**
2. Set the dropdown to **Unity Registry**
3. Find **Addressables** (`com.unity.addressables`)
4. Click **Install** and wait for the progress bar to complete

> Console should say "Package Manager resolve complete" with no errors.

---

## Part 2 — Initialize Addressables

1. Open **Window → Asset Management → Addressables → Groups**
2. If you see a **"Create Addressables Settings"** button — click it
3. If you see the Groups window directly — skip to Part 3

After clicking, Unity generates the required settings files and you'll see:

```
Addressables Groups
├── Built In Data
└── Default Local Group (Default)
```

---

## Part 3 — Create a CGs Group

1. In the Addressables Groups window, click **Create New Group**
2. Right-click the new group → **Rename**
3. Name it `CGs`

```
Addressables Groups
├── Built In Data
├── Default Local Group (Default)
└── CGs
```

---

## Part 4 — Add CG Images

Navigate to your character's folder in the Project window (e.g. `Assets/Characters/Sofia/`).

**Method A — Drag and Drop**
1. Keep the Project window and Addressables Groups window visible side by side
2. Drag each image from the Project window into the `CGs` group

**Method B — Inspector Checkbox**
1. Select the image in the Project window
2. In the Inspector, check the **Addressable** checkbox
3. Repeat for each image

After adding all images:

```
Addressables Groups
└── CGs
    ├── ChatA_CG1 (Sprite)
    ├── ChatA_CG2 (Sprite)
    ├── ChatA_CG3 (Sprite)
    └── ChatA_CG4 (Sprite)
```

---

## Part 5 — Set Addressable Keys

The default names (file names) must be renamed to match the paths used in your `.bub` files.

1. Click an entry in the Addressables Groups window
2. At the top of the window, find the **Address** field
3. Change the address to match the format below
4. Press Enter

| Default Name | Set Address To |
|---|---|
| `ChatA_CG1` | `Sofia/CG1` |
| `ChatA_CG2` | `Sofia/CG2` |
| `ChatA_CG3` | `Sofia/CG3` |
| `ChatA_CG4` | `Sofia/CG4` |

After renaming:

```
Addressables Groups
└── CGs
    ├── Sofia/CG1 (Sprite)
    ├── Sofia/CG2 (Sprite)
    ├── Sofia/CG3 (Sprite)
    └── Sofia/CG4 (Sprite)
```

---

## Part 6 — Build Addressables

**This step is required — images will not load without a build.**

1. In the Addressables Groups window, click **Build**
2. Select **New Build → Default Build Script**
3. Wait for the Console to show "Build completed in X.XX seconds"

> For subsequent characters, use **Build → Update a Previous Build** instead — it's faster than a full new build.

---

## Part 7 — Reference in .bub Files

Use the `>> media` command with the `path:` matching the address you set in Part 5.

```
Sofia: "Check this out."

>> media npc type:image path:Sofia/CG1

-> ...

Sofia: "And here's another!"

>> media npc type:image unlock:true path:Sofia/CG2
```

> Keys are case-sensitive. Do not include the file extension (no `.png`).

Also add the keys to the `cgAddressableKeys` list on the `ConversationAsset`:

```
cgAddressableKeys
├── Sofia/CG1
├── Sofia/CG2
├── Sofia/CG3
└── Sofia/CG4
```

This is what the Gallery uses to track and display unlocked CGs.

---

## Part 8 — Test in Play Mode

Press Play and open the conversation. Watch the Console:

```
[ImageMessageBubble] Loading: Sofia/CG1
[AddressablesImageLoader] ✓ Loaded: Sofia/CG1
[ImageMessageBubble] ✓ Image loaded: Sofia/CG1
```

If the image appears in chat, tap it to open the fullscreen viewer.

---

## Adding a New Character

When adding a new character (e.g. Emma):

1. Add their images to the same `CGs` group
2. Set addresses: `Emma/CG1`, `Emma/CG2`, etc.
3. Add keys to their `ConversationAsset → cgAddressableKeys`
4. Use **Build → Update a Previous Build**
5. Reference in `.bub`: `path:Emma/CG1`

---

## Checklist

```
☐ Addressables package installed
☐ Addressables Settings created
☐ CGs group created
☐ All images added to CGs group
☐ Addresses set (e.g. Sofia/CG1, Sofia/CG2)
☐ Addressables built (New Build → Default Build Script)
☐ .bub file uses correct paths (path:Sofia/CG1)
☐ cgAddressableKeys populated on ConversationAsset
```

---

## Common Mistakes

**`InvalidKeyException: Sofia/CG1`**
Addressables have not been built, or the key doesn't exist in the Groups window. Verify the address exists and run Build → New Build → Default Build Script.

**Image doesn't appear in chat**
Check that the `path:` in the `.bub` file exactly matches the address in the Addressables Groups window — spelling and case must match. Confirm a build has been run.

**Build menu is greyed out**
Close and reopen the Addressables Groups window via Window → Asset Management → Addressables → Groups.

**Can't drag images into the Groups window**
Use the Inspector checkbox method instead — select the image, check Addressable in the Inspector, then set the Address field manually.