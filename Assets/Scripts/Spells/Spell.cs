using UnityEngine;

[CreateAssetMenu(fileName = "NewSpell", menuName = "Megabonk/Spell")]
public class Spell : ScriptableObject
{
    public string spellName;
    public GameObject spellPrefab;
    public float cooldown = 2f;
    public float speed = 10f;
    public int baseDamage = 10;
    public int level = 1;
    public bool isMelee = false;

    [Header("Spawn / Targeting")]
    [Tooltip("If true, the prefab is spawned on the enemy position (katana slash VFX, debuffs, etc.).")]
    public bool spawnOnTarget = false;

    // New features
    public int projectileCount = 1;   // How many projectiles spawn per cast
    public float projectileSpreadAngle = 15f; // Spread if more than 1 projectile
    public float sizeMultiplier = 1f; // Scales projectile size
    public bool homing = false;       // Whether projectiles rotate to follow enemy
}