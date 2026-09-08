using System.Collections;
using System.Reflection;
using UnityEngine;

// v00.00.09f TEST tree pass-through compatibility layer.
// Individual decorative trees are NOT hard tactical obstacles. Movement V3 remains
// the sole steering owner; this component only removes Tree entries from navigation
// obstacle collections and clears stale V3/V4 route state once so old tree detours
// cannot persist after the rule is applied.
[DefaultExecutionOrder(1900)]
public sealed class PrototypeNavigation09BTreePassThrough : MonoBehaviour
{
    private bool applied;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeNavigation09BTreePassThrough>() != null)
            return;

        GameObject root = new GameObject("PrototypeNavigation09BTreePassThrough_v000009f");
        root.AddComponent<PrototypeNavigation09BTreePassThrough>();
    }

    private void Update()
    {
        if (applied)
            return;

        PrototypeBattlefieldNavigationV3 v3 =
            Object.FindAnyObjectByType<PrototypeBattlefieldNavigationV3>();
        if (v3 == null)
            return;

        // Keep the 09A anti-tree-snag layer completely disabled. Decorative trees
        // are pass-through instead of obstacles requiring local avoidance.
        PrototypeNavigation09AHotfix hotfix09a =
            Object.FindAnyObjectByType<PrototypeNavigation09AHotfix>();
        if (hotfix09a != null)
            hotfix09a.enabled = false;

        int removed = 0;
        removed += RemoveTreeObstacles(v3);

        // Clear V3 state after removing trees. This is essential: an already-created
        // persistent detour may still hold a direct reference to a Tree obstacle even
        // after that obstacle has been removed from the list.
        ClearPrivateCollection(v3, "states");

        PrototypeBattlefieldNavigationV4 v4 =
            Object.FindAnyObjectByType<PrototypeBattlefieldNavigationV4>();
        if (v4 != null)
        {
            removed += RemoveTreeObstacles(v4);
            ClearPrivateCollection(v4, "states");
        }

        PrototypeBattlefieldNavigationManager v1 =
            Object.FindAnyObjectByType<PrototypeBattlefieldNavigationManager>();
        if (v1 != null)
            removed += RemoveTreeObstacles(v1);

        PrototypeNavigationRecoveryManager recovery =
            Object.FindAnyObjectByType<PrototypeNavigationRecoveryManager>();
        if (recovery != null)
            removed += RemoveTreeObstacles(recovery);

        applied = true;
        Debug.Log(string.Format(
            "NAV-09B|Build=v00.00.09f|TreeRule=PassThrough|RemovedTreeObstacles={0}|V3StatesReset=True|BuildingsBlocked=True|FencesBlocked=True|RiverBlocked=True",
            removed));
    }

    private static int RemoveTreeObstacles(object manager)
    {
        if (manager == null)
            return 0;

        FieldInfo obstaclesField = manager.GetType().GetField(
            "obstacles",
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (obstaclesField == null)
            return 0;

        IList list = obstaclesField.GetValue(manager) as IList;
        if (list == null)
            return 0;

        int removed = 0;
        for (int i = list.Count - 1; i >= 0; i--)
        {
            object obstacle = list[i];
            if (obstacle == null)
                continue;

            FieldInfo nameField = obstacle.GetType().GetField(
                "Name",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            string name = nameField != null ? nameField.GetValue(obstacle) as string : null;
            if (!string.Equals(name, "Tree", System.StringComparison.Ordinal))
                continue;

            list.RemoveAt(i);
            removed++;
        }

        return removed;
    }

    private static void ClearPrivateCollection(object owner, string fieldName)
    {
        if (owner == null)
            return;

        FieldInfo field = owner.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        object value = field != null ? field.GetValue(owner) : null;
        IDictionary dictionary = value as IDictionary;
        if (dictionary != null)
            dictionary.Clear();
    }
}
