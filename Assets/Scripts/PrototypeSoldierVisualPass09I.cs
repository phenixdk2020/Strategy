using System.Collections.Generic;
using UnityEngine;

// v00.00.09i2 TEST - Soldier Visual Pass 2 / Big Graphics Buff.
// Presentation-only. Rebuilds representative infantry as lightweight articulated
// procedural soldier rigs with better human proportions, faction silhouettes,
// equipment, officer/standard-bearer distinctions and simple march/fire posing.
// 09i2 increases tactical readability at medium zoom without changing simulation footprint.
// No Regiment movement, combat, morale, cohesion, selection or Officer AI state is written.
[DefaultExecutionOrder(11200)]
public sealed class PrototypeSoldierVisualPass09I : MonoBehaviour
{
    private sealed class SoldierRig
    {
        public Transform Root;
        public Transform DetailRoot;
        public Transform LeftArm;
        public Transform RightArm;
        public Transform LeftLeg;
        public Transform RightLeg;
        public Transform RifleRoot;
        public Vector3 BaseScale;
        public float Phase;
        public bool IsOfficer;
        public bool IsStandardBearer;
    }

    private sealed class UnitVisual
    {
        public Regiment Regiment;
        public PrototypeUniformProfile09H Profile;
        public readonly List<SoldierRig> Soldiers = new List<SoldierRig>();
        public Material Coat;
        public Material CoatShadow;
        public Material Trousers;
        public Material Headgear;
        public Material Trim;
        public Material Straps;
        public Material Equipment;
        public Material Skin;
        public Material Wood;
        public Material Metal;
        public Vector3 LastPosition;
        public float NextProfileRefresh;
        public float NextLodRefresh;
        public bool Moving;
        public bool DetailsVisible = true;
        public float CurrentReadabilityScale = 1f;
    }

    private readonly Dictionary<Regiment, UnitVisual> units =
        new Dictionary<Regiment, UnitVisual>();

    private Camera mainCamera;

