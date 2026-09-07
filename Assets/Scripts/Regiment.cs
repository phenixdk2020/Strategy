using System.Collections.Generic;
using UnityEngine;

public enum BattleTeam
{
    Denmark,
    Prussia
}

public enum RegimentFormation
{
    Line,
    Column
}

public enum InfantryWeaponType
{
    RifledMuzzleLoader,
    DreyseNeedleRifle
}

public enum RegimentFirePolicy
{
    HoldFire,
    CloseRange,
    MediumRange,
    LongRange
}

public sealed class Regiment : MonoBehaviour
{
    public string RegimentName { get; private set; }
    public BattleTeam Team { get; private set; }
    public int InitialStrength { get; private set; }
    public int CurrentStrength { get; private set; }
    public float Morale { get; private set; } = 100f;
    public float Cohesion { get; private set; } = 100f;
    public float Experience { get; private set; } = 50f;
    public bool IsRouted { get; private set; }
    public bool IsSelected { get; private set; }
    public RegimentFormation Formation { get; private set; } = RegimentFormation.Line;
    public InfantryWeaponType WeaponType { get; private set; }
    public string WeaponName { get; private set; }
    public string WeaponShortName { get; private set; }
    public float BaseReloadSeconds { get; private set; }
    public float CurrentReloadSeconds => BaseReloadSeconds * GetExperienceReloadMultiplier();
    public float EffectiveRange { get; private set; }
    public float MaximumRange { get; private set; }
    public float CloseRange => EffectiveRange * 0.50f;
    public float FireArcHalfAngle => 60f;
    public RegimentFirePolicy FirePolicy { get; private set; } = RegimentFirePolicy.MediumRange;
    public int LastVolleyHits { get; private set; }
    public bool HasHitFeedback => LastVolleyHits > 0 && Time.unscaledTime < hitFeedbackUntil;
    public bool ShowRange { get; set; } = true;
    public bool IsAI { get; private set; }

    private readonly List<Transform> soldierModels = new List<Transform>();
    private Vector3 destination;
    private bool hasDestination;
    private float moveSpeed;
    private float baseAccuracy;
    private float nextFireTime;
    private float underFireTimer;
    private float aiThinkTimer;
    private float hitFeedbackUntil;
    private Regiment forcedTarget;
    private Regiment aiTarget;
    private bool hasAiWaypoint;
    private Vector3 aiWaypoint;
    private GameObject selectionMarker;
    private LineRenderer closeRangeFan;
    private LineRenderer mediumRangeFan;
    private LineRenderer longRangeFan;
    private ParticleSystem smoke;
    private Material uniformMaterial;
    private Material darkMaterial;

    public void Initialize(string regimentName, BattleTeam team, int strength, bool isAI, Vector3 startPosition, Vector3? initialAiWaypoint = null, float? experience = null)
    {
        RegimentName = regimentName;
        Team = team;
        InitialStrength = strength;
        CurrentStrength = strength;
        IsAI = isAI;
        Experience = Mathf.Clamp(experience ?? GetPrototypeExperience(regimentName), 0f, 100f);
        transform.position = startPosition;

        if (team == BattleTeam.Denmark)
        {
            ApplyWeaponProfile(InfantryWeaponType.RifledMuzzleLoader);
            moveSpeed = 3.2f;
        }
        else
        {
            ApplyWeaponProfile(InfantryWeaponType.DreyseNeedleRifle);
            moveSpeed = 3.35f;
        }

        if (initialAiWaypoint.HasValue)
        {
            hasAiWaypoint = true;
            aiWaypoint = initialAiWaypoint.Value;
        }

        uniformMaterial = PrototypeBootstrap.CreateSharedMaterial(
            team == BattleTeam.Denmark ? new Color(0.10f, 0.20f, 0.34f) : new Color(0.12f, 0.12f, 0.14f),
            "UnitUniform");
        darkMaterial = PrototypeBootstrap.CreateSharedMaterial(new Color(0.055f, 0.045f, 0.035f), "UnitEquipment");

        CreateVisuals();
        BattleManager.Instance.Register(this);
    }

    private void ApplyWeaponProfile(InfantryWeaponType weaponType)
    {
        WeaponType = weaponType;

        switch (weaponType)
        {
            case InfantryWeaponType.DreyseNeedleRifle:
                WeaponName = "Dreyse needle rifle";
                WeaponShortName = "Dreyse";
                EffectiveRange = 37f;
                MaximumRange = 49f;
                BaseReloadSeconds = 3.6f;
                baseAccuracy = 0.013f;
                break;

            default:
                WeaponName = "Rifled muzzle-loader";
                WeaponShortName = "Rifled ML";
                EffectiveRange = 43f;
                MaximumRange = 55f;
                BaseReloadSeconds = 5.0f;
                baseAccuracy = 0.014f;
                break;
        }
    }

