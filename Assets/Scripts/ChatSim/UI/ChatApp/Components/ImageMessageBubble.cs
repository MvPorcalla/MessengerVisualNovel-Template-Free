// ════════════════════════════════════════════════════════════════════════
// Assets/Scripts/ChatSim/UI/ChatApp/Components/ImageMessageBubble.cs
// ════════════════════════════════════════════════════════════════════════

using UnityEngine;
using UnityEngine.UI;
using BubbleSpinner.Data;
using ChatSim.Core;

namespace ChatSim.UI.ChatApp.Components
{
    /// <summary>
    /// Component for message bubbles that display images (CGs).
    /// Handles loading the image from Addressables, displaying it in the bubble, and opening a fullscreen viewer when clicked.
    /// Attach to: NpcImageBubble and PlayerImageBubble prefabs
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ImageMessageBubble : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════
        // ░ INSPECTOR REFERENCES
        // ═══════════════════════════════════════════════════════════
        
        [Header("Image Display")]
        [SerializeField] private Image cgImage;
        
        [Header("Fullscreen Viewer")]
        [Tooltip("Reference to FullscreenCGViewer (find at runtime if null)")]
        [SerializeField] private FullscreenCGViewer fullscreenViewer;
        
        // ═══════════════════════════════════════════════════════════
        // ░ STATE
        // ═══════════════════════════════════════════════════════════
        
        private MessageData messageData;
        private Sprite loadedSprite;
        private Button clickButton;

        // ═══════════════════════════════════════════════════════════
        // ░ LOGGING
        // ═══════════════════════════════════════════════════════════

        private readonly DebugLogger _log = new DebugLogger(
            "ImageMessageBubble",
            () => GameBootstrap.Config?.imageMessageBubbleDebugLogs ?? false
        );
        
        // ═══════════════════════════════════════════════════════════
        // ░ INITIALIZATION
        // ═══════════════════════════════════════════════════════════
        
        private void Awake()
        {
            clickButton = GetComponent<Button>();
            clickButton.onClick.AddListener(OnImageClicked);
            
            // Find fullscreen viewer if not assigned
            if (fullscreenViewer == null)
            {
                fullscreenViewer = FindObjectOfType<FullscreenCGViewer>(true);
            }
        }
        
        /// <summary>
        /// Initialize the image bubble with message data
        /// </summary>
        public void Initialize(MessageData msg, bool instant = false)
        {
            messageData = msg;
            
            // Load image from Addressables
            LoadImage(msg.imagePath);
        }
        
        // ═══════════════════════════════════════════════════════════
        // ░ IMAGE LOADING
        // ═══════════════════════════════════════════════════════════
        
        private void LoadImage(string addressableKey)
        {
            if (string.IsNullOrEmpty(addressableKey))
            {
                _log.Error("No image path specified");
                return;
            }

            _log.Info($"Loading: {addressableKey}");

            AddressablesImageLoader.LoadSpriteAsync(
                addressableKey,
                onLoaded: OnImageLoaded,
                onFailed: OnImageLoadFailed
            );
        }
        
        private void OnImageLoaded(Sprite sprite)
        {
            if (sprite == null)
            {
                _log.Error("Loaded sprite is null");
                return;
            }

            loadedSprite = sprite;

            if (cgImage != null)
            {
                cgImage.sprite = sprite;
                cgImage.enabled = true;
            }

            _log.Info($"✓ Image loaded: {messageData.imagePath}");
        }
        
        private void OnImageLoadFailed(string error)
        {
            _log.Error($"✗ Load failed: {messageData.imagePath}\n{error}");
        }
        
        // ═══════════════════════════════════════════════════════════
        // ░ FULLSCREEN INTERACTION
        // ═══════════════════════════════════════════════════════════
        
        private void OnImageClicked()
        {
            if (loadedSprite == null)
            {
                _log.Warn("Cannot show fullscreen - sprite not loaded");
                return;
            }

            if (fullscreenViewer == null)
            {
                _log.Error("FullscreenCGViewer not found!");
                return;
            }

            _log.Info($"Opening fullscreen: {messageData.imagePath}");
            fullscreenViewer.ShowFullscreen(loadedSprite, messageData.imagePath);
        }
        
        // ═══════════════════════════════════════════════════════════
        // ░ CLEANUP
        // ═══════════════════════════════════════════════════════════
        
        private void OnDestroy()
        {
            if (clickButton != null)
            {
                clickButton.onClick.RemoveListener(OnImageClicked);
            }
        }
    }
}