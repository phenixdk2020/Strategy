using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Campaign4 perspective camera for the 1851 Denmark campaign map.
///
/// Controls:
/// - Mouse wheel: zoom from country scale to city scale.
/// - WASD / arrow keys: pan the map pivot.
/// - Middle mouse drag: orbit/tilt.
/// - Q / E: rotate around the pivot.
/// - F: focus Aalborg/Limfjorden QA area.
/// - Home: reset to Denmark overview.
///
/// v00.00.10l overview tuning:
/// - Slightly more top-down strategic framing.
/// - Narrower FOV and a small yaw offset for a map-like composition.
/// - Keeps the existing close-zoom Aalborg behaviour.
/// </summary>
[DefaultExecutionOrder(32000)]
public sealed class Campaign4Camera3DController : MonoBehaviour
{
    public static bool IsActive { get; private set; }
    public static float CurrentDistance { get; private set; }
    public static float Zoom01 { get; private set; }

    private const float MinDistance = 5.5f;
    private const float MaxDistance = 115f;
    private const float MinPitch = 32f;
    private const float MaxPitch = 78f;

    private Camera mapCamera;
    private Vector3 currentPivot;
    private Vector3 targetPivot;
    private float currentDistance = 94f;
    private float targetDistance = 94f;
    private float currentYaw = -3f;
    private float targetYaw = -3f;
    private float currentPitch = 68f;
    private float targetPitch = 68f;
    private bool installed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<Campaign4Camera3DController>() != null)
            return;

        GameObject root = new GameObject("CAMPAIGN4_3D_Camera_Controller");
        DontDestroyOnLoad(root);
        root.AddComponent<Campaign4Camera3DController>();
    }

    private void Start()
    {
        ResetToDenmark(true);
        TryInstall();
    }

    private void OnDestroy()
    {
        IsActive = false;
    }

    private void Update()
    {
        if (!TryInstall())
            return;

        HandleZoom();
        HandlePan();
        HandleOrbit();
        HandleFocusShortcuts();

        CurrentDistance = currentDistance;
        Zoom01 = 1f - Mathf.InverseLerp(MinDistance, MaxDistance, currentDistance);
    }

    private void LateUpdate()
    {
        if (!TryInstall())
            return;

        float positionSharpness = 10f;
        float rotationSharpness = 12f;
        float tPosition = 1f - Mathf.Exp(-positionSharpness * Time.unscaledDeltaTime);
        float tRotation = 1f - Mathf.Exp(-rotationSharpness * Time.unscaledDeltaTime);

        currentPivot = Vector3.Lerp(currentPivot, targetPivot, tPosition);
        currentDistance = Mathf.Lerp(currentDistance, targetDistance, tPosition);
        currentYaw = Mathf.LerpAngle(currentYaw, targetYaw, tRotation);
        currentPitch = Mathf.Lerp(currentPitch, targetPitch, tRotation);

        Quaternion orbit = Quaternion.Euler(currentPitch, currentYaw, 0f);
        Vector3 offset = orbit * (Vector3.back * currentDistance);
        Vector3 cameraPosition = currentPivot + offset;

        mapCamera.transform.position = cameraPosition;
        mapCamera.transform.rotation = Quaternion.LookRotation(currentPivot - cameraPosition, Vector3.up);

        CurrentDistance = currentDistance;
        Zoom01 = 1f - Mathf.InverseLerp(MinDistance, MaxDistance, currentDistance);
    }

    private bool TryInstall()
    {
        if (mapCamera == null)
            mapCamera = Camera.main;

        if (mapCamera == null)
            return false;

        if (installed)
            return true;

        mapCamera.orthographic = false;
        mapCamera.fieldOfView = 38f;
        mapCamera.nearClipPlane = 0.05f;
        mapCamera.farClipPlane = 500f;

        currentPivot = targetPivot;
        currentDistance = targetDistance;
        currentYaw = targetYaw;
        currentPitch = targetPitch;

        installed = true;
        IsActive = true;

        Debug.Log(
            "CAMPAIGN4-CAMERA|Version=v00.00.10l|Installed=True|Mode=Perspective3D|" +
            "ZoomMin=" + MinDistance.ToString("0.0") +
            "|ZoomMax=" + MaxDistance.ToString("0.0") +
            "|OverviewDistance=94|OverviewPitch=68|FOV=38|Focus=AalborgWithF|Reset=Home");

        return true;
    }

    private void HandleZoom()
    {
        float wheel = Input.mouseScrollDelta.y;
        if (Mathf.Abs(wheel) <= 0.01f)
            return;

        float zoomFactor = Mathf.Pow(0.82f, wheel);
        targetDistance = Mathf.Clamp(targetDistance * zoomFactor, MinDistance, MaxDistance);
    }

    private void HandlePan()
    {
        float horizontal = 0f;
        float vertical = 0f;

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) horizontal -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) horizontal += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) vertical -= 1f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) vertical += 1f;

        Vector2 input = new Vector2(horizontal, vertical);
        if (input.sqrMagnitude < 0.001f)
            return;

        input.Normalize();

        Quaternion heading = Quaternion.Euler(0f, targetYaw, 0f);
        Vector3 right = heading * Vector3.right;
        Vector3 forward = heading * Vector3.forward;

        float panSpeed = Mathf.Lerp(4.0f, 42f, Mathf.InverseLerp(MinDistance, MaxDistance, targetDistance));
        Vector3 delta = (right * input.x + forward * input.y) * panSpeed * Time.unscaledDeltaTime;
        targetPivot += delta;
        ClampPivot();
    }

    private void HandleOrbit()
    {
        if (Input.GetMouseButton(2))
        {
            targetYaw += Input.GetAxis("Mouse X") * 4.5f;
            targetPitch -= Input.GetAxis("Mouse Y") * 3.5f;
        }

        float keyboardTurn = 0f;
        if (Input.GetKey(KeyCode.Q)) keyboardTurn -= 1f;
        if (Input.GetKey(KeyCode.E)) keyboardTurn += 1f;
        targetYaw += keyboardTurn * 55f * Time.unscaledDeltaTime;

        targetPitch = Mathf.Clamp(targetPitch, MinPitch, MaxPitch);
    }

    private void HandleFocusShortcuts()
    {
        if (Input.GetKeyDown(KeyCode.Home))
            ResetToDenmark(false);

        if (Input.GetKeyDown(KeyCode.F))
            FocusAalborg();
    }

    private void ResetToDenmark(bool instant)
    {
        targetPivot = CampaignGeoProjection.Project(10.20f, 56.10f, 0.25f);
        targetDistance = 94f;
        targetYaw = -3f;
        targetPitch = 68f;

        if (!instant)
            return;

        currentPivot = targetPivot;
        currentDistance = targetDistance;
        currentYaw = targetYaw;
        currentPitch = targetPitch;
    }

    private void FocusAalborg()
    {
        targetPivot = CampaignGeoProjection.Project(9.9217f, 57.0488f, 0.35f);
        targetDistance = 12.5f;
        targetYaw = -8f;
        targetPitch = 58f;
        ClampPivot();

        Debug.Log("CAMPAIGN4-CAMERA|Version=v00.00.10l|Focus=Aalborg|LimfjordQA=True|Distance=12.5");
    }

    private void ClampPivot()
    {
        targetPivot.x = Mathf.Clamp(targetPivot.x, -48f, 76f);
        targetPivot.z = Mathf.Clamp(targetPivot.z, -44f, 46f);
        targetPivot.y = Mathf.Clamp(targetPivot.y, 0.10f, 1.50f);
    }
}
