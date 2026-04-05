// ════════════════════════════════════════════════════════════════════════
// Assets/Scripts/ChatSim/UI/ChatApp/Controllers/ChatAppController.cs
// ════════════════════════════════════════════════════════════════════════

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using BubbleSpinner.Core;
using BubbleSpinner.Data;
using ChatSim.Core;

namespace ChatSim.UI.ChatApp.Controllers
{
    /// <summary>
    /// Main controller for Chat App UI. Attach to: ChatAppController GameObject.
    ///
    /// Handles LIVE UI communication with BubbleSpinner.
    /// Subscribes directly to DialogueExecutor events for real-time dialogue flow.
    ///
    /// Does NOT handle save/load/reset — those go through BubbleSpinnerBridge
    /// because they need to survive between sessions and be decoupled from UI.
    /// </summary>
    public class ChatAppController : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════
        // ░ INSPECTOR REFERENCES - PANELS
        // ═══════════════════════════════════════════════════════════
        
        [Header("Panels")]
        [SerializeField] private GameObject contactListPanel;
        [SerializeField] private GameObject chatAppPanel;
        
        // ═══════════════════════════════════════════════════════════
        // ░ INSPECTOR REFERENCES - CHAT HEADER
        // ═══════════════════════════════════════════════════════════
        
        [Header("Chat Header")]
        [SerializeField] private Button chatBackButton;
        [SerializeField] private Image chatProfileIMG;
        [SerializeField] private TextMeshProUGUI chatProfileName;

        // ═══════════════════════════════════════════════════════════
        // ░ INSPECTOR REFERENCES - CHAT MODE TOGGLE
        // ═══════════════════════════════════════════════════════════
        
        [Header("Chat Mode Toggle")]
        [SerializeField] private Button chatModeButton;
        [SerializeField] private Image chatModeIcon;
        [SerializeField] private Sprite fastModeSprite;
        [SerializeField] private Sprite normalModeSprite;
        
        // ═══════════════════════════════════════════════════════════
        // ░ INSPECTOR REFERENCES - CHAT DISPLAY
        // ═══════════════════════════════════════════════════════════
        
        [Header("Chat Display")]
        [SerializeField] private ChatMessageSpawner messageDisplay;
        [SerializeField] private ChatChoiceSpawner choiceDisplay;
        
        // ═══════════════════════════════════════════════════════════
        // ░ INSPECTOR REFERENCES - TIMING CONTROLLER
        // ═══════════════════════════════════════════════════════════
        
        [Header("Timing Controller")]
        [SerializeField] private ChatTimingController timingController;
        
        // ═══════════════════════════════════════════════════════════
        // ░ INSPECTOR REFERENCES - AUTO SCROLL
        // ═══════════════════════════════════════════════════════════
        
        [Header("Auto Scroll")]
        [SerializeField] private ChatAutoScroller autoScroll;
        
        // ═══════════════════════════════════════════════════════════
        // ░ INSPECTOR REFERENCES - NEW MESSAGE INDICATOR
        // ═══════════════════════════════════════════════════════════
        
        [Header("New Message Indicator")]
        [SerializeField] private GameObject newMessageIndicator;
        [SerializeField] private Button newMessageButton;
        [SerializeField] private TextMeshProUGUI newMessageText;
        
        // ═══════════════════════════════════════════════════════════
        // ░ STATE
        // ═══════════════════════════════════════════════════════════
        
        private DialogueExecutor currentExecutor;
        private ConversationAsset currentConversation;
        private bool isFastMode = false;
        private int unreadMessageCount = 0;

        private AsyncOperationHandle<Sprite> chatProfileImageHandle;

        // ═══════════════════════════════════════════════════════════
        // ░ LOGGING
        // ═══════════════════════════════════════════════════════════

        private readonly DebugLogger _log = new DebugLogger(
            "ChatAppController",
            () => GameBootstrap.Config?.chatAppDebugLogs ?? false
        );

        // ═══════════════════════════════════════════════════════════
        // ░ PROPERTIES
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Returns true if the chat panel is currently active.
        /// Used by NavigationButtonsController for context-sensitive back navigation.
        /// </summary>
        public bool IsChatActive => chatAppPanel != null && chatAppPanel.activeSelf;
        
        // ═══════════════════════════════════════════════════════════
        // ░ UNITY LIFECYCLE
        // ═══════════════════════════════════════════════════════════
        
        private void Awake()
        {
            ValidateReferences();
            LoadFastModePreference();
            SetupEventListeners();
            InitializePanelStates();
        }
        
