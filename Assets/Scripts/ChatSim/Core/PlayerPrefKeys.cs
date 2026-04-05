// ════════════════════════════════════════════════════════════════════════
// Assets/Scripts/ChatSim/Core/PlayerPrefKeys.cs
// ════════════════════════════════════════════════════════════════════════

namespace ChatSim.Core
{
    /// <summary>
    /// Shared PlayerPrefs key constants and default values.
    /// Always reference these instead of raw strings.
    /// Add new keys here whenever a new PlayerPrefs entry is introduced.
    ///
    /// Usage summary:
    ///   DisclaimerAccepted        → DisclaimerScreen.cs       — tracks if player has accepted TOS
    ///   FastMode                  → SettingsPanel.cs          — message speed toggle (Normal / Fast)
    ///                             → ChatAppController.cs      — loads fast mode on chat scene init
    ///   TextSize                  → SettingsPanel.cs          — bubble font size selection
    ///                             → ChatMessageSpawner.cs     — applies saved size on bubble spawn
    /// </summary>
    public static class PlayerPrefKeys
    {
        // ═══════════════════════════════════════════════════════════
        // ░ DISCLAIMER
        // ░ Used by: DisclaimerScreen.cs
        // ═══════════════════════════════════════════════════════════

        public const string DisclaimerAccepted        = "HasSeenDisclaimer";
        public const int    DefaultDisclaimerAccepted = 0;

        // ═══════════════════════════════════════════════════════════
        // ░ SETTINGS — MESSAGE SPEED
        // ░ Used by: SettingsPanel.cs, ChatAppController.cs
        // ═══════════════════════════════════════════════════════════

        public const string FastMode        = "ChatFastMode";
        public const int    DefaultFastMode = 0;

        // ═══════════════════════════════════════════════════════════
        // ░ SETTINGS — TEXT SIZE
        // ░ Used by: SettingsPanel.cs, ChatMessageSpawner.cs
        // ═══════════════════════════════════════════════════════════

        public const string TextSize        = "ChatTextSize";
        public const float  DefaultTextSize = 48f;
    }
}