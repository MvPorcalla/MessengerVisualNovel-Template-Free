// ════════════════════════════════════════════════════════════════════════
// Assets/Scripts/UI/ChatApp/Core/ChatAutoScroller.cs
// ════════════════════════════════════════════════════════════════════════

using System;
using UnityEngine;
using UnityEngine.UI;
using ChatSim.Core;

namespace ChatSim.UI.ChatApp.Controllers
{
    /// <summary>
    /// Event-driven auto-scroll that monitors Content height changes.
    /// Attach to: ChatAppController GameObject
    /// </summary>
    public class ChatAutoScroller : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════
        // ░ INSPECTOR SETTINGS
        // ═══════════════════════════════════════════════════════════
        
        [Header("References")]
        [SerializeField] private ScrollRect chatScrollRect;
        
        [Header("Auto-Scroll Settings")]
        [Tooltip("Enable or disable auto-scrolling behavior. When disabled, the scroll position will not automatically adjust when new messages are added.")]
        [SerializeField] private bool autoScrollEnabled = true;
        
        // ═══════════════════════════════════════════════════════════
        // ░ STATE
        // ═══════════════════════════════════════════════════════════
        
        private RectTransform contentTransform;
        private float lastContentHeight;
        private int lastChildCount;
        private bool wasAtBottom;
        private bool isInitialized;

        // ═══════════════════════════════════════════════════════════
        // ░ EVENTS
        // ═══════════════════════════════════════════════════════════

        public event Action OnScrollReachedBottom;

        // ═══════════════════════════════════════════════════════════
        // ░ LOGGING
        // ═══════════════════════════════════════════════════════════

        private readonly DebugLogger _log = new DebugLogger(
            "ChatAutoScroller",
            () => GameBootstrap.Config?.chatAutoScrollerDebugLogs ?? false
        );

        // ═══════════════════════════════════════════════════════════
        // ░ PROPERTIES
        // ═══════════════════════════════════════════════════════════

        public float CurrentScrollPosition => chatScrollRect?.verticalNormalizedPosition ?? -1f;
        public bool IsInitialized => isInitialized;
        private float BottomThreshold => GameBootstrap.Config != null ? GameBootstrap.Config.bottomThreshold : 0.01f;

        // ═══════════════════════════════════════════════════════════
        // ░ UNITY LIFECYCLE
        // ═══════════════════════════════════════════════════════════

        private void LateUpdate()
        {
            if (!isInitialized && !TryInitialize())
                return;

            if (!chatScrollRect.gameObject.activeInHierarchy || !autoScrollEnabled)
                return;

            if (contentTransform == null)
            {
                _log.Warn("contentTransform is null - reinitializing");
                isInitialized = false;
                return;
            }

            bool currentlyAtBottom = IsAtBottom();
            float currentHeight = contentTransform.rect.height;
            int currentChildCount = contentTransform.childCount;

            bool heightChanged = !Mathf.Approximately(currentHeight, lastContentHeight);
            bool childCountChanged = currentChildCount != lastChildCount;

            if ((heightChanged || childCountChanged) && wasAtBottom)
            {
                ScrollToBottom();
                currentlyAtBottom = true;
            }

            if (!wasAtBottom && currentlyAtBottom)
            {
                _log.Info("User scrolled to bottom");
                OnScrollReachedBottom?.Invoke();
            }

            lastContentHeight = currentHeight;
            lastChildCount = currentChildCount;
            wasAtBottom = currentlyAtBottom;
        }

        private void OnEnable()
        {
            isInitialized = false;
            wasAtBottom = true;
        }

        private void OnDisable()
        {
            wasAtBottom = true;
            isInitialized = false;
        }

        // ═══════════════════════════════════════════════════════════
        // ░ INITIALIZATION
        // ═══════════════════════════════════════════════════════════

        private bool TryInitialize()
        {
            if (chatScrollRect == null)
            {
                chatScrollRect = GetComponentInChildren<ScrollRect>(true);
                if (chatScrollRect == null)
                {
                    _log.Error("No ScrollRect found!");
                    return false;
                }
            }

            contentTransform = chatScrollRect.content;
            if (contentTransform == null)
            {
                _log.Error("ScrollRect has no Content!");
                return false;
            }

            lastContentHeight = contentTransform.rect.height;
            lastChildCount = contentTransform.childCount;
            wasAtBottom = true;
            isInitialized = true;

            _log.Info($"Initialized. Content: {contentTransform.name}");
            return true;
        }

        // ═══════════════════════════════════════════════════════════
        // ░ PUBLIC API
        // ═══════════════════════════════════════════════════════════

        public bool IsAtBottom()
        {
            if (!isInitialized || chatScrollRect == null)
                return true; // treat uninitialized as at bottom

            // If content is smaller than viewport, always at bottom
            if (contentTransform != null)
            {
                float contentHeight = contentTransform.rect.height;
                float viewportHeight = chatScrollRect.viewport != null
                    ? chatScrollRect.viewport.rect.height
                    : chatScrollRect.GetComponent<RectTransform>().rect.height;

                if (contentHeight <= viewportHeight)
                    return true;
            }

            return chatScrollRect.verticalNormalizedPosition <= BottomThreshold;
        }

        public void ScrollToBottom()
        {
            if (!isInitialized || chatScrollRect == null)
                return;

            if (contentTransform == null)
            {
                _log.Warn("Cannot scroll - contentTransform is null");
                return;
            }

            Canvas.ForceUpdateCanvases();
            chatScrollRect.verticalNormalizedPosition = 0f;
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentTransform);
        }

        public void ForceScrollToBottom()
        {
            if (!isInitialized && !TryInitialize())
            {
                _log.Warn("Cannot force scroll - initialization failed");
                return;
            }

            wasAtBottom = true;
            lastContentHeight = contentTransform.rect.height;
            lastChildCount = contentTransform.childCount;

            ScrollToBottom();

            _log.Info("Forced scroll to bottom and reset tracking");
        }

        public void SetAutoScrollEnabled(bool enabled)
        {
            autoScrollEnabled = enabled;
            _log.Info($"Auto-scroll {(enabled ? "enabled" : "disabled")}");
        }

        public void RefreshReferences()
        {
            isInitialized = false;

            if (TryInitialize())
            {
                ForceScrollToBottom();
                _log.Info("References refreshed");
            }
        }
    }
}