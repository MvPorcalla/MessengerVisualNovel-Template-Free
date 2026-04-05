#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using BubbleSpinner.Data;

namespace BubbleSpinner.EditorTools
{
    [CustomEditor(typeof(ConversationAsset))]
    public class ConversationAssetEditor : Editor
    {
        private int tabIndex;

        private SerializedProperty profileImageProp;
        private SerializedProperty conversationIdProp;
        private SerializedProperty characterNameProp;
        private SerializedProperty chaptersProp;
        private SerializedProperty cgKeysProp;

        // Profile properties
        private SerializedProperty characterAgeProp;
        private SerializedProperty birthdateProp;
        private SerializedProperty occupationProp;
        private SerializedProperty relationshipStatusProp;
        private SerializedProperty bioProp;
        private SerializedProperty descriptionProp;
        private SerializedProperty personalityTraitsProp;

        private ReorderableList chapterList;
        private ReorderableList cgList;

        // =========================
        // STYLES
        // =========================
        private GUIStyle cardStyle;
        private GUIStyle sectionStyle;
        private GUIStyle headerStyle;
        private GUIStyle subLabelStyle;

        // Chapter list styles — cached to avoid per-repaint allocations
        private GUIStyle chapterLabelStyle;
        private GUIStyle chapterPillStyle;
        private GUIStyle chapterEntryBadgeStyle;
        private GUIStyle chapterAlwaysLoadsStyle;
        private GUIStyle chapterRemoveStyle;
        private GUIStyle chapterAddButtonStyle;

        private void InitStyles()
        {
            if (cardStyle == null)
            {
                cardStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    padding = new RectOffset(12, 12, 10, 10),
                    margin  = new RectOffset(0, 0, 6, 6)
                };
            }

