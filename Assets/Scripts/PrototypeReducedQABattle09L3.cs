using System.Collections;
using System.Reflection;
using UnityEngine;

// v00.00.09l3 TEST - temporary reduced battle scale for company-control QA.
// Keeps the full OOB data model in the project, but removes the second regiment on
// each side from the active BattleManager roster after 09l2 company entities exist.
// This is a QA scenario reduction only; it does not change the intended final OOB.
[DefaultExecutionOrder(10860)]
public sealed class PrototypeReducedQABattle09L3 : MonoBehaviour
{
    private bool installed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeReducedQABattle09L3>() != null)
            return;

        GameObject root = new GameObject("PrototypeReducedQABattle_v000009l3");
        root.AddComponent<PrototypeReducedQABattle09L3>();
    }

    private void Update()
    {
        if (installed)
            return;

        BattleManager battle = BattleManager.Instance;
        PrototypeCompanyTacticalControl09L2 companyControl =
            PrototypeCompanyTacticalControl09L2.Instance;

        if (battle == null || companyControl == null || !companyControl.Installed)
            return;

        Regiment danishActive = FindActiveRegiment(battle, "1. Regiment");
        Regiment prussianActive = FindActiveRegiment(battle, "8th Regiment");
        Regiment danishReserve = FindActiveRegiment(battle, "5. Regiment");
        Regiment prussianReserve = FindActiveRegiment(battle, "18th Regiment");

        if (danishActive == null || prussianActive == null ||
            danishReserve == null || prussianReserve == null)
            return;

        // Let the retained 09k renderer create the mounted regimental HQ groups once
        // before it is disabled. This preserves the HQ presentation in the reduced test.
        if (danishActive.transform.Find("RegimentalHQ09K") == null ||
            prussianActive.transform.Find("RegimentalHQ09K") == null)
            return;

        ReduceCompanyLists(companyControl, danishReserve, prussianReserve);
        ClearPlayerCommanderReferences(danishReserve, prussianReserve);
        RemoveFromBattleRoster(battle, danishReserve, prussianReserve);

        danishReserve.SetSelected(false);
        prussianReserve.SetSelected(false);
        danishReserve.OrderHold();
        prussianReserve.OrderHold();
        danishReserve.gameObject.SetActive(false);
        prussianReserve.gameObject.SetActive(false);

        // Brigade reserve/commit logic requires two active subordinate regiments and is
        // intentionally paused during this focused company-control QA gate.
        PrototypeBrigadeCommand09L brigade = Object.FindAnyObjectByType<PrototypeBrigadeCommand09L>();
        if (brigade != null)
            brigade.enabled = false;

        // 09l2 company renderer is authoritative for ordinary infantry in this gate.
        PrototypeFullScaleRenderer09K oldRenderer = Object.FindAnyObjectByType<PrototypeFullScaleRenderer09K>();
        if (oldRenderer != null)
            oldRenderer.enabled = false;

        installed = true;

        int activeCompanyCount = CountActiveCompanies(companyControl);
        int activeInfantry = danishActive.CurrentStrength + prussianActive.CurrentStrength;

        Debug.Log(
            "QA-SCALE-09L3|Installed=True|ActiveRegiments=2|" +
            "Denmark=1.Infanteri-Regiment(" + danishActive.CurrentStrength + ")|" +
            "Prussia=8thRegimentQA(" + prussianActive.CurrentStrength + ")|" +
            "ActiveCompanies=" + activeCompanyCount +
            "|ActiveInfantry=" + activeInfantry +
            "|HiddenRegiments=11.Infanteri-Regiment+18thRegimentQA|" +
            "BrigadeReserveAI=False|FullOOBDataRetained=True");
    }

    private static Regiment FindActiveRegiment(BattleManager battle, string technicalName)
    {
        if (battle == null || battle.Regiments == null)
            return null;

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment != null && regiment.RegimentName == technicalName)
                return regiment;
        }

        return null;
    }

    private static void ReduceCompanyLists(
        PrototypeCompanyTacticalControl09L2 control,
        Regiment danishReserve,
        Regiment prussianReserve)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        FieldInfo companiesField = typeof(PrototypeCompanyTacticalControl09L2).GetField("companies", flags);
        FieldInfo selectedField = typeof(PrototypeCompanyTacticalControl09L2).GetField("selectedCompanies", flags);

        IList selected = selectedField != null ? selectedField.GetValue(control) as IList : null;
        if (selected != null)
        {
            for (int i = selected.Count - 1; i >= 0; i--)
            {
                PrototypeCompanyTacticalEntity09L2 company =
                    selected[i] as PrototypeCompanyTacticalEntity09L2;
                if (company == null || !IsRemovedParent(company.ParentRegiment, danishReserve, prussianReserve))
                    continue;

                company.SetSelected(false);
                selected.RemoveAt(i);
            }
        }

        IList companies = companiesField != null ? companiesField.GetValue(control) as IList : null;
        if (companies == null)
            return;

        for (int i = companies.Count - 1; i >= 0; i--)
        {
            PrototypeCompanyTacticalEntity09L2 company =
                companies[i] as PrototypeCompanyTacticalEntity09L2;
            if (company == null || !IsRemovedParent(company.ParentRegiment, danishReserve, prussianReserve))
                continue;

            companies.RemoveAt(i);
            Object.Destroy(company.gameObject);
        }
    }

    private static bool IsRemovedParent(
        Regiment parent,
        Regiment danishReserve,
        Regiment prussianReserve)
    {
        return parent == danishReserve || parent == prussianReserve;
    }

    private static void ClearPlayerCommanderReferences(Regiment danishReserve, Regiment prussianReserve)
    {
        PlayerCommander commander = PlayerCommander.Instance;
        if (commander == null)
            return;

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        FieldInfo selectedField = typeof(PlayerCommander).GetField("selected", flags);
        FieldInfo routesField = typeof(PlayerCommander).GetField("routes", flags);
        FieldInfo ghostsField = typeof(PlayerCommander).GetField("orderGhosts", flags);

        IList selected = selectedField != null ? selectedField.GetValue(commander) as IList : null;
        RemoveFromList(selected, danishReserve);
        RemoveFromList(selected, prussianReserve);

        IDictionary routes = routesField != null ? routesField.GetValue(commander) as IDictionary : null;
        if (routes != null)
        {
            if (routes.Contains(danishReserve)) routes.Remove(danishReserve);
            if (routes.Contains(prussianReserve)) routes.Remove(prussianReserve);
        }

        IDictionary ghosts = ghostsField != null ? ghostsField.GetValue(commander) as IDictionary : null;
        if (ghosts != null)
        {
            if (ghosts.Contains(danishReserve)) ghosts.Remove(danishReserve);
            if (ghosts.Contains(prussianReserve)) ghosts.Remove(prussianReserve);
        }
    }

    private static void RemoveFromList(IList list, object value)
    {
        if (list == null || value == null)
            return;

        while (list.Contains(value))
            list.Remove(value);
    }

    private static void RemoveFromBattleRoster(
        BattleManager battle,
        Regiment danishReserve,
        Regiment prussianReserve)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        FieldInfo rosterField = typeof(BattleManager).GetField("regiments", flags);
        IList roster = rosterField != null ? rosterField.GetValue(battle) as IList : null;
        if (roster == null)
        {
            Debug.LogError("QA-SCALE-09L3|Installed=False|Reason=BattleManagerRosterNotFound");
            return;
        }

        RemoveFromList(roster, danishReserve);
        RemoveFromList(roster, prussianReserve);
    }

    private static int CountActiveCompanies(PrototypeCompanyTacticalControl09L2 control)
    {
        if (control == null || control.Companies == null)
            return 0;

        int count = 0;
        for (int i = 0; i < control.Companies.Count; i++)
        {
            PrototypeCompanyTacticalEntity09L2 company = control.Companies[i];
            if (company != null && company.ParentRegiment != null && company.ParentRegiment.gameObject.activeInHierarchy)
                count++;
        }
        return count;
    }
}
