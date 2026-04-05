// ════════════════════════════════════════════════════════════════════════
// Assets/Scripts/Core/GameBootstrap.cs
// ════════════════════════════════════════════════════════════════════════

using System;
using System.Collections;
using UnityEngine;
using ChatSim.Data;
using BubbleSpinner.Core;

namespace ChatSim.Core
{
    /// <summary>
    /// Central bootstrapper for the game that initializes core systems, manages scene flow, and integrates BubbleSpinner.
    /// This class is responsible for setting up the game's core managers (SaveManager, SceneFlowManager, ConversationManager), 
    /// ensuring save data exists, and determining the initial scene to load.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        // ════════════════════════════════════════════════════════════════
        // SINGLETON
        // ════════════════════════════════════════════════════════════════

        public static GameBootstrap Instance { get; private set; }

        // ════════════════════════════════════════════════════════════════
        // INSPECTOR REFERENCES
        // ════════════════════════════════════════════════════════════════

        [Header("Core Managers")]
        [SerializeField] private SaveManager saveManager;
        [SerializeField] private SceneFlowManager sceneFlowManager;

        [Header("Game Systems")]
        [SerializeField] private ConversationManager conversationManager;

        [Header("Config")]
        [SerializeField] private GameConfig gameConfig;

        // ════════════════════════════════════════════════════════════════
        // PUBLIC STATIC ACCESSORS
        // ════════════════════════════════════════════════════════════════

        public static SaveManager Save { get; private set; }
        public static SceneFlowManager SceneFlow { get; private set; }
        public static ConversationManager Conversation { get; private set; }
        public static GameConfig Config { get; private set; }

        // ════════════════════════════════════════════════════════════════
        // STATE
        // ════════════════════════════════════════════════════════════════

        private bool _isInitialized = false;
        public bool IsInitialized => _isInitialized;

        private BubbleSpinnerBridge _bubbleSpinnerBridge;

        // ════════════════════════════════════════════════════════════════════════
        // LOGGING
        // ════════════════════════════════════════════════════════════════════════

        private readonly DebugLogger _log = new DebugLogger(
            "GameBootstrap",
            () => GameBootstrap.Config?.bootstrapDebugLogs ?? false
        );

        // ════════════════════════════════════════════════════════════════
        // UNITY LIFECYCLE
        // ════════════════════════════════════════════════════════════════

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            GameEvents.ClearAllEvents();

            _log.Info("GameBootstrap created - will persist across scenes");
        }

        private IEnumerator Start()
        {
            _log.Info("=== BOOTSTRAP INITIALIZATION START ===");

            try
            {
                ValidateManagerReferences();
                AssignStaticReferences();
                InitializeManagers();

                _isInitialized = true;
                _log.Info("All systems initialized");
            }
            catch (Exception e)
            {
                _log.Error($"FATAL: Bootstrap initialization failed!\n{e}");
                QuitApplication();
                yield break;
            }

            string nextScene = DetermineNextScene();
            _log.Info($"Loading next scene: {nextScene}");

            bool sceneLoadComplete = false;
            System.Action<string> onSceneLoaded = null;

            onSceneLoaded = (sceneName) =>
            {
                if (sceneName == nextScene)
                {
                    sceneLoadComplete = true;
                    GameEvents.OnSceneLoaded -= onSceneLoaded;
                }
            };

            GameEvents.OnSceneLoaded += onSceneLoaded;
            SceneFlow.LoadScene(nextScene);

            yield return new WaitUntil(() => sceneLoadComplete);

            _log.Info("=== BOOTSTRAP INITIALIZATION COMPLETE ===");
        }

        // ════════════════════════════════════════════════════════════════
        // MANAGER SETUP
        // ════════════════════════════════════════════════════════════════

        private void ValidateManagerReferences()
        {
            if (saveManager == null)
                throw new InvalidOperationException("SaveManager not assigned in Inspector!");

            if (sceneFlowManager == null)
                throw new InvalidOperationException("SceneFlowManager not assigned in Inspector!");

            if (conversationManager == null)
                throw new InvalidOperationException("ConversationManager not assigned in Inspector!");
        }

        private void AssignStaticReferences()
        {
            Save = saveManager;
            SceneFlow = sceneFlowManager;
            Conversation = conversationManager;
            Config = gameConfig;

            if (Config == null)
                UnityEngine.Debug.LogWarning("[GameBootstrap] GameConfig not assigned — all systems will use hardcoded fallback values.");
        }

        private void InitializeManagers()
        {

            if (_isInitialized)
            {
                Debug.LogWarning("[GameBootstrap] InitializeManagers called twice — ignored");
                return;
            }

            Save.Init();
            SceneFlow.Init();
            _bubbleSpinnerBridge?.Cleanup();
            _bubbleSpinnerBridge = new BubbleSpinnerBridge(conversationManager);
            Conversation.Initialize(_bubbleSpinnerBridge);
            EnsureSaveDataExists();
        }

        private void EnsureSaveDataExists()
        {
            SaveData saveData = Save.GetOrCreateSaveData();

            if (saveData == null)
                throw new InvalidOperationException("FATAL: Failed to create or load save data!");
        }

        // ════════════════════════════════════════════════════════════════
        // SCENE FLOW
        // ════════════════════════════════════════════════════════════════

        private string DetermineNextScene()
        {
            return SceneNames.LOCKSCREEN;
        }

        // ════════════════════════════════════════════════════════════════
        // UTILITIES
        // ════════════════════════════════════════════════════════════════

        private void QuitApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnDestroy()
        {
            _bubbleSpinnerBridge?.Cleanup();
        }

        // ════════════════════════════════════════════════════════════════
        // EDITOR TOOLS
        // ════════════════════════════════════════════════════════════════

#if UNITY_EDITOR
        [ContextMenu("Validate Bootstrap")]
        private void ValidateBootstrap()
        {
            _log.Info("=== BOOTSTRAP VALIDATION ===");

            try
            {
                ValidateManagerReferences();
                _log.Info("✓ All validations passed");
            }
            catch (Exception e)
            {
                _log.Error($"Validation failed: {e.Message}");
            }

            _log.Info($"Initialized: {_isInitialized}");
            _log.Info("===========================");
        }
#endif
    }
}