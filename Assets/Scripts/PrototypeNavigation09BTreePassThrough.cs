using System.Collections;
using System.Reflection;
using UnityEngine;

// v00.00.09h4 TEST soft-obstacle compatibility layer.
// Individual decorative trees and fence posts are NOT hard tactical obstacles.
// Movement V3 remains the sole steering owner; this component only removes soft
// visual obstacles from navigation collections and clears stale route state once.
[DefaultExecutionOrder(1900)]
public sealed class PrototypeNavigation09BTreePassThrough : MonoBehaviour
{
    private bool applied;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeNavigation09BTreePassThrough>() != null)
            return;

        GameObject root = new GameObject("PrototypeNavigationSoftPassThrough_v000009h4");
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

        PrototypeNavigation09AHotfix hotfix09a =
            Object.FindAnyObjectByType<PrototypeNavigation09AHotfix>();
        if (hotfix09a != null)
            hotfix09a.enabled = false;

        int removedTrees = 0;
        int removedFencePosts = 0;

        RemoveSoftObstacles(v3, ref removedTrees, ref removedFencePosts);
        ClearPrivateCollection(v3, "states");

        PrototypeBattlefieldNavigationV4 v4 =
            Object.FindAnyObjectByType<PrototypeBattlefieldNavigationV4>();
        if (v4 != null)
        {
            RemoveSoftObstacles(v4, ref removedTrees, ref removedFencePosts);
            ClearPrivateCollection(v4, "states");
        }

        PrototypeBattlefieldNavigationManager v1 =
            Object.FindAnyObjectByType<PrototypeBattlefieldNavigationManager>();
        if (v1 != null)
            RemoveSoftObstacles(v1, ref removedTrees, ref removedFencePosts);

        PrototypeNavigationRecoveryManager recovery =
            Object.FindAnyObjectByType<PrototypeNavigationRecoveryManager>();
        if (recovery != null)
            RemoveSoftObstacles(recovery, ref removedTrees, ref removedFencePosts);

        applied = true;
        Debug.Log(string.Format(
            "NAV-SOFT-09H4|Build=v00.00.09h4|Trees=PassThrough|FencePosts=PassThrough|RemovedTrees={0}|RemovedFencePosts={1}|V3StatesReset=True|BuildingsBlocked=True|RiverBlocked=True",
            removedTrees,
            removedFencePosts));
    }

    private static void RemoveSoftObstacles(
        object manager,
        ref int removedTrees,
        ref int removedFencePosts)
    {
        if (manager == null)
            return;

        FieldInfo obstaclesField = manager.GetType().GetField(
            "obstacles",
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (obstaclesField == null)
            return;

        IList list = obstaclesField.GetValue(manager) as IList;
        if (list == null)
            return;

        for (int i = list.Count - 1; i >= 0; i--)
        {
            object obstacle = list[i];
            if (obstacle == null)
                continue;

            FieldInfo nameField = obstacle.GetType().GetField(
                "Name",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            string name = nameField != null ? nameField.GetValue(obstacle) as string : null;
            if (string.Equals(name, "Tree", System.StringComparison.Ordinal))
            {
                list.RemoveAt(i);
                removedTrees++;
            }
            else if (string.Equals(name, "FencePost", System.StringComparison.Ordinal))
            {
                list.RemoveAt(i);
                removedFencePosts++;
            }
        }
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
