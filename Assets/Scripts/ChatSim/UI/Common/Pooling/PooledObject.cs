// ════════════════════════════════════════════════════════════════════════
// Assets/Scripts/ChatSim/UI/Common/Pooling/PooledObject.cs
// ════════════════════════════════════════════════════════════════════════

using UnityEngine;

namespace ChatSim.UI.Common.Pooling
{
    /// <summary>
    /// Component added to pooled objects to track their source prefab.
    /// Add this to prefabs that should preserve their content when recycled.
    /// </summary>
    public class PooledObject : MonoBehaviour
    {
        public GameObject Prefab { get; private set; }
        
        [Tooltip("If true, content won't be cleared when recycled (for typing indicators, etc.)")]
        public bool PreserveContent = false;
        
        public void SetPrefab(GameObject prefab)
        {
            Prefab = prefab;
        }
    }
}