using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f29t
// Makes the existing F29P tactical map an interaction surface without changing its drawing layer.
// Design rule shared with the OOB navigator:
// - single-click friendly company/HQ = select command entity only, camera stays where it is,
// - double-click same friendly company/HQ = select + move camera behind that entity,
// - single-click empty map/enemy marker = move camera to that map location,
// - map clicks are isolated from normal battlefield selection/order input.
[DefaultExecutionOrder(-50000)]
public sealed class PrototypeTacticalMapInteraction09F29T : MonoBehaviour
{
    private enum MapEntityType
    {
        None,
        Company,
        Major,
        RegimentHq
    }

    private struct MapBounds
    {
        public float MinX, MaxX, MinZ, MaxZ;
    }

    private struct MapHit
    {
        public MapEntityType Type;
        public Regiment Company;
        public int MajorIndex;
        public Transform Target;
        public string Key;
        public float PixelDistance;
    }

    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private const float PanelWidth = 270f;
    private const float PanelHeight = 188f;
    private const float BottomGuard = 104f;
    private const float DoubleClickSeconds = 0.34f;
    private const float CompanyHitRadius = 10f;
    private const float MajorHitRadius = 11f;
    private const float RegimentHitRadius = 12f;

    private Camera cam;
    private PrototypeCameraNavigation09F29P mapNavigation;
    private FieldInfo mapVisibleField;
    private FieldInfo playerSelectedField;
    private MethodInfo selectMajorMethod;
    private MethodInfo setRegimentalSelectedMethod;

    private string lastClickKey = string.Empty;
    private float lastClickAt = -10f;

