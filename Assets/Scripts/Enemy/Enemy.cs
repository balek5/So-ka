using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    public int health = 50;
    public float moveSpeed = 3f;
    public float stopDistance = 1.5f;
    public float rotationSpeed = 10f;

    public GameObject xpOrbPrefab;
    public GameObject coinPrefab;
    public int xpDrop = 10;
    public int coinDrop = 5;
    private int currentEnemyCount = 0;
    
    [HideInInspector]
    public WaveSpawner spawner;

    private Transform player;

    // NEW: Use NavMeshAgent for pathfinding
    private NavMeshAgent agent;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        // Add or get NavMeshAgent
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
            agent = gameObject.AddComponent<NavMeshAgent>();

        agent.speed = moveSpeed;
        agent.stoppingDistance = stopDistance;
        agent.angularSpeed = rotationSpeed * 20f; // More responsive turning
        agent.acceleration = 12f;
        agent.updateRotation = true;
        agent.updateUpAxis = false;
    }

    void Update()
    {
        if (player == null || agent == null)
            return;

        // Do not call NavMeshAgent APIs unless the agent is on a NavMesh
        if (!agent.isOnNavMesh)
            return;

        // Pathfinding instead of walking into walls
        if (!agent.isStopped)
        {
            agent.SetDestination(player.position);
        }
    }

    public void TakeDamage(int dmg)
    {
        health -= dmg;
        if (health <= 0)
            Die();
    }
    public void EnemyKilled()
    {
        currentEnemyCount--;
    }
    void Die()
    {
        GameObject xpOrb = Instantiate(xpOrbPrefab, transform.position + Vector3.up * 0.7f, Quaternion.identity);
        Pickup xp = xpOrb.GetComponent<Pickup>();
        xp.isXP = true;
        xp.value = xpDrop;
        if (spawner != null)
            spawner.EnemyKilled();

        int coinCount = Random.Range(1, 3);
        for (int i = 0; i < coinCount; i++)
        {
            Vector3 spawnPos = transform.position + Random.insideUnitSphere * 0.5f;
            spawnPos.y = transform.position.y + 0.25f;
            GameObject coin = Instantiate(coinPrefab, spawnPos, Quaternion.identity);
            Pickup pickup = coin.GetComponent<Pickup>();
            if (pickup != null)
            {
                pickup.isXP = false;
                pickup.value = coinDrop / coinCount;
            }
        }
        Destroy(gameObject);
    }
  
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerProgression playerProg = collision.gameObject.GetComponent<PlayerProgression>();
            if (playerProg != null)
                playerProg.TakeDamage(10);
        }
    }
}
