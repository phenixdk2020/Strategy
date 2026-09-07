#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class CampaignSceneSetup
{
    private const string SceneFolder = "Assets/Scenes";
    private const string CampaignScenePath = "Assets/Scenes/CampaignMap.unity";

    static CampaignSceneSetup()
    {
        EditorApplication.delayCall += EnsureCampaignSceneExists;
    }

    private static void EnsureCampaignSceneExists()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        if (!AssetDatabase.IsValidFolder(SceneFolder))
            AssetDatabase.CreateFolder("Assets", "Scenes");

        if (!File.Exists(CampaignScenePath))
        {
            Scene previous = SceneManager.GetActiveScene();
            string previousPath = previous.path;

            Scene campaign = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(campaign, CampaignScenePath);
            AssetDatabase.Refresh();

            if (!string.IsNullOrEmpty(previousPath) && File.Exists(previousPath))
                EditorSceneManager.OpenScene(previousPath, OpenSceneMode.Single);
        }

        if (!EditorBuildSettings.scenes.Any(s => s.path == CampaignScenePath))
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            EditorBuildSettings.scenes = scenes
                .Concat(new[] { new EditorBuildSettingsScene(CampaignScenePath, true) })
                .ToArray();
        }
    }

    [MenuItem("PROJECT 1864/Open Campaign Map")]
    private static void OpenCampaignMap()
    {
        EnsureCampaignSceneExists();

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        EditorSceneManager.OpenScene(CampaignScenePath, OpenSceneMode.Single);
    }

    [MenuItem("PROJECT 1864/Open Tactical Battle")]
    private static void OpenTacticalBattle()
    {
        const string tacticalPath = "Assets/Scenes/PrototypeBattle.unity";
        if (!File.Exists(tacticalPath))
            return;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        EditorSceneManager.OpenScene(tacticalPath, OpenSceneMode.Single);
    }
}
#endif
