using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f29a
// Readability/safety hotfix for the F27/F28 officer hierarchy.
// - Terrain-following command relationship lines.
// - Selecting one or more companies shows each company -> owning Major.
// - Selecting a Major shows Major -> Oberstløjtnant as well as Major -> companies.
// - Restores the round target-area cursor for officer orders.
// Visual-only: this component does not issue or replace tactical missions.
[DefaultExecutionOrder(32000)]
public sealed class PrototypeHierarchyVisualHotfix09F29A : MonoBehaviour
{
    private PrototypeRegimentHierarchy09F27 hierarchy;
    private PrototypeRegimentalHQ09F28 regimentalHq;
    private Camera cam;

    private FieldInfo selectedBattalionField;
    private FieldInfo battalionLinksField;
    private FieldInfo battalionPendingOrderField;
    private FieldInfo regimentalLinksField;
    private FieldInfo regimentalPendingOrderField;

    private LineRenderer targetRing;
    private Material targetMaterial;
    private bool installLogged;

    private const float LinkHeight = 0.82f;
    private const float TargetHeight = 0.86f;
    private const int LinkSamples = 28;
    private const int RingSamples = 72;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeHierarchyVisualHotfix09F29A>() == null)
            new GameObject("PrototypeHierarchyVisualHotfix_v000009f29a")
                .AddComponent<PrototypeHierarchyVisualHotfix09F29A>();
    }

    private void Awake()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;

        selectedBattalionField = typeof(PrototypeRegimentHierarchy09F27).GetField("selectedBattalion", flags);
        battalionLinksField = typeof(PrototypeRegimentHierarchy09F27).GetField("commandLinks", flags);
        battalionPendingOrderField = typeof(PrototypeRegimentHierarchy09F27).GetField("pendingOrder", flags);

        regimentalLinksField = typeof(PrototypeRegimentalHQ09F28).GetField("majorLinks", flags);
        regimentalPendingOrderField = typeof(PrototypeRegimentalHQ09F28).GetField("pendingTargetOrder", flags);
    }

    private void LateUpdate()
    {
        if (cam == null)
            cam = Camera.main;

        ResolveHierarchy();
        if (hierarchy == null || !hierarchy.Installed)
            return;

        EnsureTargetRing();
        RefreshBattalionCommandLinks();
        RefreshRegimentalCommandLinks();
        RefreshOfficerTargetRing();

        if (!installLogged)
        {
            installLogged = true;
            Debug.Log(
                "HQ-VISUAL-09F29A|Installed=True|TerrainFollowingLinks=True|" +
                "CompanyToMajorOnSelection=True|MajorToRegimentalOnSelection=True|OfficerTargetRing=True");
        }
    }

    private void ResolveHierarchy()
    {
        if (hierarchy == null)
            hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        if (regimentalHq == null)
            regimentalHq = PrototypeRegimentalHQ09F28.Instance;
    }

    private int SelectedBattalionIndex()
    {
        if (hierarchy == null || selectedBattalionField == null)
            return -1;

        object value = selectedBattalionField.GetValue(hierarchy);
        return value is int ? (int)value : -1;
    }

    private void RefreshBattalionCommandLinks()
    {
        if (battalionLinksField == null)
            return;

        IDictionary linksByBattalion = battalionLinksField.GetValue(hierarchy) as IDictionary;
        if (linksByBattalion == null)
            return;

        int selectedBattalion = SelectedBattalionIndex();

        for (int battalionIndex = 0; battalionIndex < hierarchy.BattalionCount; battalionIndex++)
        {
            GameObject major = hierarchy.GetMajorHq(battalionIndex);
            IReadOnlyList<Regiment> companies = hierarchy.GetCompanies(battalionIndex);
            if (major == null || companies == null || !linksByBattalion.Contains(battalionIndex))
                continue;

            List<LineRenderer> links = linksByBattalion[battalionIndex] as List<LineRenderer>;
            if (links == null)
                continue;

            bool majorSelected = selectedBattalion == battalionIndex;

            for (int i = 0; i < links.Count && i < companies.Count; i++)
            {
                LineRenderer line = links[i];
                Regiment company = companies[i];
                if (line == null)
                    continue;

                bool show = company != null && (majorSelected || company.IsSelected);
                line.enabled = show;
                if (!show)
                    continue;

                SetTerrainFollowingConnection(
                    line,
                    major.transform.position,
                    company.transform.position,
                    LinkHeight);
            }
        }
    }

    private void RefreshRegimentalCommandLinks()
    {
        if (regimentalHq == null || !regimentalHq.Installed || regimentalHq.HqRoot == null || regimentalLinksField == null)
            return;

        List<LineRenderer> links = regimentalLinksField.GetValue(regimentalHq) as List<LineRenderer>;
        if (links == null)
            return;

        int selectedBattalion = SelectedBattalionIndex();

        for (int i = 0; i < links.Count && i < hierarchy.BattalionCount; i++)
        {
            LineRenderer line = links[i];
            GameObject major = hierarchy.GetMajorHq(i);
            if (line == null)
                continue;

            bool show = major != null && (regimentalHq.Selected || selectedBattalion == i);
            line.enabled = show;
            if (!show)
                continue;

            SetTerrainFollowingConnection(
                line,
                regimentalHq.HqRoot.transform.position,
                major.transform.position,
                LinkHeight + 0.08f);
        }
    }

    private void RefreshOfficerTargetRing()
    {
        if (targetRing == null || targetMaterial == null || cam == null)
            return;

        MajorOrder09F18 order = MajorOrder09F18.None;
        bool regimentalOrder = false;

        int selectedBattalion = SelectedBattalionIndex();
        if (selectedBattalion >= 0 && battalionPendingOrderField != null)
        {
            object value = battalionPendingOrderField.GetValue(hierarchy);
            if (value is MajorOrder09F18)
                order = (MajorOrder09F18)value;
        }

        if (order == MajorOrder09F18.None &&
            regimentalHq != null && regimentalHq.Installed && regimentalHq.Selected &&
            regimentalPendingOrderField != null)
        {
            object value = regimentalPendingOrderField.GetValue(regimentalHq);
            if (value is MajorOrder09F18)
            {
                order = (MajorOrder09F18)value;
                regimentalOrder = order != MajorOrder09F18.None;
            }
        }

        if (order == MajorOrder09F18.None || !TryGetGround(Input.mousePosition, out Vector3 point))
        {
            targetRing.enabled = false;
            return;
        }

        float radius;
        if (order == MajorOrder09F18.AssembleHere)
            radius = regimentalOrder ? 20f : 12f;
        else
            radius = regimentalOrder ? 38f : 22f;

        targetMaterial.color = ColorFor(order);
        DrawTerrainCircle(targetRing, point, radius, TargetHeight);
        targetRing.enabled = true;
    }

    private void EnsureTargetRing()
    {
        if (targetRing != null)
            return;

        targetMaterial = CreateUnlit(new Color(0.92f, 0.72f, 0.18f, 0.96f), "HQ29ATargetArea");

        GameObject root = new GameObject("OfficerTargetArea09F29A");
        root.transform.SetParent(transform, false);

        targetRing = root.AddComponent<LineRenderer>();
        targetRing.useWorldSpace = true;
        targetRing.loop = true;
        targetRing.positionCount = RingSamples;
        targetRing.widthMultiplier = 0.28f;
        targetRing.numCapVertices = 2;
        targetRing.numCornerVertices = 2;
        targetRing.sharedMaterial = targetMaterial;
        targetRing.enabled = false;
    }

    private bool TryGetGround(Vector3 mousePosition, out Vector3 point)
    {
        point = Vector3.zero;
        if (cam == null)
            return false;

        Ray ray = cam.ScreenPointToRay(mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 5000f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
                continue;
            if (hit.collider.GetComponentInParent<Regiment>() != null)
                continue;
            if (hit.collider.gameObject.name != "Battlefield Ground")
                continue;

            point = hit.point;
            point.y = PrototypeBootstrap.SampleGroundHeight(point.x, point.z) + 0.10f;
            return true;
        }

        return false;
    }

    private static void SetTerrainFollowingConnection(LineRenderer line, Vector3 a, Vector3 b, float heightOffset)
    {
        if (line == null)
            return;

        line.positionCount = LinkSamples + 1;
        line.numCapVertices = 2;
        line.numCornerVertices = 2;

        for (int i = 0; i <= LinkSamples; i++)
        {
            float t = i / (float)LinkSamples;
            Vector3 p = Vector3.Lerp(a, b, t);
            p.y = PrototypeBootstrap.SampleGroundHeight(p.x, p.z) + heightOffset;
            line.SetPosition(i, p);
        }
    }

    private static void DrawTerrainCircle(LineRenderer line, Vector3 center, float radius, float yOffset)
    {
        if (line == null)
            return;

        if (line.positionCount != RingSamples)
            line.positionCount = RingSamples;

        for (int i = 0; i < RingSamples; i++)
        {
            float angle = i / (float)RingSamples * Mathf.PI * 2f;
            Vector3 p = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            p.y = PrototypeBootstrap.SampleGroundHeight(p.x, p.z) + yOffset;
            line.SetPosition(i, p);
        }
    }

    private static Color ColorFor(MajorOrder09F18 order)
    {
        switch (order)
        {
            case MajorOrder09F18.AttackHere: return PrototypeUiTheme09F15.Attack;
            case MajorOrder09F18.DefendHere: return PrototypeUiTheme09F15.Defend;
            case MajorOrder09F18.WithdrawHere: return PrototypeUiTheme09F15.Withdraw;
            default: return PrototypeUiTheme09F15.Move;
        }
    }

    private static Material CreateUnlit(Color color, string name)
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Transparent");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Standard");

        return new Material(shader)
        {
            name = name,
            color = color
        };
    }
}
