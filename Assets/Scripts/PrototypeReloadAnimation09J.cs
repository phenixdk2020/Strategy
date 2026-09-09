using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09j TEST - Fire & Reload Animation Pass.
// Presentation-only. Detects actual outgoing volleys from Regiment.nextFireTime and
// drives the 09i articulated arms/rifle through fire, reload, march and ready poses.
// It never changes reload duration, fire cadence, ammo, hit calculation or movement.
[DefaultExecutionOrder(11400)]
public sealed class PrototypeReloadAnimation09J : MonoBehaviour
{
    private sealed class SoldierPoseRig
    {
        public Transform LeftArm;
        public Transform RightArm;
        public Transform RifleRoot;
        public float Phase;
    }

    private sealed class UnitState
    {
        public Regiment Regiment;
        public readonly List<SoldierPoseRig> Soldiers = new List<SoldierPoseRig>();
        public float LastObservedNextFireTime;
        public float ReloadStartTime;
        public float ReloadEndTime;
        public float FirePoseUntil;
        public bool ReloadActive;
        public bool AnnouncedReload;
        public Vector3 LastPosition;
        public bool Moving;
    }

    private readonly Dictionary<Regiment, UnitState> units = new Dictionary<Regiment, UnitState>();
    private FieldInfo nextFireTimeField;
    private const float MoveEpsilon = 0.0025f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeReloadAnimation09J>() != null)
            return;

        GameObject root = new GameObject("PrototypeReloadAnimation_v000009j");
        root.AddComponent<PrototypeReloadAnimation09J>();
    }

    private void Awake()
    {
        nextFireTimeField = typeof(Regiment).GetField(
            "nextFireTime",
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (nextFireTimeField == null)
        {
            Debug.LogError("RELOAD-09J|Installed=False|Reason=Regiment.nextFireTime_not_found");
            enabled = false;
            return;
        }

        Debug.Log(
            "RELOAD-09J|Installed=True|ActualVolleyDetection=True|" +
            "MuzzleLoaderSequence=True|DreyseSequence=True|MarchPreserved=True|CombatWrites=False");
    }

    private void Update()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null || nextFireTimeField == null)
            return;

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null)
                continue;

            UnitState state;
            if (!units.TryGetValue(regiment, out state))
            {
                state = CreateState(regiment);
                units[regiment] = state;
            }

            RefreshRigReferencesIfNeeded(state);
            UpdateMovementState(state);
            UpdateVolleyDetection(state);
            AnimateUnit(state);
        }

        CleanupDestroyedRegiments();
    }

    private UnitState CreateState(Regiment regiment)
    {
        UnitState state = new UnitState
        {
            Regiment = regiment,
            LastObservedNextFireTime = ReadNextFireTime(regiment),
            LastPosition = regiment.transform.position
        };

        RefreshRigReferences(state);
        return state;
    }

    private float ReadNextFireTime(Regiment regiment)
    {
        object value = nextFireTimeField.GetValue(regiment);
        return value is float ? (float)value : 0f;
    }

    private static void UpdateMovementState(UnitState state)
    {
        if (state == null || state.Regiment == null)
            return;

        Vector3 current = state.Regiment.transform.position;
        Vector3 delta = current - state.LastPosition;
        delta.y = 0f;
        state.Moving = delta.sqrMagnitude > MoveEpsilon;
        state.LastPosition = current;
    }

    private void UpdateVolleyDetection(UnitState state)
    {
        Regiment regiment = state.Regiment;
        if (regiment == null)
            return;

        float nextFireTime = ReadNextFireTime(regiment);
        bool newVolley =
            nextFireTime > Time.time + 0.05f &&
            nextFireTime > state.LastObservedNextFireTime + 0.10f;

        if (newVolley)
        {
            state.ReloadStartTime = Time.time;
            state.ReloadEndTime = nextFireTime;
            state.FirePoseUntil = Time.time + 0.30f;
            state.ReloadActive = true;
            state.AnnouncedReload = false;

            Debug.Log(
                "RELOAD-09J|Unit=" + regiment.RegimentName +
                "|VolleyDetected=True|Weapon=" + regiment.WeaponShortName +
                "|ReloadSeconds=" + (state.ReloadEndTime - state.ReloadStartTime).ToString("0.00"));
        }

        state.LastObservedNextFireTime = nextFireTime;

        if (state.ReloadActive && Time.time >= state.ReloadEndTime)
        {
            state.ReloadActive = false;
            Debug.Log(
                "RELOAD-09J|Unit=" + regiment.RegimentName +
                "|ReloadComplete=True|Weapon=" + regiment.WeaponShortName);
        }
    }

    private void AnimateUnit(UnitState state)
    {
        Regiment regiment = state.Regiment;
        if (regiment == null || state.Soldiers.Count == 0)
            return;

        bool firePose = Time.time < state.FirePoseUntil;
        bool reload = state.ReloadActive && !firePose;
        float reloadProgress = 1f;

        if (reload)
        {
            float duration = Mathf.Max(0.05f, state.ReloadEndTime - state.ReloadStartTime);
            reloadProgress = Mathf.Clamp01((Time.time - state.ReloadStartTime) / duration);

            if (!state.AnnouncedReload)
            {
                state.AnnouncedReload = true;
                Debug.Log(
                    "RELOAD-09J|Unit=" + regiment.RegimentName +
                    "|ReloadAnimation=True|Type=" + regiment.WeaponType);
            }
        }

        for (int i = 0; i < state.Soldiers.Count; i++)
        {
            SoldierPoseRig rig = state.Soldiers[i];
            if (rig == null || rig.RifleRoot == null || !rig.RifleRoot.gameObject.activeInHierarchy)
                continue;

            if (firePose)
                ApplyFirePose(rig);
            else if (reload)
                ApplyReloadPose(rig, regiment.WeaponType, reloadProgress, i);
            else if (state.Moving)
                ApplyMarchPose(rig, Time.time);
            else
                ApplyReadyPose(rig);
        }
    }

    private static void ApplyFirePose(SoldierPoseRig rig)
    {
        ApplyPose(rig,
            Quaternion.Euler(-68f, 8f, -8f),
            Quaternion.Euler(-60f, -8f, 12f),
            Quaternion.Euler(-5f, 0f, 0f),
            new Vector3(0.08f, 1.08f, 0.48f), 15f);
    }

    private static void ApplyReadyPose(SoldierPoseRig rig)
    {
        ApplyPose(rig,
            Quaternion.Euler(-42f, 8f, -5f),
            Quaternion.Euler(-30f, -6f, 7f),
            Quaternion.Euler(2f, 0f, 4f),
            new Vector3(0.18f, 1.00f, 0.25f), 8f);
    }

    private static void ApplyMarchPose(SoldierPoseRig rig, float time)
    {
        float phase = time * 7.4f + rig.Phase;
        float swing = Mathf.Sin(phase);
        float opposite = Mathf.Sin(phase + Mathf.PI);

        ApplyPose(rig,
            Quaternion.Euler(swing * 18f - 8f, 0f, -4f),
            Quaternion.Euler(opposite * 18f - 8f, 0f, 5f),
            Quaternion.Euler(5f, 0f, 8f),
            new Vector3(0.22f, 0.96f, 0.10f), 10f);
    }

    private static void ApplyReloadPose(
        SoldierPoseRig rig,
        InfantryWeaponType weapon,
        float progress,
        int soldierIndex)
    {
        float stagger = Mathf.Sin(soldierIndex * 1.73f) * 0.025f;
        float p = Mathf.Clamp01(progress + stagger);

        if (weapon == InfantryWeaponType.DreyseNeedleRifle)
            ApplyDreyseReload(rig, p);
        else
            ApplyMuzzleLoaderReload(rig, p);
    }

    private static void ApplyMuzzleLoaderReload(SoldierPoseRig rig, float p)
    {
        if (p < 0.18f)
        {
            float t = Smooth01(p / 0.18f);
            BlendPose(rig, t,
                Quaternion.Euler(-42f, 8f, -5f), Quaternion.Euler(-30f, -6f, 7f),
                Quaternion.Euler(-16f, 0f, -2f), Quaternion.Euler(-24f, 0f, 8f),
                Quaternion.Euler(2f, 0f, 4f), Quaternion.Euler(-72f, 0f, 0f),
                new Vector3(0.18f, 1.00f, 0.25f), new Vector3(0.10f, 0.92f, 0.16f));
        }
        else if (p < 0.42f)
        {
            float t = Smooth01((p - 0.18f) / 0.24f);
            BlendPose(rig, t,
                Quaternion.Euler(-16f, 0f, -2f), Quaternion.Euler(-24f, 0f, 8f),
                Quaternion.Euler(-54f, 18f, -18f), Quaternion.Euler(-70f, -12f, 18f),
                Quaternion.Euler(-72f, 0f, 0f), Quaternion.Euler(-83f, 0f, 0f),
                new Vector3(0.10f, 0.92f, 0.16f), new Vector3(0.05f, 0.83f, 0.12f));
        }
        else if (p < 0.72f)
        {
            float t = (p - 0.42f) / 0.30f;
            float ram = Mathf.Sin(t * Mathf.PI * 3f);
            ApplyPose(rig,
                Quaternion.Euler(-70f + ram * 8f, 15f, -15f),
                Quaternion.Euler(-78f - ram * 10f, -10f, 15f),
                Quaternion.Euler(-86f + ram * 3f, 0f, 0f),
                new Vector3(0.04f, 0.82f + ram * 0.035f, 0.10f), 14f);
        }
        else
        {
            float t = Smooth01((p - 0.72f) / 0.28f);
            BlendPose(rig, t,
                Quaternion.Euler(-62f, 12f, -12f), Quaternion.Euler(-68f, -8f, 14f),
                Quaternion.Euler(-42f, 8f, -5f), Quaternion.Euler(-30f, -6f, 7f),
                Quaternion.Euler(-82f, 0f, 0f), Quaternion.Euler(2f, 0f, 4f),
                new Vector3(0.05f, 0.84f, 0.11f), new Vector3(0.18f, 1.00f, 0.25f));
        }
    }

    private static void ApplyDreyseReload(SoldierPoseRig rig, float p)
    {
        if (p < 0.22f)
        {
            float t = Smooth01(p / 0.22f);
            BlendPose(rig, t,
                Quaternion.Euler(-42f, 8f, -5f), Quaternion.Euler(-30f, -6f, 7f),
                Quaternion.Euler(-30f, 12f, -10f), Quaternion.Euler(-58f, -18f, 20f),
                Quaternion.Euler(2f, 0f, 4f), Quaternion.Euler(20f, 0f, 4f),
                new Vector3(0.18f, 1.00f, 0.25f), new Vector3(0.16f, 0.86f, 0.20f));
        }
        else if (p < 0.62f)
        {
            float t = (p - 0.22f) / 0.40f;
            float bolt = Mathf.Sin(t * Mathf.PI * 2f);
            ApplyPose(rig,
                Quaternion.Euler(-38f, 12f, -10f),
                Quaternion.Euler(-72f + bolt * 16f, -18f, 20f),
                Quaternion.Euler(20f + bolt * 5f, 0f, 4f),
                new Vector3(0.15f, 0.86f, 0.20f), 15f);
        }
        else
        {
            float t = Smooth01((p - 0.62f) / 0.38f);
            BlendPose(rig, t,
                Quaternion.Euler(-38f, 12f, -10f), Quaternion.Euler(-62f, -15f, 18f),
                Quaternion.Euler(-42f, 8f, -5f), Quaternion.Euler(-30f, -6f, 7f),
                Quaternion.Euler(20f, 0f, 4f), Quaternion.Euler(2f, 0f, 4f),
                new Vector3(0.15f, 0.86f, 0.20f), new Vector3(0.18f, 1.00f, 0.25f));
        }
    }

    private static void BlendPose(
        SoldierPoseRig rig, float t,
        Quaternion leftFrom, Quaternion rightFrom,
        Quaternion leftTo, Quaternion rightTo,
        Quaternion rifleFrom, Quaternion rifleTo,
        Vector3 riflePosFrom, Vector3 riflePosTo)
    {
        ApplyPose(rig,
            Quaternion.Slerp(leftFrom, leftTo, t),
            Quaternion.Slerp(rightFrom, rightTo, t),
            Quaternion.Slerp(rifleFrom, rifleTo, t),
            Vector3.Lerp(riflePosFrom, riflePosTo, t), 14f);
    }

    private static void ApplyPose(
        SoldierPoseRig rig,
        Quaternion leftArm,
        Quaternion rightArm,
        Quaternion rifle,
        Vector3 riflePosition,
        float blendSpeed)
    {
        float blend = Mathf.Clamp01(Time.deltaTime * blendSpeed);
        if (rig.LeftArm != null)
            rig.LeftArm.localRotation = Quaternion.Slerp(rig.LeftArm.localRotation, leftArm, blend);
        if (rig.RightArm != null)
            rig.RightArm.localRotation = Quaternion.Slerp(rig.RightArm.localRotation, rightArm, blend);
        if (rig.RifleRoot != null)
        {
            rig.RifleRoot.localRotation = Quaternion.Slerp(rig.RifleRoot.localRotation, rifle, blend);
            rig.RifleRoot.localPosition = Vector3.Lerp(rig.RifleRoot.localPosition, riflePosition, blend);
        }
    }

    private static float Smooth01(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    private static void RefreshRigReferencesIfNeeded(UnitState state)
    {
        if (state != null && state.Regiment != null && state.Soldiers.Count == 0)
            RefreshRigReferences(state);
    }

    private static void RefreshRigReferences(UnitState state)
    {
        state.Soldiers.Clear();
        Regiment regiment = state.Regiment;
        if (regiment == null)
            return;

        int poseIndex = 0;
        for (int i = 0; i < regiment.transform.childCount; i++)
        {
            Transform soldier = regiment.transform.GetChild(i);
            if (soldier == null || !soldier.name.StartsWith("Soldier_"))
                continue;

            Transform visual = soldier.Find("Visual09I");
            if (visual == null)
                continue;

            state.Soldiers.Add(new SoldierPoseRig
            {
                LeftArm = visual.Find("Arm_Left"),
                RightArm = visual.Find("Arm_Right"),
                RifleRoot = visual.Find("RifleRig"),
                Phase = poseIndex * 0.73f
            });
            poseIndex++;
        }
    }

    private void CleanupDestroyedRegiments()
    {
        if (units.Count == 0)
            return;

        List<Regiment> remove = null;
        foreach (KeyValuePair<Regiment, UnitState> pair in units)
        {
            if (pair.Key != null)
                continue;

            if (remove == null)
                remove = new List<Regiment>();
            remove.Add(pair.Key);
        }

        if (remove == null)
            return;

        for (int i = 0; i < remove.Count; i++)
            units.Remove(remove[i]);
    }
}
