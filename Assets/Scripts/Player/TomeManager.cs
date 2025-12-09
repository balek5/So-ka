using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TomeManager : MonoBehaviour
{
    public static TomeManager Instance;

    [Header("UI")]
    public GameObject levelUpPanel;
    public Transform choiceParent;
    public GameObject choiceButtonPrefab;

    [Header("Tomes")]
    public List<Tome> allTomes;

    private bool uiActive = false;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void ShowLevelUpChoices()
    {
        if (uiActive) return;
        if (levelUpPanel == null || choiceParent == null || choiceButtonPrefab == null)
        {
            Debug.LogError("TomeManager: Missing UI references!");
            return;
        }

        if (allTomes == null || allTomes.Count == 0)
        {
            Debug.LogError("TomeManager: No tomes assigned!");
            return;
        }

        uiActive = true;
        levelUpPanel.SetActive(true);

        // Clear old buttons
        foreach (Transform child in choiceParent)
            Destroy(child.gameObject);

        // Pick 3 random unique tomes
        List<Tome> choices = new List<Tome>();
        int attempts = 0;
        while (choices.Count < 3 && attempts < 20)
        {
            attempts++;
            Tome t = allTomes[Random.Range(0, allTomes.Count)];
            if (!choices.Contains(t))
                choices.Add(t);
        }

        foreach (Tome t in choices)
        {
            GameObject buttonObj = Instantiate(choiceButtonPrefab, choiceParent);
            TextMeshProUGUI txt = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            txt.text = $"{t.tomeName}\n+{t.percentIncrease * 100}% {t.type}";

            Tome localTome = t;
            buttonObj.GetComponent<Button>().onClick.AddListener(() =>
            {
                ApplyTome(localTome);
            });
        }
    }

void ApplyTome(Tome tome)
{
    PlayerProgression player = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerProgression>();
    if (player == null)
    {
        Debug.LogError("No Player found to apply tome!");
        return;
    }

    switch (tome.type)
    {
        case TomeType.MoveSpeed:
            player.moveSpeed *= 1 + tome.percentIncrease;
            break;
        case TomeType.AttackSpeed:
            player.attackSpeed *= 1 + tome.percentIncrease;
            break;
       
        case TomeType.SpellDamage:
            player.spellDamageMultiplier *= 1 + tome.percentIncrease;
            break;
      
        case TomeType.CritChance:
            player.critChance += tome.percentIncrease;
            break;

        // New spell tomes
        case TomeType.ProjectileCount:
            foreach (var spell in player.GetComponent<PlayerSpells>().equippedSpells)
            {
                if (spell != null)
                    spell.projectileCount += Mathf.Max(1, Mathf.RoundToInt(spell.projectileCount * tome.percentIncrease));
            }
            break;
        case TomeType.ProjectileSize:
            foreach (var spell in player.GetComponent<PlayerSpells>().equippedSpells)
            {
                if (spell != null)
                    spell.sizeMultiplier *= 1 + tome.percentIncrease;
            }
            break;
        }

    // Hide UI
    TomeManager.Instance.HideLevelUpUI();
}
public void ShowStatueUpgrades(List<UpgradeStatue.StatueUpgradeOption> options)
{
    if (uiActive) return;

    uiActive = true;
    levelUpPanel.SetActive(true);

    // Clear old buttons
    foreach (Transform child in choiceParent)
        Destroy(child.gameObject);

    // Create buttons for each option
    foreach (var opt in options)
    {
        GameObject btnObj = Instantiate(choiceButtonPrefab, choiceParent);
        TMP_Text txt = btnObj.GetComponentInChildren<TMP_Text>();
        txt.text = opt.optionName;

        UpgradeStatue.StatueUpgradeOption localOpt = opt;
        btnObj.GetComponent<Button>().onClick.AddListener(() =>
        {
            ApplyStatueUpgrade(localOpt);
        });
    }
}

private void ApplyStatueUpgrade(UpgradeStatue.StatueUpgradeOption option)
{
    PlayerProgression player = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerProgression>();
    if (player == null)
    {
        Debug.LogError("No Player found to apply upgrade!");
        return;
    }

    player.moveSpeed += option.moveSpeedIncrease;
    player.attackSpeed += option.attackSpeedIncrease;
    player.maxHealth += option.maxHealthIncrease;
    player.currentHealth += option.maxHealthIncrease;
    player.critChance += option.critChanceIncrease;

    player.UpdateUI();

    HideLevelUpUI();
}

public void HideLevelUpUI()
{
    if (levelUpPanel != null)
        levelUpPanel.SetActive(false);

    uiActive = false;
}

    public bool IsUIActive()
    {
        return uiActive;
    }
    
    
}
