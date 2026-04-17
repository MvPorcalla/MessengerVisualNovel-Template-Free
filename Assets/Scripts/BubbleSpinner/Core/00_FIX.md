What You Want
Request 1 — ... at indent 2

... is valid inside a choice option at indent 2
Acts as a pacing pause between pre-jump messages
Tap resumes the pre-jump message sequence
Currently the parser warns and ignores ... inside any choice block — that warning needs to become indent-aware: warn only if ... is at indent 0 or 1 inside a choice, allow it at indent 2

Request 2 — First Player: line skip pause, with label-match warning

First Player: line in a choice option gets no pause point — it's considered auto-sent by the choice tap
All subsequent Player: lines inside the same option get pause points normally
If a choice option contain label like -> "i was trying to make it work too" but the first line inside is not a Player: speaker — warn the author that there's no player speaker line following the choice label

Ex. 
Valid
-> "Talk to me"
    Player: talk to me pls
    NPC: Hi
    Player: How are you?

Not Valid warn the author
-> "Talk to me"
    NPC: Hi
    Player: How are you?

If no Player: line at all and it goes straight to <<jump>> — that's valid, no warning, just jump

---

Here are the exact changes needed. I'll go file by file.

---

## 1. `MessageData.cs` — Add `pausePoints` to `ChoiceData`

```csharp
// BEFORE
[Serializable]
public class ChoiceData
{
    public string choiceText;
    public JumpTarget jump;
    public List<MessageData> preJumpMessages;

    public ChoiceData()
    {
        preJumpMessages = new List<MessageData>();
    }

    public ChoiceData(string text, JumpTarget jumpTarget)
    {
        choiceText      = text;
        jump            = jumpTarget;
        preJumpMessages = new List<MessageData>();
    }

    public bool HasJump => jump != null && jump.IsValid;
    public bool HasPreJumpMessages => preJumpMessages != null && preJumpMessages.Count > 0;
}
```

```csharp
// AFTER
[Serializable]
public class ChoiceData
{
    public string choiceText;
    public JumpTarget jump;
    public List<MessageData> preJumpMessages;
    public List<PausePoint> preJumpPausePoints;     // pause points within pre-jump sequence

    public ChoiceData()
    {
        preJumpMessages    = new List<MessageData>();
        preJumpPausePoints = new List<PausePoint>();
    }

    public ChoiceData(string text, JumpTarget jumpTarget)
    {
        choiceText         = text;
        jump               = jumpTarget;
        preJumpMessages    = new List<MessageData>();
        preJumpPausePoints = new List<PausePoint>();
    }

    public bool HasJump => jump != null && jump.IsValid;
    public bool HasPreJumpMessages => preJumpMessages != null && preJumpMessages.Count > 0;
    public bool HasPreJumpPausePoints => preJumpPausePoints != null && preJumpPausePoints.Count > 0;
}
```

---

## 2. `BubbleSpinnerParser.cs` — Four changes

### Change A — Update the summary comment at the top

```csharp
// BEFORE
///   >> choice / >> endchoice                          — choice block (endchoice required, must be at indent 0)
///     -> "Option text" <<jump Node>>                  — inline jump choice (indent 1, no pre-jump dialogue)
///     -> "Option text"                                — fall-through choice (indent 1)
///         Speaker: "text"                             — pre-jump dialogue (indent 2, before <<jump>>)
///         >> media npc type:image path:Key            — pre-jump media (indent 2, before <<jump>>)
///         <<jump Node>>                               — block jump (indent 2, must come after all dialogue)
```

```csharp
// AFTER
///   >> choice / >> endchoice                          — choice block (endchoice required, must be at indent 0)
///     -> "Option text" <<jump Node>>                  — inline jump choice (indent 1, no pre-jump dialogue)
///     -> "Option text"                                — fall-through choice (indent 1)
///         Player: "text"                              — first Player: line (indent 2) — no pause point, auto-sent on choice tap
///         Speaker: "text"                             — pre-jump dialogue (indent 2, before <<jump>>)
///         ...                                         — pre-jump pacing pause (indent 2, valid inside choice option)
///         >> media npc type:image path:Key            — pre-jump media (indent 2, before <<jump>>)
///         <<jump Node>>                               — block jump (indent 2, must come after all dialogue)
```

---

### Change B — `TryParsePausePoint` — allow `...` at indent 2 inside choice

