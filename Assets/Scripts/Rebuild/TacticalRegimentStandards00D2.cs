using System.Collections.Generic;
using UnityEngine;

namespace Project1864.Rebuild
{
    // v00.01.00d2 - presentation-only regiment standards.
    // One national flag + one regiment banner are shown per active regiment.
    // Artwork is a QA placeholder and is not a source-locked historical reconstruction yet.
    [DefaultExecutionOrder(575)]
    public sealed class TacticalRegimentStandards00D2 : MonoBehaviour
    {
        private sealed class RegimentVisual
        {
            public string RegimentId;
            public RebuildNation Nation;
            public readonly List<TacticalCompanyEntity00B> Companies = new List<TacticalCompanyEntity00B>();
            public GameObject Root;
        }

        private readonly List<RegimentVisual> regiments = new List<RegimentVisual>();
        private bool installed;

        private Material poleMaterial;
        private Material dkRed;
        private Material white;
        private Material black;
        private Material gold;
        private Material dkRegiment;
        private Material prRegiment;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (UnityEngine.Object.FindAnyObjectByType<TacticalRegimentStandards00D2>() != null)
                return;

            GameObject root = new GameObject("REBUILD_00D2_REGIMENT_STANDARDS");
            root.AddComponent<TacticalRegimentStandards00D2>();
        }

        private void Update()
        {
            if (!installed)
            {
                TryInstall();
                if (!installed)
                    return;
            }

            UpdateRegimentRoots();
        }

        private void TryInstall()
        {
            TacticalCompanyEntity00B[] companies = UnityEngine.Object.FindObjectsByType<TacticalCompanyEntity00B>();
            if (companies == null || companies.Length == 0 || RebuildOOBRegistry00B.Instance == null)
                return;

            CreateMaterials();
            regiments.Clear();
            Dictionary<string, RegimentVisual> byId = new Dictionary<string, RegimentVisual>();

            for (int i = 0; i < companies.Length; i++)
            {
                TacticalCompanyEntity00B company = companies[i];
                if (company == null)
                    continue;

                string regimentId = ResolveRegimentId(company);
                if (string.IsNullOrEmpty(regimentId))
                    continue;

                if (!byId.TryGetValue(regimentId, out RegimentVisual visual))
                {
                    visual = new RegimentVisual
                    {
                        RegimentId = regimentId,
                        Nation = company.Nation,
                        Root = new GameObject("00D2_Standards_" + regimentId)
                    };
                    visual.Root.transform.SetParent(transform, false);
                    byId.Add(regimentId, visual);
                    regiments.Add(visual);
                    BuildStandardPair(visual);
                }

                visual.Companies.Add(company);
            }

            installed = regiments.Count > 0;
            if (!installed)
                return;

            UpdateRegimentRoots();
            Debug.Log(
                "REBUILD-STANDARDS-00D2|Installed=True|ActiveRegiments=" + regiments.Count +
                "|NationalFlag=True|RegimentBanner=True|PresentationOnly=True|HistoricalArtLock=False");
        }

        private void UpdateRegimentRoots()
        {
            for (int r = 0; r < regiments.Count; r++)
            {
                RegimentVisual visual = regiments[r];
                if (visual.Root == null || visual.Companies.Count == 0)
                    continue;

                Vector3 centre = Vector3.zero;
                int valid = 0;
                TacticalCompanyEntity00B orientationSource = null;

                for (int i = 0; i < visual.Companies.Count; i++)
                {
                    TacticalCompanyEntity00B company = visual.Companies[i];
                    if (company == null)
                        continue;
                    centre += company.transform.position;
                    valid++;
                    if (orientationSource == null)
                        orientationSource = company;
                }

                if (valid == 0 || orientationSource == null)
                    continue;

                centre /= valid;
                visual.Root.transform.position = centre;
                visual.Root.transform.rotation = orientationSource.transform.rotation;
            }
        }

        private void BuildStandardPair(RegimentVisual visual)
        {
            bool danish = visual.Nation == RebuildNation.Denmark;

            GameObject national = new GameObject("National_Flag");
            national.transform.SetParent(visual.Root.transform, false);
            national.transform.localPosition = new Vector3(-0.80f, 0f, 0.20f);
            BuildPole(national.transform);
            if (danish)
                BuildCrossFlag(national.transform, dkRed, white, true);
            else
                BuildCrossFlag(national.transform, white, black, false);

            GameObject regiment = new GameObject("Regiment_Banner");
            regiment.transform.SetParent(visual.Root.transform, false);
            regiment.transform.localPosition = new Vector3(0.80f, 0f, 0.20f);
            BuildPole(regiment.transform);
            BuildRegimentBanner(regiment.transform, danish ? dkRegiment : prRegiment, danish);
        }

