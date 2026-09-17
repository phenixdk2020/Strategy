using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f30c
// Final command-tree renderer for F30C. Any Danish selection reveals the whole command tree
// in both directions, and support links originate from the current live command parent.
[DefaultExecutionOrder(45000)]
public sealed class PrototypeCommandTreeVisibility09F30C : MonoBehaviour
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private const int LinkSamples = 30;

    private FieldInfo selectedBattalionField;
    private FieldInfo companyLinksField;
    private FieldInfo majorLinksField;
    private FieldInfo divisionBrigadeLinkField;
    private FieldInfo brigadeRegimentLinkField;
    private FieldInfo cavalryLinkAField;
    private FieldInfo cavalryLinkBField;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeCommandTreeVisibility09F30C>() == null)
            new GameObject("PrototypeCommandTreeVisibility_v000009f30c")
                .AddComponent<PrototypeCommandTreeVisibility09F30C>();
    }

    private void Awake()
    {
        selectedBattalionField = typeof(PrototypeRegimentHierarchy09F27).GetField("selectedBattalion", PrivateInstance);
        companyLinksField = typeof(PrototypeRegimentHierarchy09F27).GetField("commandLinks", PrivateInstance);
        majorLinksField = typeof(PrototypeRegimentalHQ09F28).GetField("majorLinks", PrivateInstance);
        divisionBrigadeLinkField = typeof(PrototypeHigherCommandHQ09F30B).GetField("divisionBrigadeLink", PrivateInstance);
        brigadeRegimentLinkField = typeof(PrototypeHigherCommandHQ09F30B).GetField("brigadeRegimentLink", PrivateInstance);
        cavalryLinkAField = typeof(PrototypeHigherCommandHQ09F30B).GetField("cavalryLinkA", PrivateInstance);
        cavalryLinkBField = typeof(PrototypeHigherCommandHQ09F30B).GetField("cavalryLinkB", PrivateInstance);
    }

    private void LateUpdate()
    {
        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        PrototypeRegimentalHQ09F28 regiment = PrototypeRegimentalHQ09F28.Instance;
        PrototypeHigherCommandHQ09F30B higher = PrototypeHigherCommandHQ09F30B.Instance;
        PrototypeCavalryManager09F30 cavalry = PrototypeCavalryManager09F30.Instance;

        if (hierarchy == null || !hierarchy.Installed || regiment == null || !regiment.Installed ||
            higher == null || !higher.Installed || higher.DivisionHqRoot == null || higher.BrigadeHqRoot == null ||
            regiment.HqRoot == null)
            return;

        bool anySelected = higher.SelectedLevel != PrototypeHigherCommandLevel09F30B.None || regiment.Selected ||
                           GetSelectedMajor(hierarchy) >= 0 || AnyCompanySelected(hierarchy) ||
                           (cavalry != null && cavalry.SelectedUnit != null);
        if (!anySelected)
            return;

        DrawHigher(higher, regiment);
        DrawLower(hierarchy, regiment);
        if (cavalry != null)
        {
            DrawSupport(GetLine(cavalryLinkAField, higher), cavalry.Gardehusar, higher, regiment, hierarchy);
            DrawSupport(GetLine(cavalryLinkBField, higher), cavalry.Dragon, higher, regiment, hierarchy);
        }
    }

    private void DrawHigher(PrototypeHigherCommandHQ09F30B higher, PrototypeRegimentalHQ09F28 regiment)
    {
        LineRenderer divBrig = GetLine(divisionBrigadeLinkField, higher);
        LineRenderer brigReg = GetLine(brigadeRegimentLinkField, higher);
        if (divBrig != null)
            DrawTerrainLink(divBrig, higher.DivisionHqRoot.transform.position, higher.BrigadeHqRoot.transform.position, 1.18f);
        if (brigReg != null)
            DrawTerrainLink(brigReg, higher.BrigadeHqRoot.transform.position, regiment.HqRoot.transform.position, 1.18f);
    }

    private void DrawLower(PrototypeRegimentHierarchy09F27 hierarchy, PrototypeRegimentalHQ09F28 regiment)
    {
        IDictionary linksByBattalion = companyLinksField != null ? companyLinksField.GetValue(hierarchy) as IDictionary : null;
        List<LineRenderer> majorLinks = majorLinksField != null ? majorLinksField.GetValue(regiment) as List<LineRenderer> : null;

        for (int b = 0; b < hierarchy.BattalionCount; b++)
        {
            GameObject major = hierarchy.GetMajorHq(b);
            IReadOnlyList<Regiment> companies = hierarchy.GetCompanies(b);
            if (major == null || companies == null)
                continue;

            if (majorLinks != null && b < majorLinks.Count && majorLinks[b] != null)
                DrawTerrainLink(majorLinks[b], regiment.HqRoot.transform.position, major.transform.position, 1.06f);

            if (linksByBattalion == null || !linksByBattalion.Contains(b))
                continue;
            List<LineRenderer> lines = linksByBattalion[b] as List<LineRenderer>;
            if (lines == null)
                continue;
            for (int i = 0; i < lines.Count && i < companies.Count; i++)
                if (lines[i] != null && companies[i] != null)
                    DrawTerrainLink(lines[i], major.transform.position, companies[i].transform.position, 0.96f);
        }
    }

    private void DrawSupport(LineRenderer line, PrototypeCavalryUnit09F30 unit,
        PrototypeHigherCommandHQ09F30B higher, PrototypeRegimentalHQ09F28 regiment,
        PrototypeRegimentHierarchy09F27 hierarchy)
    {
        if (line == null || unit == null)
            return;

        string parent = PrototypeCavalryCommandControl09F30C.GetParent(unit);
        Transform source = higher.BrigadeHqRoot.transform;
        if (parent == PrototypeCavalryCommandControl09F30C.DivisionId)
            source = higher.DivisionHqRoot.transform;
        else if (parent == PrototypeCavalryCommandControl09F30C.RegimentId)
            source = regiment.HqRoot.transform;
        else if (parent == PrototypeCavalryCommandControl09F30C.MajorAId && hierarchy.GetMajorHq(0) != null)
            source = hierarchy.GetMajorHq(0).transform;
        else if (parent == PrototypeCavalryCommandControl09F30C.MajorBId && hierarchy.GetMajorHq(1) != null)
            source = hierarchy.GetMajorHq(1).transform;

        DrawTerrainLink(line, source.position, unit.transform.position, 1.10f);
    }

    private int GetSelectedMajor(PrototypeRegimentHierarchy09F27 hierarchy)
    {
        if (selectedBattalionField == null)
            return -1;
        object value = selectedBattalionField.GetValue(hierarchy);
        return value is int ? (int)value : -1;
    }

    private static bool AnyCompanySelected(PrototypeRegimentHierarchy09F27 hierarchy)
    {
        for (int b = 0; b < hierarchy.BattalionCount; b++)
        {
            IReadOnlyList<Regiment> companies = hierarchy.GetCompanies(b);
            if (companies == null) continue;
            for (int i = 0; i < companies.Count; i++)
                if (companies[i] != null && companies[i].IsSelected)
                    return true;
        }
        return false;
    }

    private static LineRenderer GetLine(FieldInfo field, object owner)
    {
        return field != null && owner != null ? field.GetValue(owner) as LineRenderer : null;
    }

    private static void DrawTerrainLink(LineRenderer line, Vector3 a, Vector3 b, float height)
    {
        line.enabled = true;
        line.useWorldSpace = true;
        line.positionCount = LinkSamples + 1;
        line.numCapVertices = 2;
        line.numCornerVertices = 2;
        for (int i = 0; i <= LinkSamples; i++)
        {
            float t = i / (float)LinkSamples;
            Vector3 p = Vector3.Lerp(a, b, t);
            p.y = PrototypeBootstrap.SampleGroundHeight(p.x, p.z) + height;
            line.SetPosition(i, p);
        }
    }
}