    // 09i originally hid detailed equipment beyond 135 m. At ordinary tactical zoom
    // that made the visual pass collapse back toward the old stick/primitives look.
    // Keep equipment readable much farther out and use a bounded presentation-only
    // silhouette scale to preserve human readability. Formation slots/colliders are unchanged.
    private const float DetailDistance = 285f;
    private const float ReadabilityNearDistance = 70f;
    private const float ReadabilityFarDistance = 260f;
    private const float ReadabilityNearScale = 1.04f;
    private const float ReadabilityFarScale = 1.24f;
    private const float MoveEpsilon = 0.0025f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeSoldierVisualPass09I>() != null)
            return;

        GameObject root = new GameObject("PrototypeSoldierVisualPass_v000009i2");
        root.AddComponent<PrototypeSoldierVisualPass09I>();
    }

    private void Start()
    {
        mainCamera = Camera.main;
        Debug.Log(
            "SOLDIER-09I2|Installed=True|Pass=Visual2|ProceduralRig=True|" +
            "MarchPose=True|FirePose=True|DetailDistance=285|MediumZoomScale=True|" +
            "MovementWrites=False");
    }

    private void Update()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        if (mainCamera == null)
            mainCamera = Camera.main;

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null)
                continue;

            UnitVisual visual;
            if (!units.TryGetValue(regiment, out visual))
            {
                InstallRegiment(regiment);
                if (!units.TryGetValue(regiment, out visual))
                    continue;
            }

            UpdateUnitVisual(visual);
        }

        CleanupDestroyedRegiments();
    }

    private void InstallRegiment(Regiment regiment)
    {
        if (regiment == null || units.ContainsKey(regiment))
            return;

        PrototypeUniformProfile09H profile = GetCurrentProfile(regiment);
        UnitVisual visual = new UnitVisual
        {
            Regiment = regiment,
            Profile = profile,
            LastPosition = regiment.transform.position
        };

        CreateMaterials(regiment, visual);

        List<Transform> soldiers = new List<Transform>();
        for (int i = 0; i < regiment.transform.childCount; i++)
        {
            Transform child = regiment.transform.GetChild(i);
            if (child != null && child.name.StartsWith("Soldier_"))
                soldiers.Add(child);
        }

        int officerIndex = soldiers.Count > 4 ? Mathf.Clamp(soldiers.Count / 6 - 1, 0, soldiers.Count - 1) : 0;
        int standardIndex = soldiers.Count > 4 ? Mathf.Clamp(officerIndex + 1, 0, soldiers.Count - 1) : Mathf.Min(1, soldiers.Count - 1);

        for (int i = 0; i < soldiers.Count; i++)
        {
            Transform soldier = soldiers[i];
            HideLegacySoldierRenderers(soldier);
            SoldierRig rig = BuildSoldier(
                regiment,
                soldier,
                i,
                i == officerIndex,
                i == standardIndex,
                visual);
            visual.Soldiers.Add(rig);
        }

        ApplyProfileColors(visual);
        units[regiment] = visual;

        Debug.Log(
            "SOLDIER-09I2|Unit=" + regiment.RegimentName +
            "|Team=" + regiment.Team +
            "|Representatives=" + visual.Soldiers.Count +
            "|OfficerIndex=" + officerIndex +
            "|StandardBearerIndex=" + standardIndex +
            "|LegacyRenderersHidden=True|MovementWrites=False");
    }

    private void UpdateUnitVisual(UnitVisual visual)
    {
        Regiment regiment = visual.Regiment;
        if (regiment == null)
            return;

        Vector3 current = regiment.transform.position;
        Vector3 delta = current - visual.LastPosition;
        delta.y = 0f;
        visual.Moving = delta.sqrMagnitude > MoveEpsilon;
        visual.LastPosition = current;

        if (Time.unscaledTime >= visual.NextProfileRefresh)
        {
            visual.NextProfileRefresh = Time.unscaledTime + 0.20f;
            visual.Profile = GetCurrentProfile(regiment);
            ApplyProfileColors(visual);
        }

        if (Time.unscaledTime >= visual.NextLodRefresh)
        {
            visual.NextLodRefresh = Time.unscaledTime + 0.25f;
            UpdateDetailLodAndReadability(visual);
        }

        bool firing = regiment.HasHitFeedback;
        float time = Time.time;
        for (int i = 0; i < visual.Soldiers.Count; i++)
            AnimateSoldier(visual.Soldiers[i], visual.Moving, firing, time);
    }

    private void UpdateDetailLodAndReadability(UnitVisual visual)
    {
        if (mainCamera == null || visual.Regiment == null)
            return;

        float distance = Vector3.Distance(
            mainCamera.transform.position,
            visual.Regiment.transform.position);

        bool showDetails = distance <= DetailDistance;
        if (showDetails != visual.DetailsVisible)
        {
            visual.DetailsVisible = showDetails;
            for (int i = 0; i < visual.Soldiers.Count; i++)
            {
                Transform detail = visual.Soldiers[i].DetailRoot;
                if (detail != null)
                    detail.gameObject.SetActive(showDetails);
            }
        }

        float t = Mathf.InverseLerp(ReadabilityNearDistance, ReadabilityFarDistance, distance);
        float readabilityScale = Mathf.Lerp(ReadabilityNearScale, ReadabilityFarScale, t);
        if (Mathf.Abs(readabilityScale - visual.CurrentReadabilityScale) < 0.015f)
            return;

        visual.CurrentReadabilityScale = readabilityScale;
        for (int i = 0; i < visual.Soldiers.Count; i++)
        {
            SoldierRig rig = visual.Soldiers[i];
            if (rig == null || rig.Root == null)
                continue;

            // Visual-only exaggeration around each existing formation slot. It does not
            // move the slot itself and does not alter Regiment/BoxCollider dimensions.
            rig.Root.localScale = new Vector3(
                rig.BaseScale.x * readabilityScale,
                rig.BaseScale.y * readabilityScale,
                rig.BaseScale.z * readabilityScale);
        }
    }

    private static void AnimateSoldier(SoldierRig rig, bool moving, bool firing, float time)
    {
        if (rig == null || rig.Root == null)
            return;

        float phase = time * 7.4f + rig.Phase;
        float swing = Mathf.Sin(phase);
        float secondary = Mathf.Sin(phase + Mathf.PI);

        Quaternion leftArmTarget;
        Quaternion rightArmTarget;
        Quaternion leftLegTarget;
        Quaternion rightLegTarget;
        Quaternion rifleTarget;
        Vector3 riflePosition;

        if (firing && !rig.IsStandardBearer)
        {
            leftArmTarget = Quaternion.Euler(-68f, 8f, -8f);
            rightArmTarget = Quaternion.Euler(-60f, -8f, 12f);
            leftLegTarget = Quaternion.Euler(0f, 0f, 0f);
            rightLegTarget = Quaternion.Euler(0f, 0f, 0f);
            rifleTarget = Quaternion.Euler(-5f, 0f, 0f);
            riflePosition = new Vector3(0.08f, 1.08f, 0.48f);
        }
        else if (moving)
        {
            float armAmplitude = rig.IsOfficer ? 12f : 18f;
            float legAmplitude = 19f;
            leftArmTarget = Quaternion.Euler(swing * armAmplitude - 8f, 0f, -4f);
            rightArmTarget = Quaternion.Euler(secondary * armAmplitude - 8f, 0f, 5f);
            leftLegTarget = Quaternion.Euler(secondary * legAmplitude, 0f, 0f);
            rightLegTarget = Quaternion.Euler(swing * legAmplitude, 0f, 0f);
            rifleTarget = rig.IsOfficer
                ? Quaternion.Euler(0f, 0f, 0f)
                : Quaternion.Euler(5f, 0f, 8f);
            riflePosition = rig.IsOfficer
                ? new Vector3(0.22f, 0.78f, 0.08f)
                : new Vector3(0.22f, 0.96f, 0.10f);
        }
        else
        {
            leftArmTarget = rig.IsStandardBearer
                ? Quaternion.Euler(-48f, 0f, -8f)
                : Quaternion.Euler(-42f, 8f, -5f);
            rightArmTarget = rig.IsStandardBearer
                ? Quaternion.Euler(-35f, 0f, 10f)
                : Quaternion.Euler(-30f, -6f, 7f);
            leftLegTarget = Quaternion.identity;
            rightLegTarget = Quaternion.identity;
            rifleTarget = Quaternion.Euler(2f, 0f, 4f);
            riflePosition = new Vector3(0.18f, 1.00f, 0.25f);
        }

        float blend = Mathf.Clamp01(Time.deltaTime * 10f);
        if (rig.LeftArm != null)
            rig.LeftArm.localRotation = Quaternion.Slerp(rig.LeftArm.localRotation, leftArmTarget, blend);
        if (rig.RightArm != null)
            rig.RightArm.localRotation = Quaternion.Slerp(rig.RightArm.localRotation, rightArmTarget, blend);
        if (rig.LeftLeg != null)
            rig.LeftLeg.localRotation = Quaternion.Slerp(rig.LeftLeg.localRotation, leftLegTarget, blend);
        if (rig.RightLeg != null)
            rig.RightLeg.localRotation = Quaternion.Slerp(rig.RightLeg.localRotation, rightLegTarget, blend);
        if (rig.RifleRoot != null && rig.RifleRoot.gameObject.activeSelf)
        {
            rig.RifleRoot.localRotation = Quaternion.Slerp(rig.RifleRoot.localRotation, rifleTarget, blend);
            rig.RifleRoot.localPosition = Vector3.Lerp(rig.RifleRoot.localPosition, riflePosition, blend);
        }
    }

    private SoldierRig BuildSoldier(
        Regiment regiment,
        Transform soldier,
        int index,
        bool officer,
        bool standardBearer,
        UnitVisual visual)
    {
        Transform existing = soldier.Find("Visual09I");
        if (existing != null)
            Object.Destroy(existing.gameObject);

        GameObject rootObject = new GameObject("Visual09I");
        rootObject.transform.SetParent(soldier, false);
        Transform root = rootObject.transform;

        int variant = index % 6;
        float heightScale = 1.03f + variant * 0.015f;
        Vector3 baseScale = new Vector3(
            1.08f + (variant % 3) * 0.014f,
            heightScale,
            1.08f + ((variant + 1) % 3) * 0.012f);
        root.localScale = baseScale;

        CreateTaperedBox(
            root,
            "Coat_Torso",
            new Vector3(0f, 0.94f, 0f),
            0.58f,
            0.43f,
            0.35f,
            0.24f,
            0.27f,
            visual.Coat);

        CreateTaperedBox(
            root,
            "Coat_Skirts",
            new Vector3(0f, 0.62f, 0f),
            0.34f,
            0.34f,
            0.45f,
            0.25f,
            0.31f,
            visual.CoatShadow);

        CreatePrimitivePart(
            root,
            PrimitiveType.Cube,
            "Waist_Belt",
            new Vector3(0f, 0.74f, 0.005f),
            new Vector3(0.39f, 0.06f, 0.29f),
            Quaternion.identity,
            visual.Straps);

        Transform leftLeg = CreateLimb(
            root,
            "Leg_Left",
            new Vector3(-0.112f, 0.52f, 0f),
            0.49f,
            0.078f,
            visual.Trousers);
        Transform rightLeg = CreateLimb(
            root,
            "Leg_Right",
            new Vector3(0.112f, 0.52f, 0f),
            0.49f,
            0.078f,
            visual.Trousers);

        CreatePrimitivePart(
            leftLeg,
            PrimitiveType.Cube,
            "Boot_Left",
            new Vector3(0f, -0.50f, 0.06f),
            new Vector3(0.14f, 0.21f, 0.22f),
            Quaternion.identity,
            visual.Equipment);
        CreatePrimitivePart(
            rightLeg,
            PrimitiveType.Cube,
            "Boot_Right",
            new Vector3(0f, -0.50f, 0.06f),
            new Vector3(0.14f, 0.21f, 0.22f),
            Quaternion.identity,
            visual.Equipment);

        Transform leftArm = CreateLimb(
            root,
            "Arm_Left",
            new Vector3(-0.285f, 1.13f, 0f),
            0.43f,
            0.067f,
            visual.Coat);
        Transform rightArm = CreateLimb(
            root,
            "Arm_Right",
            new Vector3(0.285f, 1.13f, 0f),
            0.43f,
            0.067f,
            visual.Coat);

        CreatePrimitivePart(
            leftArm,
            PrimitiveType.Sphere,
            "Hand_Left",
            new Vector3(0f, -0.43f, 0f),
            new Vector3(0.09f, 0.09f, 0.09f),
            Quaternion.identity,
            visual.Skin);
        CreatePrimitivePart(
            rightArm,
            PrimitiveType.Sphere,
            "Hand_Right",
            new Vector3(0f, -0.43f, 0f),
            new Vector3(0.09f, 0.09f, 0.09f),
            Quaternion.identity,
            visual.Skin);

        CreatePrimitivePart(
            root,
            PrimitiveType.Cylinder,
            "Neck",
            new Vector3(0f, 1.28f, 0f),
            new Vector3(0.08f, 0.075f, 0.08f),
            Quaternion.identity,
            visual.Skin);

        CreatePrimitivePart(
            root,
            PrimitiveType.Sphere,
            "Head",
            new Vector3(0f, 1.44f, 0.02f),
            new Vector3(0.19f, 0.215f, 0.18f),
            Quaternion.identity,
            visual.Skin);

        BuildHeadgear(regiment.Team, root, variant, visual);

        GameObject detailObject = new GameObject("CloseDetails");
        detailObject.transform.SetParent(root, false);
        Transform detailRoot = detailObject.transform;

        CreatePrimitivePart(
            detailRoot,
            PrimitiveType.Cube,
            "Pack",
            new Vector3(0f, 0.93f, -0.20f),
            new Vector3(0.31f + (variant % 2) * 0.028f, 0.36f, 0.13f),
            Quaternion.identity,
            visual.Equipment);

        CreatePrimitivePart(
            detailRoot,
            PrimitiveType.Cube,
            "CrossStrap_A",
            new Vector3(-0.038f, 0.98f, 0.15f),
            new Vector3(0.06f, 0.60f, 0.028f),
            Quaternion.Euler(0f, 0f, -24f),
            visual.Straps);
        CreatePrimitivePart(
            detailRoot,
            PrimitiveType.Cube,
            "CrossStrap_B",
            new Vector3(0.038f, 0.98f, 0.155f),
            new Vector3(0.05f, 0.54f, 0.026f),
            Quaternion.Euler(0f, 0f, 25f),
            visual.Straps);

        CreatePrimitivePart(
            detailRoot,
            PrimitiveType.Cube,
            "CartridgeBox",
            new Vector3(0.225f, 0.67f, 0.10f),
            new Vector3(0.19f, 0.17f, 0.09f),
            Quaternion.identity,
            visual.Equipment);

        if (variant == 2 || variant == 5)
        {
            CreatePrimitivePart(
                detailRoot,
                PrimitiveType.Cylinder,
                "Canteen",
                new Vector3(-0.25f, 0.66f, 0.03f),
                new Vector3(0.085f, 0.05f, 0.085f),
                Quaternion.Euler(90f, 0f, 0f),
                visual.Metal);
        }

        Transform rifleRoot = BuildRifle(root, visual);

        if (officer)
            AddOfficerVisuals(root, detailRoot, visual);
        if (standardBearer)
            AddStandardBearerVisuals(root, detailRoot, visual, rifleRoot);

        return new SoldierRig
        {
            Root = root,
            DetailRoot = detailRoot,
            LeftArm = leftArm,
            RightArm = rightArm,
            LeftLeg = leftLeg,
            RightLeg = rightLeg,
            RifleRoot = rifleRoot,
            BaseScale = baseScale,
            Phase = index * 0.73f,
            IsOfficer = officer,
            IsStandardBearer = standardBearer
        };
    }

    private static void BuildHeadgear(
        BattleTeam team,
        Transform root,
        int variant,
        UnitVisual visual)
    {
        if (team == BattleTeam.Prussia)
        {
            CreatePrimitivePart(
                root,
                PrimitiveType.Sphere,
                "Pickelhaube_Dome",
                new Vector3(0f, 1.62f, 0f),
                new Vector3(0.215f, 0.145f, 0.21f),
                Quaternion.identity,
                visual.Headgear);
            CreatePrimitivePart(
                root,
                PrimitiveType.Cylinder,
                "Pickelhaube_Band",
                new Vector3(0f, 1.56f, 0f),
                new Vector3(0.205f, 0.04f, 0.205f),
                Quaternion.identity,
                visual.Headgear);
            CreatePrimitivePart(
                root,
                PrimitiveType.Cylinder,
                "Pickelhaube_Spike",
                new Vector3(0f, 1.81f, 0f),
                new Vector3(0.04f, 0.14f + variant * 0.002f, 0.04f),
                Quaternion.identity,
                visual.Metal);
            CreatePrimitivePart(
                root,
                PrimitiveType.Cube,
                "Pickelhaube_Visor",
                new Vector3(0f, 1.57f, 0.17f),
                new Vector3(0.27f, 0.03f, 0.13f),
                Quaternion.Euler(-8f, 0f, 0f),
                visual.Headgear);
        }
        else
        {
            CreatePrimitivePart(
                root,
                PrimitiveType.Cylinder,
                "DanishCap_Crown",
                new Vector3(0f, 1.62f, 0f),
                new Vector3(0.21f, 0.085f, 0.21f),
                Quaternion.identity,
                visual.Headgear);
            CreatePrimitivePart(
                root,
                PrimitiveType.Cube,
                "DanishCap_Visor",
                new Vector3(0f, 1.57f, 0.17f),
                new Vector3(0.27f, 0.03f, 0.14f),
                Quaternion.Euler(-8f, 0f, 0f),
                visual.Headgear);
            CreatePrimitivePart(
                root,
                PrimitiveType.Cube,
                "DanishCap_Band",
                new Vector3(0f, 1.58f, 0.095f),
                new Vector3(0.34f, 0.05f, 0.05f),
                Quaternion.identity,
                visual.Trim);
        }
    }

    private static Transform BuildRifle(Transform root, UnitVisual visual)
    {
        GameObject rifleRootObject = new GameObject("RifleRig");
        rifleRootObject.transform.SetParent(root, false);
        Transform rifleRoot = rifleRootObject.transform;
        rifleRoot.localPosition = new Vector3(0.18f, 1.00f, 0.25f);

        CreatePrimitivePart(
            rifleRoot,
            PrimitiveType.Cube,
            "Rifle_Stock",
            new Vector3(0f, 0f, 0.10f),
            new Vector3(0.11f, 0.12f, 0.38f),
            Quaternion.identity,
            visual.Wood);
        CreatePrimitivePart(
            rifleRoot,
            PrimitiveType.Cube,
            "Rifle_Barrel",
            new Vector3(0f, 0.02f, 0.64f),
            new Vector3(0.034f, 0.034f, 0.84f),
            Quaternion.identity,
            visual.Metal);
        CreatePrimitivePart(
            rifleRoot,
            PrimitiveType.Cube,
            "Rifle_Bayonet",
            new Vector3(0f, 0.02f, 1.16f),
            new Vector3(0.017f, 0.017f, 0.27f),
            Quaternion.identity,
            visual.Metal);

        return rifleRoot;
    }

    private static void AddOfficerVisuals(
        Transform root,
        Transform detailRoot,
        UnitVisual visual)
    {
        CreatePrimitivePart(
            detailRoot,
            PrimitiveType.Cube,
            "Officer_Sash",
            new Vector3(0f, 0.96f, 0.165f),
            new Vector3(0.065f, 0.62f, 0.03f),
            Quaternion.Euler(0f, 0f, -30f),
            visual.Trim);
        CreatePrimitivePart(
            detailRoot,
            PrimitiveType.Cube,
            "Officer_ShoulderBoard_L",
            new Vector3(-0.23f, 1.20f, 0.025f),
            new Vector3(0.15f, 0.04f, 0.11f),
            Quaternion.identity,
            visual.Metal);
        CreatePrimitivePart(
            detailRoot,
            PrimitiveType.Cube,
            "Officer_ShoulderBoard_R",
            new Vector3(0.23f, 1.20f, 0.025f),
            new Vector3(0.15f, 0.04f, 0.11f),
            Quaternion.identity,
            visual.Metal);

        CreatePrimitivePart(
            root,
            PrimitiveType.Cube,
            "Officer_Sword",
            new Vector3(0.34f, 0.60f, 0.03f),
            new Vector3(0.025f, 0.70f, 0.025f),
            Quaternion.Euler(0f, 0f, -8f),
            visual.Metal);
    }

    private static void AddStandardBearerVisuals(
        Transform root,
        Transform detailRoot,
        UnitVisual visual,
        Transform rifleRoot)
    {
        if (rifleRoot != null)
            rifleRoot.gameObject.SetActive(false);

        CreatePrimitivePart(
            detailRoot,
            PrimitiveType.Cube,
            "StandardBearer_Sash",
            new Vector3(0f, 0.97f, 0.165f),
            new Vector3(0.07f, 0.64f, 0.03f),
            Quaternion.Euler(0f, 0f, 31f),
            visual.Trim);

        CreatePrimitivePart(
            root,
            PrimitiveType.Cylinder,
            "StandardBearer_PoleGrip",
            new Vector3(0.30f, 0.98f, 0.05f),
            new Vector3(0.045f, 0.20f, 0.045f),
            Quaternion.Euler(0f, 0f, 6f),
            visual.Wood);
    }

    private static Transform CreateLimb(
        Transform parent,
        string name,
        Vector3 position,
        float length,
        float radius,
        Material material)
    {
        GameObject pivotObject = new GameObject(name + "_Pivot");
        pivotObject.transform.SetParent(parent, false);
        pivotObject.transform.localPosition = position;
        Transform pivot = pivotObject.transform;

        CreatePrimitivePart(
            pivot,
            PrimitiveType.Capsule,
            name,
            new Vector3(0f, -length * 0.5f, 0f),
            new Vector3(radius, length * 0.5f, radius),
            Quaternion.identity,
            material);

        return pivot;
    }

    private static void CreateTaperedBox(
        Transform parent,
        string name,
        Vector3 center,
        float height,
        float topWidth,
        float bottomWidth,
        float topDepth,
        float bottomDepth,
        Material material)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = center;

        MeshFilter filter = obj.AddComponent<MeshFilter>();
        MeshRenderer renderer = obj.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;

        float hh = height * 0.5f;
        float tw = topWidth * 0.5f;
        float bw = bottomWidth * 0.5f;
        float td = topDepth * 0.5f;
        float bd = bottomDepth * 0.5f;

        Vector3[] vertices =
        {
            new Vector3(-bw, -hh, -bd),
            new Vector3( bw, -hh, -bd),
            new Vector3( bw, -hh,  bd),
            new Vector3(-bw, -hh,  bd),
            new Vector3(-tw,  hh, -td),
            new Vector3( tw,  hh, -td),
            new Vector3( tw,  hh,  td),
            new Vector3(-tw,  hh,  td)
        };

        int[] triangles =
        {
            0, 2, 1, 0, 3, 2,
            4, 5, 6, 4, 6, 7,
            0, 1, 5, 0, 5, 4,
            1, 2, 6, 1, 6, 5,
            2, 3, 7, 2, 7, 6,
            3, 0, 4, 3, 4, 7
        };

        Mesh mesh = new Mesh();
        mesh.name = name + "Mesh";
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        filter.sharedMesh = mesh;
    }

    private static GameObject CreatePrimitivePart(
        Transform parent,
        PrimitiveType type,
        string name,
        Vector3 localPosition,
        Vector3 localScale,
        Quaternion localRotation,
        Material material)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        part.transform.localRotation = localRotation;
        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = material;
        Collider collider = part.GetComponent<Collider>();
        if (collider != null)
            Object.Destroy(collider);
        return part;
    }

    private static void HideLegacySoldierRenderers(Transform soldier)
    {
        if (soldier == null)
            return;

        Renderer[] renderers = soldier.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;

            // Never hide the new rig if Play mode is rebuilding after a script reload.
            if (renderer.transform.IsChildOf(soldier) &&
                renderer.transform.root == soldier.root)
            {
                Transform cursor = renderer.transform;
                bool insideNewRig = false;
                while (cursor != null && cursor != soldier)
                {
                    if (cursor.name == "Visual09I")
                    {
                        insideNewRig = true;
                        break;
                    }
                    cursor = cursor.parent;
                }

                if (!insideNewRig)
                    renderer.enabled = false;
            }
        }
    }

    private static PrototypeUniformProfile09H GetCurrentProfile(Regiment regiment)
    {
        PrototypeSoldierVisualPass09H oldPass = PrototypeSoldierVisualPass09H.Instance;
        if (oldPass != null)
        {
            PrototypeUniformProfile09H profile = oldPass.GetProfile(regiment);
            if (profile != null)
                return profile;
        }

        return regiment.Team == BattleTeam.Denmark
            ? PrototypeUniformProfile09H.CreateDenmarkDefault()
            : PrototypeUniformProfile09H.CreatePrussiaDefault();
    }

    private static void CreateMaterials(Regiment regiment, UnitVisual visual)
    {
        PrototypeUniformProfile09H profile = visual.Profile;
        visual.Coat = PrototypeBootstrap.CreateSharedMaterial(profile.CoatColor, "09I_Coat_" + regiment.RegimentName);
        visual.CoatShadow = PrototypeBootstrap.CreateSharedMaterial(profile.CoatColor * 0.78f, "09I_CoatShadow_" + regiment.RegimentName);
        visual.Trousers = PrototypeBootstrap.CreateSharedMaterial(profile.TrouserColor, "09I_Trousers_" + regiment.RegimentName);
        visual.Headgear = PrototypeBootstrap.CreateSharedMaterial(profile.HeadgearColor, "09I_Headgear_" + regiment.RegimentName);
        visual.Trim = PrototypeBootstrap.CreateSharedMaterial(profile.TrimColor, "09I_Trim_" + regiment.RegimentName);
        visual.Straps = PrototypeBootstrap.CreateSharedMaterial(profile.StrapColor, "09I_Straps_" + regiment.RegimentName);
        visual.Equipment = PrototypeBootstrap.CreateSharedMaterial(profile.EquipmentColor, "09I_Equipment_" + regiment.RegimentName);
        visual.Skin = PrototypeBootstrap.CreateSharedMaterial(profile.SkinColor, "09I_Skin_" + regiment.RegimentName);
        visual.Wood = PrototypeBootstrap.CreateSharedMaterial(new Color(0.21f, 0.10f, 0.035f), "09I_RifleWood_" + regiment.RegimentName);
        visual.Metal = PrototypeBootstrap.CreateSharedMaterial(new Color(0.16f, 0.17f, 0.18f), "09I_Metal_" + regiment.RegimentName);
    }

    private static void ApplyProfileColors(UnitVisual visual)
    {
        PrototypeUniformProfile09H profile = visual.Profile;
        if (profile == null)
            return;

        if (visual.Coat != null)
            visual.Coat.color = profile.CoatColor;
        if (visual.CoatShadow != null)
            visual.CoatShadow.color = profile.CoatColor * 0.78f;
        if (visual.Trousers != null)
            visual.Trousers.color = profile.TrouserColor;
        if (visual.Headgear != null)
            visual.Headgear.color = profile.HeadgearColor;
        if (visual.Trim != null)
            visual.Trim.color = profile.TrimColor;
        if (visual.Straps != null)
            visual.Straps.color = profile.StrapColor;
        if (visual.Equipment != null)
            visual.Equipment.color = profile.EquipmentColor;
        if (visual.Skin != null)
            visual.Skin.color = profile.SkinColor;
    }

    private void CleanupDestroyedRegiments()
    {
        if (units.Count == 0)
            return;

        List<Regiment> stale = null;
        foreach (KeyValuePair<Regiment, UnitVisual> pair in units)
        {
            if (pair.Key != null)
                continue;
            if (stale == null)
                stale = new List<Regiment>();
            stale.Add(pair.Key);
        }

        if (stale == null)
            return;
        for (int i = 0; i < stale.Count; i++)
            units.Remove(stale[i]);
    }
}