            if (sectionStyle == null)
            {
                sectionStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize   = 11,
                    fontStyle  = FontStyle.Bold,
                    normal     = { textColor = new Color(0.7f, 0.7f, 0.7f) }
                };
            }

            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 16
                };
            }

            if (subLabelStyle == null)
            {
                subLabelStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
                };
            }

            if (chapterLabelStyle == null)
            {
                chapterLabelStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    fontStyle = FontStyle.Normal,
                    normal    = { textColor = new Color(0.6f, 0.6f, 0.6f) }
                };
            }

            if (chapterPillStyle == null)
            {
                chapterPillStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    normal    = { textColor = Color.white }
                };
            }

            if (chapterEntryBadgeStyle == null)
            {
                chapterEntryBadgeStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    normal    = { textColor = Color.white }
                };
            }

            if (chapterAlwaysLoadsStyle == null)
            {
                chapterAlwaysLoadsStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleRight,
                    fontStyle = FontStyle.Italic,
                    normal    = { textColor = new Color(0.5f, 0.8f, 0.8f) }
                };
            }

            if (chapterRemoveStyle == null)
            {
                chapterRemoveStyle = new GUIStyle(EditorStyles.miniButton)
                {
                    fontStyle = FontStyle.Bold,
                    normal    = { textColor = new Color(0.8f, 0.4f, 0.4f) }
                };
            }

            if (chapterAddButtonStyle == null)
            {
                chapterAddButtonStyle = new GUIStyle(EditorStyles.miniButton)
                {
                    fontSize    = 11,
                    fontStyle   = FontStyle.Bold,
                    fixedHeight = 24f,
                    normal      = { textColor = new Color(0.7f, 0.9f, 0.7f) }
                };
            }
        }

        private void OnEnable()
        {
            // Core
            profileImageProp   = serializedObject.FindProperty("profileImage");
            conversationIdProp = serializedObject.FindProperty("conversationId");
            characterNameProp  = serializedObject.FindProperty("characterName");
            chaptersProp       = serializedObject.FindProperty("chapters");
            cgKeysProp         = serializedObject.FindProperty("cgAddressableKeys");

            // Profile
            characterAgeProp        = serializedObject.FindProperty("characterAge");
            birthdateProp           = serializedObject.FindProperty("birthdate");
            occupationProp          = serializedObject.FindProperty("occupation");
            relationshipStatusProp  = serializedObject.FindProperty("relationshipStatus");
            bioProp                 = serializedObject.FindProperty("bio");
            descriptionProp         = serializedObject.FindProperty("description");
            personalityTraitsProp   = serializedObject.FindProperty("personalityTraits");

            InitStyles();
            BuildChapterList();
            BuildCGList();
        }

        public override void OnInspectorGUI()
        {
            // Safety net — EditorStyles can be null on first OnEnable in some Unity versions
            if (cardStyle == null) InitStyles();

            var asset = (ConversationAsset)target;

            serializedObject.Update();

            DrawHeader(asset);
            DrawTabs();

            GUILayout.Space(10);

            switch (tabIndex)
            {
                case 0: DrawChapters(asset); break;
                case 1: DrawProfile(asset); break;
                case 2: DrawCG(asset); break;
                case 3: DrawTools(asset); break;
            }

            serializedObject.ApplyModifiedProperties();
        }

        // =========================
        // HEADER
        // =========================
        private void DrawHeader(ConversationAsset asset)
        {
            GUILayout.Space(5);

            EditorGUILayout.LabelField(
                string.IsNullOrEmpty(asset.characterName) ? "Unnamed Character" : asset.characterName,
                headerStyle
            );

            EditorGUILayout.LabelField("Conversation Asset", subLabelStyle);

            GUILayout.Space(8);
            DrawLine();
            GUILayout.Space(8);
        }

        // =========================
        // TABS
        // =========================

        private void DrawTabs()
        {
            tabIndex = GUILayout.Toolbar(
                tabIndex,
                new string[]
                {
                    Labels.TAB_CHAPTERS,
                    Labels.TAB_PROFILE,
                    Labels.TAB_CG,
                    Labels.TAB_TOOLS
                },
                GUILayout.Height(28)
            );
        }

        // =========================
        // CHAPTERS
        // =========================

        private void DrawChapters(ConversationAsset asset)
        {
            DrawSection(Labels.SECTION_CHARACTER);

            DrawCard(() =>
            {
                EditorGUILayout.PropertyField(characterNameProp, new GUIContent("Character Name"));
                EditorGUILayout.PropertyField(profileImageProp);
                EditorGUILayout.PropertyField(conversationIdProp);
            });

            GUILayout.Space(10);
            DrawSection(Labels.SECTION_CHAPTERS);
            chapterList.DoLayoutList();
        }

        private void BuildChapterList()
        {
            chapterList = new ReorderableList(
                serializedObject,
                chaptersProp,
                draggable:         true,
                displayHeader:     true,
                displayAddButton:  false,
                displayRemoveButton: false
            );

            chapterList.drawHeaderCallback = rect =>
                EditorGUI.LabelField(rect, $"Chapters  ({chaptersProp.arraySize})", EditorStyles.boldLabel);

            chapterList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                SerializedProperty element  = chaptersProp.GetArrayElementAtIndex(index);
                SerializedProperty idProp   = element.FindPropertyRelative("chapterId");
                SerializedProperty fileProp = element.FindPropertyRelative("file");

                float lineHeight = EditorGUIUtility.singleLineHeight;
                float spacing    = EditorGUIUtility.standardVerticalSpacing;
                float y          = rect.y + spacing;
                float fullWidth  = rect.width;

                // ── Auto-detect chapter ID from file ──
                TextAsset currentFile = fileProp.objectReferenceValue as TextAsset;
                if (currentFile != null)
                {
                    string detectedId = ConversationAssetEditorUtils.ReadChapterIdFromBub(currentFile);
                    if (idProp.stringValue != detectedId)
                    {
                        idProp.stringValue = detectedId;
                        serializedObject.ApplyModifiedProperties();
                    }
                }

                bool isEntryPoint = index == 0;

                // ── Shared styles — cached, no allocation per repaint ──
                GUIStyle labelStyle = chapterLabelStyle;
                GUIStyle pillStyle  = chapterPillStyle;

                float removeButtonWidth = 30f;

                // ════════════════════════════════════════════
                // ENTRY POINT — 3 rows
                // ════════════════════════════════════════════
                if (isEntryPoint)
                {
                    // ── Row 1: [ENTRY POINT] ──────── [Always Loads First] ──
                    float entryBadgeWidth = 90f;
                    Rect entryBadgeRect = new Rect(rect.x, y + 1f, entryBadgeWidth, lineHeight - 2f);

                    EditorGUI.DrawRect(entryBadgeRect, new Color(0.18f, 0.55f, 0.55f, 0.85f));

                    EditorGUI.LabelField(entryBadgeRect, "ENTRY POINT", chapterEntryBadgeStyle);

                    GUIStyle alwaysLoadsStyle = chapterAlwaysLoadsStyle;

                    EditorGUI.LabelField(
                        new Rect(rect.x, y, fullWidth, lineHeight),
                        "Always Loads First",
                        alwaysLoadsStyle
                    );

                    y += lineHeight + spacing;

                    // ── Row 2: Chapter ID: [ pill ] ──────────────── [ x ] ──
                    float labelWidth  = 75f;
                    float pillWidth   = Mathf.Clamp(
                        (!string.IsNullOrEmpty(idProp.stringValue) ? idProp.stringValue.Length : 5) * 7.5f + 12f,
                        48f, 140f
                    );

                    EditorGUI.LabelField(
                        new Rect(rect.x, y, labelWidth, lineHeight),
                        "Chapter ID:", labelStyle
                    );

                    Rect pillRect = new Rect(rect.x + labelWidth, y + 1f, pillWidth, lineHeight - 2f);
                    Color pillBg  = !string.IsNullOrEmpty(idProp.stringValue)
                        ? new Color(0.22f, 0.45f, 0.55f, 0.9f)
                        : new Color(0.5f, 0.2f, 0.2f, 0.9f);

                    EditorGUI.DrawRect(pillRect, pillBg);
                    EditorGUI.LabelField(pillRect,
                        !string.IsNullOrEmpty(idProp.stringValue) ? idProp.stringValue : "no id",
                        pillStyle
                    );

                    // No remove button for entry point — just occupy the space
                    y += lineHeight + spacing;

                    // ── Row 3: File: [ file field ] ──
                    EditorGUI.LabelField(
                        new Rect(rect.x, y, labelWidth, lineHeight),
                        "File:", labelStyle
                    );

                    EditorGUI.PropertyField(
                        new Rect(rect.x + labelWidth, y, fullWidth - labelWidth, lineHeight),
                        fileProp,
                        GUIContent.none
                    );
                }

                // ════════════════════════════════════════════
                // OTHER CHAPTERS — 2 rows
                // ════════════════════════════════════════════
                else
                {
                    float labelWidth = 75f;
                    float pillWidth  = Mathf.Clamp(
                        (!string.IsNullOrEmpty(idProp.stringValue) ? idProp.stringValue.Length : 5) * 7.5f + 12f,
                        48f, 140f
                    );

                    // ── Row 1: Chapter ID: [ pill ] ──────────────── [ x ] ──
                    EditorGUI.LabelField(
                        new Rect(rect.x, y, labelWidth, lineHeight),
                        "Chapter ID:", labelStyle
                    );

                    Rect pillRect = new Rect(rect.x + labelWidth, y + 1f, pillWidth, lineHeight - 2f);
                    Color pillBg  = !string.IsNullOrEmpty(idProp.stringValue)
                        ? new Color(0.22f, 0.45f, 0.55f, 0.9f)
                        : new Color(0.5f, 0.2f, 0.2f, 0.9f);

                    EditorGUI.DrawRect(pillRect, pillBg);
                    EditorGUI.LabelField(pillRect,
                        !string.IsNullOrEmpty(idProp.stringValue) ? idProp.stringValue : "no id",
                        pillStyle
                    );

                    // Remove button [ x ] on the right of row 1
                    Rect removeRect = new Rect(
                        rect.x + fullWidth - removeButtonWidth,
                        y,
                        removeButtonWidth,
                        lineHeight
                    );

                    if (GUI.Button(removeRect, "✕", chapterRemoveStyle))
                    {
                        if (EditorUtility.DisplayDialog(
                            "Remove Chapter",
                            $"Remove chapter '{(!string.IsNullOrEmpty(idProp.stringValue) ? idProp.stringValue : "unnamed")}'?",
                            "Remove", "Cancel"))
                        {
                            Undo.RecordObject((ConversationAsset)target, "Remove Chapter");
                            chaptersProp.DeleteArrayElementAtIndex(index);
                            serializedObject.ApplyModifiedProperties();
                            GUIUtility.ExitGUI();
                        }
                    }

                    y += lineHeight + spacing;

                    // ── Row 2: File: [ file field ] ──
                    EditorGUI.LabelField(
                        new Rect(rect.x, y, labelWidth, lineHeight),
                        "File:", labelStyle
                    );

                    EditorGUI.PropertyField(
                        new Rect(rect.x + labelWidth, y, fullWidth - labelWidth, lineHeight),
                        fileProp,
                        GUIContent.none
                    );
                }
            };

            chapterList.elementHeightCallback = index =>
            {
                float lineHeight = EditorGUIUtility.singleLineHeight;
                float spacing    = EditorGUIUtility.standardVerticalSpacing;

                if (index == 0)
                    return (lineHeight * 3) + (spacing * 4) + 4f;

                return (lineHeight * 2) + (spacing * 3) + 4f;
            };

            chapterList.drawElementBackgroundCallback = (rect, index, isActive, isFocused) =>
            {
                if (index < 0) return;

                Color bg = index % 2 == 0
                    ? new Color(0.2f, 0.2f, 0.2f, 0.3f)
                    : new Color(0.15f, 0.15f, 0.15f, 0.3f);

                if (isActive)
                    bg = new Color(0.18f, 0.35f, 0.45f, 0.5f);

                EditorGUI.DrawRect(rect, bg);
            };

            chapterList.onRemoveCallback = list => { };

            chapterList.drawFooterCallback = rect =>
            {
                float buttonHeight = 24f;
                Rect buttonRect    = new Rect(rect.x, rect.y + 4f, rect.width, buttonHeight);

                if (GUI.Button(buttonRect, "+ Add Chapter", chapterAddButtonStyle))
                {
                    Undo.RecordObject((ConversationAsset)target, "Add Chapter");
                    chaptersProp.arraySize++;
                    serializedObject.ApplyModifiedProperties();
                }
            };

            chapterList.footerHeight = 32f;
        }

        // =========================
        // PROFILE
        // =========================

        private void DrawProfile(ConversationAsset asset)
        {
            DrawSection(Labels.SECTION_PROFILE);

            DrawCard(() =>
            {
                // Name is read-only here — edited in Chapters tab
                EditorGUILayout.LabelField("Name", asset.characterName);

                GUILayout.Space(4);

                EditorGUILayout.PropertyField(characterAgeProp,       new GUIContent("Age"));
                EditorGUILayout.PropertyField(birthdateProp,          new GUIContent("Birthdate"));
                EditorGUILayout.PropertyField(occupationProp,         new GUIContent("Occupation"));
                EditorGUILayout.PropertyField(relationshipStatusProp, new GUIContent("Relationship"));

                GUILayout.Space(6);

                EditorGUILayout.LabelField("Bio");
                EditorGUILayout.PropertyField(bioProp, GUIContent.none);

                GUILayout.Space(4);

                EditorGUILayout.LabelField("Description");
                EditorGUILayout.PropertyField(descriptionProp, GUIContent.none);

                GUILayout.Space(4);

                EditorGUILayout.LabelField("Personality Traits");
                EditorGUILayout.PropertyField(personalityTraitsProp, GUIContent.none);
            });
        }

        // =========================
        // CG
        // =========================

        private void BuildCGList()
        {
            cgList = new ReorderableList(
                serializedObject,
                cgKeysProp,
                draggable:    true,
                displayHeader: true,
                displayAddButton: true,
                displayRemoveButton: true
            );

            cgList.drawHeaderCallback = rect =>
                EditorGUI.LabelField(rect, "CG Addressable Keys");

            cgList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                SerializedProperty element = cgKeysProp.GetArrayElementAtIndex(index);

                EditorGUI.PropertyField(
                    new Rect(rect.x, rect.y + 2, rect.width, EditorGUIUtility.singleLineHeight),
                    element,
                    GUIContent.none
                );
            };

            cgList.onAddCallback = list =>
            {
                list.serializedProperty.arraySize++;
                serializedObject.ApplyModifiedProperties();
            };

            cgList.onRemoveCallback = list =>
            {
                Undo.RecordObject((ConversationAsset)target, "Remove CG Key");
                ReorderableList.defaultBehaviours.DoRemoveButton(list);
                serializedObject.ApplyModifiedProperties();
            };
        }
        
        private void DrawCG(ConversationAsset asset)
        {
            DrawSection(Labels.SECTION_CG);

            DrawCard(() =>
            {
                cgList.DoLayoutList();
                
                if (GUILayout.Button("Clear All"))
                {
                    Undo.RecordObject(asset, "Clear CG Keys");
                    cgKeysProp.ClearArray();
                    serializedObject.ApplyModifiedProperties();
                }
            });
        }

        // =========================
        // TOOLS
        // =========================

        private void DrawTools(ConversationAsset asset)
        {
            DrawSection(Labels.SECTION_TOOLS);

            DrawCard(() =>
            {
                if (GUILayout.Button("Refresh Chapter IDs"))
                {
                    Undo.RecordObject(asset, "Refresh Chapter IDs");
                    foreach (var entry in asset.chapters)
                    {
                        if (entry.file != null)
                            entry.chapterId = ConversationAssetEditorUtils.ReadChapterIdFromBub(entry.file);
                    }
                    EditorUtility.SetDirty(asset);
                    Debug.Log($"[ConversationAsset] '{asset.characterName}' — Chapter IDs refreshed.");
                }

                if (GUILayout.Button("Auto-Fill CG from Folder"))
                {
                    string path = EditorUtility.OpenFolderPanel("Select CG Folder", "Assets", "");
                    if (!string.IsNullOrEmpty(path))
                    {
                        Undo.RecordObject(asset, "Auto-Fill CG from Folder");
                        ConversationAssetEditorUtils.FillCGFromFolder(asset, path);
                        EditorUtility.SetDirty(asset);
                    }
                }

                if (GUILayout.Button("Validate"))
                {
                    var warnings = ConversationAssetEditorUtils.Validate(asset);

                    if (warnings.Count == 0)
                    {
                        Debug.Log($"[ConversationAsset] '{asset.characterName}' — Validation passed. No issues found.");
                    }
                    else
                    {
                        foreach (var w in warnings)
                            Debug.LogWarning($"[ConversationAsset] '{asset.characterName}' — {w}");
                    }
                }
            });
        }

        // =========================
        // UI HELPERS
        // =========================
        private void DrawCard(System.Action content)
        {
            Rect rect = EditorGUILayout.BeginVertical(cardStyle);
            EditorGUI.DrawRect(rect, new Color(0.18f, 0.18f, 0.18f));

            GUILayout.Space(4);
            content?.Invoke();
            GUILayout.Space(2);

            EditorGUILayout.EndVertical();
        }

        private void DrawSection(string title)
        {
            GUILayout.Space(6);
            EditorGUILayout.LabelField(title, sectionStyle);
            DrawLine();
            GUILayout.Space(4);
        }

        private void DrawLine()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(1f, 1f, 1f, 0.05f));
        }
    }
}
#endif