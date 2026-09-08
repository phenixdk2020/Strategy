using UnityEngine;

public sealed class RTSCameraController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 72f;
    [SerializeField] private float rotateSpeed = 82f;
    [SerializeField] private float zoomStep = 4.8f;
    [SerializeField] private float minGroundClearance = 3.8f;
    [SerializeField] private float maxHeight = 300f;

    private const float MapHalfWidth = 350f;
    private const float MapHalfDepth = 230f;

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

        float currentGround = PrototypeBootstrap.SampleGroundHeight(
            transform.position.x,
            transform.position.z);
        float currentClearance = Mathf.Max(0f, transform.position.y - currentGround);
        float heightFactor = Mathf.InverseLerp(minGroundClearance, maxHeight, currentClearance);

        float speedMultiplier = 1f;
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            speedMultiplier = 2.25f;
        else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            speedMultiplier = 0.40f;

        // Pan slightly faster when zoomed far out, but remain precise close to units.
        float heightSpeed = Mathf.Lerp(0.55f, 2.25f, heightFactor);
        transform.position += movement * moveSpeed * speedMultiplier * heightSpeed * dt;

        if (Input.GetKey(KeyCode.Q))
        {
            transform.RotateAround(
                transform.position + flatForward * Mathf.Lerp(18f, 55f, heightFactor),
                Vector3.up,
                -rotateSpeed * dt);
        }

        if (Input.GetKey(KeyCode.E))
        {
            transform.RotateAround(
                transform.position + flatForward * Mathf.Lerp(18f, 55f, heightFactor),
                Vector3.up,
                rotateSpeed * dt);
        }

        float wheel = Input.mouseScrollDelta.y;
        if (Mathf.Abs(wheel) > 0.01f)
        {
            // Dynamic zoom gives fine control near ground and very fast travel from
            // strategic overview height. 09g can zoom from ~300 m to ~4 m above terrain.
            float zoomMultiplier = Mathf.Lerp(0.65f, 5.5f, heightFactor);
            Vector3 next = transform.position +
                           transform.forward * (wheel * zoomStep * zoomMultiplier);

            next.x = Mathf.Clamp(next.x, -MapHalfWidth, MapHalfWidth);
            next.z = Mathf.Clamp(next.z, -MapHalfDepth, MapHalfDepth);
            float nextGround = PrototypeBootstrap.SampleGroundHeight(next.x, next.z);
            next.y = Mathf.Clamp(next.y, nextGround + minGroundClearance, maxHeight);
            transform.position = next;
        }

        Vector3 p = transform.position;
        p.x = Mathf.Clamp(p.x, -MapHalfWidth, MapHalfWidth);
        p.z = Mathf.Clamp(p.z, -MapHalfDepth, MapHalfDepth);

        float ground = PrototypeBootstrap.SampleGroundHeight(p.x, p.z);
        p.y = Mathf.Clamp(p.y, ground + minGroundClearance, maxHeight);
        transform.position = p;
    }
}
