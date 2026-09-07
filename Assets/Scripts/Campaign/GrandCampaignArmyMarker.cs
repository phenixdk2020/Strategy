using UnityEngine;

public sealed class GrandCampaignArmyMarker : MonoBehaviour
{
    public string ArmyId { get; private set; }

    public void Initialize(string armyId)
    {
        ArmyId = armyId;
    }
}