```csharp
// BEFORE
private static bool TryParsePausePoint(string line, ParserContext ctx)
{
    if (line != "...")
        return false;

    if (ctx.inChoiceBlock)
    {
        BSDebug.Warn($"[BubbleSpinner] [{ctx.fileName}:{ctx.lineNumber}] Pause point inside choice block ignored");
        ctx.lastParsedWasTitle = false;
        return true;
    }

    int stopIndex = ctx.currentNode.messages.Count;
    ctx.currentNode.pausePoints.Add(new PausePoint(stopIndex));

    ctx.lastParsedWasTitle = false;
    return true;
}
```

```csharp
// AFTER
private static bool TryParsePausePoint(string line, ParserContext ctx)
{
    if (line != "...")
        return false;

    if (ctx.inChoiceBlock)
    {
        // ... at indent 2 inside a choice option — valid pre-jump pacing pause
        if (ctx.indentLevel == 2 && ctx.currentChoice != null)
        {
            if (ctx.choiceJumpSeen)
            {
                BSDebug.Error($"[BubbleSpinner] [{ctx.fileName}:{ctx.lineNumber}] " +
                    $"'...' after <<jump>> in choice '{ctx.currentChoice.choiceText}' is unreachable — ignored");
                ctx.lastParsedWasTitle = false;
                return true;
            }

            int stopIndex = ctx.currentChoice.preJumpMessages.Count;
            ctx.currentChoice.preJumpPausePoints.Add(new PausePoint(stopIndex));

            ctx.lastParsedWasTitle = false;
            return true;
        }

        // ... at indent 0 or 1 inside choice block — not valid
        BSDebug.Warn($"[BubbleSpinner] [{ctx.fileName}:{ctx.lineNumber}] " +
            $"'...' inside choice block must be at indent 2 inside a choice option — found at indent {ctx.indentLevel}, ignored");
        ctx.lastParsedWasTitle = false;
        return true;
    }

    int nodeStopIndex = ctx.currentNode.messages.Count;
    ctx.currentNode.pausePoints.Add(new PausePoint(nodeStopIndex));

    ctx.lastParsedWasTitle = false;
    return true;
}
```

---

### Change C — `TryParseDialogueLine` — skip pause point on first `Player:` line in choice option

```csharp
// BEFORE — inside the inChoiceBlock branch, at the bottom
// Valid pre-jump dialogue — add to choice, never generate pause points
MessageData.MessageType msgType = speaker.ToLower() == "system"
    ? MessageData.MessageType.System
    : MessageData.MessageType.Text;

var message = new MessageData(msgType, speaker, content);
ctx.currentChoice.preJumpMessages.Add(message);

ctx.lastParsedWasTitle = false;
return true;
```

```csharp
// AFTER — inside the inChoiceBlock branch, at the bottom
// Valid pre-jump dialogue
MessageData.MessageType msgType = speaker.ToLower() == "system"
    ? MessageData.MessageType.System
    : MessageData.MessageType.Text;

var message = new MessageData(msgType, speaker, content);

bool isFirstMessage = ctx.currentChoice.preJumpMessages.Count == 0;

ctx.currentChoice.preJumpMessages.Add(message);

// Player: lines after the first generate pause points.
// The first Player: line is considered auto-sent by the choice tap — no pause needed.
// Subsequent Player: lines pause and wait for tap before continuing.
if (message.IsPlayerMessage && !isFirstMessage)
{
    int playerMsgIndex = ctx.currentChoice.preJumpMessages.Count - 1;
    ctx.currentChoice.preJumpPausePoints.Add(new PausePoint(playerMsgIndex, playerMsgIndex));
}

ctx.lastParsedWasTitle = false;
return true;
```

---

### Change D — `ValidateAndAddChoice` — warn if first pre-jump message is not `Player:`

```csharp
// BEFORE
private static void ValidateAndAddChoice(ParserContext ctx)
{
    if (ctx.currentChoice == null) return;

    ctx.currentNode.choices.Add(ctx.currentChoice);
}
```

