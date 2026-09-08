using UnityEngine;

public sealed class CampaignMapCameraController : MonoBehaviour
{
    public float PanSpeed = 95f;
    public float ZoomSpeed = 90f;
    public float RotateSpeed = 55f;
    public float MinHeight = 70f;
    public float MaxHeight = 520f;

    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Update()
    {
        if (cam == null)
            return;

        float dt = Time.unscaledDeltaTime;
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();
        Vector3 right = transform.right;
        right.y = 0f;
        right.Normalize();

        float heightFactor = Mathf.Lerp(0.65f, 2.1f,
            Mathf.InverseLerp(MinHeight, MaxHeight, transform.position.y));

        Vector3 pan = (right * horizontal + forward * vertical) * PanSpeed * heightFactor * dt;
        transform.position += pan;

        float wheel = Input.mouseScrollDelta.y;
        if (Mathf.Abs(wheel) > 0.001f)
        {
            Vector3 position = transform.position;
            position.y = Mathf.Clamp(position.y - wheel * ZoomSpeed * dt * 7f, MinHeight, MaxHeight);
            transform.position = position;
        }

        float rotation = 0f;
        if (Input.GetKey(KeyCode.Q))
            rotation -= 1f;
        if (Input.GetKey(KeyCode.E))
            rotation += 1f;

        if (Mathf.Abs(rotation) > 0.01f)
            transform.Rotate(Vector3.up, rotation * RotateSpeed * dt, Space.World);

        Vector3 clamped = transform.position;
        clamped.x = Mathf.Clamp(clamped.x, -CampaignGeoProjection.MapWidth * 0.62f, CampaignGeoProjection.MapWidth * 0.62f);
        clamped.z = Mathf.Clamp(clamped.z, -CampaignGeoProjection.MapDepth * 0.62f, CampaignGeoProjection.MapDepth * 0.62f);
        clamped.y = Mathf.Clamp(clamped.y, MinHeight, MaxHeight);
        transform.position = clamped;
    }
}
