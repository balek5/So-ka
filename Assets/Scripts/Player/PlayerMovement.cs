using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    public float turnSpeed = 15f;
    public float jumpForce = 7f;
    public Transform cameraTransform;

    [Header("Collision")]
    [Tooltip("Layer(s) considered ground for jumping.")]
    public LayerMask groundMask = ~0;

    [Tooltip("Distance checked below the player to consider them grounded.")]
    public float groundCheckDistance = 0.25f;

    private Rigidbody rb;
    private PlayerProgression playerStats;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Reduce tunneling through MeshColliders (walls/corners)
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        playerStats = GetComponent<PlayerProgression>();
        if (playerStats == null)
            Debug.LogError("PlayerProgression not found on player!");
    }

    void FixedUpdate()
    {
        Move();
    }

    void Move()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 input = new Vector3(x, 0, z);

        if (input.magnitude < 0.1f) return;

        // Camera-relative directions
        Vector3 camForward = cameraTransform.forward;
        camForward.y = 0;
        camForward.Normalize();

        Vector3 camRight = cameraTransform.right;
        camRight.y = 0;
        camRight.Normalize();

        Vector3 moveDir = camForward * z + camRight * x;
        moveDir.Normalize();

        // Rotate player smoothly
        Quaternion targetRot = Quaternion.LookRotation(moveDir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.fixedDeltaTime);

        // **Use moveSpeed from PlayerProgression**
        float speed = playerStats != null ? playerStats.moveSpeed : 12f;

        Vector3 move = moveDir * speed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + move);

        // Jump (use raycast grounded check instead of velocity)
        bool grounded = Physics.Raycast(transform.position + Vector3.up * 0.05f, Vector3.down, groundCheckDistance + 0.05f, groundMask, QueryTriggerInteraction.Ignore);
        if (Input.GetButton("Jump") && grounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }
}