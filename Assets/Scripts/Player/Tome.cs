using UnityEngine;

public enum TomeType
{
    MoveSpeed,
    AttackSpeed,

    SpellDamage,
    CritChance,
    ProjectileCount,
    ProjectileSize,
}

[CreateAssetMenu(fileName = "NewTome", menuName = "Megabonk/Tome")]
public class Tome : ScriptableObject
{
    public string tomeName;
    [Range(0f, 1f)]
    public float percentIncrease; // e.g., 0.1 = +10%
    public TomeType type;
}