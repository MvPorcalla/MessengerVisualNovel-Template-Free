


Logs:
[ConversationManager] Starting conversation: Miravella (ID: 939878_Mira)
[GameEvents] Game loaded
[BubbleSpinnerBridge] SaveData loaded into cache
[DialogueExecutor] No chapter ID in state, defaulting to 'Ch1'
[BubbleSpinner] Parsing: Mira_Ch1
[BubbleSpinner] [Mira_Ch1] contact: mismatch! File says 'Mira' but asset expects 'Miravella'
[BubbleSpinner] [Mira_Ch1] chapter: 'Ch1'
[BubbleSpinner] [42] Unlockable CG: Miravella/MiraCG1
[BubbleSpinner] [419] Unlockable CG: Miravella/MiraCG2
[BubbleSpinner] [527] Unlockable CG: Miravella/MiraCG3
[BubbleSpinner] [530] Unlockable CG: Miravella/MiraCG4
[BubbleSpinner] [579] Chapter jump → 'MiraCh2' node:'Start'
[BubbleSpinner] [Mira_Ch1:581] '>> END' on node 'Ch1_N3_Rumor' which already has a jump — >> END ignored
[BubbleSpinner] Parsed 10 nodes from Mira_Ch1
[DialogueExecutor] No saved node, defaulting to 'Ch1_N1_Start'
[ConversationManager] Created new session for 939878_Mira
[GameEvents] Conversation started: 939878_Mira
[GameEvents] Game saved
[ConversationManager] ✓ Saved: 939878_Mira (Node: 'Ch1_N1_Start', Chapter: 'Ch1')

ISSUE:

Having more than one choice in a single node breaks the flow.

Behavior:

I tried adding a temporary test choice in the Ch1_N1_Start node.
However, what happened was that the temporary test choice got ignored and only the last choice block:

// last choice block
>> choice
    -> "should I be worried now"
        Player: "wow, should I be worried now"
        Mira: "please"
        Mira: "he still calls Excel the table thing"
        <<jump Ch1_N1_MissYou>>

    -> "sounds like a handful"
        Player: "sounds like a handful"
        Mira: "yeah"
        Mira: "I might lose my patience on day one"
        <<jump Ch1_N1_MissYou>>

    -> "cute, huh"
        Player: "cute, huh"
        Mira: "don't start"
        Mira: "it's not like that"
        <<jump Ch1_N1_MissYou>>
>> endchoice

Although these three choices still work, when I press the choice

---

contact: Mira
chapter: Ch1

// Type: Linear
// Chapter: 1
// Scene Context:
// Late evening. Mira just got out of the main office and messages first while heading home.
// You arrive later at your quiet rural apartment after a long day.

title: Ch1_N1_Start
---

System: "06:15 PM"

Mira: "Hey babe..."

Player: "test"

// temporary test choice
>> choice
    -> "i was trying to make it work too"
        Player: "i was trying to make it work too"

        Player: "the distance sucked for me as well"
        ...
        Mira: "blah blah" 
        Player: "yada yada" 
>> endchoice

Mira: "I missed you. It’s been months."
Mira: "Just finished work. I’m exhausted."
Mira: "Wait, hold on."

// >> media npc type:image unlock:true path:Miravella/CG1
// [Image: Miravella/CG1 — mirror selfie in the office restroom, sleeves slightly rolled up, hair a bit messy, faint tired smile]

Mira: "i look dead"

Player: "…you kinda do"
Player: "but still cute"

Mira: "wow"
Mira: "i'll take it"

Player: "i just got home"
Player: "i swear today tried to kill me"

Mira: "what happened"

Player: "system kept bugging out"
Player: "and there's like no one here to fix it fast"

Player: "kinda miss the main office chaos"
Player: "at least things moved"

Mira: "yeah…"
Mira: "rural branch life hitting you now huh"

Player: "yeah"
Player: "it's way too quiet here"

Player: "even the office feels empty after hours"
Player: "just me and flickering lights"

Mira: "okay that sounds creepy"
Mira: "i don't like that"

Player: "you should"
Player: "builds character"

Mira: "stop 😭"
Mira: "now i'm imagining you alone there"
Mira: "…i don't like that you're that far"

>> choice
    -> "it's just temporary"
        Player: "it's just temporary, i'll be back before you know it"
        Mira: "you better"
        Mira: "i'm holding onto that"
        <<jump Ch1_N1_Mentor>>
    -> "you don't trust me?"
        Player: "what, you don't trust me alone with ghosts?"
        Mira: "wow so funny"
        Mira: "i hope they haunt you properly"
        <<jump Ch1_N1_Mentor>>
    -> "i'll get used to it"
        Player: "i'll get used to it"
        Mira: "…yeah"
        Mira: "i guess you will"
        <<jump Ch1_N1_Mentor>>
>> endchoice
---

contact: Mira
chapter: Ch1

// Type: Linear
// Chapter: 1
// Scene Context:
// Late evening. Mira just got out of the main office and messages first while heading home.
// You arrive later at your quiet rural apartment after a long day.

title: Ch1_N1_Start
---

System: "06:15 PM"

Mira: "Hey babe..."

Player: "test"

// temporary test
>> choice
    -> "i was trying to make it work too"
        Player: "i was trying to make it work too"
        Player: "the distance sucked for me as well"
        ...
        Mira: "blah blah" 
        Player: "yada yada" 
    
    -> "Blah blah"
        Player: "balh blah"
        Player: "yada yada"
        ...
        Mira: "blah blah" 
        Player: "yada yada" 
>> endchoice

Player: "End Test"

Mira: "I missed you. It’s been months."
Mira: "Just finished work. I’m exhausted."
Mira: "Wait, hold on."

>> media npc type:image unlock:true path:Miravella/MiraCG1
// [Image: Miravella/CG1 — mirror selfie in the office restroom, sleeves slightly rolled up, hair a bit messy, faint tired smile]

Mira: "i look dead"

Player: "…you kinda do"
Player: "but still cute"

Mira: "wow"
Mira: "i'll take it"

Player: "i just got home"
Player: "i swear today tried to kill me"

Mira: "what happened"

Player: "system kept bugging out"
Player: "and there's like no one here to fix it fast"

Player: "kinda miss the main office chaos"
Player: "at least things moved"

Mira: "yeah…"
Mira: "rural branch life hitting you now huh"

Player: "yeah"
Player: "it's way too quiet here"

Player: "even the office feels empty after hours"
Player: "just me and flickering lights"

Mira: "okay that sounds creepy"
Mira: "i don't like that"

Player: "you should"
Player: "builds character"

Mira: "stop 😭"
Mira: "now i'm imagining you alone there"
Mira: "…i don't like that you're that far"

// last choice block
>> choice
    -> "it's just temporary"
        Player: "it's just temporary, i'll be back before you know it"
        Mira: "you better"
        Mira: "i'm holding onto that"
        <<jump Ch1_N1_Mentor>>
    -> "you don't trust me?"
        Player: "what, you don't trust me alone with ghosts?"
        Mira: "wow so funny"
        Mira: "i hope they haunt you properly"
        <<jump Ch1_N1_Mentor>>
    -> "i'll get used to it"
        Player: "i'll get used to it"
        Mira: "…yeah"
        Mira: "i guess you will"
        <<jump Ch1_N1_Mentor>>
>> endchoice
---

title: Ch1_N1_Mentor
---
Mira: "anyway"
Mira: "something new happened today"

Player: "oh?"
>> END
---