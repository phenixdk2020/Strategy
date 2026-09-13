using System;

public static class CampaignDenmark1851Registry
{
    public enum CityTier
    {
        A,
        B,
        C
    }

    public sealed class ZoneDef
    {
        public string Id { get; }
        public string Name { get; }
        public float Longitude { get; }
        public float Latitude { get; }
        public string[] LandNeighbours { get; }
        public string[] FerryNeighbours { get; }

        public ZoneDef(
            string id,
            string name,
            float longitude,
            float latitude,
            string[] landNeighbours,
            string[] ferryNeighbours)
        {
            Id = id;
            Name = name;
            Longitude = longitude;
            Latitude = latitude;
            LandNeighbours = landNeighbours ?? Array.Empty<string>();
            FerryNeighbours = ferryNeighbours ?? Array.Empty<string>();
        }
    }

    public sealed class CityDef
    {
        public string Id { get; }
        public string Name { get; }
        public string ZoneId { get; }
        public int Population1850 { get; }
        public CityTier Tier { get; }
        public float Longitude { get; }
        public float Latitude { get; }

        public CityDef(
            string id,
            string name,
            string zoneId,
            int population1850,
            CityTier tier,
            float longitude,
            float latitude)
        {
            Id = id;
            Name = name;
            ZoneId = zoneId;
            Population1850 = population1850;
            Tier = tier;
            Longitude = longitude;
            Latitude = latitude;
        }

        public bool AllowsHeavyMilitaryConstruction => Tier == CityTier.A;
    }

    public static readonly ZoneDef[] Zones =
    {
        new ZoneDef("DK-Z01-KBH", "København Stad", 12.5683f, 55.6761f, new[] { "DK-Z02-KBH-AMT" }, Array.Empty<string>()),
        new ZoneDef("DK-Z02-KBH-AMT", "Københavns Amt / Roskilde Amtsrådskreds", 12.0800f, 55.6410f, new[] { "DK-Z01-KBH", "DK-Z03-FRB", "DK-Z04-HOL", "DK-Z05-SOR", "DK-Z06-PRA" }, Array.Empty<string>()),
        new ZoneDef("DK-Z03-FRB", "Frederiksborg Amt", 12.3100f, 55.9270f, new[] { "DK-Z02-KBH-AMT", "DK-Z04-HOL" }, Array.Empty<string>()),
        new ZoneDef("DK-Z04-HOL", "Holbæk Amt", 11.7170f, 55.7160f, new[] { "DK-Z02-KBH-AMT", "DK-Z03-FRB", "DK-Z05-SOR" }, Array.Empty<string>()),
        new ZoneDef("DK-Z05-SOR", "Sorø Amt", 11.5550f, 55.4310f, new[] { "DK-Z02-KBH-AMT", "DK-Z04-HOL", "DK-Z06-PRA" }, new[] { "DK-Z09-ODE" }),
        new ZoneDef("DK-Z06-PRA", "Præstø Amt", 11.9700f, 55.1800f, new[] { "DK-Z02-KBH-AMT", "DK-Z05-SOR" }, new[] { "DK-Z07-MAR" }),
        new ZoneDef("DK-Z07-MAR", "Maribo Amt", 11.5500f, 54.7700f, Array.Empty<string>(), new[] { "DK-Z06-PRA" }),
        new ZoneDef("DK-Z08-BOR", "Bornholms Amt", 14.9100f, 55.1200f, Array.Empty<string>(), Array.Empty<string>()),
        new ZoneDef("DK-Z09-ODE", "Odense Amt", 10.2000f, 55.4500f, new[] { "DK-Z10-SVE" }, new[] { "DK-Z05-SOR", "DK-Z11-VEJ" }),
        new ZoneDef("DK-Z10-SVE", "Svendborg Amt", 10.5500f, 55.1500f, new[] { "DK-Z09-ODE" }, Array.Empty<string>()),
        new ZoneDef("DK-Z11-VEJ", "Vejle Amt", 9.5500f, 55.6000f, new[] { "DK-Z12-SKA", "DK-Z19-RIN", "DK-Z20-RIB" }, new[] { "DK-Z09-ODE" }),
        new ZoneDef("DK-Z12-SKA", "Skanderborg Amt", 9.8500f, 55.9500f, new[] { "DK-Z11-VEJ", "DK-Z13-AAR", "DK-Z14-RAN", "DK-Z15-VIB", "DK-Z19-RIN" }, Array.Empty<string>()),
        new ZoneDef("DK-Z13-AAR", "Aarhus Amt", 10.1500f, 56.2000f, new[] { "DK-Z12-SKA", "DK-Z14-RAN" }, Array.Empty<string>()),
        new ZoneDef("DK-Z14-RAN", "Randers Amt", 10.1000f, 56.4500f, new[] { "DK-Z12-SKA", "DK-Z13-AAR", "DK-Z15-VIB", "DK-Z16-AAL" }, Array.Empty<string>()),
        new ZoneDef("DK-Z15-VIB", "Viborg Amt", 9.2000f, 56.4500f, new[] { "DK-Z12-SKA", "DK-Z14-RAN", "DK-Z16-AAL", "DK-Z19-RIN" }, new[] { "DK-Z18-THI" }),
        new ZoneDef("DK-Z16-AAL", "Aalborg Amt", 9.7500f, 57.0300f, new[] { "DK-Z14-RAN", "DK-Z15-VIB" }, new[] { "DK-Z17-HJO" }),
        new ZoneDef("DK-Z17-HJO", "Hjørring Amt", 10.1000f, 57.4500f, Array.Empty<string>(), new[] { "DK-Z16-AAL" }),
        new ZoneDef("DK-Z18-THI", "Thisted Amt", 8.8000f, 56.9000f, new[] { "DK-Z19-RIN" }, new[] { "DK-Z15-VIB" }),
        new ZoneDef("DK-Z19-RIN", "Ringkøbing Amt", 8.5500f, 56.2000f, new[] { "DK-Z11-VEJ", "DK-Z12-SKA", "DK-Z15-VIB", "DK-Z18-THI", "DK-Z20-RIB" }, Array.Empty<string>()),
        new ZoneDef("DK-Z20-RIB", "Ribe Amt", 8.7000f, 55.4500f, new[] { "DK-Z11-VEJ", "DK-Z19-RIN" }, Array.Empty<string>()),
    };

