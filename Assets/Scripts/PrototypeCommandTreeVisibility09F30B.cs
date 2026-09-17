using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f30b
// Symmetric command-tree visibility. Selecting ANY Danish entity in the active command
// structure reveals the complete tree in both directions: Division -> Brigade -> Regiment
// -> both Majors -> all companies, plus current attached/support links.
[DefaultExecutionOrder(44000)]
public sealed class PrototypeCommandTreeVisibility09F30B : MonoBehaviour
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
    private bool logged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeCommandTreeVisibility09F30B>() == null)
            new GameObject("PrototypeCommandTreeVisibility_v000009f30b")
                .AddComponent<PrototypeCommandTreeVisibility09F30B>();
    }

    private void Awake()
    {
        selectedBattalionField = typeof(PrototypeRegimentHierarchy09F27)
            .GetField("selectedBattalion", PrivateInstance);
        companyLinksField = typeof(PrototypeRegimentHierarchy09F27)
            .GetField("commandLinks", PrivateInstance);
        majorLinksField = typeof(PrototypeRegimentalHQ09F28)
            .GetField("majorLinks", PrivateInstance);

        divisionBrigadeLinkField = typeof(PrototypeHigherCommandHQ09F30B)
            .GetField("divisionBrigadeLink", PrivateInstance);
        brigadeRegimentLinkField = typeof(PrototypeHigherCommandHQ09F30B)
            .GetField("brigadeRegimentLink", PrivateInstance);
        cavalryLinkAField = typeof(PrototypeHigherCommandHQ09F30B)
            .GetField("cavalryLinkA", PrivateInstance);
        cavalryLinkBField = typeof(PrototypeHigherCommandHQ09F30B)
            .GetField("cavalryLinkB", PrivateInstance);
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

        ForceLowerTree(hierarchy, regiment);
        ForceHigherTree(higher, regiment, cavalry);

        if (!logged)
        {
            logged = true;
            Debug.Log("HQ-TREE-09F30B|Symmetric=True|AnySelectionShowsFullTree=True|Directions=UP+DOWN|SupportLinks=True");
        }
    }

    private void ForceLowerTree(PrototypeRegimentHierarchy09F27 hierarchy, PrototypeRegimentalHQ09F28 regiment)
    {
        IDictionary linksByBattalion = companyLinksField != null
            ? companyLinksField.GetValue(hierarchy) as IDictionary
            : null;
        List<LineRenderer> majorLinks = majorLinksField != null
            ? majorLinksField.GetValue(regiment) as List<LineRenderer>
            : null;

        for (int b = 0; b < hierarchy.BattalionCount; b++)
        {
            GameObject major = hierarchy.GetMajorHq(b);
            IReadOnlyList<Regiment> companies = hierarchy.GetCompanies(b);
            if (major == null || companies == null)
                continue;

            if (linksByBattalion != null && linksByBattalion.Contains(b))
            {
                List<LineRenderer> companyLinks = linksByBattalion[b] as List<LineRenderer>;
                if (companyLinks != null)
                {
                    for (int i = 0; i < companyLinks.Count && i < companies.Count; i++)
                    {
                        LineRenderer line = companyLinks[i];
                        Regiment company = companies[i];
                        if (line == null || company == null)
                            continue;
                        DrawTerrainLink(line, major.transform.position, company.transform.position, 0.96f);
                    }
                }
            }

            if (majorLinks != null && b < majorLinks.Count && majorLinks[b] != null)
                DrawTerrainLink(majorLinks[b], regiment.HqRoot.transform.position, major.transform.position, 1.06f);
        }
    }

    private void ForceHigherTree(
        PrototypeHigherCommandHQ09F30B higher,
        PrototypeRegimentalHQ09F28 regiment,
        PrototypeCavalryManager09F30 cavalry)
    {
        LineRenderer divisionBrigade = GetLine(divisionBrigadeLinkField, higher);
        LineRenderer brigadeRegiment = GetLine(brigadeRegimentLinkField, higher);
        LineRenderer cavalryA = GetLine(cavalryLinkAField, higher);
        LineRenderer cavalryB = GetLine(cavalryLinkBField, higher);

        if (divisionBrigade != null)
            DrawTerrainLink(divisionBrigade, higher.DivisionHqRoot.transform.position, higher.BrigadeHqRoot.transform.position, 1.18f);
        if (brigadeRegiment != null)
            DrawTerrainLink(brigadeRegiment, higher.BrigadeHqRoot.transform.position, regiment.HqRoot.transform.position, 1.18f);

        if (cavalry != null)
        {
            DrawSupportLink(cavalryA, higher, regiment, cavalry.Gardehusar);
            DrawSupportLink(cavalryB, higher, regiment, cavalry.Dragon);
        }
    }

    private static void DrawSupportLink(
        LineRenderer line,
        PrototypeHigherCommandHQ09F30B higher,
        PrototypeRegimentalHQ09F28 regiment,
        PrototypeCavalryUnit09F30 unit)
    {
        if (line == null || unit == null)
            return;

        PrototypeCommandAttachment09F30B attachment = unit.GetComponent<PrototypeCommandAttachment09F30B>();
        bool underRegiment = attachment != null &&
                             attachment.CurrentCommandParent == PrototypeHigherCommandHQ09F30B.RegimentId;
        Vector3 parent = underRegiment
            ? regiment.HqRoot.transform.position
            : higher.BrigadeHqRoot.transform.position;
        DrawTerrainLink(line, parent, unit.transform.position, 1.10f);
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
            if (companies == null)
                continue;
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
