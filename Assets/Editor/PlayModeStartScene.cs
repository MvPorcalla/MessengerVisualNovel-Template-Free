// Purpose: Always start Play Mode from Disclaimer scene in Unity Editor
// Location: Assets/Editor/PlayModeStartScene.cs
// Usage: Toggle via Tools > Always Start from Disclaimer Scene

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public class PlayModeStartScene
{
    private const string MENU_PATH = "Tools/Always Start from Disclaimer Scene";
    private const string PREF_KEY = "PlayModeStartScene_Enabled";
    private const string START_SCENE = "Assets/Scenes/00_Disclaimer.unity";
    
    private static bool IsEnabled
    {
        get => EditorPrefs.GetBool(PREF_KEY, true); // Default: enabled
        set => EditorPrefs.SetBool(PREF_KEY, value);
    }
    
    static PlayModeStartScene()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        // Update menu checkmark
        EditorApplication.delayCall += () => Menu.SetChecked(MENU_PATH, IsEnabled);
    }

    [MenuItem(MENU_PATH)]
    private static void ToggleStartScene()
    {
        IsEnabled = !IsEnabled;
        Menu.SetChecked(MENU_PATH, IsEnabled);
        Debug.Log($"[PlayModeStartScene] {(IsEnabled ? "Enabled" : "Disabled")} - Will {(IsEnabled ? "always start" : "start normally")} from {START_SCENE}");
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (!IsEnabled) return;

        if (state == PlayModeStateChange.ExitingEditMode)
        {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            
            SceneAsset startScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(START_SCENE);
            if (startScene != null)
            {
                EditorSceneManager.playModeStartScene = startScene;
                Debug.Log($"[PlayModeStartScene] Will start from: {START_SCENE}");
            }
            else
            {
                Debug.LogError($"[PlayModeStartScene] Scene not found: {START_SCENE}");
            }
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorSceneManager.playModeStartScene = null;
        }
    }
}
#endif