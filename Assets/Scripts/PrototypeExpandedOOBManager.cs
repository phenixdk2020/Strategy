using UnityEngine;

[DefaultExecutionOrder(-11000)]
public sealed class PrototypeExpandedOOBManager : MonoBehaviour
{
    private bool installed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeExpandedOOBManager>() != null)
            return;

        GameObject managerObject = new GameObject("PrototypeExpandedOOBManager_v009");
        managerObject.AddComponent<PrototypeExpandedOOBManager>();
    }

    private void Update()
    {
        if (installed)
            return;

        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null || battle.Regiments.Count < 4)
            return;

        EnsureRegiment(
            "2. Regiment",
            BattleTeam.Denmark,
            600,
            false,
            new Vector3(-108f, 0f, -26f),
            Quaternion.Euler(0f, 90f, 0f));

        EnsureRegiment(
            "6. Regiment",
            BattleTeam.Denmark,
            570,
            false,
            new Vector3(-112f, 0f, 78f),
            Quaternion.Euler(0f, 90f, 0f));

        EnsureRegiment(
            "12th Regiment",
            BattleTeam.Prussia,
            590,
            true,
            new Vector3(108f, 0f, -26f),
            Quaternion.Euler(0f, -90f, 0f));

        EnsureRegiment(
            "24th Regiment",
            BattleTeam.Prussia,
            575,
            true,
            new Vector3(112f, 0f, 78f),
            Quaternion.Euler(0f, -90f, 0f));

        // Re-space all eight QA regiments into four clear lanes per side.
        // This happens before OfficerAIPrototypeManager's first Update because this
        // manager has the earlier execution order. Officer defend anchors therefore
        // use the final QA start positions.
        SetPose("1. Regiment", new Vector3(-112f, 0f, -78f), Quaternion.Euler(0f, 90f, 0f));
        SetPose("2. Regiment", new Vector3(-108f, 0f, -26f), Quaternion.Euler(0f, 90f, 0f));
        SetPose("5. Regiment", new Vector3(-108f, 0f, 26f), Quaternion.Euler(0f, 90f, 0f));
        SetPose("6. Regiment", new Vector3(-112f, 0f, 78f), Quaternion.Euler(0f, 90f, 0f));

        SetPose("8th Regiment", new Vector3(112f, 0f, -78f), Quaternion.Euler(0f, -90f, 0f));
        SetPose("12th Regiment", new Vector3(108f, 0f, -26f), Quaternion.Euler(0f, -90f, 0f));
        SetPose("18th Regiment", new Vector3(108f, 0f, 26f), Quaternion.Euler(0f, -90f, 0f));
        SetPose("24th Regiment", new Vector3(112f, 0f, 78f), Quaternion.Euler(0f, -90f, 0f));

        installed = true;
        Debug.Log("QA-OOB|Scenario=v009|Denmark=4|Prussia=4|Total=8|Layout=FourLanes");
    }

    private static void EnsureRegiment(
        string unitName,
        BattleTeam team,
        int strength,
        bool isAI,
        Vector3 position,
        Quaternion rotation)
    {
        if (FindRegiment(unitName) != null)
            return;

        position.y = PrototypeBootstrap.SampleGroundHeight(position.x, position.z) + 0.10f;

        GameObject unit = new GameObject(unitName);
        unit.transform.rotation = rotation;

        Regiment regiment = unit.AddComponent<Regiment>();
        regiment.Initialize(
            unitName,
            team,
            strength,
            isAI,
            position);
    }

    private static void SetPose(string unitName, Vector3 position, Quaternion rotation)
    {
        Regiment regiment = FindRegiment(unitName);
        if (regiment == null)
            return;

        position.y = PrototypeBootstrap.SampleGroundHeight(position.x, position.z) + 0.10f;
        regiment.transform.position = position;
        regiment.transform.rotation = rotation;
        regiment.SetFormation(RegimentFormation.Line);
    }

    private static Regiment FindRegiment(string unitName)
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return null;

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment != null && regiment.RegimentName == unitName)
                return regiment;
        }

        return null;
    }
}
