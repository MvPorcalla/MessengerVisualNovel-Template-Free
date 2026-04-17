// ════════════════════════════════════════════════════════════════════════
// Assets/Scripts/BubbleSpinner/Core/DialogueExecutor.cs
// ════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using UnityEngine;
using BubbleSpinner.Data;

namespace BubbleSpinner.Core
{
    public class DialogueExecutor
    {
        // ═══════════════════════════════════════════════════════════
        // DEPENDENCIES (injected)
        // ═══════════════════════════════════════════════════════════

        private ConversationAsset conversationAsset;
        private ConversationState state;
        private Dictionary<string, DialogueNode> currentNodes;
        private DialogueNode currentNode;
        private IBubbleSpinnerCallbacks callbacks;

        // ═══════════════════════════════════════════════════════════
        // PENDING DISPLAY BUFFER
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Messages that have been fired via OnMessagesReady but not yet confirmed
        /// by the UI via OnMessagesDisplayComplete.
        ///
        /// The executor does NOT advance state (currentMessageIndex, readMessageIds,
        /// messageHistory) until the UI calls back. This keeps executor state in sync
        /// with what the player has actually seen, not just what was queued.
        ///
        /// On interrupted resume, if this buffer is non-empty, the messages are
        /// re-fired so the UI can display them again. State only advances after
        /// the UI confirms a second time.
        /// </summary>
        private List<MessageData> pendingDisplayMessages = new List<MessageData>();

        /// <summary>
        /// The message index the executor will advance to once pendingDisplayMessages
        /// are confirmed. Held here instead of written to state immediately.
        /// </summary>
        private int pendingNextMessageIndex = -1;

        private bool pendingProcessAfterPlayerMessage = false;
        private ChoiceData pendingChoiceJump = null;

        // ═══════════════════════════════════════════════════════════
        // EVENTS
        // ═══════════════════════════════════════════════════════════

        public event Action<List<MessageData>> OnMessagesReady;
        public event Action<List<ChoiceData>> OnChoicesReady;
        public event Action OnPauseReached;
        public event Action OnConversationEnd;
        public event Action<string> OnChapterChange;

        // ═══════════════════════════════════════════════════════════
        // PROPERTIES
        // ═══════════════════════════════════════════════════════════

        public bool IsInPauseState => state?.isInPauseState ?? false;
        public string CurrentNodeName => state?.currentNodeName ?? "";
        public int CurrentMessageIndex => state?.currentMessageIndex ?? 0;
        public ConversationState GetState() => state;
        public bool HasMoreChapters => false;

        // ═══════════════════════════════════════════════════════════
        // INITIALIZATION
        // ═══════════════════════════════════════════════════════════

        public void Initialize(
            ConversationAsset asset,
            ConversationState conversationState,
            IBubbleSpinnerCallbacks externalCallbacks = null)
        {
            conversationAsset = asset ?? throw new ArgumentNullException(nameof(asset));
            state             = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
            callbacks         = externalCallbacks;

            ValidateChapterId();
            LoadCurrentChapter();
            ValidateState();

            if (!string.IsNullOrEmpty(state.currentNodeName) && currentNodes.ContainsKey(state.currentNodeName))
                currentNode = currentNodes[state.currentNodeName];
        }

        // ═══════════════════════════════════════════════════════════
        // PUBLIC API - MAIN FLOW CONTROL
        // ═══════════════════════════════════════════════════════════

