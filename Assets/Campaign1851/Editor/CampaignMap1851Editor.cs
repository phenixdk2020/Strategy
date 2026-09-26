#if UNITY_EDITOR
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor support for the v00.00.14 painted 1851 map: scene creation, menu entry and a
/// batch-mode capture used for visual QA without entering Play Mode.
/// </summary>
public static class CampaignMap1851Editor
{
    public const string ScenePath = "Assets/Scenes/CampaignMap1851.unity";

    [MenuItem("PROJECT 1864/Open Campaign Map 1851 (malet kort)")]
    private static void OpenScene()
    {
        EnsureScene();
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    /// <summary>Creates the scene: empty except for the campaign-mode marker that keeps the tactical bootstrap out.</summary>
    public static void EnsureScene()
    {
        if (!File.Exists(ScenePath))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            Scene previous = SceneManager.GetActiveScene();
            string previousPath = previous.path;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("CampaignModeMarker").AddComponent<GrandCampaignBootstrap>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();

            if (!string.IsNullOrEmpty(previousPath) && File.Exists(previousPath))
                EditorSceneManager.OpenScene(previousPath, OpenSceneMode.Single);
        }

        if (!EditorBuildSettings.scenes.Any(s => s.path == ScenePath))
        {
            EditorBuildSettings.scenes = EditorBuildSettings.scenes
                .Concat(new[] { new EditorBuildSettingsScene(ScenePath, true) })
                .ToArray();
        }
    }

    /// <summary>
    /// Batch capture. Usage:
    ///   Unity -batchmode -projectPath . -executeMethod CampaignMap1851Editor.CaptureCli
    ///         -map1851Shots "out.png|lat|lon|distanceKm|yaw;out2.png|..."
    /// </summary>
    public static void CaptureCli()
    {
        try
        {
            EnsureScene();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var map = new GameObject("CampaignMap1851").AddComponent<CampaignMap1851>();
            map.Build();

            string shots = Arg("-map1851Shots") ?? "Temp/map1851.png|55.45|10.35|620|0";
            // Warm-up: the canvas adopts the render-texture size only after a first render.
            Capture(map, null, 55.45f, 10.35f, 620f, 0f);
            foreach (string shot in shots.Split(';'))
            {
                string[] p = shot.Split('|');
                float lat = float.Parse(p[1], CultureInfo.InvariantCulture);
                float lon = float.Parse(p[2], CultureInfo.InvariantCulture);
                float distance = float.Parse(p[3], CultureInfo.InvariantCulture);
                float yaw = p.Length > 4 ? float.Parse(p[4], CultureInfo.InvariantCulture) : 0f;
                Capture(map, p[0], lat, lon, distance, yaw);
            }
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            EditorApplication.Exit(1);
        }
    }

    private static void Capture(CampaignMap1851 map, string path, float lat, float lon, float distance, float yaw)
    {
        const int width = 1920, height = 1080;
        Camera cam = map.MapCamera;
        var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
        cam.targetTexture = rt;

        Vector3 target = map.Project(lat, lon);
        target.y = 0f;
        map.CameraRig.SetView(target, distance, yaw);
        // The canvas only adopts the render-texture size on its next layout pass; lay out first,
        // then place labels against the correct canvas rect.
        Canvas.ForceUpdateCanvases();
        map.RefreshView();
        Canvas.ForceUpdateCanvases();
        cam.Render();

        RenderTexture.active = rt;
        var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();
        if (path != null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            File.WriteAllBytes(path, tex.EncodeToPNG());
        }

        RenderTexture.active = null;
        cam.targetTexture = null;
        UnityEngine.Object.DestroyImmediate(rt);
        Debug.Log($"CAMPAIGN-1851|Capture={path}|Distance={distance}");
    }

    private static string Arg(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}

/// <summary>Import settings for the generated map textures (large, uncompressed heights, readable data maps).</summary>
public sealed class CampaignMap1851TextureImport : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if (!assetPath.Contains("/Campaign1851/Resources/Map1851/"))
            return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Default;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.anisoLevel = 4;
        importer.maxTextureSize = 4096;

        var standalone = importer.GetPlatformTextureSettings("Standalone");
        standalone.overridden = true;
        standalone.maxTextureSize = 4096;

        string file = Path.GetFileNameWithoutExtension(assetPath);
        if (file.EndsWith("_Height"))
        {
            importer.sRGBTexture = false;
            importer.isReadable = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            standalone.format = TextureImporterFormat.R16;
        }
        else if (file.EndsWith("_DetailMask"))
        {
            importer.sRGBTexture = false;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.mipmapEnabled = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            standalone.format = TextureImporterFormat.Alpha8;
        }
        else if (file.EndsWith("_Detail"))
        {
            // Tiled across the whole map: wrap, full mip chain so it fades to neutral grey when far away.
            importer.sRGBTexture = true;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.mipmapEnabled = true;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            standalone.format = TextureImporterFormat.BC7;
            standalone.maxTextureSize = 1024;
        }
        else if (file.EndsWith("_Regions"))
        {
            importer.sRGBTexture = false;
            importer.isReadable = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            standalone.format = TextureImporterFormat.R8;
        }
        else
        {
            importer.sRGBTexture = true;
            importer.mipmapEnabled = true;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            standalone.format = TextureImporterFormat.BC7;
        }
        importer.SetPlatformTextureSettings(standalone);
    }
}
#endif
