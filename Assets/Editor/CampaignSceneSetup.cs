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
    private const string SessionKey = "PROJECT1864.CampaignSceneOpened";

    static CampaignSceneSetup()
    {
        EditorApplication.delayCall += EnsureCampaignSceneAndOpenOnce;
    }

    private static void EnsureCampaignSceneAndOpenOnce()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        EnsureCampaignSceneExists();

        if (SessionState.GetBool(SessionKey, false))
            return;

        SessionState.SetBool(SessionKey, true);

        Scene active = SceneManager.GetActiveScene();
        if (active.path == CampaignScenePath)
            return;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        EditorSceneManager.OpenScene(CampaignScenePath, OpenSceneMode.Single);
    }

    private static void EnsureCampaignSceneExists()
    {
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
            EditorBuildSettings.scenes = EditorBuildSettings.scenes
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
}
#endif
