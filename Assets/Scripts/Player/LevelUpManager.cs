using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds and resolves level-up choices (2 spells max, 2 tomes max).
/// Spells/tomes already owned can be upgraded; new ones only appear if slots available.
/// </summary>
public sealed class LevelUpManager : MonoBehaviour
{
    public static LevelUpManager Instance;

    [Header("Catalog")]
    public List<Spell> allSpells = new List<Spell>();
    public List<Tome> allTomes = new List<Tome>();

    [Header("UI")]
    public LevelUpUI ui;

    [Header("Choice Settings")]
    public int choicesToShow = 3;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void ShowLevelUp(PlayerProgression player)
    {
        if (player == null) return;
        if (ui == null)
        {
            Debug.LogError("LevelUpManager: Missing UI reference.");
            return;
        }

        var loadout = player.GetComponent<PlayerLoadout>();
        if (loadout == null) loadout = player.gameObject.AddComponent<PlayerLoadout>();

        List<LevelUpChoice> pool = BuildChoicePool(loadout);
        if (pool.Count == 0)
        {
            ui.Hide();
            return;
        }

        List<LevelUpChoice> picks = PickUnique(pool, choicesToShow);
        ui.Show(picks, choice => ApplyChoice(player, loadout, choice));
    }

    private List<LevelUpChoice> BuildChoicePool(PlayerLoadout loadout)
    {
        var pool = new List<LevelUpChoice>();

        // Upgrades for owned spells/tomes always available.
        for (int i = 0; i < loadout.spells.Count; i++)
        {
            var e = loadout.spells[i];
            if (e != null && e.spell != null)
                pool.Add(LevelUpChoice.UpgradeSpell(e.spell));
        }
        for (int i = 0; i < loadout.tomes.Count; i++)
        {
            var e = loadout.tomes[i];
            if (e != null && e.tome != null)
                pool.Add(LevelUpChoice.UpgradeTome(e.tome));
        }

        // New spells only if space.
        if (loadout.CanAddSpell())
        {
            for (int i = 0; i < allSpells.Count; i++)
            {
                var s = allSpells[i];
                if (s != null && !loadout.HasSpell(s))
                    pool.Add(LevelUpChoice.NewSpell(s));
            }
        }

        // New tomes only if space.
        if (loadout.CanAddTome())
        {
            for (int i = 0; i < allTomes.Count; i++)
            {
                var t = allTomes[i];
                if (t != null && !loadout.HasTome(t))
                    pool.Add(LevelUpChoice.NewTome(t));
            }
        }

        return pool;
    }

    private static List<LevelUpChoice> PickUnique(List<LevelUpChoice> source, int count)
    {
        var result = new List<LevelUpChoice>(count);
        if (source == null || source.Count == 0) return result;

        count = Mathf.Clamp(count, 1, source.Count);

        // Shuffle indices, then take first N
        var indices = new List<int>(source.Count);
        for (int i = 0; i < source.Count; i++) indices.Add(i);

        for (int i = 0; i < indices.Count; i++)
        {
            int j = Random.Range(i, indices.Count);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }

        for (int k = 0; k < count; k++)
            result.Add(source[indices[k]]);

        return result;
    }

    private void ApplyChoice(PlayerProgression player, PlayerLoadout loadout, LevelUpChoice choice)
    {
        if (player == null || loadout == null || choice == null) return;

        var spells = player.GetComponent<PlayerSpells>();

        switch (choice.kind)
        {
            case LevelUpChoiceKind.NewSpell:
                if (choice.spell == null) break;
                if (!loadout.CanAddSpell()) break;

                loadout.AddSpell(choice.spell);

                // Equip into PlayerSpells (use only first 2 indices)
                if (spells != null)
                {
                    for (int i = 0; i < Mathf.Min(2, spells.equippedSpells.Length); i++)
                    {
                        if (spells.equippedSpells[i] == null)
                        {
                            spells.equippedSpells[i] = choice.spell;
                            break;
                        }
                    }
                }
                break;

            case LevelUpChoiceKind.UpgradeSpell:
                if (choice.spell == null) break;
                loadout.UpgradeSpell(choice.spell);
                break;

            case LevelUpChoiceKind.NewTome:
                if (choice.tome == null) break;
                if (!loadout.CanAddTome()) break;

                loadout.AddTome(choice.tome);
                ApplyTomeOnce(player, choice.tome);
                break;

            case LevelUpChoiceKind.UpgradeTome:
                if (choice.tome == null) break;
                loadout.UpgradeTome(choice.tome);
                ApplyTomeOnce(player, choice.tome);
                break;
        }

        ui.Hide();
    }

    private static void ApplyTomeOnce(PlayerProgression player, Tome tome)
    {
        if (player == null || tome == null) return;

        var mods = player.GetComponent<PlayerModifiers>();
        if (mods == null) mods = player.gameObject.AddComponent<PlayerModifiers>();

        float inc = tome.percentIncrease;
        switch (tome.type)
        {
            case TomeType.AttackSpeed:
                player.attackSpeed *= 1 + inc;
                break;
            case TomeType.Damage:
                mods.damageMultiplier *= 1 + inc;
                break;
            case TomeType.ProjectileCount:
                mods.bonusProjectiles += 1;
                break;
            case TomeType.Size:
                mods.sizeMultiplier *= 1 + inc;
                break;
            case TomeType.XPGain:
                mods.xpGainMultiplier *= 1 + inc;
                break;
            case TomeType.Chaos:
                mods.damageMultiplier *= 1 + (inc * 2.5f);
                mods.attackSpeedMultiplier *= 1 + (inc * 1.5f);
                mods.sizeMultiplier *= 1 + (inc * 1.5f);
                mods.cooldownMultiplier *= Mathf.Max(0.05f, 1f - (inc * 0.75f));
                mods.bonusProjectiles += 2;
                mods.xpGainMultiplier *= 1 + (inc * 2f);
                break;
        }

        player.UpdateUI();
    }
}
