// ════════════════════════════════════════════════════════════════════════
// Assets/Scripts/UI/ChatApp/Core/ChatTimingController.cs
// ════════════════════════════════════════════════════════════════════════

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BubbleSpinner.Data;
using ChatSim.Core;
using ChatSim.UI.Common.Components;
using ChatSim.UI.Common.Pooling;

namespace ChatSim.UI.ChatApp.Controllers
{
    /// <summary>
    /// Controls message display timing, typing indicators, and animations.
    /// Attach to: ChatAppController GameObject
    /// </summary>
    public class ChatTimingController : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════
        // ░ INSPECTOR SETTINGS
        // ═══════════════════════════════════════════════════════════
        
        [Header("References")]
        [SerializeField] private ChatMessageSpawner messageDisplay;
        
        [Header("Typing Indicator Prefab")]
        [Tooltip("Prefab to spawn for typing indicator (will be pooled)")]
        [SerializeField] private GameObject typingIndicatorPrefab;
        
        [Header("Pooling")]
        [SerializeField] private PoolingManager poolingManager;

        [Header("Fast Mode")]
        [Tooltip("When enabled, all delays are replaced with the fast mode speed for instant display")]
        [SerializeField] private bool isFastMode = false;
        
        // ═══════════════════════════════════════════════════════════
        // ░ STATE
        // ═══════════════════════════════════════════════════════════
        
        private Queue<MessageData> messageQueue = new Queue<MessageData>();
        private bool isDisplayingMessages = false;
        private Coroutine currentMessageSequence;
        private System.Action pendingCallback = null;
        private bool isSequenceCancelled = false;

        private ChatAppController chatController;
        
        private GameObject activeTypingIndicator = null;

        // ═══════════════════════════════════════════════════════════
        // ░ LOGGING
        // ═══════════════════════════════════════════════════════════

        private readonly DebugLogger _log = new DebugLogger(
            "ChatTimingController",
            () => GameBootstrap.Config?.chatTimingControllerDebugLogs ?? false
        );

        // ═══════════════════════════════════════════════════════════
        // ░ PROPERTIES
        // ═══════════════════════════════════════════════════════════

        public bool IsDisplayingMessages => isDisplayingMessages;

        // Config accessors — fallback to hardcoded defaults if config is missing
        private float MessageDelay          => GameBootstrap.Config != null ? GameBootstrap.Config.messageDelay             : 1.2f;
        private float TypingIndicatorDuration => GameBootstrap.Config != null ? GameBootstrap.Config.typingIndicatorDuration : 1.5f;
        private float PlayerMessageDelay    => GameBootstrap.Config != null ? GameBootstrap.Config.playerMessageDelay       : 0.3f;
        private float FinalDelayBeforeChoices => GameBootstrap.Config != null ? GameBootstrap.Config.finalDelayBeforeChoices : 0.2f;
        private float FastModeSpeed         => GameBootstrap.Config != null ? GameBootstrap.Config.fastModeSpeed            : 0.1f;

        // ═══════════════════════════════════════════════════════════
        // ░ INITIALIZATION
        // ═══════════════════════════════════════════════════════════

        private void Awake()
        {
            chatController = GetComponent<ChatAppController>();

            if (chatController == null)
                _log.Error("ChatAppController not found on same GameObject!");

            if (poolingManager == null)
            {
                poolingManager = GetComponent<PoolingManager>();

                if (poolingManager == null)
                {
                    poolingManager = gameObject.AddComponent<PoolingManager>();
                    _log.Info("Created PoolingManager component");
                }
            }

            ValidateReferences();
        }

        private void ValidateReferences()
        {
            if (messageDisplay == null)
                _log.Error("messageDisplay not assigned!");

            if (typingIndicatorPrefab == null)
                _log.Warn("typingIndicatorPrefab not assigned - typing indicators disabled");
        }

        // ═══════════════════════════════════════════════════════════
        // ░ PUBLIC API
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Queue messages for timed display with typing indicators.
        /// </summary>
        public void QueueMessages(List<MessageData> messages, System.Action onComplete = null)
        {
            if (messages == null || messages.Count == 0)
            {
                _log.Warn("Empty message list");
                onComplete?.Invoke();
                return;
            }

            _log.Info($"Queueing {messages.Count} messages (Fast Mode: {isFastMode})");

            StopCurrentSequenceIfRunning();
            ResetSequenceState();

            pendingCallback = onComplete;

            messageQueue.Clear();
            foreach (var message in messages)
                messageQueue.Enqueue(message);

            currentMessageSequence = StartCoroutine(DisplayMessagesSequence());
        }

        /// <summary>
        /// Stop the current message sequence.
        /// </summary>
        public void StopCurrentSequence()
        {
            _log.Info("Stopping current sequence");

            isSequenceCancelled = true;

            var callbackToCancel = pendingCallback;
            pendingCallback = null;

            StopCurrentSequenceIfRunning();
            CleanupTypingIndicator();
            ClearMessageQueue();

            isDisplayingMessages = false;

            _log.Info($"Sequence stopped. Callback {(callbackToCancel != null ? "cancelled" : "was null")}");
        }

