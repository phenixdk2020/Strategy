using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f29y
// Adds the exact same tactical company status used by the OOB to the Major bottom HUD.
// This is a presentation-only overlay; it does not change selection, command or AI state.
[DefaultExecutionOrder(-29500)]
public sealed class PrototypeMajorHudStatus09F29Y : MonoBehaviour
{
    private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private const float HudHeight = 90f;

    private FieldInfo selectedBattalionField;
    private PrototypeOobStatus09F29V oobStatus;
    private MethodInfo getStatusMethod;
    private GUIStyle statusStyle;
    private GUIStyle headerStyle;
    private bool installLogged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeMajorHudStatus09F29Y>() == null)
            new GameObject("PrototypeMajorHudStatus_v000009f29y")
                .AddComponent<PrototypeMajorHudStatus09F29Y>();
    }

    private void Awake()
    {
        selectedBattalionField = typeof(PrototypeRegimentHierarchy09F27)
            .GetField("selectedBattalion", AnyInstance);
        getStatusMethod = typeof(PrototypeOobStatus09F29V)
            .GetMethod("GetStatus", AnyInstance);
    }

    private void Update()
    {
        if (oobStatus == null)
            oobStatus = UnityEngine.Object.FindAnyObjectByType<PrototypeOobStatus09F29V>();

        if (!installLogged && oobStatus != null && getStatusMethod != null)
        {
            installLogged = true;
            Debug.Log("MAJOR-HUD-STATUS-09F29Y|Installed=True|Source=OOB-F29V|SharedTacticalStatus=True");
        }
    }

    private void OnGUI()
    {
        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        if (hierarchy == null || !hierarchy.Installed || selectedBattalionField == null)
            return;

        object selectedValue = selectedBattalionField.GetValue(hierarchy);
        if (!(selectedValue is int))
            return;

        int battalionIndex = (int)selectedValue;
        if (battalionIndex < 0 || battalionIndex >= hierarchy.BattalionCount)
            return;

        IReadOnlyList<Regiment> companies = hierarchy.GetCompanies(battalionIndex);
        if (companies == null)
            return;

        EnsureStyles();
        GUI.depth = -6100;

        float width = Screen.width;
        float companyWidth = Mathf.Clamp(width * 0.34f, 390f, 540f);
        float companyX = width - companyWidth - 6f;
        float panelY = Screen.height - HudHeight;
        float y = panelY + 21f;
        float patchWidth = Mathf.Clamp(companyWidth * 0.22f, 82f, 106f);
        float patchX = companyX + companyWidth - patchWidth - 4f;

        DrawPatch(new Rect(patchX, y, patchWidth, 10f));
        GUI.Label(new Rect(patchX + 1f, y - 1f, patchWidth - 2f, 11f), "STATUS", headerStyle);

        for (int i = 0; i < companies.Count && i < 4; i++)
        {
            Regiment unit = companies[i];
            if (unit == null)
                continue;

            string status = ResolveStatus(hierarchy, unit);
            Rect row = new Rect(patchX, y + 11f + i * 13f, patchWidth, 13f);
            DrawPatch(row);
            statusStyle.normal.textColor = StatusColor(status);
            GUI.Label(new Rect(row.x + 2f, row.y, row.width - 4f, row.height), status, statusStyle);
        }
    }

    private string ResolveStatus(PrototypeRegimentHierarchy09F27 hierarchy, Regiment unit)
    {
        if (oobStatus == null)
            oobStatus = UnityEngine.Object.FindAnyObjectByType<PrototypeOobStatus09F29V>();

        if (oobStatus != null && getStatusMethod != null)
        {
            object value = getStatusMethod.Invoke(oobStatus, new object[] { hierarchy, unit });
            if (value is string && !string.IsNullOrEmpty((string)value))
                return (string)value;
        }

        return "KLAR";
    }

    private static void DrawPatch(Rect rect)
    {
        Color old = GUI.color;
        GUI.color = new Color(0.035f, 0.045f, 0.036f, 0.98f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = old;
    }

    private void EnsureStyles()
    {
        if (statusStyle != null)
            return;

        statusStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 8,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleRight
        };

        headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 7,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleRight
        };
        headerStyle.normal.textColor = new Color(0.73f, 0.63f, 0.32f, 1f);
    }

    private static Color StatusColor(string status)
    {
        switch (status)
        {
            case "KLAR":
            case "FORSVAR":
                return new Color(0.62f, 0.86f, 0.58f, 1f);

            case "KAMP":
            case "UNDER ILD":
            case "CHARGE":
            case "MELEE":
            case "ROUT":
                return new Color(1.00f, 0.56f, 0.24f, 1f);

            default:
                return new Color(0.78f, 0.80f, 0.72f, 1f);
        }
    }
}
