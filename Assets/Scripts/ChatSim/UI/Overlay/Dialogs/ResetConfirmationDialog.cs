// ════════════════════════════════════════════════════════════════════════
// Assets/Scripts/ChatSim/UI/Overlay/Dialogs/ResetConfirmationDialog.cs
// ════════════════════════════════════════════════════════════════════════

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ChatSim.UI.Overlay.Dialogs
{
    /// <summary>
    /// Reusable confirmation dialog for story resets.
    /// Used by both ContactsAppItem (single character) and SettingsPanel (reset all).
    /// Attach to: ResetConfirmationDialog GameObject (child of DialogOverlay)
    /// </summary>
    public class ResetConfirmationDialog : MonoBehaviour
    {
        #region Inspector References

        [Header("UI Elements")]
        [SerializeField] private GameObject confirmationDialog;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Button yesButton;
        [SerializeField] private Button noButton;

        #endregion

        #region State

        private Action _onConfirmed;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            SetupButtons();

            if (confirmationDialog != null)
                confirmationDialog.SetActive(false);
        }

        #endregion

        #region Setup

        private void SetupButtons()
        {
            if (yesButton != null)
            {
                yesButton.onClick.RemoveAllListeners();
                yesButton.onClick.AddListener(OnYesClicked);
            }
            else
            {
                Debug.LogError("[ResetConfirmationDialog] yesButton not assigned!");
            }

            if (noButton != null)
            {
                noButton.onClick.RemoveAllListeners();
                noButton.onClick.AddListener(OnNoClicked);
            }
            else
            {
                Debug.LogError("[ResetConfirmationDialog] noButton not assigned!");
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Generic show — all callers use this overload.
        /// Pass title, message, and a callback to invoke on confirm.
        /// </summary>
        public void Show(string title, string message, Action onConfirmed)
        {
            _onConfirmed = onConfirmed;

            if (titleText != null)   titleText.text   = title;
            if (messageText != null) messageText.text = message;

            if (confirmationDialog != null)
                confirmationDialog.SetActive(true);

            #if UNITY_EDITOR
            Debug.Log($"[ResetConfirmationDialog] Showing: {title}");
            #endif
        }

        /// <summary>
        /// Hide the dialog without taking any action.
        /// </summary>
        public void Hide()
        {
            _onConfirmed = null;

            if (confirmationDialog != null)
                confirmationDialog.SetActive(false);
        }

        #endregion

        #region Button Handlers

        private void OnYesClicked()
        {
            var callback = _onConfirmed;
            Hide();
            callback?.Invoke();
        }

        private void OnNoClicked()
        {
            Hide();
        }

        #endregion
    }
}