        private void BuildPole(Transform parent)
        {
            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Pole";
            pole.transform.SetParent(parent, false);
            pole.transform.localPosition = new Vector3(0f, 1.18f, 0f);
            pole.transform.localScale = new Vector3(0.025f, 1.18f, 0.025f);
            RemoveCollider(pole);
            SetMaterial(pole, poleMaterial);
        }

        private void BuildCrossFlag(Transform parent, Material field, Material cross, bool danishCross)
        {
            Vector3 centre = new Vector3(0.46f, 1.90f, 0f);
            CreateBox(parent, "Field", centre, new Vector3(0.90f, 0.56f, 0.025f), field);

            float verticalX = danishCross ? 0.30f : 0.46f;
            CreateBox(parent, "CrossVertical", new Vector3(verticalX, 1.90f, -0.018f),
                new Vector3(0.10f, 0.57f, 0.018f), cross);
            CreateBox(parent, "CrossHorizontal", new Vector3(0.46f, 1.90f, -0.020f),
                new Vector3(0.91f, 0.10f, 0.018f), cross);
        }

        private void BuildRegimentBanner(Transform parent, Material field, bool danish)
        {
            Vector3 centre = new Vector3(0.46f, 1.90f, 0f);
            CreateBox(parent, "Field", centre, new Vector3(0.90f, 0.56f, 0.025f), field);

            // Gold border.
            CreateBox(parent, "TopTrim", new Vector3(0.46f, 2.165f, -0.020f), new Vector3(0.91f, 0.035f, 0.018f), gold);
            CreateBox(parent, "BottomTrim", new Vector3(0.46f, 1.635f, -0.020f), new Vector3(0.91f, 0.035f, 0.018f), gold);
            CreateBox(parent, "HoistTrim", new Vector3(0.025f, 1.90f, -0.020f), new Vector3(0.035f, 0.56f, 0.018f), gold);
            CreateBox(parent, "FlyTrim", new Vector3(0.895f, 1.90f, -0.020f), new Vector3(0.035f, 0.56f, 0.018f), gold);

            GameObject medallion = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            medallion.name = "RegimentMedallion";
            medallion.transform.SetParent(parent, false);
            medallion.transform.localPosition = new Vector3(0.47f, 1.90f, -0.038f);
            medallion.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            medallion.transform.localScale = new Vector3(0.15f, 0.018f, 0.15f);
            RemoveCollider(medallion);
            SetMaterial(medallion, danish ? gold : white);

            if (!danish)
            {
                CreateBox(parent, "PrussianCentreVertical", new Vector3(0.47f, 1.90f, -0.060f),
                    new Vector3(0.035f, 0.25f, 0.012f), black);
                CreateBox(parent, "PrussianCentreHorizontal", new Vector3(0.47f, 1.90f, -0.061f),
                    new Vector3(0.25f, 0.035f, 0.012f), black);
            }
        }

        private static string ResolveRegimentId(TacticalCompanyEntity00B company)
        {
            if (company == null || RebuildOOBRegistry00B.Instance == null)
                return string.Empty;

            RebuildUnitRecord battalion = RebuildOOBRegistry00B.Instance.Get(company.ParentUnitId);
            return battalion != null ? battalion.ParentUnitId : string.Empty;
        }

        private void CreateMaterials()
        {
            poleMaterial = CreateMaterial(new Color(0.28f, 0.18f, 0.08f), "00D2_Pole");
            dkRed = CreateMaterial(new Color(0.72f, 0.04f, 0.06f), "00D2_DannebrogRed");
            white = CreateMaterial(new Color(0.94f, 0.94f, 0.90f), "00D2_White");
            black = CreateMaterial(new Color(0.025f, 0.025f, 0.030f), "00D2_Black");
            gold = CreateMaterial(new Color(0.76f, 0.58f, 0.16f), "00D2_Gold");
            dkRegiment = CreateMaterial(new Color(0.32f, 0.025f, 0.055f), "00D2_DK_RegimentField");
            prRegiment = CreateMaterial(new Color(0.075f, 0.085f, 0.11f), "00D2_PR_RegimentField");
        }

        private static GameObject CreateBox(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            RemoveCollider(go);
            SetMaterial(go, material);
            return go;
        }

        private static void RemoveCollider(GameObject go)
        {
            Collider collider = go.GetComponent<Collider>();
            if (collider != null)
                UnityEngine.Object.Destroy(collider);
        }

        private static void SetMaterial(GameObject go, Material material)
        {
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = material;
        }

        private static Material CreateMaterial(Color color, string name)
        {
            Shader shader = Shader.Find("Standard");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            Material material = new Material(shader);
            material.name = name;
            material.color = color;
            return material;
        }
    }
}
