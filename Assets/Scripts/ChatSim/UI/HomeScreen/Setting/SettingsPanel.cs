// ════════════════════════════════════════════════════════════════════════
// Assets/Scripts/ChatSim/UI/HomeScreen/Settings/SettingsPanel.cs
// ════════════════════════════════════════════════════════════════════════

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChatSim.Core;
using ChatSim.UI.Overlay.Dialogs;

namespace ChatSim.UI.HomeScreen.Settings
{
    /// <summary>
    /// Settings panel — Gameplay, Data, and About sections.
    /// Attach to: SettingsPanel GameObject (child of Screens in 03_PhoneScreen)
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════
        // ░ INSPECTOR REFERENCES — GAMEPLAY
        // ═══════════════════════════════════════════════════════════
        [Header("Gameplay — Message Speed")]
        [SerializeField] private Button messageSpeedButton;
        [SerializeField] private TextMeshProUGUI messageSpeedLabel;
        [SerializeField] private Image messageSpeedIcon;
        [SerializeField] private Sprite normalModeSprite;
        [SerializeField] private Sprite fastModeSprite;

        [Header("Gameplay — Text Size")]
        [SerializeField] private Button smallTextButton;
        [SerializeField] private Button mediumTextButton;
        [SerializeField] private Button largeTextButton;

        // ═══════════════════════════════════════════════════════════
        // ░ INSPECTOR REFERENCES — DATA
        // ═══════════════════════════════════════════════════════════
        [Header("Data")]
        [SerializeField] private Button resetAllButton;
        [SerializeField] private ResetConfirmationDialog resetAllDialog;

        // ═══════════════════════════════════════════════════════════
        // ░ INSPECTOR REFERENCES — ABOUT
        // ═══════════════════════════════════════════════════════════
        [Header("About")]
        [SerializeField] private TextMeshProUGUI versionText;

        // ═══════════════════════════════════════════════════════════
        // ░ CONSTANTS
        // ═══════════════════════════════════════════════════════════
        private const float TEXT_SIZE_SMALL  = 36f;
        private const float TEXT_SIZE_MEDIUM = 42f;
        private const float TEXT_SIZE_LARGE  = 48f;

        // ═══════════════════════════════════════════════════════════
        // ░ STATE
        // ═══════════════════════════════════════════════════════════
        private float currentTextSize;
        private bool isFastMode = false;

        // ═══════════════════════════════════════════════════════════
        // ░ LOGGING
        // ═══════════════════════════════════════════════════════════
        private readonly DebugLogger _log = new DebugLogger(
            "SettingsPanel",
            () => GameBootstrap.Config?.settingsPanelDebugLogs ?? false
        );

        // ═══════════════════════════════════════════════════════════
        // ░ UNITY LIFECYCLE
        // ═══════════════════════════════════════════════════════════
        private void Awake() => SetupButtons();
        private void OnEnable() => LoadAndApplySettings();

        // ═══════════════════════════════════════════════════════════
        // ░ INITIALIZATION
        // ═══════════════════════════════════════════════════════════
        private void SetupButtons()
        {
            messageSpeedButton?.onClick.AddListener(OnMessageSpeedButtonClicked);
            smallTextButton?.onClick.AddListener(() => OnTextSizeSelected(TEXT_SIZE_SMALL));
            mediumTextButton?.onClick.AddListener(() => OnTextSizeSelected(TEXT_SIZE_MEDIUM));
            largeTextButton?.onClick.AddListener(() => OnTextSizeSelected(TEXT_SIZE_LARGE));
            resetAllButton?.onClick.AddListener(OnResetAllClicked);

            if (versionText != null)
                versionText.text = $"Version {Application.version}";
        }

        private void LoadAndApplySettings()
        {
            isFastMode = PlayerPrefs.GetInt(PlayerPrefKeys.FastMode, PlayerPrefKeys.DefaultFastMode) == 1;
            UpdateMessageSpeedVisuals();

            currentTextSize = PlayerPrefs.GetFloat(PlayerPrefKeys.TextSize, TEXT_SIZE_LARGE);
            UpdateTextSizeButtonStates(currentTextSize);
        }

        // ═══════════════════════════════════════════════════════════
        // ░ GAMEPLAY — MESSAGE SPEED
        // ═══════════════════════════════════════════════════════════
        private void OnMessageSpeedButtonClicked()
        {
            isFastMode = !isFastMode;
            _log.Info($"Message speed: {(isFastMode ? "Fast" : "Normal")}");
            PlayerPrefs.SetInt(PlayerPrefKeys.FastMode, isFastMode ? 1 : 0);
            PlayerPrefs.Save();
            UpdateMessageSpeedVisuals();
            GameEvents.TriggerMessageSpeedChanged(isFastMode);
        }

        private void UpdateMessageSpeedVisuals()
        {
            if (messageSpeedLabel != null)
                messageSpeedLabel.text = isFastMode ? "Fast" : "Normal";

            if (messageSpeedIcon != null && normalModeSprite != null && fastModeSprite != null)
                messageSpeedIcon.sprite = isFastMode ? fastModeSprite : normalModeSprite;
        }

        // ═══════════════════════════════════════════════════════════
        // ░ GAMEPLAY — TEXT SIZE
        // ═══════════════════════════════════════════════════════════
        private void OnTextSizeSelected(float fontSize)
        {
            if (Mathf.Approximately(fontSize, currentTextSize)) return;

            currentTextSize = fontSize;
            _log.Info($"Text size: {fontSize}");
            PlayerPrefs.SetFloat(PlayerPrefKeys.TextSize, fontSize);
            PlayerPrefs.Save();
            UpdateTextSizeButtonStates(fontSize);
            GameEvents.TriggerTextSizeChanged(fontSize);
        }

        private void UpdateTextSizeButtonStates(float selectedSize)
        {
            SetButtonAlpha(smallTextButton,  Mathf.Approximately(selectedSize, TEXT_SIZE_SMALL)  ? 1f : 0.4f);
            SetButtonAlpha(mediumTextButton, Mathf.Approximately(selectedSize, TEXT_SIZE_MEDIUM) ? 1f : 0.4f);
            SetButtonAlpha(largeTextButton,  Mathf.Approximately(selectedSize, TEXT_SIZE_LARGE)  ? 1f : 0.4f);
        }

        private void SetButtonAlpha(Button button, float alpha)
        {
            if (button == null) return;

            var canvasGroup = button.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = button.gameObject.AddComponent<CanvasGroup>();

            canvasGroup.alpha = alpha;
        }

        // ═══════════════════════════════════════════════════════════
        // ░ DATA — RESET ALL
        // ═══════════════════════════════════════════════════════════
        private void OnResetAllClicked()
        {
            _log.Info("Reset all clicked");

            if (resetAllDialog != null)
            {
                resetAllDialog.Show(
                    title: "Reset All Stories?",
                    message: "This will erase ALL chat history and progress for every character. This cannot be undone.",
                    onConfirmed: OnResetAllConfirmed
                );
            }
            else
            {
                _log.Warn("resetAllDialog not assigned — resetting directly");
                OnResetAllConfirmed();
            }
        }

        private void OnResetAllConfirmed()
        {
            _log.Info("Reset all confirmed");

            if (GameBootstrap.Save == null)
            {
                _log.Error("GameBootstrap.Save is null!");
                return;
            }

            if (GameBootstrap.Conversation == null)
            {
                _log.Error("GameBootstrap.Conversation is null!");
                return;
            }

            GameBootstrap.Save.ResetAllData();
            GameBootstrap.Conversation.EvictAllConversationCaches();
        }
    }
}