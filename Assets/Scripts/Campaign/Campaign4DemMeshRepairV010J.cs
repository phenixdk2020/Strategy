using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Campaign4 v00.00.10k runtime repair for the inherited v10g DEM generator.
///
/// The v10g BuildDemTile triangle order produces downward-facing normals in the
/// WGS84 X/Z projection. In the Campaign4 perspective camera this can make the
/// terrain disappear because the material back-face culls the mesh.
///
/// This component watches streamed DEM tiles and reverses only meshes whose
/// calculated average normal points downward. It is deliberately isolated to
/// Campaign4 so Campaign3 remains untouched.
///
/// v00.00.10k Unity 6.6 compatibility:
/// - Removed Object.GetInstanceID(), which is obsolete/error-level in Unity 6.6.
/// - Tracks processed meshes directly with HashSet<Mesh> instead of numeric IDs.
/// </summary>
[DefaultExecutionOrder(31900)]
public sealed class Campaign4DemMeshRepairV010J : MonoBehaviour
{
    private readonly HashSet<Mesh> checkedMeshes = new HashSet<Mesh>();
    private float nextScan;
    private int repairedCount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<Campaign4DemMeshRepairV010J>() != null)
            return;

        GameObject root = new GameObject("CAMPAIGN4_v10k_DEM_Mesh_Repair");
        DontDestroyOnLoad(root);
        root.AddComponent<Campaign4DemMeshRepairV010J>();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextScan)
            return;

        nextScan = Time.unscaledTime + 0.35f;
        RepairNewDemMeshes();
    }

    private void RepairNewDemMeshes()
    {
        MeshFilter[] filters = Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include);
        int newlyRepaired = 0;

        for (int i = 0; i < filters.Length; i++)
        {
            MeshFilter filter = filters[i];
            if (filter == null || filter.sharedMesh == null)
                continue;

            GameObject go = filter.gameObject;
            if (go == null || !go.name.StartsWith("DEM_"))
                continue;

            Mesh mesh = filter.sharedMesh;

            // Unity 6.6 marks Object.GetInstanceID() obsolete/error-level.
            // A direct Mesh reference is sufficient because each streamed DEM tile
            // owns one mesh instance for its lifetime.
            if (!checkedMeshes.Add(mesh))
                continue;

            mesh.RecalculateNormals();
            Vector3[] normals = mesh.normals;
            if (normals == null || normals.Length == 0)
                continue;

            float sumY = 0f;
            int samples = 0;
            int step = Mathf.Max(1, normals.Length / 64);
            for (int n = 0; n < normals.Length; n += step)
            {
                sumY += normals[n].y;
                samples++;
            }

            if (samples == 0 || sumY / samples >= 0f)
                continue;

            int[] triangles = mesh.triangles;
            for (int t = 0; t + 2 < triangles.Length; t += 3)
            {
                int swap = triangles[t + 1];
                triangles[t + 1] = triangles[t + 2];
                triangles[t + 2] = swap;
            }

            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            newlyRepaired++;
            repairedCount++;
        }

        if (newlyRepaired > 0)
        {
            Debug.Log(
                "CAMPAIGN4-DEM-REPAIR|Version=v00.00.10k|New=" + newlyRepaired +
                "|Total=" + repairedCount + "|UpwardNormals=True");
        }
    }
}
