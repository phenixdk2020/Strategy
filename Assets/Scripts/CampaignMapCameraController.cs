using System;
using UnityEngine;

public sealed class CampaignMapCameraController : MonoBehaviour
{
    public float PanSpeed = 95f;
    public float ZoomSpeed = 90f;
    public float RotateSpeed = 55f;
    public float PitchSpeed = 42f;
    public float MinHeight = 60f;
    public float MaxHeight = 520f;
    public float MinPitch = 32f;
    public float MaxPitch = 72f;

    // v13g+: presentation layers can opt out of the hidden legacy terrain floor.
    public bool UseLegacyTerrainFloor = true;
    public float TerrainClearance = 42f;

    // v13j: close cartographic inspection. Scroll zooms toward the mouse position
    // when a GIS terrain collider is under the cursor; middle-mouse drag pans.
    public bool ZoomTowardCursor = true;
    public bool MiddleMousePan = true;

    private Camera cam;
    private Vector3 homePosition;
    private Quaternion homeRotation;
    private Vector3 lastMousePosition;
    private bool mousePanning;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        Vector3 denmark = CampaignGeoProjection.Project3D(56.15, 10.20, 0f);
        homePosition = new Vector3(denmark.x, 245f, denmark.z - 92f);
        homeRotation = Quaternion.Euler(58f, 0f, 0f);
        transform.position = homePosition;
        transform.rotation = homeRotation;
    }

    public void ApplyPresentationHome(Vector3 position, Quaternion rotation)
    {
        homePosition = position;
        homeRotation = rotation;
        transform.position = position;
        transform.rotation = rotation;
    }

    private void Update()
    {
        if (cam == null)
            return;

        float dt = Time.unscaledDeltaTime;
        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
        forward.Normalize();
        Vector3 right = transform.right;
        right.y = 0f;
        if (right.sqrMagnitude < 0.0001f) right = Vector3.right;
        right.Normalize();

        float height01 = Mathf.InverseLerp(MinHeight, MaxHeight, transform.position.y);
        float heightFactor = Mathf.Lerp(0.22f, 2.1f, height01);

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        transform.position += (right * horizontal + forward * vertical) * PanSpeed * heightFactor * dt;

        HandleMousePan(right, forward, heightFactor);
        HandleZoom(height01);
        HandleRotation(dt);
        HandlePitch(dt);

        if (Input.GetKeyDown(KeyCode.Home))
        {
            transform.position = homePosition;
            transform.rotation = homeRotation;
        }

        ClampPositionToMapAndTerrain();
    }

    private void HandleMousePan(Vector3 right, Vector3 forward, float heightFactor)
    {
        if (!MiddleMousePan)
            return;

        if (Input.GetMouseButtonDown(2))
        {
            mousePanning = true;
            lastMousePosition = Input.mousePosition;
        }
        if (Input.GetMouseButtonUp(2))
            mousePanning = false;

        if (!mousePanning || !Input.GetMouseButton(2))
            return;

        Vector3 current = Input.mousePosition;
        Vector3 delta = current - lastMousePosition;
        lastMousePosition = current;
        float scale = PanSpeed * heightFactor * 0.00042f;
        transform.position += (-right * delta.x - forward * delta.y) * scale;
    }

    private void HandleZoom(float height01)
    {
        float wheel = Input.mouseScrollDelta.y;
        if (Mathf.Abs(wheel) < 0.001f)
            return;

        float step = Mathf.Lerp(1.0f, 30f, height01) * Mathf.Max(0.1f, ZoomSpeed / 110f) * Mathf.Abs(wheel);
        Vector3 direction;

        if (ZoomTowardCursor && wheel > 0f && TryGetCursorTerrainPoint(out Vector3 target))
            direction = (target - transform.position).normalized;
        else if (wheel > 0f)
            direction = transform.forward.normalized;
        else
            direction = -transform.forward.normalized;

        transform.position += direction * step;
    }

    private bool TryGetCursorTerrainPoint(out Vector3 point)
    {
        point = Vector3.zero;
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 4000f);
        float bestDistance = float.PositiveInfinity;
        bool found = false;
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null || !IsCampaignTerrainCollider(hit.collider))
                continue;
            if (hit.distance < bestDistance)
            {
                bestDistance = hit.distance;
                point = hit.point;
                found = true;
            }
        }
        return found;
    }

    private void HandleRotation(float dt)
    {
        float input = 0f;
        if (Input.GetKey(KeyCode.Q)) input -= 1f;
        if (Input.GetKey(KeyCode.E)) input += 1f;
        if (Mathf.Abs(input) > 0.01f)
            transform.Rotate(Vector3.up, input * RotateSpeed * dt, Space.World);
    }

    private void HandlePitch(float dt)
    {
        float input = 0f;
        if (Input.GetKey(KeyCode.PageUp)) input -= 1f;
        if (Input.GetKey(KeyCode.PageDown)) input += 1f;
        if (Mathf.Abs(input) <= 0.01f)
            return;

        Vector3 euler = transform.eulerAngles;
        float pitch = NormalizeAngle(euler.x);
        pitch = Mathf.Clamp(pitch + input * PitchSpeed * dt, MinPitch, MaxPitch);
        transform.rotation = Quaternion.Euler(pitch, euler.y, 0f);
    }

    private void ClampPositionToMapAndTerrain()
    {
        Vector3 p = transform.position;
        p.x = Mathf.Clamp(p.x, -CampaignGeoProjection.MapWidth * 0.62f, CampaignGeoProjection.MapWidth * 0.62f);
        p.z = Mathf.Clamp(p.z, -CampaignGeoProjection.MapDepth * 0.62f, CampaignGeoProjection.MapDepth * 0.62f);

        float surfaceY;
        bool hasSurface;
        if (UseLegacyTerrainFloor)
        {
            surfaceY = CampaignTerrainV013.SampleSurfaceY(p.x, p.z);
            hasSurface = true;
        }
        else
        {
            hasSurface = TrySampleGisSurface(p.x, p.z, out surfaceY);
        }

        float floor = hasSurface ? surfaceY + Mathf.Max(0.05f, TerrainClearance) : MinHeight;
        p.y = Mathf.Clamp(p.y, Mathf.Max(MinHeight, floor), MaxHeight);
        transform.position = p;
    }

    private static bool TrySampleGisSurface(float x, float z, out float y)
    {
        y = 0f;
        RaycastHit[] hits = Physics.RaycastAll(new Vector3(x, 1000f, z), Vector3.down, 2000f);
        float best = float.NegativeInfinity;
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null || !IsCampaignTerrainCollider(hit.collider))
                continue;
            if (hit.point.y > best)
                best = hit.point.y;
        }
        if (float.IsNegativeInfinity(best))
            return false;
        y = best;
        return true;
    }

    private static bool IsCampaignTerrainCollider(Collider collider)
    {
        string name = collider.gameObject.name;
        return string.Equals(name, Campaign2Config.LandObjectName, StringComparison.Ordinal) ||
               string.Equals(name, "V013J_SmoothTerrain", StringComparison.Ordinal) ||
               name.StartsWith("GIS_Terrain_", StringComparison.Ordinal);
    }

    private static float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }
}
