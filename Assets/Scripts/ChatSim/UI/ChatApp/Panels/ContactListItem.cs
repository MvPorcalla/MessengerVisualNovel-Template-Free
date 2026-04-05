// ════════════════════════════════════════════════════════════════════════
// Assets/Scripts/UI/ChatApp/Panels/ContactListItem.cs
// ════════════════════════════════════════════════════════════════════════

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using BubbleSpinner.Data;
using ChatSim.UI.ChatApp.Controllers;
using ChatSim.Core;

namespace ChatSim.UI.ChatApp.Panels
{
    /// <summary>
    /// Individual contact button in the contact list
    /// Attach to: ContactListItem prefab
    /// </summary>
    public class ContactListItem : MonoBehaviour
    {
        #region Inspector References
        
        [Header("UI Elements")]
        [SerializeField] private Button button;
        [SerializeField] private Image profileIMG;
        [SerializeField] private TextMeshProUGUI profileName;
        [SerializeField] private TextMeshProUGUI lastMessageText;
        [SerializeField] private GameObject badge;
        
        #endregion
        
        #region State
        
        private ConversationAsset conversationAsset;
        private ChatAppController chatController;
        private AsyncOperationHandle<Sprite> imageLoadHandle;
        
        #endregion

        #region Logging
        private readonly DebugLogger _log = new DebugLogger(
            "ContactListItem",
            () => GameBootstrap.Config?.contactChatListDebugLogs ?? false
        );
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// Initialize the button with conversation data
        /// Called by ContactListPanel when creating buttons
        /// </summary>
        public void Initialize(ConversationAsset asset, ChatAppController controller, string lastMessage)
        {
            conversationAsset = asset;
            chatController = controller;

            if (profileName != null)
                profileName.text = asset.characterName;

            if (profileIMG != null && asset.profileImage != null && asset.profileImage.RuntimeKeyIsValid())
                LoadProfileImage(asset.profileImage);
            else
                _log.Warn($"No valid profile image for {asset.characterName}");

            if (badge != null)
                badge.SetActive(false);

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OnButtonClicked);
            }
            else
                _log.Error("Button component not assigned!");

            if (lastMessageText != null)
                lastMessageText.text = lastMessage ?? "";
        }
        
        #endregion
        
        #region Image Loading
        
        private void LoadProfileImage(AssetReference assetRef)
        {
            // If already loaded, reuse the result directly — do not call LoadAssetAsync again
            if (assetRef.OperationHandle.IsValid() && assetRef.OperationHandle.IsDone)
            {
                var sprite = assetRef.OperationHandle.Convert<Sprite>().Result;
                if (sprite != null && profileIMG != null)
                    profileIMG.sprite = sprite;
                return;
            }

            // Release our own previous handle if we have one
            if (imageLoadHandle.IsValid())
                Addressables.Release(imageLoadHandle);

            imageLoadHandle = assetRef.LoadAssetAsync<Sprite>();
            imageLoadHandle.Completed += OnProfileImageLoaded;
        }
        
        private void OnProfileImageLoaded(AsyncOperationHandle<Sprite> handle)
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                if (profileIMG != null)
                {
                    profileIMG.sprite = handle.Result;
                    _log.Info($"✓ Profile image loaded: {conversationAsset.characterName}");
                }
            }
            else
            {
                _log.Error($"✗ Failed to load profile image: {conversationAsset.characterName}");
            }
        }
        
        #endregion
        
        #region Button Interaction
        
        private void OnButtonClicked()
        {
            if (conversationAsset == null)
            {
                _log.Error("No conversation asset assigned!");
                return;
            }

            if (chatController == null)
            {
                _log.Error("No chat controller assigned!");
                return;
            }

            _log.Info($"Opening conversation with {conversationAsset.characterName}");
            chatController.StartConversation(conversationAsset);
        }
        
        #endregion
        
        #region Cleanup
        
        private void OnDestroy()
        {
            // Release Addressables handle
            if (imageLoadHandle.IsValid())
            {
                Addressables.Release(imageLoadHandle);
            }
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Show/hide the notification badge
        /// </summary>
        public void SetBadgeVisible(bool visible)
        {
            if (badge != null)
            {
                badge.SetActive(visible);
            }
        }
        
        #endregion

    }
}