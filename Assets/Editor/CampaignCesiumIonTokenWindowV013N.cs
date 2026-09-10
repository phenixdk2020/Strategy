#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public sealed class CampaignCesiumIonTokenWindowV013N : EditorWindow
{
    private string token = string.Empty;
    private string message = string.Empty;

    [MenuItem("PROJECT 1864/Campaign/Cesium ion token (lokal)")]
    public static void Open()
    {
        CampaignCesiumIonTokenWindowV013N window = GetWindow<CampaignCesiumIonTokenWindowV013N>(true, "PROJECT 1864 · Cesium ion", true);
        window.minSize = new Vector2(560f, 215f);
        window.Show();
    }

    private static string TokenPath
    {
        get
        {
            return Path.Combine(Application.persistentDataPath, "PROJECT1864", "Cesium", "ion-token.txt");
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10f);
        EditorGUILayout.LabelField("Cesium ion token", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Token gemmes KUN lokalt under Unitys persistentDataPath og bliver ikke skrevet til GitHub. " +
            "Opret/brug en Cesium ion access token med adgang til Cesium World Terrain og Bing Maps Aerial.",
            MessageType.Info);

        EditorGUILayout.Space(6f);
        token = EditorGUILayout.PasswordField("Token", token);

        EditorGUILayout.Space(8f);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Gem lokal token", GUILayout.Height(30f)))
                SaveToken();

            if (GUILayout.Button("Fjern lokal token", GUILayout.Height(30f)))
                DeleteToken();
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.SelectableLabel(TokenPath, EditorStyles.textField, GUILayout.Height(22f));

        if (!string.IsNullOrEmpty(message))
            EditorGUILayout.HelpBox(message, MessageType.None);
    }

    private void SaveToken()
    {
        string value = token == null ? string.Empty : token.Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            message = "Indtast en token først.";
            return;
        }

        string directory = Path.GetDirectoryName(TokenPath);
        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
        File.WriteAllText(TokenPath, value);
        token = string.Empty;
        message = "Token gemt lokalt. Stop/start Play Mode for at genindlæse Cesium.";
    }

    private void DeleteToken()
    {
        if (File.Exists(TokenPath)) File.Delete(TokenPath);
        token = string.Empty;
        message = "Lokal token er fjernet.";
    }
}
#endif
