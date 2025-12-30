using UnityEngine;

public enum TomeType
{
    AttackSpeed,

    Damage,

    // Spell shaping
    ProjectileCount,
    Size,

    // Progression
    XPGain,

    // Special
    Chaos,
}

[CreateAssetMenu(fileName = "NewTome", menuName = "Tomes/Tome")]
public class Tome : ScriptableObject
{
    public string tomeName;
    [Range(0f, 1f)]
    public float percentIncrease; // e.g., 0.1 = +10%
    public TomeType type;
}