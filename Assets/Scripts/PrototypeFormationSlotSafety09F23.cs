using System.Collections.Generic;
using UnityEngine;

// v00.00.09f29a
// Shared endpoint resolver for Major-owned and direct/manual multi-company formations.
// The requested line remains authoritative. A blocked slot is searched along that line
// first; only if the line itself cannot provide a legal footprint is a shallow depth
// offset allowed. Already reserved company footprints are treated as hard reservations.
// F29A hardens river safety: the complete company footprint is checked against the river,
// and an illegal river endpoint is never deliberately returned while a nearby legal slot exists.
public static class PrototypeFormationSlotSafety09F23
{
    public const float CompanyMinCentreSpacing = 68f;

    private static readonly float[] AlongLineOffsets =
    {
        0f,
        12f, -12f,
        24f, -24f,
        36f, -36f,
        48f, -48f,
        60f, -60f,
        72f, -72f,
        90f, -90f,
        120f, -120f,
        150f, -150f,
        180f, -180f
    };

    private static readonly float[] DepthFallbacks = { 0f, -12f, 12f, -24f, 24f };

    private static readonly float[] FootprintLateralSamples =
    {
        -25f, -18f, -10f, 0f, 10f, 18f, 25f
    };

    private static readonly float[] FootprintDepthSamples =
    {
        -5.5f, 0f, 5.5f
    };

    public static Vector3 ResolveOnFormationLine(
        Vector3 desired,
        Vector3 facing,
        Vector3 lineDirection,
        IList<Vector3> reserved,
        out bool moved,
        out bool usedDepthFallback)
    {
        Vector3 forward = Flat(facing);
        Vector3 lateral = Flat(lineDirection);
        if (lateral.sqrMagnitude < 0.01f)
            lateral = Vector3.Cross(Vector3.up, forward).normalized;
        if (lateral.sqrMagnitude < 0.01f)
            lateral = Vector3.right;

        desired = Ground(desired);
        moved = false;
        usedDepthFallback = false;

        // First pass: preserve the requested line exactly and search left/right only.
        foreach (float offset in AlongLineOffsets)
        {
            Vector3 candidate = Ground(desired + lateral * offset);
            if (!IsLegalFormationEndpoint(candidate, forward) || ConflictsWithReserved(candidate, reserved))
                continue;

            moved = PlanarDistance(candidate, desired) > 0.05f;
            return candidate;
        }

        // Second pass: stay close to the line, but permit a shallow fore/aft stagger.
        foreach (float depth in DepthFallbacks)
        {
            if (Mathf.Abs(depth) < 0.01f)
                continue;

            foreach (float offset in AlongLineOffsets)
            {
                Vector3 candidate = Ground(desired + lateral * offset + forward * depth);
                if (!IsLegalFormationEndpoint(candidate, forward) || ConflictsWithReserved(candidate, reserved))
                    continue;

                moved = true;
                usedDepthFallback = true;
                return candidate;
            }
        }

        // F29A emergency search: expand around the requested slot instead of falling back
        // to a knowingly illegal point in water or inside a hard obstacle.
        if (TryEmergencySearch(desired, forward, lateral, reserved, true, out Vector3 emergency, out bool depthUsed))
        {
            moved = true;
            usedDepthFallback = depthUsed;
            Debug.LogWarning(
                "FORMATION-SAFETY-09F29A|EmergencyRelocation=True|ReservedHonoured=True|" +
                "From=" + desired.x.ToString("0.0") + "," + desired.z.ToString("0.0") +
                "|To=" + emergency.x.ToString("0.0") + "," + emergency.z.ToString("0.0"));
            return emergency;
        }

        // If spacing reservations make every nearby point impossible, terrain safety wins.
        // A legal slot may overlap the preferred spacing before we ever return an illegal
        // river/building endpoint.
        if (TryEmergencySearch(desired, forward, lateral, reserved, false, out emergency, out depthUsed))
        {
            moved = true;
            usedDepthFallback = depthUsed;
            Debug.LogWarning(
                "FORMATION-SAFETY-09F29A|EmergencyRelocation=True|ReservedHonoured=False|" +
                "Reason=TerrainSafetyPriority|From=" + desired.x.ToString("0.0") + "," + desired.z.ToString("0.0") +
                "|To=" + emergency.x.ToString("0.0") + "," + emergency.z.ToString("0.0"));
            return emergency;
        }

        Debug.LogError(
            "FORMATION-SAFETY-09F29A|LegalEndpoint=False|Reason=NoLegalSlotWithin400m|" +
            "Requested=" + desired.x.ToString("0.0") + "," + desired.z.ToString("0.0"));

        // This should only occur if the battlefield itself has no legal position in the
        // search neighbourhood. Preserve the request as a diagnosable last resort.
        return desired;
    }

