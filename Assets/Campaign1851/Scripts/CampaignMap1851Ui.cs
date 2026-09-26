using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Screen-space presentation for the 1851 map: title cartouche, legend, compass, scale bar,
/// Bornholm inset, city info and all place-name labels (projected every frame).
/// Built entirely in code; dark navy panels with gold rules and a serif face.
/// </summary>
public sealed class CampaignMap1851Ui : MonoBehaviour
{
    private static readonly Color Gold = new Color(0.83f, 0.69f, 0.40f);
    private static readonly Color PanelColour = new Color(0.055f, 0.075f, 0.10f, 0.9f);
    private static readonly Color Ink = new Color(0.95f, 0.92f, 0.84f);
    private static readonly Color SeaInk = new Color(0.68f, 0.78f, 0.86f, 0.85f);
    private static readonly Color MutedInk = new Color(0.72f, 0.71f, 0.67f, 0.8f);
    private static readonly Color CityRed = new Color(0.72f, 0.1f, 0.07f);
    private static readonly CultureInfo Danish = new CultureInfo("da-DK");

    private sealed class ScreenLabel
    {
        public RectTransform Rect;
        public Text Text;
        public int Priority;
        public Vector3 World;
        public string Kind;
        public int Population;
        public bool Capital;
        public Vector2 Offset;
    }

    private CampaignMap1851 map;
    private Canvas canvas;
    private RectTransform canvasRect;
    private RectTransform labelLayer;
    private Font serif;
    private Sprite circle;
    private readonly List<ScreenLabel> labels = new List<ScreenLabel>();

    private RectTransform compassRose;
    private readonly List<RectTransform> scaleSegments = new List<RectTransform>();
    private readonly List<Text> scaleTexts = new List<Text>();
    private RawImage clouds;
    private GameObject infoPanel;
    private Text infoName, infoKind, infoRegion, infoPopulation;

