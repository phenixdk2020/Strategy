using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f22
// PlayerCommander still uses a legacy 1000 m click ray while the expanded battlefield
// and Major selection use 5000 m. This late selection layer gives company clicks the
// same world reach and guarantees a company click can take selection authority from HQ.
[DefaultExecutionOrder(710)]
public sealed class PrototypeSelectionRange09F22 : MonoBehaviour
{
    private Camera cam;
    private FieldInfo selectedField;
    private List<Regiment> selected;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeSelectionRange09F22>() == null)
            new GameObject("PrototypeSelectionRange_v000009f22").AddComponent<PrototypeSelectionRange09F22>();
    }

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0))
            return;

        if (BattleManager.Instance != null && BattleManager.Instance.IsPointerOverSimulationControls(Input.mousePosition))
            return;

        if (!Resolve())
            return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 5000f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        Regiment clicked = null;
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
                continue;
            Regiment regiment = hit.collider.GetComponentInParent<Regiment>();
            if (regiment != null && regiment.Team == BattleTeam.Denmark)
            {
                clicked = regiment;
                break;
            }
        }

        if (clicked == null)
            return;

        bool additive =
            Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ||
            Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        PrototypeMajorBattalion09F18 major = PrototypeMajorBattalion09F18.Instance;
        if (major != null && major.Selected)
            major.SetSelected(false);

        if (!additive)
        {
            foreach (Regiment regiment in selected)
                if (regiment != null)
                    regiment.SetSelected(false);
            selected.Clear();
        }

        if (additive && selected.Contains(clicked))
        {
            selected.Remove(clicked);
            clicked.SetSelected(false);
        }
        else if (!selected.Contains(clicked))
        {
            selected.Add(clicked);
            clicked.SetSelected(true);
        }

        Debug.Log("SELECTION-09F22|Unit=" + clicked.RegimentName + "|Ray=5000m|MajorReleased=True");
    }

    private bool Resolve()
    {
        if (cam == null)
            cam = Camera.main;
        if (cam == null || PlayerCommander.Instance == null)
            return false;

        if (selectedField == null)
            selectedField = typeof(PlayerCommander).GetField("selected", BindingFlags.Instance | BindingFlags.NonPublic);
        if (selectedField == null)
            return false;

        selected = selectedField.GetValue(PlayerCommander.Instance) as List<Regiment>;
        return selected != null;
    }
}
