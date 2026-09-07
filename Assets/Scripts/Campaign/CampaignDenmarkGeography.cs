using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Denmark-first real-geography render layer.
/// Coastline geometry is derived from Natural Earth 1:50m Admin-0 country data
/// (public-domain Natural Earth vector data). It is physical/geographic scaffold,
/// not a claim about 1851 political borders. Historical 1851 borders,
/// infrastructure and ownership are separate campaign layers.
/// Source dataset:
/// natural-earth-vector/geojson/ne_50m_admin_0_countries.geojson, DNK feature.
/// </summary>
public static class CampaignDenmarkGeography
{
    private static readonly Vector2[][] DenmarkRings =
    {
        // Sjælland
        Ring(new float[] {
            12.568750f,55.785059f, 12.571191f,55.684961f, 12.545215f,55.655811f, 12.507031f,55.636621f,
            12.407129f,55.616260f, 12.320605f,55.587842f, 12.243457f,55.537891f, 12.215039f,55.466504f,
            12.275391f,55.414258f, 12.385156f,55.385645f, 12.413086f,55.286182f, 12.322461f,55.237109f,
            12.089941f,55.188135f, 12.065527f,55.069922f, 12.073047f,54.976758f, 12.068848f,54.909033f,
            12.050391f,54.815332f, 11.862305f,54.772607f, 11.740918f,54.915332f, 11.739844f,54.972461f,
            11.703613f,55.039160f, 11.696777f,55.095996f, 11.653809f,55.186914f, 11.475879f,55.211523f,
            11.406836f,55.214746f, 11.310254f,55.197852f, 11.286328f,55.204443f, 11.170703f,55.328613f,
            11.189746f,55.465625f, 11.128027f,55.534766f, 11.119531f,55.566064f, 11.120996f,55.600732f,
            11.070312f,55.629297f, 11.008789f,55.644434f, 10.978906f,55.721533f, 11.049609f,55.740234f,
            11.224414f,55.731201f, 11.275488f,55.736475f, 11.322266f,55.752539f, 11.463672f,55.879297f,
            11.459570f,55.907227f, 11.474707f,55.943457f, 11.627734f,55.956885f, 11.695898f,55.907910f,
            11.682227f,55.829492f, 11.690918f,55.729004f, 11.783594f,55.701660f, 11.819727f,55.697656f,
            11.858301f,55.771875f, 11.885352f,55.807959f, 11.922070f,55.828076f, 11.934570f,55.895898f,
            11.912793f,55.937305f, 11.866406f,55.968164f, 12.039648f,56.052148f, 12.218945f,56.118652f,
            12.323242f,56.122119f, 12.428223f,56.105859f, 12.525781f,56.083398f, 12.578711f,56.064062f,
            12.608398f,56.033008f, 12.542969f,55.958984f, 12.524805f,55.918457f
        }),
        // Jylland
        Ring(new float[] {
            9.739746f,54.825537f, 9.725000f,54.825537f, 9.661230f,54.834375f, 9.615820f,54.855420f,
            9.498730f,54.840430f, 9.341992f,54.806299f, 9.254980f,54.808008f, 9.185840f,54.844678f,
            8.902930f,54.896924f, 8.857227f,54.901123f, 8.670703f,54.903320f, 8.661426f,54.985937f,
            8.638281f,55.045557f, 8.572949f,55.134277f, 8.669824f,55.155664f, 8.651074f,55.328564f,
            8.615918f,55.418213f, 8.345313f,55.510303f, 8.132129f,55.599805f, 8.181348f,55.901172f,
            8.202344f,55.982373f, 8.121484f,56.139893f, 8.129883f,56.321191f, 8.163965f,56.606885f,
            8.231738f,56.618066f, 8.281445f,56.616699f, 8.473145f,56.565430f, 8.552930f,56.560303f,
            8.607617f,56.514502f, 8.671680f,56.495654f, 8.718066f,56.544287f, 8.736133f,56.627441f,
            8.888086f,56.735059f, 8.994531f,56.774805f, 9.067090f,56.793848f, 9.140332f,56.750439f,
            9.196387f,56.701660f, 9.209668f,56.808398f, 9.254883f,57.011719f, 9.110449f,57.043652f,
            8.992773f,57.016113f, 8.876074f,56.887256f, 8.771973f,56.725293f, 8.603125f,56.710400f,
            8.468359f,56.664551f, 8.346680f,56.712109f, 8.268262f,56.754004f, 8.266309f,56.815332f,
            8.284082f,56.852344f, 8.427051f,56.984424f, 8.618555f,57.111279f, 8.811523f,57.110059f,
            8.952246f,57.150586f, 9.036328f,57.155420f, 9.298828f,57.146533f, 9.433594f,57.174316f,
            9.554297f,57.232471f, 9.815137f,57.478418f, 9.962305f,57.580957f, 10.259082f,57.617041f,
            10.533301f,57.735400f, 10.609961f,57.736914f, 10.480957f,57.648682f, 10.460254f,57.614551f,
            10.444629f,57.562207f, 10.537109f,57.448535f, 10.517578f,57.379346f, 10.524121f,57.243213f,
            10.436914f,57.172266f, 10.338477f,57.021338f, 10.296094f,56.999121f, 10.287012f,56.822949f,
            10.296680f,56.780908f, 10.282715f,56.620508f, 10.383594f,56.554834f, 10.490234f,56.520508f,
            10.845898f,56.521729f, 10.882812f,56.492871f, 10.926172f,56.443262f, 10.894434f,56.359033f,
            10.856445f,56.295508f, 10.753418f,56.241992f, 10.621191f,56.202100f, 10.538965f,56.200342f,
            10.426953f,56.276172f, 10.373730f,56.251562f, 10.318750f,56.212891f, 10.226660f,56.005371f,
            10.183008f,55.865186f, 10.159375f,55.853809f, 10.107324f,55.874463f, 10.017383f,55.876074f,
            9.903711f,55.842822f, 9.962012f,55.813086f, 10.023633f,55.761426f, 9.999023f,55.735547f,
            9.899023f,55.707568f, 9.810352f,55.650977f, 9.773242f,55.608154f, 9.661426f,55.557471f,
            9.591113f,55.493213f, 9.625586f,55.413574f, 9.640234f,55.343652f, 9.670996f,55.266406f,
            9.643262f,55.204736f, 9.504785f,55.116260f, 9.453711f,55.039551f, 9.572363f,55.040527f,
            9.645410f,55.022803f, 9.688184f,55.000146f, 9.732324f,54.968018f, 9.705273f,54.928320f
        }),
        // Fyn
        Ring(new float[] {
            10.645117f,55.609814f, 10.686816f,55.557617f, 10.738086f,55.446338f, 10.819238f,55.321875f,
            10.785352f,55.269775f, 10.808398f,55.203027f, 10.785254f,55.133398f, 10.623828f,55.052441f,
            10.442773f,55.048779f, 10.254590f,55.087891f, 9.988770f,55.163184f, 9.967383f,55.205469f,
            9.930078f,55.228906f, 9.858984f,55.357227f, 9.860645f,55.515479f, 9.994238f,55.535303f,
            10.286133f,55.610840f, 10.353613f,55.598975f, 10.424023f,55.560352f, 10.505078f,55.558057f,
            10.622754f,55.612842f
        }),
        // Lolland
        Ring(new float[] {
            11.361426f,54.891650f, 11.538379f,54.829590f, 11.658105f,54.833154f, 11.739551f,54.807422f,
            11.758984f,54.767676f, 11.765918f,54.679443f, 11.680371f,54.653711f, 11.585938f,54.662451f,
            11.457422f,54.628857f, 11.035547f,54.773096f, 11.041699f,54.893359f, 11.058594f,54.940576f,
            11.258496f,54.951807f
        }),
        // Langeland
        Ring(new float[] {
            10.734082f,54.750732f, 10.689746f,54.745068f, 10.629492f,54.826074f, 10.621680f,54.851416f,
            10.692480f,54.903271f, 10.738281f,54.962012f, 10.856738f,55.052197f, 10.925000f,55.157861f,
            10.951074f,55.156201f, 10.920801f,55.062109f, 10.765234f,54.799658f
        }),
        // Falster / Møn group
        Ring(new float[] {
            12.549219f,54.965771f, 12.511035f,54.950879f, 12.357520f,54.961816f, 12.184473f,54.892480f,
            12.118848f,54.914404f, 12.143652f,54.958691f, 12.161719f,54.974805f, 12.219922f,54.993604f,
            12.258789f,55.021094f, 12.274023f,55.064111f, 12.310059f,55.040918f, 12.417188f,55.031201f,
            12.469531f,55.017480f, 12.513281f,54.997314f
        }),
        // Amager
        Ring(new float[] {
            12.665723f,55.596533f, 12.571582f,55.554004f, 12.550879f,55.556250f, 12.520313f,55.614600f,
            12.569922f,55.650098f, 12.599219f,55.680225f, 12.620020f,55.679346f, 12.648438f,55.646777f
        }),
        // Ærø
        Ring(new float[] {
            10.484375f,54.847559f, 10.417285f,54.837158f, 10.340527f,54.858936f, 10.215625f,54.940967f,
            10.199902f,54.962744f, 10.265527f,54.948828f, 10.346973f,54.905957f, 10.413672f,54.896826f,
            10.504883f,54.860547f
        }),
        // Als
        Ring(new float[] {
            10.061230f,54.886377f, 9.957129f,54.872461f, 9.903906f,54.896631f, 9.806250f,54.906006f,
            9.771191f,55.059912f, 9.781250f,55.069043f, 9.830371f,55.058252f, 9.998828f,54.986475f,
            10.057715f,54.907910f
        }),
        // Samsø
        Ring(new float[] {
            10.607324f,55.783057f, 10.590332f,55.765088f, 10.526953f,55.783789f, 10.520313f,55.848486f,
            10.544336f,55.906592f, 10.516113f,55.958545f, 10.547168f,55.991943f, 10.636328f,55.914160f,
            10.661719f,55.877588f, 10.627344f,55.833887f
        }),
        // Læsø
        Ring(new float[] {
            11.052148f,57.252539f, 11.011426f,57.229102f, 10.873828f,57.262256f, 10.934570f,57.308594f,
            11.085742f,57.329932f, 11.174512f,57.322900f, 11.076855f,57.276904f
        }),
        // Bornholm
        Ring(new float[] {
            15.087695f,55.021875f, 15.050781f,55.004932f, 14.885547f,55.032959f, 14.684180f,55.102246f,
            14.713672f,55.238037f, 14.765332f,55.296729f, 15.132617f,55.144531f, 15.137109f,55.087158f
        })
    };

