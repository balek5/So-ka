using UnityEngine;

public class SpellProjectile : MonoBehaviour
{
    public int damage = 10;      // Damage of the projectile
    public float lifetime = 3f;  // Auto-destroy after this time

    void Start()
    {
        Destroy(gameObject, lifetime); // Clean up automatically
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Only damage enemies
        if (collision.gameObject.CompareTag("Enemy"))
        {
            Enemy enemy = collision.gameObject.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
        }

        // Destroy projectile on hit
        Destroy(gameObject);
    }
}