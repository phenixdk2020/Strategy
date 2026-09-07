using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// P0A v00.00.09 TEST visual-only hardening for the infantry fire fan.
/// The authoritative range/fire-arc logic remains in Regiment; this component
/// only makes the already-existing Close/Medium/Long overlays terrain-following
/// and readable from the tactical camera.
/// </summary>
[DefaultExecutionOrder(1000)]
public sealed class PrototypeRangeFanVisualEnhancer : MonoBehaviour
{
    private sealed class FanSet
    {
        public LineRenderer Close;
        public LineRenderer Medium;
        public LineRenderer Long;
        public bool Prepared;
    }

    private readonly Dictionary<Regiment, FanSet> fanSets = new Dictionary<Regiment, FanSet>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeRangeFanVisualEnhancer>() != null)
            return;

        GameObject managerObject = new GameObject("PrototypeRangeFanVisualEnhancer_v00.00.09");
        managerObject.AddComponent<PrototypeRangeFanVisualEnhancer>();
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

            FanSet fans = GetOrCreateFanSet(regiment);
            if (fans == null)
                continue;

            if (!fans.Prepared)
                PrepareFans(fans);

            // Only rebuild visible lines. This keeps the QA overlay cheap while
            // still following facing, movement and the uneven terrain exactly.
            if (fans.Close != null && fans.Close.enabled)
                UpdateArcOnly(regiment, fans.Close, regiment.CloseRange);

            if (fans.Medium != null && fans.Medium.enabled)
                UpdateArcOnly(regiment, fans.Medium, regiment.EffectiveRange);

            if (fans.Long != null && fans.Long.enabled)
                UpdateOuterFan(regiment, fans.Long, regiment.MaximumRange);
        }
    }

    private FanSet GetOrCreateFanSet(Regiment regiment)
    {
        FanSet fans;
        if (fanSets.TryGetValue(regiment, out fans))
            return fans;

        Transform close = regiment.transform.Find("CloseRangeFan");
        Transform medium = regiment.transform.Find("MediumRangeFan");
        Transform longRange = regiment.transform.Find("LongRangeFan");

        if (close == null || medium == null || longRange == null)
            return null;

        fans = new FanSet
        {
            Close = close.GetComponent<LineRenderer>(),
            Medium = medium.GetComponent<LineRenderer>(),
            Long = longRange.GetComponent<LineRenderer>()
        };

        fanSets.Add(regiment, fans);
        return fans;
    }

    private static void PrepareFans(FanSet fans)
    {
        PrepareLine(
            fans.Close,
            0.22f,
            new Color(1.00f, 0.94f, 0.18f),
            "RangeClose_Unlit");

        PrepareLine(
            fans.Medium,
            0.28f,
            new Color(1.00f, 0.58f, 0.06f),
            "RangeMedium_Unlit");

        PrepareLine(
            fans.Long,
            0.38f,
            new Color(1.00f, 0.20f, 0.05f),
            "RangeLong_Unlit");

        fans.Prepared = true;
    }

    private static void PrepareLine(LineRenderer line, float width, Color color, string materialName)
    {
        if (line == null)
            return;

        line.useWorldSpace = true;
        line.loop = false;
        line.widthMultiplier = width;
        line.numCapVertices = 2;
        line.numCornerVertices = 2;
        line.alignment = LineAlignment.View;
        line.textureMode = LineTextureMode.Stretch;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.startColor = color;
        line.endColor = color;

        Shader shader = Shader.Find("Unlit/Color");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        if (shader != null)
        {
            Material material = new Material(shader)
            {
                name = materialName,
                color = color
            };
            line.sharedMaterial = material;
        }
    }

    private static void UpdateArcOnly(Regiment regiment, LineRenderer line, float range)
    {
        const int arcSegments = 48;
        line.positionCount = arcSegments + 1;

        Vector3 forward = GetFlatForward(regiment.transform);
        Vector3 origin = regiment.transform.position;

        for (int i = 0; i <= arcSegments; i++)
        {
            float t = i / (float)arcSegments;
            float angle = Mathf.Lerp(-regiment.FireArcHalfAngle, regiment.FireArcHalfAngle, t);
            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * forward;
            Vector3 point = origin + direction * range;
            line.SetPosition(i, GroundVisiblePoint(point));
        }
    }

    private static void UpdateOuterFan(Regiment regiment, LineRenderer line, float range)
    {
        const int sideSegments = 12;
        const int arcSegments = 48;
        const float muzzleHalfWidth = 8.5f;
        const float muzzleForward = 1.30f;

        Vector3 forward = GetFlatForward(regiment.transform);
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 origin = regiment.transform.position;

        Vector3 leftMuzzle = origin - right * muzzleHalfWidth + forward * muzzleForward;
        Vector3 rightMuzzle = origin + right * muzzleHalfWidth + forward * muzzleForward;

        Vector3 leftDirection = Quaternion.AngleAxis(-regiment.FireArcHalfAngle, Vector3.up) * forward;
        Vector3 rightDirection = Quaternion.AngleAxis(regiment.FireArcHalfAngle, Vector3.up) * forward;
        Vector3 leftEdge = origin + leftDirection * range;
        Vector3 rightEdge = origin + rightDirection * range;

        int positionCount = 1 + sideSegments + arcSegments + sideSegments;
        line.positionCount = positionCount;
        int index = 0;

        line.SetPosition(index++, GroundVisiblePoint(leftMuzzle));

        for (int i = 1; i <= sideSegments; i++)
        {
            float t = i / (float)sideSegments;
            line.SetPosition(index++, GroundVisiblePoint(Vector3.Lerp(leftMuzzle, leftEdge, t)));
        }

        for (int i = 1; i <= arcSegments; i++)
        {
            float t = i / (float)arcSegments;
            float angle = Mathf.Lerp(-regiment.FireArcHalfAngle, regiment.FireArcHalfAngle, t);
            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * forward;
            line.SetPosition(index++, GroundVisiblePoint(origin + direction * range));
        }

        for (int i = 1; i <= sideSegments; i++)
        {
            float t = i / (float)sideSegments;
            line.SetPosition(index++, GroundVisiblePoint(Vector3.Lerp(rightEdge, rightMuzzle, t)));
        }
    }

    private static Vector3 GetFlatForward(Transform unitTransform)
    {
        Vector3 forward = unitTransform.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
            return Vector3.forward;

        return forward.normalized;
    }

    private static Vector3 GroundVisiblePoint(Vector3 point)
    {
        point.y = PrototypeBootstrap.SampleGroundHeight(point.x, point.z) + 0.34f;
        return point;
    }
}
