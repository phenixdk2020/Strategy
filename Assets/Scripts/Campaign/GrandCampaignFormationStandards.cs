using System.Collections.Generic;
using UnityEngine;

// v00.00.10d CAMPAIGN TEST: readable formation standards on current army tokens.
// Individual regimental colours appear when the formation is later unfolded.
[DefaultExecutionOrder(5000)]
public sealed class GrandCampaignFormationStandards : MonoBehaviour
{
    private readonly HashSet<GrandCampaignArmyMarker> installed =
        new HashSet<GrandCampaignArmyMarker>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<GrandCampaignFormationStandards>() != null)
            return;

        GameObject root = new GameObject("GrandCampaignFormationStandards_v000010d");
        root.AddComponent<GrandCampaignFormationStandards>();
    }

    private void Update()
    {
        GrandCampaignArmyMarker[] markers =
            UnityEngine.Object.FindObjectsByType<GrandCampaignArmyMarker>();

        foreach (GrandCampaignArmyMarker marker in markers)
        {
            if (marker == null || installed.Contains(marker))
                continue;

            CreateStandard(marker);
            installed.Add(marker);
        }
    }

    private static void CreateStandard(GrandCampaignArmyMarker marker)
    {
        string id = marker.ArmyId ?? string.Empty;
        Color field = GetFieldColor(id);
        Color device = GetDeviceColor(id);

        Material poleMaterial = CreateMaterial(new Color(0.30f, 0.20f, 0.09f), "CampaignStandardPole");
        Material fieldMaterial = CreateMaterial(field, "CampaignStandardField_" + id);
        Material deviceMaterial = CreateMaterial(device, "CampaignStandardDevice_" + id);

        GameObject root = new GameObject("CampaignStandard_" + id);
        root.transform.SetParent(marker.transform, false);
        root.transform.localPosition = new Vector3(0f, 0.65f, 0f);

        GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pole.name = "Pole";
        pole.transform.SetParent(root.transform, false);
        pole.transform.localPosition = new Vector3(0f, 1.3f, 0f);
        pole.transform.localScale = new Vector3(0.07f, 1.3f, 0.07f);
        pole.GetComponent<Renderer>().sharedMaterial = poleMaterial;
        UnityEngine.Object.Destroy(pole.GetComponent<Collider>());

        GameObject cloth = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cloth.name = "Flag";
        cloth.transform.SetParent(root.transform, false);
        cloth.transform.localPosition = new Vector3(0.75f, 2.15f, 0f);
        cloth.transform.localScale = new Vector3(1.45f, 0.80f, 0.06f);
        cloth.GetComponent<Renderer>().sharedMaterial = fieldMaterial;
        UnityEngine.Object.Destroy(cloth.GetComponent<Collider>());

        GameObject deviceBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        deviceBar.name = "IdentityDevice";
        deviceBar.transform.SetParent(root.transform, false);
        deviceBar.transform.localPosition = new Vector3(0.75f, 2.15f, -0.04f);
        deviceBar.transform.localScale = new Vector3(0.55f, 0.16f, 0.025f);
        deviceBar.GetComponent<Renderer>().sharedMaterial = deviceMaterial;
        UnityEngine.Object.Destroy(deviceBar.GetComponent<Collider>());

        Debug.Log("CAMPAIGN-STANDARD|Formation=" + id + "|Created=True|Art=QAPlaceholder");
    }

    private static Color GetFieldColor(string id)
    {
        if (id.StartsWith("DK-")) return new Color(0.72f, 0.08f, 0.10f);
        if (id.StartsWith("SN-")) return new Color(0.18f, 0.38f, 0.70f);
        if (id.StartsWith("PR-")) return new Color(0.90f, 0.88f, 0.80f);
        if (id.StartsWith("AT-")) return new Color(0.88f, 0.82f, 0.74f);
        if (id.StartsWith("FR-")) return new Color(0.24f, 0.36f, 0.72f);
        if (id.StartsWith("UK-")) return new Color(0.65f, 0.12f, 0.16f);
        if (id.StartsWith("RU-")) return new Color(0.86f, 0.83f, 0.72f);
        if (id.StartsWith("NL-")) return new Color(0.86f, 0.42f, 0.12f);
        if (id.StartsWith("HA-")) return new Color(0.78f, 0.66f, 0.28f);
        if (id.StartsWith("ME-")) return new Color(0.46f, 0.58f, 0.32f);
        return new Color(0.55f, 0.47f, 0.36f);
    }

    private static Color GetDeviceColor(string id)
    {
        if (id.StartsWith("PR-")) return new Color(0.08f, 0.08f, 0.09f);
        if (id.StartsWith("DK-")) return new Color(0.95f, 0.95f, 0.92f);
        return new Color(0.95f, 0.95f, 0.90f);
    }

    private static Material CreateMaterial(Color color, string name)
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        return new Material(shader) { name = name, color = color };
    }
}
