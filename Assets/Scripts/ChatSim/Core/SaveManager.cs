// ════════════════════════════════════════════════════════════════════════
// Assets/Scripts/Core/SaveManager.cs
// ════════════════════════════════════════════════════════════════════════

using System;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using ChatSim.Data;
using BubbleSpinner.Data;

namespace ChatSim.Core
{
    /// <summary>
    /// Handles save/load operations for game state
    /// Access via: GameBootstrap.Save
    /// 
    /// Features:
    /// - Atomic saves (temp file → swap)
    /// - Automatic backup on overwrite
    /// - Backup recovery on corrupted save
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        // ════════════════════════════════════════════════════════════════════════
        // FILE PATHS
        // ════════════════════════════════════════════════════════════════════════

        private const string ROOT_SAVE_FOLDER = "Saves";
        private const string GAME_DATA_FOLDER = "ChatSimData";
        private const string SAVE_FILE = "game_save.json";
        private const string BACKUP_SUFFIX = ".bak";
        private const string TEMP_SUFFIX = ".tmp";

        private string RootSavePath => Path.Combine(Application.persistentDataPath, ROOT_SAVE_FOLDER);
        private string GameDataPath => Path.Combine(RootSavePath, GAME_DATA_FOLDER);
        private string SaveFilePath => Path.Combine(GameDataPath, SAVE_FILE);
        private string BackupFilePath => SaveFilePath + BACKUP_SUFFIX;
        private string TempFilePath => SaveFilePath + TEMP_SUFFIX;

        // ════════════════════════════════════════════════════════════════════════
        // STATE
        // ════════════════════════════════════════════════════════════════════════

        private bool _isInitialized = false;
        public bool IsInitialized => _isInitialized;

        // ════════════════════════════════════════════════════════════════════════
        // LOGGING
        // ════════════════════════════════════════════════════════════════════════

        private readonly DebugLogger _log = new DebugLogger(
            "SaveManager",
            () => GameBootstrap.Config?.saveManagerDebugLogs ?? false
        );

        #region Initialization

        // ════════════════════════════════════════════════════════════════════════
        // INITIALIZATION
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Called by GameBootstrap during initialization
        /// </summary>
        public void Init()
        {
            try
            {
                CreateFolderStructure();
                ValidateFileSystem();
                
                _isInitialized = true;
                _log.Info("✓ SaveManager initialized successfully");
                LogSavePath();
            }
            catch (Exception e)
            {
                _log.Error($"SaveManager initialization failed: {e.Message}");
                throw;
            }
        }
        #endregion

        #region File System Setup

        // ════════════════════════════════════════════════════════════════════════
        // FILE SYSTEM SETUP
        // ════════════════════════════════════════════════════════════════════════

        private void CreateFolderStructure()
        {
            if (!Directory.Exists(RootSavePath))
            {
                Directory.CreateDirectory(RootSavePath);
            }

            if (!Directory.Exists(GameDataPath))
            {
                Directory.CreateDirectory(GameDataPath);
            }
        }

        private void ValidateFileSystem()
        {
            string testFile = Path.Combine(RootSavePath, ".test");
            
            try
            {
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Cannot write to save directory: {e.Message}");
            }
        }

        private void LogSavePath()
        {
            _log.Info($"Save location: {RootSavePath}");
            
            #if UNITY_EDITOR
            _log.Info("Press F12 to open save folder in explorer");
            #endif
        }
        #endregion

        #region Public API - Save/Load
        
        // ════════════════════════════════════════════════════════════════════════
        // PUBLIC API - SAVE/LOAD
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Check if a save file exists
        /// </summary>
        public bool SaveExists()
        {
            bool exists = File.Exists(SaveFilePath);
            return exists;
        }

        /// <summary>
        /// Get existing save data or create new one if none exists
        /// GUARANTEES a valid SaveData is returned
        /// </summary>
        public SaveData GetOrCreateSaveData()
        {
            // Try to load existing save
            SaveData saveData = LoadGame();
            
            if (saveData != null)
            {
                return saveData;
            }
            
            // No save exists - create new one
            SaveData newSave = CreateNewSave();
            
            // Save it immediately to disk
            SaveGame(newSave);
            
            return newSave;
        }

