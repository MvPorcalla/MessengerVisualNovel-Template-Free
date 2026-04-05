#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using BubbleSpinner.Data;

namespace BubbleSpinner.EditorTools
{
    [CustomEditor(typeof(CharacterDatabase))]
    public class CharacterDatabaseEditor : Editor
    {
        private ReorderableList characterList;
        private SerializedProperty allCharactersProp;

        // =========================
        // STYLES
        // =========================
        private GUIStyle cardStyle;
        private GUIStyle sectionStyle;
        private GUIStyle headerStyle;
        private GUIStyle subLabelStyle;
        private GUIStyle rowLabelStyle;
        private GUIStyle addButtonStyle;
        private GUIStyle removeButtonStyle;

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
                    fontSize  = 11,
                    fontStyle = FontStyle.Bold,
                    normal    = { textColor = new Color(0.7f, 0.7f, 0.7f) }
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

            if (rowLabelStyle == null)
            {
                rowLabelStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    fontStyle = FontStyle.Normal,
                    normal    = { textColor = new Color(0.6f, 0.6f, 0.6f) }
                };
            }

            if (addButtonStyle == null)
            {
                addButtonStyle = new GUIStyle(EditorStyles.miniButton)
                {
                    fontSize    = 11,
                    fontStyle   = FontStyle.Bold,
                    fixedHeight = 24f,
                    normal      = { textColor = new Color(0.7f, 0.9f, 0.7f) }
                };
            }

