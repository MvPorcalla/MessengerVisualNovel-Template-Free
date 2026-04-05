// ════════════════════════════════════════════════════════════════════════
// Assets/Scripts/ChatSim/Core/AddressablesImageLoader.cs
// ════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace ChatSim.Core
{
    /// <summary>
    /// Utility class for loading sprites from Addressables with caching and error handling.
    /// This class provides an asynchronous API to load sprites by their addressable keys,
    /// </summary>
    public static class AddressablesImageLoader
    {
        // ═══════════════════════════════════════════════════════════
        // ░ CACHE
        // ═══════════════════════════════════════════════════════════
        
        private static Dictionary<string, Sprite> cachedSprites = new Dictionary<string, Sprite>();
        private static Dictionary<string, AsyncOperationHandle<Sprite>> ongoingLoads = new Dictionary<string, AsyncOperationHandle<Sprite>>();
        
        // ═══════════════════════════════════════════════════════════
        // ░ PUBLIC API
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>
        /// Asynchronously load a sprite by its addressable key. If the sprite is already cached, the callback is invoked immediately.
        /// If the sprite is currently being loaded, the callback will be added to the existing load operation.
        /// Otherwise, a new load operation will be started. On failure, an error message is passed to the onFailed callback.
        /// </summary>
        public static void LoadSpriteAsync(
            string addressableKey, 
            Action<Sprite> onLoaded, 
            Action<string> onFailed = null)
        {
            if (string.IsNullOrEmpty(addressableKey))
            {
                Debug.LogError("[AddressablesImageLoader] Empty addressable key!");
                onFailed?.Invoke("Empty addressable key");
                return;
            }
            
            if (cachedSprites.TryGetValue(addressableKey, out Sprite cached))
            {
                onLoaded?.Invoke(cached);
                return;
            }
            
            if (ongoingLoads.ContainsKey(addressableKey))
            {
                var existingHandle = ongoingLoads[addressableKey];
                existingHandle.Completed += (op) => HandleLoadComplete(op, addressableKey, onLoaded, onFailed);
                return;
            }
            
            var handle = Addressables.LoadAssetAsync<Sprite>(addressableKey);
            ongoingLoads[addressableKey] = handle;
            
            handle.Completed += (op) => HandleLoadComplete(op, addressableKey, onLoaded, onFailed);
        }
        
        /// <summary>
        /// Preload a sprite into the cache without needing it immediately. 
        /// Useful for warming up assets before they are needed.
        /// </summary>
        public static void PreloadSprite(string addressableKey)
        {
            LoadSpriteAsync(addressableKey, null, null);
        }
        
        /// <summary>
        /// Check if a sprite is already cached for the given addressable key. 
        /// This does not check ongoing loads.
        /// </summary>
        public static bool IsCached(string addressableKey)
        {
            return cachedSprites.ContainsKey(addressableKey);
        }
        
        /// <summary>
        /// Clear the entire sprite cache and release any ongoing load operations. 
        /// Use this if you need to free up memory or reset state.
        /// </summary>
        public static void ClearCache()
        {
            Debug.Log($"[AddressablesImageLoader] Clearing cache ({cachedSprites.Count} sprites)");
            
            foreach (var kvp in ongoingLoads)
            {
                if (kvp.Value.IsValid())
                {
                    Addressables.Release(kvp.Value);
                }
            }
            
            cachedSprites.Clear();
            ongoingLoads.Clear();
        }
        
        // ═══════════════════════════════════════════════════════════
        // ░ LOAD COMPLETION HANDLER
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>
        /// Handle the completion of a sprite load operation. On success, cache the sprite and invoke the onLoaded callback. 
        /// On failure, log the error and invoke the onFailed callback with an error message.
        /// </summary>
        /// <param name="operation">The async operation handle for the sprite load.</param>
        /// <param name="addressableKey">The addressable key used to identify the sprite.</param>
        /// <param name="onLoaded">Callback invoked with the loaded sprite on success.</param>
        /// <param name="onFailed">Callback invoked with an error message on failure.</param>
        private static void HandleLoadComplete(
            AsyncOperationHandle<Sprite> operation, 
            string addressableKey,
            Action<Sprite> onLoaded,
            Action<string> onFailed)
        {
            ongoingLoads.Remove(addressableKey);
            
            if (operation.Status == AsyncOperationStatus.Succeeded)
            {
                var sprite = operation.Result;
                
                if (sprite != null)
                {
                    cachedSprites[addressableKey] = sprite;
                    onLoaded?.Invoke(sprite);
                }
                else
                {
                    string error = $"Sprite is null: {addressableKey}";
                    Debug.LogError($"[AddressablesImageLoader] ✗ {error}");
                    onFailed?.Invoke(error);
                }
            }
            else
            {
                string error = operation.OperationException?.Message ?? "Unknown error";
                Debug.LogError($"[AddressablesImageLoader] ✗ Failed to load '{addressableKey}': {error}");
                onFailed?.Invoke(error);
            }
        }
        
        // ═══════════════════════════════════════════════════════════
        // ░ EDITOR TOOLS
        // ═══════════════════════════════════════════════════════════
        
        #if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            cachedSprites.Clear();
            ongoingLoads.Clear();
        }
        #endif
    }
}