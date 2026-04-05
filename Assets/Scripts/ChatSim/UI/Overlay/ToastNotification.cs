// ════════════════════════════════════════════════════════════════════════
// Assets/Scripts/ChatSim/UI/Overlay/ToastNotification.cs
// ════════════════════════════════════════════════════════════════════════

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChatSim.Core;

namespace ChatSim.UI.Overlay
{
    /// <summary>
    /// Reusable toast notification — slides in from top, holds, fades out.
    /// Subscribes to GameEvents for automatic reset confirmations.
    /// Attach to: ToastNotification GameObject (child of Overlays in 03_PhoneScreen)
    /// </summary>
    public class ToastNotification : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════
        // ░ TOAST TYPE
        // ═══════════════════════════════════════════════════════════

        public enum ToastType { Success, Info, Warning }

        // ═══════════════════════════════════════════════════════════
        // ░ INSPECTOR REFERENCES
        // ═══════════════════════════════════════════════════════════

        [Header("UI Elements")]
        [SerializeField] private GameObject toastPanel;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Image icon;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Icons")]
        [SerializeField] private Sprite successSprite;
        [SerializeField] private Sprite infoSprite;
        [SerializeField] private Sprite warningSprite;

        [Header("Colors")]
        [SerializeField] private Color successColor = new Color(0.2f, 0.8f, 0.4f);
        [SerializeField] private Color infoColor    = new Color(0.3f, 0.6f, 1.0f);
        [SerializeField] private Color warningColor = new Color(1.0f, 0.7f, 0.2f);

        [Header("Timing")]
        [SerializeField] private float holdDuration  = 2.5f;
        [SerializeField] private float fadeDuration  = 0.3f;
        [SerializeField] private float slideDistance = 80f;

        // ═══════════════════════════════════════════════════════════
        // ░ STATE
        // ═══════════════════════════════════════════════════════════

        private Coroutine activeCoroutine;
        private RectTransform toastRect;
        private Vector2 restingPosition;

        // ═══════════════════════════════════════════════════════════
        // ░ UNITY LIFECYCLE
        // ═══════════════════════════════════════════════════════════

        private void Awake()
        {
            if (toastPanel != null)
            {
                toastRect = toastPanel.GetComponent<RectTransform>();
                restingPosition = toastRect.anchoredPosition; // ← store before hiding
                toastPanel.SetActive(false);
            }
        }

        private void OnEnable()
        {
            GameEvents.OnCharacterStoryReset += OnCharacterStoryReset;
            GameEvents.OnAllStoriesReset     += OnAllStoriesReset;
        }

        private void OnDisable()
        {
            GameEvents.OnCharacterStoryReset -= OnCharacterStoryReset;
            GameEvents.OnAllStoriesReset     -= OnAllStoriesReset;
        }

        // ═══════════════════════════════════════════════════════════
        // ░ GAME EVENT HANDLERS
        // ═══════════════════════════════════════════════════════════

        private void OnCharacterStoryReset(string conversationId)
        {
            Show(
                title: "Story Reset",
                message: "Character story has been cleared.",
                type: ToastType.Success
            );
        }

        private void OnAllStoriesReset()
        {
            Show(
                title: "All Stories Reset",
                message: "All progress has been erased.",
                type: ToastType.Success
            );
        }

        // ═══════════════════════════════════════════════════════════
        // ░ PUBLIC API
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Show a toast notification.
        /// Safe to call while another toast is showing — interrupts and replaces it.
        /// </summary>
        public void Show(string title, string message, ToastType type = ToastType.Success)
        {
            if (activeCoroutine != null)
                StopCoroutine(activeCoroutine);

            activeCoroutine = StartCoroutine(ShowSequence(title, message, type));
        }

        // ═══════════════════════════════════════════════════════════
        // ░ SEQUENCE
        // ═══════════════════════════════════════════════════════════

        private IEnumerator ShowSequence(string title, string message, ToastType type)
        {
            // Setup content
            if (titleText != null)   titleText.text   = title;
            if (messageText != null) messageText.text = message;

            SetTypeVisuals(type);

            // Activate panel
            toastPanel.SetActive(true);

            // Slide in + fade in
            yield return StartCoroutine(Animate(slideIn: true));

            // Hold
            yield return new WaitForSeconds(holdDuration);

            // Fade out + slide out
            yield return StartCoroutine(Animate(slideIn: false));

            toastPanel.SetActive(false);
            activeCoroutine = null;
        }

        private IEnumerator Animate(bool slideIn)
        {
            if (canvasGroup == null || toastRect == null) yield break;

            float elapsed = 0f;
            float startY  = slideIn ? restingPosition.y + slideDistance : restingPosition.y;
            float endY    = slideIn ? restingPosition.y : restingPosition.y + slideDistance;
            float startA  = slideIn ? 0f : 1f;
            float endA    = slideIn ? 1f : 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / fadeDuration);

                canvasGroup.alpha = Mathf.Lerp(startA, endA, t);
                toastRect.anchoredPosition = new Vector2(
                    restingPosition.x,
                    Mathf.Lerp(startY, endY, t)
                );

                yield return null;
            }

            canvasGroup.alpha = endA;
            toastRect.anchoredPosition = new Vector2(restingPosition.x, endY);
        }

        // ═══════════════════════════════════════════════════════════
        // ░ VISUALS
        // ═══════════════════════════════════════════════════════════

        private void SetTypeVisuals(ToastType type)
        {
            Color color = type switch
            {
                ToastType.Success => successColor,
                ToastType.Warning => warningColor,
                _                 => infoColor
            };

            Sprite sprite = type switch
            {
                ToastType.Success => successSprite,
                ToastType.Warning => warningSprite,
                _                 => infoSprite
            };

            if (icon != null)
            {
                icon.color = color;
                if (sprite != null) icon.sprite = sprite;
            }

            if (titleText != null)
                titleText.color = color;
        }
    }
}