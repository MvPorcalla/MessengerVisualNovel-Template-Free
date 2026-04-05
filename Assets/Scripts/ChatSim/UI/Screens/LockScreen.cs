// ════════════════════════════════════════════════════════════════════════
// Assets/Scripts/UI/Screens/LockScreen.cs
// ════════════════════════════════════════════════════════════════════════

using System.Collections.Generic;
using UnityEngine;
using TMPro;
using ChatSim.Core;
using ChatSim.Data;
using BubbleSpinner.Data;

namespace ChatSim.UI.Screens
{
    /// <summary>
    /// Manages the lock screen (02_LockScreen scene).
    /// Loaded after Bootstrap — GameBootstrap.SceneFlow is guaranteed available.
    ///
    /// Unlock: swipe up anywhere on screen.
    /// Content fades out as player swipes up, fades back in if they release early.
    ///
    /// Flow: 02_LockScreen → 03_PhoneScreen
    /// </summary>
    public class LockScreen : MonoBehaviour
    {
        // ═════════════════════════════════════════════
        // UI References
        // ═════════════════════════════════════════════

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private TextMeshProUGUI dateText;
        [SerializeField] private CanvasGroup contentGroup;

        [Header("Notifications")]
        [SerializeField] private GameObject notificationContainer;
        [SerializeField] private GameObject notificationItemPrefab;
        [SerializeField] private GameObject moreIndicator;

        [Header("Swipe Arrow")]
        [SerializeField] private RectTransform swipeArrow;    // Drag your arrow image here
        [SerializeField] private float arrowAmplitude = 10f;  // Pixels to move up/down
        [SerializeField] private float arrowSpeed = 2f;       // Speed of movement

        // ═════════════════════════════════════════════
        // State
        // ═════════════════════════════════════════════

        private Vector2 _touchStartPos;
        private bool _isTouching = false;
        private bool _isUnlocking = false;

        private Vector2 arrowStartPos;

        private string _lastTimeText;
        private string _lastDateText;

        // ═════════════════════════════════════════════
        // Logging
        // ═════════════════════════════════════════════
        private readonly DebugLogger _log = new DebugLogger(
            "LockScreen",
            () => GameBootstrap.Config?.lockScreenDebugLogs ?? false
        );

        // ═════════════════════════════════════════════
        // Config Fallbacks
        // ═════════════════════════════════════════════

        private float SwipeThreshold => GameBootstrap.Config != null ? GameBootstrap.Config.swipeThreshold : 300f;
        private float FadeSwipeRange => GameBootstrap.Config != null ? GameBootstrap.Config.fadeSwipeRange : 400f;
        private int MaxNotifications => GameBootstrap.Config != null ? GameBootstrap.Config.maxIndividualNotifications : 3;
        private string TimeFormat => GameBootstrap.Config != null ? GameBootstrap.Config.GetTimeFormatString() : "HH:mm";
        private string DateFormat => GameBootstrap.Config != null ? GameBootstrap.Config.GetDateFormatString() : "dddd, MMMM dd";

        // ═════════════════════════════════════════════
        // Unity Lifecycle
        // ═════════════════════════════════════════════

        private void Awake()
        {
            if (swipeArrow != null)
                arrowStartPos = swipeArrow.anchoredPosition;
        }

        private void Start()
        {
            UpdateTimeDate();

            if (contentGroup != null)
                contentGroup.alpha = 1f;

            PopulateNotifications();

            GameEvents.TriggerPhoneLocked();
            _log.Info("Lock screen ready — swipe up to unlock");
        }

        private void Update()
        {
            UpdateTimeDate();
            
            AnimateSwipeArrow();

            if (_isUnlocking) return;

            HandleSwipeInput();
        }

        // ═════════════════════════════════════════════
        // Time & Date
        // ═════════════════════════════════════════════

        private void UpdateTimeDate()
        {
            string newTime = System.DateTime.Now.ToString(TimeFormat);
            string newDate = System.DateTime.Now.ToString(DateFormat);

            if (newTime != _lastTimeText)
            {
                _lastTimeText = newTime;
                if (timeText != null) timeText.text = newTime;
            }

            if (newDate != _lastDateText)
            {
                _lastDateText = newDate;
                if (dateText != null) dateText.text = newDate;
            }
        }

        // ═════════════════════════════════════════════
        // Notifications
        // ═════════════════════════════════════════════

        private void PopulateNotifications()
        {
            if (notificationContainer == null || notificationItemPrefab == null)
            {
                _log.Info("Notification refs not assigned — skipping");
                return;
            }

            // Clear existing
            foreach (Transform child in notificationContainer.transform)
            {
                if (child.gameObject != moreIndicator)
                    Destroy(child.gameObject);
            }

            if (moreIndicator != null)
                moreIndicator.SetActive(false);

            if (MaxNotifications == 0) return;

            if (GameBootstrap.Save == null)
            {
                _log.Warn("SaveManager not ready — skipping notifications");
                return;
            }

            SaveData saveData = GameBootstrap.Save.GetOrCreateSaveData();
            if (saveData == null) return;

            List<ConversationState> unread = GetUnreadConversations(saveData);

            if (unread.Count == 0)
            {
                _log.Info("No unread conversations");
                return;
            }

            _log.Info($"Unread conversations: {unread.Count}");

            int individualCount = Mathf.Min(unread.Count, MaxNotifications);
            int remainingCount  = unread.Count - individualCount;

            for (int i = 0; i < individualCount; i++)
                SpawnNotificationItem(unread[i]);

            if (remainingCount > 0)
                ShowMoreIndicator();
        }

