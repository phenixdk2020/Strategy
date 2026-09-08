using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(200)]
public sealed class CampaignConstructionV013 : MonoBehaviour
{
    private sealed class ProjectVisual
    {
        public string Id;
        public string Type;
        public DateTime Start;
        public double DurationHours;
        public GameObject Root;
        public readonly List<GameObject> Stages = new List<GameObject>();
        public readonly List<Transform> Workers = new List<Transform>();
        public int CurrentStage = -1;
    }

    private readonly List<ProjectVisual> projects = new List<ProjectVisual>();
    private Material wood;
    private Material stone;
    private Material wall;
    private Material roof;
    private Material worker;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!string.Equals(scene.name, "CampaignMap", StringComparison.Ordinal))
            return;

        if (UnityEngine.Object.FindAnyObjectByType<CampaignConstructionV013>() != null)
            return;

        GameObject root = new GameObject("CampaignConstructionV013");
        root.AddComponent<CampaignConstructionV013>();
    }

    private void Start()
    {
        CampaignSession.EnsureInitialized();
        BuildMaterials();
        CreateDemoProjects();
    }

    private void Update()
    {
        DateTime now = CampaignSession.CurrentDateTime;
        double animationHours = (now - new DateTime(1864, 1, 1)).TotalHours;

        foreach (ProjectVisual project in projects)
        {
            double raw = project.DurationHours <= 0.0 ? 1.0 : (now - project.Start).TotalHours / project.DurationHours;
            float progress = Mathf.Clamp01((float)raw);
            int stage = progress >= 1f ? 5 : Mathf.Clamp(Mathf.FloorToInt(progress * 5f), 0, 4);

            if (stage != project.CurrentStage)
            {
                project.CurrentStage = stage;
                for (int i = 0; i < project.Stages.Count; i++)
                    project.Stages[i].SetActive(i == stage);

                Debug.Log(string.Format(
                    "CAMPAIGN-V013-CONSTRUCTION|Id={0}|Type={1}|Progress={2:0.00}|Stage={3}|Time={4:yyyy-MM-dd HH:mm}",
                    project.Id,
                    project.Type,
                    progress,
                    stage,
                    now));
            }

            bool activeWork = stage > 0 && stage < 5;
            for (int i = 0; i < project.Workers.Count; i++)
            {
                Transform w = project.Workers[i];
                if (w == null)
                    continue;
                w.gameObject.SetActive(activeWork);
                if (!activeWork)
                    continue;

                float phase = (float)(animationHours * 1.8 + i * 1.77);
                Vector3 basePos = new Vector3(i == 0 ? -3.0f : 3.1f, 0.8f, i == 0 ? -2.2f : 2.0f);
                w.localPosition = basePos + new Vector3(Mathf.Sin(phase) * 1.25f, Mathf.Abs(Mathf.Sin(phase * 2f)) * 0.18f, Mathf.Cos(phase * 0.7f) * 0.7f);
                w.localRotation = Quaternion.Euler(0f, phase * 22f, Mathf.Sin(phase * 3f) * 8f);
            }
        }
    }

    private void CreateDemoProjects()
    {
        DateTime epoch = new DateTime(1864, 2, 1, 8, 0, 0);
        CreateBarracksProject("QA-BARRACKS-AALBORG", "AALBORG", new Vector3(10f, 0f, 8f), epoch.AddHours(2), 96.0);
        CreateFarmProject("QA-FARM-AARHUS", "AARHUS", new Vector3(-11f, 0f, 8f), epoch.AddHours(10), 72.0);
    }

    private void CreateBarracksProject(string id, string nodeId, Vector3 offset, DateTime start, double durationHours)
    {
        CampaignNodeState node = CampaignSession.GetNode(nodeId);
        if (node == null)
            return;

        ProjectVisual p = NewProject(id, "Barracks", node, offset, start, durationHours);
        p.Stages.Add(CreateSiteStage(p.Root.transform));
        p.Stages.Add(CreateFoundationStage(p.Root.transform, new Vector3(8f, 0.7f, 5f)));
        p.Stages.Add(CreateFrameStage(p.Root.transform, new Vector3(8f, 4.0f, 5f)));
        p.Stages.Add(CreatePartialBuildingStage(p.Root.transform, new Vector3(8f, 4.0f, 5f), false));
        p.Stages.Add(CreatePartialBuildingStage(p.Root.transform, new Vector3(8f, 4.0f, 5f), true));
        p.Stages.Add(CreateCompletedBuildingStage(p.Root.transform, new Vector3(8f, 4.0f, 5f), "Barracks_Complete"));
        CreateWorkers(p);
        projects.Add(p);
    }

    private void CreateFarmProject(string id, string nodeId, Vector3 offset, DateTime start, double durationHours)
    {
        CampaignNodeState node = CampaignSession.GetNode(nodeId);
        if (node == null)
            return;

        ProjectVisual p = NewProject(id, "Farm", node, offset, start, durationHours);
        p.Stages.Add(CreateSiteStage(p.Root.transform));
        p.Stages.Add(CreateFoundationStage(p.Root.transform, new Vector3(6f, 0.6f, 4.5f)));
        p.Stages.Add(CreateFrameStage(p.Root.transform, new Vector3(6f, 3.5f, 4.5f)));
        p.Stages.Add(CreatePartialBuildingStage(p.Root.transform, new Vector3(6f, 3.5f, 4.5f), false));
        p.Stages.Add(CreateFarmNearCompleteStage(p.Root.transform));
        p.Stages.Add(CreateFarmCompleteStage(p.Root.transform));
        CreateWorkers(p);
        projects.Add(p);
    }

    private ProjectVisual NewProject(string id, string type, CampaignNodeState node, Vector3 offset, DateTime start, double durationHours)
    {
        GameObject root = new GameObject("ConstructionProject_" + id);
        root.transform.SetParent(CampaignTerrainV013.GetOrCreateLayerRoot(CampaignTerrainV013.RootLivingWorld).transform, false);
        float y = CampaignTerrainV013.SampleSurfaceY(node.MapPosition.x + offset.x, node.MapPosition.y + offset.z);
        root.transform.position = new Vector3(node.MapPosition.x + offset.x, y, node.MapPosition.y + offset.z);

        return new ProjectVisual
        {
            Id = id,
            Type = type,
            Start = start,
            DurationHours = durationHours,
            Root = root
        };
    }

    private GameObject CreateSiteStage(Transform parent)
    {
        GameObject stage = NewStage(parent, "Stage0_SITE");
        for (int i = 0; i < 4; i++)
        {
            float x = i < 2 ? -4f : 4f;
            float z = (i % 2 == 0) ? -3f : 3f;
            CreateBlock(stage.transform, "SurveyStake", new Vector3(x, 0.55f, z), new Vector3(0.25f, 1.1f, 0.25f), wood);
        }
        return stage;
    }

    private GameObject CreateFoundationStage(Transform parent, Vector3 footprint)
    {
        GameObject stage = NewStage(parent, "Stage1_FOUNDATION");
        CreateBlock(stage.transform, "Foundation", new Vector3(0f, footprint.y * 0.5f, 0f), new Vector3(footprint.x, footprint.y, footprint.z), stone);
        CreateBlock(stage.transform, "TimberStack", new Vector3(-5f, 0.45f, 3.5f), new Vector3(3f, 0.6f, 1.3f), wood);
        return stage;
    }

    private GameObject CreateFrameStage(Transform parent, Vector3 size)
    {
        GameObject stage = NewStage(parent, "Stage2_FRAME_SCAFFOLD");
        float hx = size.x * 0.5f;
        float hz = size.z * 0.5f;
        float h = size.y;
        for (int xi = -1; xi <= 1; xi += 2)
        {
            for (int zi = -1; zi <= 1; zi += 2)
                CreateBlock(stage.transform, "Post", new Vector3(xi * hx, h * 0.5f, zi * hz), new Vector3(0.35f, h, 0.35f), wood);
        }
        CreateBlock(stage.transform, "TopBeamA", new Vector3(0f, h, -hz), new Vector3(size.x + 0.5f, 0.3f, 0.3f), wood);
        CreateBlock(stage.transform, "TopBeamB", new Vector3(0f, h, hz), new Vector3(size.x + 0.5f, 0.3f, 0.3f), wood);
        CreateBlock(stage.transform, "Scaffold", new Vector3(size.x * 0.62f, h * 0.55f, 0f), new Vector3(0.25f, h * 1.05f, size.z * 1.2f), wood);
        return stage;
    }

    private GameObject CreatePartialBuildingStage(Transform parent, Vector3 size, bool withRoof)
    {
        GameObject stage = NewStage(parent, withRoof ? "Stage4_NEAR_COMPLETE" : "Stage3_PARTIAL");
        float wallHeight = withRoof ? size.y : size.y * 0.68f;
        CreateBlock(stage.transform, "Body", new Vector3(0f, wallHeight * 0.5f, 0f), new Vector3(size.x, wallHeight, size.z), wall);
        if (withRoof)
            CreateRoof(stage.transform, new Vector3(0f, size.y + 0.4f, 0f), size.x, size.z);
        else
            CreateBlock(stage.transform, "Scaffold", new Vector3(size.x * 0.62f, size.y * 0.55f, 0f), new Vector3(0.25f, size.y, size.z * 1.2f), wood);
        return stage;
    }

    private GameObject CreateCompletedBuildingStage(Transform parent, Vector3 size, string name)
    {
        GameObject stage = NewStage(parent, "Stage5_COMPLETE");
        CreateBlock(stage.transform, name, new Vector3(0f, size.y * 0.5f, 0f), size, wall);
        CreateRoof(stage.transform, new Vector3(0f, size.y + 0.45f, 0f), size.x, size.z);
        CreateBlock(stage.transform, "Door", new Vector3(0f, 1.0f, -size.z * 0.51f), new Vector3(1.2f, 2.0f, 0.18f), wood);
        return stage;
    }

    private GameObject CreateFarmNearCompleteStage(Transform parent)
    {
        GameObject stage = CreatePartialBuildingStage(parent, new Vector3(6f, 3.5f, 4.5f), true);
        stage.name = "Stage4_NEAR_COMPLETE_FARM";
        CreateFence(stage.transform);
        return stage;
    }

    private GameObject CreateFarmCompleteStage(Transform parent)
    {
        GameObject stage = CreateCompletedBuildingStage(parent, new Vector3(6f, 3.5f, 4.5f), "Farmhouse_Complete");
        stage.name = "Stage5_COMPLETE_FARM";
        CreateFence(stage.transform);
        CreateBlock(stage.transform, "HayStack", new Vector3(5.5f, 0.9f, 2.5f), new Vector3(2.0f, 1.8f, 2.0f), wood);
        return stage;
    }

    private void CreateFence(Transform parent)
    {
        for (int i = -3; i <= 3; i++)
        {
            CreateBlock(parent, "FencePost", new Vector3(i * 2f, 0.65f, 6f), new Vector3(0.18f, 1.3f, 0.18f), wood);
        }
        CreateBlock(parent, "FenceRail", new Vector3(0f, 0.75f, 6f), new Vector3(12.5f, 0.18f, 0.18f), wood);
    }

    private void CreateWorkers(ProjectVisual p)
    {
        for (int i = 0; i < 2; i++)
        {
            GameObject w = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            w.name = "Worker_" + (i + 1);
            w.transform.SetParent(p.Root.transform, false);
            w.transform.localScale = new Vector3(0.45f, 0.75f, 0.45f);
            w.GetComponent<Renderer>().sharedMaterial = worker;
            p.Workers.Add(w.transform);
        }
    }

    private static GameObject NewStage(Transform parent, string name)
    {
        GameObject stage = new GameObject(name);
        stage.transform.SetParent(parent, false);
        stage.SetActive(false);
        return stage;
    }

    private void CreateRoof(Transform parent, Vector3 localPosition, float width, float depth)
    {
        GameObject r = CreateBlock(parent, "Roof", localPosition, new Vector3(width * 0.78f, 0.85f, depth * 1.08f), roof);
        r.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
    }

    private static GameObject CreateBlock(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = name;
        block.transform.SetParent(parent, false);
        block.transform.localPosition = localPosition;
        block.transform.localScale = localScale;
        block.GetComponent<Renderer>().sharedMaterial = material;
        return block;
    }

    private void BuildMaterials()
    {
        wood = CreateMaterial(new Color(0.43f, 0.27f, 0.14f), "Construction_Wood");
        stone = CreateMaterial(new Color(0.43f, 0.43f, 0.40f), "Construction_Stone");
        wall = CreateMaterial(new Color(0.62f, 0.56f, 0.45f), "Construction_Wall");
        roof = CreateMaterial(new Color(0.29f, 0.18f, 0.14f), "Construction_Roof");
        worker = CreateMaterial(new Color(0.25f, 0.29f, 0.34f), "Construction_Worker");
    }

    private static Material CreateMaterial(Color color, string name)
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");
        return new Material(shader) { name = name, color = color };
    }
}
