using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f30i
// Lightweight procedural cavalry animation: visible mount/dismount transitions and
// a simple riding gait. Simulation authority remains in PrototypeCavalryUnit09F30.
[DefaultExecutionOrder(43400)]
public sealed class PrototypeCavalryAnimation09F30I : MonoBehaviour
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private const float DismountDuration = 2.25f;
    private const float RemountDuration = 4.50f;

    private sealed class TransitionState
    {
        public PrototypeCavalryUnit09F30 Unit;
        public bool Dismount;
        public float Started;
        public List<GameObject> Riders = new List<GameObject>();
        public List<Vector3> RiderScales = new List<Vector3>();
        public List<Transform> Feet = new List<Transform>();
        public List<Vector3> FootScales = new List<Vector3>();
        public List<Vector3> FootStartPositions = new List<Vector3>();
        public List<Vector3> FootRemountPositions = new List<Vector3>();
        public Transform FootRoot;
    }

    private static PrototypeCavalryAnimation09F30I instance;
    private readonly Dictionary<PrototypeCavalryUnit09F30, TransitionState> transitions =
        new Dictionary<PrototypeCavalryUnit09F30, TransitionState>();
    private sealed class MountedRig
    {
        public Transform Root;
        public Transform[] Legs = new Transform[4];
        public Quaternion[] LegBase = new Quaternion[4];
        public Transform[] Hooves = new Transform[4];
        public Quaternion[] HoofBase = new Quaternion[4];
        public Transform Head;
        public Quaternion HeadBase;
        public Transform Tail;
        public Quaternion TailBase;
        public Transform Rider;
        public Quaternion RiderBase;
    }

    private readonly Dictionary<PrototypeCavalryUnit09F30, List<Transform>> mountedCache =
        new Dictionary<PrototypeCavalryUnit09F30, List<Transform>>();
    private readonly Dictionary<PrototypeCavalryUnit09F30, List<MountedRig>> rigCache =
        new Dictionary<PrototypeCavalryUnit09F30, List<MountedRig>>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        GetOrCreate();
    }

    private static PrototypeCavalryAnimation09F30I GetOrCreate()
    {
        if (instance != null)
            return instance;

        instance = UnityEngine.Object.FindAnyObjectByType<PrototypeCavalryAnimation09F30I>();
        if (instance == null)
            instance = new GameObject("PrototypeCavalryAnimation_v000009f30i")
                .AddComponent<PrototypeCavalryAnimation09F30I>();
        return instance;
    }

    public static bool IsTransitioning(PrototypeCavalryUnit09F30 unit)
    {
        return unit != null && GetOrCreate().transitions.ContainsKey(unit);
    }

    public static void BeginDismount(PrototypeCavalryUnit09F30 unit)
    {
        GetOrCreate().Begin(unit, true);
    }

    public static void BeginRemount(PrototypeCavalryUnit09F30 unit)
    {
        GetOrCreate().Begin(unit, false);
    }

    private void Begin(PrototypeCavalryUnit09F30 unit, bool dismount)
    {
        if (unit == null || transitions.ContainsKey(unit))
            return;

        TransitionState state = Capture(unit, dismount);
        if (state == null)
            return;

        state.Started = Time.time;
        transitions[unit] = state;

        for (int i = 0; i < state.Riders.Count; i++)
        {
            if (state.Riders[i] == null) continue;
            state.Riders[i].SetActive(true);
            if (!dismount)
                state.Riders[i].transform.localScale = state.RiderScales[i] * 0.08f;
        }

        if (state.FootRoot != null)
            state.FootRoot.gameObject.SetActive(true);

        for (int i = 0; i < state.Feet.Count; i++)
        {
            if (state.Feet[i] == null) continue;
            if (dismount)
                state.Feet[i].localScale = state.FootScales[i] * 0.08f;
        }
    }

    private static TransitionState Capture(PrototypeCavalryUnit09F30 unit, bool dismount)
    {
        FieldInfo ridersField = typeof(PrototypeCavalryUnit09F30).GetField("mountedRiders", PrivateInstance);
        FieldInfo feetField = typeof(PrototypeCavalryUnit09F30).GetField("footFigures", PrivateInstance);
        FieldInfo footRootField = typeof(PrototypeCavalryUnit09F30).GetField("footRoot", PrivateInstance);

        List<GameObject> riders = ridersField != null ? ridersField.GetValue(unit) as List<GameObject> : null;
        List<Transform> feet = feetField != null ? feetField.GetValue(unit) as List<Transform> : null;
        Transform footRoot = footRootField != null ? footRootField.GetValue(unit) as Transform : null;
        if (riders == null || feet == null)
            return null;

        TransitionState state = new TransitionState
        {
            Unit = unit,
            Dismount = dismount,
            FootRoot = footRoot
        };

        for (int i = 0; i < riders.Count; i++)
        {
            GameObject rider = riders[i];
            if (rider == null) continue;
            state.Riders.Add(rider);
            state.RiderScales.Add(rider.transform.localScale);
        }

        for (int i = 0; i < feet.Count; i++)
        {
            Transform foot = feet[i];
            if (foot == null) continue;
            state.Feet.Add(foot);
            state.FootScales.Add(foot.localScale);
            state.FootStartPositions.Add(foot.localPosition);

            int slot = state.Feet.Count - 1;
            int columns = 8;
            int row = slot / columns;
            int col = slot % columns;
            state.FootRemountPositions.Add(new Vector3(
                (col - (columns - 1) * 0.5f) * 0.74f,
                0f,
                -2.0f - row * 0.78f));
        }

        return state;
    }

    private void Update()
    {
        UpdateTransitions();
        UpdateGait();
    }

    private void UpdateTransitions()
    {
        if (transitions.Count == 0)
            return;

        List<PrototypeCavalryUnit09F30> completed = null;
        foreach (KeyValuePair<PrototypeCavalryUnit09F30, TransitionState> pair in transitions)
        {
            TransitionState s = pair.Value;
            if (s == null || s.Unit == null)
            {
                if (completed == null) completed = new List<PrototypeCavalryUnit09F30>();
                completed.Add(pair.Key);
                continue;
            }

            float duration = s.Dismount ? DismountDuration : RemountDuration;
            float p = Mathf.Clamp01((Time.time - s.Started) / duration);
            float smooth = p * p * (3f - 2f * p);

            for (int i = 0; i < s.Riders.Count; i++)
            {
                if (s.Riders[i] == null) continue;
                float scale = s.Dismount ? Mathf.Lerp(1f, 0.08f, smooth) : Mathf.Lerp(0.08f, 1f, smooth);
                s.Riders[i].transform.localScale = s.RiderScales[i] * scale;
                s.Riders[i].transform.localRotation =
                    Quaternion.Euler(0f, 0f, (s.Dismount ? 1f : -1f) * 18f * Mathf.Sin(p * Mathf.PI));
            }

            for (int i = 0; i < s.Feet.Count; i++)
            {
                if (s.Feet[i] == null) continue;
                float scale = s.Dismount ? Mathf.Lerp(0.08f, 1f, smooth) : Mathf.Lerp(1f, 0.08f, smooth);
                s.Feet[i].localScale = s.FootScales[i] * scale;

                if (!s.Dismount)
                    s.Feet[i].localPosition = Vector3.Lerp(
                        s.FootStartPositions[i], s.FootRemountPositions[i], smooth);
            }

            if (p < 1f)
                continue;

            for (int i = 0; i < s.Riders.Count; i++)
            {
                if (s.Riders[i] == null) continue;
                s.Riders[i].transform.localScale = s.RiderScales[i];
                s.Riders[i].transform.localRotation = Quaternion.identity;
                s.Riders[i].SetActive(!s.Dismount);
            }

            for (int i = 0; i < s.Feet.Count; i++)
                if (s.Feet[i] != null)
                    s.Feet[i].localScale = s.FootScales[i];

            if (!s.Dismount && s.FootRoot != null)
                s.FootRoot.gameObject.SetActive(false);

            if (completed == null) completed = new List<PrototypeCavalryUnit09F30>();
            completed.Add(pair.Key);

            Debug.Log("CAV-ANIM-09F30I|Unit=" + s.Unit.UnitName +
                      "|Transition=" + (s.Dismount ? "DISMOUNT" : "REMOUNT") + "|Complete=True");
        }

        if (completed != null)
            for (int i = 0; i < completed.Count; i++)
                transitions.Remove(completed[i]);
    }

    private void UpdateGait()
    {
        PrototypeCavalryManager09F30 cavalry = PrototypeCavalryManager09F30.Instance;
        if (cavalry == null || !cavalry.Installed)
            return;

        ApplyGait(cavalry.Gardehusar);
        ApplyGait(cavalry.Dragon);
    }

    private void ApplyGait(PrototypeCavalryUnit09F30 unit)
    {
        if (unit == null || unit.Mode != PrototypeCavalryMode09F30.Mounted ||
            IsTransitioning(unit))
            return;

        List<MountedRig> rigs = GetRigs(unit);
        if (rigs == null)
            return;

        bool moving = unit.Action != PrototypeCavalryAction09F30.Hold || unit.HasDestination;
        bool charge = unit.Action == PrototypeCavalryAction09F30.Charge;
        float frequency = charge ? 11.2f : (unit.Formation == PrototypeCavalryFormation09F30.Column ? 7.8f : 6.6f);
        float legSwing = charge ? 38f : 24f;
        float hoofSwing = charge ? 20f : 12f;
        float bodyPitch = charge ? -7.0f : -1.5f;
        float bodyRoll = charge ? 2.2f : 1.2f;
        float headNod = charge ? 8.0f : 4.0f;
        float tailSwing = charge ? 12f : 7f;

        for (int i = 0; i < rigs.Count; i++)
        {
            MountedRig rig = rigs[i];
            if (rig == null || rig.Root == null)
                continue;

            if (!moving)
            {
                rig.Root.localRotation = Quaternion.RotateTowards(
                    rig.Root.localRotation, Quaternion.identity, 150f * Time.deltaTime);
                RestoreRig(rig, 160f * Time.deltaTime);
                continue;
            }

            float phase = Time.time * frequency + i * 0.47f;
            float diagonalA = Mathf.Sin(phase);
            float diagonalB = Mathf.Sin(phase + Mathf.PI);
            float halfBeat = Mathf.Sin(phase * 0.5f);

            // Horse diagonal gait: FL + RR together, FR + RL opposite.
            SetAnimatedRotation(rig.Legs[0], rig.LegBase[0], new Vector3(diagonalA * legSwing, 0f, 0f));
            SetAnimatedRotation(rig.Legs[1], rig.LegBase[1], new Vector3(diagonalB * legSwing, 0f, 0f));
            SetAnimatedRotation(rig.Legs[2], rig.LegBase[2], new Vector3(diagonalB * legSwing, 0f, 0f));
            SetAnimatedRotation(rig.Legs[3], rig.LegBase[3], new Vector3(diagonalA * legSwing, 0f, 0f));

            SetAnimatedRotation(rig.Hooves[0], rig.HoofBase[0], new Vector3(-diagonalA * hoofSwing, 0f, 0f));
            SetAnimatedRotation(rig.Hooves[1], rig.HoofBase[1], new Vector3(-diagonalB * hoofSwing, 0f, 0f));
            SetAnimatedRotation(rig.Hooves[2], rig.HoofBase[2], new Vector3(-diagonalB * hoofSwing, 0f, 0f));
            SetAnimatedRotation(rig.Hooves[3], rig.HoofBase[3], new Vector3(-diagonalA * hoofSwing, 0f, 0f));

            if (rig.Head != null)
                rig.Head.localRotation = rig.HeadBase * Quaternion.Euler(halfBeat * headNod, 0f, 0f);
            if (rig.Tail != null)
                rig.Tail.localRotation = rig.TailBase * Quaternion.Euler(0f, halfBeat * tailSwing, 0f);

            if (rig.Rider != null)
            {
                float riderBounce = Mathf.Sin(phase * 2f) * (charge ? 2.5f : 1.3f);
                rig.Rider.localRotation = rig.RiderBase *
                    Quaternion.Euler(bodyPitch + riderBounce, 0f, halfBeat * bodyRoll);
            }

            float rootPitch = bodyPitch * 0.35f + halfBeat * (charge ? 1.5f : 0.7f);
            rig.Root.localRotation = Quaternion.Euler(rootPitch, 0f, halfBeat * bodyRoll * 0.45f);
        }
    }

    private List<MountedRig> GetRigs(PrototypeCavalryUnit09F30 unit)
    {
        if (rigCache.TryGetValue(unit, out List<MountedRig> cached) && cached != null)
            return cached;

        List<Transform> figures;
        if (!mountedCache.TryGetValue(unit, out figures) || figures == null)
        {
            FieldInfo field = typeof(PrototypeCavalryUnit09F30).GetField("mountedFigures", PrivateInstance);
            figures = field != null ? field.GetValue(unit) as List<Transform> : null;
            mountedCache[unit] = figures;
        }
        if (figures == null)
            return null;

        List<MountedRig> rigs = new List<MountedRig>(figures.Count);
        for (int i = 0; i < figures.Count; i++)
        {
            Transform root = figures[i];
            if (root == null)
            {
                rigs.Add(null);
                continue;
            }

            MountedRig rig = new MountedRig { Root = root };
            for (int leg = 0; leg < 4; leg++)
            {
                rig.Legs[leg] = FindDescendant(root, "Leg" + leg);
                rig.LegBase[leg] = rig.Legs[leg] != null ? rig.Legs[leg].localRotation : Quaternion.identity;
                rig.Hooves[leg] = FindDescendant(root, "Hoof" + leg);
                rig.HoofBase[leg] = rig.Hooves[leg] != null ? rig.Hooves[leg].localRotation : Quaternion.identity;
            }

            rig.Head = FindDescendant(root, "HorseHead");
            rig.HeadBase = rig.Head != null ? rig.Head.localRotation : Quaternion.identity;
            rig.Tail = FindDescendant(root, "Tail");
            rig.TailBase = rig.Tail != null ? rig.Tail.localRotation : Quaternion.identity;

            rig.Rider = FindDescendant(root, "F30E_IdentityDetails");
            if (rig.Rider == null)
                rig.Rider = FindDescendant(root, "Rider");
            rig.RiderBase = rig.Rider != null ? rig.Rider.localRotation : Quaternion.identity;

            rigs.Add(rig);
        }

        rigCache[unit] = rigs;
        return rigs;
    }

    private static Transform FindDescendant(Transform root, string name)
    {
        if (root == null)
            return null;

        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
            if (all[i] != null && all[i].name == name)
                return all[i];
        return null;
    }

    private static void SetAnimatedRotation(Transform target, Quaternion baseRotation, Vector3 euler)
    {
        if (target != null)
            target.localRotation = baseRotation * Quaternion.Euler(euler);
    }

    private static void RestoreRig(MountedRig rig, float maxDegrees)
    {
        if (rig == null)
            return;

        for (int i = 0; i < 4; i++)
        {
            if (rig.Legs[i] != null)
                rig.Legs[i].localRotation = Quaternion.RotateTowards(rig.Legs[i].localRotation, rig.LegBase[i], maxDegrees);
            if (rig.Hooves[i] != null)
                rig.Hooves[i].localRotation = Quaternion.RotateTowards(rig.Hooves[i].localRotation, rig.HoofBase[i], maxDegrees);
        }

        if (rig.Head != null)
            rig.Head.localRotation = Quaternion.RotateTowards(rig.Head.localRotation, rig.HeadBase, maxDegrees);
        if (rig.Tail != null)
            rig.Tail.localRotation = Quaternion.RotateTowards(rig.Tail.localRotation, rig.TailBase, maxDegrees);
        if (rig.Rider != null)
            rig.Rider.localRotation = Quaternion.RotateTowards(rig.Rider.localRotation, rig.RiderBase, maxDegrees);
    }

}
