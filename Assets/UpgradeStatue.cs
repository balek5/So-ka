using UnityEngine;
using System.Collections.Generic;

public class UpgradeStatue : MonoBehaviour
{
    [Header("Ring Settings")]
    public GameObject ringPrefab;
    public float ringSpawnRadius = 5f;
    public float timeBetweenRings = 1.2f;
    public int ringsNeeded = 3;

    [Header("Upgrade Options")]
    public List<StatueUpgradeOption> upgradeOptions; // options to show in UI

    private bool playerInRange = false;
    private float ringTimer;
    private int progress;
    private GameObject activeRing;

    public void Update()
    {
        if (!playerInRange) return;

        ringTimer += Time.deltaTime;
        if (ringTimer >= timeBetweenRings && activeRing == null)
        {
            SpawnRing();
            ringTimer = 0f;
        }
    }

    void SpawnRing()
    {
        Vector2 pos = Random.insideUnitCircle * ringSpawnRadius;
        Vector3 spawnPos = new Vector3(transform.position.x + pos.x, transform.position.y, transform.position.z + pos.y);

        activeRing = Instantiate(ringPrefab, spawnPos, Quaternion.identity);
        activeRing.GetComponent<UpgradeRing>().Init(this);
    }

    public void RingHit()
    {
        progress++;

        if (progress >= ringsNeeded)
        {
            GiveUpgradeChoices();
            progress = 0;
        }

        activeRing = null;
    }

    public void RingMissed()
    {
        progress = Mathf.Max(0, progress - 1);
        activeRing = null;
    }

    private void GiveUpgradeChoices()
    {
        // Use TomeManager to show the same UI
        if (TomeManager.Instance != null)
            TomeManager.Instance.ShowStatueUpgrades(upgradeOptions);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInRange = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            progress = 0;
            if (activeRing != null) Destroy(activeRing);
        }
    }

    [System.Serializable]
    public class StatueUpgradeOption
    {
        public string optionName;
        public float moveSpeedIncrease;
        public float attackSpeedIncrease;
        public int maxHealthIncrease;
        public float critChanceIncrease;
    }
}
