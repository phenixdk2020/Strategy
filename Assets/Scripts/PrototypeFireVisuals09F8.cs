using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// v00.00.09f8 fire-range visual authority, hardened by v00.00.09f29n.
// Keeps the 70-degree sector, gives Close/Medium/Long the same side-boundary rays,
// and hides range fans while the company is in Column, physically reforming to Line,
// explicitly charging, or participating in charge-created bayonet melee.
[DefaultExecutionOrder(35000)]
public sealed class PrototypeFireVisuals09F8 : MonoBehaviour
{
    private sealed class FormationState
    {
        public bool Initialized;
        public RegimentFormation LastFormation;
        public float FireReadyAt;
    }

    private struct FormationBounds
    {
        public float MinX, MaxX, MinZ, MaxZ;
        public float Width => Mathf.Max(0.8f, MaxX - MinX + 0.55f);
        public float CenterX => (MinX + MaxX) * 0.5f;
    }

    public static PrototypeFireVisuals09F8 Instance { get; private set; }

    private readonly Dictionary<Regiment, FormationState> states =
        new Dictionary<Regiment, FormationState>();

    private Material lineMaterial;

    private const int ArcSegments = 40;
    private const float GroundOffset = 0.42f;
    private const float ReformSpeedMetresPerSecond = 2.8f;
    private const float ReformSafetySeconds = 0.30f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeFireVisuals09F8>() != null)
            return;

        GameObject root = new GameObject("PrototypeFireVisuals_v000009f8");
        root.AddComponent<PrototypeFireVisuals09F8>();
    }

    private void Awake()
    {
        Instance = this;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Standard");

        lineMaterial = new Material(shader)
        {
            name = "FireVisual09F8_Line",
            color = Color.white
        };

        Debug.Log(
            "FIRE-VISUAL-09F30T|Installed=True|Ranges=35/70/100m|Cone=70deg|ActiveBandEmphasis=True|EnemyQaCones=True|" +
            "SharedSideRays=True|HiddenInColumn=True|HiddenDuringReform=True|" +
            "HiddenDuringCharge=True|HiddenDuringChargeMelee=True");
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void LateUpdate()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null)
                continue;

            bool formationReady = UpdateAndGetFormationReady(regiment);
            bool chargeSuppressed = IsChargeFireSuppressed(regiment);

            // F30T TEST/QA:
            // - Danish infantry shows cones when selected.
            // - Prussian infantry cones remain visible for QA even without selection.
            // - This visual rule does NOT grant LOS/target knowledge.
            bool visible =
                regiment.ShowRange &&
                formationReady &&
                !chargeSuppressed &&
                (regiment.IsSelected || regiment.Team == BattleTeam.Prussia);

            FormationBounds bounds =
                CalculateBounds(regiment.Formation, regiment.CurrentStrength);

            BuildFan(
                regiment,
                regiment.transform.Find("CloseRangeFan"),
                regiment.CloseRange,
                bounds,
                visible,
                regiment.FirePolicy == RegimentFirePolicy.CloseRange,
                0.15f,
                new Color(1.00f, 0.92f, 0.10f, 1.00f));

            BuildFan(
                regiment,
                regiment.transform.Find("MediumRangeFan"),
                regiment.EffectiveRange,
                bounds,
                visible,
                regiment.FirePolicy == RegimentFirePolicy.MediumRange,
                0.18f,
                new Color(1.00f, 0.58f, 0.05f, 1.00f));

            BuildFan(
                regiment,
                regiment.transform.Find("LongRangeFan"),
                regiment.MaximumRange,
                bounds,
                visible,
                regiment.FirePolicy == RegimentFirePolicy.LongRange,
                0.22f,
                new Color(1.00f, 0.16f, 0.04f, 1.00f));
        }
    }

    public bool IsFormationFireReady(Regiment regiment)
    {
        return regiment != null &&
               !IsChargeFireSuppressed(regiment) &&
               UpdateAndGetFormationReady(regiment);
    }

    private static bool IsChargeFireSuppressed(Regiment regiment)
    {
        PrototypeInfantryCharge09F25 charge = PrototypeInfantryCharge09F25.Instance;
        return charge != null &&
               (charge.IsCharging(regiment) || charge.IsChargeMeleeParticipant(regiment));
    }

    private bool UpdateAndGetFormationReady(Regiment regiment)
    {
        if (!states.TryGetValue(regiment, out FormationState state))
        {
            state = new FormationState
            {
                Initialized = true,
                LastFormation = regiment.Formation,
                FireReadyAt = regiment.Formation == RegimentFormation.Line ? 0f : float.PositiveInfinity
            };
            states[regiment] = state;
        }

        if (state.LastFormation != regiment.Formation)
        {
            RegimentFormation previous = state.LastFormation;
            state.LastFormation = regiment.Formation;

            if (regiment.Formation == RegimentFormation.Line)
            {
                float reformSeconds = CalculateReformSeconds(
                    previous,
                    RegimentFormation.Line,
                    regiment.CurrentStrength);
                state.FireReadyAt = Time.time + reformSeconds + ReformSafetySeconds;

                Debug.Log(
                    "FIRE-VISUAL-09F8|Unit=" + regiment.RegimentName +
                    "|Cone=False|Reason=REFORMING_TO_LINE|ReadyIn=" +
                    (reformSeconds + ReformSafetySeconds).ToString("0.0") + "s");
            }
            else
            {
                state.FireReadyAt = float.PositiveInfinity;
                Debug.Log(
                    "FIRE-VISUAL-09F8|Unit=" + regiment.RegimentName +
                    "|Cone=False|Reason=COLUMN");
            }
        }

        return regiment.Formation == RegimentFormation.Line && Time.time >= state.FireReadyAt;
    }

    private static float CalculateReformSeconds(
        RegimentFormation from,
        RegimentFormation to,
        int strength)
    {
        if (from == to)
            return 0f;

        int count = Mathf.Max(1, strength);
        float maxDistance = 0f;
        for (int i = 0; i < count; i++)
        {
            Vector3 a = PrototypeBattleVisuals09F5.GetFormationPosition(from, i, count);
            Vector3 b = PrototypeBattleVisuals09F5.GetFormationPosition(to, i, count);
            maxDistance = Mathf.Max(maxDistance, Vector3.Distance(a, b));
        }

        return maxDistance / Mathf.Max(0.1f, ReformSpeedMetresPerSecond);
    }

    private void BuildFan(
        Regiment regiment,
        Transform fanTransform,
        float range,
        FormationBounds bounds,
        bool visible,
        bool activeRange,
        float width,
        Color color)
    {
        if (fanTransform == null)
            return;

        LineRenderer line = fanTransform.GetComponent<LineRenderer>();
        if (line == null)
            return;

        ConfigureLine(line, width, color, activeRange);
        line.enabled = visible;
        if (!visible)
            return;

        Vector3 forward = PlanarNormalized(regiment.transform.forward, Vector3.forward);
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 pivot = regiment.transform.position;
        Vector3 frontCenter =
            pivot + right * bounds.CenterX + forward * (bounds.MaxZ + 0.35f);

        float halfWidth = bounds.Width * 0.5f;
        Vector3 leftFront = frontCenter - right * halfWidth;
        Vector3 rightFront = frontCenter + right * halfWidth;

        // Each range uses the SAME left/right direction rays. The origin for the
        // arc is interpolated across the whole firing frontage, which prevents the
        // shorter fans from visually pinching inward compared with Long range.
        List<Vector3> points = new List<Vector3>(ArcSegments + 4);
        points.Add(TerrainPoint(leftFront));

        for (int i = 0; i <= ArcSegments; i++)
        {
            float t = i / (float)ArcSegments;
            Vector3 muzzle = Vector3.Lerp(leftFront, rightFront, t);
            float angle = Mathf.Lerp(
                -PrototypeRangeTuning09F8.FireArcHalfAngleDegrees,
                PrototypeRangeTuning09F8.FireArcHalfAngleDegrees,
                t) * Mathf.Deg2Rad;
            Vector3 direction = forward * Mathf.Cos(angle) + right * Mathf.Sin(angle);
            points.Add(TerrainPoint(muzzle + direction * range));
        }

        points.Add(TerrainPoint(rightFront));
        points.Add(TerrainPoint(leftFront));

        line.positionCount = points.Count;
        for (int i = 0; i < points.Count; i++)
            line.SetPosition(i, points[i]);
    }

    private void ConfigureLine(
        LineRenderer line,
        float width,
        Color color,
        bool activeRange)
    {
        line.useWorldSpace = true;
        line.loop = false;

        // F30T: PrototypeFireVisuals is the final LateUpdate authority for infantry
        // cone appearance. Active fire-policy band is deliberately much stronger;
        // other physical ranges remain faint reference geometry.
        float alpha = activeRange ? 0.98f : 0.18f;
        float widthScale = activeRange ? 1.55f : 0.55f;

        color.a = alpha;
        line.widthMultiplier = width * widthScale;
        line.numCapVertices = activeRange ? 3 : 1;
        line.numCornerVertices = activeRange ? 3 : 1;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.sharedMaterial = lineMaterial;
        line.startColor = color;
        line.endColor = color;
    }

    private static FormationBounds CalculateBounds(RegimentFormation formation, int strength)
    {
        int count = Mathf.Max(1, strength);
        Vector3 first = PrototypeBattleVisuals09F5.GetFormationPosition(formation, 0, count);
        FormationBounds bounds = new FormationBounds
        {
            MinX = first.x,
            MaxX = first.x,
            MinZ = first.z,
            MaxZ = first.z
        };

        for (int i = 1; i < count; i++)
        {
            Vector3 p = PrototypeBattleVisuals09F5.GetFormationPosition(formation, i, count);
            bounds.MinX = Mathf.Min(bounds.MinX, p.x);
            bounds.MaxX = Mathf.Max(bounds.MaxX, p.x);
            bounds.MinZ = Mathf.Min(bounds.MinZ, p.z);
            bounds.MaxZ = Mathf.Max(bounds.MaxZ, p.z);
        }

        return bounds;
    }

    private static Vector3 TerrainPoint(Vector3 point)
    {
        point.y = PrototypeBootstrap.SampleGroundHeight(point.x, point.z) + GroundOffset;
        return point;
    }

    private static Vector3 PlanarNormalized(Vector3 value, Vector3 fallback)
    {
        value.y = 0f;
        if (value.sqrMagnitude < 0.001f)
        {
            value = fallback;
            value.y = 0f;
        }
        if (value.sqrMagnitude < 0.001f)
            value = Vector3.forward;
        return value.normalized;
    }
}
