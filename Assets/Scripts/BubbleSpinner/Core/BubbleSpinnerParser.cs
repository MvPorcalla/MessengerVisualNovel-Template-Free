// ════════════════════════════════════════════════════════════════════════
// Assets/Scripts/BubbleSpinner/Core/BubbleSpinnerParser.cs
// ════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using BubbleSpinner.Data;

namespace BubbleSpinner.Core
{
    /// <summary>
    /// Parses .bub dialogue files into a structured format of DialogueNodes, Messages, and Choices.
    /// Supports dialogue lines, media commands, jump commands, pause points, and choice blocks.
    ///
    /// .bub syntax summary:
    ///   title: NodeName                                   — declares a node
    ///   ---                                               — opens node content (must follow title:)
    ///   ...                                               — pure pacing pause (tap to continue, nothing sent)
    ///                                                     — no pause points inside choice (... is ignored in choice blocks)
    ///   Player: "text"                                    — implicit pause point; tap sends the message then NPC continues
    ///   Speaker: "text"                                   — NPC or System message
    ///   >> media npc type:image [unlock:true] path:Key    — image bubble
    ///   >> choice / >> endchoice                          — choice block (endchoice required, must be at indent 0)
    ///     -> "Option text" <<jump Node>>                  — inline jump choice (indent 1, no pre-jump dialogue)
    ///     -> "Option text"                                — fall-through choice (indent 1)
    ///         Speaker: "text"                             — pre-jump dialogue (indent 2, before <<jump>>)
    ///         >> media npc type:image path:Key            — pre-jump media (indent 2, before <<jump>>)
    ///         <<jump Node>>                               — block jump (indent 2, must come after all dialogue)
    ///   //                                                — comment (inline or full line)
    /// >> END                                              — marks a node as an explicit conversation end (no jump, not a warning)
    /// </summary>
    public static class BubbleSpinnerParser
    {
        /// <summary>
        /// DISPATCH CHAIN — ORDER IS LOAD-BEARING
        /// ═══════════════════════════════════════════════════════════
        /// These checks are not independent. Reordering them can cause
        /// silent misparses with no warnings or errors.
        ///
        /// Key ordering constraints:
        ///   TryParseMediaCommand before TryParseDialogueLine
        ///     → media commands contain ":" and will match as dialogue lines
        ///
        ///   TryParseChoiceOption before TryParseJumpCommand
        ///     → both match "<<jump"; choice context disambiguates them
        ///
        ///   TryParseNodeOpen (above) before all of these
        ///     → isNodeOpen must be set before any content handlers run
        /// </summary>
        private class ParserContext
        {
            public DialogueNode currentNode;
            public ChoiceData currentChoice;
            public bool inChoiceBlock;
            public bool choiceJumpSeen;
            public bool lastParsedWasTitle;
            public bool isNodeOpen;
            public bool hasWarnedAboutMissingOpen;
            public int indentLevel;
            public int lineNumber;
            public string fileName;
            public string chapterId;
        }

        // ═══════════════════════════════════════════════════════════
        // PUBLIC API
        // ═══════════════════════════════════════════════════════════

