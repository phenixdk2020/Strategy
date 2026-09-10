#if UNITY_EDITOR
using System.IO;
using CesiumForUnity;
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
        window.minSize = new Vector2(620f, 290f);
        window.Show();
    }

    private static string TokenPath
    {
        get { return Path.Combine(Application.persistentDataPath, "PROJECT1864", "Cesium", "ion-token.txt"); }
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10f);
        EditorGUILayout.LabelField("Cesium ion token", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "PROJECT 1864 bruger en eksplicit runtime-token, så Cesium aldrig starter asset requests med access_token tom. " +
            "Token gemmes kun lokalt under Unitys persistentDataPath og bliver ikke skrevet til GitHub.",
            MessageType.Info);

        CesiumIonServer server = CesiumIonServer.defaultServer;
        bool hasProjectDefault = server != null && !string.IsNullOrWhiteSpace(server.defaultIonAccessToken);
        bool hasLocal = File.Exists(TokenPath) && new FileInfo(TokenPath).Length > 0;

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Cesium Project Default Token", hasProjectDefault ? "FUNDET" : "IKKE FUNDET");
        EditorGUILayout.LabelField("PROJECT 1864 lokal token", hasLocal ? "FUNDET" : "IKKE FUNDET");

        EditorGUILayout.Space(8f);
        if (GUILayout.Button("Importér Cesium Project Default Token → lokal PROJECT 1864 token", GUILayout.Height(32f)))
            ImportProjectDefaultToken();

        EditorGUILayout.Space(8f);
        token = EditorGUILayout.PasswordField("Token (manuel)", token);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Gem manuel token", GUILayout.Height(30f)))
                SaveToken();

            if (GUILayout.Button("Fjern lokal token", GUILayout.Height(30f)))
                DeleteToken();
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Lokal sti:");
        EditorGUILayout.SelectableLabel(TokenPath, EditorStyles.textField, GUILayout.Height(22f));

        if (!string.IsNullOrEmpty(message))
            EditorGUILayout.HelpBox(message, MessageType.None);
    }

    private static void WriteLocalToken(string value)
    {
        string directory = Path.GetDirectoryName(TokenPath);
        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
        File.WriteAllText(TokenPath, value.Trim());
    }

    private void ImportProjectDefaultToken()
    {
        CesiumIonServer server = CesiumIonServer.defaultServer;
        if (server == null || string.IsNullOrWhiteSpace(server.defaultIonAccessToken))
        {
            message = "Cesiums Project Default Token indeholder ingen token-værdi. Åbn Cesium-panelet, vælg Token og brug en eksisterende token som Project Default Token — eller indsæt token manuelt her.";
            return;
        }

        WriteLocalToken(server.defaultIonAccessToken);
        message = "Project Default Token importeret lokalt. Stop/start Play Mode. 13n3 vil nu binde token eksplicit før asset 1/2 startes.";
        Repaint();
    }

    private void SaveToken()
    {
        string value = token == null ? string.Empty : token.Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            message = "Indtast en token først.";
            return;
        }

        WriteLocalToken(value);
        token = string.Empty;
        message = "Token gemt lokalt. Stop/start Play Mode for at genindlæse Cesium.";
        Repaint();
    }

    private void DeleteToken()
    {
        if (File.Exists(TokenPath)) File.Delete(TokenPath);
        token = string.Empty;
        message = "Lokal token er fjernet.";
        Repaint();
    }
}
#endif