            if (removeButtonStyle == null)
            {
                removeButtonStyle = new GUIStyle(EditorStyles.miniButton)
                {
                    fontStyle = FontStyle.Bold,
                    normal    = { textColor = new Color(0.8f, 0.4f, 0.4f) }
                };
            }
        }

        private void OnEnable()
        {
            allCharactersProp = serializedObject.FindProperty("allCharacters");
            InitStyles();
            BuildCharacterList();
        }

        public override void OnInspectorGUI()
        {
            if (cardStyle == null) InitStyles();

            var db = (CharacterDatabase)target;

            serializedObject.Update();

            DrawHeader(db);
            DrawCharactersSection(db);
            GUILayout.Space(10);
            DrawToolsSection(db);

            serializedObject.ApplyModifiedProperties();
        }

        // =========================
        // HEADER
        // =========================

        private void DrawHeader(CharacterDatabase db)
        {
            GUILayout.Space(5);
            EditorGUILayout.LabelField("Character Database", headerStyle);
            EditorGUILayout.LabelField(
                $"{db.allCharacters.Count} character{(db.allCharacters.Count != 1 ? "s" : "")} registered",
                subLabelStyle
            );
            GUILayout.Space(8);
            DrawLine();
            GUILayout.Space(8);
        }

        // =========================
        // CHARACTERS
        // =========================

        private void DrawCharactersSection(CharacterDatabase db)
        {
            DrawSection("CHARACTERS");
            characterList.DoLayoutList();
        }

        private void BuildCharacterList()
        {
            characterList = new ReorderableList(
                serializedObject,
                allCharactersProp,
                draggable:           true,
                displayHeader:       true,
                displayAddButton:    false,
                displayRemoveButton: false
            );

            characterList.drawHeaderCallback = rect =>
                EditorGUI.LabelField(
                    rect,
                    $"Characters  ({allCharactersProp.arraySize})",
                    EditorStyles.boldLabel
                );

            characterList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                SerializedProperty element = allCharactersProp.GetArrayElementAtIndex(index);
                ConversationAsset asset    = element.objectReferenceValue as ConversationAsset;

                float lineHeight      = EditorGUIUtility.singleLineHeight;
                float spacing         = EditorGUIUtility.standardVerticalSpacing;
                float y               = rect.y + spacing;
                float fullWidth       = rect.width;
                float removeWidth     = 30f;
                float labelWidth      = 55f;
                float fieldWidth      = fullWidth - labelWidth - removeWidth - 6f;

                // ── Row 1: index badge + character name ──
                float badgeWidth = 28f;
                Rect  badgeRect  = new Rect(rect.x, y + 1f, badgeWidth, lineHeight - 2f);
                EditorGUI.DrawRect(badgeRect, new Color(0.3f, 0.3f, 0.3f, 0.85f));

                GUIStyle badgeStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    normal    = { textColor = Color.white }
                };
                EditorGUI.LabelField(badgeRect, $"#{index + 1}", badgeStyle);

                string characterName = asset != null ? asset.characterName : "(empty slot)";
                GUIStyle nameStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    normal = { textColor = asset != null ? Color.white : new Color(0.6f, 0.4f, 0.4f) }
                };
                EditorGUI.LabelField(
                    new Rect(rect.x + badgeWidth + 4f, y, fullWidth - badgeWidth - removeWidth - 4f, lineHeight),
                    characterName,
                    nameStyle
                );

                // ── Remove button on row 1 ──
                Rect removeRect = new Rect(rect.x + fullWidth - removeWidth, y, removeWidth, lineHeight);
                if (GUI.Button(removeRect, "✕", removeButtonStyle))
                {
                    if (EditorUtility.DisplayDialog(
                        "Remove Character",
                        $"Remove '{characterName}' from the database?",
                        "Remove", "Cancel"))
                    {
                        Undo.RecordObject((CharacterDatabase)target, "Remove Character");
                        allCharactersProp.DeleteArrayElementAtIndex(index);
                        serializedObject.ApplyModifiedProperties();
                        GUIUtility.ExitGUI();
                    }
                }

                y += lineHeight + spacing;

                // ── Row 2: Asset field ──
                EditorGUI.LabelField(
                    new Rect(rect.x, y, labelWidth, lineHeight),
                    "Asset:", rowLabelStyle
                );

                EditorGUI.PropertyField(
                    new Rect(rect.x + labelWidth, y, fieldWidth + removeWidth + 6f, lineHeight),
                    element,
                    GUIContent.none
                );
            };

            characterList.elementHeightCallback = index =>
            {
                float lineHeight = EditorGUIUtility.singleLineHeight;
                float spacing    = EditorGUIUtility.standardVerticalSpacing;
                return (lineHeight * 2) + (spacing * 3) + 4f;
            };

            characterList.drawElementBackgroundCallback = (rect, index, isActive, isFocused) =>
            {
                if (index < 0) return;

                Color bg = index % 2 == 0
                    ? new Color(0.2f, 0.2f, 0.2f, 0.3f)
                    : new Color(0.15f, 0.15f, 0.15f, 0.3f);

                if (isActive)
                    bg = new Color(0.18f, 0.35f, 0.45f, 0.5f);

                EditorGUI.DrawRect(rect, bg);
            };

            characterList.onRemoveCallback = list => { };

            characterList.drawFooterCallback = rect =>
            {
                float buttonHeight = 24f;
                Rect  buttonRect   = new Rect(rect.x, rect.y + 4f, rect.width, buttonHeight);

                if (GUI.Button(buttonRect, "+ Add Character", addButtonStyle))
                {
                    Undo.RecordObject((CharacterDatabase)target, "Add Character");
                    allCharactersProp.arraySize++;
                    serializedObject.ApplyModifiedProperties();
                }
            };

            characterList.footerHeight = 32f;
        }

        // =========================
        // TOOLS
        // =========================

        private void DrawToolsSection(CharacterDatabase db)
        {
            DrawSection("TOOLS");

            DrawCard(() =>
            {
                EditorGUILayout.LabelField("Auto-find all ConversationAssets in the project.", rowLabelStyle);
                GUILayout.Space(4);

                if (GUILayout.Button("Auto-Find All Characters"))
                {
                    Undo.RecordObject(db, "Auto-Find All Characters");
                    db.allCharacters.Clear();

                    string[] guids = AssetDatabase.FindAssets("t:ConversationAsset");

                    foreach (string guid in guids)
                    {
                        string path  = AssetDatabase.GUIDToAssetPath(guid);
                        var    asset = AssetDatabase.LoadAssetAtPath<ConversationAsset>(path);
                        if (asset != null)
                            db.allCharacters.Add(asset);
                    }

                    EditorUtility.SetDirty(db);
                    serializedObject.Update();
                    Debug.Log($"[CharacterDatabase] Found {db.allCharacters.Count} characters.");
                }

                GUILayout.Space(6);
                DrawLine();
                GUILayout.Space(6);

                EditorGUILayout.LabelField("Remove all entries from the list.", rowLabelStyle);
                GUILayout.Space(4);

                if (GUILayout.Button("Clear All"))
                {
                    if (EditorUtility.DisplayDialog(
                        "Clear Character Database",
                        $"Remove all {db.allCharacters.Count} characters from the database?",
                        "Clear", "Cancel"))
                    {
                        Undo.RecordObject(db, "Clear Characters");
                        allCharactersProp.ClearArray();
                        serializedObject.ApplyModifiedProperties();
                        Debug.Log("[CharacterDatabase] Cleared.");
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