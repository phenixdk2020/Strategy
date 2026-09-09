using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(20500)]
public sealed class CampaignHistoricalMapLabelsV013J : MonoBehaviour
{
    private static readonly HashSet<string> Overview = new HashSet<string>(StringComparer.Ordinal)
    {
        "AALBORG", "AARHUS", "FREDERICIA", "ODENSE", "CPH"
    };

    private static readonly HashSet<string> Regional = new HashSet<string>(StringComparer.Ordinal)
    {
        "HJORRING", "VIBORG", "HORSENS", "VEJLE", "KOLDING", "HADERSLEV",
        "DYBBOEL", "SONDERBORG", "NYBORG", "KORSOR", "ROSKILDE"
    };

    private GUIStyle overviewStyle;
    private GUIStyle regionalStyle;
    private GUIStyle localStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!string.Equals(scene.name, "CampaignMap", StringComparison.Ordinal))
            return;
        if (UnityEngine.Object.FindAnyObjectByType<CampaignHistoricalMapLabelsV013J>() != null)
            return;
        new GameObject("CampaignHistoricalMapLabelsV013J").AddComponent<CampaignHistoricalMapLabelsV013J>();
    }

    private void OnGUI()
    {
        if (GameObject.Find("V013J_SmoothTerrain") == null)
            return;

        EnsureStyles();
        Camera cam = Camera.main;
        if (cam == null)
            return;

        float cameraHeight = cam.transform.position.y;
        List<Rect> occupied = new List<Rect>();

        foreach (KeyValuePair<string, CampaignNodeState> pair in CampaignSession.Nodes)
        {
            CampaignNodeState node = pair.Value;
            if (node == null || node.Region != CampaignMapRegion.Denmark)
                continue;
            if (!ShouldShow(node.Id, cameraHeight))
                continue;

            Vector3 world = CampaignGeoProjection.Project3D(node.Latitude, node.Longitude, 0f);
            world.y = SampleGround(world.x, world.z) + 0.22f;
            Vector3 screen = cam.WorldToScreenPoint(world);
            if (screen.z <= 0f)
                continue;

            float guiY = Screen.height - screen.y;
            if (screen.x < 0f || screen.x > Screen.width || guiY < 54f || guiY > Screen.height - 8f)
                continue;

            GUIStyle style = Overview.Contains(node.Id) ? overviewStyle : Regional.Contains(node.Id) ? regionalStyle : localStyle;
            float width = Overview.Contains(node.Id) ? 96f : Regional.Contains(node.Id) ? 82f : 72f;
            Rect rect = FindFreeRect(new Rect(screen.x - width * 0.5f, guiY - 8f, width, 18f), occupied);
            occupied.Add(rect);

            GUIStyle shadow = new GUIStyle(style);
            shadow.normal.textColor = new Color(0f, 0f, 0f, 0.72f);
            GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), node.Name, shadow);
            GUI.Label(rect, node.Name, style);
        }
    }

    private static bool ShouldShow(string id, float h)
    {
        if (h > 72f) return Overview.Contains(id);
        if (h > 28f) return Overview.Contains(id) || Regional.Contains(id);
        return true;
    }

    private static float SampleGround(float x, float z)
    {
        RaycastHit[] hits = Physics.RaycastAll(new Vector3(x, 1000f, z), Vector3.down, 2000f);
        float best = float.NegativeInfinity;
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null || !string.Equals(hit.collider.gameObject.name, "V013J_SmoothTerrain", StringComparison.Ordinal))
                continue;
            if (hit.point.y > best) best = hit.point.y;
        }
        return float.IsNegativeInfinity(best) ? 0.18f : best;
    }

    private void EnsureStyles()
    {
        if (overviewStyle != null)
            return;
        overviewStyle = MakeStyle(12, FontStyle.Bold, new Color(0.97f, 0.95f, 0.86f));
        regionalStyle = MakeStyle(10, FontStyle.Bold, new Color(0.91f, 0.91f, 0.84f));
        localStyle = MakeStyle(9, FontStyle.Normal, new Color(0.84f, 0.85f, 0.78f));
    }

    private static GUIStyle MakeStyle(int size, FontStyle fontStyle, Color color)
    {
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = size,
            fontStyle = fontStyle,
            alignment = TextAnchor.MiddleCenter,
            clipping = TextClipping.Overflow
        };
        style.normal.textColor = color;
        return style;
    }

    private static Rect FindFreeRect(Rect initial, List<Rect> occupied)
    {
        Vector2[] offsets =
        {
            Vector2.zero, new Vector2(0f,-18f), new Vector2(0f,18f),
            new Vector2(-42f,0f), new Vector2(42f,0f),
            new Vector2(-32f,-18f), new Vector2(32f,-18f)
        };

        foreach (Vector2 offset in offsets)
        {
            Rect candidate = new Rect(initial.position + offset, initial.size);
            bool overlap = false;
            foreach (Rect existing in occupied)
            {
                if (candidate.Overlaps(existing)) { overlap = true; break; }
            }
            if (!overlap) return candidate;
        }
        return initial;
    }
}
