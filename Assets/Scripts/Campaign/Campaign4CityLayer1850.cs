using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Campaign4 historical city layer for the Denmark 1851 campaign map.
///
/// The city selection is the 40 largest Danish købstæder in the official
/// 1 February 1850 census ranking published by Danmarks Statistik.
/// Population is used for rank and marker hierarchy; coordinates are WGS84
/// town-centre anchors used by the shared campaign projection.
///
/// Source for ranking/population:
/// Statistisk Tabelværk, Ny Række, Bd. 1 (Folkemængden 1850),
/// Danmarks Statistik publication id 19933.
///
/// Important historical guardrail:
/// Esbjerg is intentionally absent. It was not an 1850 købstad and must not
/// appear in the 1851 historical city layer merely because it is large today.
/// </summary>
[DefaultExecutionOrder(-28000)]
public sealed class Campaign4CityLayer1850 : MonoBehaviour
{
    private readonly struct CityRecord
    {
        public readonly int Rank;
        public readonly string Name;
        public readonly int Population1850;
        public readonly float Longitude;
        public readonly float Latitude;

        public CityRecord(int rank, string name, int population1850, float longitude, float latitude)
        {
            Rank = rank;
            Name = name;
            Population1850 = population1850;
            Longitude = longitude;
            Latitude = latitude;
        }
    }

    private sealed class CityVisual
    {
        public CityRecord Data;
        public GameObject Root;
    }

    // Official 1850 size order, limited to the 40 largest towns for Campaign4.
    private static readonly CityRecord[] CityData =
    {
        new CityRecord( 1, "København",        129695, 12.5683f, 55.6761f),
        new CityRecord( 2, "Odense",            11122, 10.3883f, 55.3959f),
        new CityRecord( 3, "Helsingør",           8111, 12.5926f, 56.0361f),
        new CityRecord( 4, "Aarhus",              7886, 10.2039f, 56.1629f),
        new CityRecord( 5, "Aalborg",             7745,  9.9217f, 57.0488f),
        new CityRecord( 6, "Randers",             7338, 10.0364f, 56.4607f),
        new CityRecord( 7, "Horsens",             5827,  9.8503f, 55.8607f),
        new CityRecord( 8, "Rønne",               4717, 14.7066f, 55.1009f),
        new CityRecord( 9, "Svendborg",           4556, 10.6073f, 55.0598f),
        new CityRecord(10, "Fredericia",          4326,  9.7526f, 55.5657f),
        new CityRecord(11, "Viborg",              4039,  9.4020f, 56.4532f),
        new CityRecord(12, "Slagelse",            4011, 11.3546f, 55.4028f),
        new CityRecord(13, "Roskilde",            3805, 12.0803f, 55.6415f),
        new CityRecord(14, "Vejle",               3300,  9.5357f, 55.7113f),
        new CityRecord(15, "Nyborg",              3059, 10.7896f, 55.3127f),
        new CityRecord(16, "Ribe",                2984,  8.7622f, 55.3305f),
        new CityRecord(17, "Assens",              2965,  9.9008f, 55.2702f),
        new CityRecord(18, "Nakskov",             2955, 11.1454f, 54.8304f),
        new CityRecord(19, "Kolding",             2865,  9.4722f, 55.4904f),
        new CityRecord(20, "Næstved",             2735, 11.7609f, 55.2299f),
        new CityRecord(21, "Holbæk",              2638, 11.7167f, 55.7167f),
        new CityRecord(22, "Kalundborg",          2490, 11.0886f, 55.6795f),
        new CityRecord(23, "Køge",                2456, 12.1821f, 55.4580f),
        new CityRecord(24, "Thisted",             2342,  8.6949f, 56.9557f),
        new CityRecord(25, "Rudkøbing",           2333, 10.7101f, 54.9364f),
        new CityRecord(26, "Faaborg",             2328, 10.2423f, 55.0951f),
        new CityRecord(27, "Nykøbing Falster",    2123, 11.8743f, 54.7656f),
        new CityRecord(28, "Hillerød",            1929, 12.3083f, 55.9279f),
        new CityRecord(29, "Hjørring",            1914,  9.9823f, 57.4642f),
        new CityRecord(30, "Kerteminde",          1833, 10.6577f, 55.4490f),
        new CityRecord(31, "Korsør",              1819, 11.1386f, 55.3299f),
        new CityRecord(32, "Stege",               1808, 12.2849f, 54.9870f),
        new CityRecord(33, "Varde",               1774,  8.4807f, 55.6211f),
        new CityRecord(34, "Maribo",              1667, 11.5002f, 54.7744f),
        new CityRecord(35, "Middelfart",           1655,  9.7305f, 55.5059f),
        new CityRecord(36, "Nykøbing Mors",       1598,  8.8520f, 56.7933f),
        new CityRecord(37, "Vordingborg",         1579, 11.9106f, 55.0080f),
        new CityRecord(38, "Bogense",             1497, 10.0891f, 55.5660f),
        new CityRecord(39, "Nexø",                1403, 15.1303f, 55.0607f),
        new CityRecord(40, "Skagen",              1400, 10.5839f, 57.7209f)
    };

