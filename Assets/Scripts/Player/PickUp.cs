using UnityEngine;

public class Pickup : MonoBehaviour
{
    public int value = 1;           // XP or coin amount
    public bool isXP = true;        // True = XP, False = coin
    public float moveSpeed = 5f;    // How fast it moves toward player
    public float rotateSpeed = 180f; 
    public float bobAmplitude = 0.25f;
    public float bobFrequency = 2f;
    public float attractDistance = 3f;   // only follow player when close

    private Vector3 startPos;
    private Transform player;

    void Start()
    {
        startPos = transform.position;
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void Update()
    {
        if (player == null) return;

        // Spin
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);

        // Distance to player
        float distance = Vector3.Distance(transform.position, player.position);

        // Only chase player when close enough
        if (distance < attractDistance)
        {
            Vector3 dir = (player.position - transform.position).normalized;
            transform.position += dir * moveSpeed * Time.deltaTime;
        }

        // Bobbing (optional)
        transform.position += Vector3.up * Mathf.Sin(Time.time * bobFrequency) * bobAmplitude * Time.deltaTime;
    }


    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // THIS MUST MATCH YOUR PLAYER SCRIPT NAME
        PlayerProgression progress = other.GetComponent<PlayerProgression>();
        if (progress != null)
        {
            if (isXP)
                progress.GainXP(value);      // Call method in your PlayerProgression
            else
                progress.GainCoins(value);
        }

        Destroy(gameObject);
    }
}