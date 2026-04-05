// ════════════════════════════════════════════════════════════════════════
// Assets/Scripts/Core/SceneNames.cs
// ════════════════════════════════════════════════════════════════════════

namespace ChatSim.Core
{
    /// <summary>
    /// Scene name constants - must match Unity scene file names exactly
    /// </summary>
    public static class SceneNames
    {
        // ════════════════════════════════════════════════════════════════
        // CORE SCENES
        // ════════════════════════════════════════════════════════════════
        
        public const string DISCLAIMER = "00_Disclaimer";
        public const string BOOTSTRAP = "01_Bootstrap";
        public const string LOCKSCREEN = "02_LockScreen";
        public const string PHONE_SCREEN = "03_PhoneScreen";
        public const string CHAT_APP = "04_ChatApp";
        
        // ════════════════════════════════════════════════════════════════
        // HELPER METHODS
        // ════════════════════════════════════════════════════════════════
        
        /// <summary>
        /// Check if scene name is valid
        /// </summary>
        public static bool IsValidScene(string sceneName)
        {
            return sceneName == DISCLAIMER
                || sceneName == BOOTSTRAP
                || sceneName == LOCKSCREEN
                || sceneName == PHONE_SCREEN
                || sceneName == CHAT_APP;
        }
        
        /// <summary>
        /// Get display name for scene
        /// </summary>
        public static string GetDisplayName(string sceneName)
        {
            switch (sceneName)
            {
                case DISCLAIMER: return "Disclaimer";
                case BOOTSTRAP: return "Bootstrap";
                case LOCKSCREEN: return "LOCKSCREEN";
                case PHONE_SCREEN: return "Phone Screen";
                case CHAT_APP: return "Chat App";
                default: return sceneName;
            }
        }
    }
}