// ════════════════════════════════════════════════════════════════════════
// Assets/Scripts/UI/ChatApp/Panels/ContactListPanel.cs
// ════════════════════════════════════════════════════════════════════════

using System.Collections.Generic;
using UnityEngine;
using BubbleSpinner.Data;
using ChatSim.UI.ChatApp.Controllers;
using ChatSim.Core;

namespace ChatSim.UI.ChatApp.Panels
{
    /// <summary>
    /// Manages the contact list UI and populates character buttons
    /// Attach to: ContactListPanel GameObject
    /// </summary>
    public class ContactListPanel : MonoBehaviour
    {
        #region Inspector References
        
        [Header("Database")]
        [SerializeField] private CharacterDatabase characterDatabase;
        
        [Header("UI References")]
        [SerializeField] private Transform contactContainer;
        [SerializeField] private GameObject ContactListItemPrefab;
        
        [Header("Controller Reference")]
        [SerializeField] private ChatAppController chatController;
        
        #endregion

        #region Logging
        private readonly DebugLogger _log = new DebugLogger(
            "ContactListPanel",
            () => GameBootstrap.Config?.contactChatListDebugLogs ?? false
        );
        #endregion
        
        #region Unity Lifecycle
        
        private void OnEnable()
        {
            PopulateContactList();
        }
        
        #endregion
        
        #region Contact List Population
        
        private void PopulateContactList()
        {
            ClearContactList();

            if (characterDatabase == null)
            {
                _log.Error("CharacterDatabase is not assigned!");
                return;
            }

            var conversations = characterDatabase.GetAllCharacters();

            if (conversations == null || conversations.Count == 0)
            {
                _log.Warn("No conversations found in database!");
                return;
            }

            foreach (var conversation in conversations)
            {
                if (conversation == null)
                {
                    _log.Warn("Null conversation in database, skipping");
                    continue;
                }

                CreateContactButton(conversation);
            }

            _log.Info($"Populated {conversations.Count} contacts from database");
        }
        
        private void CreateContactButton(ConversationAsset conversation)
        {
            GameObject buttonObj = Instantiate(ContactListItemPrefab, contactContainer);
            var contactItem = buttonObj.GetComponent<ContactListItem>();

            if (contactItem != null)
            {
                string lastMessage = GetLastMessagePreview(conversation.ConversationId);
                contactItem.Initialize(conversation, chatController, lastMessage);
            }
            else
            {
                _log.Error("ContactListItem component missing on prefab!");
            }
        }

        private string GetLastMessagePreview(string conversationId)
        {
            var saveData = GameBootstrap.Save?.GetOrCreateSaveData();
            var state = saveData?.conversationStates
                .Find(s => s.conversationId == conversationId);

            if (state == null || state.messageHistory == null || state.messageHistory.Count == 0)
                return "";

            for (int i = state.messageHistory.Count - 1; i >= 0; i--)
            {
                var msg = state.messageHistory[i];

                if (msg.IsSystemMessage)
                    continue;

                if (msg.type == MessageData.MessageType.Image)
                    return msg.IsPlayerMessage ? "You sent an image." : "Sent an image.";

                if (msg.type == MessageData.MessageType.Text)
                    return msg.content;
            }

            return "";
        }
        
        private void ClearContactList()
        {
            foreach (Transform child in contactContainer)
            {
                Destroy(child.gameObject);
            }
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Refresh the contact list (call after adding/removing conversations)
        /// </summary>
        public void RefreshContactList()
        {
            PopulateContactList();
        }
        
        #endregion
    }
}