    private static float GetPrototypeExperience(string regimentName)
    {
        // Prototype QA values only. These are deliberately varied so reload scaling
        // can be verified before experience is moved to historical/OOB data.
        switch (regimentName)
        {
            case "1. Regiment":
                return 55f;
            case "5. Regiment":
                return 42f;
            case "8th Regiment":
                return 65f;
            case "18th Regiment":
                return 50f;
            default:
                return 50f;
        }
    }

    private float GetExperienceReloadMultiplier()
    {
        // Experience 50 is neutral. The full 0-100 range is bounded to +20%/-20%
        // reload time so weapon technology remains the dominant cadence factor.
        return Mathf.Lerp(1.20f, 0.80f, Experience / 100f);
    }

    private void CreateVisuals()
    {
        int visualCount = Mathf.CeilToInt(InitialStrength / 10f);
        for (int i = 0; i < visualCount; i++)
        {
            GameObject soldier = new GameObject("Soldier_" + (i + 1));
            soldier.transform.SetParent(transform, false);

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.transform.SetParent(soldier.transform, false);
            body.transform.localScale = new Vector3(0.28f, 0.48f, 0.28f);
            body.transform.localPosition = new Vector3(0f, 0.68f, 0f);
            body.GetComponent<Renderer>().sharedMaterial = uniformMaterial;
            Destroy(body.GetComponent<Collider>());

            GameObject rifle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rifle.transform.SetParent(soldier.transform, false);
            rifle.transform.localScale = new Vector3(0.07f, 0.07f, 0.85f);
            rifle.transform.localPosition = new Vector3(0.22f, 0.75f, 0.20f);
            rifle.transform.localRotation = Quaternion.Euler(10f, 0f, 6f);
            rifle.GetComponent<Renderer>().sharedMaterial = darkMaterial;
            Destroy(rifle.GetComponent<Collider>());

            GameObject headgear = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            headgear.transform.SetParent(soldier.transform, false);
            headgear.transform.localScale = new Vector3(0.18f, 0.08f, 0.18f);
            headgear.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            headgear.GetComponent<Renderer>().sharedMaterial = darkMaterial;
            Destroy(headgear.GetComponent<Collider>());

            soldierModels.Add(soldier.transform);
        }

        BoxCollider box = gameObject.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 0.9f, 0f);
        box.size = new Vector3(19f, 2.2f, 5f);

        selectionMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        selectionMarker.transform.SetParent(transform, false);
        selectionMarker.transform.localPosition = new Vector3(0f, 0.08f, 0f);
        selectionMarker.transform.localScale = new Vector3(4.6f, 0.02f, 4.6f);
        selectionMarker.GetComponent<Renderer>().sharedMaterial = PrototypeBootstrap.CreateSharedMaterial(new Color(0.95f, 0.78f, 0.18f), "Selection");
        Destroy(selectionMarker.GetComponent<Collider>());
        selectionMarker.SetActive(false);

        CreateRangeFans();

        GameObject psObject = new GameObject("BlackPowderSmoke");
        psObject.transform.SetParent(transform, false);
        psObject.transform.localPosition = new Vector3(0f, 1.1f, 1.2f);
        smoke = psObject.AddComponent<ParticleSystem>();
        var main = smoke.main;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = 3.2f;
        main.startSpeed = 1.1f;
        main.startSize = 1.8f;
        main.startColor = new Color(0.88f, 0.88f, 0.86f, 0.7f);
        main.maxParticles = 300;
        var emission = smoke.emission;
        emission.enabled = false;
        var shape = smoke.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(12f, 0.4f, 1f);
        var renderer = smoke.GetComponent<ParticleSystemRenderer>();
        Shader particleShader = Shader.Find("Particles/Standard Unlit");
        if (particleShader == null)
            particleShader = Shader.Find("Unlit/Color");
        renderer.material = new Material(particleShader);

