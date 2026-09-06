using System.Collections.Generic;
using UnityEngine;

public sealed class PrototypeCasualtyVisualManager : MonoBehaviour
{
    private readonly Dictionary<Regiment, int> lastObservedStrength = new Dictionary<Regiment, int>();
    private readonly HashSet<Regiment> casualtyVisualCreated = new HashSet<Regiment>();

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

        foreach (Regiment regiment in battleManager.Regiments)
        {
            if (regiment == null)
                continue;

            if (!lastObservedStrength.TryGetValue(regiment, out int previousStrength))
                previousStrength = regiment.InitialStrength;

            if (regiment.CurrentStrength < previousStrength && !casualtyVisualCreated.Contains(regiment))
            {
                CreateCasualtyVisual(regiment);
                casualtyVisualCreated.Add(regiment);
            }

            lastObservedStrength[regiment] = regiment.CurrentStrength;
        }
    }

    private void CreateCasualtyVisual(Regiment regiment)
    {
        Vector3 position = regiment.transform.position;
        position += new Vector3(Random.Range(-2.2f, 2.2f), 0f, Random.Range(-1.8f, 1.8f));
        position.y = PrototypeBootstrap.SampleGroundHeight(position.x, position.z) + 0.20f;

        GameObject casualty = new GameObject(regiment.RegimentName + "_PrototypeCasualty");
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
        body.transform.localScale = new Vector3(0.28f, 0.48f, 0.28f);
        body.transform.localPosition = new Vector3(0f, 0.28f, 0f);
        body.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        body.GetComponent<Renderer>().sharedMaterial = uniformMaterial;
        Destroy(body.GetComponent<Collider>());

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(casualty.transform, false);
        head.transform.localScale = Vector3.one * 0.28f;
        head.transform.localPosition = new Vector3(0.62f, 0.24f, 0f);
        head.GetComponent<Renderer>().sharedMaterial = uniformMaterial;
        Destroy(head.GetComponent<Collider>());

        GameObject rifle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rifle.name = "Rifle";
        rifle.transform.SetParent(casualty.transform, false);
        rifle.transform.localScale = new Vector3(0.07f, 0.07f, 0.92f);
        rifle.transform.localPosition = new Vector3(-0.05f, 0.18f, 0.38f);
        rifle.transform.localRotation = Quaternion.Euler(0f, 28f, 0f);
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
