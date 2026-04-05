// ════════════════════════════════════════════════════════════════════════
// Assets/Scripts/UI/ChatApp/Core/ChatMessageSpawner.cs
// ════════════════════════════════════════════════════════════════════════

using System.Collections.Generic;
using UnityEngine;
using BubbleSpinner.Data;
using ChatSim.UI.ChatApp.Components;
using ChatSim.UI.Common.Components;
using ChatSim.UI.Common.Pooling;
using ChatSim.Core;

namespace ChatSim.UI.ChatApp.Controllers
{
    /// <summary>
    /// Handles message bubble spawning and display.
    /// Pools MessageBubble (text/system) prefabs for performance.
    /// ImageMessageBubble is NOT pooled due to async Addressables loading.
    /// </summary>
    public class ChatMessageSpawner : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════
        // ░ INSPECTOR REFERENCES
        // ═══════════════════════════════════════════════════════════

        [Header("Message Prefabs")]
        [SerializeField] private GameObject systemBubblePrefab;
        [SerializeField] private GameObject npcTextBubblePrefab;
        [SerializeField] private GameObject npcImageBubblePrefab;
        [SerializeField] private GameObject playerTextBubblePrefab;
        [SerializeField] private GameObject playerImageBubblePrefab;

        [Header("Content Container")]
        [SerializeField] private RectTransform chatContent;

        [Header("Pooling")]
        [SerializeField] private PoolingManager poolingManager;
        [SerializeField] private int prewarmCount = 10;

        // ═══════════════════════════════════════════════════════════
        // ░ STATE
        // ═══════════════════════════════════════════════════════════

        // Tracks pooled bubbles (text + system) for recycle on clear
        private List<GameObject> pooledBubbles = new List<GameObject>();

        // Tracks non-pooled image bubbles for destroy on clear
        private List<GameObject> imageBubbles = new List<GameObject>();

        // ═══════════════════════════════════════════════════════════
        // ░ LOGGING
        // ═══════════════════════════════════════════════════════════

        private readonly DebugLogger _log = new DebugLogger(
            "ChatMessageSpawner",
            () => GameBootstrap.Config?.chatMessageSpawnerDebugLogs ?? false
        );

        // ═══════════════════════════════════════════════════════════
        // ░ INITIALIZATION
        // ═══════════════════════════════════════════════════════════

        private void Start()
        {
            PrewarmPools();
        }

        private void PrewarmPools()
        {
            if (poolingManager == null) return;

            if (npcTextBubblePrefab != null)
                poolingManager.PreWarm(npcTextBubblePrefab, prewarmCount);

            if (playerTextBubblePrefab != null)
                poolingManager.PreWarm(playerTextBubblePrefab, prewarmCount / 2);

            if (systemBubblePrefab != null)
                poolingManager.PreWarm(systemBubblePrefab, 3);

            _log.Info("Pools prewarmed");
        }

        // ═══════════════════════════════════════════════════════════
        // ░ EVENT HANDLERS
        // ═══════════════════════════════════════════════════════════

        private void OnEnable()
        {
            GameEvents.OnTextSizeChanged += OnTextSizeChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnTextSizeChanged -= OnTextSizeChanged;
        }

        // ═══════════════════════════════════════════════════════════
        // ░ PUBLIC API
        // ═══════════════════════════════════════════════════════════

        public Transform GetChatContent() => chatContent;

        /// <summary>
        /// Display a message bubble.
        /// Text and system bubbles are pooled.
        /// Image bubbles are instantiated directly (not pooled).
        /// </summary>
        public void DisplayMessage(MessageData msg, bool instant = false)
        {
            if (msg.type == MessageData.MessageType.Image)
            {
                SpawnImageBubble(msg, instant);
            }
            else
            {
                SpawnPooledBubble(msg, instant);
            }
        }

        /// <summary>
        /// Recycles all pooled bubbles and destroys all image bubbles.
        /// </summary>
        public void ClearAllMessages()
        {
            if (chatContent == null)
            {
                _log.Error("chatContent is null!");
                return;
            }

            foreach (var bubble in pooledBubbles)
            {
                if (bubble == null) continue;

                var messageBubble = bubble.GetComponent<TextMessageBubble>();
                messageBubble?.ResetForPool();

                if (poolingManager != null)
                    poolingManager.Recycle(bubble);
                else
                    Destroy(bubble);
            }
            pooledBubbles.Clear();

            foreach (var bubble in imageBubbles)
            {
                if (bubble != null)
                    Destroy(bubble);
            }
            imageBubbles.Clear();

            _log.Info("All messages cleared");
        }

