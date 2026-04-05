// ════════════════════════════════════════════════════════════════════════
// Assets/Scripts/ChatSim/UI/HomeScreen/Gallery/Controllers/GalleryController.cs
// ════════════════════════════════════════════════════════════════════════

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChatSim.Core;
using BubbleSpinner.Data;
using ChatSim.Data;

namespace ChatSim.UI.HomeScreen.Gallery
{
    /// <summary>
    /// Main controller for the CG Gallery panel.
    /// Responsibilities:
    /// - Dynamically builds the gallery UI based on CharacterDatabase and save data
    /// - Handles thumbnail clicks to open fullscreen viewer
    /// - Displays overall progress of unlocked CGs
    /// - Provides editor tools for debugging and validation
    /// Attach to: GalleryController GameObject (child of GalleryPanel)
    /// </summary>
    public class GalleryController : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════
        // ░ INSPECTOR REFERENCES
        // ═══════════════════════════════════════════════════════════
        
        [Header("Gallery UI")]
        [SerializeField] private Transform contentContainer;
        [SerializeField] private TextMeshProUGUI progressText;
        
        [Header("Prefabs")]
        [SerializeField] private GameObject characterSectionPrefab;
        [SerializeField] private GameObject thumbnailPrefab;
        
        [Header("Character Data")]
        [Tooltip("Drag your CharacterDatabase ScriptableObject here")]
        [SerializeField] private CharacterDatabase characterDatabase;
        
        [Header("Display Options")]
        [SerializeField] private bool showLockedCGs = true;
        [SerializeField] private bool showEmptySections = false;
        [SerializeField] private Sprite lockedCGSprite;
        
        [Header("Fullscreen Viewer")]
        [SerializeField] private GalleryFullscreenViewer fullscreenViewer;

        // ═══════════════════════════════════════════════════════════
        // ░ LOGGING
        // ═══════════════════════════════════════════════════════════

        private readonly DebugLogger _log = new DebugLogger(
            "GalleryController",
            () => GameBootstrap.Config?.galleryAppDebugLogs ?? false
        );
        
        // ═══════════════════════════════════════════════════════════
        // ░ STATE
        // ═══════════════════════════════════════════════════════════
        
        private List<GameObject> spawnedObjects = new List<GameObject>();
        private SaveData currentSaveData;
        
        // ═══════════════════════════════════════════════════════════
        // ░ LIFECYCLE
        // ═══════════════════════════════════════════════════════════
        
        private void OnEnable()
        {
            RefreshGallery();
        }
        
        // ═══════════════════════════════════════════════════════════
        // ░ PUBLIC API
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>
        /// Rebuild the entire gallery from save data
        /// </summary>
        public void RefreshGallery()
        {
            ClearGallery();

            if (characterDatabase == null)
            {
               _log.Error("CharacterDatabase not assigned!");
                return;
            }

            currentSaveData = GameBootstrap.Save?.GetOrCreateSaveData();

            if (currentSaveData == null)
            {
               _log.Error("Failed to load save data!");
                return;
            }

            var allCharacters = characterDatabase.GetAllCharacters();

            if (allCharacters == null || allCharacters.Count == 0)
            {
                _log.Warn("CharacterDatabase is empty!");
                return;
            }

            // Build lookup once — single scan of save data regardless of character count
            var unlockedCGsLookup = BuildUnlockedCGsLookup();

            int totalUnlocked = 0;
            int totalCGs = 0;

            foreach (var convAsset in allCharacters)
            {
                if (convAsset == null) continue;

                if (convAsset.cgAddressableKeys == null || convAsset.cgAddressableKeys.Count == 0)
                {
                    _log.Warn($"{convAsset.characterName} has no CGs defined");
                    continue;
                }

                // Use lookup instead of scanning save data per character
                unlockedCGsLookup.TryGetValue(convAsset.ConversationId, out HashSet<string> unlockedCGs);
                unlockedCGs ??= new HashSet<string>();

                if (!showEmptySections && unlockedCGs.Count == 0)
                {
                    _log.Info($"Skipping {convAsset.characterName} (0 CGs unlocked)");
                    continue;
                }

                totalUnlocked += unlockedCGs.Count;
                totalCGs += convAsset.cgAddressableKeys.Count;

                CreateCharacterSection(convAsset, unlockedCGs);
            }

            UpdateProgressDisplay(totalUnlocked, totalCGs);

            _log.Info($"Gallery refreshed: {totalUnlocked}/{totalCGs} CGs unlocked from {allCharacters.Count} characters");
        }
        
