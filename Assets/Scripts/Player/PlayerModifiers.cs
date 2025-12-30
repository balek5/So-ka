using UnityEngine;

/// <summary>
/// Runtime-only modifiers applied to the player by tomes, items, etc.
/// Keep all upgrades here (do NOT mutate ScriptableObject assets at runtime).
/// </summary>
public sealed class PlayerModifiers : MonoBehaviour
{
    [Header("Multipliers")]
    [Tooltip("Multiplies all outgoing damage (1 = no change).")]
    public float damageMultiplier = 1f;

    [Tooltip("Multiplies attack speed (1 = no change).")]
    public float attackSpeedMultiplier = 1f;

    [Tooltip("Multiplies cooldowns (1 = no change). < 1 means faster cooldown.")]
    public float cooldownMultiplier = 1f;

    [Tooltip("Multiplies projectile size / generic spell size (1 = no change).")]
    public float sizeMultiplier = 1f;

    [Tooltip("XP gain multiplier (1 = no change).")]
    public float xpGainMultiplier = 1f;

    [Header("Flat / additive")]
    [Tooltip("Adds extra projectiles to any spell cast (0 = none).")]
    public int bonusProjectiles;

    public void ResetToDefaults()
    {
        damageMultiplier = 1f;
        attackSpeedMultiplier = 1f;
        cooldownMultiplier = 1f;
        sizeMultiplier = 1f;
        xpGainMultiplier = 1f;
        bonusProjectiles = 0;
    }
}
