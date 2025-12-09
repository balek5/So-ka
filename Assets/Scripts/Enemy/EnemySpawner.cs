using UnityEngine;
using System.Collections;

public class WaveSpawner : MonoBehaviour
{
    [Header("Enemies")] public GameObject[] enemyPrefabs;
    public Transform player;
    public LayerMask groundLayer;

    [Header("Wave Settings")] public int baseEnemiesPerWave = 5;
    public float spawnRadius = 15f;
    public float minDistanceFromPlayer = 5f;
    public float timeBetweenWaves = 3f;
    public int maxEnemies = 50;

    [Header("Difficulty Scaling")] public float enemySpeedMultiplier = 0.1f;
    public float enemyHealthMultiplier = 0.1f;
    public float spawnAcceleration = 0.05f;

    private int currentEnemyCount = 0;
    private int currentWave = 0;
    private float currentSpawnDelay = 0.3f;

    void Start()
    {
        StartCoroutine(WaveRoutine());
    }

    IEnumerator WaveRoutine()
    {
        while (true)
        {
            currentWave++;
            int enemiesToSpawn = Mathf.Min(baseEnemiesPerWave + currentWave * 2, maxEnemies);
            float spawnDelay = Mathf.Max(0.05f, currentSpawnDelay - spawnAcceleration * currentWave);

            for (int i = 0; i < enemiesToSpawn; i++)
            {
                SpawnEnemy();
                yield return new WaitForSeconds(spawnDelay);
            }

            yield return new WaitForSeconds(timeBetweenWaves);
        }
    }

    public void EnemyKilled()
    {
        currentEnemyCount--;
    }

    void SpawnEnemy()
    {
        if (currentEnemyCount >= maxEnemies) return;

        for (int tries = 0; tries < 10; tries++)
        {
            Vector2 randomCircle = Random.insideUnitCircle.normalized *
                                   Random.Range(minDistanceFromPlayer, spawnRadius);
            Vector3 spawnPosAbove = player.position + new Vector3(randomCircle.x, 50f, randomCircle.y);

            if (Physics.Raycast(spawnPosAbove, Vector3.down, out RaycastHit hit, 100f, groundLayer))
            {
                // Raise the spawn to the agent's base height (0.5–1.0 usually works)
                const float spawnHeightOffset = 1.15f; // tweak as needed
                Vector3 spawnPos = hit.point + Vector3.up * spawnHeightOffset;

                GameObject enemyPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
                GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
                currentEnemyCount++;

                Enemy enemyScript = enemy.GetComponent<Enemy>();
                if (enemyScript != null)
                {
                    enemyScript.spawner = this;
                    enemyScript.moveSpeed += enemyScript.moveSpeed * enemySpeedMultiplier * currentWave;
                    enemyScript.health += Mathf.RoundToInt(enemyScript.health * enemyHealthMultiplier * currentWave);
                }

                return;
            }
        }
    }
}