using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;

    [Header("UI (Optional)")]
    public UnityEngine.UI.Slider healthBar; // Assign a UI Slider if you want to show health

    void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();
    }

    // Call this to deal damage
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        UpdateHealthUI();

        if (currentHealth <= 0)
            Die();
    }

    // Heal the player
    public void Heal(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        UpdateHealthUI();
    }

    void Die()
    {
        // For now, just destroy the player
        Debug.Log("Player Died!");
        Destroy(gameObject);
    }

    void UpdateHealthUI()
    {
        if (healthBar != null)
            healthBar.value = (float)currentHealth / maxHealth;
    }
}