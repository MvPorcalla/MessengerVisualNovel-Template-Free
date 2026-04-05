// ════════════════════════════════════════════════════════════════════════
// Assets/Scripts/ChatSim/UI/ChatApp/FullscreenCGViewer.cs
// Fullscreen CG Viewer - Pinch zoom, pan, swipe to close
// ════════════════════════════════════════════════════════════════════════

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChatSim.Core;

namespace ChatSim.UI.ChatApp
{
    /// <summary>
    /// Displays CG images in fullscreen mode.
    /// Features: Pinch-to-zoom, pan, tap-to-close
    /// Attach to: FullscreenCGViewer GameObject (child of Canvas)
    /// </summary>
    public class FullscreenCGViewer : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════
        // ░ INSPECTOR REFERENCES
        // ═══════════════════════════════════════════════════════════
        
        [Header("UI Elements")]
        [SerializeField] private GameObject viewerPanel;
        [SerializeField] private Image cgImage;
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI cgNameText;
        [SerializeField] private CanvasGroup canvasGroup;
        
        // ═══════════════════════════════════════════════════════════
        // ░ STATE
        // ═══════════════════════════════════════════════════════════
        
        private float currentZoom = 1f;
        private Vector2 lastTouchPosition;
        private bool isDragging = false;
        private RectTransform imageRect;
        private Coroutine fadeCoroutine;

        // ═══════════════════════════════════════════════════════════
        // ░ CONFIG ACCESSORS
        // ═══════════════════════════════════════════════════════════

        private float MinZoom          => GameBootstrap.Config != null ? GameBootstrap.Config.minZoom              : 1f;
        private float MaxZoom          => GameBootstrap.Config != null ? GameBootstrap.Config.maxZoom              : 3f;
        private float ZoomSpeed        => GameBootstrap.Config != null ? GameBootstrap.Config.zoomSpeed            : 0.001f;
        private float FadeDuration     => GameBootstrap.Config != null ? GameBootstrap.Config.cgViewerFadeDuration : 0.3f;
        
        // ═══════════════════════════════════════════════════════════
        // ░ INITIALIZATION
        // ═══════════════════════════════════════════════════════════
        
