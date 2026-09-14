using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f22
// Prevents the classic oscillation where a Line destination itself lies inside the
// formation-clearance area of Farmhouse/Barn. Before the static router sees the goal,
// the endpoint is projected to the nearest legal edge of the expanded blocker.
[DefaultExecutionOrder(-210)]
public sealed class PrototypeObstacleEndpointGuard09F22 : MonoBehaviour
{
    private FieldInfo destinationField;
    private FieldInfo hasDestinationField;
    private Renderer[] blockers;
    private readonly Dictionary<Regiment, float> nextLog = new Dictionary<Regiment, float>();

    private const float LineHalfWidth = 25f;
    private const float ColumnHalfWidth = 3.4f;
    private const float Pad = 1.5f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeObstacleEndpointGuard09F22>() == null)
            new GameObject("PrototypeObstacleEndpointGuard_v000009f22").AddComponent<PrototypeObstacleEndpointGuard09F22>();
    }

    private void Awake()
    {
        BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        destinationField = typeof(Regiment).GetField("destination", flags);
        hasDestinationField = typeof(Regiment).GetField("hasDestination", flags);
        Scan();
    }

    private void Update()
    {
        if (destinationField == null || hasDestinationField == null)
            return;
        if (blockers == null || blockers.Length == 0)
            Scan();

        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null || regiment.IsRouted)
                continue;
            if (!(bool)hasDestinationField.GetValue(regiment))
                continue;

            Vector3 goal = (Vector3)destinationField.GetValue(regiment);
            float clearance = regiment.Formation == RegimentFormation.Line ? LineHalfWidth : ColumnHalfWidth;
            Vector3 corrected = CorrectEndpoint(goal, clearance, out string blockerName);
            if (PlanarDistance(goal, corrected) < 0.05f)
                continue;

            corrected.y = PrototypeBootstrap.SampleGroundHeight(corrected.x, corrected.z) + 0.10f;
            destinationField.SetValue(regiment, corrected);

            if (!nextLog.TryGetValue(regiment, out float allowed) || Time.time >= allowed)
            {
                nextLog[regiment] = Time.time + 2f;
                Debug.Log("OBSTACLE-ENDPOINT-09F22|Unit=" + regiment.RegimentName +
                          "|BlockedBy=" + blockerName + "|EndpointProjected=True|Formation=" + regiment.Formation);
            }
        }
    }

    private void Scan()
    {
        Renderer[] all = UnityEngine.Object.FindObjectsByType<Renderer>();
        List<Renderer> found = new List<Renderer>();
        foreach (Renderer renderer in all)
        {
            if (renderer == null)
                continue;
            string name = renderer.gameObject.name;
            if (name == "Farmhouse" || name == "Barn")
                found.Add(renderer);
        }
        blockers = found.ToArray();
    }

    private Vector3 CorrectEndpoint(Vector3 goal, float clearance, out string blockerName)
    {
        blockerName = string.Empty;
        Vector3 corrected = goal;

        foreach (Renderer renderer in blockers)
        {
            if (renderer == null)
                continue;

            Bounds b = renderer.bounds;
            float minX = b.min.x - clearance - Pad;
            float maxX = b.max.x + clearance + Pad;
            float minZ = b.min.z - clearance - Pad;
            float maxZ = b.max.z + clearance + Pad;

            if (corrected.x < minX || corrected.x > maxX || corrected.z < minZ || corrected.z > maxZ)
                continue;

            float dLeft = Mathf.Abs(corrected.x - minX);
            float dRight = Mathf.Abs(maxX - corrected.x);
            float dBottom = Mathf.Abs(corrected.z - minZ);
            float dTop = Mathf.Abs(maxZ - corrected.z);
            float best = Mathf.Min(Mathf.Min(dLeft, dRight), Mathf.Min(dBottom, dTop));

            if (best == dLeft) corrected.x = minX;
            else if (best == dRight) corrected.x = maxX;
            else if (best == dBottom) corrected.z = minZ;
            else corrected.z = maxZ;

            blockerName = renderer.gameObject.name;
        }

        return corrected;
    }

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