    public static readonly CityDef[] Cities =
    {
        new CityDef("COPENHAGEN", "København", "DK-Z01-KBH", 129695, CityTier.A, 12.5683f, 55.6761f),
        new CityDef("HELSINGOR", "Helsingør", "DK-Z03-FRB", 8111, CityTier.A, 12.5920f, 56.0360f),
        new CityDef("HILLEROD", "Hillerød", "DK-Z03-FRB", 1929, CityTier.C, 12.3100f, 55.9270f),
        new CityDef("FREDERIKSSUND", "Frederikssund", "DK-Z03-FRB", 612, CityTier.C, 12.0680f, 55.8400f),
        new CityDef("ROSKILDE", "Roskilde", "DK-Z02-KBH-AMT", 3805, CityTier.B, 12.0800f, 55.6410f),
        new CityDef("KOGE", "Køge", "DK-Z02-KBH-AMT", 2436, CityTier.B, 12.1820f, 55.4590f),
        new CityDef("HOLBAEK", "Holbæk", "DK-Z04-HOL", 2638, CityTier.B, 11.7170f, 55.7160f),
        new CityDef("NYKOBING_SJ", "Nykøbing Sjælland", "DK-Z04-HOL", 1282, CityTier.C, 11.6720f, 55.9240f),
        new CityDef("KALUNDBORG", "Kalundborg", "DK-Z04-HOL", 2490, CityTier.B, 11.0890f, 55.6790f),
        new CityDef("RINGSTED", "Ringsted", "DK-Z05-SOR", 1380, CityTier.C, 11.7900f, 55.4430f),
        new CityDef("SORO", "Sorø", "DK-Z05-SOR", 901, CityTier.C, 11.5550f, 55.4310f),
        new CityDef("SLAGELSE", "Slagelse", "DK-Z05-SOR", 4011, CityTier.A, 11.3540f, 55.4020f),
        new CityDef("KORSOR", "Korsør", "DK-Z05-SOR", 1819, CityTier.C, 11.1350f, 55.3290f),
        new CityDef("SKAELSKOR", "Skælskør", "DK-Z05-SOR", 1134, CityTier.C, 11.2900f, 55.2530f),
        new CityDef("NAESTVED", "Næstved", "DK-Z06-PRA", 2735, CityTier.B, 11.7609f, 55.2299f),
        new CityDef("STORE_HEDDINGE", "Store Heddinge", "DK-Z06-PRA", 1076, CityTier.C, 12.4060f, 55.3090f),
        new CityDef("PRAESTO", "Præstø", "DK-Z06-PRA", 951, CityTier.C, 12.0450f, 55.1230f),
        new CityDef("VORDINGBORG", "Vordingborg", "DK-Z06-PRA", 1579, CityTier.C, 11.9100f, 55.0080f),
        new CityDef("STEGE", "Stege", "DK-Z06-PRA", 1808, CityTier.C, 12.2850f, 54.9870f),
        new CityDef("RONNE", "Rønne", "DK-Z08-BOR", 4717, CityTier.A, 14.7066f, 55.1009f),
        new CityDef("HASLE", "Hasle", "DK-Z08-BOR", 853, CityTier.C, 14.7060f, 55.1840f),
        new CityDef("ALLINGE", "Allinge", "DK-Z08-BOR", 609, CityTier.C, 14.8020f, 55.2770f),
        new CityDef("SANDVIG", "Sandvig", "DK-Z08-BOR", 298, CityTier.C, 14.7810f, 55.2880f),
        new CityDef("SVANEKE", "Svaneke", "DK-Z08-BOR", 1009, CityTier.C, 15.1390f, 55.1350f),
        new CityDef("NEKSO", "Neksø", "DK-Z08-BOR", 1403, CityTier.C, 15.1300f, 55.0600f),
        new CityDef("AAKIRKEBY", "Åkirkeby", "DK-Z08-BOR", 561, CityTier.C, 14.9190f, 55.0700f),
        new CityDef("STUBBEKOBING", "Stubbekøbing", "DK-Z07-MAR", 1081, CityTier.C, 11.9710f, 54.8880f),
        new CityDef("NYKOBING_F", "Nykøbing Falster", "DK-Z07-MAR", 2123, CityTier.B, 11.8740f, 54.7700f),
        new CityDef("NYSTED", "Nysted", "DK-Z07-MAR", 1082, CityTier.C, 11.7270f, 54.6660f),
        new CityDef("SAKSKOBING", "Sakskøbing", "DK-Z07-MAR", 917, CityTier.C, 11.6330f, 54.8000f),
        new CityDef("MARIBO", "Maribo", "DK-Z07-MAR", 1667, CityTier.C, 11.5000f, 54.7750f),
        new CityDef("RODBY", "Rødby", "DK-Z07-MAR", 1339, CityTier.C, 11.3890f, 54.6940f),
        new CityDef("NAKSKOV", "Nakskov", "DK-Z07-MAR", 2955, CityTier.B, 11.1450f, 54.8310f),
        new CityDef("ODENSE", "Odense", "DK-Z09-ODE", 11122, CityTier.A, 10.4024f, 55.4038f),
        new CityDef("SVENDBORG", "Svendborg", "DK-Z10-SVE", 4556, CityTier.A, 10.6070f, 55.0590f),
        new CityDef("NYBORG", "Nyborg", "DK-Z10-SVE", 3059, CityTier.B, 10.7900f, 55.3120f),
        new CityDef("ASSENS", "Assens", "DK-Z09-ODE", 2963, CityTier.B, 9.9000f, 55.2700f),
        new CityDef("FAABORG", "Fåborg", "DK-Z10-SVE", 2328, CityTier.B, 10.2430f, 55.0950f),
        new CityDef("KERTEMINDE", "Kerteminde", "DK-Z09-ODE", 1833, CityTier.C, 10.6570f, 55.4490f),
        new CityDef("MIDDELFART", "Middelfart", "DK-Z09-ODE", 1633, CityTier.C, 9.7300f, 55.5050f),
        new CityDef("BOGENSE", "Bogense", "DK-Z09-ODE", 1497, CityTier.C, 10.0900f, 55.5670f),
        new CityDef("RUDKOBING", "Rudkøbing", "DK-Z10-SVE", 2333, CityTier.B, 10.7110f, 54.9370f),
        new CityDef("KOLDING", "Kolding", "DK-Z11-VEJ", 2865, CityTier.B, 9.4722f, 55.4904f),
        new CityDef("FREDERICIA", "Fredericia", "DK-Z11-VEJ", 4326, CityTier.A, 9.7526f, 55.5657f),
        new CityDef("VEJLE", "Vejle", "DK-Z11-VEJ", 3300, CityTier.B, 9.5360f, 55.7110f),
        new CityDef("HORSENS", "Horsens", "DK-Z12-SKA", 5827, CityTier.A, 9.8500f, 55.8600f),
        new CityDef("SKANDERBORG", "Skanderborg", "DK-Z12-SKA", 1042, CityTier.C, 9.9270f, 56.0390f),
        new CityDef("AARHUS", "Aarhus", "DK-Z13-AAR", 7886, CityTier.A, 10.2039f, 56.1629f),
        new CityDef("EBELTOFT", "Ebeltoft", "DK-Z14-RAN", 1112, CityTier.C, 10.6830f, 56.1940f),
        new CityDef("GRENAA", "Grenaa", "DK-Z14-RAN", 1099, CityTier.C, 10.8780f, 56.4150f),
        new CityDef("RANDERS", "Randers", "DK-Z14-RAN", 7338, CityTier.A, 10.0360f, 56.4600f),
        new CityDef("MARIAGER", "Mariager", "DK-Z14-RAN", 546, CityTier.C, 9.9750f, 56.6490f),
        new CityDef("HOBRO", "Hobro", "DK-Z14-RAN", 1173, CityTier.C, 9.7900f, 56.6430f),
        new CityDef("NIBE", "Nibe", "DK-Z16-AAL", 1161, CityTier.C, 9.6390f, 56.9810f),
        new CityDef("AALBORG", "Aalborg", "DK-Z16-AAL", 7745, CityTier.A, 9.9217f, 57.0488f),
        new CityDef("SAEBY", "Sæby", "DK-Z17-HJO", 895, CityTier.C, 10.5330f, 57.3330f),
        new CityDef("FREDERIKSHAVN", "Frederikshavn", "DK-Z17-HJO", 1374, CityTier.C, 10.5360f, 57.4410f),
        new CityDef("SKAGEN", "Skagen", "DK-Z17-HJO", 1400, CityTier.C, 10.5830f, 57.7200f),
        new CityDef("HJORRING", "Hjørring", "DK-Z17-HJO", 1914, CityTier.C, 9.9820f, 57.4640f),
        new CityDef("THISTED", "Thisted", "DK-Z18-THI", 2343, CityTier.B, 8.6940f, 56.9550f),
        new CityDef("NYKOBING_MORS", "Nykøbing Mors", "DK-Z18-THI", 1398, CityTier.C, 8.8520f, 56.7940f),
        new CityDef("SKIVE", "Skive", "DK-Z15-VIB", 1256, CityTier.C, 9.0270f, 56.5670f),
        new CityDef("VIBORG", "Viborg", "DK-Z15-VIB", 4039, CityTier.A, 9.4020f, 56.4532f),
        new CityDef("LEMVIG", "Lemvig", "DK-Z19-RIN", 859, CityTier.C, 8.3100f, 56.5480f),
        new CityDef("HOLSTEBRO", "Holstebro", "DK-Z19-RIN", 1305, CityTier.C, 8.6170f, 56.3600f),
        new CityDef("RINGKOBING", "Ringkøbing", "DK-Z19-RIN", 1274, CityTier.C, 8.2440f, 56.0900f),
        new CityDef("VARDE", "Varde", "DK-Z20-RIB", 1774, CityTier.C, 8.4800f, 55.6210f),
        new CityDef("RIBE", "Ribe", "DK-Z20-RIB", 2984, CityTier.B, 8.7610f, 55.3300f),
    };

    public static bool Validate(out string error)
    {
        error = string.Empty;

        if (Zones.Length != 20)
        {
            error = "Expected 20 zones, got " + Zones.Length;
            return false;
        }

        if (Cities.Length != 68)
        {
            error = "Expected 68 cities, got " + Cities.Length;
            return false;
        }

        int population = 0;
        for (int i = 0; i < Cities.Length; i++)
        {
            CityDef city = Cities[i];
            population += city.Population1850;

            bool zoneFound = false;
            for (int z = 0; z < Zones.Length; z++)
            {
                if (Zones[z].Id == city.ZoneId)
                {
                    zoneFound = true;
                    break;
                }
            }

            if (!zoneFound)
            {
                error = "City " + city.Name + " references unknown zone " + city.ZoneId;
                return false;
            }
        }

        if (population != 290565)
        {
            error = "Expected urban population checksum 290565, got " + population;
            return false;
        }

        return true;
    }
}
