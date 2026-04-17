# BubbleSpinner — .bub Format Reference

---

## Syntax Legend

| Symbol | Meaning |
|--------|---------|
| `contact:`                      | Optional file metadata — declares the character name                   |
| `chapter:`                      | Required file metadata — declares the chapter ID for cross-chapter jumps |
| `title:`                        | Declares a new node — must be unique within the file                   |
| `---`                           | Opens or closes a node — first `---` opens, second `---` closes        |
| `...`                           | Pure pacing pause — tap to continue, nothing sent                      |
| `Speaker: "text"`               | NPC message bubble                                                     |
| `Player: "text"`                | Player message — implicit pause point, tap sends then NPC continues    |
| `System: "text"`                | Non-chat system message (timestamps, scene breaks)                     |
| `>> media`                      | Image bubble command                                                   |
| `>> choice`                     | Opens a choice block — must be at indent 0                             |
| `>> endchoice`                  | Closes a choice block — required, must be at indent 0                  |
| `-> "text"`                     | Choice button — must be inside `>> choice` at indent 1                 |
| `Speaker: "text"`               | Pre-jump dialogue inside a choice option — indent 2, before `<<jump>>` |
| `>> media`                      | Pre-jump media inside a choice option — indent 2, before `<<jump>>`    |
| `<<jump NodeName>>`             | Local jump — stays within the current chapter file                     |
| `<<jump chapter:ChapterId>>`    | Chapter jump — loads a new chapter file, enters at Start node          |
| `<<jump chapter:ChapterId node:NodeName>>` | Chapter jump — loads a new chapter file, enters at specific node |
| `//`                            | Comment — inline or full line, stripped by parser                      |
| `>> END`                        | Marks a node as an intentional conversation end — no jump required, suppresses missing-jump warning |

---

## File Structure
```
contact: Fern                                                   // optional — validated against ConversationAsset.characterName
chapter: Ch1                                                    // required — must match ChapterEntry.chapterId in ConversationAsset

title: Start                                                    // declares node named "Start"
---                                                             // opens node content
System: "7:15 AM"                                               // system message — timestamp, scene label, etc.

Fern: "Good morning."                                           // NPC message bubble
Fern: "I hope I'm not disturbing you this early."

>> media npc type:image unlock:true path:Fern/CG1               // image bubble — unlock:true adds to gallery

...                                                             // pure pacing pause — tap to continue, nothing sent

Fern: "Still, there are moments when I feel..."

Player: "I'm sure she notices"                                  // implicit pause point — tap sends this message, then NPC continues

Fern: "...Perhaps you're right."

>> choice                                                       // opens choice block — must be at indent 0
    -> "Lonely?"                                                // choice button — indent 1
        Player: "Test Choice Dialogue"                          // pre-jump dialogue — indent 2, shows before jumping
        <<jump Node_Loneliness>>                                // local block jump — indent 2
    -> "Unappreciated?"                                         // second choice option — indent 1
        Player: "Test Choice Dialogue"
        <<jump Node_Appreciation>>
>> endchoice                                                    // closes choice block — required, must be at indent 0

---                                                             // closes node "Start"


title: Node_Loneliness                                          // declares next node
---                                                             // opens node content
Fern: "..."
<<jump chapter:Ch2>>                                            // chapter jump — loads Ch2, enters at Start
---                                                             // closes node


title: Node_Appreciation
---
Fern: "..."
<<jump chapter:Ch2 node:Branch_A>>                              // chapter jump — loads Ch2, enters at Branch_A
---
```

---

## Commands

### `contact: Name`
Optional metadata. Validated against `ConversationAsset.characterName` at parse time. Mismatch logs a warning but does not stop parsing.

---

### `chapter: ChapterId`
Declares the chapter ID for this file. Must match the `ChapterEntry.chapterId` registered in `ConversationAsset`. Used by cross-chapter jumps to identify and load this file.
```
chapter: Ch1
chapter: Ch2
```

Missing `chapter:` logs a warning. Cross-chapter jumps targeting this file will fail to resolve if the ID is absent or mismatched.

---

### `title: NodeName`
Declares a dialogue node. Must be unique within the file. First node is typically `Start`.
```
title: Start
title: Node_Loneliness
title: Branch_A
```

---

### `---`
Dual-purpose delimiter. The first `---` after `title:` opens node content. The second `---` closes the node. Parser errors if `---` appears with no current node.
```
title: Start
---
// content here
---
```

---

### `...`
Pure pacing pause. Shows the continue button — tapping resumes NPC flow, nothing is sent. Cannot be inside `>> choice` — ignored with a warning.
```
Fern: "I have something to tell you."
...
Fern: "I like you."
```

---

### `Speaker: "text"`
Message bubble. Quotes are optional — parser strips them. Any speaker name that is not `Player` or `System` is treated as an NPC.
```
Fern: "Good morning."
System: "7:15 AM"
```

---

### `Player: "text"`
Implicit pause point. Shows the continue button — tapping sends the player message first, then NPC continues. Cannot generate pause points inside a `>> choice` block — treated as a normal pre-jump message with no pause.
```
Fern: "What do you think?"
Player: "I think it's great."
Fern: "Really?"
```

---

### `System: "text"`
Non-chat system message (timestamps, scene breaks). Case-insensitive.
```
System: "Later that evening."
```

---

