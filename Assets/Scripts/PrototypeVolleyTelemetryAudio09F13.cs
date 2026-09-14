using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f13 ranged-combat QA + first-pass audio.
// Detects completed volleys from Regiment.nextFireTime, logs actual distance/band/hits,
// and plays a lightweight procedural 3D black-powder volley sound with no external asset.
[DefaultExecutionOrder(37000)]
public sealed class PrototypeVolleyTelemetryAudio09F13 : MonoBehaviour
{
    private sealed class State
    {
        public float LastNextFireTime;
        public AudioSource Audio;
    }

    private readonly Dictionary<Regiment, State> states = new Dictionary<Regiment, State>();

    private FieldInfo nextFireTimeField;
    private FieldInfo forcedTargetField;
    private AudioClip volleyClip;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeVolleyTelemetryAudio09F13>() != null)
            return;

        GameObject root = new GameObject("PrototypeVolleyTelemetryAudio_v000009f13");
        root.AddComponent<PrototypeVolleyTelemetryAudio09F13>();
    }

    private void Awake()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        nextFireTimeField = typeof(Regiment).GetField("nextFireTime", flags);
        forcedTargetField = typeof(Regiment).GetField("forcedTarget", flags);
        volleyClip = CreateProceduralVolleyClip();

        Debug.Log(
            "VOLLEY-QA-09F13|Installed=True|Log=Distance,Band,FiringMen,Expected,Hits|" +
            "Audio=Procedural3DPlaceholder");
    }

    private void Update()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null || nextFireTimeField == null)
            return;

        HashSet<Regiment> active = new HashSet<Regiment>();

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null)
                continue;

            active.Add(regiment);
            float next = ReadNextFireTime(regiment);

            if (!states.TryGetValue(regiment, out State state))
            {
                state = new State
                {
                    LastNextFireTime = next,
                    Audio = EnsureAudioSource(regiment)
                };
                states[regiment] = state;
                continue;
            }

            if (next > state.LastNextFireTime + 0.05f && next > Time.time)
                OnVolley(regiment, battle, state);

            state.LastNextFireTime = next;
        }

        Cleanup(active);
    }

    private void OnVolley(Regiment shooter, BattleManager battle, State state)
    {
        Regiment target = FindVolleyTarget(shooter, battle);

        if (state.Audio != null && volleyClip != null)
        {
            state.Audio.pitch = UnityEngine.Random.Range(0.94f, 1.06f);
            state.Audio.PlayOneShot(volleyClip, 0.92f);
        }

        if (target == null)
        {
            Debug.Log(
                "VOLLEY-QA-09F13|Shooter=" + shooter.RegimentName +
                "|Target=UNKNOWN|DistanceM=NA|Band=UNKNOWN|Hits=UNKNOWN");
            return;
        }

        float distance = PlanarDistance(shooter.transform.position, target.transform.position);
        string band = PrototypeCombatTuningManager.GetRangeBandLabel(shooter, distance);
        int firingMen = PrototypeCombatTuningManager.GetConfiguredFiringMen(shooter);
        float rangeFactor = PrototypeCombatTuningManager.GetConfiguredRangeMultiplier(shooter, distance);
        float quality = PrototypeCombatTuningManager.GetConfiguredQualityMultiplier(shooter);
        float expected = PrototypeCombatTuningManager.GetExpectedHitsPreview(shooter, distance);
        int hits = target.LastVolleyHits;

        Debug.Log(
            "VOLLEY-QA-09F13|Shooter=" + shooter.RegimentName +
            "|Target=" + target.RegimentName +
            "|DistanceM=" + distance.ToString("0.0") +
            "|Band=" + band +
            "|FiringMen=" + firingMen +
            "|BaseHit=" + PrototypeCombatTuningManager.BaseHitChancePercent.ToString("0.00") + "%" +
            "|RangeFactor=x" + rangeFactor.ToString("0.00") +
            "|Quality=x" + quality.ToString("0.00") +
            "|Expected=" + expected.ToString("0.00") +
            "|Hits=" + hits +
            "|TargetRemaining=" + target.CurrentStrength);
    }

    private Regiment FindVolleyTarget(Regiment shooter, BattleManager battle)
    {
        if (forcedTargetField != null)
        {
            Regiment forced = forcedTargetField.GetValue(shooter) as Regiment;
            if (forced != null && forced.Team != shooter.Team)
                return forced;
        }

        Regiment bestTarget = null;
        float bestDistance = float.PositiveInfinity;

        foreach (Regiment candidate in battle.Regiments)
        {
            if (candidate == null || candidate == shooter || candidate.Team == shooter.Team)
                continue;

            float distance = PlanarDistance(shooter.transform.position, candidate.transform.position);
            if (distance > shooter.MaximumRange + 5f || distance >= bestDistance)
                continue;

            Vector3 toTarget = candidate.transform.position - shooter.transform.position;
            toTarget.y = 0f;
            Vector3 forward = shooter.transform.forward;
            forward.y = 0f;
            if (toTarget.sqrMagnitude > 0.01f && forward.sqrMagnitude > 0.01f)
            {
                float angle = Vector3.Angle(forward.normalized, toTarget.normalized);
                if (angle > 50f)
                    continue;
            }

            bestDistance = distance;
            bestTarget = candidate;
        }

        return bestTarget;
    }

    private float ReadNextFireTime(Regiment regiment)
    {
        object value = nextFireTimeField.GetValue(regiment);
        return value is float f ? f : 0f;
    }

    private AudioSource EnsureAudioSource(Regiment regiment)
    {
        AudioSource source = regiment.GetComponent<AudioSource>();
        if (source == null)
            source = regiment.gameObject.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 1.0f;
        source.dopplerLevel = 0f;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = 15f;
        source.maxDistance = 650f;
        source.volume = 0.82f;
        return source;
    }

    private static AudioClip CreateProceduralVolleyClip()
    {
        const int sampleRate = 44100;
        const float duration = 0.72f;
        int samples = Mathf.CeilToInt(sampleRate * duration);
        float[] data = new float[samples];
        System.Random random = new System.Random(1864);

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float noise = (float)(random.NextDouble() * 2.0 - 1.0);

            // Fast muzzle crack, dense short noise body, then low black-powder boom/tail.
            float crack = noise * Mathf.Exp(-t * 78f) * 0.92f;
            float body = noise * Mathf.Exp(-t * 12f) * 0.34f;
            float boom = Mathf.Sin(2f * Mathf.PI * 82f * t) * Mathf.Exp(-t * 7.5f) * 0.46f;
            float rumble = Mathf.Sin(2f * Mathf.PI * 43f * t + 0.35f) * Mathf.Exp(-t * 4.3f) * 0.20f;

            data[i] = Mathf.Clamp(crack + body + boom + rumble, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("ProceduralMusketVolley09F13", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private void Cleanup(HashSet<Regiment> active)
    {
        if (states.Count == 0)
            return;

        List<Regiment> remove = null;
        foreach (KeyValuePair<Regiment, State> pair in states)
        {
            if (pair.Key != null && active.Contains(pair.Key))
                continue;

            if (remove == null)
                remove = new List<Regiment>();
            remove.Add(pair.Key);
        }

        if (remove == null)
            return;

        for (int i = 0; i < remove.Count; i++)
            states.Remove(remove[i]);
    }
}
