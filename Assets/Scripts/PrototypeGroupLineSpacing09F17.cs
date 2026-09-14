using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f17 fixes group Line orders where multiple selected companies were
// placed too close together and visually overlapped. The authoritative PlayerCommander
// routes are adjusted after creation so all existing pathfinding/ghost logic is retained.
[DefaultExecutionOrder(350)]
public sealed class PrototypeGroupLineSpacing09F17 : MonoBehaviour
{
    private const float FormationDragThreshold = 4f;
    private const float CompanyFrontage = 48f;
    private const float CompanySpacing = 60f; // 48 m frontage + ~12 m interval.

    private FieldInfo routesField;
    private FieldInfo formationPreviewField;
    private MethodInfo issueCurrentRouteLegMethod;

    private bool tracking;
    private bool skipOrder;
    private Vector3 start;
    private Vector3 end;
    private Camera cam;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeGroupLineSpacing09F17>() != null)
            return;

        GameObject root = new GameObject("PrototypeGroupLineSpacing_v000009f17");
        root.AddComponent<PrototypeGroupLineSpacing09F17>();
    }

    private void Awake()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        routesField = typeof(PlayerCommander).GetField("routes", flags);
        formationPreviewField = typeof(PlayerCommander).GetField("formationPreview", flags);
        issueCurrentRouteLegMethod = typeof(PlayerCommander).GetMethod("IssueCurrentRouteLeg", flags);

        if (routesField == null || formationPreviewField == null || issueCurrentRouteLegMethod == null)
        {
            Debug.LogError("GROUP-LINE-09F17|Installed=False|Reason=PlayerCommanderReflectionMissing");
            enabled = false;
            return;
        }

        Debug.Log("GROUP-LINE-09F17|Installed=True|Frontage=48m|Spacing=60m|SideBySide=True");
    }

    private void Update()
    {
        if (cam == null)
            cam = Camera.main;
        if (cam == null || PlayerCommander.Instance == null)
            return;

        bool overUi = BattleManager.Instance != null &&
                      BattleManager.Instance.IsPointerOverSimulationControls(Input.mousePosition);

        if (Input.GetMouseButtonDown(1))
        {
            tracking = false;
            skipOrder = false;

            if (overUi || GetSelectedDanish().Count < 2)
                return;

            if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
            {
                skipOrder = true;
                return;
            }

            if (GetEnemyUnderMouse() != null)
            {
                skipOrder = true;
                return;
            }

            if (!TryGetGroundPoint(Input.mousePosition, out start))
                return;

            end = start;
            tracking = true;
        }

        if (tracking && Input.GetMouseButton(1))
        {
            if (TryGetGroundPoint(Input.mousePosition, out Vector3 point))
                end = point;
            CorrectPreview();
        }

        if (Input.GetMouseButtonUp(1))
        {
            if (tracking && !skipOrder)
                CorrectCreatedRoutes();
            tracking = false;
            skipOrder = false;
        }
    }

    private void CorrectCreatedRoutes()
    {
        List<Regiment> selected = GetSelectedDanish();
        if (selected.Count < 2)
            return;

        Vector3 drag = end - start;
        drag.y = 0f;
        bool explicitFacing = drag.magnitude >= FormationDragThreshold;
        Vector3 facing = explicitFacing ? drag.normalized : GetAutomaticFacing(selected, start);
        Vector3 lineDirection = Vector3.Cross(Vector3.up, facing).normalized;
        if (lineDirection.sqrMagnitude < 0.01f)
            lineDirection = Vector3.right;

        selected.Sort((a, b) =>
        {
            float ap = Vector3.Dot(a.transform.position, lineDirection);
            float bp = Vector3.Dot(b.transform.position, lineDirection);
            return ap.CompareTo(bp);
        });

        object routesObject = routesField.GetValue(PlayerCommander.Instance);
        IDictionary routes = routesObject as IDictionary;
        if (routes == null)
            return;

        float centerIndex = (selected.Count - 1) * 0.5f;
        int adjusted = 0;

        for (int i = 0; i < selected.Count; i++)
        {
            Regiment regiment = selected[i];
            if (regiment == null || !routes.Contains(regiment))
                continue;

            object route = routes[regiment];
            if (route == null)
                continue;

            System.Type routeType = route.GetType();
            FieldInfo waypointsField = routeType.GetField("Waypoints", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            FieldInfo finalFacingField = routeType.GetField("FinalFacing", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            FieldInfo finalFormationField = routeType.GetField("FinalFormation", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            FieldInfo explicitField = routeType.GetField("HasExplicitFinalFacing", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            FieldInfo currentIndexField = routeType.GetField("CurrentIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            IList waypoints = waypointsField != null ? waypointsField.GetValue(route) as IList : null;
            if (waypoints == null || waypoints.Count == 0)
                continue;

            float offset = (i - centerIndex) * CompanySpacing;
            Vector3 destination = start + lineDirection * offset;
            destination.y = PrototypeBootstrap.SampleGroundHeight(destination.x, destination.z) + 0.10f;

            int currentIndex = currentIndexField != null ? (int)currentIndexField.GetValue(route) : 0;
            if (currentIndex < 0 || currentIndex >= waypoints.Count)
                currentIndex = 0;

            waypoints[currentIndex] = destination;
            if (finalFacingField != null) finalFacingField.SetValue(route, facing);
            if (finalFormationField != null) finalFormationField.SetValue(route, RegimentFormation.Line);
            if (explicitField != null) explicitField.SetValue(route, explicitFacing);

            issueCurrentRouteLegMethod.Invoke(PlayerCommander.Instance, new object[] { regiment, route });
            adjusted++;
        }

        Debug.Log("GROUP-LINE-09F17|Adjusted=" + adjusted +
                  "|Selected=" + selected.Count +
                  "|Spacing=" + CompanySpacing.ToString("0") +
                  "m|SideBySide=True");
    }

    private void CorrectPreview()
    {
        List<Regiment> selected = GetSelectedDanish();
        if (selected.Count < 2)
            return;

        LineRenderer preview = formationPreviewField.GetValue(PlayerCommander.Instance) as LineRenderer;
        if (preview == null)
            return;

        Vector3 drag = end - start;
        drag.y = 0f;
        Vector3 facing = drag.magnitude >= FormationDragThreshold ? drag.normalized : GetAutomaticFacing(selected, start);
        Vector3 lineDirection = Vector3.Cross(Vector3.up, facing).normalized;
        if (lineDirection.sqrMagnitude < 0.01f)
            lineDirection = Vector3.right;

        float fullFrontage = CompanyFrontage + Mathf.Max(0, selected.Count - 1) * CompanySpacing;
        Vector3 a = start - lineDirection * fullFrontage * 0.5f;
        Vector3 b = start + lineDirection * fullFrontage * 0.5f;

        const int segments = 32;
        preview.positionCount = segments + 1;
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector3 p = Vector3.Lerp(a, b, t);
            p.y = PrototypeBootstrap.SampleGroundHeight(p.x, p.z) + 0.48f;
            preview.SetPosition(i, p);
        }
    }

    private List<Regiment> GetSelectedDanish()
    {
        List<Regiment> result = new List<Regiment>();
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return result;

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment != null && regiment.Team == BattleTeam.Denmark && regiment.IsSelected && !regiment.IsRouted)
                result.Add(regiment);
        }
        return result;
    }

    private static Vector3 GetAutomaticFacing(List<Regiment> selected, Vector3 destination)
    {
        Vector3 center = Vector3.zero;
        int count = 0;
        foreach (Regiment regiment in selected)
        {
            if (regiment == null) continue;
            center += regiment.transform.position;
            count++;
        }
        if (count > 0) center /= count;

        Vector3 facing = destination - center;
        facing.y = 0f;
        if (facing.sqrMagnitude > 0.01f)
            return facing.normalized;

        Vector3 average = Vector3.zero;
        foreach (Regiment regiment in selected)
        {
            Vector3 f = regiment.transform.forward;
            f.y = 0f;
            if (f.sqrMagnitude > 0.01f) average += f.normalized;
        }
        return average.sqrMagnitude > 0.01f ? average.normalized : Vector3.forward;
    }

    private Regiment GetEnemyUnderMouse()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 4000f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            Regiment regiment = hit.collider != null ? hit.collider.GetComponentInParent<Regiment>() : null;
            if (regiment != null && regiment.Team == BattleTeam.Prussia)
                return regiment;
        }
        return null;
    }

    private bool TryGetGroundPoint(Vector3 screenPoint, out Vector3 point)
    {
        point = default;
        Ray ray = cam.ScreenPointToRay(screenPoint);
        RaycastHit[] hits = Physics.RaycastAll(ray, 4000f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null) continue;
            if (hit.collider.GetComponentInParent<Regiment>() != null) continue;
            if (hit.collider.gameObject.name != "Battlefield Ground") continue;
            point = hit.point;
            point.y = PrototypeBootstrap.SampleGroundHeight(point.x, point.z) + 0.10f;
            return true;
        }
        return false;
    }
}
