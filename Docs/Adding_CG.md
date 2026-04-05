# Adding CGs for a New Character (Addressables Already Set Up)

---

## Step 1 — Open Addressables Groups

**Window → Asset Management → Addressables → Groups**

You should already see your existing groups. Do not touch them.

> **Tip:** Drag the Addressables Groups tab next to the Inspector for easy side-by-side access.
> To keep the layout: **Window → Layouts → Save Layout**

---

## Step 2 — Create a Group for the Character

1. Click **Create New Group**
2. Right-click the new group → **Rename**
3. Name it after the character — e.g. `CHR_Luxanna`

```
Addressables Groups
├── Built In Data
├── Default Local Group (Default)
├── CGs                    ← existing
└── CHR_Luxanna            ← new
```

> One group per character keeps things organized and makes it easy to update a single character later.

---

## Step 3 — Add the Images

Navigate to the character's folder in the Project window (e.g. `Assets/Characters/Luxanna/`).

**Method A — Drag and Drop**
1. Keep Project window and Addressables Groups window visible side by side
2. Drag each CG image into `CHR_Luxanna`

**Method B — Inspector Checkbox**
1. Select the image in the Project window
2. In the Inspector, check the **Addressable** checkbox
3. In the dropdown next to it, select `CHR_Luxanna` as the group
4. Repeat for each image

---

## Step 4 — Set the Address Keys

Each entry gets an address that your `.bub` files will reference.

1. Click an entry in the `CHR_Luxanna` group
2. Find the **Address** field at the top of the window
3. Set it to the format `CharacterName/CGName`
4. Press Enter

| File Name | Set Address To |
|---|---|
| `Luxanna_CG1` | `Luxanna/CG1` |
| `Luxanna_CG2` | `Luxanna/CG2` |
| `Luxanna_CG3` | `Luxanna/CG3` |
| `Luxanna_CG4` | `Luxanna/CG4` |

After setting:

```
Addressables Groups
└── CHR_Luxanna
    ├── Luxanna/CG1 (Sprite)
    ├── Luxanna/CG2 (Sprite)
    ├── Luxanna/CG3 (Sprite)
    └── Luxanna/CG4 (Sprite)
```

> Keys are case-sensitive. No file extensions. No spaces.

---

## Step 5 — Update the Build

Since Addressables already exist in the project, do **not** run a New Build — use Update instead:

1. In the Addressables Groups window click **Build**
2. Select **Update a Previous Build**
3. Wait for Console: `"Build completed in X.XX seconds"`

> New Build rebuilds everything from scratch and is slower. Update a Previous Build only processes what changed.

---

## Step 6 — Add Keys to ConversationAsset

1. Select the character's `ConversationAsset` in the Project window
2. Scroll to **CG Addressable Keys**
3. Add each key you set in Step 4

```
cgAddressableKeys
├── Luxanna/CG1
├── Luxanna/CG2
├── Luxanna/CG3
└── Luxanna/CG4
```

This is what the Gallery reads to know which CGs exist and which are unlocked.

---

## Step 7 — Reference in .bub Files

```
>> media npc type:image path:Luxanna/CG1

>> media npc type:image unlock:true path:Luxanna/CG2
```

Use `unlock:true` for any CG that should be saved to the gallery when the player sees it.

---

## Step 8 — Test in Play Mode

Press Play and open the conversation. Console should show:

```
[AddressablesImageLoader] ✓ Loaded: Luxanna/CG1
```

If you see `InvalidKeyException` — the address doesn't match or the build wasn't updated. Double-check spelling and re-run Update a Previous Build.

---

## Checklist

```
☐ New group created (e.g. CHR_Luxanna)
☐ All CG images added to the group
☐ Address keys set (Luxanna/CG1, Luxanna/CG2...)
☐ Build → Update a Previous Build run
☐ Keys added to ConversationAsset → cgAddressableKeys
☐ .bub file uses matching path: keys
```