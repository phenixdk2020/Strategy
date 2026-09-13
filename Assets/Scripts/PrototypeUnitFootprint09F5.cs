using System.Collections.Generic;
using UnityEngine;

// v00.00.09f5: collider, selected footprint and fire fans follow the actual company formation.
[DefaultExecutionOrder(26000)]
public sealed class PrototypeUnitFootprint09F5 : MonoBehaviour
{
    private readonly Dictionary<Regiment, LineRenderer> outlines =
        new Dictionary<Regiment, LineRenderer>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeUnitFootprint09F5>() != null)
            return;

        GameObject root = new GameObject("PrototypeUnitFootprint_v000009f5");
        root.AddComponent<PrototypeUnitFootprint09F5>();
    }

    private void LateUpdate()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null)
                continue;

            Vector2 footprint = PrototypeBattleVisuals09F5.GetFootprint(regiment);
            UpdateCollider(regiment, footprint);
            HideLegacySelectionDisc(regiment);
            UpdateSelectionOutline(regiment, footprint);
            UpdateRangeFans(regiment, footprint);
        }
    }

    private static void UpdateCollider(Regiment regiment, Vector2 footprint)
    {
        BoxCollider box = regiment.GetComponent<BoxCollider>();
        if (box == null)
            box = regiment.gameObject.AddComponent<BoxCollider>();

        box.center = new Vector3(0f, 1.0f, 0f);
        box.size = new Vector3(
            Mathf.Max(1.2f, footprint.x + 1.0f),
            2.4f,
            Mathf.Max(1.2f, footprint.y + 1.0f));
    }

    private static void HideLegacySelectionDisc(Regiment regiment)
    {
        Renderer[] renderers = regiment.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || renderer.sharedMaterial == null)
                continue;

            string materialName = renderer.sharedMaterial.name;
            if (!string.IsNullOrEmpty(materialName) && materialName.Contains("Selection"))
                renderer.enabled = false;
        }
    }

    private void UpdateSelectionOutline(Regiment regiment, Vector2 footprint)
    {
        if (!outlines.TryGetValue(regiment, out LineRenderer line) || line == null)
        {
            GameObject go = new GameObject("SelectionFootprint09F5");
            go.transform.SetParent(regiment.transform, false);
            go.transform.localPosition = new Vector3(0f, 0.12f, 0f);

            line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 4;
            line.widthMultiplier = 0.12f;
            line.sharedMaterial = PrototypeBootstrap.CreateSharedMaterial(
                new Color(1.0f, 0.82f, 0.10f),
                "SelectionFootprint09F5");
            line.startColor = new Color(1.0f, 0.82f, 0.10f, 0.82f);
            line.endColor = line.startColor;
            outlines[regiment] = line;
        }

        float halfX = footprint.x * 0.5f + 0.55f;
        float halfZ = footprint.y * 0.5f + 0.55f;
        line.SetPosition(0, new Vector3(-halfX, 0f, -halfZ));
        line.SetPosition(1, new Vector3(-halfX, 0f, halfZ));
        line.SetPosition(2, new Vector3(halfX, 0f, halfZ));
        line.SetPosition(3, new Vector3(halfX, 0f, -halfZ));
        line.enabled = regiment.IsSelected;
    }

    private static void UpdateRangeFans(Regiment regiment, Vector2 footprint)
    {
        float muzzleHalfWidth = Mathf.Max(1.0f, footprint.x * 0.5f);
        float muzzleZ = footprint.y * 0.5f + 0.35f;

        RebuildFan(regiment.transform.Find("CloseRangeFan"), regiment.CloseRange, muzzleHalfWidth, muzzleZ);
        RebuildFan(regiment.transform.Find("MediumRangeFan"), regiment.EffectiveRange, muzzleHalfWidth, muzzleZ);
        RebuildFan(regiment.transform.Find("LongRangeFan"), regiment.MaximumRange, muzzleHalfWidth, muzzleZ);
    }

    private static void RebuildFan(Transform fanTransform, float range, float muzzleHalfWidth, float muzzleZ)
    {
        if (fanTransform == null)
            return;

        LineRenderer line = fanTransform.GetComponent<LineRenderer>();
        if (line == null)
            return;

        const int arcSegments = 40;
        line.loop = true;
        line.positionCount = arcSegments + 3;
        line.SetPosition(0, new Vector3(-muzzleHalfWidth, 0.15f, muzzleZ));

        for (int i = 0; i <= arcSegments; i++)
        {
            float t = i / (float)arcSegments;
            float angle = Mathf.Lerp(-regimentHalfArc, regimentHalfArc, t) * Mathf.Deg2Rad;
            float x = Mathf.Sin(angle) * range;
            float z = muzzleZ + Mathf.Cos(angle) * range;
            line.SetPosition(i + 1, new Vector3(x, 0.15f, z));
        }

        line.SetPosition(line.positionCount - 1, new Vector3(muzzleHalfWidth, 0.15f, muzzleZ));
    }

    private const float regimentHalfArc = 60f;
}