        /// <summary>
        /// Save game data to disk with atomic write and backup
        /// </summary>
        /// <param name="gameData">Game data to save</param>
        /// <param name="playtime">Current playtime in seconds (optional)</param>
        /// <returns>True if save successful</returns>
        public bool SaveGame(SaveData gameData, float playtime = 0f)
        {
            if (gameData == null)
            {
                _log.Error("Cannot save null game data!");
                return false;
            }

            try
            {
                // Create save data wrapper (includes metadata)
                SaveDataWrapper saveWrapper = new SaveDataWrapper
                {
                    gameData = gameData,
                    saveVersion = gameData.saveVersion,
                    saveTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    playtimeSeconds = playtime
                };

                // Serialize to JSON using Newtonsoft.Json
                #if UNITY_EDITOR
                    string json = JsonConvert.SerializeObject(saveWrapper, Formatting.Indented);
                #else
                    string json = JsonConvert.SerializeObject(saveWrapper, Formatting.None);
                #endif

                // ATOMIC SAVE PROCESS:
                // 1. Write to temp file first
                File.WriteAllText(TempFilePath, json);
                
                // 2. Backup existing save (if it exists)
                if (File.Exists(SaveFilePath))
                {
                    File.Copy(SaveFilePath, BackupFilePath, overwrite: true);
                }
                
                // 3. Replace save file with temp file
                // Note: File.Move doesn't support overwrite in .NET Standard 2.0
                if (File.Exists(SaveFilePath))
                    File.Delete(SaveFilePath);
                
                File.Move(TempFilePath, SaveFilePath);

                _log.Info($"✓ Game saved (version {gameData.saveVersion})");
                
                // Trigger event
                GameEvents.TriggerGameSaved();
                
                return true;
            }
            catch (Exception e)
            {
                _log.Error($"Failed to save game: {e.Message}");
                
                // Clean up temp file if it exists
                if (File.Exists(TempFilePath))
                {
                    try { File.Delete(TempFilePath); } 
                    catch { /* Ignore cleanup errors */ }
                }
                
                return false;
            }
        }

        /// <summary>
        /// Load game data from disk with automatic backup recovery
        /// </summary>
        /// <returns>SaveData if successful, null if failed</returns>
        public SaveData LoadGame()
        {
            // No save files exist at all — fresh install or after intentional delete
            if (!File.Exists(SaveFilePath) && !File.Exists(BackupFilePath))
            {
                _log.Info("No save file found — fresh start");
                return null;
            }

            SaveData saveData = LoadFromFile(SaveFilePath, "primary save");

            if (saveData != null)
                return saveData;

            _log.Warn("Primary save corrupted or missing, attempting backup recovery...");
            saveData = LoadFromFile(BackupFilePath, "backup save");

            if (saveData != null)
            {
                _log.Warn("✓ Recovered from backup!");
                try
                {
                    File.Copy(BackupFilePath, SaveFilePath, overwrite: true);
                    _log.Info("  ✓ Backup restored as primary save");
                }
                catch (Exception e)
                {
                    _log.Error($"Failed to restore backup: {e.Message}");
                }
                return saveData;
            }

            // Only a genuine error if files existed but couldn't be read
            _log.Error("Both primary and backup saves are corrupted or missing!");
            return null;
        }

        /// <summary>
        /// Delete save file and backup
        /// </summary>
        public bool DeleteSave()
        {
            bool deleted = false;

            // Delete primary save
            if (File.Exists(SaveFilePath))
            {
                try
                {
                    File.Delete(SaveFilePath);
                    _log.Info("✓ Primary save deleted");
                    deleted = true;
                }
                catch (Exception e)
                {
                    _log.Error($"Failed to delete primary save: {e.Message}");
                }
            }

            // Delete backup
            if (File.Exists(BackupFilePath))
            {
                try
                {
                    File.Delete(BackupFilePath);
                    _log.Info("✓ Backup save deleted");
                    deleted = true;
                }
                catch (Exception e)
                {
                    _log.Error($"Failed to delete backup: {e.Message}");
                }
            }

            if (deleted)
            {
                GameEvents.TriggerSaveDeleted();
            }
            else
            {
                _log.Warn("No save files to delete");
            }

            return deleted;
        }

        /// <summary>
        /// Create a new default save data
        /// </summary>
        public SaveData CreateNewSave()
        {
            SaveData newSave = new SaveData
            {
                saveVersion = 1
            };
            
            return newSave;
        }