        public void ContinueFromCurrentState()
        {
            if (state == null || currentNodes == null)
            {
                BSDebug.Error("[DialogueExecutor] Cannot continue: not initialized");
                return;
            }

            if (!currentNodes.ContainsKey(state.currentNodeName))
            {
                BSDebug.Error($"[DialogueExecutor] Node '{state.currentNodeName}' not found");
                return;
            }

            currentNode = currentNodes[state.currentNodeName];

            switch (state.resumeTarget)
            {
                case ResumeTarget.Pause:
                    state.isInPauseState = true;
                    OnPauseReached?.Invoke();
                    break;

                case ResumeTarget.Interrupted:
                    state.isInPauseState = false;
                    state.resumeTarget   = ResumeTarget.None;

                    if (pendingDisplayMessages.Count > 0)
                    {
                        // UI was mid-animation when player left — re-fire the same
                        // batch. State has not advanced, so nothing is lost.
                        BSDebug.Info($"[DialogueExecutor] Re-firing {pendingDisplayMessages.Count} pending messages after interrupt");
                        OnMessagesReady?.Invoke(new List<MessageData>(pendingDisplayMessages));
                    }
                    else
                    {
                        // No pending batch — executor was at a clean pause boundary.
                        // Restore the pause so the player can tap continue normally.
                        if (currentNode.ShouldPauseAfter(state.currentMessageIndex))
                        {
                            var pausePoint = currentNode.GetPauseAt(state.currentMessageIndex);
                            if (pausePoint != null && HasContentAfterPause(pausePoint))
                            {
                                state.isInPauseState = true;
                                state.resumeTarget   = ResumeTarget.Pause;
                                OnPauseReached?.Invoke();
                                break;
                            }
                        }

                        DetermineNextActionSkipPause();
                    }
                    break;

                case ResumeTarget.Choices:
                    state.isInPauseState = false;
                    var choiceBlock = currentNode.GetChoiceBlockAt(state.currentMessageIndex);
                    if (choiceBlock != null)
                    {
                        if (PlayerMessageBeforeChoiceWasSent(choiceBlock))
                            OnChoicesReady?.Invoke(choiceBlock.choices);
                        else
                        {
                            state.resumeTarget = ResumeTarget.None;
                            ProcessCurrentNode();
                        }
                    }
                    else
                    {
                        state.resumeTarget = ResumeTarget.None;
                        ProcessCurrentNode();
                    }
                    break;

                case ResumeTarget.End:
                    state.isInPauseState = false;
                    OnConversationEnd?.Invoke();
                    break;

                case ResumeTarget.None:
                default:
                    ProcessCurrentNode();
                    break;
            }
        }

        /// <summary>
        /// Called by UI when player taps the pause/continue button.
        /// </summary>
        public void OnPauseButtonClicked()
        {
            state.isInPauseState = false;
            state.resumeTarget   = ResumeTarget.None;

            var pausePoint = currentNode.GetPauseAt(state.currentMessageIndex);

            if (pausePoint != null && pausePoint.HasPlayerMessage)
            {
                var playerMessage = currentNode.messages[pausePoint.playerMessageIndex];

                BSDebug.Info($"[DialogueExecutor] Player-turn pause — emitting player message: '{playerMessage.content}'");

                // Stage the player message in the pending buffer.
                // State advances in OnMessagesDisplayComplete after the UI confirms.
                pendingDisplayMessages.Clear();
                pendingDisplayMessages.Add(playerMessage);
                pendingNextMessageIndex          = pausePoint.playerMessageIndex + 1;
                pendingProcessAfterPlayerMessage = true;

                OnMessagesReady?.Invoke(new List<MessageData> { playerMessage });
                return;
            }

            // Pure pacing pause — no player message
            ProcessCurrentNode();
        }

        /// <summary>
        /// Called by UI when conversation is exited mid-message sequence.
        /// State has NOT advanced yet (pending buffer holds the in-flight batch),
        /// so resuming will simply re-fire the same messages.
        /// </summary>
        public void NotifyInterrupted()
        {
            if (state == null) return;

            BSDebug.Info($"[DialogueExecutor] Interrupted — {pendingDisplayMessages.Count} messages pending re-fire on resume");
            state.isInPauseState = true;
            state.resumeTarget   = ResumeTarget.Interrupted;

            // Do NOT clear pendingDisplayMessages here.
            // They are the source of truth for what to re-fire on resume.
        }

        /// <summary>
        /// Called by UI when a choice is selected.
        /// </summary>
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
                pendingNextMessageIndex = -1; // chapter/node index managed by ExecuteJump

                OnMessagesReady?.Invoke(new List<MessageData>(choice.preJumpMessages));
                return;
            }

