using UnityEngine;

public enum LevelUpChoiceKind
{
    NewSpell,
    UpgradeSpell,
    NewTome,
    UpgradeTome
}

/// <summary>
/// Represents one selectable choice in the level-up UI.
/// </summary>
public sealed class LevelUpChoice
{
    public LevelUpChoiceKind kind;
    public Spell spell;
    public Tome tome;

    public static LevelUpChoice NewSpell(Spell s) => new LevelUpChoice { kind = LevelUpChoiceKind.NewSpell, spell = s };
    public static LevelUpChoice UpgradeSpell(Spell s) => new LevelUpChoice { kind = LevelUpChoiceKind.UpgradeSpell, spell = s };
    public static LevelUpChoice NewTome(Tome t) => new LevelUpChoice { kind = LevelUpChoiceKind.NewTome, tome = t };
    public static LevelUpChoice UpgradeTome(Tome t) => new LevelUpChoice { kind = LevelUpChoiceKind.UpgradeTome, tome = t };
}

