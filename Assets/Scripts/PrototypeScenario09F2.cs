using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f2 isolation baseline, extended by v00.00.09f17.
// 09f17 keeps four Danish company-scale units versus one Prussian test company so the
// Major can practice frontage, reserve and flanking behavior without re-enabling legacy OOB.
[DefaultExecutionOrder(-12000)]
public sealed class PrototypeScenario09F2 : MonoBehaviour
{
    private bool applied;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeScenario09F2>() != null)
            return;

        GameObject root = new GameObject("PrototypeScenario_v000009f17");
        root.AddComponent<PrototypeScenario09F2>();
    }

    private void Update()
    {
        if (applied)
            return;

        PrototypeExpandedOOBManager expanded = Object.FindAnyObjectByType<PrototypeExpandedOOBManager>();
        if (expanded != null)
            expanded.enabled = false;

        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null || battle.Regiments.Count < 3)
            return;

        FieldInfo regimentsField = typeof(BattleManager).GetField(
            "regiments",
            BindingFlags.Instance | BindingFlags.NonPublic);

        List<Regiment> regiments = regimentsField != null
            ? regimentsField.GetValue(battle) as List<Regiment>
            : null;

        if (regiments == null)
        {
            Debug.LogError("SCENARIO-09F17|Applied=False|Reason=BattleManager.regiments_not_found");
            enabled = false;
            return;
        }

        EnsureDanishCompany(regiments, "2. Regiment", 600, new Vector3(-220f, 0f, 30f));
        EnsureDanishCompany(regiments, "3. Regiment", 595, new Vector3(-220f, 0f, 90f));

        for (int i = regiments.Count - 1; i >= 0; i--)
        {
            Regiment regiment = regiments[i];
            if (regiment == null)
            {
                regiments.RemoveAt(i);
                continue;
            }

            bool keep =
                regiment.RegimentName == "1. Regiment" ||
                regiment.RegimentName == "5. Regiment" ||
                regiment.RegimentName == "2. Regiment" ||
                regiment.RegimentName == "3. Regiment" ||
                regiment.RegimentName == "8th Regiment";

            if (keep)
                continue;

            regiments.RemoveAt(i);
            Destroy(regiment.gameObject);
        }

        Regiment danish1 = FindRegiment(regiments, "1. Regiment");
        Regiment danish2 = FindRegiment(regiments, "5. Regiment");
        Regiment danish3 = FindRegiment(regiments, "2. Regiment");
        Regiment danish4 = FindRegiment(regiments, "3. Regiment");
        Regiment prussian = FindRegiment(regiments, "8th Regiment");

        SetPose(danish1, new Vector3(-220f, 0f, -90f), Quaternion.Euler(0f, 90f, 0f));
        SetPose(danish2, new Vector3(-220f, 0f, -30f), Quaternion.Euler(0f, 90f, 0f));
        SetPose(danish3, new Vector3(-220f, 0f, 30f), Quaternion.Euler(0f, 90f, 0f));
        SetPose(danish4, new Vector3(-220f, 0f, 90f), Quaternion.Euler(0f, 90f, 0f));
        SetPose(prussian, new Vector3(220f, 0f, 0f), Quaternion.Euler(0f, -90f, 0f));

        Collider[] colliders = Object.FindObjectsByType<Collider>();
        int disabledSceneryColliders = 0;
        int preservedHouseColliders = 0;

        foreach (Collider collider in colliders)
        {
            if (collider == null)
                continue;

            if (collider.GetComponentInParent<Regiment>() != null)
                continue;

            if (collider.gameObject.name == "Battlefield Ground")
                continue;

            if (collider.gameObject.name == "Farmhouse" || collider.gameObject.name == "Barn")
            {
                collider.enabled = true;
                preservedHouseColliders++;
                continue;
            }

            collider.enabled = false;
            disabledSceneryColliders++;
        }

        applied = true;
        Debug.Log(
            "SCENARIO-09F17|Applied=True|DenmarkCompanies=4|PrussiaCompanies=1|" +
            "InternalDanishIds=1.Regiment,5.Regiment,2.Regiment,3.Regiment|" +
            "HardBlockers=Farmhouse,Barn|HouseColliders=" + preservedHouseColliders +
            "|DisabledOtherSceneryColliders=" + disabledSceneryColliders);
    }

    private static void EnsureDanishCompany(
        List<Regiment> regiments,
        string name,
        int strength,
        Vector3 position)
    {
        if (FindRegiment(regiments, name) != null)
            return;

        position.y = PrototypeBootstrap.SampleGroundHeight(position.x, position.z) + 0.10f;
        GameObject unit = new GameObject(name);
        unit.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        Regiment regiment = unit.AddComponent<Regiment>();
        regiment.Initialize(name, BattleTeam.Denmark, strength, false, position);

        // Regiment.Initialize registers itself with BattleManager; regiments references
        // that same authoritative list, so no separate Add is required here.
        Debug.Log("SCENARIO-09F17|SpawnedExtraCompany=True|InternalId=" + name);
    }

    private static Regiment FindRegiment(List<Regiment> regiments, string name)
    {
        foreach (Regiment regiment in regiments)
        {
            if (regiment != null && regiment.RegimentName == name)
                return regiment;
        }
        return null;
    }

    private static void SetPose(Regiment regiment, Vector3 position, Quaternion rotation)
    {
        if (regiment == null)
            return;

        position.y = PrototypeBootstrap.SampleGroundHeight(position.x, position.z) + 0.10f;
        regiment.transform.position = position;
        regiment.transform.rotation = rotation;
        regiment.SetFormation(RegimentFormation.Line);
        regiment.OrderHold();
    }
}