            if (choice.HasJump)
                ExecuteJump(choice.jump);
            else
            {
                BSDebug.Info("[DialogueExecutor] Fall-through choice — continuing node");
                ProcessCurrentNode();
            }
        }

        /// <summary>
        /// Called by UI when the full message batch has finished displaying.
        /// This is the moment state actually advances — not when messages were fired.
        /// </summary>
        public void OnMessagesDisplayComplete()
        {
            // ── Commit the pending batch to state ──────────────────
            if (pendingDisplayMessages.Count > 0)
            {
                foreach (var message in pendingDisplayMessages)
                {
                    if (!state.readMessageIds.Contains(message.messageId))
                    {
                        state.messageHistory.Add(message);
                        state.readMessageIds.Add(message.messageId);
                        CheckAndUnlockCG(message);
                    }
                }

                if (pendingNextMessageIndex >= 0)
                    state.currentMessageIndex = pendingNextMessageIndex;

                pendingDisplayMessages.Clear();
                pendingNextMessageIndex = -1;
            }

            // ── Route to next action ───────────────────────────────
            if (pendingChoiceJump != null)
            {
                var choice    = pendingChoiceJump;
                pendingChoiceJump = null;

                if (choice.HasJump)
                    ExecuteJump(choice.jump);
                else
                    ProcessCurrentNode();
                return;
            }

            if (pendingProcessAfterPlayerMessage)
            {
                pendingProcessAfterPlayerMessage = false;
                ProcessCurrentNode();
                return;
            }

            DetermineNextAction();
        }

        public void AdvanceToNextChapter()
        {
            BSDebug.Warn("[DialogueExecutor] AdvanceToNextChapter() called — use <<jump ChapterId>> in your .bub file instead.");
        }

        // ═══════════════════════════════════════════════════════════
        // CORE PROCESSING LOGIC
        // ═══════════════════════════════════════════════════════════

        private void ProcessCurrentNode()
        {
            if (currentNode == null || state == null)
            {
                BSDebug.Error("[DialogueExecutor] Cannot process: invalid state");
                return;
            }

            var messagesToShow = GetUnreadMessagesToNextStop();

            if (messagesToShow.Count > 0)
            {
                // Stage in pending buffer — state advances in OnMessagesDisplayComplete.
                pendingDisplayMessages.Clear();
                pendingDisplayMessages.AddRange(messagesToShow);
                pendingNextMessageIndex = GetEndIndexForNextStop();

                OnMessagesReady?.Invoke(new List<MessageData>(messagesToShow));
            }
            else
            {
                DetermineNextAction();
            }
        }

        private bool HasContentAfterPause(PausePoint pausePoint)
        {
            if (pausePoint.HasPlayerMessage)
                return true;

            int nextIndex = state.currentMessageIndex + 1;

            if (GetEndIndexForNextStop(nextIndex) > nextIndex)
                return true;

            if (nextIndex < currentNode.messages.Count)
                return true;

            return false;
        }

        private void DetermineNextAction()
        {
            // Check for a regular pause point first
            if (currentNode.ShouldPauseAfter(state.currentMessageIndex))
            {
                var pausePoint = currentNode.GetPauseAt(state.currentMessageIndex);
                if (pausePoint != null && HasContentAfterPause(pausePoint))
                {
                    state.isInPauseState = true;
                    state.resumeTarget   = ResumeTarget.Pause;
                    OnPauseReached?.Invoke();
                    return;
                }
            }

            // Check for a choice block — runs unconditionally, independent of pausePoints
            var choiceBlock = currentNode.GetChoiceBlockAt(state.currentMessageIndex);
            if (choiceBlock != null)
            {
                state.isInPauseState = false;
                state.resumeTarget   = ResumeTarget.Choices;
                OnChoicesReady?.Invoke(choiceBlock.choices);
                return;
            }

            if (currentNode.jump != null && currentNode.jump.IsValid)
            {
                state.isInPauseState = false;
                state.resumeTarget   = ResumeTarget.None;
                ExecuteJump(currentNode.jump);
                return;
            }

            state.isInPauseState = false;
            state.resumeTarget   = ResumeTarget.End;
            OnConversationEnd?.Invoke();
        }

        private void DetermineNextActionSkipPause()
        {
            var choiceBlock = currentNode.GetChoiceBlockAt(state.currentMessageIndex);
            if (choiceBlock != null)
            {
                state.resumeTarget = ResumeTarget.Choices;
                OnChoicesReady?.Invoke(choiceBlock.choices);
                return;
            }

            if (currentNode.jump != null && currentNode.jump.IsValid)
            {
                state.resumeTarget = ResumeTarget.None;
                ExecuteJump(currentNode.jump);
                return;
            }

            state.resumeTarget = ResumeTarget.End;
            OnConversationEnd?.Invoke();
        }

        // ═══════════════════════════════════════════════════════════
        // NODE NAVIGATION
        // ═══════════════════════════════════════════════════════════

        private void ExecuteJump(JumpTarget jump)
        {
            if (jump == null || !jump.IsValid)
            {
                BSDebug.Error("[DialogueExecutor] ExecuteJump called with null or invalid JumpTarget");
                state.resumeTarget = ResumeTarget.End;
                OnConversationEnd?.Invoke();
                return;
            }

            if (jump.isChapterJump)
            {
                BSDebug.Info($"[DialogueExecutor] Chapter jump → '{jump.chapterId}' node:'{jump.nodeName}'");
                LoadChapterById(jump.chapterId, jump.nodeName);
            }
            else
            {
                BSDebug.Info($"[DialogueExecutor] Local jump → '{jump.nodeName}'");

                if (!currentNodes.ContainsKey(jump.nodeName))
                {
                    BSDebug.Error($"[DialogueExecutor] Local jump target '{jump.nodeName}' not found");
                    state.resumeTarget = ResumeTarget.End;
                    OnConversationEnd?.Invoke();
                    return;
                }

                state.currentNodeName     = jump.nodeName;
                state.currentMessageIndex = 0;
                currentNode               = currentNodes[jump.nodeName];
                ProcessCurrentNode();
            }
        }

        private void LoadChapterById(string chapterId, string targetNode)
        {
            BSDebug.Info($"[DialogueExecutor] Loading chapter '{chapterId}' targeting node '{targetNode}'");

            var file = conversationAsset.GetChapterById(chapterId);

            if (file == null)
            {
                BSDebug.Error($"[DialogueExecutor] Chapter '{chapterId}' not found — ending conversation");
                state.resumeTarget = ResumeTarget.End;
                OnConversationEnd?.Invoke();
                return;
            }

            state.currentChapterId    = chapterId;
            state.currentMessageIndex = 0;
            state.readMessageIds.Clear();
            pendingDisplayMessages.Clear();
            pendingNextMessageIndex = -1;

            currentNodes = BubbleSpinnerParser.Parse(file, conversationAsset.characterName);

            if (currentNodes == null || currentNodes.Count == 0)
            {
                BSDebug.Error($"[DialogueExecutor] Failed to parse chapter '{chapterId}'");
                state.resumeTarget = ResumeTarget.End;
                OnConversationEnd?.Invoke();
                return;
            }

            callbacks?.OnChapterChanged(state.conversationId, chapterId, chapterId);
            OnChapterChange?.Invoke(chapterId);

            if (currentNodes.ContainsKey(targetNode))
            {
                state.currentNodeName     = targetNode;
                state.currentMessageIndex = 0;
                currentNode               = currentNodes[targetNode];
                ProcessCurrentNode();
            }
            else
            {
                BSDebug.Error($"[DialogueExecutor] Node '{targetNode}' not found in chapter '{chapterId}'");
                state.resumeTarget = ResumeTarget.End;
                OnConversationEnd?.Invoke();
            }
        }

        // ═══════════════════════════════════════════════════════════
        // MESSAGE COLLECTION
        // ═══════════════════════════════════════════════════════════

        private List<MessageData> GetUnreadMessagesToNextStop()
        {
            var unread     = new List<MessageData>();
            int startIndex = state.currentMessageIndex;
            int endIndex   = GetEndIndexForNextStop();

            for (int i = startIndex; i < endIndex; i++)
            {
                var message = currentNode.messages[i];
                if (!state.readMessageIds.Contains(message.messageId))
                    unread.Add(message);
            }

            return unread;
        }

        private int GetEndIndexForNextPause(int fromIndex = -1)
        {
            int startFrom = fromIndex >= 0 ? fromIndex : state.currentMessageIndex;
            int endIndex  = currentNode.messages.Count;

            foreach (var pausePoint in currentNode.pausePoints)
            {
                if (pausePoint.stopIndex >= startFrom)
                {
                    endIndex = pausePoint.stopIndex;
                    break;
                }
            }

            return endIndex;
        }

        /// <summary>
        /// Returns the next execution boundary from the given index.
        /// Boundaries include both pause points and unresolved choice blocks.
        /// Without this, message batching can skip past an earlier choice block
        /// and only stop when a later pause or later choice is reached.
        /// </summary>
        private int GetEndIndexForNextStop(int fromIndex = -1)
        {
            int startFrom = fromIndex >= 0 ? fromIndex : state.currentMessageIndex;
            int endIndex  = GetEndIndexForNextPause(startFrom);

            foreach (var block in currentNode.choiceBlocks)
            {
                if (block.pauseIndex >= startFrom && block.pauseIndex < endIndex)
                    endIndex = block.pauseIndex;
            }

            return endIndex;
        }

        // ═══════════════════════════════════════════════════════════
        // CG UNLOCK LOGIC
        // ═══════════════════════════════════════════════════════════

        private void CheckAndUnlockCG(MessageData message)
        {
            if (!message.shouldUnlockCG || string.IsNullOrEmpty(message.imagePath))
                return;

            if (state.unlockedCGs.Contains(message.imagePath))
            {
                BSDebug.Info($"[DialogueExecutor] CG already unlocked: {message.imagePath}");
                return;
            }

            state.unlockedCGs.Add(message.imagePath);
            BSDebug.Info($"[DialogueExecutor] CG UNLOCKED: {message.imagePath}");
            callbacks?.OnCGUnlocked(message.imagePath);
        }

        // ═══════════════════════════════════════════════════════════
        // VALIDATION
        // ═══════════════════════════════════════════════════════════

        private void ValidateChapterId()
        {
            if (string.IsNullOrEmpty(state.currentChapterId))
            {
                var entry = conversationAsset.chapters[0];
                state.currentChapterId    = entry.chapterId;
                state.currentMessageIndex = 0;
                state.readMessageIds.Clear();
                BSDebug.Info($"[DialogueExecutor] No chapter ID in state, defaulting to '{state.currentChapterId}'");
            }
        }

        private void LoadCurrentChapter()
        {
            var file = string.IsNullOrEmpty(state.currentChapterId)
                ? conversationAsset.GetEntryPointChapter()
                : conversationAsset.GetChapterById(state.currentChapterId);

            if (file == null)
                throw new InvalidOperationException($"Chapter '{state.currentChapterId}' not found in registry!");

            currentNodes = BubbleSpinnerParser.Parse(file, conversationAsset.characterName);

            if (currentNodes == null || currentNodes.Count == 0)
                throw new InvalidOperationException($"Failed to parse chapter '{state.currentChapterId}'");

            BSDebug.Info($"[DialogueExecutor] Loaded chapter '{state.currentChapterId}' with {currentNodes.Count} nodes");
        }

        private void ValidateState()
        {
            if (string.IsNullOrEmpty(state.currentNodeName) ||
                !currentNodes.ContainsKey(state.currentNodeName))
            {
                var firstNode = GetFirstNodeName();
                BSDebug.Info($"[DialogueExecutor] No saved node, defaulting to '{firstNode}'");
                state.currentNodeName     = firstNode;
                state.currentMessageIndex = 0;
                state.resumeTarget        = ResumeTarget.None;
            }

            if (currentNodes.ContainsKey(state.currentNodeName))
            {
                var node = currentNodes[state.currentNodeName];
                if (state.currentMessageIndex < 0 || state.currentMessageIndex > node.messages.Count)
                {
                    BSDebug.Warn($"[DialogueExecutor] Invalid message index {state.currentMessageIndex}, resetting to 0");
                    state.currentMessageIndex = 0;
                    state.resumeTarget        = ResumeTarget.None;
                }
            }
        }

        private bool PlayerMessageBeforeChoiceWasSent(ChoiceBlock block)
        {
            if (block.pauseIndex <= 0)
                return true;

            int priorIndex = block.pauseIndex - 1;
            if (priorIndex < 0 || priorIndex >= currentNode.messages.Count)
                return true;

            var priorMessage = currentNode.messages[priorIndex];
            if (!priorMessage.IsPlayerMessage)
                return true;

            return state.readMessageIds.Contains(priorMessage.messageId);
        }

        private string GetFirstNodeName()
        {
            if (currentNodes.ContainsKey("Start"))
                return "Start";

            foreach (var key in currentNodes.Keys)
                return key;

            return "";
        }
    }
}