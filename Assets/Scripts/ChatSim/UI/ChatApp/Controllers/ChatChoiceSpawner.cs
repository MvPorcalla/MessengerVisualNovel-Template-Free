// ════════════════════════════════════════════════════════════════════════
// Assets/Scripts/UI/ChatApp/Core/ChatChoiceSpawner.cs
// ════════════════════════════════════════════════════════════════════════

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BubbleSpinner.Data;
using ChatSim.UI.ChatApp.Components;
using ChatSim.UI.Common.Components;
using ChatSim.UI.Common.Pooling;
using ChatSim.Core;

namespace ChatSim.UI.ChatApp.Controllers
{
    /// <summary>
    /// Handles spawning and displaying choice buttons.
    /// Choice buttons are pooled for performance.
    /// </summary>
    public class ChatChoiceSpawner : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════
        // ░ INSPECTOR REFERENCES
        // ═══════════════════════════════════════════════════════════

        [Header("Prefabs")]
        [SerializeField] private GameObject choiceButtonPrefab;

        [Header("Optional: Separate Continue Button Styling")]
        [Tooltip("Leave empty to use choiceButtonPrefab for continue/end buttons")]
        [SerializeField] private GameObject continueButtonPrefab;

        [Header("Container")]
        [SerializeField] private RectTransform choiceContainer;

        [Header("Pooling")]
        [SerializeField] private PoolingManager poolingManager;
        [SerializeField] private int prewarmCount = 4;

        [Header("Auto Scroll")]
        [SerializeField] private ChatAutoScroller autoScroller;

        // ═══════════════════════════════════════════════════════════
        // ░ STATE
        // ═══════════════════════════════════════════════════════════

        private Action<ChoiceData> onChoiceSelected;
        private List<GameObject> activeButtons = new List<GameObject>();
        private Coroutine rebuildLayoutCoroutine;

        // ═══════════════════════════════════════════════════════════
        // ░ LOGGING
        // ═══════════════════════════════════════════════════════════

        private readonly DebugLogger _log = new DebugLogger(
            "ChatChoiceSpawner",
            () => GameBootstrap.Config?.chatChoiceSpawnerDebugLogs ?? false
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

            if (choiceButtonPrefab != null)
                poolingManager.PreWarm(choiceButtonPrefab, prewarmCount);

            if (continueButtonPrefab != null)
                poolingManager.PreWarm(continueButtonPrefab, 1);

            _log.Info("Pools prewarmed");
        }

        // ═══════════════════════════════════════════════════════════
        // ░ PUBLIC API
        // ═══════════════════════════════════════════════════════════

        public void DisplayChoices(List<ChoiceData> choices, Action<ChoiceData> callback)
        {
            ClearChoices();

            onChoiceSelected = callback;

            foreach (var choice in choices)
                SpawnChoiceButton(choice);

            gameObject.SetActive(true);
            rebuildLayoutCoroutine = StartCoroutine(RebuildLayoutDelayed());
        }

        public void ShowContinueButton(Action callback)
        {
            ClearChoices();
            SpawnUtilityButton("...", callback);
            gameObject.SetActive(true);
            rebuildLayoutCoroutine = StartCoroutine(RebuildLayoutDelayed());

        }

        public void ShowEndButton(string buttonText, Action callback)
        {
            ClearChoices();
            SpawnUtilityButton(buttonText, callback);
            gameObject.SetActive(true);
            rebuildLayoutCoroutine = StartCoroutine(RebuildLayoutDelayed());
            _log.Info($"Showing end button: {buttonText}");
        }

        public void ClearChoices()
        {
            if (rebuildLayoutCoroutine != null)
            {
                StopCoroutine(rebuildLayoutCoroutine);
                rebuildLayoutCoroutine = null;
            }

            if (choiceContainer == null)
            {
                _log.Error("choiceContainer is null!");
                return;
            }

            foreach (var btn in activeButtons)
            {
                if (btn == null) continue;

                var choiceButton = btn.GetComponent<ChoiceButton>();
                choiceButton?.ResetForPool();

                if (poolingManager != null)
                    poolingManager.Recycle(btn);
                else
                    Destroy(btn);
            }

            activeButtons.Clear();
            gameObject.SetActive(false);
        }

        // ═══════════════════════════════════════════════════════════
        // ░ SPAWNING
        // ═══════════════════════════════════════════════════════════

        private void SpawnChoiceButton(ChoiceData choice)
        {
            if (choiceButtonPrefab == null)
            {
                _log.Error("choiceButtonPrefab is null!");
                return;
            }

            GameObject btnObj = poolingManager != null
                ? poolingManager.Get(choiceButtonPrefab, choiceContainer, activateOnGet: true)
                : Instantiate(choiceButtonPrefab, choiceContainer);

            var button = btnObj.GetComponent<ChoiceButton>();
            if (button != null)
            {
                button.Initialize(choice.choiceText, () => OnChoiceClicked(choice));
                activeButtons.Add(btnObj);
            }
            else
            {
                _log.Error("ChoiceButton component missing!");
                Destroy(btnObj);
            }
        }

        private void SpawnUtilityButton(string text, Action callback)
        {
            GameObject prefab = continueButtonPrefab != null
                ? continueButtonPrefab
                : choiceButtonPrefab;

            if (prefab == null)
            {
                _log.Error("No button prefab assigned!");
                return;
            }

            GameObject btnObj = poolingManager != null
                ? poolingManager.Get(prefab, choiceContainer, activateOnGet: true)
                : Instantiate(prefab, choiceContainer);

            var button = btnObj.GetComponent<ChoiceButton>();
            if (button != null)
            {
                button.Initialize(text, callback);
                activeButtons.Add(btnObj);
            }
            else
            {
                _log.Error("ChoiceButton component missing!");
                Destroy(btnObj);
            }
        }

        private void OnChoiceClicked(ChoiceData choice)
        {
            onChoiceSelected?.Invoke(choice);
        }

        // ═══════════════════════════════════════════════════════════
        // ░ LAYOUT
        // ═══════════════════════════════════════════════════════════

        private IEnumerator RebuildLayoutDelayed()
        {
            yield return new WaitForEndOfFrame();

            if (choiceContainer != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(choiceContainer);

            yield return new WaitForEndOfFrame();

            if (autoScroller != null)
                autoScroller.ForceScrollToBottom();

            rebuildLayoutCoroutine = null;
        }
    }
}