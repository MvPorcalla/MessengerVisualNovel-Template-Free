// ════════════════════════════════════════════════════════════════════════
// Assets/Scripts/ChatSim/UI/HomeScreen/ChatAppNavButtons.cs
// ════════════════════════════════════════════════════════════════════════

using UnityEngine;
using UnityEngine.UI;
using ChatSim.Core;
using ChatSim.UI.ChatApp.Controllers;

namespace ChatSim.UI.HomeScreen
{
    /// <summary>
    /// Handles the three phone OS navigation buttons: Home, Back, and Quit.
    /// Attach to: NavigationBar GameObject
    /// </summary>
    public class ChatAppNavButtons : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════
        // ░ INSPECTOR REFERENCES
        // ═══════════════════════════════════════════════════════════

        [Header("Navigation Buttons")]
        [SerializeField] private Button homeButton;
        [SerializeField] private Button backButton;
        [SerializeField] private Button quitButton;

        [Header("Quit Confirmation")]
        [SerializeField] private GameObject quitConfirmationPanel;
        [SerializeField] private Button yesQuitButton;
        [SerializeField] private Button noQuitButton;

        [Header("Chat App")]
        [SerializeField] private ChatAppController chatAppController;

        // ═══════════════════════════════════════════════════════════
        // ░ LOGGING
        // ═══════════════════════════════════════════════════════════
        
        private readonly DebugLogger _log = new DebugLogger(
            "ChatAppNavButtons",
            () => GameBootstrap.Config?.chatAppNavButtonsDebugLogs ?? false
        );

        // ═══════════════════════════════════════════════════════════
        // ░ UNITY LIFECYCLE
        // ═══════════════════════════════════════════════════════════

        private void Awake()
        {
            ValidateReferences();
            SetupEventListeners();
            InitializeState();
        }

        // ═══════════════════════════════════════════════════════════
        // ░ INITIALIZATION
        // ═══════════════════════════════════════════════════════════

        private void ValidateReferences()
        {
            if (homeButton == null) _log.Warn("homeButton not assigned!");
            if (backButton == null) _log.Warn("backButton not assigned!");
            if (quitButton == null) _log.Warn("quitButton not assigned!");
            if (quitConfirmationPanel == null) _log.Warn("quitConfirmationPanel not assigned!");
            if (chatAppController == null) _log.Error("chatAppController not assigned!");
        }

        private void SetupEventListeners()
        {
            homeButton?.onClick.AddListener(OnHomePressed);
            backButton?.onClick.AddListener(OnBackPressed);
            quitButton?.onClick.AddListener(() => quitConfirmationPanel?.SetActive(true));
            yesQuitButton?.onClick.AddListener(OnConfirmQuit);
            noQuitButton?.onClick.AddListener(() => quitConfirmationPanel?.SetActive(false));
        }

        private void InitializeState()
        {
            if (quitConfirmationPanel != null)
                quitConfirmationPanel.SetActive(false);
        }

        // ═══════════════════════════════════════════════════════════
        // ░ BUTTON HANDLERS
        // ═══════════════════════════════════════════════════════════

        private void OnHomePressed()
        {
            _log.Info("Home pressed");

            if (chatAppController != null && chatAppController.IsChatActive)
                chatAppController.ExitForSceneTransition();

            GameBootstrap.SceneFlow.GoToPhoneScreen();
        }

        private void OnBackPressed()
        {
            _log.Info("Back pressed");

            if (chatAppController != null && chatAppController.IsChatActive)
            {
                _log.Info("Back: ChatApp → ContactList");
                chatAppController.ExitToContactList();
            }
            else
            {
                _log.Info("Back: ContactList → PhoneScreen");
                GameBootstrap.SceneFlow.GoToPhoneScreen();
            }
        }

        private void OnConfirmQuit()
        {
            _log.Info("Quitting game...");

            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
    }
}