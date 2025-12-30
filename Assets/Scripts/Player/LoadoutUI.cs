using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class LoadoutUI : MonoBehaviour
{
    public PlayerLoadout loadout;

    [Header("Spell Slots")]
    public Image spellSlot1;
    public Image spellSlot2;
    public TextMeshProUGUI spellText1;
    public TextMeshProUGUI spellText2;

    [Header("Tome Slots")]
    public Image tomeSlot1;
    public Image tomeSlot2;
    public TextMeshProUGUI tomeText1;
    public TextMeshProUGUI tomeText2;

    [Header("Fallback Sprites")]
    public Sprite emptySprite;
    public Sprite defaultSpellSprite;
    public Sprite defaultTomeSprite;

    private void Update()
    {
        if (loadout == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) loadout = p.GetComponent<PlayerLoadout>();
        }

        if (loadout == null) return;

        SetSpellSlot(0, spellSlot1, spellText1);
        SetSpellSlot(1, spellSlot2, spellText2);
        SetTomeSlot(0, tomeSlot1, tomeText1);
        SetTomeSlot(1, tomeSlot2, tomeText2);
    }

    private void SetSpellSlot(int index, Image img, TextMeshProUGUI txt)
    {
        if (index >= loadout.spells.Count || loadout.spells[index] == null || loadout.spells[index].spell == null)
        {
            if (img != null) img.sprite = emptySprite;
            if (txt != null) txt.text = string.Empty;
            return;
        }

        var e = loadout.spells[index];
        if (img != null) img.sprite = defaultSpellSprite != null ? defaultSpellSprite : emptySprite;
        if (txt != null) txt.text = $"{e.spell.spellName} Lv.{e.level}";
    }

    private void SetTomeSlot(int index, Image img, TextMeshProUGUI txt)
    {
        if (index >= loadout.tomes.Count || loadout.tomes[index] == null || loadout.tomes[index].tome == null)
        {
            if (img != null) img.sprite = emptySprite;
            if (txt != null) txt.text = string.Empty;
            return;
        }

        var e = loadout.tomes[index];
        if (img != null) img.sprite = defaultTomeSprite != null ? defaultTomeSprite : emptySprite;
        if (txt != null) txt.text = $"{e.tome.tomeName} x{e.stacks}";
    }
}
