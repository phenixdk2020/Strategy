using UnityEngine;

public sealed class GrandCampaignCityMarker : MonoBehaviour
{
    public string CityId { get; private set; }

    public void Initialize(string cityId)
    {
        CityId = cityId;
    }
}