    public static GameObject Create(Material landMaterial, Material coastMaterial)
    {
        GameObject root = new GameObject("GEO_Denmark_NaturalEarth50m");

        for (int i = 0; i < DenmarkRings.Length; i++)
        {
            Vector2[] ring = DenmarkRings[i];
            GameObject island = new GameObject("DNK_LandPart_" + (i + 1));
            island.transform.SetParent(root.transform, false);

            Vector3[] vertices = new Vector3[ring.Length];
            for (int p = 0; p < ring.Length; p++)
            {
                Vector3 projected = CampaignGeoProjection.Project(ring[p].x, ring[p].y, 0.18f);
                vertices[p] = island.transform.InverseTransformPoint(projected);
            }

            int[] triangles = Triangulate(ring);
            Mesh mesh = new Mesh
            {
                name = "DNK_NaturalEarth50m_Part_" + (i + 1),
                vertices = vertices,
                triangles = triangles
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            MeshFilter filter = island.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = island.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = landMaterial;

            CreateCoastline(island.transform, ring, coastMaterial, i + 1);
        }

        return root;
    }

    private static void CreateCoastline(Transform parent, Vector2[] ring, Material material, int part)
    {
        GameObject coast = new GameObject("DNK_Coast_" + part);
        coast.transform.SetParent(parent, false);
        LineRenderer line = coast.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.loop = true;
        line.widthMultiplier = 0.08f;
        line.positionCount = ring.Length;
        line.sharedMaterial = material;

        for (int i = 0; i < ring.Length; i++)
            line.SetPosition(i, CampaignGeoProjection.Project(ring[i].x, ring[i].y, 0.24f));
    }

    private static Vector2[] Ring(float[] values)
    {
        Vector2[] result = new Vector2[values.Length / 2];
        for (int i = 0; i < result.Length; i++)
            result[i] = new Vector2(values[i * 2], values[i * 2 + 1]);
        return result;
    }

    private static int[] Triangulate(Vector2[] input)
    {
        int n = input.Length;
        if (n < 3)
            return new int[0];

        List<int> indices = new List<int>(n);
        if (SignedArea(input) > 0f)
        {
            for (int i = 0; i < n; i++) indices.Add(i);
        }
        else
        {
            for (int i = n - 1; i >= 0; i--) indices.Add(i);
        }

        List<int> triangles = new List<int>((n - 2) * 3);
        int guard = 0;
        while (indices.Count > 2 && guard++ < n * n)
        {
            bool clipped = false;
            for (int i = 0; i < indices.Count; i++)
            {
                int prev = indices[(i - 1 + indices.Count) % indices.Count];
                int curr = indices[i];
                int next = indices[(i + 1) % indices.Count];

                Vector2 a = input[prev];
                Vector2 b = input[curr];
                Vector2 c = input[next];
                if (Cross(a, b, c) <= 0.0000001f)
                    continue;

                bool contains = false;
                for (int j = 0; j < indices.Count; j++)
                {
                    int candidate = indices[j];
                    if (candidate == prev || candidate == curr || candidate == next)
                        continue;
                    if (PointInTriangle(input[candidate], a, b, c))
                    {
                        contains = true;
                        break;
                    }
                }

                if (contains)
                    continue;

                // Top-down Unity mesh uses X/Z; reverse one winding axis so normals
                // face upward rather than down.
                triangles.Add(prev);
                triangles.Add(next);
                triangles.Add(curr);
                indices.RemoveAt(i);
                clipped = true;
                break;
            }

            if (!clipped)
                break;
        }

        return triangles.ToArray();
    }

    private static float SignedArea(Vector2[] points)
    {
        float area = 0f;
        for (int i = 0; i < points.Length; i++)
        {
            Vector2 a = points[i];
            Vector2 b = points[(i + 1) % points.Length];
            area += a.x * b.y - b.x * a.y;
        }
        return area * 0.5f;
    }

    private static float Cross(Vector2 a, Vector2 b, Vector2 c)
    {
        return (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
    }

    private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float c1 = Cross(a, b, p);
        float c2 = Cross(b, c, p);
        float c3 = Cross(c, a, p);
        bool hasNegative = c1 < 0f || c2 < 0f || c3 < 0f;
        bool hasPositive = c1 > 0f || c2 > 0f || c3 > 0f;
        return !(hasNegative && hasPositive);
    }
}
