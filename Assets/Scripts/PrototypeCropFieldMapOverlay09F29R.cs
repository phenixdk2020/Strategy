using System.Reflection;
using UnityEngine;

// v00.00.09f29r
// Draws the same crop-field geometry used by gameplay as a subtle yellow overlay on
// the F29P tactical map. It is presentation-only and deliberately ignores small yaw
// angles in the miniature fill while retaining the true world geometry for simulation.
[DefaultExecutionOrder(39100)]
public sealed class PrototypeCropFieldMapOverlay09F29R : MonoBehaviour
{
    private struct MapBounds
    {
        public float MinX, MaxX, MinZ, MaxZ;
    }

    private FieldInfo mapVisibleField;
    private Texture2D yellowTexture;

    private const float PanelWidth = 270f;
    private const float PanelHeight = 188f;
    private const float BottomGuard = 104f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeCropFieldMapOverlay09F29R>() == null)
            new GameObject("PrototypeCropFieldMapOverlay_v000009f29r")
                .AddComponent<PrototypeCropFieldMapOverlay09F29R>();
    }

    private void Awake()
    {
        mapVisibleField = typeof(PrototypeCameraNavigation09F29P).GetField(
            "mapVisible", BindingFlags.Instance | BindingFlags.NonPublic);

        yellowTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        yellowTexture.hideFlags = HideFlags.HideAndDontSave;
        yellowTexture.SetPixel(0, 0, new Color(0.92f, 0.75f, 0.17f, 1f));
        yellowTexture.Apply(false, true);
    }

    private void OnGUI()
    {
        PrototypeCameraNavigation09F29P navigation =
            UnityEngine.Object.FindAnyObjectByType<PrototypeCameraNavigation09F29P>();
        if (navigation == null || !navigation.enabled)
            return;

        if (mapVisibleField != null)
        {
            object value = mapVisibleField.GetValue(navigation);
            if (value is bool visible && !visible)
                return;
        }

        GUI.depth = -70010;

        float panelX = Mathf.Max(8f, Screen.width - PanelWidth - 10f);
        float panelY = Mathf.Max(66f, Screen.height - BottomGuard - PanelHeight - 8f);
        Rect map = new Rect(panelX + 8f, panelY + 25f, PanelWidth - 16f, 112f);
        MapBounds bounds = CalculateMapBounds();

        Color old = GUI.color;
        IReadOnlyFieldLoop(map, bounds);
        GUI.color = old;
    }

    private void IReadOnlyFieldLoop(Rect map, MapBounds bounds)
    {
        System.Collections.Generic.IReadOnlyList<PrototypeCropFieldTerrain09F29R.FieldDescriptor> fields =
            PrototypeCropFieldTerrain09F29R.Fields;

        for (int i = 0; i < fields.Count; i++)
        {
            PrototypeCropFieldTerrain09F29R.FieldDescriptor field = fields[i];
            Vector2 min = WorldToMap(map, bounds,
                new Vector3(field.Center.x - field.Width * 0.5f, 0f, field.Center.y - field.Depth * 0.5f));
            Vector2 max = WorldToMap(map, bounds,
                new Vector3(field.Center.x + field.Width * 0.5f, 0f, field.Center.y + field.Depth * 0.5f));

            Rect rect = Rect.MinMaxRect(
                Mathf.Min(min.x, max.x),
                Mathf.Min(min.y, max.y),
                Mathf.Max(min.x, max.x),
                Mathf.Max(min.y, max.y));

            Rect clipped = Intersect(rect, map);
            if (clipped.width <= 0f || clipped.height <= 0f)
                continue;

            GUI.color = new Color(1f, 1f, 1f, 0.20f + field.Density * 0.08f);
            GUI.DrawTexture(clipped, yellowTexture);

            GUI.color = new Color(0.96f, 0.78f, 0.18f, 0.72f);
            DrawBorder(clipped, 1f);
        }
    }

    private static MapBounds CalculateMapBounds()
    {
        BattleManager battle = BattleManager.Instance;
        float minX = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float minZ = float.PositiveInfinity;
        float maxZ = float.NegativeInfinity;

        if (battle != null && battle.Regiments != null)
        {
            foreach (Regiment unit in battle.Regiments)
            {
                if (unit == null || unit.CurrentStrength <= 0)
                    continue;
                Vector3 p = unit.transform.position;
                minX = Mathf.Min(minX, p.x);
                maxX = Mathf.Max(maxX, p.x);
                minZ = Mathf.Min(minZ, p.z);
                maxZ = Mathf.Max(maxZ, p.z);
            }
        }

        if (float.IsInfinity(minX))
        {
            minX = -450f; maxX = 450f;
            minZ = -300f; maxZ = 300f;
        }

        float centerX = (minX + maxX) * 0.5f;
        float centerZ = (minZ + maxZ) * 0.5f;
        float width = Mathf.Clamp(Mathf.Max(900f, maxX - minX + 360f), 900f, PrototypeBootstrap.BattlefieldWidth);
        float depth = Mathf.Clamp(Mathf.Max(620f, maxZ - minZ + 300f), 620f, PrototypeBootstrap.BattlefieldDepth);

        centerX = Mathf.Clamp(centerX,
            -PrototypeBootstrap.BattlefieldHalfWidth + width * 0.5f,
            PrototypeBootstrap.BattlefieldHalfWidth - width * 0.5f);
        centerZ = Mathf.Clamp(centerZ,
            -PrototypeBootstrap.BattlefieldHalfDepth + depth * 0.5f,
            PrototypeBootstrap.BattlefieldHalfDepth - depth * 0.5f);

        return new MapBounds
        {
            MinX = centerX - width * 0.5f,
            MaxX = centerX + width * 0.5f,
            MinZ = centerZ - depth * 0.5f,
            MaxZ = centerZ + depth * 0.5f
        };
    }

    private static Vector2 WorldToMap(Rect rect, MapBounds bounds, Vector3 world)
    {
        float tx = Mathf.InverseLerp(bounds.MinX, bounds.MaxX, world.x);
        float tz = Mathf.InverseLerp(bounds.MinZ, bounds.MaxZ, world.z);
        return new Vector2(rect.x + tx * rect.width, rect.yMax - tz * rect.height);
    }

    private static Rect Intersect(Rect a, Rect b)
    {
        float xMin = Mathf.Max(a.xMin, b.xMin);
        float yMin = Mathf.Max(a.yMin, b.yMin);
        float xMax = Mathf.Min(a.xMax, b.xMax);
        float yMax = Mathf.Min(a.yMax, b.yMax);
        return Rect.MinMaxRect(xMin, yMin, Mathf.Max(xMin, xMax), Mathf.Max(yMin, yMax));
    }

    private static void DrawBorder(Rect rect, float thickness)
    {
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), Texture2D.whiteTexture);
    }
}