    private readonly List<CityVisual> cityVisuals = new List<CityVisual>(CityData.Length);

    private Material capitalMaterial;
    private Material majorMaterial;
    private Material townMaterial;
    private GUIStyle labelStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<Campaign4CityLayer1850>() != null)
            return;

        GameObject root = new GameObject("CAMPAIGN4_Cities_1850_Top40");
        DontDestroyOnLoad(root);
        root.AddComponent<Campaign4CityLayer1850>();
    }

    private void Start()
    {
        RemoveLegacyCityMarkers();
        BuildMaterials();
        BuildCities();

        Debug.Log(
            "CAMPAIGN4-CITIES|Installed=True|Source=Census1850|Selection=Top40Købstæder|" +
            "Count=" + cityVisuals.Count + "|Esbjerg=False|CRS=WGS84");
    }

    private void RemoveLegacyCityMarkers()
    {
        Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate == null || candidate == transform)
                continue;

            // GrandCampaignBootstrap v10e markers use CITY_<name>.
            // Campaign4 markers deliberately use CITY1850_<rank>_<name>.
            if (candidate.name.StartsWith("CITY_", StringComparison.Ordinal))
                Destroy(candidate.gameObject);
        }
    }

    private void BuildMaterials()
    {
        capitalMaterial = CreateMaterial(new Color(0.95f, 0.80f, 0.35f), "C4_City_Capital");
        majorMaterial = CreateMaterial(new Color(0.91f, 0.86f, 0.70f), "C4_City_Major");
        townMaterial = CreateMaterial(new Color(0.80f, 0.80f, 0.74f), "C4_City_Town");
    }

    private void BuildCities()
    {
        for (int i = 0; i < CityData.Length; i++)
        {
            CityRecord city = CityData[i];
            GameObject root = new GameObject(
                "CITY1850_" + city.Rank.ToString("00") + "_" + city.Name);
            root.transform.SetParent(transform, false);
            root.transform.position = CampaignGeoProjection.Project(city.Longitude, city.Latitude, 0.52f);

            float diameter = MarkerDiameter(city);
            Material material = city.Rank == 1
                ? capitalMaterial
                : city.Rank <= 10 ? majorMaterial : townMaterial;

            GameObject baseMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseMarker.name = "Marker";
            baseMarker.transform.SetParent(root.transform, false);
            baseMarker.transform.localPosition = Vector3.zero;
            baseMarker.transform.localScale = new Vector3(diameter, 0.07f, diameter);
            baseMarker.GetComponent<Renderer>().sharedMaterial = material;
            RemoveCollider(baseMarker);

            // Small vertical centre piece makes the marker legible as a 3D city
            // symbol when the map camera is tilted in later Campaign4 iterations.
            GameObject centre = GameObject.CreatePrimitive(PrimitiveType.Cube);
            centre.name = "Centre";
            centre.transform.SetParent(root.transform, false);
            centre.transform.localPosition = new Vector3(0f, 0.13f, 0f);
            float tower = Mathf.Max(0.11f, diameter * 0.30f);
            centre.transform.localScale = new Vector3(tower, 0.16f, tower);
            centre.GetComponent<Renderer>().sharedMaterial = material;
            RemoveCollider(centre);

            cityVisuals.Add(new CityVisual { Data = city, Root = root });
        }
    }

    private static float MarkerDiameter(CityRecord city)
    {
        if (city.Rank == 1)
            return 0.76f;

        float t = Mathf.InverseLerp(1400f, 11122f, city.Population1850);
        return Mathf.Lerp(0.28f, 0.52f, Mathf.Sqrt(t));
    }

    private static void RemoveCollider(GameObject gameObject)
    {
        Collider collider = gameObject.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);
    }

    private void EnsureLabelStyle()
    {
        if (labelStyle != null)
            return;

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 9,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        labelStyle.normal.textColor = new Color(0.96f, 0.94f, 0.82f);
    }

    private void OnGUI()
    {
        Camera mapCamera = Camera.main;
        if (mapCamera == null || cityVisuals.Count == 0)
            return;

        EnsureLabelStyle();

        int maxRankToLabel = 40;
        if (mapCamera.orthographic)
        {
            if (mapCamera.orthographicSize > 49f)
                maxRankToLabel = 5;
            else if (mapCamera.orthographicSize > 29f)
                maxRankToLabel = 15;
        }

        for (int i = 0; i < cityVisuals.Count; i++)
        {
            CityVisual city = cityVisuals[i];
            if (city.Data.Rank > maxRankToLabel || city.Root == null)
                continue;

            Vector3 screen = mapCamera.WorldToScreenPoint(city.Root.transform.position);
            if (screen.z <= 0f)
                continue;
            if (screen.x < -120f || screen.x > Screen.width + 20f ||
                screen.y < -20f || screen.y > Screen.height + 20f)
                continue;

            float y = Screen.height - screen.y;
            GUI.Label(
                new Rect(screen.x + 5f, y - 9f, 145f, 18f),
                city.Data.Name,
                labelStyle);
        }
    }

    private static Material CreateMaterial(Color color, string name)
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        return new Material(shader) { name = name, color = color };
    }
}