        /// <summary>
        /// Resets a single character's conversation state back to the beginning.
        /// Clears message history, read IDs, unlocked CGs, and all progress.
        /// Called by ContactsAppItem.ExecuteReset()
        /// </summary>
        public bool ResetCharacterStory(string conversationId)
        {
            if (string.IsNullOrEmpty(conversationId))
            {
                _log.Error("ResetCharacterStory: conversationId is null or empty!");
                return false;
            }

            SaveData saveData = GetOrCreateSaveData();

            if (saveData == null)
            {
                _log.Error("ResetCharacterStory: Failed to load save data!");
                return false;
            }

            ConversationState existing = saveData.conversationStates
                .Find(s => s.conversationId == conversationId);

            if (existing == null)
            {
                _log.Warn($"ResetCharacterStory: No save state found for '{conversationId}'. Nothing to reset.");
                return false;
            }

            existing.currentChapterId = "";
            existing.currentNodeName = "";
            existing.currentMessageIndex = 0;
            existing.isInPauseState = false;
            existing.readMessageIds.Clear();
            existing.messageHistory.Clear();
            existing.unlockedCGs.Clear();
            existing.version = ConversationState.CURRENT_VERSION;

            bool saved = SaveGame(saveData);

            if (saved)
            {
                _log.Info($"✓ Story reset for: {existing.characterName} ({conversationId})");
                GameEvents.TriggerCharacterStoryReset(conversationId);
            }
            else
            {
                _log.Error($"ResetCharacterStory: Save failed after resetting '{conversationId}'!");
            }

            return saved;
        }

        /// <summary>
        /// Resets ALL conversation states back to the beginning.
        /// Clears all message history, read IDs, unlocked CGs, and progress for every character.
        /// Called by SettingsPanel when the player confirms Reset All Stories.
        /// </summary>
        public bool ResetAllData()
        {
            SaveData saveData = GetOrCreateSaveData();

            if (saveData == null)
            {
                _log.Error("ResetAllData: Failed to load save data!");
                return false;
            }

            if (saveData.conversationStates == null || saveData.conversationStates.Count == 0)
            {
                _log.Warn("ResetAllData: No conversation states to reset.");
                return false;
            }

            // Reset every conversation state
            foreach (var state in saveData.conversationStates)
            {
                if (state == null) continue;

                state.currentChapterId = "";
                state.currentNodeName     = "";
                state.currentMessageIndex = 0;
                state.isInPauseState      = false;
                state.resumeTarget        = ResumeTarget.None;
                state.readMessageIds.Clear();
                state.messageHistory.Clear();
                state.unlockedCGs.Clear();
                state.version             = ConversationState.CURRENT_VERSION;
            }

            bool saved = SaveGame(saveData);

            if (saved)
            {
                _log.Info($"✓ All stories reset ({saveData.conversationStates.Count} conversations cleared)");
                GameEvents.TriggerAllStoriesReset();
            }
            else
            {
                _log.Error("ResetAllData: Save failed after resetting all stories!");
            }

            return saved;
        }

        #endregion

        #region Private Helpers

        // ════════════════════════════════════════════════════════════════════════
        // PRIVATE HELPERS
        // ════════════════════════════════════════════════════════════════════════
        