        /// <summary>
        /// Set fast mode on/off.
        /// </summary>
        public void SetFastMode(bool enabled)
        {
            isFastMode = enabled;
            _log.Info($"Fast mode: {(enabled ? "ENABLED" : "DISABLED")}");
        }

        // ═══════════════════════════════════════════════════════════
        // ░ MESSAGE DISPLAY SEQUENCE
        // ═══════════════════════════════════════════════════════════

        private IEnumerator DisplayMessagesSequence()
        {
            isDisplayingMessages = true;
            _log.Info("Starting message display sequence");

            while (messageQueue.Count > 0)
            {
                if (isSequenceCancelled)
                {
                    _log.Info("Sequence aborted (cancelled)");
                    break;
                }

                var message = messageQueue.Dequeue();

                if (ShouldShowTypingIndicator(message))
                {
                    yield return StartCoroutine(ShowTypingIndicatorSequence());

                    if (isSequenceCancelled)
                        break;
                }

                messageDisplay.DisplayMessage(message, instant: isFastMode);

                if (chatController != null)
                    chatController.OnNewMessageDisplayed(message);

                if (messageQueue.Count > 0)
                    yield return new WaitForSeconds(GetMessageDelay(message));
            }

            if (!isSequenceCancelled)
            {
                if (!isFastMode && FinalDelayBeforeChoices > 0)
                    yield return new WaitForSeconds(FinalDelayBeforeChoices);

                isDisplayingMessages = false;
                _log.Info("Message sequence complete");
                InvokeCallbackSafely();
            }
            else
            {
                isDisplayingMessages = false;
                _log.Info("Message sequence cancelled - callback suppressed");
            }
        }

        // ═══════════════════════════════════════════════════════════
        // ░ TYPING INDICATOR (PREFAB POOLING SYSTEM)
        // ═══════════════════════════════════════════════════════════

        private bool ShouldShowTypingIndicator(MessageData message)
        {
            if (isFastMode)
                return false;

            return typingIndicatorPrefab != null &&
                !message.IsPlayerMessage &&
                message.type == MessageData.MessageType.Text;
        }

        /// <summary>
        /// Spawn typing indicator from prefab via pooling
        /// </summary>
        private IEnumerator ShowTypingIndicatorSequence()
        {
            if (typingIndicatorPrefab == null || poolingManager == null)
                yield break;

            _log.Info("Showing typing indicator");

            Transform chatContent = messageDisplay.GetChatContent();
            activeTypingIndicator = poolingManager.Get(typingIndicatorPrefab, chatContent, activateOnGet: true);

            if (activeTypingIndicator != null)
                activeTypingIndicator.transform.SetAsLastSibling();

            yield return new WaitForSeconds(GetTypingIndicatorDuration());

            if (!isSequenceCancelled)
                CleanupTypingIndicator();
        }

        /// <summary>
        /// Recycle typing indicator back to pool
        /// </summary>
        private void CleanupTypingIndicator()
        {
            if (activeTypingIndicator == null)
                return;

            if (poolingManager != null)
            {
                poolingManager.Recycle(activeTypingIndicator);
                _log.Info("Typing indicator recycled");
            }
            else
            {
                Destroy(activeTypingIndicator);
                _log.Warn("No pooling manager - destroyed typing indicator");
            }

            activeTypingIndicator = null;
        }

        // ═══════════════════════════════════════════════════════════
        // ░ TIMING CALCULATIONS
        // ═══════════════════════════════════════════════════════════

        private float GetTypingIndicatorDuration()
        {
            return isFastMode ? FastModeSpeed : TypingIndicatorDuration;
        }

        private float GetMessageDelay(MessageData message)
        {
            if (isFastMode)
                return FastModeSpeed;

            if (message.IsPlayerMessage)
                return PlayerMessageDelay;

            return MessageDelay;
        }

        // ═══════════════════════════════════════════════════════════
        // ░ HELPER METHODS
        // ═══════════════════════════════════════════════════════════

        private void StopCurrentSequenceIfRunning()
        {
            if (currentMessageSequence != null)
            {
                StopCoroutine(currentMessageSequence);
                currentMessageSequence = null;
                _log.Info("Stopped previous sequence");
            }
        }

        private void ResetSequenceState()
        {
            isSequenceCancelled = false;
        }

        private void ClearMessageQueue()
        {
            if (messageQueue != null)
            {
                int queuedCount = messageQueue.Count;
                messageQueue.Clear();

                if (queuedCount > 0)
                    _log.Info($"Cleared {queuedCount} queued messages");
            }
        }

        private void InvokeCallbackSafely()
        {
            if (pendingCallback != null && !isSequenceCancelled)
            {
                _log.Info("Invoking completion callback");
                var callback = pendingCallback;
                pendingCallback = null;
                callback.Invoke();
            }
            else if (isSequenceCancelled)
            {
                _log.Info("Callback suppressed (cancelled)");
                pendingCallback = null;
            }
        }

        // ═══════════════════════════════════════════════════════════
        // ░ LIFECYCLE
        // ═══════════════════════════════════════════════════════════

        private void OnDestroy()
        {
            isSequenceCancelled = true;
            pendingCallback = null;
            CleanupTypingIndicator();
        }
    }
}