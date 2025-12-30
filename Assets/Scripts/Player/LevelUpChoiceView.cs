using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI component meant to live on the choice button prefab.
/// </summary>
public sealed class LevelUpChoiceView : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI title;
    public TextMeshProUGUI description;
    public Button button;

    [Header("Fallback Sprites")]
    public Sprite defaultSpellSprite;
    public Sprite defaultTomeSprite;

    private LevelUpChoice _choice;
    private Action<LevelUpChoice> _onPick;

    public void Bind(LevelUpChoice choice, Action<LevelUpChoice> onPick)
    {
        _choice = choice;
        _onPick = onPick;

        if (button == null) button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => _onPick?.Invoke(_choice));
        }

        ApplyText();
    }

    private void ApplyText()
    {
        if (_choice == null)
        {
            if (title != null) title.text = "";
            if (description != null) description.text = "";
            return;
        }

        switch (_choice.kind)
        {
            case LevelUpChoiceKind.NewSpell:
                if (title != null) title.text = _choice.spell != null ? _choice.spell.spellName : "New Spell";
                if (description != null) description.text = "Add spell";
                if (icon != null) icon.sprite = defaultSpellSprite;
                break;

            case LevelUpChoiceKind.UpgradeSpell:
                if (title != null) title.text = _choice.spell != null ? _choice.spell.spellName : "Spell";
                if (description != null) description.text = "Upgrade spell";
                if (icon != null) icon.sprite = defaultSpellSprite;
                break;

            case LevelUpChoiceKind.NewTome:
                if (title != null) title.text = _choice.tome != null ? _choice.tome.tomeName : "New Tome";
                if (description != null) description.text = FormatTome(_choice.tome);
                if (icon != null) icon.sprite = defaultTomeSprite;
                break;

            case LevelUpChoiceKind.UpgradeTome:
                if (title != null) title.text = _choice.tome != null ? _choice.tome.tomeName : "Tome";
                if (description != null) description.text = "Upgrade: " + FormatTome(_choice.tome);
                if (icon != null) icon.sprite = defaultTomeSprite;
                break;
        }
    }

    private static string FormatTome(Tome t)
    {
        if (t == null) return string.Empty;
        float mult = 1f + t.percentIncrease;
        switch (t.type)
        {
            case TomeType.Damage:
                return $"Damage x{mult:0.##}";
            case TomeType.AttackSpeed:
                return $"Attack Speed x{mult:0.##}";
            case TomeType.Size:
                return $"Size x{mult:0.##}";
            case TomeType.XPGain:
                return $"XP Gain x{mult:0.##}";
            case TomeType.ProjectileCount:
                return "+1 Projectile";
            case TomeType.Chaos:
                return "CHAOS";
            default:
                return t.type.ToString();
        }
    }
}

