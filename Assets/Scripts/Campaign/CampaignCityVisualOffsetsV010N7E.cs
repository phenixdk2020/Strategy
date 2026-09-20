using UnityEngine;

/// <summary>
/// Visual-only city offsets introduced in n7e and expanded in n7g.
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
            // Existing pass 1 correction.
            case "SAEBY":
                return new Vector3(-0.18f, 0f, -0.02f);

            // Pass 2: move farther inland from Øresund.
            case "HELSINGOR":
                return new Vector3(-0.34f, 0f, -0.12f);

            // Pass 2: move farther south from the north-coast shoreline.
            case "NYKOBING_SJ":
                return new Vector3(0.00f, 0f, -0.24f);

            // North-west Fyn: move east/south onto land.
            case "BOGENSE":
                return new Vector3(0.14f, 0f, -0.12f);

            // West Fyn: move east from the coast.
            case "ASSENS":
                return new Vector3(0.18f, 0f, 0.00f);

            // Møn: move east/inland from Stege Nor/coast.
            case "STEGE":
                return new Vector3(0.12f, 0f, 0.02f);

            // Præstø Fjord: move north-west/inland.
            case "PRAESTO":
                return new Vector3(-0.10f, 0f, 0.10f);

            // South-east Mors: move north-west/inland.
            case "NYKOBING_MORS":
                return new Vector3(-0.12f, 0f, 0.08f);

            default:
                return Vector3.zero;
        }
    }

    public static bool HasOffset(string cityId)
    {
        switch (cityId)
        {
            case "SAEBY":
            case "HELSINGOR":
            case "NYKOBING_SJ":
            case "BOGENSE":
            case "ASSENS":
            case "STEGE":
            case "PRAESTO":
            case "NYKOBING_MORS":
                return true;
            default:
                return false;
        }
    }
}
