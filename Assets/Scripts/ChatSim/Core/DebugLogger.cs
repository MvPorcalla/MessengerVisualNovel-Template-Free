// ════════════════════════════════════════════════════════════════════════
// Assets/Scripts/ChatSim/Core/DebugLogger.cs
// ════════════════════════════════════════════════════════════════════════

using System;
using UnityEngine;

namespace ChatSim.Core
{
    /// <summary>
    /// Per-class logging helper. Eliminates repeated logging boilerplate
    /// across all ChatSim systems.
    ///
    /// Each class declares one instance. The toggle delegate reads the live
    /// GameConfig value — Inspector changes take effect immediately in play mode.
    ///
    /// Usage:
    ///
    ///   private readonly DebugLogger _log = new DebugLogger(
    ///       "ChatAutoScroller",
    ///       () => GameBootstrap.Config?.chatAutoScrollerDebugLogs ?? false
    ///   );
    ///
    ///   _log.Info("Scrolled to bottom.");
    ///   _log.Warn("Scroll rect missing.");
    ///   _log.Error("Null reference in scroller.");
    ///
    /// Notes:
    ///   - Info and Warn are stripped from release builds via [Conditional].
    ///   - Error always fires in all builds, with no toggle.
    /// </summary>
    public sealed class DebugLogger
    {
        // ════════════════════════════════════════════════════════════════
        // FIELDS
        // ════════════════════════════════════════════════════════════════

        private readonly string     _tag;
        private readonly Func<bool> _isEnabled;

        // ════════════════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ════════════════════════════════════════════════════════════════

        /// <param name="tag">Class name shown in brackets — e.g. "ChatAutoScroller"</param>
        /// <param name="isEnabled">Delegate that reads the live GameConfig toggle</param>
        public DebugLogger(string tag, Func<bool> isEnabled)
        {
            _tag       = tag;
            _isEnabled = isEnabled;
        }

        // ════════════════════════════════════════════════════════════════
        // PUBLIC API
        // ════════════════════════════════════════════════════════════════

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public void Info(string message)
        {
            if (!_isEnabled()) return;
            Debug.Log($"[{_tag}] {message}");
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public void Warn(string message)
        {
            if (!_isEnabled()) return;
            Debug.LogWarning($"[{_tag}] WARNING: {message}");
        }

        public void Error(string message)
        {
            // Always fires — no Conditional, no toggle, all builds
            Debug.LogError($"[{_tag}] ERROR: {message}");
        }
    }
}