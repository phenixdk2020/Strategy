using System.Collections.Generic;
using UnityEngine;

public sealed class PrototypeCasualtyVisualManager : MonoBehaviour
{
    private readonly Dictionary<Regiment, int> createdVisuals = new Dictionary<Regiment, int>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeCasualtyVisualManager>() != null)
            return;

        GameObject managerObject = new GameObject("PrototypeCasualtyVisualManager");
        managerObject.AddComponent<PrototypeCasualtyVisualManager>();
    }

    private void Update()
    {
        BattleManager battleManager = BattleManager.Instance;
        if (battleManager == null)
            return;

        int casualtiesPerBody = PrototypeCombatTuningManager.GetCasualtiesPerBody();

        foreach (Regiment regiment in battleManager.Regiments)
        {
            if (regiment == null)
                continue;

            int losses = Mathf.Max(0, regiment.InitialStrength - regiment.CurrentStrength);
            int desiredVisuals = losses <= 0
                ? 0
                : Mathf.Clamp(Mathf.CeilToInt(losses / (float)casualtiesPerBody), 1, 100);

            if (!createdVisuals.TryGetValue(regiment, out int currentVisuals))
                currentVisuals = 0;

            while (currentVisuals < desiredVisuals)
            {
                CreateCasualtyVisual(regiment, currentVisuals);
                currentVisuals++;
            }

            createdVisuals[regiment] = currentVisuals;
        }
    }

    private void CreateCasualtyVisual(Regiment regiment, int visualIndex)
    {
        // Bodies are created at the formation's current battlefield position at the
        // moment the loss threshold is crossed. They are parented to this manager,
        // not to the regiment, so they remain where men fell when the regiment moves.
        // This also makes losses accumulate visibly while two stationary lines exchange fire.
        Vector3 localOffset = new Vector3(
            Random.Range(-9.2f, 9.2f),
            0f,
            Random.Range(-4.0f, 2.8f));

        Vector3 position = regiment.transform.TransformPoint(localOffset);
        position.y = PrototypeBootstrap.SampleGroundHeight(position.x, position.z) + 0.22f;

        GameObject casualty = new GameObject(regiment.RegimentName + "_PrototypeCasualty_" + (visualIndex + 1));
        casualty.transform.SetParent(transform, true);
        casualty.transform.position = position;
        casualty.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        Material uniformMaterial = CreateMaterial(
            regiment.Team == BattleTeam.Denmark
                ? new Color(0.10f, 0.20f, 0.34f)
                : new Color(0.12f, 0.12f, 0.14f));
        Material equipmentMaterial = CreateMaterial(new Color(0.055f, 0.045f, 0.035f));

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(casualty.transform, false);
        body.transform.localScale = new Vector3(0.33f, 0.54f, 0.33f);
        body.transform.localPosition = new Vector3(0f, 0.31f, 0f);
        body.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        body.GetComponent<Renderer>().sharedMaterial = uniformMaterial;
        Destroy(body.GetComponent<Collider>());

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(casualty.transform, false);
        head.transform.localScale = Vector3.one * 0.30f;
        head.transform.localPosition = new Vector3(0.68f, 0.25f, 0f);
        head.GetComponent<Renderer>().sharedMaterial = uniformMaterial;
        Destroy(head.GetComponent<Collider>());

        GameObject rifle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rifle.name = "Rifle";
        rifle.transform.SetParent(casualty.transform, false);
        rifle.transform.localScale = new Vector3(0.07f, 0.07f, 0.96f);
        rifle.transform.localPosition = new Vector3(-0.05f, 0.18f, 0.40f);
        rifle.transform.localRotation = Quaternion.Euler(0f, Random.Range(12f, 42f), 0f);
        rifle.GetComponent<Renderer>().sharedMaterial = equipmentMaterial;
        Destroy(rifle.GetComponent<Collider>());
    }

    private static Material CreateMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        Material material = new Material(shader);
        material.color = color;
        return material;
    }
}