        private void OnEnable()
        {
            SubscribeToScrollEvents();
        }
        
        private void OnDisable()
        {
            UnsubscribeFromScrollEvents();
            UnsubscribeFromExecutorEvents();
        }
        
        // ═══════════════════════════════════════════════════════════
        // ░ INITIALIZATION
        // ═══════════════════════════════════════════════════════════
        
        private void ValidateReferences()
        {
            if (chatBackButton == null)
                _log.Error("chatBackButton not assigned!");

            if (messageDisplay == null)
                _log.Error("messageDisplay not assigned!");

            if (choiceDisplay == null)
                _log.Error("choiceDisplay not assigned!");

            if (timingController == null)
                _log.Error("timingController not assigned!");

            if (autoScroll == null)
                _log.Warn("autoScroll not assigned - auto-scroll disabled");
        }
        
        /// <summary>
        /// Initialize panel states (merged from ChatAppUIManager)
        /// </summary>
        private void InitializePanelStates()
        {
            if (contactListPanel == null || chatAppPanel == null)
            {
                _log.Error("Panel references are missing!");
                return;
            }

            contactListPanel.SetActive(true);
            chatAppPanel.SetActive(false);
        }
        
        private void SetupEventListeners()
        {
            // ─────────────────────────────────────────────────────────
            // CHAT-SPECIFIC BUTTONS
            // ─────────────────────────────────────────────────────────
            
            chatBackButton?.onClick.AddListener(OnChatBackButtonClicked);
            chatModeButton?.onClick.AddListener(OnModeButtonClicked);
            newMessageButton?.onClick.AddListener(OnNewMessageIndicatorClicked);
            
            // Set initial mode icon
            UpdateModeIcon();
            
            // Hide new message indicator initially
            if (newMessageIndicator != null)
            {
                newMessageIndicator.SetActive(false);
            }
        }
        
        private void SubscribeToScrollEvents()
        {
            if (autoScroll != null)
            {
                autoScroll.OnScrollReachedBottom += OnScrollReachedBottom;
            }
        }
        
        private void UnsubscribeFromScrollEvents()
        {
            if (autoScroll != null)
            {
                autoScroll.OnScrollReachedBottom -= OnScrollReachedBottom;
            }
        }

        private void LoadFastModePreference()
        {
            isFastMode = PlayerPrefs.GetInt(PlayerPrefKeys.FastMode, PlayerPrefKeys.DefaultFastMode) == 1;

            if (timingController != null)
                timingController.SetFastMode(isFastMode);
        }
        
        // ═══════════════════════════════════════════════════════════
        // ░ PUBLIC API - START CONVERSATION
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>
        /// Called by CharacterButton when a conversation is selected
        /// </summary>
        public void StartConversation(ConversationAsset conversationAsset)
        {
            if (conversationAsset == null)
            {
                _log.Error("Cannot start null conversation!");
                return;
            }

            StartCoroutine(StartConversationSequence(conversationAsset));
        }

        private IEnumerator StartConversationSequence(ConversationAsset conversationAsset)
        {
            _log.Info($"Starting conversation: {conversationAsset.characterName}");
            
            if (currentExecutor != null)
            {
                _log.Warn("StartConversation called while executor active — cleaning up previous");
                PerformConversationCleanup();
            }

            // STEP 1: Switch to chat panel FIRST
            SwitchToChatPanel();

            // STEP 2: Wait for panel to activate
            yield return null;
            Canvas.ForceUpdateCanvases();

            // STEP 3: Store reference
            currentConversation = conversationAsset;

            // STEP 4: Setup UI
            SetupChatHeader(conversationAsset);
            ClearChatDisplay();
            HideNewMessageIndicator();

            // STEP 5: Start conversation via GameBootstrap
            currentExecutor = GameBootstrap.Conversation.StartConversation(conversationAsset);

            if (currentExecutor == null)
            {
                _log.Error("Failed to start conversation!");
                yield break;
            }

            // STEP 6: Subscribe to executor events
            SubscribeToExecutorEvents();

            // STEP 7: Load conversation history (if resuming)
            LoadConversationHistory();

            // STEP 8: Start dialogue flow
            currentExecutor.ContinueFromCurrentState();
        }
        
        /// <summary>
        /// Called by NavigationButtonsController when OS navigation requires exiting chat.
        /// Cleans up conversation and returns to contact list.
        /// </summary>
        public void ExitToContactList()
        {
            PerformConversationCleanup();
            SwitchToContactList();
        }