```csharp
// AFTER
private static void ValidateAndAddChoice(ParserContext ctx)
{
    if (ctx.currentChoice == null) return;

    // Warn if the choice label has no Player: line as its first message.
    // The choice label text is never displayed in chat on its own —
    // it only appears if followed immediately by a Player: bubble.
    bool firstLineIsPlayer = ctx.currentChoice.preJumpMessages.Count > 0
        && ctx.currentChoice.preJumpMessages[0].IsPlayerMessage;

    if (!firstLineIsPlayer)
    {
        BSDebug.Warn($"[BubbleSpinner] [{ctx.fileName}:{ctx.lineNumber}] " +
            $"Choice '{ctx.currentChoice.choiceText}' has no Player: line as its first message " +
            $"— choice label will not appear as a chat bubble");
    }

    ctx.currentNode.choices.Add(ctx.currentChoice);
}
```

---

## 3. `DialogueExecutor.cs` — Handle pre-jump pause points

This is the executor side of Request 1. The pre-jump sequence currently fires all messages at once via `OnMessagesReady`. It needs to step through `preJumpPausePoints` the same way `ProcessCurrentNode` steps through node-level pause points.

### Add new fields

```csharp
// ADD these alongside the existing pending fields
private int preJumpMessageIndex = 0;        // current position in pre-jump sequence
private ChoiceData activePreJumpChoice = null; // choice whose pre-jump sequence is running
```

### Replace `OnChoiceSelected`

```csharp
// BEFORE
public void OnChoiceSelected(ChoiceData choice)
{
    BSDebug.Info($"[DialogueExecutor] Choice selected: {choice.choiceText}");

    state.isInPauseState = false;
    state.resumeTarget   = ResumeTarget.None;

    if (choice.HasPreJumpMessages)
    {
        pendingChoiceJump = choice;

        // Stage pre-jump messages in the pending buffer.
        pendingDisplayMessages.Clear();
        pendingDisplayMessages.AddRange(choice.preJumpMessages);
        pendingNextMessageIndex = -1;

        OnMessagesReady?.Invoke(new List<MessageData>(choice.preJumpMessages));
        return;
    }

    if (choice.HasJump)
        ExecuteJump(choice.jump);
    else
    {
        BSDebug.Info("[DialogueExecutor] Fall-through choice — continuing node");
        currentNode.choicesResolved = true;
        ProcessCurrentNode();
    }
}
```

```csharp
// AFTER
public void OnChoiceSelected(ChoiceData choice)
{
    BSDebug.Info($"[DialogueExecutor] Choice selected: {choice.choiceText}");

    state.isInPauseState = false;
    state.resumeTarget   = ResumeTarget.None;

    if (choice.HasPreJumpMessages)
    {
        activePreJumpChoice  = choice;
        preJumpMessageIndex  = 0;
        pendingChoiceJump    = choice;

        ProcessPreJumpSequence();
        return;
    }

    if (choice.HasJump)
        ExecuteJump(choice.jump);
    else
    {
        BSDebug.Info("[DialogueExecutor] Fall-through choice — continuing node");
        currentNode.choicesResolved = true;
        ProcessCurrentNode();
    }
}
```

### Add `ProcessPreJumpSequence` — new private method, place it in the Core Processing Logic region

```csharp
/// <summary>
/// Steps through the pre-jump message sequence for the active choice.
/// Respects preJumpPausePoints — fires messages up to the next pause,
/// then waits for OnPauseButtonClicked to continue.
/// When all messages are delivered, routes to the choice jump or fall-through.
/// </summary>
private void ProcessPreJumpSequence()
{
    if (activePreJumpChoice == null) return;

    var messages    = activePreJumpChoice.preJumpMessages;
    var pausePoints = activePreJumpChoice.preJumpPausePoints;

    // Find the next pause point at or after current index
    int endIndex = messages.Count;

    foreach (var pausePoint in pausePoints)
    {
        if (pausePoint.stopIndex >= preJumpMessageIndex)
        {
            endIndex = pausePoint.stopIndex;
            break;
        }
    }

    // Collect messages from current index to endIndex
    var batch = new List<MessageData>();
    for (int i = preJumpMessageIndex; i < endIndex; i++)
        batch.Add(messages[i]);

    if (batch.Count > 0)
    {
        pendingDisplayMessages.Clear();
        pendingDisplayMessages.AddRange(batch);
        pendingNextMessageIndex = -1; // pre-jump sequence manages its own index

        OnMessagesReady?.Invoke(new List<MessageData>(batch));
        preJumpMessageIndex = endIndex;
    }
    else
    {
        // No messages before next pause — check if we hit a pause point
        var pausePoint = pausePoints.Find(p => p.stopIndex == preJumpMessageIndex);

        if (pausePoint != null && pausePoint.HasPlayerMessage)
        {
            // Player-turn pause inside pre-jump sequence
            var playerMessage = messages[pausePoint.playerMessageIndex];

            pendingDisplayMessages.Clear();
            pendingDisplayMessages.Add(playerMessage);
            pendingNextMessageIndex = -1;

            preJumpMessageIndex = pausePoint.playerMessageIndex + 1;

            OnMessagesReady?.Invoke(new List<MessageData> { playerMessage });
            return;
        }

        // All pre-jump messages delivered — execute the jump
        FinishPreJumpSequence();
    }
}

private void FinishPreJumpSequence()
{
    var choice          = activePreJumpChoice;
    activePreJumpChoice = null;
    preJumpMessageIndex = 0;
    pendingChoiceJump   = null;

    if (choice.HasJump)
        ExecuteJump(choice.jump);
    else
    {
        currentNode.choicesResolved = true;
        ProcessCurrentNode();
    }
}
```

