using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f29g + v00.00.09f30b higher-command extension
// Company -> Major -> Regiment remains the proven lower chain.
// F30B additionally reveals the complete lower subtree when Brigade/Division HQ is selected.
[DefaultExecutionOrder(33000)]
public sealed class PrototypeCommandChainVisual09F29G : MonoBehaviour
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private const int LinkSamples = 30;
    private const float LinkHeight = 0.96f;

    private PrototypeRegimentHierarchy09F27 hierarchy;
    private PrototypeRegimentalHQ09F28 regimental;
    private FieldInfo selectedBattalionField;
    private FieldInfo companyLinksField;
    private FieldInfo majorLinksField;
    private bool logged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeCommandChainVisual09F29G>() == null)
            new GameObject("PrototypeCommandChainVisual_v000009f29g")
                .AddComponent<PrototypeCommandChainVisual09F29G>();
    }

    private void Awake()
    {
        selectedBattalionField = typeof(PrototypeRegimentHierarchy09F27)
            .GetField("selectedBattalion", PrivateInstance);
        companyLinksField = typeof(PrototypeRegimentHierarchy09F27)
            .GetField("commandLinks", PrivateInstance);
        majorLinksField = typeof(PrototypeRegimentalHQ09F28)
            .GetField("majorLinks", PrivateInstance);
    }

    private void LateUpdate()
    {
        hierarchy = hierarchy != null ? hierarchy : PrototypeRegimentHierarchy09F27.Instance;
        regimental = regimental != null ? regimental : PrototypeRegimentalHQ09F28.Instance;
        if (hierarchy == null || !hierarchy.Installed)
            return;

        int selectedMajor = GetSelectedMajor();
        bool regimentSelected = regimental != null && regimental.Installed && regimental.Selected;
        PrototypeHigherCommandHQ09F30B higher = PrototypeHigherCommandHQ09F30B.Instance;
        bool higherSelected = higher != null && higher.Installed &&
                              higher.SelectedLevel != PrototypeHigherCommandLevel09F30B.None;

        IDictionary linksByBattalion = companyLinksField != null
            ? companyLinksField.GetValue(hierarchy) as IDictionary
            : null;
        List<LineRenderer> majorLinks = regimental != null && majorLinksField != null
            ? majorLinksField.GetValue(regimental) as List<LineRenderer>
            : null;

        for (int b = 0; b < hierarchy.BattalionCount; b++)
        {
            GameObject major = hierarchy.GetMajorHq(b);
            IReadOnlyList<Regiment> companies = hierarchy.GetCompanies(b);
            if (major == null || companies == null)
                continue;

            bool anyCompanySelected = false;
            for (int i = 0; i < companies.Count; i++)
                if (companies[i] != null && companies[i].IsSelected)
                    anyCompanySelected = true;

            bool showAllCompanyLinks = higherSelected || regimentSelected || selectedMajor == b;

            if (linksByBattalion != null && linksByBattalion.Contains(b))
            {
                List<LineRenderer> companyLinks = linksByBattalion[b] as List<LineRenderer>;
                if (companyLinks != null)
                {
                    for (int i = 0; i < companyLinks.Count && i < companies.Count; i++)
                    {
                        LineRenderer line = companyLinks[i];
                        Regiment company = companies[i];
                        if (line == null)
                            continue;

                        bool show = company != null && (showAllCompanyLinks || company.IsSelected);
                        line.enabled = show;
                        if (show)
                            DrawTerrainLink(line, major.transform.position, company.transform.position, LinkHeight);
                    }
                }
            }

            if (majorLinks != null && b < majorLinks.Count && majorLinks[b] != null &&
                regimental != null && regimental.HqRoot != null)
            {
                LineRenderer line = majorLinks[b];
                bool show = higherSelected || regimentSelected || selectedMajor == b || anyCompanySelected;
                line.enabled = show;
                if (show)
                    DrawTerrainLink(line, regimental.HqRoot.transform.position, major.transform.position, LinkHeight + 0.10f);
            }
        }

        if (!logged)
        {
            logged = true;
            Debug.Log("HQ-CHAIN-09F30B|Installed=True|CompanyToMajor=True|MajorToRegimental=True|HigherSelectionShowsFullLowerTree=True");
        }
    }

    private int GetSelectedMajor()
    {
        if (selectedBattalionField == null || hierarchy == null)
            return -1;
        object value = selectedBattalionField.GetValue(hierarchy);
        return value is int ? (int)value : -1;
    }

    private static void DrawTerrainLink(LineRenderer line, Vector3 a, Vector3 b, float height)
    {
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
