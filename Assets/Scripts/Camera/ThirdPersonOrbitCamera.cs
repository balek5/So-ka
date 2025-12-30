using UnityEngine;

/// <summary>
/// Simple third-person orbit camera:
/// - Orbits around a target (yaw/pitch)
/// - Zoom with scroll wheel
/// - Avoids clipping through geometry via raycast
///
/// Works with any input; by default uses Mouse X/Y + Mouse ScrollWheel.
/// If you use the new Input System, you can replace the input reads.
/// </summary>
[DefaultExecutionOrder(100)]
public class ThirdPersonOrbitCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public Vector3 targetOffset = new Vector3(0f, 1.6f, 0f);

    [Header("Rotation")]
    public float yawSpeed = 240f;
    public float pitchSpeed = 160f;
    public float minPitch = -30f;
    public float maxPitch = 70f;
    public bool invertY = false;

    [Header("Distance")]
    public float distance = 6f;
    public float minDistance = 2f;
    public float maxDistance = 10f;
    public float zoomSpeed = 4f;

    [Header("Smoothing")]
    public float positionSmoothTime = 0.06f;
    public float rotationLerp = 18f;

    [Header("Collision")]
    public LayerMask collisionMask = ~0; // set to environment only; exclude Roof if needed
    public float cameraRadius = 0.2f;
    public float collisionBuffer = 0.12f;

    [Header("Input")]
    [Tooltip("If enabled, the camera only reads mouse input while the cursor is locked.")]
    public bool requireCursorLockForLook = true;

    [Tooltip("Lock/hide cursor automatically on play.")]
    public bool autoLockCursor = true;

    [Tooltip("Use unscaled time for camera updates. Disable if you don't use timeScale changes.")]
    public bool useUnscaledTime = false;

    private float _yaw;
    private float _pitch;
    private Vector3 _posVel;

    private void Reset()
    {
        // Common default: collide with everything except Ignore Raycast
        collisionMask = Physics.DefaultRaycastLayers;
    }

    private void Awake()
    {
        if (autoLockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void Start()
    {
        if (target != null)
        {
            Vector3 toCam = transform.position - (target.position + targetOffset);
            distance = Mathf.Clamp(toCam.magnitude, minDistance, maxDistance);
            if (toCam.sqrMagnitude > 0.0001f)
            {
                Quaternion r = Quaternion.LookRotation(toCam.normalized);
                _yaw = r.eulerAngles.y;
                _pitch = r.eulerAngles.x;
                if (_pitch > 180f) _pitch -= 360f;
            }
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        bool canLook = true;
        if (requireCursorLockForLook)
            canLook = Cursor.lockState == CursorLockMode.Locked;

        // Basic mouse input (old input manager). Replace if using Input System actions.
        float mx = canLook ? Input.GetAxisRaw("Mouse X") : 0f;
        float my = canLook ? Input.GetAxisRaw("Mouse Y") : 0f;
        float scroll = Input.GetAxisRaw("Mouse ScrollWheel");

        _yaw += mx * yawSpeed * dt;
        _pitch += (invertY ? my : -my) * pitchSpeed * dt;
        _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);

        distance = Mathf.Clamp(distance - scroll * zoomSpeed, minDistance, maxDistance);

        Vector3 focus = target.position + targetOffset;
        Quaternion rot = Quaternion.Euler(_pitch, _yaw, 0f);
        Vector3 desiredPos = focus + rot * (Vector3.back * distance);

        // Collision: sphere cast from focus to desired camera position.
        Vector3 dir = desiredPos - focus;
        float len = dir.magnitude;
        if (len > 0.0001f)
        {
            dir /= len;
            if (Physics.SphereCast(focus, cameraRadius, dir, out RaycastHit hit, len + collisionBuffer, collisionMask, QueryTriggerInteraction.Ignore))
            {
                desiredPos = hit.point - dir * (cameraRadius + collisionBuffer);
            }
        }

        // Smooth position
        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref _posVel, positionSmoothTime, Mathf.Infinity, dt);

        // Smooth look
        Quaternion desiredRot = Quaternion.LookRotation((focus - transform.position).normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, 1f - Mathf.Exp(-rotationLerp * dt));
    }
}