### Update `OnMessagesDisplayComplete` — route pre-jump continuation correctly

```csharp
// BEFORE — the routing block at the bottom of OnMessagesDisplayComplete
if (pendingChoiceJump != null)
{
    var choice    = pendingChoiceJump;
    pendingChoiceJump = null;

    if (choice.HasJump)
        ExecuteJump(choice.jump);
    else
    {
        currentNode.choicesResolved = true;
        ProcessCurrentNode();
    }
    return;
}
```

```csharp
// AFTER
if (activePreJumpChoice != null)
{
    // Check if there are more messages or pause points remaining
    if (preJumpMessageIndex < activePreJumpChoice.preJumpMessages.Count)
    {
        // More pre-jump content — check if we're at a pause point
        var pausePoint = activePreJumpChoice.preJumpPausePoints
            .Find(p => p.stopIndex == preJumpMessageIndex);

        if (pausePoint != null)
        {
            state.isInPauseState = true;
            state.resumeTarget   = ResumeTarget.Pause;
            OnPauseReached?.Invoke();
        }
        else
        {
            ProcessPreJumpSequence();
        }
    }
    else
    {
        FinishPreJumpSequence();
    }
    return;
}

if (pendingChoiceJump != null)
{
    var choice    = pendingChoiceJump;
    pendingChoiceJump = null;

    if (choice.HasJump)
        ExecuteJump(choice.jump);
    else
    {
        currentNode.choicesResolved = true;
        ProcessCurrentNode();
    }
    return;
}
```

### Update `OnPauseButtonClicked` — handle pre-jump pause resume

```csharp
// BEFORE
public void OnPauseButtonClicked()
{
    state.isInPauseState = false;
    state.resumeTarget   = ResumeTarget.None;

    var pausePoint = currentNode.GetPauseAt(state.currentMessageIndex);

    if (pausePoint != null && pausePoint.HasPlayerMessage)
    {
        // ... player message handling
        return;
    }

    // Pure pacing pause — no player message
    ProcessCurrentNode();
}
```

```csharp
// AFTER
public void OnPauseButtonClicked()
{
    state.isInPauseState = false;
    state.resumeTarget   = ResumeTarget.None;

    // If we're mid pre-jump sequence, continue it instead of the node
    if (activePreJumpChoice != null)
    {
        ProcessPreJumpSequence();
        return;
    }

    var pausePoint = currentNode.GetPauseAt(state.currentMessageIndex);

    if (pausePoint != null && pausePoint.HasPlayerMessage)
    {
        var playerMessage = currentNode.messages[pausePoint.playerMessageIndex];

        BSDebug.Info($"[DialogueExecutor] Player-turn pause — emitting player message: '{playerMessage.content}'");

        pendingDisplayMessages.Clear();
        pendingDisplayMessages.Add(playerMessage);
        pendingNextMessageIndex          = pausePoint.playerMessageIndex + 1;
        pendingProcessAfterPlayerMessage = true;

        OnMessagesReady?.Invoke(new List<MessageData> { playerMessage });
        return;
    }

    ProcessCurrentNode();
}
```

---

That's the complete changeset. Four files touched, no existing behavior broken for nodes outside choice blocks. The pre-jump pause system is fully isolated to `activePreJumpChoice` so it can't interfere with normal node processing.