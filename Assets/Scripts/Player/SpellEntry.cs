using UnityEngine;

/// <summary>
/// Runtime entry for an owned spell (asset + level).
/// </summary>
[System.Serializable]
public sealed class SpellEntry
{
    public Spell spell;
    [Min(1)]
    public int level = 1;
}