        // ═══════════════════════════════════════════════════════════
        // ░ SPAWNING
        // ═══════════════════════════════════════════════════════════

        private void SpawnPooledBubble(MessageData msg, bool instant)
        {
            GameObject prefab = GetTextBubblePrefab(msg);

            if (prefab == null)
            {
                _log.Error($"No prefab for type: {msg.type}, speaker: {msg.speaker}");
                return;
            }

            GameObject bubbleObj = poolingManager != null
                ? poolingManager.Get(prefab, chatContent, activateOnGet: true)
                : Instantiate(prefab, chatContent);

            var bubble = bubbleObj.GetComponent<TextMessageBubble>();
            if (bubble != null)
            {
                bubble.Initialize(msg, instant);
                bubble.ApplyFontSize(PlayerPrefs.GetFloat(PlayerPrefKeys.TextSize, PlayerPrefKeys.DefaultTextSize));
                pooledBubbles.Add(bubbleObj);
            }
            else
            {
                _log.Error("MessageBubble component missing!");
                Destroy(bubbleObj);
            }
        }

        private void SpawnImageBubble(MessageData msg, bool instant)
        {
            GameObject prefab = msg.IsPlayerMessage
                ? playerImageBubblePrefab
                : npcImageBubblePrefab;

            if (prefab == null)
            {
                _log.Error($"No image prefab for speaker: {msg.speaker}");
                return;
            }

            GameObject bubbleObj = Instantiate(prefab, chatContent);

            var imageBubble = bubbleObj.GetComponent<ImageMessageBubble>();
            if (imageBubble != null)
            {
                imageBubble.Initialize(msg, instant);
                imageBubbles.Add(bubbleObj);
            }
            else
            {
                _log.Error("ImageMessageBubble component missing!");
                Destroy(bubbleObj);
            }
        }

        // ═══════════════════════════════════════════════════════════
        // ░ PREFAB SELECTION
        // ═══════════════════════════════════════════════════════════

        private GameObject GetTextBubblePrefab(MessageData msg)
        {
            switch (msg.type)
            {
                case MessageData.MessageType.System:
                    return systemBubblePrefab;

                case MessageData.MessageType.Text:
                    return msg.IsPlayerMessage
                        ? playerTextBubblePrefab
                        : npcTextBubblePrefab;

                default:
                    return null;
            }
        }

        // ═══════════════════════════════════════════════════════════
        // ░ TEXT SIZE
        // ═══════════════════════════════════════════════════════════

        private void OnTextSizeChanged(float fontSize)
        {
            ApplyFontSizeToAllBubbles(fontSize);
        }

        private void ApplyFontSizeToAllBubbles(float fontSize)
        {
            foreach (var bubble in pooledBubbles)
            {
                if (bubble == null) continue;
                bubble.GetComponent<TextMessageBubble>()?.ApplyFontSize(fontSize);
            }

            _log.Info($"Font size applied to {pooledBubbles.Count} bubbles: {fontSize}");
        }

        // ═══════════════════════════════════════════════════════════
        // ░ VALIDATION
        // ═══════════════════════════════════════════════════════════

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (systemBubblePrefab == null)
                Debug.LogWarning("[ChatMessageSpawner] systemBubblePrefab not assigned!");

            if (npcTextBubblePrefab == null)
                Debug.LogWarning("[ChatMessageSpawner] npcTextBubblePrefab not assigned!");

            if (npcImageBubblePrefab == null)
                Debug.LogWarning("[ChatMessageSpawner] npcImageBubblePrefab not assigned!");

            if (playerTextBubblePrefab == null)
                Debug.LogWarning("[ChatMessageSpawner] playerTextBubblePrefab not assigned!");

            if (playerImageBubblePrefab == null)
                Debug.LogWarning("[ChatMessageSpawner] playerImageBubblePrefab not assigned!");

            if (chatContent == null)
                Debug.LogError("[ChatMessageSpawner] chatContent not assigned!");

            if (poolingManager == null)
                Debug.LogWarning("[ChatMessageSpawner] poolingManager not assigned - will fall back to Instantiate!");
        }
#endif
    }
}