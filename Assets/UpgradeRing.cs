using UnityEngine;

public class UpgradeRing : MonoBehaviour
{
    public float lifetime = 3f;
    private float timer;
    
    private UpgradeStatue statue;

    public void Init(UpgradeStatue parentStatue)
    {
        statue = parentStatue;
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= lifetime)
        {
            statue.RingMissed();
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            statue.RingHit();
            Destroy(gameObject);
        }
    }


}