        // ═══════════════════════════════════════════════════════════
        // ░ CHARACTER SECTION CREATION
        // ═══════════════════════════════════════════════════════════
        
        private void CreateCharacterSection(ConversationAsset convAsset, HashSet<string> unlockedCGs)
        {
            if (characterSectionPrefab == null || contentContainer == null)
            {
               _log.Error("Missing characterSectionPrefab or contentContainer!");
                return;
            }

            GameObject section = Instantiate(characterSectionPrefab, contentContainer);
            spawnedObjects.Add(section);

            if (section.transform.childCount < 2)
            {
               _log.Error($"CGContainer prefab must have at least 2 children (CharacterName, CGGrid). Found: {section.transform.childCount}");
                return;
            }

            TextMeshProUGUI headerText = section.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            if (headerText != null)
            {
                int unlocked = unlockedCGs.Count;
                int total = convAsset.cgAddressableKeys.Count;
                headerText.text = $"{convAsset.characterName} — {unlocked}/{total}";
            }
            else
            {
               _log.Error("CGContainer child 0 missing TextMeshProUGUI component!");
            }

            Transform gridContainer = section.transform.GetChild(1);

            foreach (string cgKey in convAsset.cgAddressableKeys)
            {
                bool isUnlocked = unlockedCGs.Contains(cgKey);

                if (!showLockedCGs && !isUnlocked)
                    continue;

                CreateThumbnail(cgKey, isUnlocked, gridContainer);
            }
        }
        
        // ═══════════════════════════════════════════════════════════
        // ░ THUMBNAIL CREATION
        // ═══════════════════════════════════════════════════════════
        
        private void CreateThumbnail(string cgKey, bool isUnlocked, Transform parent)
        {
            if (thumbnailPrefab == null)
            {
               _log.Error("Missing thumbnailPrefab!");
                return;
            }
            
            GameObject thumbnail = Instantiate(thumbnailPrefab, parent);
            spawnedObjects.Add(thumbnail);
            
            // Setup thumbnail component
            GalleryThumbnailItem item = thumbnail.GetComponent<GalleryThumbnailItem>();
            if (item != null)
            {
                item.Initialize(cgKey, isUnlocked, lockedCGSprite, OnThumbnailClicked);
            }
            else
            {
                _log.Error("Thumbnail prefab missing GalleryThumbnailItem component!");
            }
        }
        
        // ═══════════════════════════════════════════════════════════
        // ░ THUMBNAIL CLICK HANDLER
        // ═══════════════════════════════════════════════════════════
        
        private void OnThumbnailClicked(string cgKey, Sprite sprite)
        {
            if (fullscreenViewer == null)
            {
                _log.Warn("FullscreenCGViewer not assigned!");
                return;
            }
            
            string cgName = System.IO.Path.GetFileName(cgKey);
            
            fullscreenViewer.Show(sprite, cgName);
            
            _log.Info($"Opened fullscreen: {cgKey}");
        }
        
        // ═══════════════════════════════════════════════════════════
        // ░ SAVE DATA QUERIES
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Builds a lookup dictionary of all unlocked CGs per conversation.
        /// Called once per RefreshGallery — avoids repeated list scans per character.
        /// </summary>
        private Dictionary<string, HashSet<string>> BuildUnlockedCGsLookup()
        {
            var lookup = new Dictionary<string, HashSet<string>>();

            if (currentSaveData?.conversationStates == null)
                return lookup;

            foreach (var convState in currentSaveData.conversationStates)
            {
                if (convState?.unlockedCGs == null) continue;

                lookup[convState.conversationId] = new HashSet<string>(convState.unlockedCGs);
            }

            return lookup;
        }
        
        // ═══════════════════════════════════════════════════════════
        // ░ PROGRESS DISPLAY
        // ═══════════════════════════════════════════════════════════
        
