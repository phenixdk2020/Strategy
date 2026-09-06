using UnityEngine;

public sealed class RTSCameraController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 35f;
    [SerializeField] private float rotateSpeed = 75f;
    [SerializeField] private float zoomSpeed = 65f;
    [SerializeField] private float minHeight = 16f;
    [SerializeField] private float maxHeight = 70f;

    private void Update()
    {
        HandleCameraInput(Time.timeScale <= 0f);
    }

    private void HandleCameraInput(bool unscaled)
    {
        float dt = unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 flatForward = transform.forward;
        flatForward.y = 0f;
        flatForward.Normalize();
        Vector3 flatRight = transform.right;
        flatRight.y = 0f;
        flatRight.Normalize();

        transform.position += (flatForward * vertical + flatRight * horizontal) * moveSpeed * dt;

        if (Input.GetKey(KeyCode.Q))
            transform.RotateAround(transform.position + flatForward * 20f, Vector3.up, -rotateSpeed * dt);
        if (Input.GetKey(KeyCode.E))
            transform.RotateAround(transform.position + flatForward * 20f, Vector3.up, rotateSpeed * dt);

        float wheel = Input.mouseScrollDelta.y;
        if (Mathf.Abs(wheel) > 0.01f)
        {
            Vector3 next = transform.position + transform.forward * (wheel * zoomSpeed * dt * 4f);
            next.y = Mathf.Clamp(next.y, minHeight, maxHeight);
            transform.position = next;
        }

        Vector3 p = transform.position;
        p.x = Mathf.Clamp(p.x, -90f, 90f);
        p.z = Mathf.Clamp(p.z, -70f, 70f);
        p.y = Mathf.Clamp(p.y, minHeight, maxHeight);
        transform.position = p;
    }
}
