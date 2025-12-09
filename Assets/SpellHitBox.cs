using UnityEngine;

public class SpellHitbox : MonoBehaviour
{
    public int damage = 10;
    public float activeTime = 0.3f;

    private void OnEnable()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = true;
        Invoke(nameof(DisableHitbox), activeTime);
    }

    private void DisableHitbox()
    {
        gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
        }
    }
}