        private void Awake()
        {
            if (cgImage != null)
            {
                imageRect = cgImage.rectTransform;
                cgImage.preserveAspect = true;
            }
            
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Hide);
            }
            
            // Start hidden
            if (viewerPanel != null)
            {
                viewerPanel.SetActive(false);
            }
        }
        
        // ═══════════════════════════════════════════════════════════
        // ░ PUBLIC API
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>
        /// Show a CG in fullscreen mode
        /// </summary>
        public void ShowFullscreen(Sprite sprite, string cgName = "")
        {
            if (sprite == null)
            {
                Debug.LogError("[FullscreenCGViewer] Cannot show null sprite!");
                return;
            }
            
            // Set sprite
            if (cgImage != null)
            {
                cgImage.sprite = sprite;
            }
            
            // Set name (optional)
            if (cgNameText != null && !string.IsNullOrEmpty(cgName))
            {
                cgNameText.text = cgName;
                cgNameText.gameObject.SetActive(true);
            }
            else if (cgNameText != null)
            {
                cgNameText.gameObject.SetActive(false);
            }
            
            // Reset zoom/pan
            ResetTransform();
            
            // Show with fade-in
            if (viewerPanel != null)
            {
                viewerPanel.SetActive(true);
            }
            
            // Fade in
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            fadeCoroutine = StartCoroutine(FadeCanvasGroup(0f, 1f, FadeDuration));
        }
        
        /// <summary>
        /// Hide the fullscreen viewer
        /// </summary>
        public void Hide()
        {
            
            // Fade out
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            fadeCoroutine = StartCoroutine(FadeCanvasGroupAndHide());
        }
        
        // ═══════════════════════════════════════════════════════════
        // ░ FADE ANIMATIONS (Coroutine-based)
        // ═══════════════════════════════════════════════════════════
        
        private IEnumerator FadeCanvasGroup(float fromAlpha, float toAlpha, float duration)
        {
            if (canvasGroup == null)
                yield break;
            
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                canvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, t);
                yield return null;
            }
            
            canvasGroup.alpha = toAlpha;
        }
        
        private IEnumerator FadeCanvasGroupAndHide()
        {
            yield return FadeCanvasGroup(1f, 0f, FadeDuration);

            if (viewerPanel != null)
                viewerPanel.SetActive(false);
        }
        
        // ═══════════════════════════════════════════════════════════
        // ░ ZOOM & PAN (MOBILE TOUCH)
        // ═══════════════════════════════════════════════════════════
        
        private void Update()
        {
            if (viewerPanel == null || !viewerPanel.activeSelf || imageRect == null)
                return;

            if (Input.touchCount == 2)
            {
                HandlePinchZoom();
            }
            else if (Input.touchCount == 1)
            {
                HandleDrag();
            }
            else if (Input.mouseScrollDelta.y != 0)
            {
                HandleMouseWheelZoom();
            }
        }
        
        private void HandlePinchZoom()
        {
            isDragging = false;

            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);

            Vector2 touch0PrevPos = touch0.position - touch0.deltaPosition;
            Vector2 touch1PrevPos = touch1.position - touch1.deltaPosition;

            float prevMagnitude = (touch0PrevPos - touch1PrevPos).magnitude;
            float currentMagnitude = (touch0.position - touch1.position).magnitude;
            float difference = currentMagnitude - prevMagnitude;
            float zoomDelta = difference * ZoomSpeed;

            SetZoom(currentZoom + zoomDelta);
        }
        
        private void HandleDrag()
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                isDragging = true;
                lastTouchPosition = touch.position;
            }
            else if (touch.phase == TouchPhase.Moved && isDragging)
            {
                Vector2 delta = touch.position - lastTouchPosition;
                imageRect.anchoredPosition += delta;
                imageRect.anchoredPosition = ClampPosition(imageRect.anchoredPosition);
                lastTouchPosition = touch.position;
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                isDragging = false;
            }
        }
        
        private void HandleMouseWheelZoom()
        {
            float scrollDelta = Input.mouseScrollDelta.y;
            float zoomDelta = scrollDelta * 0.1f;
            SetZoom(currentZoom + zoomDelta);
        }
        
        // ═══════════════════════════════════════════════════════════
        // ░ TRANSFORM CONTROL
        // ═══════════════════════════════════════════════════════════
        
        private void SetZoom(float newZoom)
        {
            currentZoom = Mathf.Clamp(newZoom, MinZoom, MaxZoom);
            imageRect.localScale = Vector3.one * currentZoom;

            if (Mathf.Approximately(currentZoom, MinZoom))
                imageRect.anchoredPosition = Vector2.zero;
        }
        
        private void ResetTransform()
        {
            currentZoom = 1f;
            
            if (imageRect != null)
            {
                imageRect.localScale = Vector3.one;
                imageRect.anchoredPosition = Vector2.zero;
            }
        }

        // ═══════════════════════════════════════════════════════════
        // ░ HELPERS
        // ═══════════════════════════════════════════════════════════

        private Vector2 ClampPosition(Vector2 position)
        {
            if (imageRect == null) return position;

            RectTransform parentRect = imageRect.parent as RectTransform;
            if (parentRect == null) return position;

            Vector2 imageSize = imageRect.rect.size * currentZoom;
            Vector2 panelSize = parentRect.rect.size;

            Vector2 maxPan = (imageSize - panelSize) / 2f;
            maxPan = Vector2.Max(maxPan, Vector2.zero);

            return new Vector2(
                Mathf.Clamp(position.x, -maxPan.x, maxPan.x),
                Mathf.Clamp(position.y, -maxPan.y, maxPan.y)
            );
        }
        
        // ═══════════════════════════════════════════════════════════
        // ░ CLEANUP
        // ═══════════════════════════════════════════════════════════
        
        private void OnDestroy()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Hide);
            }
            
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
        }
    }
}