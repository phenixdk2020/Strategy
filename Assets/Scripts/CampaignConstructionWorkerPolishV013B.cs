using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Overrides the deliberately simple v13 worker loop with a calmer, readable
// construction animation. Presentation only; project progress remains authoritative
// in CampaignConstructionV013 and CampaignSession.CurrentDateTime.
[DefaultExecutionOrder(32000)]
public sealed class CampaignConstructionWorkerPolishV013B : MonoBehaviour
{
    private sealed class WorkerState
    {
        public Transform Worker;
        public Vector3 BasePosition;
        public float PhaseOffset;
        public float Side;
    }

    private readonly List<WorkerState> workers = new List<WorkerState>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!string.Equals(scene.name, "CampaignMap", StringComparison.Ordinal))
            return;

        if (UnityEngine.Object.FindAnyObjectByType<CampaignConstructionWorkerPolishV013B>() != null)
            return;

        GameObject root = new GameObject("CampaignConstructionWorkerPolishV013B");
        root.AddComponent<CampaignConstructionWorkerPolishV013B>();
    }

    private void Start()
    {
        RebuildWorkerCache();
    }

    private void Update()
    {
        if (workers.Count == 0)
            RebuildWorkerCache();

        double hours = (CampaignSession.CurrentDateTime - new DateTime(1864, 1, 1)).TotalHours;
        float t = (float)hours;

        for (int i = 0; i < workers.Count; i++)
        {
            WorkerState state = workers[i];
            Transform worker = state.Worker;
            if (worker == null || !worker.gameObject.activeInHierarchy)
                continue;

            float phase = t * 1.35f + state.PhaseOffset;
            float walk = Mathf.Sin(phase * 0.72f);
            float work = Mathf.Sin(phase * 2.4f);

            worker.localPosition = state.BasePosition + new Vector3(
                walk * 0.85f,
                Mathf.Abs(work) * 0.06f,
                Mathf.Cos(phase * 0.56f) * 0.38f);

            float yaw = state.Side > 0f ? 205f : 25f;
            yaw += walk * 12f;
            worker.localRotation = Quaternion.Euler(work * 3.5f, yaw, work * 4f);
        }
    }

    private void RebuildWorkerCache()
    {
        workers.Clear();

        GameObject[] projects =
        {
            GameObject.Find("ConstructionProject_QA-BARRACKS-AALBORG"),
            GameObject.Find("ConstructionProject_QA-FARM-AARHUS")
        };

        foreach (GameObject project in projects)
        {
            if (project == null)
                continue;

            Transform[] transforms = project.GetComponentsInChildren<Transform>(true);
            int index = 0;
            foreach (Transform candidate in transforms)
            {
                if (candidate == null || !candidate.name.StartsWith("Worker_", StringComparison.Ordinal))
                    continue;

                float side = index % 2 == 0 ? -1f : 1f;
                workers.Add(new WorkerState
                {
                    Worker = candidate,
                    BasePosition = new Vector3(side < 0f ? -3.0f : 3.1f, 0.8f, side < 0f ? -2.2f : 2.0f),
                    PhaseOffset = index * 1.85f + (project.name.IndexOf("FARM", StringComparison.Ordinal) >= 0 ? 0.9f : 0f),
                    Side = side
                });
                index++;
            }
        }
    }
}