        private List<ConversationState> GetUnreadConversations(SaveData saveData)
        {
            var unread = new List<ConversationState>();

            if (saveData.conversationStates == null) return unread;

            foreach (var state in saveData.conversationStates)
            {
                if (state == null) continue;
                if (state.messageHistory == null || state.messageHistory.Count == 0) continue;

                bool isPending = state.resumeTarget == ResumeTarget.Pause
                              || state.resumeTarget == ResumeTarget.Interrupted;

                if (isPending)
                    unread.Add(state);
            }

            return unread;
        }

        private void SpawnNotificationItem(ConversationState state)
        {
            GameObject item = Instantiate(notificationItemPrefab, notificationContainer.transform);

            TextMeshProUGUI senderText  = FindTMP(item, "SenderText");
            TextMeshProUGUI previewText = FindTMP(item, "PreviewText");

            if (senderText != null)
                senderText.text = state.characterName;

            if (previewText != null)
                previewText.text = GetLastNpcMessage(state);
        }

        private void ShowMoreIndicator()
        {
            if (moreIndicator != null)
            {
                moreIndicator.SetActive(true);
                moreIndicator.transform.SetAsLastSibling();
            }
        }

        private string GetLastNpcMessage(ConversationState state)
        {
            if (state.messageHistory == null) return "New message";

            for (int i = state.messageHistory.Count - 1; i >= 0; i--)
            {
                var msg = state.messageHistory[i];
                if (msg == null) continue;
                if (msg.IsSystemMessage) continue;

                if (msg.type == MessageData.MessageType.Image)
                    return msg.IsPlayerMessage ? "You sent an image." : "Sent an image.";

                if (msg.type == MessageData.MessageType.Text && !string.IsNullOrEmpty(msg.content))
                    return msg.content;
            }

            return "New message";
        }

        private TextMeshProUGUI FindTMP(GameObject root, string childName)
        {
            Transform found = root.transform.Find(childName);
            if (found != null) return found.GetComponent<TextMeshProUGUI>();

            _log.Warn($"Could not find '{childName}' in notification prefab");
            return null;
        }

        // ═════════════════════════════════════════════
        // Swipe Input
        // ═════════════════════════════════════════════

        private void HandleSwipeInput()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            if (Input.GetMouseButtonDown(0))
            {
                _touchStartPos = Input.mousePosition;
                _isTouching = true;
            }

            if (_isTouching)
            {
                float delta = ((Vector2)Input.mousePosition - _touchStartPos).y;
                UpdateFade(delta);

                if (Input.GetMouseButtonUp(0))
                {
                    _log.Info($"Swipe delta: {delta}px (threshold: {SwipeThreshold}px)");

                    if (delta >= SwipeThreshold)
                        OnUnlockTriggered();
                    else
                        ResetFade();

                    _isTouching = false;
                }
            }
#else
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);

                if (touch.phase == TouchPhase.Began)
                {
                    _touchStartPos = touch.position;
                    _isTouching = true;
                }

                if (_isTouching)
                {
                    float delta = (touch.position - _touchStartPos).y;
                    UpdateFade(delta);

                    if (touch.phase == TouchPhase.Ended)
                    {
                        _log.Info($"Swipe delta: {delta}px (threshold: {SwipeThreshold}px)");

                        if (delta >= SwipeThreshold)
                            OnUnlockTriggered();
                        else
                            ResetFade();

                        _isTouching = false;
                    }
                }
            }
#endif
        }

        // ═════════════════════════════════════════════
        // Fade
        // ═════════════════════════════════════════════

        private void UpdateFade(float swipeDelta)
        {
            if (contentGroup == null) return;

            float t = Mathf.Clamp01(swipeDelta / FadeSwipeRange);
            contentGroup.alpha = 1f - t;
        }

        private void ResetFade()
        {
            if (contentGroup == null) return;
            contentGroup.alpha = 1f;
            _log.Info("Swipe released — content restored");
        }

        // ═════════════════════════════════════════════
        // Swipe Arrow Animation
        // ═════════════════════════════════════════════

        private void AnimateSwipeArrow()
        {
            if (swipeArrow == null) return;

            float yOffset = Mathf.Sin(Time.time * arrowSpeed) * arrowAmplitude;
            swipeArrow.anchoredPosition = arrowStartPos + new Vector2(0f, yOffset);
        }

        // ═════════════════════════════════════════════
        // Unlock
        // ═════════════════════════════════════════════

        private void OnUnlockTriggered()
        {
            if (_isUnlocking) return;
            _isUnlocking = true;

            _log.Info("Unlocked via swipe");
            GameEvents.TriggerPhoneUnlocked();

            if (GameBootstrap.SceneFlow != null)
                GameBootstrap.SceneFlow.GoToPhoneScreen();
            else
                _log.Error("GameBootstrap.SceneFlow not found!");
        }
    }
}