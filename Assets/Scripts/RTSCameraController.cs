using UnityEngine;

public sealed class RTSCameraController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 52f;
    [SerializeField] private float rotateSpeed = 75f;
    [SerializeField] private float zoomStep = 3.2f;
    [SerializeField] private float minHeight = 16f;
    [SerializeField] private float maxHeight = 125f;

    private void Update()
    {
        HandleCameraInput();
    }

    private void HandleCameraInput()
    {
        // Camera input is presentation and remains independent of battle timeScale.
        float dt = Time.unscaledDeltaTime;

        float horizontal = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            horizontal -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            horizontal += 1f;

        float vertical = 0f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            vertical -= 1f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            vertical += 1f;

        Vector3 flatForward = transform.forward;
        flatForward.y = 0f;
        flatForward.Normalize();

        Vector3 flatRight = transform.right;
        flatRight.y = 0f;
        flatRight.Normalize();

        Vector3 movement = flatForward * vertical + flatRight * horizontal;
        if (movement.sqrMagnitude > 1f)
            movement.Normalize();

        transform.position += movement * moveSpeed * dt;

        if (Input.GetKey(KeyCode.Q))
        {
            transform.RotateAround(
                transform.position + flatForward * 30f,
                Vector3.up,
                -rotateSpeed * dt);
        }

        if (Input.GetKey(KeyCode.E))
        {
            transform.RotateAround(
                transform.position + flatForward * 30f,
                Vector3.up,
                rotateSpeed * dt);
        }

        float wheel = Input.mouseScrollDelta.y;
        if (Mathf.Abs(wheel) > 0.01f)
        {
            Vector3 next = transform.position + transform.forward * (wheel * zoomStep);
            next.y = Mathf.Clamp(next.y, minHeight, maxHeight);
            transform.position = next;
        }

        Vector3 p = transform.position;
        p.x = Mathf.Clamp(p.x, -178f, 178f);
        p.z = Mathf.Clamp(p.z, -118f, 118f);
        p.y = Mathf.Clamp(p.y, minHeight, maxHeight);
        transform.position = p;
    }
}
