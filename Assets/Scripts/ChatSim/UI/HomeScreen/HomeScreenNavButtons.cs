// ════════════════════════════════════════════════════════════════════════
// Assets/Scripts/ChatSim/UI/HomeScreen/HomeScreenNavButtons.cs
// ════════════════════════════════════════════════════════════════════════

using UnityEngine;
using UnityEngine.UI;
using ChatSim.Core;

namespace ChatSim.UI.HomeScreen
{
    /// <summary>
    /// Handles the three phone OS navigation buttons on the Home Screen.
    /// Delegates navigation to HomeScreenController for panel stack management.
    /// Attach to: NavigationBar GameObject in HomeScreen scene
    /// </summary>
    public class HomeScreenNavButtons : MonoBehaviour
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

        [Header("Home Screen")]
        [SerializeField] private HomeScreenController homeScreenController;

        // ═══════════════════════════════════════════════════════════
        // ░ LOGGING
        // ═══════════════════════════════════════════════════════════
        private readonly DebugLogger _log = new DebugLogger(
            "HomeScreenNavButtons",
            () => GameBootstrap.Config?.homeScreenDebugLogs ?? false
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
            if (homeButton == null)
                _log.Warn("homeButton not assigned!");

            if (backButton == null)
                _log.Warn("backButton not assigned!");

            if (quitButton == null)
                _log.Warn("quitButton not assigned!");

            if (quitConfirmationPanel == null)
                _log.Warn("quitConfirmationPanel not assigned!");

            if (homeScreenController == null)
                _log.Error("homeScreenController not assigned!");
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

        /// <summary>
        /// Home always returns to the phone home screen panel.
        /// </summary>
        private void OnHomePressed()
        {
            _log.Info("Home pressed");
            homeScreenController?.GoHome();
        }

        /// <summary>
        /// Back navigates to the previous panel in the stack.
        /// Falls back to GoHome if no history exists.
        /// </summary>
        private void OnBackPressed()
        {
            _log.Info("Back pressed");
            homeScreenController?.GoBack();
        }

        /// <summary>
        /// Quit confirmation handler.
        /// </summary>
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