    public void Build(CampaignMap1851 owner)
    {
        map = owner;
        serif = Font.CreateDynamicFontFromOSFont(new[] { "Georgia", "Garamond", "Times New Roman" }, 16);
        circle = MakeCircleSprite(64);

        var canvasGo = new GameObject("Map1851_UI", typeof(RectTransform));
        canvasGo.transform.SetParent(transform, false);
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = map.MapCamera;
        canvas.planeDistance = 1f;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();
        canvasRect = (RectTransform)canvasGo.transform;

        if (FindAnyObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        labelLayer = NewRect("Labels", canvasRect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        clouds = BuildClouds();
        BuildPlaceLabels();
        BuildTitle();
        BuildLegend();
        BuildCompass();
        BuildScaleBar();
        BuildBornholmInset();
        BuildInfoPanel();
        BuildFooter();
    }

    // ---------------------------------------------------------------- per-frame

    public void Refresh(float cameraDistance)
    {
        Camera cam = map.MapCamera;
        placed.Clear();
        foreach (var label in labels)   // sorted by priority: important names claim space first
        {
            bool visible = IsVisibleAt(label, cameraDistance);
            // Viewport space maps straight onto the canvas rect, whatever the render target size is.
            Vector3 viewport = cam.WorldToViewportPoint(label.World);
            if (visible && viewport.z > 0f && viewport.x > -0.1f && viewport.x < 1.1f && viewport.y > -0.1f && viewport.y < 1.1f)
            {
                Vector2 size = canvasRect.rect.size;
                var local = new Vector2((viewport.x - 0.5f) * size.x, (viewport.y - 0.5f) * size.y);
                label.Rect.anchoredPosition = local + label.Offset;
                label.Rect.gameObject.SetActive(Claim(label));
            }
            else
            {
                label.Rect.gameObject.SetActive(false);
            }
        }

        compassRose.localRotation = Quaternion.Euler(0f, 0f, map.CameraRig.Yaw);
        UpdateScaleBar(cam);

        // Clouds drift in at the edges when zoomed in close, as in a painted bird's-eye view.
        clouds.color = new Color(1f, 1f, 1f, Mathf.Clamp01((220f - cameraDistance) / 160f) * 0.6f);
    }

    private readonly List<Rect> placed = new List<Rect>();

    /// <summary>Reserves the label's screen box; false if a higher-priority label already covers it.</summary>
    private bool Claim(ScreenLabel label)
    {
        float width = label.Text.preferredWidth;
        float height = label.Text.fontSize * 1.15f;
        Vector2 pivot = label.Rect.pivot;
        Vector2 p = label.Rect.anchoredPosition;
        var box = new Rect(p.x - width * pivot.x - 4f, p.y - height * 0.5f - 2f, width + 8f, height + 4f);
        foreach (var other in placed)
            if (other.Overlaps(box))
                return false;
        placed.Add(box);
        return true;
    }

    private static bool IsVisibleAt(ScreenLabel label, float d)
    {
        switch (label.Kind)
        {
            case "city":
                if (label.Capital) return true;
                int threshold = d > 420f ? 7500 : d > 220f ? 2500 : d > 110f ? 1200 : 0;
                return label.Population >= threshold;
            case "foreignCity": return d < 380f;
            case "strait": return d < 400f;
            case "land": return d > 45f;
            default: return true;
        }
    }

    // ---------------------------------------------------------------- labels

    private void BuildPlaceLabels()
    {
        foreach (var l in map.Data.labels)
        {
            Vector3 world = map.Project(l.lat, l.lon);
            switch (l.kind)
            {
                case "sea":
                    AddLabel(Spaced(l.text), world, l.kind, 24, SeaInk, FontStyle.Italic, TextAnchor.MiddleCenter);
                    break;
                case "strait":
                    AddLabel(l.text, world, l.kind, 15, SeaInk, FontStyle.Italic, TextAnchor.MiddleCenter);
                    break;
                case "land":
                    AddLabel(Spaced(l.text), world, l.kind, 26, new Color(0.96f, 0.93f, 0.8f, 0.78f), FontStyle.Italic, TextAnchor.MiddleCenter);
                    break;
                case "duchy":
                    AddLabel(Spaced(l.text.ToUpperInvariant()), world, l.kind, 17, Gold, FontStyle.Normal, TextAnchor.MiddleCenter);
                    break;
                default:
                    AddLabel(l.text, world, l.kind, 18, MutedInk, FontStyle.Italic, TextAnchor.MiddleCenter);
                    break;
            }
        }

        foreach (var c in map.Data.cities)
        {
            if (c.bornholm)
                continue;
            var label = AddLabel(c.name, map.Project(c.lat, c.lon), "city", c.capital ? 27 : 18, Ink,
                c.capital ? FontStyle.Bold : FontStyle.Normal, TextAnchor.MiddleLeft);
            label.Population = c.pop;
            label.Capital = c.capital;
            label.Priority = c.capital ? 1000000 : 1000 + c.pop / 10;
            label.Offset = new Vector2(c.capital ? 16f : 11f, 1f);
        }

        foreach (var c in map.Data.foreignCities)
        {
            // Foreign towns are labelled to the left so Hamborg and Altona do not collide.
            var label = AddLabel(c.name, map.Project(c.lat, c.lon), "foreignCity", 15, MutedInk, FontStyle.Italic, TextAnchor.MiddleRight);
            label.Offset = new Vector2(-10f, 1f);
            label.Priority = 100 + c.pop / 1000;
        }

        // Region and sea names outrank towns except the capital; straits and neighbours come last.
        foreach (var label in labels)
        {
            switch (label.Kind)
            {
                case "sea": label.Priority = 900000; break;
                case "duchy": label.Priority = 800000; break;
                case "land": label.Priority = 700000; break;
                case "strait": label.Priority = 500; break;
                case "foreign": label.Priority = 400; break;
            }
        }
        labels.Sort((a, b) => b.Priority.CompareTo(a.Priority));
    }

    private ScreenLabel AddLabel(string text, Vector3 world, string kind, int size, Color colour, FontStyle style, TextAnchor anchor)
    {
        Vector2 pivot = anchor == TextAnchor.MiddleLeft ? new Vector2(0f, 0.5f)
            : anchor == TextAnchor.MiddleRight ? new Vector2(1f, 0.5f) : new Vector2(0.5f, 0.5f);
        var rect = NewRect(text, labelLayer, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pivot, Vector2.zero, new Vector2(420f, size * 1.6f));
        var t = NewText(rect, text, size, colour, style, anchor);
        t.raycastTarget = false;
        var shadow = t.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
        shadow.effectDistance = new Vector2(1.2f, -1.2f);
        var label = new ScreenLabel { Rect = rect, Text = t, World = world, Kind = kind };
        labels.Add(label);
        return label;
    }

    private static string Spaced(string text) => string.Join(" ", text.ToCharArray()).Replace("   ", "     ");

    // ---------------------------------------------------------------- panels

    private void BuildTitle()
    {
        var panel = NewPanel("Title", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -28f), new Vector2(430f, 168f));
        Crown(panel, new Vector2(0f, -14f));
        var title = NewText(NewRect("Name", panel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(0f, 66f)),
            "DANMARK", 60, Ink, FontStyle.Normal, TextAnchor.MiddleCenter);
        title.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.8f);
        NewText(NewRect("Year", panel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(0f, 44f)),
            "1851", 38, Ink, FontStyle.Normal, TextAnchor.MiddleCenter);
        NewImage(NewRect("Rule", panel, new Vector2(0.2f, 1f), new Vector2(0.8f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -138f), new Vector2(0f, 1.5f)), Gold);
        NewText(NewRect("Sub", panel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -142f), new Vector2(0f, 22f)),
            "KONGERIGET  ·  HERTUGDØMMERNE", 14, Gold, FontStyle.Normal, TextAnchor.MiddleCenter);
    }

    private void Crown(RectTransform panel, Vector2 offset)
    {
        // A small gold lozenge flanked by two rules, in place of a printed crown ornament.
        var lozenge = NewImage(NewRect("Lozenge", panel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), offset, new Vector2(9f, 9f)), Gold);
        lozenge.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        NewImage(NewRect("RuleL", panel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(1f, 0.5f), offset + new Vector2(-12f, 0f), new Vector2(70f, 1.5f)), Gold);
        NewImage(NewRect("RuleR", panel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0.5f), offset + new Vector2(12f, 0f), new Vector2(70f, 1.5f)), Gold);
    }

    private void BuildLegend()
    {
        var panel = NewPanel("Legend", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28f, -28f), new Vector2(360f, 330f));
        NewText(NewRect("Heading", panel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(-24f, 26f)),
            "Byer efter befolkning (ca. 1850)", 18, Ink, FontStyle.Normal, TextAnchor.MiddleCenter);

        var rows = new (string text, int pop)[]
        {
            ("> 50.000", 60000), ("20.000 – 50.000", 30000), ("10.000 – 20.000", 15000), ("5.000 – 10.000", 7000), ("< 5.000", 2000)
        };
        float y = -56f;
        foreach (var (text, pop) in rows)
        {
            float size = 13f * CampaignMap1851CityMarker.SizeClass(pop);
            var dot = NewImage(NewRect("Dot", panel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(58f, y), new Vector2(size, size)), CityRed);
            dot.sprite = circle;
            NewText(NewRect("Text", panel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 0.5f), new Vector2(100f, y), new Vector2(-110f, 24f)),
                text, 17, Ink, FontStyle.Normal, TextAnchor.MiddleLeft);
            y -= 30f;
        }

        y -= 6f;
        NewImage(NewRect("Rule", panel, new Vector2(0.08f, 1f), new Vector2(0.92f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(0f, 1f)), new Color(Gold.r, Gold.g, Gold.b, 0.5f));
        y -= 22f;
        LegendLine(panel, y, new Color(0.47f, 0.12f, 0.09f), false, "Monarkiets ydre grænse");
        y -= 28f;
        LegendLine(panel, y, new Color(0.84f, 0.71f, 0.38f), true, "Kongeå- og Ejdergrænsen");

        int count = 0;
        foreach (var c in map.Data.cities)
            count++;
        NewText(NewRect("Total", panel, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(-24f, 22f)),
            $"I alt: {count} byer i monarkiet", 15, MutedInk, FontStyle.Italic, TextAnchor.MiddleCenter);
    }

    private void LegendLine(RectTransform panel, float y, Color colour, bool dashed, string text)
    {
        int pieces = dashed ? 4 : 1;
        float width = 40f / pieces;
        for (int i = 0; i < pieces; i++)
        {
            float x = 38f + i * width + (dashed ? width * 0.25f : 0f);
            NewImage(NewRect("Line", panel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(x, y), new Vector2(dashed ? width * 0.55f : 40f, 3f)), colour);
        }
        NewText(NewRect("Text", panel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 0.5f), new Vector2(100f, y), new Vector2(-110f, 24f)),
            text, 16, Ink, FontStyle.Normal, TextAnchor.MiddleLeft);
    }

    private void BuildCompass()
    {
        var holder = NewRect("Compass", canvasRect, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(100f, 100f), new Vector2(150f, 150f));
        compassRose = NewRect("Rose", holder, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150f, 150f));
        var ring = NewImage(NewRect("Ring", compassRose, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(96f, 96f)), new Color(0.05f, 0.07f, 0.1f, 0.55f));
        ring.sprite = circle;
        var star = NewImage(NewRect("Star", compassRose, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120f, 120f)), Color.white);
        star.sprite = MakeCompassSprite(256);
        var letters = new (string t, Vector2 p)[] { ("N", new Vector2(0f, 68f)), ("S", new Vector2(0f, -68f)), ("Ø", new Vector2(68f, 0f)), ("V", new Vector2(-68f, 0f)) };
        foreach (var (t, p) in letters)
        {
            var txt = NewText(NewRect(t, compassRose, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), p, new Vector2(30f, 26f)), t, 19, Ink, FontStyle.Normal, TextAnchor.MiddleCenter);
            txt.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.9f);
        }
    }

    private void BuildScaleBar()
    {
        var holder = NewRect("ScaleBar", canvasRect, Vector2.zero, Vector2.zero, new Vector2(0f, 0f), new Vector2(200f, 44f), new Vector2(420f, 40f));
        for (int i = 0; i < 3; i++)
        {
            var seg = NewRect("Seg" + i, holder, Vector2.zero, Vector2.zero, new Vector2(0f, 0f), Vector2.zero, new Vector2(10f, 6f));
            NewImage(seg, i % 2 == 0 ? Ink : new Color(0.1f, 0.1f, 0.1f, 0.9f)).gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
            scaleSegments.Add(seg);
        }
        for (int i = 0; i < 4; i++)
        {
            var t = NewText(NewRect("Tick" + i, holder, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(90f, 22f)), "", 15, Ink, FontStyle.Normal, TextAnchor.LowerCenter);
            t.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.9f);
            scaleTexts.Add(t);
        }
    }

    private void UpdateScaleBar(Camera cam)
    {
        // Ground distance across 10% of the view width, converted to canvas units.
        var centre = new Vector3(cam.pixelWidth * 0.5f, cam.pixelHeight * 0.3f, 0f);
        var offset = new Vector3(cam.pixelWidth * 0.1f, 0f, 0f);
        if (!map.CameraRig.GroundPoint(centre, out Vector3 a) || !map.CameraRig.GroundPoint(centre + offset, out Vector3 b))
            return;
        float kmPerUnit = Vector3.Distance(a, b) / (canvasRect.rect.width * 0.1f);
        float[] steps = { 1f, 2f, 5f, 10f, 20f, 25f, 50f, 100f, 150f, 200f };
        float step = steps[0];
        foreach (float s in steps)
            if (3f * s / kmPerUnit <= 300f) step = s;
        float segUnits = step / kmPerUnit;
        for (int i = 0; i < 3; i++)
        {
            scaleSegments[i].anchoredPosition = new Vector2(i * segUnits, 0f);
            scaleSegments[i].sizeDelta = new Vector2(segUnits, 6f);
        }
        for (int i = 0; i < 4; i++)
        {
            scaleTexts[i].rectTransform.anchoredPosition = new Vector2(i * segUnits, 10f);
            scaleTexts[i].text = i == 3 ? $"{step * 3:0} km" : $"{step * i:0}";
        }
    }

    private void BuildBornholmInset()
    {
        var tex = map.BornholmTexture;
        float aspect = tex != null ? tex.width / (float)tex.height : 1.5f;
        var panel = NewPanel("Bornholm", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-28f, 28f), new Vector2(170f * aspect + 16f, 170f + 44f));
        var imageRect = NewRect("Map", panel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(170f * aspect, 170f));
        var raw = imageRect.gameObject.AddComponent<RawImage>();
        raw.texture = tex;
        raw.raycastTarget = false;
        NewText(NewRect("Name", panel, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(0f, 26f)),
            "Bornholm", 18, Ink, FontStyle.Italic, TextAnchor.MiddleCenter);

        foreach (var c in map.Data.cities)
        {
            if (!c.bornholm)
                continue;
            Vector2 uv = map.Data.bornholm.ToUv(c.lat, c.lon);
            var pos = new Vector2((uv.x - 0.5f) * imageRect.sizeDelta.x, (uv.y - 0.5f) * imageRect.sizeDelta.y);
            var dot = NewImage(NewRect("Dot", imageRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(11f, 11f)), CityRed);
            dot.sprite = circle;
            var t = NewText(NewRect("Label", imageRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f), pos + new Vector2(9f, 0f), new Vector2(120f, 22f)),
                c.name, 16, Ink, FontStyle.Normal, TextAnchor.MiddleLeft);
            t.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.9f);
        }
    }

    private void BuildInfoPanel()
    {
        var panel = NewPanel("CityInfo", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(28f, 190f), new Vector2(380f, 150f));
        infoPanel = panel.gameObject;
        infoName = NewText(NewRect("Name", panel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(22f, -12f), new Vector2(-40f, 40f)), "", 32, Ink, FontStyle.Normal, TextAnchor.MiddleLeft);
        infoKind = NewText(NewRect("Kind", panel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(22f, -52f), new Vector2(-40f, 24f)), "", 18, Gold, FontStyle.Italic, TextAnchor.MiddleLeft);
        infoRegion = NewText(NewRect("Region", panel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(22f, -80f), new Vector2(-40f, 24f)), "", 17, Ink, FontStyle.Normal, TextAnchor.MiddleLeft);
        infoPopulation = NewText(NewRect("Population", panel, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(22f, -108f), new Vector2(-40f, 24f)), "", 17, Ink, FontStyle.Normal, TextAnchor.MiddleLeft);
        infoPanel.SetActive(false);
    }

    public void ShowCity(Map1851City city, bool isForeign)
    {
        if (city == null)
        {
            infoPanel.SetActive(false);
            return;
        }
        infoPanel.SetActive(true);
        infoName.text = city.name;
        infoKind.text = isForeign ? "Udenlandsk by" : city.capital ? "Hovedstad" : "Købstad";
        infoRegion.text = isForeign ? "Uden for monarkiet" : Map1851Data.RegionName(city.region);
        infoPopulation.text = $"ca. {city.pop.ToString("N0", Danish)} indbyggere (ca. 1850)";
    }

    private void BuildFooter()
    {
        NewText(NewRect("Hint", canvasRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(1000f, 24f)),
            "Klik på en by  ·  Hjul: zoom  ·  Højre/midt-træk eller WASD: panorer  ·  Q/E: drej  ·  Home: hele kortet", 15, MutedInk, FontStyle.Normal, TextAnchor.MiddleCenter);
        NewText(NewRect("Version", canvasRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(800f, 20f)),
            CampaignMap1851.Version, 12, new Color(0.7f, 0.7f, 0.66f, 0.55f), FontStyle.Normal, TextAnchor.MiddleCenter);
    }

    private RawImage BuildClouds()
    {
        var rect = NewRect("Clouds", canvasRect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        var raw = rect.gameObject.AddComponent<RawImage>();
        raw.texture = MakeCloudTexture(512);
        raw.raycastTarget = false;
        return raw;
    }

    // ---------------------------------------------------------------- building blocks

    private RectTransform NewPanel(string name, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        var rect = NewRect(name, canvasRect, anchor, anchor, pivot, position, size);
        var image = NewImage(rect, PanelColour);
        image.raycastTarget = true;
        var outline = image.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(Gold.r, Gold.g, Gold.b, 0.85f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);
        // Inner gold rule, inset like a printed cartouche.
        var inner = NewRect("Frame", rect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-12f, -12f));
        var frame = NewImage(inner, new Color(0f, 0f, 0f, 0f));
        var innerOutline = frame.gameObject.AddComponent<Outline>();
        innerOutline.effectColor = new Color(Gold.r, Gold.g, Gold.b, 0.45f);
        innerOutline.effectDistance = new Vector2(1f, -1f);
        frame.raycastTarget = false;
        return rect;
    }

    private static RectTransform NewRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static Image NewImage(RectTransform rect, Color colour)
    {
        var image = rect.gameObject.AddComponent<Image>();
        image.color = colour;
        image.raycastTarget = false;
        return image;
    }

    private Text NewText(RectTransform rect, string text, int size, Color colour, FontStyle style, TextAnchor anchor)
    {
        var t = rect.gameObject.AddComponent<Text>();
        t.font = serif;
        t.text = text;
        t.fontSize = size;
        t.color = colour;
        t.fontStyle = style;
        t.alignment = anchor;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    // ---------------------------------------------------------------- procedural art

    private static Sprite MakeCircleSprite(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        float r = size * 0.5f - 1f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size * 0.5f, size * 0.5f));
                // Soft highlight so the dot reads as a ball, like the 3D markers.
                float light = Mathf.Lerp(1.25f, 0.8f, Mathf.Clamp01((x - y + size * 0.3f) / size));
                tex.SetPixel(x, y, new Color(light, light, light, Mathf.Clamp01(r - d + 0.5f)));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite MakeCompassSprite(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var gold = new Color(0.86f, 0.72f, 0.42f);
        var dark = new Color(0.52f, 0.41f, 0.22f);
        float half = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                var p = new Vector2(x + 0.5f - half, y + 0.5f - half);
                float d = p.magnitude / half;
                float a = Mathf.Atan2(p.y, p.x);
                float main = Mathf.Lerp(0.09f, 0.92f, Mathf.Pow(Mathf.Abs(Mathf.Cos(2f * a)), 14f));
                float minor = Mathf.Lerp(0.09f, 0.55f, Mathf.Pow(Mathf.Abs(Mathf.Sin(2f * a)), 14f));
                float edge = Mathf.Max(main, minor);
                float alpha = Mathf.Clamp01((edge - d) * half * 0.9f);
                // Split each point into a light and a dark half for a bevelled look.
                bool lit = Mathf.Repeat(a, Mathf.PI * 0.5f) < Mathf.PI * 0.25f;
                tex.SetPixel(x, y, new Color((lit ? gold : dark).r, (lit ? gold : dark).g, (lit ? gold : dark).b, alpha));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Texture2D MakeCloudTexture(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size, v = y / (float)size;
                float edge = Mathf.Max(Mathf.Abs(u - 0.5f) * 2f, Mathf.Abs(v - 0.5f) * 2f);
                float n = Mathf.PerlinNoise(u * 5f, v * 5f) * 0.6f + Mathf.PerlinNoise(u * 13f + 7f, v * 13f + 3f) * 0.4f;
                float a = Mathf.Clamp01((edge - 0.8f + (n - 0.5f) * 0.3f) * 5f);
                float shade = 0.82f + 0.18f * n;
                tex.SetPixel(x, y, new Color(shade, shade, shade * 1.02f, a * a));
            }
        }
        tex.Apply();
        return tex;
    }
}
