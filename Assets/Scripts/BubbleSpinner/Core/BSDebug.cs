// ════════════════════════════════════════════════════════════════════════
// Assets/Scripts/BubbleSpinner/Core/BSDebug.cs
// ════════════════════════════════════════════════════════════════════════

using UnityEngine;

namespace BubbleSpinner.Core
{
    public static class BSDebug
    {
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void Info(string message) => Debug.Log(message);

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void Warn(string message) => Debug.LogWarning(message);

        public static void Error(string message)   => Debug.LogError(message);
    }
}