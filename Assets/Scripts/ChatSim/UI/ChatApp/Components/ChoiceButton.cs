// ════════════════════════════════════════════════════════════════════════
// Assets/Scripts/UI/ChatApp/Components/ChoiceButton.cs
// ════════════════════════════════════════════════════════════════════════

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ChatSim.UI.ChatApp.Components
{
    /// <summary>
    /// Attached to each choice button prefab
    /// </summary>
    public class ChoiceButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TextMeshProUGUI buttonText;
        
        public void Initialize(string text, Action onClick)
        {
            if (buttonText != null)
                buttonText.text = text;

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => onClick?.Invoke());
            }
        }

        // Called by ChatChoiceSpawner when applying text size changes from settings
        public void ApplyFontSize(float fontSize)
        {
            if (buttonText != null)
                buttonText.fontSize = fontSize;
        }

        /// <summary>
        /// Called by ChatChoiceSpawner before returning this button to the pool.
        /// </summary>
        public void ResetForPool()
        {
            if (buttonText != null)
                buttonText.text = string.Empty;

            if (button != null)
                button.onClick.RemoveAllListeners();
        }
    }
}