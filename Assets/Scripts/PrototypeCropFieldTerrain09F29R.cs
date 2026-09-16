using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f29r
// Crop-field terrain foundation. Fields provide concealment, not ballistic cover:
// targets are a little harder to acquire/hit, movement is slightly slower, and firing
// briefly reveals a concealed formation through black-powder smoke/activity.
[DefaultExecutionOrder(1950)]
public sealed class PrototypeCropFieldTerrain09F29R : MonoBehaviour
{
    public readonly struct FieldDescriptor
    {
        public readonly Vector2 Center;
        public readonly float Width;
        public readonly float Depth;
        public readonly float Yaw;
        public readonly float Density;
        public readonly string Label;

        public FieldDescriptor(Vector2 center, float width, float depth, float yaw, float density, string label)
        {
            Center = center;
            Width = width;
            Depth = depth;
            Yaw = yaw;
            Density = Mathf.Clamp01(density);
            Label = label;
        }
    }

    public static PrototypeCropFieldTerrain09F29R Instance { get; private set; }

    private static readonly FieldDescriptor[] fieldDescriptors =
    {
        new FieldDescriptor(new Vector2(-390f, -230f), 250f, 135f, -7f, 0.92f, "Kornmark A"),
        new FieldDescriptor(new Vector2(-340f, 245f), 220f, 120f, 5f, 0.82f, "Kornmark B"),
        new FieldDescriptor(new Vector2(370f, 250f), 245f, 135f, -4f, 0.90f, "Kornmark C"),
        new FieldDescriptor(new Vector2(430f, -235f), 260f, 145f, 8f, 0.86f, "Kornmark D"),
        new FieldDescriptor(new Vector2(-60f, 330f), 190f, 105f, 3f, 0.78f, "Kornmark E"),
        new FieldDescriptor(new Vector2(95f, -350f), 210f, 115f, -5f, 0.88f, "Kornmark F")
    };

    private static readonly Dictionary<Regiment, float> revealUntil = new Dictionary<Regiment, float>();

    private Material fieldBaseMaterial;
    private Material cropMaterial;
    private Material cropDarkMaterial;
    private FieldInfo moveSpeedField;
    private bool visualsBuilt;

    private const float MovementMultiplierDense = 0.92f;
    private const float VolleyRevealSeconds = 4.5f;
    private const float ConcealedVisibilityMultiplier = 0.84f;
    private const float RevealedVisibilityMultiplier = 0.96f;

