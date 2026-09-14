using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f2 isolation baseline, extended by v00.00.09f15.
// 09f15 keeps two Danish company-scale units versus one Prussian test company so the
// new Major HQ can command two subordinates without re-enabling the larger legacy OOB.
[DefaultExecutionOrder(-12000)]
public sealed class PrototypeScenario09F2 : MonoBehaviour
{
    private bool applied;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeScenario09F2>() != null)
            return;

        GameObject root = new GameObject("PrototypeScenario_v000009f2");
        root.AddComponent<PrototypeScenario09F2>();
    }

    private void Update()
    {
        if (applied)
            return;

        PrototypeExpandedOOBManager expanded =
            Object.FindAnyObjectByType<PrototypeExpandedOOBManager>();
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
            Debug.LogError("SCENARIO-09F15|Applied=False|Reason=BattleManager.regiments_not_found");
            enabled = false;
            return;
        }

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
                regiment.RegimentName == "8th Regiment";

            if (keep)
                continue;

            regiments.RemoveAt(i);
            Destroy(regiment.gameObject);
        }

        Regiment danish1 = FindRegiment(regiments, "1. Regiment");
        Regiment danish2 = FindRegiment(regiments, "5. Regiment");
        Regiment prussian = FindRegiment(regiments, "8th Regiment");

        SetPose(danish1, new Vector3(-220f, 0f, -54f), Quaternion.Euler(0f, 90f, 0f));
        SetPose(danish2, new Vector3(-220f, 0f, 54f), Quaternion.Euler(0f, 90f, 0f));
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

            if (collider.gameObject.name == "Farmhouse" ||
                collider.gameObject.name == "Barn")
            {
                collider.enabled = true;
                preservedHouseColliders++;
                continue;
            }

            // The Major HQ is created after this isolation pass and therefore keeps
            // its own selection collider. All existing scenery remains non-blocking
            // except the explicitly approved building blockers above.
            collider.enabled = false;
            disabledSceneryColliders++;
        }

        applied = true;
        Debug.Log(
            "SCENARIO-09F15|Applied=True|DenmarkCompanies=2|PrussiaCompanies=1|" +
            "InternalDanishIds=1.Regiment,5.Regiment|HardBlockers=Farmhouse,Barn|" +
            "HouseColliders=" + preservedHouseColliders +
            "|DisabledOtherSceneryColliders=" + disabledSceneryColliders);
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
