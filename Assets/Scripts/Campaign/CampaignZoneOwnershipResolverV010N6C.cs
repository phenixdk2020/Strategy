using UnityEngine;

/// <summary>
/// Campaign3 v00.00.10n6c deterministic zone ownership resolver.
///
/// The visible prototype overlay is generated as a multi-site Voronoi partition
/// from the 20 zone centres plus CITY-REG-01 cities. This resolver uses the same
/// authoritative influence sites directly, so a map click cannot be overridden by
/// an unrelated zone-centre collider.
///
/// This is still prototype administrative geometry. Exact 1851 Amt borders remain
/// targeted at source-backed DigDag polygons.
/// </summary>
public static class CampaignZoneOwnershipResolverV010N6C
{
    public const string ResolverMode = "CANONICAL_CITY_PLUS_ZONE_CENTRE_NEAREST_SITE";

    public static bool TryResolveWorld(Vector3 worldPoint, out string zoneId)
    {
        Vector2 geo = CampaignGeoProjection.Unproject(worldPoint);
        return TryResolveGeo(geo, out zoneId);
    }

    public static bool TryResolveGeo(Vector2 geoPoint, out string zoneId)
    {
        zoneId = null;
        float bestDistance = float.MaxValue;

        CampaignDenmark1851Registry.ZoneDef[] zones = CampaignDenmark1851Registry.Zones;
        for (int i = 0; i < zones.Length; i++)
        {
            CampaignDenmark1851Registry.ZoneDef zone = zones[i];
            Vector2 site = new Vector2(zone.Longitude, zone.Latitude);
            float distance = (site - geoPoint).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                zoneId = zone.Id;
            }
        }

        CampaignDenmark1851Registry.CityDef[] cities = CampaignDenmark1851Registry.Cities;
        for (int i = 0; i < cities.Length; i++)
        {
            CampaignDenmark1851Registry.CityDef city = cities[i];
            Vector2 site = new Vector2(city.Longitude, city.Latitude);
            float distance = (site - geoPoint).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                zoneId = city.ZoneId;
            }
        }

        return !string.IsNullOrEmpty(zoneId);
    }

    public static void ValidateCanonicalSites(out int cityCorrect, out int zoneCentreCorrect)
    {
        cityCorrect = 0;
        zoneCentreCorrect = 0;

        CampaignDenmark1851Registry.CityDef[] cities = CampaignDenmark1851Registry.Cities;
        for (int i = 0; i < cities.Length; i++)
        {
            CampaignDenmark1851Registry.CityDef city = cities[i];
            if (TryResolveGeo(new Vector2(city.Longitude, city.Latitude), out string owner) && owner == city.ZoneId)
                cityCorrect++;
        }

        CampaignDenmark1851Registry.ZoneDef[] zones = CampaignDenmark1851Registry.Zones;
        for (int i = 0; i < zones.Length; i++)
        {
            CampaignDenmark1851Registry.ZoneDef zone = zones[i];
            if (TryResolveGeo(new Vector2(zone.Longitude, zone.Latitude), out string owner) && owner == zone.Id)
                zoneCentreCorrect++;
        }
    }
}
