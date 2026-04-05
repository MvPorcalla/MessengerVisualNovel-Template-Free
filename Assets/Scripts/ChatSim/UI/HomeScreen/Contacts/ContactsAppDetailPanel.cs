// ════════════════════════════════════════════════════════════════════════
// Assets/Scripts/UI/HomeScreen/Contacts/ContactsAppDetailPanel.cs
// Note: This panel is for showing character details when a contact is tapped in the Contacts app.
// not implemented yet
// ════════════════════════════════════════════════════════════════════════

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BubbleSpinner.Data;

namespace ChatSim.UI.HomeScreen.Contacts
{
    /// <summary>
    /// Shows full character info when a contact is tapped.
    /// Pulls all data from ConversationAsset via the Get*() helper methods.
    /// Shows "N/A" for missing fields automatically.
    ///
    /// Attach to: ContactsAppDetailPanel GameObject (child of ContactsAppPanel, starts inactive)
    /// 
    /// Connect the logs to GameConfig toggles for easy debugging:
    ///
    /// Hierarchy:
    ///   ContactsAppDetailPanel              ← ATTACH THIS SCRIPT (starts inactive)
    ///   ├── Overlay                         ← Image, black ~50% alpha, Raycast Target ON
    ///   └── DetailCard
    ///       ├── CloseButton
    ///       ├── ProfileImage                ← TODO: wire when Addressables ready
    ///       ├── NameText
    ///       ├── InfoGroup
    ///       │   ├── AgeText
    ///       │   ├── BirthdateText
    ///       │   ├── RelationshipStatusText
    ///       │   ├── OccupationText
    ///       │   ├── BioText
    ///       │   ├── DescriptionText
    ///       │   └── PersonalityTraitsText
    ///       └── ResetButton
    ///           └── Text
    /// </summary>
    public class ContactsAppDetailPanel : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════
        // INSPECTOR REFERENCES
        // ═══════════════════════════════════════════════════════════

        [Header("Header")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Image profileImage;        // TODO: load from Addressables
        [SerializeField] private TextMeshProUGUI nameText;

        [Header("Info Fields")]
        [SerializeField] private TextMeshProUGUI ageText;
        [SerializeField] private TextMeshProUGUI birthdateText;
        [SerializeField] private TextMeshProUGUI relationshipStatusText;
        [SerializeField] private TextMeshProUGUI occupationText;
        [SerializeField] private TextMeshProUGUI bioText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI personalityTraitsText;

        [Header("Actions")]
        [SerializeField] private Button resetButton;

        // ═══════════════════════════════════════════════════════════
        // STATE
        // ═══════════════════════════════════════════════════════════

        private ContactsAppItem _caller;

        // ═══════════════════════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════════════════════

        private void Awake()
        {
            gameObject.SetActive(false);
        }

        private void Start()
        {
            SetupButtons();
        }

        // ═══════════════════════════════════════════════════════════
        // SETUP
        // ═══════════════════════════════════════════════════════════

        private void SetupButtons()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Hide);
            else
                Debug.LogError("[ContactsAppDetailPanel] closeButton not assigned!");

            if (resetButton != null)
                resetButton.onClick.AddListener(OnResetClicked);
            else
                Debug.LogError("[ContactsAppDetailPanel] resetButton not assigned!");
        }

        // ═══════════════════════════════════════════════════════════
        // PUBLIC API
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Show the detail panel for a character.
        /// Called by ContactsAppItem.OnItemClicked().
        /// </summary>
        public void Show(ConversationAsset asset, ContactsAppItem caller)
        {
            if (asset == null)
            {
                Debug.LogError("[ContactsAppDetailPanel] Cannot show — asset is null!");
                return;
            }

            _caller = caller;
            PopulateInfo(asset);
            gameObject.SetActive(true);

            Debug.Log($"[ContactsAppDetailPanel] Showing detail for: {asset.characterName}");
        }

        /// <summary>
        /// Hide the detail panel without taking any action.
        /// </summary>
        public void Hide()
        {
            _caller = null;
            gameObject.SetActive(false);
        }

        // ═══════════════════════════════════════════════════════════
        // POPULATION
        // ═══════════════════════════════════════════════════════════

        private void PopulateInfo(ConversationAsset asset)
        {
            // Name
            if (nameText != null)
                nameText.text = asset.characterName;

            // TODO: Load profile image from Addressables when ready
            // if (profileImage != null && asset.profileImage != null && asset.profileImage.RuntimeKeyIsValid())
            // {
            //     var handle = asset.profileImage.LoadAssetAsync<Sprite>();
            //     handle.Completed += h =>
            //     {
            //         if (profileImage != null && h.Status == AsyncOperationStatus.Succeeded)
            //             profileImage.sprite = h.Result;
            //     };
            // }

            // Info fields — all use ConversationAsset's Get*() helpers for N/A fallback
            if (ageText != null)
                ageText.text = asset.GetAge();

            if (birthdateText != null)
                birthdateText.text = asset.GetBirthdate();

            if (relationshipStatusText != null)
                relationshipStatusText.text = asset.GetRelationshipStatus();

            if (occupationText != null)
                occupationText.text = asset.GetOccupation();

            if (bioText != null)
                bioText.text = asset.GetBio();

            if (descriptionText != null)
                descriptionText.text = asset.GetDescription();

            if (personalityTraitsText != null)
                personalityTraitsText.text = asset.GetPersonalityTraits();
        }

        // ═══════════════════════════════════════════════════════════
        // RESET
        // ═══════════════════════════════════════════════════════════

        private void OnResetClicked()
        {
            if (_caller == null)
            {
                Debug.LogError("[ContactsAppDetailPanel] Reset clicked but caller is null!");
                return;
            }

            // Hide detail panel first, then route through item's reset logic
            // (item handles confirmation dialog if enabled)
            Hide();
            _caller.RequestReset();
        }
    }
}