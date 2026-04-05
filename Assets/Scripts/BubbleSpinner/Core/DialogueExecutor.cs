// ════════════════════════════════════════════════════════════════════════
// Assets/Scripts/BubbleSpinner/Core/DialogueExecutor.cs
// ════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using UnityEngine;
using BubbleSpinner.Data;

namespace BubbleSpinner.Core
{
    /// <summary>
    /// Executes dialogue nodes and manages conversation flow.
    /// This class is responsible for processing the current dialogue node, 
    /// determining what messages to show, when to show choices, when to pause, and when to jump to other nodes or chapters.
    /// It maintains the current state of the conversation and communicates with the UI via events.
    /// </summary>
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

        private bool pendingProcessAfterPlayerMessage = false;
        private ChoiceData pendingChoiceJump = null;

        // ═══════════════════════════════════════════════════════════
        // EVENTS (UI subscribes to these)
        // ═══════════════════════════════════════════════════════════

        /// <summary>Fired when new messages are ready to display</summary>
        public event Action<List<MessageData>> OnMessagesReady;

        /// <summary>Fired when choices should be shown</summary>
        public event Action<List<ChoiceData>> OnChoicesReady;

        /// <summary>Fired when pause button should be shown</summary>
        public event Action OnPauseReached;

        /// <summary>Fired when conversation ends</summary>
        public event Action OnConversationEnd;

        /// <summary>Fired when chapter changes</summary>
        public event Action<string> OnChapterChange;

        // ═══════════════════════════════════════════════════════════
        // PROPERTIES
        // ═══════════════════════════════════════════════════════════

        public bool IsInPauseState => state?.isInPauseState ?? false;
        public string CurrentNodeName => state?.currentNodeName ?? "";
        public int CurrentMessageIndex => state?.currentMessageIndex ?? 0;
        public ConversationState GetState() => state;

        /// <summary>
        /// Returns true if the current node has more messages to show after the current message index.
        /// Used by UI to determine whether to show a continue button or skip directly to choices/next node.
        /// </summary>
        public bool HasMoreChapters => false;

        // ═══════════════════════════════════════════════════════════
        // INITIALIZATION
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Initialize executor with conversation asset, state, and callbacks.
        /// ConversationState should be loaded from save or created new before calling this.
        /// </summary>
        public void Initialize(
            ConversationAsset asset,
            ConversationState conversationState,
            IBubbleSpinnerCallbacks externalCallbacks = null)
        {
            conversationAsset = asset ?? throw new ArgumentNullException(nameof(asset));
            state = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
            callbacks = externalCallbacks;

            ValidateChapterId();
            LoadCurrentChapter();
            ValidateState();

            if (!string.IsNullOrEmpty(state.currentNodeName) && currentNodes.ContainsKey(state.currentNodeName))
            {
                currentNode = currentNodes[state.currentNodeName];
            }
        }

        // ═══════════════════════════════════════════════════════════
        // PUBLIC API - MAIN FLOW CONTROL
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Single public entry point for starting or resuming dialogue after initialization.
        /// Determines where to resume based on the state's resumeTarget and current node/message indices.
        /// </summary>
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
                    // Player exited mid-message sequence — show continue button if there are unread messages,
                    // otherwise determine next action directly.
                    var unreadOnResume = GetUnreadMessagesToNextPause();
                    if (unreadOnResume.Count > 0)
                    {
                        state.isInPauseState = true;
                        OnPauseReached?.Invoke();
                    }
                    else
                    {
                        state.isInPauseState = false;
                        state.resumeTarget = ResumeTarget.None;
                        DetermineNextActionSkipPause();
                    }
                    break;

                case ResumeTarget.Choices:
                    // Player was at choices — show them directly, no message processing needed.
                    state.isInPauseState = false;
                    OnChoicesReady?.Invoke(currentNode.choices);
                    break;

                case ResumeTarget.End:
                    // Player was at the end/next-chapter button — restore it directly.
                    state.isInPauseState = false;
                    OnConversationEnd?.Invoke();
                    break;