        RefreshVisualStrength();
    }

    private void CreateRangeFans()
    {
        closeRangeFan = CreateRangeFan(
            "CloseRangeFan",
            CloseRange,
            0.08f,
            new Color(0.95f, 0.78f, 0.18f));

        mediumRangeFan = CreateRangeFan(
            "MediumRangeFan",
            EffectiveRange,
            0.11f,
            new Color(0.95f, 0.64f, 0.12f));

        longRangeFan = CreateRangeFan(
            "LongRangeFan",
            MaximumRange,
            0.17f,
            new Color(0.95f, 0.42f, 0.10f));

        SetRangeVisualEnabled(false);
    }

    private LineRenderer CreateRangeFan(string objectName, float range, float width, Color color)
    {
        GameObject fanObject = new GameObject(objectName);
        fanObject.transform.SetParent(transform, false);

        LineRenderer line = fanObject.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.widthMultiplier = width;
        line.sharedMaterial = PrototypeBootstrap.CreateSharedMaterial(color, objectName + "Material");

        const int arcSegments = 28;
        const float muzzleHalfWidth = 8.5f;
        const float muzzleZ = 1.30f;
        line.positionCount = arcSegments + 3;

        line.SetPosition(0, new Vector3(-muzzleHalfWidth, 0.15f, muzzleZ));

        for (int i = 0; i <= arcSegments; i++)
        {
            float t = i / (float)arcSegments;
            float angle = Mathf.Lerp(-FireArcHalfAngle, FireArcHalfAngle, t) * Mathf.Deg2Rad;
            float x = Mathf.Sin(angle) * range;
            float z = Mathf.Cos(angle) * range;
            line.SetPosition(i + 1, new Vector3(x, 0.15f, z));
        }

        line.SetPosition(line.positionCount - 1, new Vector3(muzzleHalfWidth, 0.15f, muzzleZ));
        line.enabled = false;
        return line;
    }

    private void Update()
    {
        if (IsRouted)
        {
            UpdateMovement();
            return;
        }

        if (underFireTimer > 0f)
            underFireTimer -= Time.deltaTime;
        else
        {
            Morale = Mathf.Min(100f, Morale + 0.7f * Time.deltaTime);
            Cohesion = Mathf.Min(100f, Cohesion + 1.0f * Time.deltaTime);
        }

        if (IsAI)
            UpdateAI();

        if (forcedTarget != null && !forcedTarget.IsRouted)
        {
            float distance = Vector3.Distance(transform.position, forcedTarget.transform.position);
            float stopRange = GetAttackStopRange();

            if (distance > stopRange)
            {
                destination = forcedTarget.transform.position;
                hasDestination = true;
            }
            else
            {
                hasDestination = false;
                FaceTarget(forcedTarget, 3.0f);
            }
        }

        UpdateMovement();
        UpdateSoldierFormation();

        if (!hasDestination && Time.time >= nextFireTime && FirePolicy != RegimentFirePolicy.HoldFire)
        {
            Regiment target = null;

            if (forcedTarget != null && !forcedTarget.IsRouted && CanFireAt(forcedTarget))
                target = forcedTarget;
            else
                target = FindNearestEnemyInFireArc(GetFireTriggerRange());

            if (target != null)
                FireVolley(target);
        }
    }

    private void UpdateAI()
    {
        // Legacy v00.00.08 AI. v00.00.09 OfficerAIPrototypeManager disables this
        // path at runtime so both teams use OfficerAIController instead.
        aiThinkTimer -= Time.deltaTime;
        if (aiThinkTimer > 0f)
            return;
        aiThinkTimer = Random.Range(0.7f, 1.2f);

        if (hasAiWaypoint)
        {
            if (Vector3.Distance(transform.position, aiWaypoint) > 4f)
            {
                OrderMove(aiWaypoint);
                return;
            }
            hasAiWaypoint = false;
        }

        if (aiTarget == null || aiTarget.IsRouted)
            aiTarget = FindNearestEnemy(999f);

        if (aiTarget == null)
            return;

        float distance = Vector3.Distance(transform.position, aiTarget.transform.position);
        if (distance > EffectiveRange * 0.86f)
            OrderAttack(aiTarget);
        else
        {
            hasDestination = false;
            forcedTarget = aiTarget;
        }
    }

    private void UpdateMovement()
    {
        if (!hasDestination)
            return;

        Vector3 here = transform.position;
        Vector3 target = destination;
        target.y = PrototypeBootstrap.SampleGroundHeight(target.x, target.z) + 0.10f;
        Vector3 planar = target - here;
        planar.y = 0f;

        if (planar.magnitude < 0.45f)
        {
            hasDestination = false;
            return;
        }

        float fatiguePenalty = Mathf.Lerp(0.72f, 1f, Cohesion / 100f);
        Vector3 step = planar.normalized * moveSpeed * fatiguePenalty * Time.deltaTime;
        if (step.magnitude > planar.magnitude)
            step = planar;

        Vector3 next = here + step;
        next.y = PrototypeBootstrap.SampleGroundHeight(next.x, next.z) + 0.10f;
        transform.position = next;

        if (planar.sqrMagnitude > 0.05f)
        {
            Quaternion facing = Quaternion.LookRotation(planar.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, facing, 4f * Time.deltaTime);
        }

        Cohesion = Mathf.Max(35f, Cohesion - 0.18f * Time.deltaTime);
    }

    private void UpdateSoldierFormation()
    {
        for (int i = 0; i < soldierModels.Count; i++)
        {
            Transform soldier = soldierModels[i];
            if (!soldier.gameObject.activeSelf)
                continue;

            Vector3 target = GetFormationPosition(i, soldierModels.Count);
            soldier.localPosition = Vector3.Lerp(soldier.localPosition, target, 7f * Time.deltaTime);
            soldier.localRotation = Quaternion.Slerp(soldier.localRotation, Quaternion.identity, 7f * Time.deltaTime);
        }
    }

    private Vector3 GetFormationPosition(int index, int total)
    {
        if (Formation == RegimentFormation.Line)
        {
            int ranks = 3;
            int columns = Mathf.CeilToInt(total / (float)ranks);
            int rank = index / columns;
            int column = index % columns;
            float x = (column - (columns - 1) * 0.5f) * 0.75f;
            float z = (rank - 1f) * -0.90f;
            return new Vector3(x, 0f, z);
        }

        int columnCount = 4;
        int columnRank = index / columnCount;
        int columnIndex = index % columnCount;
        return new Vector3((columnIndex - 1.5f) * 0.78f, 0f, -columnRank * 0.78f);
    }

    private Regiment FindNearestEnemy(float maxDistance)
    {
        Regiment nearest = null;
        float best = maxDistance;

        foreach (Regiment candidate in BattleManager.Instance.Regiments)
        {
            if (candidate == null || candidate.Team == Team || candidate.IsRouted)
                continue;

            float distance = Vector3.Distance(transform.position, candidate.transform.position);
            if (distance < best)
            {
                best = distance;
                nearest = candidate;
            }
        }

        return nearest;
    }

    private Regiment FindNearestEnemyInFireArc(float maxDistance)
    {
        Regiment nearest = null;
        float best = maxDistance;

        foreach (Regiment candidate in BattleManager.Instance.Regiments)
        {
            if (candidate == null || candidate.Team == Team || candidate.IsRouted)
                continue;

            float distance = Vector3.Distance(transform.position, candidate.transform.position);
            if (distance > best || !IsTargetInFireArc(candidate))
                continue;

            best = distance;
            nearest = candidate;
        }

        return nearest;
    }

    public float GetFireTriggerRange()
    {
        switch (FirePolicy)
        {
            case RegimentFirePolicy.HoldFire:
                return 0f;
            case RegimentFirePolicy.CloseRange:
                return CloseRange;
            case RegimentFirePolicy.LongRange:
                return MaximumRange;
            default:
                return EffectiveRange;
        }
    }

    public string GetFirePolicyLabel()
    {
        switch (FirePolicy)
        {
            case RegimentFirePolicy.HoldFire:
                return "HOLD";
            case RegimentFirePolicy.CloseRange:
                return "CLOSE";
            case RegimentFirePolicy.LongRange:
                return "LONG";
            default:
                return "MEDIUM";
        }
    }

    public bool IsTargetInFireArc(Regiment target)
    {
        if (target == null)
            return false;

        Vector3 toTarget = target.transform.position - transform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude < 0.01f)
            return true;

        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.01f)
            return true;

        return Vector3.Angle(forward.normalized, toTarget.normalized) <= FireArcHalfAngle;
    }

    public bool CanFireAt(Regiment target)
    {
        if (target == null || target.Team == Team || target.IsRouted || FirePolicy == RegimentFirePolicy.HoldFire)
            return false;

        float distance = Vector3.Distance(transform.position, target.transform.position);
        return distance <= GetFireTriggerRange() && IsTargetInFireArc(target);
    }

    public void SetFirePolicy(RegimentFirePolicy policy)
    {
        FirePolicy = policy;
    }

    private float GetAttackStopRange()
    {
        if (FirePolicy == RegimentFirePolicy.HoldFire)
            return EffectiveRange * 0.70f;

        return Mathf.Max(4f, GetFireTriggerRange() * 0.92f);
    }

    private float GetRangeAccuracyMultiplier(float distance)
    {
        // Continuous distance curve: no artificial accuracy jump at the Close/Medium/Long
        // UI boundaries. Closer targets are progressively easier to hit.
        float distance01 = Mathf.Clamp01(distance / Mathf.Max(1f, MaximumRange));
        return Mathf.Lerp(1.35f, 0.16f, Mathf.Pow(distance01, 0.85f));
    }

    private void FireVolley(Regiment target)
    {
        if (!CanFireAt(target))
            return;

        float distance = Vector3.Distance(transform.position, target.transform.position);
        FaceTarget(target, 0.75f);

        float rangeAccuracy = GetRangeAccuracyMultiplier(distance);
        float quality = (Morale / 100f) * (Cohesion / 100f);
        int firingMen = Mathf.RoundToInt(CurrentStrength * 0.58f);
        float expected = firingMen * baseAccuracy * rangeAccuracy * quality;
        int hits = Mathf.Clamp(Mathf.RoundToInt(expected * Random.Range(0.72f, 1.28f)), 0, 16);
        float shock = Mathf.Lerp(5.0f, 1.5f, Mathf.Clamp01(distance / MaximumRange));

        target.ReceiveVolley(hits, shock, this);
        smoke.Emit(Random.Range(18, 34));
        nextFireTime = Time.time + CurrentReloadSeconds * Random.Range(0.90f, 1.12f);
        Cohesion = Mathf.Max(30f, Cohesion - Random.Range(0.4f, 1.2f));
    }

    private void FaceTarget(Regiment target, float turnRate)
    {
        if (target == null)
            return;

        Vector3 toTarget = target.transform.position - transform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude <= 0.01f)
            return;

        Quaternion desired = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, desired, turnRate * Time.deltaTime);
    }

    public void ReceiveVolley(int hits, float shock, Regiment attacker)
    {
        int resolvedHits = Mathf.Clamp(hits, 0, CurrentStrength);
        CurrentStrength -= resolvedHits;
        Morale = Mathf.Max(0f, Morale - resolvedHits * 0.32f - shock);
        Cohesion = Mathf.Max(0f, Cohesion - resolvedHits * 0.25f - shock * 0.7f);
        underFireTimer = 7f;

        LastVolleyHits = resolvedHits;
        hitFeedbackUntil = resolvedHits > 0 ? Time.unscaledTime + 1.45f : 0f;

        RefreshVisualStrength();

        if (CurrentStrength <= 0 || Morale <= 17f || CurrentStrength <= InitialStrength * 0.24f)
            Route();
    }

    private void Route()
    {
        if (IsRouted)
            return;

        IsRouted = true;
        Morale = Mathf.Min(Morale, 15f);
        forcedTarget = null;

        Vector3 retreat = transform.position + (Team == BattleTeam.Denmark ? Vector3.left : Vector3.right) * 150f;
        retreat.z += Random.Range(-25f, 25f);
        destination = retreat;
        hasDestination = true;

        BattleManager.Instance.NotifyRout(this);
    }

    private void RefreshVisualStrength()
    {
        int visible = Mathf.Clamp(Mathf.CeilToInt(CurrentStrength / 10f), 0, soldierModels.Count);
        for (int i = 0; i < soldierModels.Count; i++)
            soldierModels[i].gameObject.SetActive(i < visible);
    }

    public void SetSelected(bool selected)
    {
        IsSelected = selected;

        if (selectionMarker != null)
            selectionMarker.SetActive(selected);

        SetRangeVisualEnabled(selected && ShowRange);
    }

    public void RefreshRangeVisibility()
    {
        SetRangeVisualEnabled(IsSelected && ShowRange);
    }

    private void SetRangeVisualEnabled(bool enabledValue)
    {
        if (closeRangeFan != null)
            closeRangeFan.enabled = enabledValue;
        if (mediumRangeFan != null)
            mediumRangeFan.enabled = enabledValue;
        if (longRangeFan != null)
            longRangeFan.enabled = enabledValue;
    }

    public void OrderMove(Vector3 worldPoint)
    {
        if (IsRouted)
            return;

        forcedTarget = null;
        destination = worldPoint;
        destination.y = PrototypeBootstrap.SampleGroundHeight(destination.x, destination.z) + 0.10f;
        hasDestination = true;
    }

    public void OrderAttack(Regiment target)
    {
        if (IsRouted || target == null || target.Team == Team)
            return;

        forcedTarget = target;
        float distance = Vector3.Distance(transform.position, target.transform.position);

        if (distance > GetAttackStopRange())
        {
            destination = target.transform.position;
            hasDestination = true;
        }
        else
        {
            hasDestination = false;
        }
    }

    public void OrderHold()
    {
        if (IsRouted)
            return;

        hasDestination = false;
        forcedTarget = null;
    }

    public void SetFormation(RegimentFormation formation)
    {
        Formation = formation;

        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
            box.size = formation == RegimentFormation.Line
                ? new Vector3(19f, 2.2f, 5f)
                : new Vector3(6f, 2.2f, 15f);
    }
}
