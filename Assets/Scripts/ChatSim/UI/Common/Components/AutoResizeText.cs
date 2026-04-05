// ════════════════════════════════════════════════════════════════════════
// Assets/Scripts/ChatSim/UI/Common/Components/AutoResizeText.cs
// ════════════════════════════════════════════════════════════════════════

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using ChatSim.Core;

namespace ChatSim.UI.Common.Components
{
    /// <summary>
    /// Automatically resizes TextMeshProUGUI width based on content.
    /// Designed for message bubbles to ensure proper sizing without manual adjustments.
    /// Attach to: TextMeshProUGUI GameObject with LayoutElement
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    [RequireComponent(typeof(LayoutElement))]
    public class AutoResizeText : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════
        // ░ COMPONENTS
        // ═══════════════════════════════════════════════════════════
        
        private TextMeshProUGUI textComponent;
        private LayoutElement layoutElement;
        private RectTransform rectTransform;

        // ═══════════════════════════════════════════════════════════
        // ░ STATE
        // ═══════════════════════════════════════════════════════════
        
        private float lastCalculatedWidth = -1f;
        private Coroutine layoutRebuildCoroutine;
        private bool isInitialized = false;

        // ═══════════════════════════════════════════════════════════
        // ░ LOGGER
        // ═══════════════════════════════════════════════════════════

        private readonly DebugLogger _log = new DebugLogger(
            "AutoResize",
            () => GameBootstrap.Config?.imageMessageBubbleDebugLogs ?? false
        );

        // ═══════════════════════════════════════════════════════════
        // ░ PROPERTIES
        // ═══════════════════════════════════════════════════════════
        
        public bool IsInitialized => isInitialized;

        private float MaxWidth => GameBootstrap.Config != null ? GameBootstrap.Config.bubbleMaxWidth : 650f;
        private float MinWidth => GameBootstrap.Config != null ? GameBootstrap.Config.bubbleMinWidth : 40f;
        private float WidthChangeThreshold => GameBootstrap.Config != null ? GameBootstrap.Config.bubbleWidthChangeThreshold : 0.1f;

        // ═══════════════════════════════════════════════════════════
        // ░ INITIALIZATION
        // ═══════════════════════════════════════════════════════════

        void Awake()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            if (isInitialized) return;

            try
            {
                textComponent = GetComponent<TextMeshProUGUI>();
                layoutElement = GetComponent<LayoutElement>();
                rectTransform = transform as RectTransform;

                if (textComponent == null)
                {
                    _log.Error("TextMeshProUGUI missing on " + gameObject.name);
                    return;
                }

                if (layoutElement == null)
                {
                    _log.Error("LayoutElement missing on " + gameObject.name);
                    return;
                }

                textComponent.enableWordWrapping = true;
                isInitialized = true;
            }
            catch (System.Exception e)
            {
                _log.Error("Initialization failed on " + gameObject.name + ": " + e.Message);
                isInitialized = false;
            }
        }

        void Start()
        {
            if (!isInitialized)
            {
                InitializeComponents();
            }

            if (isInitialized)
            {
                SetupLayoutElement();
                UpdateWidthImmediate();
            }
        }

        void OnDestroy()
        {
            if (layoutRebuildCoroutine != null)
            {
                StopCoroutine(layoutRebuildCoroutine);
                layoutRebuildCoroutine = null;
            }
        }

        private void SetupLayoutElement()
        {
            if (layoutElement != null)
            {
                layoutElement.minWidth = MinWidth;
                layoutElement.flexibleWidth = 0;
            }
        }

        // ═══════════════════════════════════════════════════════════
        // ░ PUBLIC API
        // ═══════════════════════════════════════════════════════════

        public void SetText(string newText)
        {
            if (!isInitialized)
            {
                _log.Warn("SetText called before init on " + gameObject.name);
                InitializeComponents();

                if (!isInitialized)
                {
                    _log.Error("Failed to initialize on " + gameObject.name);
                    return;
                }
            }

            if (textComponent != null && textComponent.text != newText)
            {
                textComponent.text = newText;
                UpdateWidth();
            }
        }

        public void RefreshWidth()
        {
            if (!isInitialized)
            {
                _log.Warn("RefreshWidth called before init on " + gameObject.name);
                return;
            }

            if (gameObject.activeInHierarchy)
                UpdateWidth();
            else
                UpdateWidthImmediate();
        }

        [ContextMenu("Force Reinitialize")]
        public void ForceReinitialize()
        {
            isInitialized = false;
            lastCalculatedWidth = -1f;
            
            InitializeComponents();
            
            if (isInitialized)
            {
                SetupLayoutElement();
                UpdateWidthImmediate();
            }
        }

        // ═══════════════════════════════════════════════════════════
        // ░ WIDTH CALCULATION
        // ═══════════════════════════════════════════════════════════

        private float CalculatePreferredWidth()
        {
            if (string.IsNullOrEmpty(textComponent.text))
                return MinWidth;

            Vector2 textSize = textComponent.GetPreferredValues(textComponent.text, MaxWidth, 0);
            return Mathf.Clamp(textSize.x, MinWidth, MaxWidth);
        }

        private bool CalculateAndApplyWidth()
        {
            if (!isInitialized || textComponent == null || layoutElement == null)
                return false;

            try
            {
                float preferredWidth = CalculatePreferredWidth();

                if (Mathf.Abs(preferredWidth - lastCalculatedWidth) > WidthChangeThreshold)
                {
                    layoutElement.preferredWidth = preferredWidth;
                    lastCalculatedWidth = preferredWidth;
                    return true;
                }
            }
            catch (System.Exception e)
            {
                _log.Error("Width calculation failed on " + gameObject.name + ": " + e.Message);
            }

            return false;
        }

        // ═══════════════════════════════════════════════════════════
        // ░ UPDATE METHODS
        // ═══════════════════════════════════════════════════════════

        private void UpdateWidth()
        {
            if (!CalculateAndApplyWidth())
                return;

            if (gameObject.activeInHierarchy)
            {
                if (layoutRebuildCoroutine != null)
                    StopCoroutine(layoutRebuildCoroutine);

                layoutRebuildCoroutine = StartCoroutine(RebuildLayoutEndOfFrame());
            }
            else
            {
                if (rectTransform != null)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            }
        }

        private void UpdateWidthImmediate()
        {
            if (!CalculateAndApplyWidth())
                return;

            if (rectTransform != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }

        private IEnumerator RebuildLayoutEndOfFrame()
        {
            yield return new WaitForEndOfFrame();

            try
            {
                if (rectTransform != null)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            }
            catch (System.Exception e)
            {
                _log.Error("Layout rebuild failed on " + gameObject.name + ": " + e.Message);
            }
            finally
            {
                layoutRebuildCoroutine = null;
            }
        }

        // ═══════════════════════════════════════════════════════════
        // ░ EDITOR SUPPORT
        // ═══════════════════════════════════════════════════════════

        void OnValidate()
        {
            if (Application.isPlaying && isInitialized)
            {
                RefreshWidth();
            }
        }
    }
}