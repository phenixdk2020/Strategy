using UnityEngine;

/// <summary>
/// Strategic map camera: near top-down over all of Denmark, tilting towards a
/// three-quarter view as it zooms in. WASD/arrows or right/middle drag pans,
/// the wheel zooms towards the cursor, Q/E rotates, Home resets.
/// Units are kilometres (1 Unity unit = 1 km).
/// </summary>
public sealed class CampaignMap1851Camera : MonoBehaviour
{
    public float MinDistance = 22f;
    public float MaxDistance = 1000f;
    public float PitchNear = 46f;
    public float PitchFar = 84f;

    public Vector3 Target;
    public float Distance = 600f;
    public float Yaw;
    public Vector2 HalfExtent = new Vector2(190f, 255f);

    private float targetDistance;
    private Vector3 homeTarget;
    private float homeDistance;
    private Vector3 lastMouse;
    private Camera cam;

    public float Pitch
    {
        get
        {
            float t = Mathf.InverseLerp(MinDistance, MaxDistance, Distance);
            return Mathf.Lerp(PitchNear, PitchFar, Mathf.Pow(t, 0.55f));
        }
    }

    public void Init(Vector3 target, float distance)
    {
        cam = GetComponent<Camera>();
        Target = homeTarget = target;
        Distance = targetDistance = homeDistance = distance;
        Apply();
    }

    public void SetView(Vector3 target, float distance, float yaw)
    {
        Target = target;
        Distance = targetDistance = Mathf.Clamp(distance, MinDistance, MaxDistance);
        Yaw = yaw;
        Apply();
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;

        // Keyboard pan, relative to the camera's yaw.
        Vector3 fwd = Quaternion.Euler(0f, Yaw, 0f) * Vector3.forward;
        Vector3 right = Quaternion.Euler(0f, Yaw, 0f) * Vector3.right;
        Vector3 move = Vector3.zero;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) move += fwd;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) move -= fwd;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) move += right;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) move -= right;
        Target += move * Distance * 0.8f * dt;

        // Drag pan: keep the ground point under the cursor.
        if (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
            lastMouse = Input.mousePosition;
        if (Input.GetMouseButton(1) || Input.GetMouseButton(2))
        {
            if (GroundPoint(lastMouse, out Vector3 a) && GroundPoint(Input.mousePosition, out Vector3 b))
                Target += a - b;
            lastMouse = Input.mousePosition;
        }

        if (Input.GetKey(KeyCode.Q)) Yaw -= 60f * dt;
        if (Input.GetKey(KeyCode.E)) Yaw += 60f * dt;

        if (Input.GetKeyDown(KeyCode.Home))
        {
            Target = homeTarget;
            targetDistance = homeDistance;
            Yaw = 0f;
        }

        // Wheel zoom towards the cursor.
        float wheel = Input.mouseScrollDelta.y;
        if (Mathf.Abs(wheel) > 0.01f)
        {
            float before = targetDistance;
            targetDistance = Mathf.Clamp(targetDistance * Mathf.Pow(0.85f, wheel), MinDistance, MaxDistance);
            if (GroundPoint(Input.mousePosition, out Vector3 p))
                Target += (p - Target) * (1f - targetDistance / before);
        }

        Distance = Mathf.Lerp(Distance, targetDistance, 1f - Mathf.Exp(-10f * dt));
        Apply();
    }

    private void Apply()
    {
        Target.x = Mathf.Clamp(Target.x, -HalfExtent.x, HalfExtent.x);
        Target.z = Mathf.Clamp(Target.z, -HalfExtent.y, HalfExtent.y);
        Quaternion rot = Quaternion.Euler(Pitch, Yaw, 0f);
        transform.SetPositionAndRotation(Target - rot * Vector3.forward * Distance, rot);
    }

    public bool GroundPoint(Vector3 screen, out Vector3 point)
    {
        if (cam == null) cam = GetComponent<Camera>();
        Ray ray = cam.ScreenPointToRay(screen);
        var plane = new Plane(Vector3.up, Vector3.zero);
        if (plane.Raycast(ray, out float enter))
        {
            point = ray.GetPoint(enter);
            return true;
        }
        point = Vector3.zero;
        return false;
    }
}