        public static Dictionary<string, DialogueNode> Parse(TextAsset bubbleFile, string expectedCharacterName = "")
        {
            var nodes = new Dictionary<string, DialogueNode>();

            if (bubbleFile == null)
            {
                BSDebug.Error("[BubbleSpinner] Null .bub file provided");
                return nodes;
            }

            BSDebug.Info($"[BubbleSpinner] Parsing: {bubbleFile.name}");

            string[] lines = bubbleFile.text.Split('\n');
            var context = new ParserContext { fileName = bubbleFile.name };

            for (int i = 0; i < lines.Length; i++)
            {
                string rawLine = lines[i];
                context.lineNumber = i + 1;
                context.indentLevel = MeasureIndent(rawLine, context);
                string line = rawLine.Trim();

                try
                {
                    line = StripInlineComments(line);

                    if (string.IsNullOrEmpty(line) || line.StartsWith("//"))
                        continue;

                    if (line.StartsWith("contact:"))
                    {
                        if (!string.IsNullOrEmpty(expectedCharacterName))
                        {
                            string contactName = line.Substring(8).Trim();
                            if (!string.IsNullOrEmpty(contactName) &&
                                !contactName.Equals(expectedCharacterName, StringComparison.OrdinalIgnoreCase))
                            {
                                BSDebug.Warn($"[BubbleSpinner] [{context.fileName}] contact: mismatch! " +
                                    $"File says '{contactName}' but asset expects '{expectedCharacterName}'");
                            }
                        }
                        context.lastParsedWasTitle = false;
                        continue;
                    }

                    if (line.StartsWith("chapter:"))
                    {
                        string declaredChapterId = line.Substring(8).Trim();
                        if (string.IsNullOrEmpty(declaredChapterId))
                        {
                            BSDebug.Warn($"[BubbleSpinner] [{context.fileName}:{context.lineNumber}] chapter: declaration is empty");
                        }
                        else
                        {
                            context.chapterId = declaredChapterId;
                            BSDebug.Info($"[BubbleSpinner] [{context.fileName}] chapter: '{declaredChapterId}'");
                        }
                        context.lastParsedWasTitle = false;
                        continue;
                    }

                    if (TryParseNodeTitle(line, context, nodes)) continue;
                    if (TryParseNodeOpen(line, context, nodes)) continue;

                    if (context.currentNode == null)
                    {
                        BSDebug.Warn($"[BubbleSpinner] [{context.fileName}:{context.lineNumber}] Content outside node: {line}");
                        context.lastParsedWasTitle = false;
                        continue;
                    }

                    if (!context.isNodeOpen)
                    {
                        if (!context.hasWarnedAboutMissingOpen)
                        {
                            BSDebug.Warn($"[BubbleSpinner] [{context.fileName}:{context.lineNumber}] " +
                                $"Node '{context.currentNode.nodeName}' is missing its opening '---' — all content will be ignored until '---' is found");
                            context.hasWarnedAboutMissingOpen = true;
                        }
                        context.lastParsedWasTitle = false;
                        continue;
                    }

                    if (TryParsePausePoint(line, context)) continue;
                    if (TryParseEndCommand(line, context)) continue;
                    if (TryParseChoiceBlockStart(line, context)) continue;
                    if (TryParseChoiceBlockEnd(line, context)) continue;
                    if (TryParseChoiceOption(line, context)) continue;  // handles <<jump>> inside choice block
                    if (TryParseJumpCommand(line, context)) continue;   // handles <<jump>> outside choice block
                    if (TryParseMediaCommand(line, context)) continue;
                    if (TryParseDialogueLine(line, context)) continue;

                    BSDebug.Warn($"[BubbleSpinner] [{context.fileName}:{context.lineNumber}] Unrecognized: {line}");
                    context.lastParsedWasTitle = false;
                }
                catch (Exception ex)
                {
                    BSDebug.Error($"[BubbleSpinner] [{context.fileName}:{context.lineNumber}] Parse error: {line}\n{ex.Message}");
                }
            }

            FinalizeParser(context, nodes);
            ValidateDialogueGraph(nodes, bubbleFile.name);
            AssignNodeMessageIds(nodes);

            BSDebug.Info($"[BubbleSpinner] Parsed {nodes.Count} nodes from {bubbleFile.name}");
            return nodes;
        }