### `>> media [Speaker] type:image path:[key]`
Image bubble. `path:` must be a valid Addressables key. Place `path:` last.
```
>> media npc type:image path:Fern/happy
```

### `>> media [Speaker] type:image unlock:true path:[key]`
Same as above but also unlocks the CG to the gallery and fires `OnCGUnlocked`. `unlock:true` must come before `path:`.
```
>> media npc type:image unlock:true path:Fern/CG1
```

---

### `>> choice` / `>> endchoice`
Opens and closes a choice block. Both must be at indent 0. `>> endchoice` is required — missing it logs a warning and closes implicitly.
```
>> choice
    -> "Lonely?"
        Player: "Lonely? Even when you're traveling together?"
        <<jump Node_Loneliness>>
    -> "Unappreciated?"
        <<jump Node_Appreciation>>
>> endchoice
```

Rules:
- `>> choice` must be at indent 0 — error if indented
- `>> endchoice` must be at indent 0 — warns and recovers if indented
- Nested choice blocks not supported — warns and ignores
- `...` inside a choice block is ignored with a warning
- `<<jump>>` at indent 0 inside a choice block is an error — use `>> endchoice` first

---

### `-> "Choice Text"`
One choice button. Must be inside `>> choice` at indent 1. Text in double quotes.

The choice text is the button label only.
It is never sent as a chat bubble.
To send a player message after selecting a choice,
add a Player: line at indent 2 below the option.

**Inline jump** — jump defined on the same line, no pre-jump dialogue supported:
```
-> "Lonely?" <<jump Node_Loneliness>>
-> "Continue" <<jump chapter:Ch2>>
-> "Continue" <<jump chapter:Ch2 node:Branch_A>>
```

**Block jump** — jump defined at indent 2, pre-jump dialogue supported:
```
-> "Lonely?"
    Player: "Lonely? Even when you're traveling together?"
    Fern: "...Yes."
    >> media npc type:image path:Fern/Reaction
    <<jump Node_Loneliness>>
```

**Fall-through** — no jump, continues downward in the node:
```
-> "Lonely?"
-> "Unappreciated?"
```

Runtime behavior:
- After the player selects a fall-through option, that choice block is considered consumed
- Execution continues with the next message in the node after the `>> endchoice`
- Multiple sequential choice blocks in the same node are supported and resolve in order

---

### Pre-jump dialogue inside a choice option
Dialogue and media at indent 2 inside a choice option display as chat bubbles after the choice is selected, before the jump fires. All speakers allowed. No pause points generated — everything auto-plays.

Rules:
- Must be at indent 2
- Must come before `<<jump>>`
- Anything after `<<jump>>` is an error and ignored
- `Player:` lines display as player bubbles with no pause
```
-> "Lonely?"
    Player: "Lonely? Even when you're traveling together?"
    Fern: "...Yes. That's exactly it."
    >> media npc type:image path:Fern/Reaction
    <<jump Node_Loneliness>>
```

---

### `<<jump NodeName>>`
Local jump. Stays within the current chapter file. Target must be a valid `title:` node in the same file. Parser warns if the target node does not exist.
```
<<jump EndNode>>
<<jump Node_Loneliness>>
```

Indent rules:
- Indent 0 — node-level jump
- Indent 2 — belongs to the choice option directly above
- Indent 1 — error
- Indent 0 inside `>> choice` — error

---

### `<<jump chapter:ChapterId>>`
Cross-chapter jump. Loads a new chapter file and enters at its `Start` node.
`ChapterId` must match a `ChapterEntry.chapterId` registered in `ConversationAsset`.
```
<<jump chapter:Ch2>>
<<jump chapter:Epilogue>>
```

---

### `<<jump chapter:ChapterId node:NodeName>>`
Cross-chapter jump to a specific node. Loads a new chapter file and enters at the named node instead of `Start`.
```
<<jump chapter:Ch2 node:Branch_A>>
<<jump chapter:Ch3 node:Node_Concern>>
```

---

### `>> END`
Marks a node as an explicit conversation end. Prevents the validator from 
warning about a missing `<<jump>>`. Use when a node intentionally terminates 
the conversation with no further progression.

Rules:
- Must be at indent 0 — error if indented
- Cannot appear inside a `>> choice` block — ignored with an error
- Ignored if the node already has a `<<jump>>` — warns and skips

    Fern: "Goodbye."
    >> END

---

### `//`
Comment. Inline or full line. Stripped by parser before processing.
```
Fern: "Hi!" // greeting

//=====================================
// LONELINESS NODE
//=====================================
title: Node_Loneliness
```

---

## Known Limitations

- `type:audio` not implemented — falls through to unrecognized line warning
- Conditionals (`<<if>>`, `<<else>>`, `<<endif>>`) not yet implemented — see parser TODO block
- Indent system is flat (levels 0–2 only) — no nested scope support until `<<if>>` is added
- Timestamps assigned at parse time, not display time
- Message history grows with playtime — save file size grows accordingly
- `contact:` mismatch is warning only — parsing continues
- `chapter:` missing or mismatched logs a warning — cross-chapter jumps targeting this file will fail
- Nested choice blocks not supported
- Inline jump choices (`-> "text" <<jump Node>>`) do not support pre-jump dialogue — use block style instead
- Two choices jumping to the same node with different player messages require separate routing nodes
