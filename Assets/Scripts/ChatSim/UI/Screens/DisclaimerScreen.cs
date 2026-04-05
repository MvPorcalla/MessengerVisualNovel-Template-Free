// ════════════════════════════════════════════════════════════════════════
// Assets/Scripts/UI/Screens/DisclaimerScreen.cs
// ════════════════════════════════════════════════════════════════════════

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using ChatSim.Core;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ChatSim.UI.Screens
{
    /// <summary>
    /// This scene is OPTIONAL — loads before Bootstrap.
    /// - No game systems are active yet
    /// - No managers initialized
    /// - Completely isolated
    ///
    /// Flow: 00_Disclaimer → 01_Bootstrap
    ///
    /// To not include this scene, simply set Bootstrap as the first scene in Build Settings.
    /// This is not connected to bootstrap in any way, so it can be added or removed at any time without affecting the rest of the game.
    /// </summary>
    public class DisclaimerScreen : MonoBehaviour
    {
        #region Serialized Fields

        [Header("UI References")]
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button exitButton;

        [Header("Debug Settings")]
        [SerializeField] private bool skipForTesting = false;
        [SerializeField] private bool enableDebugLogs = true;

        #endregion

        // TODO: Multi-panel support (Disclaimer ↔ TOS switching)
        // [Header("Panels")]
        // [SerializeField] private GameObject disclaimerPanel;
        // [SerializeField] private GameObject tosPanel;
        // [SerializeField] private Button tosButton;
        // [SerializeField] private Button tosBackButton;
        //
        // private void OpenTOS()
        // {
        //     tosPanel.SetActive(true);
        //     disclaimerPanel.SetActive(false);
        // }
        //
        // private void BackToDisclaimer()
        // {
        //     tosPanel.SetActive(false);
        //     disclaimerPanel.SetActive(true);
        // }

        #region Unity Lifecycle

        private void Awake()
        {
            if (!ValidateReferences())
            {
                Debug.LogError("[Disclaimer] Missing UI references!");
                return;
            }

            // Already accepted → skip entirely
            if (skipForTesting || HasAcceptedDisclaimer())
            {
                Log("Disclaimer already accepted - proceeding to Bootstrap");
                LoadBootstrap();
                return;
            }
        }

        private void Start()
        {
            if (!skipForTesting && !HasAcceptedDisclaimer())
            {
                RegisterListeners();
                Log("Disclaimer ready - awaiting user input");
            }
        }

#if UNITY_EDITOR
        private void Update()
        {
            // F10: Force accept and continue
            if (Input.GetKeyDown(KeyCode.F10))
            {
                MarkAccepted();
                LoadBootstrap();
            }

            // F9: Reset disclaimer acceptance
            if (Input.GetKeyDown(KeyCode.F9))
            {
                ResetDisclaimer();
            }
        }
#endif

        #endregion

        #region Setup

        private bool ValidateReferences()
        {
            return confirmButton != null
                && exitButton != null;
        }

        private void RegisterListeners()
        {
            confirmButton.onClick.AddListener(OnConfirm);
            exitButton.onClick.AddListener(OnExit);
        }

        #endregion

        #region UI Callbacks

        private void OnConfirm()
        {
            MarkAccepted();
            Log("Disclaimer accepted - loading Bootstrap");
            LoadBootstrap();
        }

        private void OnExit()
        {
            Log("User declined - exiting");

#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #endregion

        #region Scene Loading

        private void LoadBootstrap()
        {
            Log("Loading Bootstrap scene...");
            SceneManager.LoadScene(SceneNames.BOOTSTRAP);
        }

        #endregion

        #region Disclaimer State (PlayerPrefs)

        private static bool HasAcceptedDisclaimer()
        {
            return PlayerPrefs.GetInt(PlayerPrefKeys.DisclaimerAccepted, PlayerPrefKeys.DefaultDisclaimerAccepted) == 1;
        }

        private static void MarkAccepted()
        {
            PlayerPrefs.SetInt(PlayerPrefKeys.DisclaimerAccepted, 1);
            PlayerPrefs.Save();
            Debug.Log("[Disclaimer] Acceptance saved");
        }

        #endregion

        #region Debug Tools

#if UNITY_EDITOR
        [ContextMenu("Reset Disclaimer")]
        private void ResetDisclaimer()
        {
            PlayerPrefs.DeleteKey(PlayerPrefKeys.DisclaimerAccepted);
            PlayerPrefs.Save();
            Debug.Log("[Disclaimer] Reset - will show on next launch");
        }
#endif

        #endregion

        #region Logging

        private void Log(string message)
        {
            if (enableDebugLogs)
                Debug.Log($"[Disclaimer] {message}");
        }

        #endregion
    }
}