using System.Collections.Generic;
using UnityEngine;

// v00.00.09f23
// Shared endpoint resolver for Major-owned and direct/manual multi-company formations.
// The requested line remains authoritative. A blocked slot is searched along that line
// first; only if the line itself cannot provide a legal footprint is a shallow depth
// offset allowed. Already reserved company footprints are treated as hard reservations.
public static class PrototypeFormationSlotSafety09F23
{
    public const float CompanyMinCentreSpacing = 55f;

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

        // Last resort: stay near the line, but permit a shallow fore/aft stagger.
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

        // No legal slot found in the local search. Return the original request rather
        // than inventing an unlimited displacement; the existing path/endpoint layers
        // can still diagnose it and QA telemetry will reveal the unresolved case.
        return desired;
    }

    public static bool IsLegalFormationEndpoint(Vector3 point, Vector3 facing)
    {
        if (Mathf.Abs(point.x) > PrototypeBootstrap.BattlefieldHalfWidth - 30f ||
            Mathf.Abs(point.z) > PrototypeBootstrap.BattlefieldHalfDepth - 30f)
            return false;

        float riverX = PrototypeBootstrap.StreamCenterX(point.z);
        if (Mathf.Abs(point.x - riverX) <= 5f && Mathf.Abs(point.z - 22f) > 7f)
            return false;

        Vector3 forward = Flat(facing);
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