        private void UpdateProgressDisplay(int unlocked, int total)
        {
            if (progressText == null) return;
            
            float percentage = total > 0 ? (unlocked / (float)total) * 100f : 0f;
            progressText.text = $"{unlocked}/{total} ({percentage:F0}%)";
        }
        
        // ═══════════════════════════════════════════════════════════
        // ░ CLEANUP
        // ═══════════════════════════════════════════════════════════
        
        private void ClearGallery()
        {
            foreach (var obj in spawnedObjects)
            {
                if (obj != null)
                    Destroy(obj);
            }
            spawnedObjects.Clear();
        }
        
        private void OnDisable()
        {
            // Optional: Clear gallery when panel is hidden to free memory
            // ClearGallery();
        }
        
        #region EDITOR TOOLS
        // ═══════════════════════════════════════════════════════════
        // ░ EDITOR TOOLS
        // ═══════════════════════════════════════════════════════════
        
        #if UNITY_EDITOR
        [ContextMenu("Debug/Print Gallery Stats")]
        private void DebugPrintStats()
        {
            if (characterDatabase == null)
            {
                Debug.LogError("CharacterDatabase not assigned!");
                return;
            }

            currentSaveData = GameBootstrap.Save?.GetOrCreateSaveData();
            if (currentSaveData == null) return;

            // Build lookup the same way RefreshGallery does
            var unlockedCGsLookup = BuildUnlockedCGsLookup();

            Debug.Log("╔═══════════════ GALLERY STATS ═══════════════╗");

            var allCharacters = characterDatabase.GetAllCharacters();

            foreach (var convAsset in allCharacters)
            {
                if (convAsset == null) continue;

                unlockedCGsLookup.TryGetValue(convAsset.ConversationId, out HashSet<string> unlocked);
                unlocked ??= new HashSet<string>();

                int total = convAsset.cgAddressableKeys?.Count ?? 0;
                float percentage = total > 0 ? (unlocked.Count / (float)total) * 100f : 0f;

                Debug.Log($"║ {convAsset.characterName}");
                Debug.Log($"║   {unlocked.Count}/{total} ({percentage:F1}%)");

                if (unlocked.Count > 0)
                {
                    Debug.Log($"║   Unlocked: {string.Join(", ", unlocked)}");
                }

                Debug.Log("╠═════════════════════════════════════════════╣");
            }

            Debug.Log("╚═════════════════════════════════════════════╝");
        }
        
        [ContextMenu("Debug/Validate References")]
        private void DebugValidateReferences()
        {
            Debug.Log("╔═══════ VALIDATING GALLERY REFERENCES ═══════╗");
            
            bool allValid = true;
            
            if (contentContainer == null)
            {
                Debug.LogError("║ Content Container not assigned!");
                allValid = false;
            }
            
            if (characterSectionPrefab == null)
            {
                Debug.LogError("║ Character Section Prefab not assigned!");
                allValid = false;
            }
            
            if (thumbnailPrefab == null)
            {
                Debug.LogError("║ Thumbnail Prefab not assigned!");
                allValid = false;
            }
            
            if (fullscreenViewer == null)
            {
                Debug.LogWarning("║ Fullscreen Viewer not assigned!");
            }
            
            if (characterDatabase == null)
            {
                Debug.LogError("║ CharacterDatabase not assigned!");
                allValid = false;
            }
            else
            {
                var allCharacters = characterDatabase.GetAllCharacters();
                Debug.Log($"║ ✓ CharacterDatabase assigned ({allCharacters.Count} characters)");
                
                for (int i = 0; i < allCharacters.Count; i++)
                {
                    var asset = allCharacters[i];
                    if (asset == null)
                    {
                        Debug.LogError($"║   [{i}] NULL reference in database!");
                        allValid = false;
                    }
                    else
                    {
                        int cgCount = asset.cgAddressableKeys?.Count ?? 0;
                        Debug.Log($"║   [{i}] ✓ {asset.characterName} ({cgCount} CGs)");
                    }
                }
            }
            
            Debug.Log("╚═════════════════════════════════════════════╝");
            
            if (allValid)
            {
                Debug.Log("✓ All references valid!");
            }
        }
        #endif
        #endregion
    }
}