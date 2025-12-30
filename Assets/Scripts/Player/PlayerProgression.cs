using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PlayerProgression : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Leveling")]
    public int level = 1;
    public int currentXP = 0;
    public int xpToNextLevel = 50;

    [Header("Currency")]
    public int coins = 0;

    [Header("Player Stats")]
    public float moveSpeed = 12f;
    public float attackSpeed = 1f;   
    public int spellSlots = 4;
    public float critChance = 0.1f;
    public float critMultiplier = 1.5f;

    public float spellDamageMultiplier = 1f;
    public float spellCooldownMultiplier = 1f;

    [Header("UI")]
    public Slider healthBar;
    public Slider xpBar;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI coinText;

    private int pendingLevelUps = 0;
    private bool isShowingLevelUpUI = false;

    private PlayerModifiers modifiers;

    void Start()
    {
        currentHealth = maxHealth;
        modifiers = GetComponent<PlayerModifiers>();
        if (modifiers == null) modifiers = gameObject.AddComponent<PlayerModifiers>();
        UpdateUI();
    }

    public void GainXP(int amount)
    {
        if (amount <= 0) return;

        float xpMult = modifiers != null ? modifiers.xpGainMultiplier : 1f;
        xpMult = Mathf.Max(0f, xpMult);
        int finalAmount = Mathf.RoundToInt(amount * xpMult);

        currentXP += finalAmount;
        UpdateUI();

        while (currentXP >= xpToNextLevel)
        {
            currentXP -= xpToNextLevel;
            pendingLevelUps++;
            xpToNextLevel = Mathf.RoundToInt(xpToNextLevel * 1.3f);
        }

        if (!isShowingLevelUpUI && pendingLevelUps > 0)
            StartCoroutine(ProcessLevelUps());
    }

    private IEnumerator ProcessLevelUps()
    {
        isShowingLevelUpUI = true;

        while (pendingLevelUps > 0)
        {
            pendingLevelUps--;
            level++;
            UpdateUI();

            // Prefer the new loadout-based level-up flow.
            if (LevelUpManager.Instance != null)
            {
                LevelUpManager.Instance.ShowLevelUp(this);
                while (LevelUpManager.Instance.ui != null && LevelUpManager.Instance.ui.IsActive())
                    yield return null;
            }
            else if (TomeManager.Instance != null)
            {
                // Fallback to old tome-only UI
                TomeManager.Instance.ShowLevelUpChoices();
                while (TomeManager.Instance.IsUIActive())
                    yield return null;
            }

            yield return null;
        }

        isShowingLevelUpUI = false;
    }

    public void GainCoins(int amount)
    {
        coins += amount;
        UpdateUI();
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        UpdateUI();

        if (currentHealth <= 0)
            Die();
    }

    public void Heal(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (healthBar != null)
            healthBar.value = (float)currentHealth / maxHealth;
        if (xpBar != null)
            xpBar.value = (float)currentXP / xpToNextLevel;
        if (levelText != null)
            levelText.text = "Lvl " + level;
        if (coinText != null)
            coinText.text = coins.ToString();
    }

    void Die()
    {
        Debug.Log("Player Died!");
        Destroy(gameObject);
    }
}
