using UnityEngine;

/// <summary>
/// Campaign3 v00.00.10n7e - visual-only city offsets.
///
/// IMPORTANT:
/// These offsets do NOT change CITY-REG-01 WGS84 coordinates, click anchors,
/// ZoneId, save identity or simulation geography. They only nudge the rendered
/// city artwork/label so coastal settlement icons read correctly on the campaign map.
/// </summary>
public static class CampaignCityVisualOffsetsV010N7E
{
    public static Vector3 Get(string cityId)
    {
        switch (cityId)
        {
            // Move artwork inland from the east coast.
            case "SAEBY":
                return new Vector3(-0.18f, 0f, -0.02f);

            // Move artwork inland from Øresund and slightly south from the narrow tip.
            case "HELSINGOR":
                return new Vector3(-0.22f, 0f, -0.08f);

            // Move artwork slightly south from the Isefjord/kattegat-facing shoreline.
            case "NYKOBING_SJ":
                return new Vector3(0.00f, 0f, -0.13f);

            default:
                return Vector3.zero;
        }
    }

    public static bool HasOffset(string cityId)
    {
        return cityId == "SAEBY" ||
               cityId == "HELSINGOR" ||
               cityId == "NYKOBING_SJ";
    }
}
