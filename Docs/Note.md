Create a chapter-based visual novel story in a messenger/chat format.

The output MUST be strictly structured for parsing. No extra commentary.

────────────────────────────

1. CORE STRUCTURE RULES
   ────────────────────────────

* Story is divided into chapters.
* Each chapter is a node graph made of **chat turns**.
* Nodes MUST feel like real messenger conversations (back-and-forth dialogue).
* Every node is a sequence of messages, not single-sided text dumps.

Start format:
[chp.1:startNode] -> [Node] -> [Node] -> [BranchPoint]

────────────────────────────
2. NODE FLOW RULE (IMPORTANT)
────────────────────────────

Each node MUST follow a conversation flow:

Example:

Node:
Mia: "Hey… are you there?"
Player: "Yeah, what’s up?"
Mia: "Something weird is happening."

OR mixed:

Node:
Mia: "I saw something outside."
Player: "What did you see?"
Mia: "Don’t laugh… it looked human."

Rules:

* MUST alternate NPC ↔ Player whenever possible
* NPC can speak multiple times only if emotional/intense moment
* Player always responds before major progression
* Nodes should feel like live chat, not narration blocks

────────────────────────────
3. CHAPTER 1 BRANCH RULE (STRICT)
────────────────────────────

Chapter 1 MUST follow this structure:

A. First split:

* Exactly 2 branches:

  * BranchA
  * BranchB

B. Second split (ONLY inside BranchB):

* BranchB must split into:

  * BranchB (final path)
  * BranchC (final path)

C. Final result:

* 3 endings only:

  * [end-chp1:branchA]
  * [end-chp1:branchB]
  * [end-chp1:branchC]

❗ DO NOT:

* create 3 branches at once
* branch outside BranchB again
* add extra endings

────────────────────────────
4. CHAPTER 2 STRUCTURE
────────────────────────────

Format:

[ch2:branchA-start] -> chat nodes -> End
[ch2:branchB-start] -> chat nodes -> End
[ch2:branchC-start] -> chat nodes -> End

Rules:

* Each branch starts independently
* Each branch MUST still use back-and-forth chat style
* Branches may include sub-branches (optional)

────────────────────────────
5. NODE CONTENT FORMAT
────────────────────────────

Each node may include:

A. Chat Dialogue (REQUIRED CORE FORMAT)
Node:
NPC Name: "Message"
Player: "Response"
NPC Name: "Reply"

✔ Always prefer conversation exchange format

---

B. Player Choices (2–3 max)

[Player Choices]:

1. Choice A
2. Choice B
3. Choice C (optional)

Rules:

* Choices appear ONLY at decision nodes
* Choices MUST affect next node or branch
* Choices should feel like chat replies (not abstract options)

---

C. Image System

[System]:
*Image Sent*
Description: detailed visual description for generation

Must include:

* environment
* character expression
* mood/lighting
* camera perspective if relevant

────────────────────────────
6. BRANCHING FORMAT (STRICT VISUAL GUIDE)
────────────────────────────

[chp.1:startNode]
-> Node
-> Node
-> BranchPoint

[BranchPoint]
├── [BranchA]
│     -> chat nodes
│     -> [end-chp1:branchA]

└── [BranchB]
-> chat nodes
-> [BranchSplit]

```
       ├── [BranchB]
       │     -> chat nodes
       │     -> [end-chp1:branchB]

       └── [BranchC]
             -> chat nodes
             -> [end-chp1:branchC]
```

────────────────────────────
7. STYLE RULES
────────────────────────────

* Messenger/chat UI feel (real texting behavior)
* Fast back-and-forth dialogue
* Emotional pacing allowed (typing pauses, read receipts implied)
* NPC can send images anytime
* Messages should feel human, not scripted narration

────────────────────────────
8. RESTRICTIONS
────────────────────────────

* ONLY text + image descriptions allowed
* No audio, video, or external systems
* Must be parser-friendly (strict labels, no ambiguity)
* No hidden branching

────────────────────────────
9. STORY REQUIREMENTS
────────────────────────────

You must include:

* Genre: (user-defined or inferred)
* Setting: messenger/chat-based world
* Main Character: Player (unnamed)
* At least 1 NPC (chat partner)

Must include:

* At least 1 image early in Chapter 1
* Each node must contain minimum of 20 chat bubbles
* At least 1 emotional or impactful decision
* Clear differentiation between all 3 endings

────────────────────────────
10. OUTPUT RULE
────────────────────────────

Generate FULL Chapter 1 first using the exact structure above.

Do not include explanations or extra text outside the format.
