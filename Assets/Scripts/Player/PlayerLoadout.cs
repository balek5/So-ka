using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Owns the player's permanent selections: up to 2 spells and 2 tomes.
/// Also tracks their upgrade levels/stacks.
/// </summary>
public sealed class PlayerLoadout : MonoBehaviour
{
    [Header("Limits")]
    public int maxSpells = 2;
    public int maxTomes = 2;

    [Header("Owned")]
    public List<SpellEntry> spells = new List<SpellEntry>();
    public List<TomeEntry> tomes = new List<TomeEntry>();

    public bool HasSpell(Spell spell)
    {
        if (spell == null) return false;
        for (int i = 0; i < spells.Count; i++)
            if (spells[i] != null && spells[i].spell == spell) return true;
        return false;
    }

    public bool HasTome(Tome tome)
    {
        if (tome == null) return false;
        for (int i = 0; i < tomes.Count; i++)
            if (tomes[i] != null && tomes[i].tome == tome) return true;
        return false;
    }

    public bool CanAddSpell() => spells.Count < maxSpells;
    public bool CanAddTome() => tomes.Count < maxTomes;

    public void AddSpell(Spell spell)
    {
        if (spell == null) return;
        if (!CanAddSpell()) return;
        if (HasSpell(spell)) return;
        spells.Add(new SpellEntry { spell = spell, level = 1 });
    }

    public void AddTome(Tome tome)
    {
        if (tome == null) return;
        if (!CanAddTome()) return;
        if (HasTome(tome)) return;
        tomes.Add(new TomeEntry { tome = tome, stacks = 1 });
    }

    public void UpgradeSpell(Spell spell)
    {
        if (spell == null) return;
        for (int i = 0; i < spells.Count; i++)
        {
            if (spells[i] != null && spells[i].spell == spell)
            {
                spells[i].level++;
                return;
            }
        }
    }

    public void UpgradeTome(Tome tome)
    {
        if (tome == null) return;
        for (int i = 0; i < tomes.Count; i++)
        {
            if (tomes[i] != null && tomes[i].tome == tome)
            {
                tomes[i].stacks++;
                return;
            }
        }
    }
}
