using System.Collections.Generic;
using UnityEngine;

// v00.00.09m - testable company volleys for Strategy-Kamp.
// Parent HQ stays HoldFire (09l4). Companies fire from their own centres
// using the documented 09l5 hit model so battles can actually be played.
[DefaultExecutionOrder(11210)]
public sealed class PrototypeKampCompanyFire09M : MonoBehaviour
{
    public static PrototypeKampCompanyFire09M Instance { get; private set; }

    private sealed class FireState
    {
        public float NextFireTime;
        public float LastVolleyTime;
        public float ReloadEnd;
        public ParticleSystem Smoke;
    }

    private readonly Dictionary<PrototypeCompanyTacticalEntity09L2, FireState> states =
        new Dictionary<PrototypeCompanyTacticalEntity09L2, FireState>();
    private readonly Dictionary<Regiment, float> lastRegimentVolley =
        new Dictionary<Regiment, float>();
    private readonly Dictionary<Regiment, float> regimentReloadEnd =
        new Dictionary<Regiment, float>();
    private bool announced;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeKampCompanyFire09M>() != null)
            return;

        GameObject root = new GameObject("PrototypeKampCompanyFire_v000009m");
        root.AddComponent<PrototypeKampCompanyFire09M>();
    }

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public bool TryGetLastVolley(Regiment regiment, out float time)
    {
        if (regiment != null && lastRegimentVolley.TryGetValue(regiment, out time))
            return true;
        time = -999f;
        return false;
    }

    public bool TryGetReloadWindow(Regiment regiment, out float start, out float end)
    {
        start = -999f;
        end = -999f;
        if (regiment == null || !lastRegimentVolley.TryGetValue(regiment, out start))
            return false;
        if (!regimentReloadEnd.TryGetValue(regiment, out end))
            return false;
        return end > start;
    }

    private void Update()
    {
        PrototypeCompanyTacticalControl09L2 control = PrototypeCompanyTacticalControl09L2.Instance;
        if (control == null || !control.Installed)
            return;

        IReadOnlyList<PrototypeCompanyTacticalEntity09L2> companies = control.Companies;
        if (!announced && companies.Count > 0)
        {
            announced = true;
            Debug.Log("KAMP-FIRE-09M|Installed=True|Owner=CompanyCentre|ParentHQ=HoldFire|Formula=09l5Volley|Smoke=CompanyMuzzle");
        }

        for (int i = 0; i < companies.Count; i++)
        {
            PrototypeCompanyTacticalEntity09L2 shooter = companies[i];
            if (shooter == null || shooter.ParentRegiment == null || shooter.CurrentStrength <= 0)
                continue;

            Regiment regiment = shooter.ParentRegiment;
            if (regiment.IsRouted)
                continue;

            FireState state = GetState(shooter);
            if (Time.time < state.NextFireTime)
                continue;

            PrototypeCompanyTacticalEntity09L2 target = FindTarget(shooter, companies);
            if (target == null)
                continue;

            FireVolley(shooter, target, state);
        }
    }

    private FireState GetState(PrototypeCompanyTacticalEntity09L2 company)
    {
        if (!states.TryGetValue(company, out FireState state))
        {
            state = new FireState
            {
                NextFireTime = Time.time + Random.Range(0.2f, 1.4f),
                Smoke = CreateCompanySmoke(company)
            };
            states[company] = state;
        }
        return state;
    }

    private static ParticleSystem CreateCompanySmoke(PrototypeCompanyTacticalEntity09L2 company)
    {
        GameObject psObject = new GameObject("CompanyMuzzleSmoke09M");
        psObject.transform.SetParent(company.transform, false);
        psObject.transform.localPosition = new Vector3(0f, 1.15f, 1.35f);
        ParticleSystem smoke = psObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = smoke.main;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = 2.4f;
        main.startSpeed = 1.15f;
        main.startSize = 1.05f;
        main.startColor = new Color(0.86f, 0.86f, 0.84f, 0.62f);
        main.maxParticles = 80;
        ParticleSystem.EmissionModule emission = smoke.emission;
        emission.enabled = false;
        ParticleSystem.ShapeModule shape = smoke.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(6.5f, 0.35f, 0.8f);
        ParticleSystemRenderer renderer = smoke.GetComponent<ParticleSystemRenderer>();
        Shader particleShader = Shader.Find("Particles/Standard Unlit");
        if (particleShader == null)
            particleShader = Shader.Find("Unlit/Color");
        if (particleShader != null)
            renderer.material = new Material(particleShader);
        return smoke;
    }

    private static PrototypeCompanyTacticalEntity09L2 FindTarget(
        PrototypeCompanyTacticalEntity09L2 shooter,
        IReadOnlyList<PrototypeCompanyTacticalEntity09L2> companies)
    {
        Regiment regiment = shooter.ParentRegiment;
        float trigger = regiment.MaximumRange;
        if (trigger <= 0.01f)
            trigger = regiment.EffectiveRange;

        PrototypeCompanyTacticalEntity09L2 best = null;
        float bestDist = trigger;

        Vector3 forward = shooter.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.01f)
            forward = Vector3.forward;
        forward.Normalize();

        for (int i = 0; i < companies.Count; i++)
        {
            PrototypeCompanyTacticalEntity09L2 candidate = companies[i];
            if (candidate == null || candidate == shooter || candidate.CurrentStrength <= 0)
                continue;
            if (candidate.ParentRegiment == null || candidate.ParentRegiment.Team == regiment.Team)
                continue;
            if (candidate.ParentRegiment.IsRouted)
                continue;

            Vector3 to = candidate.transform.position - shooter.transform.position;
            to.y = 0f;
            float dist = to.magnitude;
            if (dist > bestDist || dist < 0.5f)
                continue;
            if (Vector3.Angle(forward, to) > regiment.FireArcHalfAngle)
                continue;

            bestDist = dist;
            best = candidate;
        }

        return best;
    }

    private void FireVolley(
        PrototypeCompanyTacticalEntity09L2 shooter,
        PrototypeCompanyTacticalEntity09L2 target,
        FireState state)
    {
        Regiment regiment = shooter.ParentRegiment;
        Regiment enemy = target.ParentRegiment;
        float distance = Vector3.Distance(shooter.transform.position, target.transform.position);

        if (!shooter.IsMoving)
        {
            Vector3 to = target.transform.position - shooter.transform.position;
            to.y = 0f;
            if (to.sqrMagnitude > 0.05f)
            {
                Quaternion desired = Quaternion.LookRotation(to.normalized, Vector3.up);
                shooter.transform.rotation = Quaternion.Slerp(shooter.transform.rotation, desired, 0.65f);
            }
        }

        float rangeX;
        if (distance <= regiment.CloseRange)
            rangeX = 1.75f;
        else if (distance <= regiment.EffectiveRange)
            rangeX = Mathf.Lerp(1.75f, 1.00f, Mathf.InverseLerp(regiment.CloseRange, regiment.EffectiveRange, distance));
        else
            rangeX = Mathf.Lerp(1.00f, 0.30f, Mathf.InverseLerp(regiment.EffectiveRange, regiment.MaximumRange, distance));

        float weaponX = regiment.WeaponType == InfantryWeaponType.DreyseNeedleRifle ? (0.013f / 0.014f) : 1f;
        float moraleX = Mathf.Lerp(0.72f, 1f, regiment.Morale / 100f);
        float cohesionX = Mathf.Lerp(0.68f, 1f, regiment.Cohesion / 100f);
        int firingMen = Mathf.RoundToInt(shooter.CurrentStrength * 0.58f);
        float expected = firingMen * 0.014f * weaponX * rangeX * moraleX * cohesionX;
        int hits = Mathf.Max(0, Mathf.RoundToInt(expected * Random.Range(0.72f, 1.28f)));
        hits = Mathf.Min(hits, target.CurrentStrength);

        float shock = Mathf.Lerp(5.0f, 1.5f, Mathf.Clamp01(distance / regiment.MaximumRange));
        if (hits > 0 && enemy != null)
            enemy.ReceiveVolley(hits, shock, regiment);

        if (state.Smoke != null)
            state.Smoke.Emit(Random.Range(10, 22));

        float reload = regiment.CurrentReloadSeconds * Random.Range(0.90f, 1.12f);
        state.LastVolleyTime = Time.time;
        state.ReloadEnd = Time.time + reload;
        lastRegimentVolley[regiment] = Time.time;
        regimentReloadEnd[regiment] = state.ReloadEnd;
        state.NextFireTime = state.ReloadEnd;
    }
}
