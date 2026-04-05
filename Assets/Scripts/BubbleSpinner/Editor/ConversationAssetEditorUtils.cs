#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.AddressableAssets;
using BubbleSpinner.Data;

namespace BubbleSpinner.EditorTools
{
    public static class Labels
    {
        // ─────────────────────────────────────────────
        // TABS
        // ─────────────────────────────────────────────
        public const string TAB_CHAPTERS = "Chapters";
        public const string TAB_PROFILE  = "Profile";
        public const string TAB_CG       = "CG Images";
        public const string TAB_TOOLS    = "Tools";

        // ─────────────────────────────────────────────
        // SECTION HEADERS
        // ─────────────────────────────────────────────
        public const string SECTION_CHARACTER = "CHARACTER";
        public const string SECTION_CHAPTERS  = "CHAPTERS";
        public const string SECTION_PROFILE   = "PROFILE";
        public const string SECTION_CG        = "CG IMAGES";
        public const string SECTION_TOOLS     = "TOOLS";
    }

    public static class ConversationAssetEditorUtils
    {
        // ─────────────────────────────────────────────
        // FILE PARSING
        // ─────────────────────────────────────────────

        /// <summary>
        /// Reads the `chapter:` header from a .bub TextAsset.
        /// Returns the file name as fallback if no chapter: declaration is found.
        /// </summary>
        public static string ReadChapterIdFromBub(TextAsset file)
        {
            if (file == null) return "";

            foreach (var line in file.text.Split('\n'))
            {
                var t = System.Text.RegularExpressions.Regex.Replace(line.Trim(), @"\s+", " ");

                if (t.StartsWith("chapter :", StringComparison.OrdinalIgnoreCase))
                    return t.Substring("chapter :".Length).Trim();

                if (t.StartsWith("chapter:", StringComparison.OrdinalIgnoreCase))
                    return t.Substring("chapter:".Length).Trim();
            }

            Debug.LogWarning($"[ConversationAsset] No 'chapter:' declaration found in '{file.name}' — using file name as fallback");
            return file.name;
        }

        // ─────────────────────────────────────────────
        // FOLDER SCANNING
        // ─────────────────────────────────────────────

        /// <summary>
        /// Fills cgAddressableKeys from all Texture2D and Sprite assets in the given folder.
        /// Uses the Addressables address registered for each asset, not the asset path.
        /// Assets not marked as Addressable are skipped with a warning.
        /// fullPath must be inside the Assets directory.
        /// </summary>
        public static void FillCGFromFolder(ConversationAsset asset, string fullPath)
        {
            if (!fullPath.Contains("Assets"))
            {
                Debug.LogWarning("[ConversationAsset] Selected folder must be inside the Assets directory.");
                return;
            }

            string projectPath = fullPath.Substring(fullPath.IndexOf("Assets"));
            string[] guids     = AssetDatabase.FindAssets("t:Texture2D t:Sprite", new[] { projectPath });

            asset.cgAddressableKeys.Clear();

            var settings = AddressableAssetSettingsDefaultObject.Settings;

            if (settings == null)
            {
                Debug.LogWarning("[ConversationAsset] Addressables settings not found — make sure Addressables is initialized.");
                return;
            }

            int skipped = 0;

            foreach (string guid in guids)
            {
                var entry = settings.FindAssetEntry(guid);

                if (entry == null)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    Debug.LogWarning($"[ConversationAsset] Asset not marked as Addressable — skipped: {assetPath}");
                    skipped++;
                    continue;
                }

                asset.cgAddressableKeys.Add(entry.address);
            }

            if (skipped > 0)
            {
                Debug.LogWarning($"[ConversationAsset] {skipped} asset(s) skipped — mark them as Addressable first, then re-run.");
            }

            Debug.Log($"[ConversationAsset] Filled {asset.cgAddressableKeys.Count} CG keys from Addressables.");
        }

        // ─────────────────────────────────────────────
        // VALIDATION
        // ─────────────────────────────────────────────

        /// <summary>
        /// Validates a ConversationAsset and returns a list of warning strings.
        /// Empty list means the asset is valid.
        /// </summary>
        public static List<string> Validate(ConversationAsset asset)
        {
            var warnings = new List<string>();

            if (string.IsNullOrWhiteSpace(asset.characterName))
                warnings.Add("Character name is empty.");

            if (asset.chapters == null || asset.chapters.Count == 0)
            {
                warnings.Add("No chapters assigned.");
                return warnings; // No point checking further
            }

            if (asset.chapters[0].file == null)
                warnings.Add("Entry point chapter (index 0) has no file assigned.");

            for (int i = 0; i < asset.chapters.Count; i++)
            {
                var c = asset.chapters[i];

                if (string.IsNullOrEmpty(c.chapterId))
                    warnings.Add($"Chapter [{i}] is missing a Chapter ID.");

                if (c.file == null)
                    warnings.Add($"Chapter [{i}] \"{c.chapterId}\" has no file assigned.");
            }

            // Validate CG keys against Addressables
            if (asset.cgAddressableKeys != null && asset.cgAddressableKeys.Count > 0)
            {
                var settings = AddressableAssetSettingsDefaultObject.Settings;

                if (settings == null)
                {
                    warnings.Add("Addressables settings not found — cannot validate CG keys.");
                }
                else
                {
                    for (int i = 0; i < asset.cgAddressableKeys.Count; i++)
                    {
                        string key = asset.cgAddressableKeys[i];

                        if (string.IsNullOrEmpty(key))
                        {
                            warnings.Add($"CG key [{i}] is empty.");
                            continue;
                        }

                        // Check if any entry has this address
                        bool found = false;
                        foreach (var group in settings.groups)
                        {
                            if (group == null) continue;
                            foreach (var entry in group.entries)
                            {
                                if (entry.address == key) { found = true; break; }
                            }
                            if (found) break;
                        }

                        if (!found)
                            warnings.Add($"CG key [{i}] '{key}' not found in Addressables — check address or mark asset as Addressable.");
                    }
                }
            }

            return warnings;
        }
    }
}
#endif