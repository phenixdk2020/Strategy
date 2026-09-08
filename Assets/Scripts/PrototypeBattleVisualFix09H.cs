using UnityEngine;

// v00.00.09h visual QA fixes observed in the 09g test recording.
// Replaces opaque rectangular particle quads with a generated soft-alpha smoke sprite
// and tunes smoke size/lifetime. This component is presentation-only.
[DefaultExecutionOrder(22000)]
public sealed class PrototypeBattleVisualFix09H : MonoBehaviour
{
    private Material smokeMaterial;
    private Texture2D smokeTexture;
    private float nextScan;
    private bool announced;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeBattleVisualFix09H>() != null)
            return;

        GameObject root = new GameObject("PrototypeBattleVisualFix_v000009h");
        root.AddComponent<PrototypeBattleVisualFix09H>();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextScan)
            return;

        nextScan = Time.unscaledTime + 1f;
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        EnsureSmokeMaterial();
        int tuned = 0;

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null)
                continue;

            Transform smokeTransform = regiment.transform.Find("BlackPowderSmoke");
            if (smokeTransform == null)
                continue;

            ParticleSystem smoke = smokeTransform.GetComponent<ParticleSystem>();
            ParticleSystemRenderer renderer = smokeTransform.GetComponent<ParticleSystemRenderer>();
            if (smoke == null || renderer == null)
                continue;

            TuneSmoke(smoke, renderer);
            tuned++;
        }

        if (!announced && tuned > 0)
        {
            announced = true;
            Debug.Log(
                "VISFIX-09H|SmokeSoftSprite=True|OpaqueQuadFix=True|TunedRegiments=" + tuned +
                "|MovementWrites=False");
        }
    }

    private void EnsureSmokeMaterial()
    {
        if (smokeMaterial != null)
            return;

        smokeTexture = CreateSoftSmokeTexture(48);

        Shader shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null)
            shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Unlit/Transparent");

        if (shader == null)
            return;

        smokeMaterial = new Material(shader)
        {
            name = "BlackPowderSmoke09H_Soft",
            color = new Color(0.86f, 0.87f, 0.84f, 0.52f)
        };

        if (smokeMaterial.HasProperty("_MainTex"))
            smokeMaterial.SetTexture("_MainTex", smokeTexture);
        if (smokeMaterial.HasProperty("_BaseMap"))
            smokeMaterial.SetTexture("_BaseMap", smokeTexture);
    }

    private void TuneSmoke(ParticleSystem smoke, ParticleSystemRenderer renderer)
    {
        ParticleSystem.MainModule main = smoke.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2.2f, 3.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.35f, 0.85f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.55f, 1.20f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.78f, 0.79f, 0.76f, 0.22f),
            new Color(0.92f, 0.92f, 0.89f, 0.46f));
        main.maxParticles = 220;

        ParticleSystem.ShapeModule shape = smoke.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(12f, 0.28f, 0.55f);

        ParticleSystem.ColorOverLifetimeModule colorLifetime = smoke.colorOverLifetime;
        colorLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.92f, 0.92f, 0.90f), 0f),
                new GradientColorKey(new Color(0.72f, 0.74f, 0.72f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.05f, 0f),
                new GradientAlphaKey(0.42f, 0.14f),
                new GradientAlphaKey(0.24f, 0.62f),
                new GradientAlphaKey(0f, 1f)
            });
        colorLifetime.color = gradient;

        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        if (smokeMaterial != null)
            renderer.sharedMaterial = smokeMaterial;
    }

    private static Texture2D CreateSoftSmokeTexture(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "SmokeSoftCircle09H",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Color[] pixels = new Color[size * size];
        float half = (size - 1) * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x - half) / half;
                float ny = (y - half) / half;
                float distance = Mathf.Sqrt(nx * nx + ny * ny);
                float alpha = Mathf.Clamp01(1f - distance);
                alpha = alpha * alpha * (3f - 2f * alpha);

                // Slight deterministic breakup prevents the sprite reading as a
                // perfect circle while remaining free of opaque square edges.
                float breakup = 0.88f + 0.12f * Mathf.Sin((x * 0.73f) + (y * 1.17f));
                alpha *= breakup;
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, false);
        return texture;
    }
}