        // ═══════════════════════════════════════════════════════════
        // DETERMINISTIC MESSAGE ID ASSIGNMENT
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Assigns deterministic messageIds to all messages after the full parse is complete.
        /// Format: "{nodeName}_{messageIndex}"
        /// Called once per Parse() invocation after all nodes are finalized.
        /// </summary>
        private static void AssignNodeMessageIds(Dictionary<string, DialogueNode> nodes)
        {
            foreach (var kvp in nodes)
            {
                var node = kvp.Value;
                string nodeName = node.nodeName;

                for (int i = 0; i < node.messages.Count; i++)
                {
                    node.messages[i].messageId = $"{nodeName}_{i}";
                }

                // Assign IDs to pre-jump messages inside choices
                for (int c = 0; c < node.choices.Count; c++)
                {
                    var choice = node.choices[c];
                    for (int m = 0; m < choice.preJumpMessages.Count; m++)
                    {
                        choice.preJumpMessages[m].messageId = $"{nodeName}_choice{c}_prejump{m}";
                    }
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        // PARSING METHODS
        // ═══════════════════════════════════════════════════════════

        private static string StripInlineComments(string line)
        {
            int commentIndex = line.IndexOf("//");
            return commentIndex >= 0 ? line.Substring(0, commentIndex).Trim() : line;
        }

        private static bool TryParseNodeTitle(string line, ParserContext ctx, Dictionary<string, DialogueNode> nodes)
        {
            if (!line.StartsWith("title:"))
                return false;

            if (ctx.currentNode != null)
            {
                if (ctx.isNodeOpen)
                {
                    BSDebug.Warn($"[BubbleSpinner] [{ctx.fileName}:{ctx.lineNumber}] " +
                        $"Node '{ctx.currentNode.nodeName}' was never closed with '---' — closing implicitly");
                }
                FinalizeCurrentNode(ctx, nodes);
            }

            string nodeName = line.Substring(6).Trim();

            if (string.IsNullOrEmpty(nodeName))
            {
                BSDebug.Warn($"[BubbleSpinner] [{ctx.fileName}:{ctx.lineNumber}] Empty node name");
                ctx.lastParsedWasTitle = false;
                return true;
            }

            if (nodes.ContainsKey(nodeName))
            {
                BSDebug.Warn($"[BubbleSpinner] [{ctx.fileName}:{ctx.lineNumber}] Duplicate node '{nodeName}'");
            }

            ctx.currentNode = new DialogueNode(nodeName);
            ctx.isNodeOpen = false;
            ctx.hasWarnedAboutMissingOpen = false;
            ctx.inChoiceBlock = false;
            ctx.currentChoice = null;
            ctx.lastParsedWasTitle = true;

            return true;
        }

        /// <summary>
        /// Handles --- which opens or closes a node.
        /// First --- after title: opens the node content.
        /// Second --- closes the node.
        /// Orphan --- with no current node is an error.
        /// </summary>
        private static bool TryParseNodeOpen(string line, ParserContext ctx, Dictionary<string, DialogueNode> nodes)
        {
            if (line != "---")
                return false;

            // No node at all — orphan closing ---
            if (ctx.currentNode == null)
            {
                BSDebug.Error($"[BubbleSpinner] [{ctx.fileName}:{ctx.lineNumber}] '---' found with no node — missing title: before this");
                ctx.lastParsedWasTitle = false;
                return true;
            }

            if (!ctx.isNodeOpen)
            {
                // Opening ---
                if (!ctx.lastParsedWasTitle)
                {
                    BSDebug.Warn($"[BubbleSpinner] [{ctx.fileName}:{ctx.lineNumber}] '---' found without preceding title: — ignored");
                }

                ctx.isNodeOpen = true;
                ctx.lastParsedWasTitle = false;
                return true;
            }
            else
            {
                // Closing ---
                FinalizeCurrentNode(ctx, nodes);
                ctx.isNodeOpen = false;
                ctx.lastParsedWasTitle = false;
                return true;
            }
        }

        private static bool TryParseJumpCommand(string line, ParserContext ctx)
        {
            if (!line.StartsWith("<<jump") || !line.EndsWith(">>"))
                return false;

            string jumpBody = line.Substring(6, line.Length - 8).Trim();

            if (string.IsNullOrEmpty(jumpBody))
            {
                BSDebug.Warn($"[BubbleSpinner] [{ctx.fileName}:{ctx.lineNumber}] Empty jump target");
                ctx.lastParsedWasTitle = false;
                return true;
            }

            JumpTarget jumpTarget = ParseJumpTarget(jumpBody, ctx);

            if (jumpTarget == null)
            {
                ctx.lastParsedWasTitle = false;
                return true;
            }

            if (ctx.inChoiceBlock)
            {
                if (ctx.indentLevel == 0)
                {
                    BSDebug.Error($"[BubbleSpinner] [{ctx.fileName}:{ctx.lineNumber}] " +
                        $"Unexpected <<jump>> at indent 0 inside choice block — use '>> endchoice' before a node-level jump");
                    ctx.lastParsedWasTitle = false;
                    return true;
                }

                if (ctx.indentLevel == 1)
                {
                    BSDebug.Error($"[BubbleSpinner] [{ctx.fileName}:{ctx.lineNumber}] " +
                        $"<<jump>> at indent 1 is invalid — must be at indent 2 to belong to a choice, or indent 0 for node level");
                    ctx.lastParsedWasTitle = false;
                    return true;
                }

                // indent 2 — belongs to current choice
                if (ctx.currentChoice == null)
                {
                    BSDebug.Error($"[BubbleSpinner] [{ctx.fileName}:{ctx.lineNumber}] " +
                        $"<<jump>> at indent 2 but no choice is open");
                    ctx.lastParsedWasTitle = false;
                    return true;
                }

                if (ctx.currentChoice.HasJump)
                {
                    BSDebug.Warn($"[BubbleSpinner] [{ctx.fileName}:{ctx.lineNumber}] " +
                        $"Choice '{ctx.currentChoice.choiceText}' already has a jump target — duplicate ignored");
                    ctx.lastParsedWasTitle = false;
                    return true;
                }

                ctx.currentChoice.jump = jumpTarget;
                ctx.choiceJumpSeen = true;
            }
            else
            {
                // indent 0 — node level
                ctx.currentNode.jump = jumpTarget;
            }

            ctx.lastParsedWasTitle = false;
            return true;
        }

        /// <summary>
        /// Parses the body of a <<jump>> command into a JumpTarget.
        /// 
        /// Supported forms:
        ///   NodeName                            — local node jump
        ///   chapter:Ch2                         — chapter jump, defaults to Start node
        ///   chapter:Ch2 node:Branch_A           — chapter jump to specific node
        /// 
        /// Returns null and logs an error if the jump body is malformed.
        /// </summary>
        private static JumpTarget ParseJumpTarget(string jumpBody, ParserContext ctx)
        {
            if (jumpBody.StartsWith("chapter:"))
            {
                // Chapter jump — extract chapter ID and optional node
                string remainder = jumpBody.Substring(8).Trim();

                string chapterId = null;
                string nodeName  = "Start";

                int nodeKeyword = remainder.IndexOf("node:", StringComparison.OrdinalIgnoreCase);

                if (nodeKeyword >= 0)
                {
                    chapterId = remainder.Substring(0, nodeKeyword).Trim();
                    nodeName  = remainder.Substring(nodeKeyword + 5).Trim();

                    if (string.IsNullOrEmpty(nodeName))
                    {
                        BSDebug.Warn($"[BubbleSpinner] [{ctx.fileName}:{ctx.lineNumber}] " +
                            $"chapter jump has empty node: value — defaulting to 'Start'");
                        nodeName = "Start";
                    }
                }
                else
                {
                    chapterId = remainder;
                }

                if (string.IsNullOrEmpty(chapterId))
                {
                    BSDebug.Error($"[BubbleSpinner] [{ctx.fileName}:{ctx.lineNumber}] " +
                        $"chapter jump has no chapter ID");
                    return null;
                }

                BSDebug.Info($"[BubbleSpinner] [{ctx.lineNumber}] Chapter jump → '{chapterId}' node:'{nodeName}'");
                return JumpTarget.ToChapter(chapterId, nodeName);
            }
            else
            {
                // Local node jump — must not contain spaces
                if (jumpBody.Contains(" "))
                {
                    BSDebug.Error($"[BubbleSpinner] [{ctx.fileName}:{ctx.lineNumber}] " +
                        $"Local jump target '{jumpBody}' contains spaces — did you mean 'chapter:{jumpBody}'?");
                    return null;
                }

                return JumpTarget.ToNode(jumpBody);
            }
        }

        /// <summary>
        /// Handles standalone ... pause points.
        /// Pure pacing pause — shows continue button, nothing sent on tap.
        /// Player: "text" lines are implicit pause points handled in TryParseDialogueLine.
        /// </summary>
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

        /// <summary>
        /// Handles >> END — marks the node as an intentional conversation stop.
        /// Prevents ValidateDialogueGraph from warning about missing jumps on this node.
        /// Must be at indent 0. Cannot appear inside a choice block.
        /// </summary>
        private static bool TryParseEndCommand(string line, ParserContext ctx)
        {
            if (line != ">> END")
                return false;

            if (ctx.inChoiceBlock)
            {
                BSDebug.Error($"[BubbleSpinner] [{ctx.fileName}:{ctx.lineNumber}] '>> END' inside choice block is not allowed — ignored");
                ctx.lastParsedWasTitle = false;
                return true;
            }

            if (ctx.indentLevel != 0)
            {
                BSDebug.Error($"[BubbleSpinner] [{ctx.fileName}:{ctx.lineNumber}] '>> END' must be at indent 0 — found at indent {ctx.indentLevel}, ignored");
                ctx.lastParsedWasTitle = false;
                return true;
            }

            if (ctx.currentNode.jump != null && ctx.currentNode.jump.IsValid)
            {
                BSDebug.Warn($"[BubbleSpinner] [{ctx.fileName}:{ctx.lineNumber}] '>> END' on node '{ctx.currentNode.nodeName}' which already has a jump — >> END ignored");
                ctx.lastParsedWasTitle = false;
                return true;
            }

            ctx.currentNode.isExplicitEnd = true;
            BSDebug.Info($"[BubbleSpinner] [{ctx.fileName}:{ctx.lineNumber}] Explicit end on node '{ctx.currentNode.nodeName}'");

            ctx.lastParsedWasTitle = false;
            return true;
        }

        private static bool TryParseChoiceBlockStart(string line, ParserContext ctx)
        {
            if (line != ">> choice")
                return false;

            if (ctx.indentLevel != 0)
            {
                BSDebug.Error($"[BubbleSpinner] [{ctx.fileName}:{ctx.lineNumber}] '>> choice' must be at indent 0 — found at indent {ctx.indentLevel}");
                ctx.lastParsedWasTitle = false;
                return true;
            }

            if (ctx.inChoiceBlock)
            {
                BSDebug.Warn($"[BubbleSpinner] [{ctx.fileName}:{ctx.lineNumber}] '>> choice' found inside an open choice block — close with '>> endchoice' first");
                ctx.lastParsedWasTitle = false;
                return true;
            }

            ctx.inChoiceBlock = true;

            // Insert an implicit pause point at the current message count.
            // Store the index on the node so the executor can distinguish this
            // pause from a regular pacing pause and fire choices directly.
            int choicePauseIndex = ctx.currentNode.messages.Count;
            ctx.currentNode.pausePoints.Add(new PausePoint(choicePauseIndex));
            ctx.currentNode.choicePauseIndex = choicePauseIndex;

            ctx.lastParsedWasTitle = false;
            return true;
        }

        private static bool TryParseChoiceBlockEnd(string line, ParserContext ctx)
        {
            if (line != ">> endchoice")
                return false;

            if (ctx.indentLevel != 0)
            {
                BSDebug.Warn($"[BubbleSpinner] [{ctx.fileName}:{ctx.lineNumber}] '>> endchoice' should be at indent 0 — found at indent {ctx.indentLevel}, recovering");
            }

            if (!ctx.inChoiceBlock)
            {
                BSDebug.Warn($"[BubbleSpinner] [{ctx.fileName}:{ctx.lineNumber}] '>> endchoice' found with no open choice block — ignored");
                ctx.lastParsedWasTitle = false;
                return true;
            }

            if (ctx.currentChoice != null)
            {
                ValidateAndAddChoice(ctx);
                ctx.currentChoice = null;
            }

            ctx.inChoiceBlock = false;
            ctx.lastParsedWasTitle = false;
            return true;
        }

        private static bool TryParseChoiceOption(string line, ParserContext ctx)
        {
            if (!ctx.inChoiceBlock || !line.StartsWith("->"))
                return false;

            if (ctx.indentLevel != 1)
            {
                BSDebug.Error($"[BubbleSpinner] [{ctx.fileName}:{ctx.lineNumber}] Choice option '->' must be at indent 1 — found at indent {ctx.indentLevel}");
                ctx.lastParsedWasTitle = false;
                return true;
            }

            // Finalize previous choice if one is pending
            if (ctx.currentChoice != null)
            {
                ValidateAndAddChoice(ctx);
            }

            string remainder = line.Substring(2).Trim();

            if (!remainder.StartsWith("\""))
            {
                BSDebug.Warn($"[BubbleSpinner] [{ctx.fileName}:{ctx.lineNumber}] Choice missing opening quote: {line}");
                ctx.lastParsedWasTitle = false;
                return true;
            }

            int closingQuote = remainder.IndexOf('"', 1);
            if (closingQuote == -1)
            {
                BSDebug.Warn($"[BubbleSpinner] [{ctx.fileName}:{ctx.lineNumber}] Choice missing closing quote: {line}");
                ctx.lastParsedWasTitle = false;
                return true;
            }

            string choiceText = remainder.Substring(1, closingQuote - 1);

            if (string.IsNullOrEmpty(choiceText))
            {
                BSDebug.Warn($"[BubbleSpinner] [{ctx.fileName}:{ctx.lineNumber}] Empty choice text");
                ctx.lastParsedWasTitle = false;
                return true;
            }

            ctx.currentChoice = new ChoiceData(choiceText, null);
            ctx.choiceJumpSeen = false;

            // Check for inline jump: -> "Text" <<jump NodeName>> or <<jump chapter:Ch2>>
            string afterQuote = remainder.Substring(closingQuote + 1).Trim();
            if (afterQuote.StartsWith("<<jump") && afterQuote.EndsWith(">>"))
            {
                string jumpBody = afterQuote.Substring(6, afterQuote.Length - 8).Trim();

                if (jumpBody.Contains("<<jump") || jumpBody.Contains(">>"))
                {
                    BSDebug.Warn($"[BubbleSpinner] [{ctx.fileName}:{ctx.lineNumber}] Malformed inline jump — possible double jump: {line}");
                    ctx.lastParsedWasTitle = false;
                    return true;
                }

                JumpTarget inlineJump = ParseJumpTarget(jumpBody, ctx);
                if (inlineJump != null)
                {
                    ctx.currentChoice.jump = inlineJump;
                    ctx.choiceJumpSeen = true;
                    ValidateAndAddChoice(ctx);
                    ctx.currentChoice = null;
                }
            }

            ctx.lastParsedWasTitle = false;
            return true;
        }

        private static bool TryParseMediaCommand(string line, ParserContext ctx)
        {
            if (!line.StartsWith(">> media"))
                return false;

            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3)
            {
                BSDebug.Warn($"[BubbleSpinner] [{ctx.fileName}:{ctx.lineNumber}] Invalid media command: {line}");
                ctx.lastParsedWasTitle = false;
                return true;
            }

            string speaker = parts[2];
            string imagePath = ExtractPathFromMediaCommand(line);

            if (string.IsNullOrEmpty(imagePath))
            {
                BSDebug.Warn($"[BubbleSpinner] [{ctx.fileName}:{ctx.lineNumber}] No path in media command");
                ctx.lastParsedWasTitle = false;
                return true;
            }

            bool shouldUnlock = line.Contains("unlock:true");

            var imageMessage = new MessageData(MessageData.MessageType.Image, speaker, "", imagePath)
            {
                shouldUnlockCG = shouldUnlock
            };

            if (shouldUnlock)
            {
                BSDebug.Info($"[BubbleSpinner] [{ctx.lineNumber}] Unlockable CG: {imagePath}");
            }

            if (ctx.inChoiceBlock)
            {
                if (ctx.currentChoice == null)
                {
                    BSDebug.Warn($"[BubbleSpinner] [{ctx.fileName}:{ctx.lineNumber}] " +
                        $">> media inside choice block but no option is open — ignored");
                    ctx.lastParsedWasTitle = false;
                    return true;
                }

                if (ctx.indentLevel != 2)
                {
                    BSDebug.Warn($"[BubbleSpinner] [{ctx.fileName}:{ctx.lineNumber}] " +
                        $">> media inside choice option must be at indent 2 — found at indent {ctx.indentLevel}, ignored");
                    ctx.lastParsedWasTitle = false;
                    return true;
                }

                if (ctx.choiceJumpSeen)
                {
                    BSDebug.Error($"[BubbleSpinner] [{ctx.fileName}:{ctx.lineNumber}] " +
                        $">> media after <<jump>> in choice '{ctx.currentChoice.choiceText}' is unreachable — ignored");
                    ctx.lastParsedWasTitle = false;
                    return true;
                }

                ctx.currentChoice.preJumpMessages.Add(imageMessage);
            }
            else
            {
                ctx.currentNode.messages.Add(imageMessage);
            }

            ctx.lastParsedWasTitle = false;
            return true;
        }

        private static bool TryParseDialogueLine(string line, ParserContext ctx)
        {
            if (!line.Contains(":"))
                return false;

            int colonIndex = line.IndexOf(':');
            string speaker = line.Substring(0, colonIndex).Trim();
            string content = line.Substring(colonIndex + 1).Trim();

            if (!Regex.IsMatch(speaker, @"^[\w\s]+$"))
            {
                BSDebug.Warn($"[BubbleSpinner] [{ctx.fileName}:{ctx.lineNumber}] Invalid speaker format, skipping: {line}");
                return false;
            }

            if (string.IsNullOrEmpty(speaker))
            {
                BSDebug.Warn($"[BubbleSpinner] [{ctx.fileName}:{ctx.lineNumber}] Empty speaker: {line}");
                ctx.lastParsedWasTitle = false;
                return true;
            }

            if (content.StartsWith("\"") && content.EndsWith("\""))
            {
                content = content.Substring(1, content.Length - 2);
            }

            if (ctx.inChoiceBlock)
            {
                // Must have an open choice option to attach to
                if (ctx.currentChoice == null)
                {
                    BSDebug.Warn($"[BubbleSpinner] [{ctx.fileName}:{ctx.lineNumber}] " +
                        $"Dialogue inside choice block but no option is open — ignored");
                    ctx.lastParsedWasTitle = false;
                    return true;
                }

                // Must be at indent 2
                if (ctx.indentLevel != 2)
                {
                    BSDebug.Warn($"[BubbleSpinner] [{ctx.fileName}:{ctx.lineNumber}] " +
                        $"Dialogue inside choice option must be at indent 2 — found at indent {ctx.indentLevel}, ignored");
                    ctx.lastParsedWasTitle = false;
                    return true;
                }

                // Dialogue after <<jump>> is unreachable
                if (ctx.choiceJumpSeen)
                {
                    BSDebug.Error($"[BubbleSpinner] [{ctx.fileName}:{ctx.lineNumber}] " +
                        $"Dialogue after <<jump>> in choice '{ctx.currentChoice.choiceText}' is unreachable — ignored");
                    ctx.lastParsedWasTitle = false;
                    return true;
                }

                // Valid pre-jump dialogue — add to choice, never generate pause points
                MessageData.MessageType msgType = speaker.ToLower() == "system"
                    ? MessageData.MessageType.System
                    : MessageData.MessageType.Text;

                var message = new MessageData(msgType, speaker, content);
                ctx.currentChoice.preJumpMessages.Add(message);

                ctx.lastParsedWasTitle = false;
                return true;
            }

            MessageData.MessageType nodeMsgType = speaker.ToLower() == "system"
                ? MessageData.MessageType.System
                : MessageData.MessageType.Text;

            var nodeMessage = new MessageData(nodeMsgType, speaker, content);
            ctx.currentNode.messages.Add(nodeMessage);

            // Player: lines are implicit pause points.
            // The pause stops before this message — player taps, message sends, NPC continues.
            if (nodeMessage.IsPlayerMessage)
            {
                int playerMessageIndex = ctx.currentNode.messages.Count - 1;
                ctx.currentNode.pausePoints.Add(new PausePoint(playerMessageIndex, playerMessageIndex));
            }

            ctx.lastParsedWasTitle = false;
            return true;
        }

        // ═══════════════════════════════════════════════════════════
        // HELPER METHODS
        // ═══════════════════════════════════════════════════════════

        // ═══════════════════════════════════════════════════════════
        // TODO: <<if>> / <<else>> / <<endif>> IMPLEMENTATION NOTES
        // ═══════════════════════════════════════════════════════════
        //
        // CURRENT LIMITATION:
        // The parser is a flat state machine with indent validation only (levels 0–2).
        // It does NOT build a parse tree or track nested scopes.
        //
        // WHEN IMPLEMENTING <<if>>:
        // Replace the flat indent checks with a scope stack:
        //
        //   private Stack<BlockScope> scopeStack = new Stack<BlockScope>();
        //
        //   enum BlockScope { Node, Choice, ChoiceOption, If, Else }
        //
        // This allows the parser to know at any point:
        //   - what block it is currently inside
        //   - what indent level that block opened at
        //   - whether an <<else>> is valid here
        //
        // PLANNED INDENT STRUCTURE WITH <<if>>:
        //
        //   indent 0  — node level (messages, >> choice, <<jump>>)
        //   indent 1  — choice option (->)
        //   indent 2  — choice content (<<jump>>, <<if>>)
        //   indent 3  — body of <<if>> or <<else>>
        //   indent 4  — nested <<if>> inside <<else>>
        //
        // PLANNED SYNTAX:
        //
        //   >> choice
        //       -> "Ask how she's feeling"
        //           <<if hasMet == true>>
        //               <<jump Node_Concern_Met>>
        //           <<else>>
        //               <<jump Node_Concern_New>>
        //           <<endif>>
        //   >> endchoice
        //
        // REQUIRED NEW PARSE METHODS:
        //   TryParseIfBlock()     — opens <<if>> scope, evaluates condition
        //   TryParseElseBlock()   — switches <<if>> scope to <<else>> branch
        //   TryParseEndIf()       — closes <<if>> scope
        //
        // REQUIRED NEW DATA:
        //   ConditionData.cs      — stores condition expression and branch targets
        //   DialogueNode needs a conditions list alongside choices
        //
        // EXECUTOR CHANGES:
        //   DialogueExecutor must evaluate conditions at runtime against game state
        //   A new IConditionEvaluator interface keeps condition logic decoupled
        //   from the parser and executor
        //
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Measures the indent level of a raw line before trimming.
        /// 1 tab = 1 level. Spaces are rounded to nearest tab equivalent (4 spaces = 1 level).
        /// Mixed tabs and spaces on the same line produce a warning.
        /// </summary>
        private static int MeasureIndent(string rawLine, ParserContext ctx)
        {
            int tabs = 0;
            int spaces = 0;
            bool hasTabs = false;
            bool hasSpaces = false;

            foreach (char c in rawLine)
            {
                if (c == '\t') { tabs++; hasTabs = true; }
                else if (c == ' ') { spaces++; hasSpaces = true; }
                else break;
            }

            if (hasTabs && hasSpaces)
            {
                BSDebug.Warn($"[BubbleSpinner] [{ctx.fileName}:{ctx.lineNumber}] Mixed tabs and spaces — treating spaces as tabs");
            }

            int spaceLevel = (int)Math.Round(spaces / 4.0);
            return tabs + spaceLevel;
        }

        private static string ExtractPathFromMediaCommand(string line)
        {
            int pathIndex = line.IndexOf("path:");
            if (pathIndex == -1)
                return "";

            return line.Substring(pathIndex + 5).Trim();
        }

        private static void ValidateAndAddChoice(ParserContext ctx)
        {
            if (ctx.currentChoice == null) return;

            ctx.currentNode.choices.Add(ctx.currentChoice);
        }

        private static void FinalizeCurrentNode(ParserContext ctx, Dictionary<string, DialogueNode> nodes)
        {
            if (ctx.inChoiceBlock)
            {
                BSDebug.Warn($"[BubbleSpinner] [{ctx.fileName}] " +
                    $"Node '{ctx.currentNode.nodeName}' ended with unclosed '>> choice' block — missing '>> endchoice'");

                if (ctx.currentChoice != null)
                {
                    ValidateAndAddChoice(ctx);
                    ctx.currentChoice = null;
                }

                ctx.inChoiceBlock = false;
            }
            else if (ctx.currentChoice != null)
            {
                ValidateAndAddChoice(ctx);
                ctx.currentChoice = null;
            }

            var node = ctx.currentNode;

            if (node.messages.Count == 0 && (node.choices.Count > 0 || (node.jump != null && node.jump.IsValid)))
            {
                BSDebug.Warn($"[BubbleSpinner] [{ctx.fileName}] Node '{node.nodeName}' has no messages");
            }

            if (node.choices.Count > 0 && node.jump != null && node.jump.IsValid)
            {
                bool allChoicesHaveJumps = node.choices.TrueForAll(c => c.HasJump);

                if (allChoicesHaveJumps)
                {
                    BSDebug.Warn($"[BubbleSpinner] [{ctx.fileName}] Node '{node.nodeName}' has both choices and auto-jump — auto-jump is unreachable");
                }
            }

            nodes[node.nodeName] = node;
            ctx.isNodeOpen = false;
            ctx.currentNode = null;
        }

        private static void FinalizeParser(ParserContext ctx, Dictionary<string, DialogueNode> nodes)
        {
            if (ctx.currentNode != null)
            {
                if (ctx.isNodeOpen)
                {
                    BSDebug.Warn($"[BubbleSpinner] [{ctx.fileName}] " +
                        $"File ended with unclosed node '{ctx.currentNode.nodeName}' — missing closing '---'");
                }
                FinalizeCurrentNode(ctx, nodes);
            }
        }

        // ═══════════════════════════════════════════════════════════
        // VALIDATION
        // ═══════════════════════════════════════════════════════════

        private static void ValidateDialogueGraph(Dictionary<string, DialogueNode> nodes, string fileName)
        {
            foreach (var kvp in nodes)
            {
                var node = kvp.Value;

                if (node.jump != null && node.jump.IsValid && !node.jump.isChapterJump)
                {
                    if (!nodes.ContainsKey(node.jump.nodeName))
                        BSDebug.Warn($"[BubbleSpinner] [{fileName}] Node '{node.nodeName}' jumps to non-existent local node '{node.jump.nodeName}'");
                }

                // Warn if node has no jump and no explicit end — likely a missing <<jump>>
                bool hasNoDestination = (node.jump == null || !node.jump.IsValid) &&
                                        node.choices.Count == 0 &&
                                        !node.isExplicitEnd;

                if (hasNoDestination)
                {
                    BSDebug.Warn($"[BubbleSpinner] [{fileName}] Node '{node.nodeName}' has no jump and no '>> END' — did you forget a <<jump>>?");
                }

                foreach (var choice in node.choices)
                {
                    if (choice.jump != null && choice.jump.IsValid && !choice.jump.isChapterJump)
                    {
                        if (!nodes.ContainsKey(choice.jump.nodeName))
                            BSDebug.Warn($"[BubbleSpinner] [{fileName}] Choice '{choice.choiceText}' targets non-existent local node '{choice.jump.nodeName}'");
                    }
                }
            }
        }
    }
}