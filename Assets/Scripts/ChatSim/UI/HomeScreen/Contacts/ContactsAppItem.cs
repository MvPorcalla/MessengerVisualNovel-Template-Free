// ════════════════════════════════════════════════════════════════════════
// Assets/Scripts/UI/HomeScreen/Contacts/ContactsAppItem.cs
// Phone Chat Simulation Game - Contacts App Item
// ════════════════════════════════════════════════════════════════════════

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using BubbleSpinner.Data;
using ChatSim.Core;
using ChatSim.UI.Overlay.Dialogs;

namespace ChatSim.UI.HomeScreen.Contacts
{
    /// <summary>
    /// Individual contact button in the Contacts App.
    /// Displays: profile picture, character name, and a reset story button.
    ///
    /// TODO: Clicking the item (itemButton) opens ContactsAppDetailPanel with full character info.
    ///       - Wire up detailPanel reference in ContactsAppPanel.SpawnContactItem()
    ///       - Pass detail panel into Initialize()
    ///       - Uncomment detailPanel field and OnItemClicked body below
    ///       - The detail panel will show all ConversationAsset fields (name, age,
    ///         birthdate, bio, description) with N/A fallback for missing fields
    ///
    /// LOGIC OWNER: This class owns ExecuteReset().
    /// ResetConfirmationDialog is an optional UI middleman.
    ///
    /// Attach to: ContactsAppItem prefab root
    /// </summary>
    public class ContactsAppItem : MonoBehaviour
    {
        #region Inspector References

        [Header("UI Elements")]
        [SerializeField] private Button itemButton;
        [SerializeField] private Image profileImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private Button resetButton;

        // TODO: Uncomment when ContactsAppDetailPanel is ready
        // [Header("Detail Panel Reference")]
        // [SerializeField] private ContactsAppDetailPanel detailPanel;

        #endregion

        #region State

        private ConversationAsset _conversationAsset;
        private ResetConfirmationDialog _confirmationDialog;
        private bool _useConfirmationDialog;
        private AsyncOperationHandle<Sprite> _imageLoadHandle;

        #endregion

        #region Logging

        private readonly DebugLogger _log = new DebugLogger(
            "ContactsAppItem",
            () => GameBootstrap.Config?.contactsAppDebugLogs ?? false
        );

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize this contact button.
        /// Called by ContactsAppPanel when spawning items.
        /// </summary>
        /// <param name="asset">The character's ConversationAsset</param>
        /// <param name="dialog">Shared ResetConfirmationDialog — can be null</param>
        /// <param name="useDialog">If false, reset skips the dialog entirely</param>
        public void Initialize(ConversationAsset asset, ResetConfirmationDialog dialog, bool useDialog)
        {
            _conversationAsset = asset;
            _confirmationDialog = dialog;
            _useConfirmationDialog = useDialog;

            // TODO: Accept ContactsAppDetailPanel parameter once detail panel is ready
            // public void Initialize(ConversationAsset asset, ContactsAppDetailPanel detail, ResetConfirmationDialog dialog, bool useDialog)
            // detailPanel = detail;

            SetupName(asset);
            SetupProfileImage(asset);
            SetupItemButton();
            SetupResetButton();
        }

        private void SetupName(ConversationAsset asset)
        {
            if (nameText != null)
                nameText.text = asset.characterName;
            else
                _log.Warn($"nameText not assigned on {gameObject.name}");
        }

        private void SetupProfileImage(ConversationAsset asset)
        {
            if (profileImage == null)
            {
                _log.Warn($"profileImage not assigned on {gameObject.name}");
                return;
            }

            if (asset.profileImage == null || !asset.profileImage.RuntimeKeyIsValid())
            {
                _log.Warn($"No valid profile image for {asset.characterName}");
                return;
            }

            if (asset.profileImage.OperationHandle.IsValid() && asset.profileImage.OperationHandle.IsDone)
            {
                var sprite = asset.profileImage.OperationHandle.Convert<Sprite>().Result;
                if (sprite != null && profileImage != null)
                {
                    profileImage.sprite = sprite;
                    _log.Info($"✓ Using cached profile image: {asset.characterName}");
                }
                return;
            }

            _imageLoadHandle = asset.profileImage.LoadAssetAsync<Sprite>();
            _imageLoadHandle.Completed += OnProfileImageLoaded;
        }

        private void SetupItemButton()
        {
            if (itemButton == null)
            {
                _log.Error($"itemButton not assigned on {gameObject.name}");
                return;
            }

            itemButton.onClick.RemoveAllListeners();
            itemButton.onClick.AddListener(OnItemClicked);
        }

        private void SetupResetButton()
        {
            if (resetButton == null)
            {
                _log.Error($"resetButton not assigned on {gameObject.name}");
                return;
            }

            resetButton.onClick.RemoveAllListeners();
            resetButton.onClick.AddListener(RequestReset);
        }

        #endregion

        #region Image Loading

        private void OnProfileImageLoaded(AsyncOperationHandle<Sprite> handle)
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                if (profileImage != null)
                    profileImage.sprite = handle.Result;
            }
            else
            {
                _log.Error($"Failed to load profile image for {_conversationAsset?.characterName}");
            }
        }

        #endregion

        #region Item Button

        private void OnItemClicked()
        {
            // TODO: Open ContactsAppDetailPanel with full character info
            // Uncomment once ContactsAppDetailPanel is ready:
            //
            // if (_conversationAsset == null)
            // {
            //     _log.Error("[ContactsAppItem] Cannot open detail: ConversationAsset is null!");
            //     return;
            // }
            // if (detailPanel == null)
            // {
            //     _log.Error("[ContactsAppItem] detailPanel reference is missing!");
            //     return;
            // }
            // detailPanel.Show(_conversationAsset, this);

            _log.Info($"[ContactsAppItem] TODO: Open detail panel for {_conversationAsset?.characterName}");
        }

        #endregion

        #region Reset Logic

        /// <summary>
        /// Entry point for reset — wired to resetButton.onClick via SetupResetButton().
        /// Routes through confirmation dialog if enabled, otherwise resets directly.
        /// </summary>
        public void RequestReset()
        {
            if (_conversationAsset == null)
            {
                _log.Error("Cannot reset: ConversationAsset is null!");
                return;
            }

            if (_useConfirmationDialog && _confirmationDialog != null)
            {
                _confirmationDialog.Show(
                    title: $"Reset {_conversationAsset.characterName}?",
                    message: "This will erase all chat history and progress for this character. This cannot be undone.",
                    onConfirmed: ExecuteReset
                );
                return;
            }

            ExecuteReset();
        }

        /// <summary>
        /// Performs the actual story reset for this character.
        /// Called directly by RequestReset() (no dialog),
        /// OR called by ResetConfirmationDialog after the player confirms Yes.
        /// </summary>
        public void ExecuteReset()
        {
            if (_conversationAsset == null)
            {
                _log.Error("ExecuteReset: ConversationAsset is null!");
                return;
            }

            if (GameBootstrap.Save == null)
            {
                _log.Error("ExecuteReset: GameBootstrap.Save is null!");
                return;
            }

            _log.Info($"Executing story reset for: {_conversationAsset.characterName}");
            GameBootstrap.Save.ResetCharacterStory(_conversationAsset.ConversationId);

            GameBootstrap.Conversation?.EvictConversationCache(_conversationAsset.ConversationId);
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            if (_imageLoadHandle.IsValid())
                Addressables.Release(_imageLoadHandle);
        }

        #endregion
    }
}