    private bool restoreCommander;
    private bool restoreBoxSelection;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeTacticalMapInteraction09F29T>() == null)
            new GameObject("PrototypeTacticalMapInteraction_v000009f29t")
                .AddComponent<PrototypeTacticalMapInteraction09F29T>();
    }

    private void Awake()
    {
        mapVisibleField = typeof(PrototypeCameraNavigation09F29P)
            .GetField("mapVisible", PrivateInstance);
        playerSelectedField = typeof(PlayerCommander)
            .GetField("selected", PrivateInstance);
        selectMajorMethod = typeof(PrototypeRegimentHierarchy09F27)
            .GetMethod("SelectMajor", PrivateInstance);
        setRegimentalSelectedMethod = typeof(PrototypeRegimentalHQ09F28)
            .GetMethod("SetSelected", PrivateInstance);

        Debug.Log(
            "TACTICAL-MAP-09F29T|Installed=True|SingleClick=SelectOnly|" +
            "DoubleClick=BehindSelected|EmptyClick=CameraToMap|EnemyClick=CameraToMap|" +
            "PointerIsolation=True");
    }

    private void Update()
    {
        if (cam == null)
            cam = Camera.main;
        if (mapNavigation == null)
            mapNavigation = UnityEngine.Object.FindAnyObjectByType<PrototypeCameraNavigation09F29P>();

        if (cam == null || mapNavigation == null || !IsMapVisible())
            return;

        if (!Input.GetMouseButtonDown(0))
            return;

        Rect mapRect = GetMapRect();
        Vector2 guiPoint = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        if (!mapRect.Contains(guiPoint))
            return;

        BlockBattlefieldPointerForFrame();

        MapBounds bounds = CalculateMapBounds();
        MapHit hit = FindFriendlyEntity(guiPoint, mapRect, bounds);

        if (hit.Type != MapEntityType.None)
        {
            bool doubleClick = RegisterClick(hit.Key);
            SelectHit(hit);

            if (doubleClick && hit.Target != null)
                FocusBehind(hit.Target);

            Debug.Log(
                "TACTICAL-MAP-09F29T|Action=" + (doubleClick ? "SELECT_AND_BEHIND" : "SELECT_ONLY") +
                "|Entity=" + hit.Key +
                "|CameraMoved=" + (doubleClick ? "True" : "False"));
            return;
        }

        lastClickKey = string.Empty;
        lastClickAt = -10f;
        Vector3 world = MapToWorld(guiPoint, mapRect, bounds);
        FocusWorld(world);
        Debug.Log(
            "TACTICAL-MAP-09F29T|Action=FOCUS_MAP|WorldX=" + world.x.ToString("0.0") +
            "|WorldZ=" + world.z.ToString("0.0"));
    }

    private void LateUpdate()
    {
        RestoreBattlefieldPointerHandlers();
    }

    private void OnDisable()
    {
        RestoreBattlefieldPointerHandlers();
    }

    private bool IsMapVisible()
    {
        if (mapNavigation == null || !mapNavigation.enabled)
            return false;
        if (mapVisibleField == null)
            return true;

        object value = mapVisibleField.GetValue(mapNavigation);
        return !(value is bool) || (bool)value;
    }

    private static Rect GetMapRect()
    {
        float panelX = Mathf.Max(8f, Screen.width - PanelWidth - 10f);
        float panelY = Mathf.Max(66f, Screen.height - BottomGuard - PanelHeight - 8f);
        return new Rect(panelX + 8f, panelY + 25f, PanelWidth - 16f, 112f);
    }

    private MapHit FindFriendlyEntity(Vector2 click, Rect rect, MapBounds bounds)
    {
        MapHit best = new MapHit
        {
            Type = MapEntityType.None,
            MajorIndex = -1,
            PixelDistance = float.PositiveInfinity
        };

        BattleManager battle = BattleManager.Instance;
        if (battle != null && battle.Regiments != null)
        {
            foreach (Regiment unit in battle.Regiments)
            {
                if (unit == null || unit.Team != BattleTeam.Denmark || unit.IsRouted || unit.CurrentStrength <= 0)
                    continue;

                float distance = Vector2.Distance(click, WorldToMap(rect, bounds, unit.transform.position));
                if (distance <= CompanyHitRadius && distance < best.PixelDistance)
                {
                    best.Type = MapEntityType.Company;
                    best.Company = unit;
                    best.MajorIndex = -1;
                    best.Target = unit.transform;
                    best.Key = "COMP:" + unit.RegimentName;
                    best.PixelDistance = distance;
                }
            }
        }

        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        if (hierarchy != null && hierarchy.Installed)
        {
            for (int i = 0; i < hierarchy.BattalionCount; i++)
            {
                GameObject hq = hierarchy.GetMajorHq(i);
                if (hq == null)
                    continue;

                float distance = Vector2.Distance(click, WorldToMap(rect, bounds, hq.transform.position));
                if (distance <= MajorHitRadius && distance < best.PixelDistance)
                {
                    best.Type = MapEntityType.Major;
                    best.Company = null;
                    best.MajorIndex = i;
                    best.Target = hq.transform;
                    best.Key = "MAJOR:" + i;
                    best.PixelDistance = distance;
                }
            }
        }

        PrototypeRegimentalHQ09F28 regimental = PrototypeRegimentalHQ09F28.Instance;
        if (regimental != null && regimental.Installed && regimental.HqRoot != null)
        {
            float distance = Vector2.Distance(
                click,
                WorldToMap(rect, bounds, regimental.HqRoot.transform.position));
            if (distance <= RegimentHitRadius && distance < best.PixelDistance)
            {
                best.Type = MapEntityType.RegimentHq;
                best.Company = null;
                best.MajorIndex = -1;
                best.Target = regimental.HqRoot.transform;
                best.Key = "REGHQ";
                best.PixelDistance = distance;
            }
        }

        return best;
    }

    private void SelectHit(MapHit hit)
    {
        switch (hit.Type)
        {
            case MapEntityType.Company:
                SelectCompanyOnly(hit.Company);
                break;
            case MapEntityType.Major:
                SelectMajorOnly(hit.MajorIndex);
                break;
            case MapEntityType.RegimentHq:
                SelectRegimentalOnly();
                break;
        }
    }

    private void SelectCompanyOnly(Regiment unit)
    {
        if (unit == null || unit.Team != BattleTeam.Denmark)
            return;

        SetRegimentalSelected(false);
        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        if (hierarchy != null)
            hierarchy.ClearMajorSelection();

        ClearCompanySelection();
        PlayerCommander commander = PlayerCommander.Instance;
        if (commander != null && playerSelectedField != null)
        {
            List<Regiment> selected = playerSelectedField.GetValue(commander) as List<Regiment>;
            if (selected != null)
            {
                selected.Add(unit);
                unit.SetSelected(true);
            }
        }
    }

    private void SelectMajorOnly(int battalionIndex)
    {
        SetRegimentalSelected(false);
        ClearCompanySelection();

        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        if (hierarchy == null || !hierarchy.Installed || selectMajorMethod == null)
            return;

        selectMajorMethod.Invoke(hierarchy, new object[] { battalionIndex });
    }

    private void SelectRegimentalOnly()
    {
        ClearCompanySelection();
        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        if (hierarchy != null)
            hierarchy.ClearMajorSelection();
        SetRegimentalSelected(true);
    }

    private void SetRegimentalSelected(bool value)
    {
        PrototypeRegimentalHQ09F28 regimental = PrototypeRegimentalHQ09F28.Instance;
        if (regimental != null && setRegimentalSelectedMethod != null)
            setRegimentalSelectedMethod.Invoke(regimental, new object[] { value });
    }

    private void ClearCompanySelection()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle != null && battle.Regiments != null)
        {
            foreach (Regiment unit in battle.Regiments)
            {
                if (unit != null && unit.Team == BattleTeam.Denmark && unit.IsSelected)
                    unit.SetSelected(false);
            }
        }

        PlayerCommander commander = PlayerCommander.Instance;
        if (commander != null && playerSelectedField != null)
        {
            List<Regiment> selected = playerSelectedField.GetValue(commander) as List<Regiment>;
            if (selected != null)
                selected.Clear();
        }
    }

    private bool RegisterClick(string key)
    {
        float now = Time.unscaledTime;
        bool isDouble = key == lastClickKey && now - lastClickAt <= DoubleClickSeconds;
        lastClickKey = key;
        lastClickAt = now;
        return isDouble;
    }

    private void FocusBehind(Transform target)
    {
        if (target == null || cam == null)
            return;

        Vector3 forward = target.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;
        forward.Normalize();

        Vector3 p = target.position - forward * 48f;
        p.y = PrototypeBootstrap.SampleGroundHeight(target.position.x, target.position.z) + 34f;
        cam.transform.position = ClampCamera(p);
        cam.transform.rotation = Quaternion.Euler(33f, target.eulerAngles.y, 0f);
    }

    private void FocusWorld(Vector3 point)
    {
        if (cam == null)
            return;

        Vector3 flat = cam.transform.forward;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.001f)
            flat = Vector3.forward;
        flat.Normalize();

        float height = Mathf.Clamp(cam.transform.position.y, 18f, 600f);
        Vector3 position = point - flat * Mathf.Clamp(height * 0.72f, 22f, 260f);
        position.y = height;
        cam.transform.position = ClampCamera(position);
    }

    private static Vector3 ClampCamera(Vector3 p)
    {
        float xLimit = Mathf.Max(20f, PrototypeBootstrap.BattlefieldHalfWidth - 8f);
        float zLimit = Mathf.Max(20f, PrototypeBootstrap.BattlefieldHalfDepth - 8f);
        p.x = Mathf.Clamp(p.x, -xLimit, xLimit);
        p.z = Mathf.Clamp(p.z, -zLimit, zLimit);
        p.y = Mathf.Clamp(p.y, 8f, 600f);
        return p;
    }

    private void BlockBattlefieldPointerForFrame()
    {
        PlayerCommander commander = PlayerCommander.Instance;
        if (commander != null && commander.enabled)
        {
            commander.enabled = false;
            restoreCommander = true;
        }

        PrototypeBoxSelection09H box = UnityEngine.Object.FindAnyObjectByType<PrototypeBoxSelection09H>();
        if (box != null && box.enabled)
        {
            box.enabled = false;
            restoreBoxSelection = true;
        }
    }

    private void RestoreBattlefieldPointerHandlers()
    {
        if (restoreCommander)
        {
            PlayerCommander commander = PlayerCommander.Instance;
            if (commander != null)
                commander.enabled = true;
            restoreCommander = false;
        }

        if (restoreBoxSelection)
        {
            PrototypeBoxSelection09H box = UnityEngine.Object.FindAnyObjectByType<PrototypeBoxSelection09H>();
            if (box != null)
                box.enabled = true;
            restoreBoxSelection = false;
        }
    }

    private static MapBounds CalculateMapBounds()
    {
        BattleManager battle = BattleManager.Instance;
        float minX = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float minZ = float.PositiveInfinity;
        float maxZ = float.NegativeInfinity;

        if (battle != null && battle.Regiments != null)
        {
            foreach (Regiment unit in battle.Regiments)
            {
                if (unit == null || unit.CurrentStrength <= 0)
                    continue;

                Vector3 p = unit.transform.position;
                minX = Mathf.Min(minX, p.x);
                maxX = Mathf.Max(maxX, p.x);
                minZ = Mathf.Min(minZ, p.z);
                maxZ = Mathf.Max(maxZ, p.z);
            }
        }

        if (float.IsInfinity(minX))
        {
            minX = -450f; maxX = 450f;
            minZ = -300f; maxZ = 300f;
        }

        float centerX = (minX + maxX) * 0.5f;
        float centerZ = (minZ + maxZ) * 0.5f;
        float width = Mathf.Clamp(
            Mathf.Max(900f, maxX - minX + 360f),
            900f,
            PrototypeBootstrap.BattlefieldWidth);
        float depth = Mathf.Clamp(
            Mathf.Max(620f, maxZ - minZ + 300f),
            620f,
            PrototypeBootstrap.BattlefieldDepth);

        centerX = Mathf.Clamp(
            centerX,
            -PrototypeBootstrap.BattlefieldHalfWidth + width * 0.5f,
            PrototypeBootstrap.BattlefieldHalfWidth - width * 0.5f);
        centerZ = Mathf.Clamp(
            centerZ,
            -PrototypeBootstrap.BattlefieldHalfDepth + depth * 0.5f,
            PrototypeBootstrap.BattlefieldHalfDepth - depth * 0.5f);

        return new MapBounds
        {
            MinX = centerX - width * 0.5f,
            MaxX = centerX + width * 0.5f,
            MinZ = centerZ - depth * 0.5f,
            MaxZ = centerZ + depth * 0.5f
        };
    }

    private static Vector2 WorldToMap(Rect rect, MapBounds bounds, Vector3 world)
    {
        float tx = Mathf.InverseLerp(bounds.MinX, bounds.MaxX, world.x);
        float tz = Mathf.InverseLerp(bounds.MinZ, bounds.MaxZ, world.z);
        return new Vector2(rect.x + tx * rect.width, rect.yMax - tz * rect.height);
    }

    private static Vector3 MapToWorld(Vector2 gui, Rect rect, MapBounds bounds)
    {
        float tx = Mathf.Clamp01((gui.x - rect.x) / rect.width);
        float tz = Mathf.Clamp01((rect.yMax - gui.y) / rect.height);
        float x = Mathf.Lerp(bounds.MinX, bounds.MaxX, tx);
        float z = Mathf.Lerp(bounds.MinZ, bounds.MaxZ, tz);
        return new Vector3(x, PrototypeBootstrap.SampleGroundHeight(x, z), z);
    }
}
