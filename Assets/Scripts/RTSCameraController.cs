using UnityEngine;

public sealed class RTSCameraController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 150f;
    [SerializeField] private float rotateSpeed = 75f;
    [SerializeField] private float zoomStep = 9.5f;
    [SerializeField] private float minHeight = 9.5f;
    [SerializeField] private float maxHeight = 600f;

    private void Update()
    {
        HandleCameraInput();
    }

    private void HandleCameraInput()
    {
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
        float xLimit = Mathf.Max(20f, PrototypeBootstrap.BattlefieldHalfWidth - 10f);
        float zLimit = Mathf.Max(20f, PrototypeBootstrap.BattlefieldHalfDepth - 10f);
        p.x = Mathf.Clamp(p.x, -xLimit, xLimit);
        p.z = Mathf.Clamp(p.z, -zLimit, zLimit);
        p.y = Mathf.Clamp(p.y, minHeight, maxHeight);
        transform.position = p;
    }
}