        /// <summary>
        /// Called when a scene transition is about to happen (Home button, Quit).
        /// Performs cleanup without switching panels — scene change makes panel state irrelevant.
        /// </summary>
        public void ExitForSceneTransition()
        {
            PerformConversationCleanup();
            // intentionally does NOT call SwitchToContactList()
        }
        
        // ═══════════════════════════════════════════════════════════
        // ░ UI SETUP
        // ═══════════════════════════════════════════════════════════
        
        private void SetupChatHeader(ConversationAsset asset)
        {
            if (chatProfileName != null)
            {
                chatProfileName.text = asset.characterName;
            }

            if (chatProfileIMG != null && asset.profileImage != null && asset.profileImage.RuntimeKeyIsValid())
            {
                LoadChatProfileImage(asset.profileImage);
            }
            else
            {
                _log.Warn($"No valid profile image for {asset.characterName}");
            }
        }

        private void LoadChatProfileImage(AssetReference assetRef)
        {
            // Release previous handle if exists
            if (chatProfileImageHandle.IsValid())
            {
                Addressables.Release(chatProfileImageHandle);
            }
            
            // Check if already loaded by another component
            if (assetRef.OperationHandle.IsValid() && assetRef.OperationHandle.IsDone)
            {
                // Reuse the already-loaded sprite (don't create new handle)
                var sprite = assetRef.OperationHandle.Convert<Sprite>().Result;
                if (sprite != null && chatProfileIMG != null)
                {
                    chatProfileIMG.sprite = sprite;
                    _log.Info($"✓ Using cached profile image: {currentConversation?.characterName}");
                }
                return;
            }
            
            // Load new asset
            chatProfileImageHandle = assetRef.LoadAssetAsync<Sprite>();
            chatProfileImageHandle.Completed += OnChatProfileImageLoaded;
        }

