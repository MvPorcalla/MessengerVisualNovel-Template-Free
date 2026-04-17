


Logs:
[BubbleSpinner][ChoiceDebug] [Mira_Ch1:20] Opened choice block in node 'Ch1_N1_Start' at pauseIndex 3
[BubbleSpinner][ChoiceDebug] [Mira_Ch1:21] Parsed choice option 'i was trying to make it work too' in node 'Ch1_N1_Start'
[BubbleSpinner][ChoiceDebug] [Mira_Ch1:28] Added choice 'i was trying to make it work too' to node 'Ch1_N1_Start' (preJumpMessages=4, hasJump=False, blockPauseIndex=3)
[BubbleSpinner][ChoiceDebug] [Mira_Ch1:28] Parsed choice option 'Blah blah' in node 'Ch1_N1_Start'
[BubbleSpinner][ChoiceDebug] [Mira_Ch1:34] Added choice 'Blah blah' to node 'Ch1_N1_Start' (preJumpMessages=4, hasJump=False, blockPauseIndex=3)
[BubbleSpinner][ChoiceDebug] [Mira_Ch1:34] Closed choice block in node 'Ch1_N1_Start' with 2 choices at pauseIndex 3
[BubbleSpinner] [42] Unlockable CG: Miravella/MiraCG1
[BubbleSpinner][ChoiceDebug] [Mira_Ch1:141] Opened choice block in node 'Ch1_N1_Mentor' at pauseIndex 22
[BubbleSpinner][ChoiceDebug] [Mira_Ch1:142] Parsed choice option 'should i be worried now' in node 'Ch1_N1_Mentor'
[BubbleSpinner][ChoiceDebug] [Mira_Ch1:147] Added choice 'should i be worried now' to node 'Ch1_N1_Mentor' (preJumpMessages=3, hasJump=True, blockPauseIndex=22)
[BubbleSpinner][ChoiceDebug] [Mira_Ch1:147] Parsed choice option 'sounds like a handful' in node 'Ch1_N1_Mentor'
[BubbleSpinner][ChoiceDebug] [Mira_Ch1:152] Added choice 'sounds like a handful' to node 'Ch1_N1_Mentor' (preJumpMessages=3, hasJump=True, blockPauseIndex=22)
[BubbleSpinner][ChoiceDebug] [Mira_Ch1:152] Parsed choice option 'cute huh' in node 'Ch1_N1_Mentor'
[BubbleSpinner][ChoiceDebug] [Mira_Ch1:157] Added choice 'cute huh' to node 'Ch1_N1_Mentor' (preJumpMessages=3, hasJump=True, blockPauseIndex=22)
[BubbleSpinner][ChoiceDebug] [Mira_Ch1:157] Closed choice block in node 'Ch1_N1_Mentor' with 3 choices at pauseIndex 22
[BubbleSpinner][ChoiceDebug] [Mira_Ch1:176] Opened choice block in node 'Ch1_N1_MissYou' at pauseIndex 9
[BubbleSpinner][ChoiceDebug] [Mira_Ch1:177] Parsed choice option 'i'll make it worth the wait' in node 'Ch1_N1_MissYou'
[BubbleSpinner][ChoiceDebug] [Mira_Ch1:182] Added choice 'i'll make it worth the wait' to node 'Ch1_N1_MissYou' (preJumpMessages=3, hasJump=True, blockPauseIndex=9)
[BubbleSpinner][ChoiceDebug] [Mira_Ch1:182] Parsed choice option 'life is boring without me' in node 'Ch1_N1_MissYou'
[BubbleSpinner][ChoiceDebug] [Mira_Ch1:186] Added choice 'life is boring without me' to node 'Ch1_N1_MissYou' (preJumpMessages=2, hasJump=True, blockPauseIndex=9)
[BubbleSpinner][ChoiceDebug] [Mira_Ch1:186] Parsed choice option 'you'll manage' in node 'Ch1_N1_MissYou'
[BubbleSpinner][ChoiceDebug] [Mira_Ch1:191] Added choice 'you'll manage' to node 'Ch1_N1_MissYou' (preJumpMessages=3, hasJump=True, blockPauseIndex=9)
[BubbleSpinner][ChoiceDebug] [Mira_Ch1:191] Closed choice block in node 'Ch1_N1_MissYou' with 3 choices at pauseIndex 9
[BubbleSpinner][ChoiceDebug] [Mira_Ch1:276] Opened choice block in node 'Ch1_N2_Start' at pauseIndex 32
[BubbleSpinner][ChoiceDebug] [Mira_Ch1:277] Parsed choice option 'yes ma'am' in node 'Ch1_N2_Start'
...

[DialogueExecutor][ChoiceDebug] GetEndIndexForNextStop node='Ch1_N1_Start', startFrom=0, endIndex=2, unresolvedBlocks=Ch1_N1_Start_block0@3:pending
[DialogueExecutor][ChoiceDebug] GetEndIndexForNextStop node='Ch1_N1_Start', startFrom=0, endIndex=2, unresolvedBlocks=Ch1_N1_Start_block0@3:pending
[DialogueExecutor][ChoiceDebug] ProcessCurrentNode node='Ch1_N1_Start', currentMessageIndex=0, unreadToNextStop=2, nextStopIndex=2
[DialogueExecutor][ChoiceDebug] GetEndIndexForNextStop node='Ch1_N1_Start', startFrom=0, endIndex=2, unresolvedBlocks=Ch1_N1_Start_block0@3:pending

[DialogueExecutor] Player-turn pause — emitting player message: 'test'

[DialogueExecutor][ChoiceDebug] GetEndIndexForNextStop node='Ch1_N1_Start', startFrom=3, endIndex=3, unresolvedBlocks=Ch1_N1_Start_block0@3:pending
[DialogueExecutor][ChoiceDebug] GetEndIndexForNextStop node='Ch1_N1_Start', startFrom=3, endIndex=3, unresolvedBlocks=Ch1_N1_Start_block0@3:pending
[DialogueExecutor][ChoiceDebug] ProcessCurrentNode node='Ch1_N1_Start', currentMessageIndex=3, unreadToNextStop=0, nextStopIndex=3

[DialogueExecutor] Player-turn pause — emitting player message: 'End Test'
[DialogueExecutor][ChoiceDebug] GetEndIndexForNextStop node='Ch1_N1_Start', startFrom=4, endIndex=9, unresolvedBlocks=Ch1_N1_Start_block0@3:pending
[DialogueExecutor][ChoiceDebug] GetEndIndexForNextStop node='Ch1_N1_Start', startFrom=4, endIndex=9, unresolvedBlocks=Ch1_N1_Start_block0@3:pending
[DialogueExecutor][ChoiceDebug] ProcessCurrentNode node='Ch1_N1_Start', currentMessageIndex=4, unreadToNextStop=5, nextStopIndex=9
[DialogueExecutor][ChoiceDebug] GetEndIndexForNextStop node='Ch1_N1_Start', startFrom=4, endIndex=9, unresolvedBlocks=Ch1_N1_Start_block0@3:pending

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