        /// <summary>
        /// Load save data from a specific file
        /// </summary>
        private SaveData LoadFromFile(string filePath, string fileDescription)
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            try
            {
                // Read file
                string json = File.ReadAllText(filePath);

                // Deserialize using Newtonsoft.Json
                SaveDataWrapper saveWrapper = JsonConvert.DeserializeObject<SaveDataWrapper>(json);

                if (saveWrapper?.gameData == null)
                {
                    _log.Error($"{fileDescription} is corrupted or invalid!");
                    return null;
                }

                _log.Info($"✓ Loaded {fileDescription}");
                
                // Trigger event only when loading primary save
                if (filePath == SaveFilePath)
                {
                    GameEvents.TriggerGameLoaded();
                }
                
                return saveWrapper.gameData;
            }
            catch (Exception e)
            {
                _log.Warn($"Failed to load {fileDescription}: {e.Message}");
                return null;
            }
        }
        
        #endregion

        #region Save Data Wrapper

        // ════════════════════════════════════════════════════════════════════════
        //  SAVE DATA WRAPPER
        // ════════════════════════════════════════════════════════════════════════
        
        /// <summary>
        /// Wrapper for save data with metadata
        /// </summary>
        [Serializable]
        private class SaveDataWrapper
        {
            public SaveData gameData;
            public int saveVersion;
            public string saveTimestamp;
            public float playtimeSeconds;
        }
        
        #endregion

        #region Editor Debug Tools

        // ════════════════════════════════════════════════════════════════════════
        // EDITOR DEBUG TOOLS
        // ════════════════════════════════════════════════════════════════════════

        #if UNITY_EDITOR

        private string FormatPlaytime(float seconds)
        {
            TimeSpan time = TimeSpan.FromSeconds(seconds);
            
            if (time.TotalHours >= 1)
                return $"{(int)time.TotalHours}h {time.Minutes}m";
            else if (time.TotalMinutes >= 1)
                return $"{time.Minutes}m {time.Seconds}s";
            else
                return $"{time.Seconds}s";
        }

        private void Update()
        {
            // F12: Open save folder
            if (Input.GetKeyDown(KeyCode.F12))
            {
                OpenSaveFolder();
            }
            
            // F11: Delete save (for testing)
            if (Input.GetKeyDown(KeyCode.F11))
            {
                if (UnityEditor.EditorUtility.DisplayDialog(
                    "Delete Save?",
                    "This will delete the save file and backup. Continue?",
                    "Delete",
                    "Cancel"))
                {
                    DeleteSave();
                }
            }
        }

        [ContextMenu("Open Save Folder")]
        public void OpenSaveFolder()
        {
            if (!Directory.Exists(RootSavePath))
            {
                CreateFolderStructure();
            }

            Application.OpenURL("file://" + RootSavePath);
            _log.Info("Opened save folder in explorer");
        }

        [ContextMenu("Delete Save File")]
        private void EditorDeleteSave()
        {
            DeleteSave();
        }

        [ContextMenu("Print Save Info")]
        private void PrintSaveInfo()
        {
            UnityEngine.Debug.Log("=== SAVE MANAGER INFO ===");
            UnityEngine.Debug.Log($"Initialized: {_isInitialized}");
            UnityEngine.Debug.Log($"Root Path: {RootSavePath}");
            UnityEngine.Debug.Log($"Game Data Path: {GameDataPath}");
            UnityEngine.Debug.Log($"Save File: {SaveFilePath}");
            UnityEngine.Debug.Log($"Backup File: {BackupFilePath}");
            UnityEngine.Debug.Log($"Primary Save Exists: {File.Exists(SaveFilePath)}");
            UnityEngine.Debug.Log($"Backup Exists: {File.Exists(BackupFilePath)}");
            
            if (File.Exists(SaveFilePath))
            {
                try
                {
                    string json = File.ReadAllText(SaveFilePath);
                    SaveDataWrapper wrapper = JsonConvert.DeserializeObject<SaveDataWrapper>(json);
                    UnityEngine.Debug.Log($"Save version: {wrapper.saveVersion}");
                    UnityEngine.Debug.Log($"Saved: {wrapper.saveTimestamp}");
                    UnityEngine.Debug.Log($"Playtime: {FormatPlaytime(wrapper.playtimeSeconds)}");
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"Failed to read save: {e.Message}");
                }
            }
            
            UnityEngine.Debug.Log("========================");
        }

        [ContextMenu("Create Test Save")]
        private void CreateTestSave()
        {
            SaveData testData = CreateNewSave();
            SaveGame(testData, 3600f); // 1 hour playtime
            UnityEngine.Debug.Log("✓ Test save created");
        }

        [ContextMenu("Test Backup Recovery")]
        private void TestBackupRecovery()
        {
            if (!File.Exists(BackupFilePath))
            {
                UnityEngine.Debug.LogError("No backup file exists to test recovery!");
                return;
            }

            string corruptData = "CORRUPTED_DATA_TEST";
            File.WriteAllText(SaveFilePath, corruptData);
            UnityEngine.Debug.Log("✓ Corrupted primary save for testing");

            SaveData recovered = LoadGame();
            
            if (recovered != null)
            {
                UnityEngine.Debug.Log("✓ Backup recovery successful!");
            }
            else
            {
                UnityEngine.Debug.LogError("ERROR: Backup recovery failed!");
            }
        }

        #endif
        #endregion
    }
}