        private void OnChatProfileImageLoaded(AsyncOperationHandle<Sprite> handle)
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                if (chatProfileIMG != null)
                {
                    chatProfileIMG.sprite = handle.Result;
                    _log.Info($"✓ Chat profile image loaded: {currentConversation?.characterName}");
                }
            }
            else
            {
                _log.Error($"✗ Failed to load chat profile image: {currentConversation?.characterName}");
            }
        }

        private void OnDestroy()
        {
            // Release Addressables handle
            if (chatProfileImageHandle.IsValid())
            {
                Addressables.Release(chatProfileImageHandle);
            }
        }
        
        private void ClearChatDisplay()
        {
            messageDisplay.ClearAllMessages();
            choiceDisplay.ClearChoices();
        }
        
        private void SwitchToChatPanel()
        {
            contactListPanel.SetActive(false);
            chatAppPanel.SetActive(true);
        }
        
        private void SwitchToContactList()
        {
            chatAppPanel.SetActive(false);
            contactListPanel.SetActive(true);
        }
        
        // ═══════════════════════════════════════════════════════════
        // ░ CONVERSATION HISTORY
        // ═══════════════════════════════════════════════════════════
        
        private void LoadConversationHistory()
        {
            if (currentExecutor == null) return;
            
            var state = currentExecutor.GetState();
            
            if (state?.messageHistory != null && state.messageHistory.Count > 0)
            {
                _log.Info($"Loading {state.messageHistory.Count} historical messages");
                
                // Display all historical messages instantly (no animation)
                foreach (var msg in state.messageHistory)
                {
                    messageDisplay.DisplayMessage(msg, instant: true);
                }
                
                // Scroll to bottom after history is loaded
                StartCoroutine(ScrollToBottomDelayed());
            }
        }
        
        // ═══════════════════════════════════════════════════════════
        // ░ EXECUTOR EVENT SUBSCRIPTIONS
        // ═══════════════════════════════════════════════════════════
        
        private void SubscribeToExecutorEvents()
        {
            if (currentExecutor == null) return;
            
            currentExecutor.OnMessagesReady += HandleMessagesReady;
            currentExecutor.OnChoicesReady += HandleChoicesReady;
            currentExecutor.OnPauseReached += HandlePauseReached;
            currentExecutor.OnConversationEnd += HandleConversationEnd;
            currentExecutor.OnChapterChange += HandleChapterChange;
            
            _log.Info("Subscribed to executor events");
        }
        
        private void UnsubscribeFromExecutorEvents()
        {
            if (currentExecutor == null) return;
            
            currentExecutor.OnMessagesReady -= HandleMessagesReady;
            currentExecutor.OnChoicesReady -= HandleChoicesReady;
            currentExecutor.OnPauseReached -= HandlePauseReached;
            currentExecutor.OnConversationEnd -= HandleConversationEnd;
            currentExecutor.OnChapterChange -= HandleChapterChange;
            
            _log.Info("Unsubscribed from executor events");
        }
        
        // ═══════════════════════════════════════════════════════════
        // ░ EXECUTOR EVENT HANDLERS
        // ═══════════════════════════════════════════════════════════
        
        private void HandleMessagesReady(List<MessageData> messages)
        {
            _log.Info($"Handling {messages.Count} new messages");
            
            if (messages.Count == 0)
            {
                currentExecutor.OnMessagesDisplayComplete();
                return;
            }
            
            // Queue messages for timed display
            timingController.QueueMessages(messages, OnMessagesDisplayComplete);
        }
        
        private void HandleChoicesReady(List<ChoiceData> choices)
        {
            _log.Info($"Showing {choices.Count} choices");
            
            choiceDisplay.DisplayChoices(choices, OnChoiceSelected);
        }
        
        private void HandlePauseReached()
        {
            _log.Info("Pause reached - showing continue button");
            
            choiceDisplay.ShowContinueButton(OnContinueButtonClicked);
        }
        
        /// <summary>
        /// Proper end button handling with chapter detection
        /// </summary>
        private void HandleConversationEnd()
        {
            _log.Info("Conversation ended");

            GameBootstrap.Conversation.SaveCurrentConversation();
            choiceDisplay.ClearChoices();

            if (currentExecutor.HasMoreChapters)
            {
                _log.Info("More chapters available - showing continue to next chapter button");
                choiceDisplay.ShowEndButton("Continue to Next Chapter", OnContinueToNextChapterClicked);
            }
            else
            {
                _log.Info("No more chapters - showing return button");
                choiceDisplay.ShowEndButton("Return to Contacts", OnReturnToContactsClicked);
            }
        }
        
        private void HandleChapterChange(string chapterName)
        {
            _log.Info($"Chapter changed: {chapterName}");
            
            // Optional: Show chapter transition UI
            // messageDisplay.DisplaySystemMessage($"--- {chapterName} ---");
        }
        
        // ═══════════════════════════════════════════════════════════
        // ░ CHAPTER NAVIGATION
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Handle "Continue to Next Chapter" button click - advances to next chapter in current conversation
        /// </summary>
        private void OnContinueToNextChapterClicked()
        {
            _log.Info("Continue to next chapter clicked");

            if (currentExecutor == null)
            {
                _log.Error("Cannot advance chapter - no active executor");
                return;
            }

            choiceDisplay.ClearChoices();
            currentExecutor.AdvanceToNextChapter();
        }
        
        /// <summary>
        /// Handle "Return to Contacts" button click
        /// </summary>
        private void OnReturnToContactsClicked()
        {
            _log.Info("Return to contacts clicked");
            
            // Clear choices
            choiceDisplay.ClearChoices();
            
            // Save and exit conversation (use chat back button logic)
            OnChatBackButtonClicked();
        }
        
        // ═══════════════════════════════════════════════════════════
        // ░ TIMING CONTROLLER INTERFACE
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>
        /// Called by ChatTimingController (same GameObject) when the full message sequence finishes.
        /// Notifies the executor to determine the next dialogue action.
        /// </summary>
        private void OnMessagesDisplayComplete()
        {
            _log.Info("Messages display complete");
            currentExecutor?.OnMessagesDisplayComplete();
        }
        
        /// <summary>
        /// Called by ChatTimingController (same GameObject) after each message bubble is spawned.
        /// Shows the new message indicator if the user has scrolled up.
        /// </summary>
        public void OnNewMessageDisplayed(MessageData message)
        {
            if (autoScroll != null && !autoScroll.IsAtBottom())
            {
                unreadMessageCount++;
                ShowNewMessageIndicator();
            }
        }
        
        // ═══════════════════════════════════════════════════════════
        // ░ CHOICE HANDLING
        // ═══════════════════════════════════════════════════════════
        
        private void OnChoiceSelected(ChoiceData choice)
        {
            _log.Info($"Choice selected: {choice.choiceText}");
            
            // Clear choices
            choiceDisplay.ClearChoices();
            
            // Notify executor
            currentExecutor.OnChoiceSelected(choice);
        }
        
        private void OnContinueButtonClicked()
        {
            _log.Info("Continue button clicked");
            
            // Clear continue button
            choiceDisplay.ClearChoices();
            
            // Notify executor
            currentExecutor.OnPauseButtonClicked();
        }
        
        // ═══════════════════════════════════════════════════════════
        // ░ UI INTERACTIONS - CHAT BACK BUTTON (INTERNAL)
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>
        /// Internal chat back button (in chat header) - returns to contact list
        /// </summary>
        private void OnChatBackButtonClicked()
        {
            _log.Info("Chat back button clicked → Contact List");
            
            // Cleanup conversation
            PerformConversationCleanup();
            
            // Return to contact list
            SwitchToContactList();
        }
        
        // ═══════════════════════════════════════════════════════════
        // ░ SHARED CLEANUP LOGIC
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>
        /// Core cleanup logic used by all exit methods
        /// </summary>
        private void PerformConversationCleanup()
        {
            _log.Info("Performing conversation cleanup");
            
            // STEP 1: Check if we're interrupting an active message sequence
            bool wasInterrupted = timingController != null && timingController.IsDisplayingMessages;
            
            // STEP 2: Stop timing controller FIRST (prevents coroutine errors)
            if (timingController != null)
            {
                timingController.StopCurrentSequence();
            }
            
            // STEP 3: Notify executor if we interrupted mid-sequence (it will handle pause state)
            if (wasInterrupted && currentExecutor != null)
            {
                currentExecutor.NotifyInterrupted();
            }
            
            // STEP 4: Force save BEFORE nulling currentExecutor or unsubscribing.
            // ORDER CRITICAL: ForceSaveCurrentConversation() relies on ConversationManager
            // having a valid currentConversationId, which is cleared when the executor
            // is nulled. Moving this step after Step 5 or 6 will cause a silent no-op save.
            if (currentExecutor != null)
            {
                GameBootstrap.Conversation.ForceSaveCurrentConversation();
            }
            
            // STEP 5: Unsubscribe from events AFTER save (executor must still be valid above)
            UnsubscribeFromExecutorEvents();
            
            // STEP 6: Clear references only after save and unsubscribe are complete
            currentExecutor = null;
            currentConversation = null;
            
            // STEP 7: Clear UI elements
            ClearChatDisplay();
            HideNewMessageIndicator();
            
            _log.Info("Cleanup complete");
        }
        
        // ═══════════════════════════════════════════════════════════
        // ░ MODE TOGGLE
        // ═══════════════════════════════════════════════════════════
        
        private void OnModeButtonClicked()
        {
            // Toggle mode
            isFastMode = !isFastMode;
            
            // Save preference
            PlayerPrefs.SetInt(PlayerPrefKeys.FastMode, isFastMode ? 1 : 0);
            PlayerPrefs.Save();
            
            // Update timing controller
            if (timingController != null)
            {
                timingController.SetFastMode(isFastMode);
            }
            
            // Update icon
            UpdateModeIcon();
            
            _log.Info($"Fast mode: {isFastMode}");
        }

        private void UpdateModeIcon()
        {
            if (chatModeIcon != null && fastModeSprite != null && normalModeSprite != null)
            {
                chatModeIcon.sprite = isFastMode ? fastModeSprite : normalModeSprite;
            }
        }
        
        // ═══════════════════════════════════════════════════════════
        // ░ NEW MESSAGE INDICATOR
        // ═══════════════════════════════════════════════════════════
        
        private void ShowNewMessageIndicator()
        {
            if (newMessageIndicator != null)
            {
                newMessageIndicator.SetActive(true);
                
                if (newMessageText != null)
                {
                    newMessageText.text = unreadMessageCount == 1 
                        ? "1 new message" 
                        : $"{unreadMessageCount} new messages";
                }
            }
        }
        
        private void HideNewMessageIndicator()
        {
            if (newMessageIndicator != null)
            {
                newMessageIndicator.SetActive(false);
            }
            
            unreadMessageCount = 0;
        }
        
        private void OnNewMessageIndicatorClicked()
        {
            _log.Info("New message indicator clicked");
            
            // Scroll to bottom
            if (autoScroll != null)
            {
                autoScroll.ScrollToBottom();
            }
            
            HideNewMessageIndicator();
        }
        
        private void OnScrollReachedBottom()
        {
            HideNewMessageIndicator();
        }
        
        // ═══════════════════════════════════════════════════════════
        // ░ SCROLLING
        // ═══════════════════════════════════════════════════════════
        
        private void ForceScrollToBottom()
        {
            if (autoScroll != null)
            {
                autoScroll.ForceScrollToBottom();
            }
        }
        
        /// <summary
        /// Delays scroll to bottom by 2 frames to ensure all UI updates are processed.
        /// </summary>
        private IEnumerator ScrollToBottomDelayed()
        {
            yield return new WaitUntil(() => autoScroll != null && autoScroll.IsInitialized);
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            ForceScrollToBottom();
        }
    }
}