    private static bool TryEmergencySearch(
        Vector3 desired,
        Vector3 forward,
        Vector3 lateral,
        IList<Vector3> reserved,
        bool honourReserved,
        out Vector3 result,
        out bool usedDepth)
    {
        const float step = 20f;
        const float maxRadius = 400f;

        for (float radius = step; radius <= maxRadius; radius += step)
        {
            int steps = Mathf.RoundToInt(radius / step);

            for (int x = -steps; x <= steps; x++)
            {
                float lateralOffset = x * step;

                Vector3 candidateA = Ground(desired + lateral * lateralOffset + forward * radius);
                if (IsEmergencyCandidate(candidateA, forward, reserved, honourReserved))
                {
                    result = candidateA;
                    usedDepth = true;
                    return true;
                }

                Vector3 candidateB = Ground(desired + lateral * lateralOffset - forward * radius);
                if (IsEmergencyCandidate(candidateB, forward, reserved, honourReserved))
                {
                    result = candidateB;
                    usedDepth = true;
                    return true;
                }
            }

            for (int z = -steps + 1; z <= steps - 1; z++)
            {
                float depthOffset = z * step;

                Vector3 candidateA = Ground(desired + lateral * radius + forward * depthOffset);
                if (IsEmergencyCandidate(candidateA, forward, reserved, honourReserved))
                {
                    result = candidateA;
                    usedDepth = Mathf.Abs(depthOffset) > 0.01f;
                    return true;
                }

                Vector3 candidateB = Ground(desired - lateral * radius + forward * depthOffset);
                if (IsEmergencyCandidate(candidateB, forward, reserved, honourReserved))
                {
                    result = candidateB;
                    usedDepth = Mathf.Abs(depthOffset) > 0.01f;
                    return true;
                }
            }
        }

        result = desired;
        usedDepth = false;
        return false;
    }

    private static bool IsEmergencyCandidate(
        Vector3 candidate,
        Vector3 forward,
        IList<Vector3> reserved,
        bool honourReserved)
    {
        if (!IsLegalFormationEndpoint(candidate, forward))
            return false;
        if (honourReserved && ConflictsWithReserved(candidate, reserved))
            return false;
        return true;
    }

    public static bool IsLegalFormationEndpoint(Vector3 point, Vector3 facing)
    {
        if (Mathf.Abs(point.x) > PrototypeBootstrap.BattlefieldHalfWidth - 30f ||
            Mathf.Abs(point.z) > PrototypeBootstrap.BattlefieldHalfDepth - 30f)
            return false;

        Vector3 forward = Flat(facing);

        // F29A: validate the whole 48 m company footprint, not just its centre.
        // This prevents a legal-looking centre on the bank from placing half a company
        // in the river. A full line cannot occupy the narrow bridge itself; bridge crossing
        // remains a movement/column problem rather than a legal line-formation endpoint.
        if (!IsFormationFootprintClearOfRiver(point, forward))
            return false;

        Quaternion rotation = Quaternion.LookRotation(forward, Vector3.up);
        Vector3 centre = new Vector3(
            point.x,
            PrototypeBootstrap.SampleGroundHeight(point.x, point.z) + 1f,
            point.z);

        // Approximate the 48 m three-rank company footprint with a small safety margin.
        Collider[] hits = Physics.OverlapBox(
            centre,
            new Vector3(25f, 2.5f, 5.5f),
            rotation);

        foreach (Collider hit in hits)
        {
            if (hit == null)
                continue;

            string name = hit.gameObject != null ? hit.gameObject.name : string.Empty;
            string rootName = hit.transform != null && hit.transform.root != null
                ? hit.transform.root.name
                : string.Empty;

            if (ContainsHardBuilding(name) || ContainsHardBuilding(rootName))
                return false;
        }

        return true;
    }

    private static bool IsFormationFootprintClearOfRiver(Vector3 centre, Vector3 forward)
    {
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        if (right.sqrMagnitude < 0.01f)
            right = Vector3.right;

        foreach (float lateral in FootprintLateralSamples)
        {
            foreach (float depth in FootprintDepthSamples)
            {
                Vector3 sample = centre + right * lateral + forward * depth;
                float riverX = PrototypeBootstrap.StreamCenterX(sample.z);
                bool bridgeZone = Mathf.Abs(sample.z - 22f) <= 7f;

                if (Mathf.Abs(sample.x - riverX) <= 5.5f && !bridgeZone)
                    return false;
            }
        }

        return true;
    }

    public static bool ConflictsWithReserved(Vector3 point, IList<Vector3> reserved)
    {
        if (reserved == null)
            return false;

        for (int i = 0; i < reserved.Count; i++)
        {
            if (PlanarDistance(point, reserved[i]) < CompanyMinCentreSpacing)
                return true;
        }
        return false;
    }

    private static bool ContainsHardBuilding(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;
        return value.Contains("Farmhouse") || value.Contains("Barn");
    }

    private static Vector3 Flat(Vector3 value)
    {
        value.y = 0f;
        if (value.sqrMagnitude < 0.0001f)
            return Vector3.forward;
        return value.normalized;
    }

    private static Vector3 Ground(Vector3 point)
    {
        point.y = PrototypeBootstrap.SampleGroundHeight(point.x, point.z) + 0.10f;
        return point;
    }

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
