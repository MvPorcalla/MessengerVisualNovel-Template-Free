// ════════════════════════════════════════════════════════════════════════
// Assets/Scripts/ChatSim/Data/GameConfig.cs
// ════════════════════════════════════════════════════════════════════════

using UnityEngine;

namespace ChatSim.Data
{
    // ════════════════════════════════════════════════════════════════
    // ENUMS
    // ════════════════════════════════════════════════════════════════

    public enum TimeFormat
    {
        HH_mm,          // 14:30  (24-hour)
        h_mm_tt,        // 2:30 PM (12-hour)
        HH_mm_ss,       // 14:30:00 (24-hour with seconds)
        h_mm_ss_tt,     // 2:30:00 PM (12-hour with seconds)
    }

    public enum DateFormat
    {
        dddd_MMMM_dd,   // Monday, January 01
        MMMM_dd_yyyy,   // January 01 2025
        dd_MMMM_yyyy,   // 01 January 2025
        MM_dd_yyyy,     // 01/01/2025
        yyyy_MM_dd,     // 2025-01-01
    }

    /// <summary>
    /// Central ScriptableObject config for all game settings.
    /// Create via: Right-click → Create → ChatSim → Game Config
    ///
    /// Sections:
    ///   - Save Manager
    ///   - Lock Screen
    ///   - Debug
    ///   (add more sections here as the game grows)
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "ChatSim/Game Config")]
    public class GameConfig : ScriptableObject
    {
        // ════════════════════════════════════════════════════════════════
        // LOCK SCREEN
        // ════════════════════════════════════════════════════════════════

        [Header("── Lock Screen Configuration ──────────────────────────")]

        [Header("Swipe")]
        [Tooltip("Minimum swipe distance in pixels to trigger unlock")]
        public float swipeThreshold = 300f;

        [Tooltip("How many pixels of upward swipe = fully faded out")]
        public float fadeSwipeRange = 400f;

        [Header("Notifications")]
        [Tooltip("Max individual notifications shown on lock screen. 0 = disabled.")]
        public int maxIndividualNotifications = 3;

        [Header("Time & Date")]
        public TimeFormat timeFormat = TimeFormat.HH_mm;
        public DateFormat dateFormat = DateFormat.dddd_MMMM_dd;

        // ════════════════════════════════════════════════════════════════════════
        // GALLERY FULLSCREEN VIEWER
        // ════════════════════════════════════════════════════════════════════════

        [Header("── Gallery Fullscreen Viewer ───────────────")]

        [Header("Zoom")]
        [Tooltip("Minimum zoom level (1 = no zoom)")]
        public float galleryMinZoom = 1f;

        [Tooltip("Maximum zoom level")]
        public float galleryMaxZoom = 3f;

        [Tooltip("Pinch zoom sensitivity")]
        public float galleryZoomSpeed = 0.001f;

        [Tooltip("Zoom level when double-tapping")]
        public float galleryDoubleTapZoom = 2f;

        [Tooltip("Maximum time between taps to register as double-tap")]
        public float galleryDoubleTapTime = 0.3f;

        [Header("Animation")]
        [Tooltip("Fade in/out duration in seconds")]
        public float galleryFadeDuration = 0.3f;

        // ════════════════════════════════════════════════════════════════════════
        // CHAT APP
        // ════════════════════════════════════════════════════════════════════════

        [Header("── Chat App Configuration ─────────────────────────")]

        [Header("Auto Scroll Settings")]
        [Tooltip("Normalized scroll position (0–1) considered 'at bottom'. Default 0.01 = within 1% of bottom.")]
        public float bottomThreshold = 0.01f;

        [Header("Timing Settings")]

        [Header("Normal Mode")]
        [Tooltip("Delay in seconds between NPC messages")]
        public float messageDelay = 1.2f;

        [Tooltip("How long the typing indicator shows before a message appears")]
        public float typingIndicatorDuration = 1.5f;

        [Tooltip("Delay in seconds between player messages")]
        public float playerMessageDelay = 0.3f;

        [Tooltip("Final pause before choices or continue button appears")]
        public float finalDelayBeforeChoices = 0.2f;

        [Header("Fast Mode")]
        [Tooltip("Delay used for all message types when fast mode is active")]
        public float fastModeSpeed = 0.1f;

        [Header("Message Bubbles")]

        [Header("Auto Resize Text")]
        [Tooltip("Maximum width of a message bubble in pixels")]
        public float bubbleMaxWidth = 650f;

        [Tooltip("Minimum width of a message bubble in pixels")]
        public float bubbleMinWidth = 40f;

        [Tooltip("Minimum width change required to trigger a layout rebuild")]
        public float bubbleWidthChangeThreshold = 0.1f;

        [Header("Chat CG Fullscreen Viewer")]

        [Header("Zoom")]
        [Tooltip("Minimum zoom level (1 = no zoom)")]
        public float minZoom = 1f;

        [Tooltip("Maximum zoom level")]
        public float maxZoom = 3f;

        [Tooltip("Pinch zoom sensitivity")]
        public float zoomSpeed = 0.001f;

        [Header("Animation")]
        [Tooltip("Fade in/out duration in seconds")]
        public float cgViewerFadeDuration = 0.3f;

        // ════════════════════════════════════════════════════════════════
        // DEBUG
        // ════════════════════════════════════════════════════════════════

        [Header("── Debug ────────────────────────────────")]

        [Header("Core Systems")]
        public bool bootstrapDebugLogs = false;
        public bool saveManagerDebugLogs = false;

        [Header("UI Scene")]
        public bool lockScreenDebugLogs = false;

        [Header("Chat App")]
        public bool chatAppDebugLogs = false;
        public bool chatAutoScrollerDebugLogs = false;
        public bool chatChoiceSpawnerDebugLogs = false;
        public bool chatMessageSpawnerDebugLogs = false;
        public bool chatTimingControllerDebugLogs = false;
        public bool imageMessageBubbleDebugLogs = false;
        public bool contactChatListDebugLogs = false;
        public bool chatAppNavButtonsDebugLogs = false;

        [Header("Pooling")]
        public bool poolingManagerDebugLogs = false;

        [Header("Phone Screen")]
        public bool contactsAppDebugLogs = false;
        public bool galleryAppDebugLogs = false;
        public bool settingsPanelDebugLogs = false;
        public bool homeScreenDebugLogs = false;

        // ════════════════════════════════════════════════════════════════
        // HELPERS — converts enum to C# DateTime format string
        // ════════════════════════════════════════════════════════════════

        public string GetTimeFormatString()
        {
            switch (timeFormat)
            {
                case TimeFormat.HH_mm:      return "HH:mm";
                case TimeFormat.h_mm_tt:    return "h:mm tt";
                case TimeFormat.HH_mm_ss:   return "HH:mm:ss";
                case TimeFormat.h_mm_ss_tt: return "h:mm:ss tt";
                default:                    return "HH:mm";
            }
        }

        public string GetDateFormatString()
        {
            switch (dateFormat)
            {
                case DateFormat.dddd_MMMM_dd: return "dddd, MMMM dd";
                case DateFormat.MMMM_dd_yyyy: return "MMMM dd yyyy";
                case DateFormat.dd_MMMM_yyyy: return "dd MMMM yyyy";
                case DateFormat.MM_dd_yyyy:   return "MM/dd/yyyy";
                case DateFormat.yyyy_MM_dd:   return "yyyy-MM-dd";
                default:                      return "dddd, MMMM dd";
            }
        }
    }
}