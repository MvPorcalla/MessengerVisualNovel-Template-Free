// ════════════════════════════════════════════════════════════════════════
// Assets/Scripts/Core/SceneFlowManager.cs
// ════════════════════════════════════════════════════════════════════════

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChatSim.Core
{
    /// <summary>
    /// Manages scene transitions and flow control for the game. 
    /// Provides a centralized API to load scenes, trigger events during scene changes, and handle transition states.
    /// Access via: GameBootstrap.SceneFlow
    /// </summary>
    public class SceneFlowManager : MonoBehaviour
    {
        #region State
        private string _currentScene = null;
        private bool _isTransitioning = false;
        private Coroutine _transitionCoroutine = null;
        #endregion

        #region Properties
        public string CurrentScene => _currentScene;
        public bool IsTransitioning => _isTransitioning;
        #endregion

        #region Initialization
        /// <summary>
        /// Called by GameBootstrap during initialization
        /// </summary>
        public void Init()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            _currentScene = activeScene.name;
        }
        #endregion

        #region Public API - Scene Loading
        
        /// <summary>
        /// Load a scene (replaces current scene)
        /// </summary>
        public void LoadScene(string sceneName)
        {
            // Validation 1: Scene name not empty
            if (string.IsNullOrEmpty(sceneName))
            {
                LogError("Cannot load scene with null/empty name!");
                return;
            }

            // Validation 2: Not already transitioning
            if (_isTransitioning)
            {
                LogWarning($"Cannot load {sceneName} - already transitioning");
                return;
            }

            // Validation 3: Not loading the same scene
            if (_currentScene == sceneName)
            {
                LogWarning($"Already in scene: {sceneName}");
                return;
            }

            // Validation 4: Scene exists in build settings
            if (!IsSceneInBuildSettings(sceneName))
            {
                LogError($"Scene '{sceneName}' not found in Build Settings! Add it via File > Build Settings.");
                return;
            }

            if (_transitionCoroutine != null) StopCoroutine(_transitionCoroutine);
            _transitionCoroutine = StartCoroutine(LoadSceneCoroutine(sceneName));
        }

        /// <summary>
        /// Set current scene (called after loading completes)
        /// </summary>
        public void SetCurrentScene(string sceneName)
        {
            _currentScene = sceneName;
        }
        
        #endregion

        #region Convenience Methods
        
        public void GoToDisclaimer() => LoadScene(SceneNames.DISCLAIMER);
        public void GoToLockScreen() => LoadScene(SceneNames.LOCKSCREEN);
        public void GoToPhoneScreen() => LoadScene(SceneNames.PHONE_SCREEN);
        public void GoToChatApp() => LoadScene(SceneNames.CHAT_APP);
        
        #endregion

        #region Scene Validation
        
        /// <summary>
        /// Check if a scene exists in the build settings
        /// </summary>
        private bool IsSceneInBuildSettings(string sceneName)
        {
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                string name = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                
                if (name == sceneName)
                    return true;
            }
            
            return false;
        }
        
        #endregion

        #region Scene Loading Coroutine
        
        private IEnumerator LoadSceneCoroutine(string sceneName)
        {
            _isTransitioning = true;
            
            // Trigger scene changing event
            GameEvents.TriggerSceneChanging(sceneName);
            
            // Optional: Add fade out here if you have a fade system
            // yield return FadeOut();
            
            // Load scene asynchronously
            AsyncOperation loadOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            
            if (loadOp == null)
            {
                LogError($"Failed to start loading {sceneName}");
                _isTransitioning = false;
                yield break;
            }

            // Wait for scene to load
            while (!loadOp.isDone)
            {
                // Optional: Update loading progress UI here
                // float progress = loadOp.progress;
                yield return null;
            }

            // Verify scene loaded successfully
            Scene newScene = SceneManager.GetSceneByName(sceneName);
            if (newScene.IsValid() && newScene.isLoaded)
            {
                SceneManager.SetActiveScene(newScene);
                _currentScene = sceneName;
                
                // Trigger scene loaded event
                GameEvents.TriggerSceneLoaded(sceneName);
            }
            else
            {
                LogError($"Scene {sceneName} invalid after load");
            }

            // Optional: Add fade in here if you have a fade system
            // yield return FadeIn();

            _isTransitioning = false;
        }
        
        #endregion

        #region Logging

        private static void LogError(string message)
        {
            Debug.LogError($"[SceneFlow] {message}");
        }

        private static void LogWarning(string message)
        {
            Debug.LogWarning($"[SceneFlow] {message}");
        }
        #endregion

        #region Editor Tools
        #if UNITY_EDITOR
        [ContextMenu("Print Current State")]
        private void PrintCurrentState()
        {
            Debug.Log("=== SCENE FLOW STATE ===");
            Debug.Log($"Current Scene: {_currentScene ?? "None"}");
            Debug.Log($"Is Transitioning: {_isTransitioning}");
            Debug.Log("=======================");
        }

        [ContextMenu("Validate All Scenes")]
        private void ValidateAllScenes()
        {
            Debug.Log("=== SCENE VALIDATION ===");
            
            string[] sceneNames = new string[]
            {
                SceneNames.DISCLAIMER,
                SceneNames.BOOTSTRAP,
                SceneNames.LOCKSCREEN,
                SceneNames.PHONE_SCREEN,
                SceneNames.CHAT_APP
            };

            bool allValid = true;
            
            foreach (string sceneName in sceneNames)
            {
                bool exists = IsSceneInBuildSettings(sceneName);
                string status = exists ? "✓" : "✗";
                Debug.Log($"{status} {sceneName}");
                
                if (!exists)
                    allValid = false;
            }

            if (allValid)
            {
                Debug.Log("✓ All scenes are in Build Settings!");
            }
            else
            {
                Debug.LogWarning("WARNING: Some scenes missing from Build Settings!");
                Debug.LogWarning("Add them via File > Build Settings > Add Open Scenes");
            }
            
            Debug.Log("=======================");
        }

        [ContextMenu("Load Disclaimer")]
        private void EditorLoadDisclaimer() => LoadScene(SceneNames.DISCLAIMER);

        [ContextMenu("Load LockScreen")]
        private void EditorLoadLockScreen() => LoadScene(SceneNames.LOCKSCREEN);

        [ContextMenu("Load PhoneScreen")]
        private void EditorLoadPhoneScreen() => LoadScene(SceneNames.PHONE_SCREEN);

        [ContextMenu("Load ChatApp")]
        private void EditorLoadChatApp() => LoadScene(SceneNames.CHAT_APP);
        #endif
        #endregion
    }
}