// ════════════════════════════════════════════════════════════════════════
// Assets/Scripts/ChatSim/UI/HomeScreen/Gallery/Components/GalleryThumbnailItem.cs
// ════════════════════════════════════════════════════════════════════════

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using ChatSim.Core;

namespace ChatSim.UI.HomeScreen.Gallery
{
    /// <summary>
    /// Represents a single CG thumbnail in the Gallery App.
    /// Displays locked/unlocked state, loads thumbnail sprite, and handles click events.
    /// Attach to: GalleryThumbnailItem prefab root
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class GalleryThumbnailItem : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════
        // ░ INSPECTOR REFERENCES
        // ═══════════════════════════════════════════════════════════
        [Header("UI References")]
        [SerializeField] private Image thumbnailImage;

        // ═══════════════════════════════════════════════════════════
        // ░ LOGGING
        // ═══════════════════════════════════════════════════════════
        private readonly DebugLogger _log = new DebugLogger(
            "GalleryThumbnailItem",
            () => GameBootstrap.Config?.galleryAppDebugLogs ?? false
        );

        // ═══════════════════════════════════════════════════════════
        // ░ STATE
        // ═══════════════════════════════════════════════════════════
        private Button button;
        private string cgKey;
        private bool isUnlocked;
        private Sprite loadedSprite;
        private AsyncOperationHandle<Sprite> loadHandle;
        private Action<string, Sprite> onClickCallback;

        // ═══════════════════════════════════════════════════════════
        // ░ INITIALIZATION
        // ═══════════════════════════════════════════════════════════
        private void Awake()
        {
            button = GetComponent<Button>();
        }

        public void Initialize(
            string addressableKey, 
            bool unlocked, 
            Sprite lockedSprite, 
            Action<string, Sprite> clickCallback)
        {
            cgKey = addressableKey;
            isUnlocked = unlocked;
            onClickCallback = clickCallback;

            button.onClick.RemoveAllListeners();

            if (isUnlocked)
            {
                StartCoroutine(LoadCGSprite());
                button.onClick.AddListener(OnClicked);
                button.interactable = true;
            }
            else
            {
                if (thumbnailImage != null && lockedSprite != null)
                {
                    thumbnailImage.sprite = lockedSprite;
                    thumbnailImage.color = Color.white;
                }
                else if (thumbnailImage != null)
                {
                    thumbnailImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
                }

                button.interactable = false;
            }
        }

        // ═══════════════════════════════════════════════════════════
        // ░ IMAGE LOADING
        // ═══════════════════════════════════════════════════════════
        private IEnumerator LoadCGSprite()
        {
            if (string.IsNullOrEmpty(cgKey))
            {
                _log.Error("Cannot load: cgKey is null/empty!");
                yield break;
            }

            if (loadHandle.IsValid())
            {
                _log.Warn($"Load already in progress for: {cgKey}");
                yield break;
            }

            loadHandle = Addressables.LoadAssetAsync<Sprite>(cgKey);
            yield return loadHandle;

            if (loadHandle.Status == AsyncOperationStatus.Succeeded)
            {
                loadedSprite = loadHandle.Result;

                if (thumbnailImage != null)
                {
                    thumbnailImage.sprite = loadedSprite;
                    thumbnailImage.color = Color.white;
                }

                _log.Info($"Loaded: {cgKey}");
            }
            else
            {
                _log.Error($"Failed to load: {cgKey}");

                if (thumbnailImage != null)
                {
                    thumbnailImage.color = new Color(1f, 0.3f, 0.3f, 1f);
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        // ░ CLICK HANDLER
        // ═══════════════════════════════════════════════════════════
        private void OnClicked()
        {
            if (!isUnlocked || loadedSprite == null)
            {
                _log.Warn("Cannot open: CG not unlocked or not loaded");
                return;
            }

            onClickCallback?.Invoke(cgKey, loadedSprite);
        }

        // ═══════════════════════════════════════════════════════════
        // ░ CLEANUP
        // ═══════════════════════════════════════════════════════════
        private void OnDestroy()
        {
            if (loadHandle.IsValid())
            {
                Addressables.Release(loadHandle);
            }

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
            }
        }
    }
}