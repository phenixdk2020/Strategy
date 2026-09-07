using System.Collections;
using System.Reflection;
using UnityEngine;

// v00.00.09b TEST simplification.
// Individual decorative trees are NOT hard tactical obstacles. Regiments may pass
// through them. Buildings, fences and river/water remain navigation obstacles.
// This keeps prototype movement stable until proper forest/woodland terrain zones
// are introduced later as area-based movement/cohesion/visibility modifiers.
[DefaultExecutionOrder(1900)]
public sealed class PrototypeNavigation09BTreePassThrough : MonoBehaviour
{
    private bool applied;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeNavigation09BTreePassThrough>() != null)
            return;

        GameObject root = new GameObject("PrototypeNavigation09BTreePassThrough_v000009b");
        root.AddComponent<PrototypeNavigation09BTreePassThrough>();
    }

    private void Update()
    {
        if (applied)
            return;

        PrototypeBattlefieldNavigationV4 v4 =
            Object.FindAnyObjectByType<PrototypeBattlefieldNavigationV4>();

        if (v4 == null)
            return;

        // Disable the 09a anti-tree snag layer completely. 09b deliberately allows
        // passage through decorative trees instead of trying to steer around them.
        PrototypeNavigation09AHotfix hotfix09a =
            Object.FindAnyObjectByType<PrototypeNavigation09AHotfix>();
        if (hotfix09a != null)
            hotfix09a.enabled = false;

        int removed = 0;
        removed += RemoveTreeObstacles(v4);

        PrototypeBattlefieldNavigationV3 v3 =
            Object.FindAnyObjectByType<PrototypeBattlefieldNavigationV3>();
        if (v3 != null)
            removed += RemoveTreeObstacles(v3);

        PrototypeBattlefieldNavigationManager v1 =
            Object.FindAnyObjectByType<PrototypeBattlefieldNavigationManager>();
        if (v1 != null)
            removed += RemoveTreeObstacles(v1);

        PrototypeNavigationRecoveryManager recovery =
            Object.FindAnyObjectByType<PrototypeNavigationRecoveryManager>();
        if (recovery != null)
            removed += RemoveTreeObstacles(recovery);

        // V4 may already have generated routes in the first frame while trees were
        // still present. Clearing its private state forces a clean re-plan on the
        // next frame using the new tree-pass-through obstacle set.
        ClearPrivateCollection(v4, "states");

        applied = true;
        Debug.Log(string.Format(
            "NAV-09B|Installed=True|TreeRule=PassThrough|RemovedTreeObstacles={0}|BuildingsBlocked=True|RiverBlocked=True",
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
