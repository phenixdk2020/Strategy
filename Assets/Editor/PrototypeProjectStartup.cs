#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;

// Campaign branches must never auto-open the tactical PrototypeBattle scene.
// The tactical scene remains available as an explicit menu action only.
public static class PrototypeProjectStartup
{
    [MenuItem("PROJECT 1864/Open Tactical Battle (explicit)")]
    private static void OpenPrototypeBattle()
    {
        const string scenePath = "Assets/Scenes/PrototypeBattle.unity";
        if (!File.Exists(scenePath))
            return;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
    }
}
#endif