    public static IReadOnlyList<FieldDescriptor> Fields => fieldDescriptors;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeCropFieldTerrain09F29R>() == null)
            new GameObject("PrototypeCropFieldTerrain_v000009f29r")
                .AddComponent<PrototypeCropFieldTerrain09F29R>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        revealUntil.Clear();
        moveSpeedField = typeof(Regiment).GetField("moveSpeed", BindingFlags.Instance | BindingFlags.NonPublic);

        fieldBaseMaterial = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.63f, 0.53f, 0.17f), "09F29R_FieldBase");
        cropMaterial = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.78f, 0.67f, 0.20f), "09F29R_Crop");
        cropDarkMaterial = PrototypeBootstrap.CreateSharedMaterial(
            new Color(0.58f, 0.48f, 0.13f), "09F29R_CropDark");

        Debug.Log(
            "CROP-FIELD-09F29R|Installed=True|Fields=" + fieldDescriptors.Length +
            "|Move=x" + MovementMultiplierDense.ToString("0.00") +
            "|Visibility=x" + ConcealedVisibilityMultiplier.ToString("0.00") +
            "|VolleyReveal=" + VolleyRevealSeconds.ToString("0.0") + "s|Cover=False");
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (!visualsBuilt && BattleManager.Instance != null)
        {
            visualsBuilt = true;
            BuildFieldVisuals();
        }

        ApplyMovementFriction();
        CleanupRevealState();
    }

    private void ApplyMovementFriction()
    {
        // F7 rewrites the normal/forced/rout speed every frame at execution order 1850.
        // This pass deliberately runs immediately afterwards and only scales that value,
        // so it does not own movement or create a competing route writer. Charge later
        // reasserts its own speed in LateUpdate and therefore keeps charge authority.
        if (moveSpeedField == null || PrototypeForcedMarch09F7.Instance == null)
            return;

        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null)
                continue;

            float multiplier = GetMovementMultiplier(regiment.transform.position);
            if (multiplier >= 0.999f)
                continue;

            object raw = moveSpeedField.GetValue(regiment);
            if (raw is float speed)
                moveSpeedField.SetValue(regiment, speed * multiplier);
        }
    }

    private static void CleanupRevealState()
    {
        if (revealUntil.Count == 0)
            return;

        List<Regiment> stale = null;
        foreach (KeyValuePair<Regiment, float> pair in revealUntil)
        {
            if (pair.Key != null && Time.time <= pair.Value)
                continue;
            if (stale == null)
                stale = new List<Regiment>();
            stale.Add(pair.Key);
        }

        if (stale != null)
            for (int i = 0; i < stale.Count; i++)
                revealUntil.Remove(stale[i]);
    }

    public static bool IsInCropField(Vector3 world)
    {
        FieldDescriptor ignored;
        return TryGetField(world, out ignored);
    }

    public static bool TryGetField(Vector3 world, out FieldDescriptor field)
    {
        for (int i = 0; i < fieldDescriptors.Length; i++)
        {
            FieldDescriptor candidate = fieldDescriptors[i];
            if (!Contains(candidate, world))
                continue;
            field = candidate;
            return true;
        }

        field = default;
        return false;
    }

    public static float GetMovementMultiplier(Vector3 world)
    {
        if (!TryGetField(world, out FieldDescriptor field))
            return 1f;

        // Lighter crops retain a smaller penalty; dense ripe crop reaches the 8% QA target.
        return Mathf.Lerp(0.96f, MovementMultiplierDense, field.Density);
    }

    public static float GetVisibilityRangeMultiplier(Regiment target)
    {
        if (target == null || !TryGetField(target.transform.position, out FieldDescriptor field))
            return 1f;

        float baseMultiplier = IsTemporarilyRevealed(target)
            ? RevealedVisibilityMultiplier
            : ConcealedVisibilityMultiplier;

        return Mathf.Lerp(1f, baseMultiplier, field.Density);
    }

    public static float GetTargetAccuracyMultiplier(Regiment shooter, Regiment target, float distance)
    {
        if (shooter == null || target == null || !TryGetField(target.transform.position, out FieldDescriptor field))
            return 1f;

        float multiplier;
        if (distance <= shooter.CloseRange)
        {
            multiplier = 0.98f;
        }
        else if (distance <= shooter.EffectiveRange)
        {
            float t = Mathf.InverseLerp(shooter.CloseRange, shooter.EffectiveRange, distance);
            multiplier = Mathf.Lerp(0.98f, 0.95f, t);
        }
        else
        {
            float t = Mathf.InverseLerp(shooter.EffectiveRange, shooter.MaximumRange, distance);
            multiplier = Mathf.Lerp(0.95f, 0.90f, t);
        }

        if (IsTemporarilyRevealed(target))
            multiplier = Mathf.Lerp(multiplier, 1f, 0.65f);

        return Mathf.Lerp(1f, multiplier, field.Density);
    }

    public static void NotifyVolley(Regiment shooter)
    {
        if (shooter == null || !IsInCropField(shooter.transform.position))
            return;

        revealUntil[shooter] = Time.time + VolleyRevealSeconds;
        Debug.Log("CROP-FIELD-09F29R|Reveal=True|Unit=" + shooter.RegimentName +
                  "|Reason=VOLLEY|Seconds=" + VolleyRevealSeconds.ToString("0.0"));
    }

    public static bool IsTemporarilyRevealed(Regiment regiment)
    {
        return regiment != null && revealUntil.TryGetValue(regiment, out float until) && Time.time <= until;
    }

    private static bool Contains(FieldDescriptor field, Vector3 world)
    {
        Vector2 delta = new Vector2(world.x - field.Center.x, world.z - field.Center.y);
        float radians = -field.Yaw * Mathf.Deg2Rad;
        float c = Mathf.Cos(radians);
        float s = Mathf.Sin(radians);
        float localX = c * delta.x - s * delta.y;
        float localZ = s * delta.x + c * delta.y;

        return Mathf.Abs(localX) <= field.Width * 0.5f &&
               Mathf.Abs(localZ) <= field.Depth * 0.5f;
    }

    private void BuildFieldVisuals()
    {
        GameObject root = new GameObject("CropFields09F29R");

        for (int i = 0; i < fieldDescriptors.Length; i++)
            BuildField(root.transform, fieldDescriptors[i], i);
    }

    private void BuildField(Transform parent, FieldDescriptor field, int fieldIndex)
    {
        float centerY = PrototypeBootstrap.SampleGroundHeight(field.Center.x, field.Center.y);

        GameObject fieldRoot = new GameObject("CropField09F29R_" + (fieldIndex + 1));
        fieldRoot.transform.SetParent(parent, false);
        fieldRoot.transform.position = new Vector3(field.Center.x, centerY, field.Center.y);
        fieldRoot.transform.rotation = Quaternion.Euler(0f, field.Yaw, 0f);

        GameObject basePlane = GameObject.CreatePrimitive(PrimitiveType.Cube);
        basePlane.name = "CropFieldBase09F29R";
        basePlane.transform.SetParent(fieldRoot.transform, false);
        basePlane.transform.localPosition = new Vector3(0f, 0.035f, 0f);
        basePlane.transform.localScale = new Vector3(field.Width, 0.035f, field.Depth);
        Renderer baseRenderer = basePlane.GetComponent<Renderer>();
        if (baseRenderer != null)
            baseRenderer.sharedMaterial = fieldBaseMaterial;
        RemoveCollider(basePlane);

        int rows = Mathf.Clamp(Mathf.RoundToInt(field.Depth / 11f), 8, 14);
        for (int row = 0; row < rows; row++)
        {
            float z = Mathf.Lerp(-field.Depth * 0.46f, field.Depth * 0.46f,
                rows <= 1 ? 0.5f : row / (float)(rows - 1));

            float rowWidth = field.Width * (row % 3 == 0 ? 0.90f : row % 3 == 1 ? 0.96f : 0.86f);
            float localX = (row % 2 == 0 ? -1f : 1f) * Mathf.Min(6f, field.Width * 0.025f);
            Vector3 localCenter = new Vector3(localX, 0f, z);
            Vector3 worldCenter = fieldRoot.transform.TransformPoint(localCenter);
            float ground = PrototypeBootstrap.SampleGroundHeight(worldCenter.x, worldCenter.z);

            GameObject strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            strip.name = "CropRow09F29R_" + row;
            strip.transform.SetParent(fieldRoot.transform, false);
            strip.transform.localPosition = new Vector3(
                localX,
                ground - centerY + 0.43f,
                z);
            strip.transform.localScale = new Vector3(rowWidth, 0.78f, 0.22f);

            Renderer renderer = strip.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = row % 4 == 0 ? cropDarkMaterial : cropMaterial;
            RemoveCollider(strip);
        }
    }

    private static void RemoveCollider(GameObject go)
    {
        Collider collider = go != null ? go.GetComponent<Collider>() : null;
        if (collider != null)
            UnityEngine.Object.Destroy(collider);
    }
}