                case ResumeTarget.None:
                default:
                    // Fresh start or legacy save with no resumeTarget recorded.
                    ProcessCurrentNode();
                    break;
            }
        }

        /// <summary>
        /// Called by UI when player taps the pause/continue button.
        /// If the pause point has a paired player message, emits it first then resumes NPC flow.
        /// Otherwise continues directly.
        /// </summary>
        public void OnPauseButtonClicked()
        {
            state.isInPauseState = false;
            state.resumeTarget = ResumeTarget.None;

            var pausePoint = currentNode.GetPauseAt(state.currentMessageIndex);

            if (pausePoint != null && pausePoint.HasPlayerMessage)
            {
                var playerMessage = currentNode.messages[pausePoint.playerMessageIndex];

                BSDebug.Info($"[DialogueExecutor] Player-turn pause — emitting player message: '{playerMessage.content}'");

                // Emit the paired player message, then resume NPC processing.
                state.messageHistory.Add(playerMessage);
                state.readMessageIds.Add(playerMessage.messageId);
                state.currentMessageIndex = pausePoint.playerMessageIndex + 1;
                pendingProcessAfterPlayerMessage = true;

                OnMessagesReady?.Invoke(new List<MessageData> { playerMessage });
                return;
            }

            // Pure pacing pause — no player message, continue directly.
            var remainingMessages = GetUnreadMessagesToNextPause();

            if (remainingMessages.Count > 0)
            {
                ProcessCurrentNode();
            }
            else
            {
                DetermineNextActionSkipPause();
            }
        }

        /// <summary>
        /// Called by UI when conversation is exited mid-message sequence.
        /// Sets resumeTarget to Interrupted so re-entry shows the continue button.
        /// </summary>
        public void NotifyInterrupted()
        {
            if (state == null) return;

            BSDebug.Info("[DialogueExecutor] Conversation interrupted - setting ResumeTarget.Interrupted");
            state.isInPauseState = true;
            state.resumeTarget = ResumeTarget.Interrupted;
        }

        /// <summary>
        /// Called by UI when a choice is selected.
        /// Jumps directly to the target node — player message is the first line of that node.
        /// </summary>
        public void OnChoiceSelected(ChoiceData choice)
        {
            BSDebug.Info($"[DialogueExecutor] Choice selected: {choice.choiceText} -> {(choice.jump != null ? choice.jump.nodeName : "none")}");

            state.isInPauseState = false;
            state.resumeTarget = ResumeTarget.None;

            if (choice.HasPreJumpMessages)
            {
                pendingChoiceJump = choice;

                foreach (var message in choice.preJumpMessages)
                {
                    state.messageHistory.Add(message);
                    state.readMessageIds.Add(message.messageId);
                    CheckAndUnlockCG(message);
                }

                OnMessagesReady?.Invoke(choice.preJumpMessages);
                return;
            }

            // No pre-jump messages — jump if target exists, otherwise fall through
            if (choice.HasJump)
            {
                ExecuteJump(choice.jump);
            }
            else
            {
                // Fall-through choice — mark choices resolved so DetermineNextAction
                // skips the choice block on subsequent passes and continues to the jump
                BSDebug.Info($"[DialogueExecutor] Fall-through choice — continuing node");
                currentNode.choicesResolved = true;
                ProcessCurrentNode();
            }
        }

        /// <summary>
        /// Called by UI when messages have finished displaying.
        /// If a player message was just displayed from a pause point, resumes NPC processing.
        /// Otherwise determines the next dialogue action normally.
        /// </summary>
        public void OnMessagesDisplayComplete()
        {
            if (pendingChoiceJump != null)
            {
                var choice = pendingChoiceJump;
                pendingChoiceJump = null;
                ExecuteJump(choice.jump);
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

        /// <summary>
        /// Called by UI when the "Next Chapter" button is clicked at the end of a chapter.
        /// Chapters are no longer sequential — use <<jump ChapterId>> in .bub files instead.
        /// </summary>
        public void AdvanceToNextChapter()
        {
            BSDebug.Warn("[DialogueExecutor] AdvanceToNextChapter() called — chapters are now registry-based. Use <<jump ChapterId>> in your .bub file instead.");
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

            var messagesToShow = GetUnreadMessagesToNextPause();

            if (messagesToShow.Count > 0)
            {
                foreach (var message in messagesToShow)
                {
                    state.messageHistory.Add(message);
                    state.readMessageIds.Add(message.messageId);
                    CheckAndUnlockCG(message);
                }

                int endIndex = GetEndIndexForNextPause();
                state.currentMessageIndex = endIndex;

                OnMessagesReady?.Invoke(messagesToShow);
            }
            else
            {
                DetermineNextAction();
            }
        }

        /// <summary>
        /// Returns true if there is meaningful content remaining after a pause point —
        /// either a paired player message, more messages in the current batch,
        /// or remaining messages in the node.
        /// Prevents showing a continue button at the very end of a node where nothing follows.
        /// </summary>
        private bool HasContentAfterPause(PausePoint pausePoint)
        {
            if (pausePoint.HasPlayerMessage)
                return true;

            int nextIndex = state.currentMessageIndex + 1;

            if (GetEndIndexForNextPause(nextIndex) > nextIndex)
                return true;

            if (nextIndex < currentNode.messages.Count)
                return true;

            return false;
        }

        private void DetermineNextAction()
        {
            if (currentNode.ShouldPauseAfter(state.currentMessageIndex))
            {
                // If this pause point is the implicit choice pause, skip it and
                // go directly to choices — do not show a continue button here.
                bool isChoicePause = currentNode.choicePauseIndex >= 0 &&
                                    state.currentMessageIndex == currentNode.choicePauseIndex;

                if (!isChoicePause)
                {
                    var pausePoint = currentNode.GetPauseAt(state.currentMessageIndex);

                    if (HasContentAfterPause(pausePoint))
                    {
                        state.isInPauseState = true;
                        state.resumeTarget = ResumeTarget.Pause;
                        OnPauseReached?.Invoke();
                        return;
                    }
                }
            }

            if (currentNode.choices != null && currentNode.choices.Count > 0 && !currentNode.choicesResolved)
            {
                state.isInPauseState = false;
                state.resumeTarget = ResumeTarget.Choices;
                OnChoicesReady?.Invoke(currentNode.choices);
                return;
            }

            if (currentNode.jump != null && currentNode.jump.IsValid)
            {
                state.isInPauseState = false;
                state.resumeTarget = ResumeTarget.None;
                ExecuteJump(currentNode.jump);
                return;
            }

            state.isInPauseState = false;
            state.resumeTarget = ResumeTarget.End;
            OnConversationEnd?.Invoke();
        }

        /// <summary>
        /// Determines next action while skipping pause points.
        /// Used when resuming from Interrupted state or continuing after a pure pacing pause
        /// with no remaining messages in the current batch.
        /// Priority: choices → auto-jump → end.
        /// </summary>
        private void DetermineNextActionSkipPause()
        {
            if (currentNode.choices != null && currentNode.choices.Count > 0 && !currentNode.choicesResolved)
            {
                state.resumeTarget = ResumeTarget.Choices;
                OnChoicesReady?.Invoke(currentNode.choices);
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

        /// <summary>
        /// Resolves and executes a JumpTarget.
        /// Local jumps stay in the current chapter file.
        /// Chapter jumps load a new chapter file via LoadChapterById.
        /// </summary>
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
                    BSDebug.Error($"[DialogueExecutor] Local jump target '{jump.nodeName}' not found in current chapter");
                    state.resumeTarget = ResumeTarget.End;
                    OnConversationEnd?.Invoke();
                    return;
                }

                state.currentNodeName = jump.nodeName;
                state.currentMessageIndex = 0;
                currentNode = currentNodes[jump.nodeName];
                ProcessCurrentNode();
            }
        }

        private void LoadChapterById(string chapterId, string targetNode)
        {
            BSDebug.Info($"[DialogueExecutor] Loading chapter '{chapterId}' targeting node '{targetNode}'");

            var file = conversationAsset.GetChapterById(chapterId);

            if (file == null)
            {
                BSDebug.Error($"[DialogueExecutor] Chapter '{chapterId}' not found in registry — ending conversation");
                state.resumeTarget = ResumeTarget.End;
                OnConversationEnd?.Invoke();
                return;
            }

            state.currentChapterId = chapterId;
            state.currentMessageIndex = 0;
            state.readMessageIds.Clear();
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
                state.currentNodeName = targetNode;
                state.currentMessageIndex = 0;
                currentNode = currentNodes[targetNode];
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

        private List<MessageData> GetUnreadMessagesToNextPause()
        {
            var unread = new List<MessageData>();

            int startIndex = state.currentMessageIndex;
            int endIndex = GetEndIndexForNextPause();

            for (int i = startIndex; i < endIndex; i++)
            {
                var message = currentNode.messages[i];

                if (!state.readMessageIds.Contains(message.messageId))
                {
                    unread.Add(message);
                }
            }

            return unread;
        }

        /// <summary>
        /// Returns the index at which the next pause point stops message processing.
        /// If no pause point exists beyond fromIndex, returns the total message count.
        /// Defaults to state.currentMessageIndex when no fromIndex is provided.
        /// </summary>
        private int GetEndIndexForNextPause(int fromIndex = -1)
        {
            int startFrom = fromIndex >= 0 ? fromIndex : state.currentMessageIndex;
            int endIndex = currentNode.messages.Count;

            foreach (var pausePoint in currentNode.pausePoints)
            {
                if (pausePoint.stopIndex > startFrom)
                {
                    endIndex = pausePoint.stopIndex;
                    break;
                }
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
                state.currentChapterId = entry.chapterId;
                state.currentMessageIndex = 0;
                state.readMessageIds.Clear();
                BSDebug.Info($"[DialogueExecutor] No chapter ID in state, defaulting to entry point: '{state.currentChapterId}'");
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
                state.currentNodeName = firstNode;
                state.currentMessageIndex = 0;
                state.resumeTarget = ResumeTarget.None;
            }

            if (currentNodes.ContainsKey(state.currentNodeName))
            {
                var node = currentNodes[state.currentNodeName];
                if (state.currentMessageIndex < 0 || state.currentMessageIndex > node.messages.Count)
                {
                    BSDebug.Warn($"[DialogueExecutor] Invalid message index {state.currentMessageIndex}, resetting to 0");
                    state.currentMessageIndex = 0;
                    state.resumeTarget = ResumeTarget.None;
                }
            }
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