// ════════════════════════════════════════════════════════════════════════
// Assets/Scripts/ChatSim/UI/HomeScreen/HomeScreenController.cs
// ════════════════════════════════════════════════════════════════════════

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ChatSim.Core;

namespace ChatSim.UI.HomeScreen
{
    [System.Serializable]
    public class AppButton
    {
        public bool enabled = true;
        public string appName;
        public Button button;
        public string targetScene;
        public GameObject targetPanel;
    }

    /// <summary>
    /// Manages the phone home screen panel and app launching.
    /// Panel stack navigation is delegated to from PhoneNavigationButtons.
    /// Attach to: HomeScreenController GameObject
    /// </summary>
    public class HomeScreenController : MonoBehaviour
    {
        public static HomeScreenController Instance { get; private set; }

        // ═══════════════════════════════════════════════════════════
        // ░ INSPECTOR REFERENCES
        // ═══════════════════════════════════════════════════════════

        [Header("Home Screen Panel")]
        [SerializeField] private GameObject homeScreenPanel;

        [Header("App Buttons")]
        [SerializeField] private List<AppButton> apps = new List<AppButton>();

        // ═══════════════════════════════════════════════════════════
        // ░ LOGGING
        // ═══════════════════════════════════════════════════════════

        private readonly DebugLogger _log = new DebugLogger(
            "HomeScreenController",
            () => GameBootstrap.Config?.homeScreenDebugLogs ?? false
        );

        // ═══════════════════════════════════════════════════════════
        // ░ STATE
        // ═══════════════════════════════════════════════════════════

        private GameObject currentPanel;
        private Stack<GameObject> panelHistory = new Stack<GameObject>();

        // ═══════════════════════════════════════════════════════════
        // ░ UNITY LIFECYCLE
        // ═══════════════════════════════════════════════════════════

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Start()
        {
            InitializeApps();
            ShowHomeScreen();
        }

        // ═══════════════════════════════════════════════════════════
        // ░ INITIALIZATION
        // ═══════════════════════════════════════════════════════════

        private void InitializeApps()
        {
            // Hide all app panels initially
            foreach (var app in apps)
            {
                if (app.targetPanel != null)
                    app.targetPanel.SetActive(false);
            }

            // Setup app buttons
            foreach (var app in apps)
            {
                if (!app.enabled)
                {
                    if (app.button != null)
                        app.button.gameObject.SetActive(false);
                    continue;
                }

                if (app.button == null)
                {
                    _log.Warn($"Button not assigned for app: {app.appName}");
                    continue;
                }

                string scene = app.targetScene;
                GameObject panel = app.targetPanel;
                string name = app.appName;

                app.button.onClick.AddListener(() =>
                {
                    GameEvents.TriggerAppOpened(name);

                    if (!string.IsNullOrEmpty(scene))
                    {
                        GameBootstrap.SceneFlow.LoadScene(scene);
                    }
                    else if (panel != null)
                    {
                        OpenPanel(panel);
                    }
                });
            }

            _log.Info($"Initialized {apps.Count} app buttons");
        }

        private void ShowHomeScreen()
        {
            if (homeScreenPanel != null)
            {
                homeScreenPanel.SetActive(true);
                currentPanel = homeScreenPanel;
            }
            else
            {
                _log.Warn("homeScreenPanel not assigned!");
            }
        }

        // ═══════════════════════════════════════════════════════════
        // ░ PANEL NAVIGATION
        // ═══════════════════════════════════════════════════════════

        private void OpenPanel(GameObject panel)
        {
            if (currentPanel != null && currentPanel != homeScreenPanel)
            {
                panelHistory.Push(currentPanel);
                currentPanel.SetActive(false);
            }
            else if (currentPanel == homeScreenPanel)
            {
                homeScreenPanel.SetActive(false);
            }

            panel.SetActive(true);
            currentPanel = panel;

            _log.Info($"Opened panel: {panel.name}");
        }

        /// <summary>
        /// Returns to the home screen panel and clears panel history.
        /// Called by PhoneNavigationButtons when home is pressed.
        /// </summary>
        public void GoHome()
        {
            if (currentPanel != null && currentPanel != homeScreenPanel)
                currentPanel.SetActive(false);

            CloseAllPanels();

            if (homeScreenPanel != null)
            {
                homeScreenPanel.SetActive(true);
                currentPanel = homeScreenPanel;
            }

            _log.Info("Returned to home screen");
        }

        /// <summary>
        /// Navigates to the previous panel in the stack.
        /// Falls back to GoHome if no history exists.
        /// Called by PhoneNavigationButtons when back is pressed.
        /// </summary>
        public void GoBack()
        {
            if (panelHistory.Count == 0)
            {
                GoHome();
                return;
            }

            if (currentPanel != null)
                currentPanel.SetActive(false);

            currentPanel = panelHistory.Pop();
            currentPanel.SetActive(true);

            _log.Info($"Back to panel: {currentPanel.name}");
        }

        // ═══════════════════════════════════════════════════════════
        // ░ HELPERS
        // ═══════════════════════════════════════════════════════════

        private void CloseAllPanels()
        {
            while (panelHistory.Count > 0)
            {
                var panel = panelHistory.Pop();
                if (panel != null)
                    panel.SetActive(false);
            }
        }
    }
}