using UnityEngine;

public enum PrototypeOfficerMission09K
{
    None,
    DefendArea,
    AttackCaptureArea
}

public enum PrototypeSubunitCommitment09K
{
    Engaged,
    Support,
    Reserve
}

// v00.00.09k command scaffold. This component stores the high-level officer mission
// that future battalion/company allocation AI will execute. 09k does NOT yet move
// subordinate battalions independently; it establishes explicit objective/reserve state
// instead of hiding future command logic inside ad-hoc movement code.
public sealed class PrototypeOfficerObjective09K : MonoBehaviour
{
    public PrototypeOfficerMission09K Mission { get; private set; }
    public Vector3 ObjectiveCenter { get; private set; }
    public float ObjectiveRadius { get; private set; } = 24f;
    public float RequestedReserveFraction { get; private set; } = 0.25f;

    public void SetDefendArea(Vector3 center, float radius, float reserveFraction)
    {
        Mission = PrototypeOfficerMission09K.DefendArea;
        ObjectiveCenter = center;
        ObjectiveRadius = Mathf.Clamp(radius, 8f, 120f);
        RequestedReserveFraction = Mathf.Clamp01(reserveFraction);
        LogMission();
    }

    public void SetAttackCaptureArea(Vector3 center, float radius, float reserveFraction)
    {
        Mission = PrototypeOfficerMission09K.AttackCaptureArea;
        ObjectiveCenter = center;
        ObjectiveRadius = Mathf.Clamp(radius, 8f, 120f);
        RequestedReserveFraction = Mathf.Clamp01(reserveFraction);
        LogMission();
    }

    public void ClearMission()
    {
        Mission = PrototypeOfficerMission09K.None;
    }

    private void LogMission()
    {
        Regiment regiment = GetComponent<Regiment>();
        Debug.Log(
            "COMMAND-09K|Unit=" + (regiment != null ? regiment.RegimentName : name) +
            "|Mission=" + Mission +
            "|Objective=(" + ObjectiveCenter.x.ToString("0.0") + "," + ObjectiveCenter.z.ToString("0.0") + ")" +
            "|Radius=" + ObjectiveRadius.ToString("0.0") +
            "|ReserveRequested=" + (RequestedReserveFraction * 100f).ToString("0") + "%" +
            "|AllocationAI=FutureGate");
    }
}
