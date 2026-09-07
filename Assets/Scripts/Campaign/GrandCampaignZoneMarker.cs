using UnityEngine;

public sealed class GrandCampaignZoneMarker : MonoBehaviour
{
    public string ZoneId { get; private set; }

    public void Initialize(string zoneId)
    {
        ZoneId = zoneId;
    }
}
