using System.Collections.Generic;
using UnityEngine;

public sealed class PlayerCommander : MonoBehaviour
{
    private readonly List<Regiment> selected = new List<Regiment>();
    private Camera cam;

    private void Start()
    {
        cam = Camera.main;
    }

    private void Update()
    {
        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null)
                return;
        }

        bool pointerOverSimulationControls = BattleManager.Instance != null &&
                                             BattleManager.Instance.IsPointerOverSimulationControls(Input.mousePosition);

        if (!pointerOverSimulationControls && Input.GetMouseButtonDown(0))
            HandleSelection();

        if (!pointerOverSimulationControls && Input.GetMouseButtonDown(1))
            HandleOrder();

        if (Input.GetKeyDown(KeyCode.H))
        {
            ForEachSelected(regiment =>
            {
                OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
                if (controller != null && controller.AIEnabled)
                    controller.SetHoldMission();
                else
                    regiment.OrderHold();
            });
        }

        if (Input.GetKeyDown(KeyCode.F))
            ForEachSelected(r => r.SetFormation(RegimentFormation.Line));

        if (Input.GetKeyDown(KeyCode.C))
            ForEachSelected(r => r.SetFormation(RegimentFormation.Column));

        if (Input.GetKeyDown(KeyCode.T))
        {
            ForEachSelected(r =>
            {
                r.ShowRange = !r.ShowRange;
                r.RefreshRangeVisibility();
            });
        }
    }

    private void HandleSelection()
    {
        bool additive = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 700f))
        {
            Regiment regiment = hit.collider.GetComponentInParent<Regiment>();
            if (regiment != null && regiment.Team == BattleTeam.Denmark)
            {
                if (!additive)
                    ClearSelection();

                if (selected.Contains(regiment) && additive)
                {
                    selected.Remove(regiment);
                    regiment.SetSelected(false);
                }
                else if (!selected.Contains(regiment))
                {
                    selected.Add(regiment);
                    regiment.SetSelected(true);
                }

                return;
            }
        }

        if (!additive)
            ClearSelection();
    }

    private void HandleOrder()
    {
        if (selected.Count == 0)
            return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 700f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            Regiment target = hit.collider.GetComponentInParent<Regiment>();
            if (target != null && target.Team == BattleTeam.Prussia)
            {
                ForEachSelected(regiment =>
                {
                    OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
                    if (controller != null && controller.AIEnabled)
                        controller.SetAttackMission(target);
                    else
                        regiment.OrderAttack(target);
                });

                return;
            }
        }

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.GetComponentInParent<Regiment>() != null)
                continue;

            Vector3 basePoint = hit.point;
            Vector3 right = cam.transform.right;
            right.y = 0f;
            right.Normalize();

            for (int i = 0; i < selected.Count; i++)
            {
                Regiment regiment = selected[i];
                if (regiment == null)
                    continue;

                float offset = (i - (selected.Count - 1) * 0.5f) * 8f;
                Vector3 point = basePoint + right * offset;

                OfficerAIController controller = regiment.GetComponent<OfficerAIController>();
                if (controller != null && controller.AIEnabled)
                    controller.SetMoveMission(point);
                else
                    regiment.OrderMove(point);
            }

            return;
        }
    }

    private void ForEachSelected(System.Action<Regiment> action)
    {
        for (int i = selected.Count - 1; i >= 0; i--)
        {
            if (selected[i] == null)
            {
                selected.RemoveAt(i);
                continue;
            }

            action(selected[i]);
        }
    }

    private void ClearSelection()
    {
        foreach (Regiment regiment in selected)
        {
            if (regiment != null)
                regiment.SetSelected(false);
        }

        selected.Clear();
    }
}
