// ════════════════════════════════════════════════════════════════════════
// Assets/Scripts/Data/SaveData.cs
// Phone Chat Simulation Game - Save Data Structure
// ════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using BubbleSpinner.Data;

namespace ChatSim.Data
{
    /// <summary>
    /// Main save data structure - contains all persistent game state
    /// </summary>
    [Serializable]
    public class SaveData
    {
        // ════════════════════════════════════════════════════════════════
        // METADATA
        // ════════════════════════════════════════════════════════════════
        
        public int saveVersion = 1;
        
        // ════════════════════════════════════════════════════════════════
        // CONVERSATION STATES (BubbleSpinner)
        // ════════════════════════════════════════════════════════════════
        
        public List<ConversationState> conversationStates = new List<ConversationState>();
        
        // ════════════════════════════════════════════════════════════════
        // FUTURE: Add additional game state as needed
        // ════════════════════════════════════════════════════════════════
        
        // public int currentChapter = 1;
        // public bool isPhoneLocked = true;
        // public List<string> storyFlags = new List<string>();
        // public Dictionary<string, bool> unlockedApps = new Dictionary<string, bool>();
    }
}