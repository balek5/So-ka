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
        while (choices.Count < 3 && attempts < 40)
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
            if (txt != null)
                txt.text = FormatTomeText(t);

            Tome localTome = t;
            buttonObj.GetComponent<Button>().onClick.AddListener(() =>
            {
                ApplyTome(localTome);
            });
        }
    }

    private static string FormatTomeText(Tome t)
    {
        if (t == null) return string.Empty;

        float mult = 1f + t.percentIncrease;

        switch (t.type)
        {
            case TomeType.Damage:
                return $"{t.tomeName}\nDamage x{mult:0.##}";
            case TomeType.AttackSpeed:
                return $"{t.tomeName}\nAttack Speed x{mult:0.##}";
            case TomeType.Size:
                return $"{t.tomeName}\nSize x{mult:0.##}";
            case TomeType.XPGain:
                return $"{t.tomeName}\nXP Gain x{mult:0.##}";

            case TomeType.ProjectileCount:
                // Your apply logic always gives at least +1.
                return $"{t.tomeName}\n+1 Projectile";

            case TomeType.Chaos:
                return $"{t.tomeName}\nCHAOS (massive boosts)";

            default:
                return $"{t.tomeName}\n+{t.percentIncrease * 100f:0.#}% {t.type}";
        }
    }

    void ApplyTome(Tome tome)
    {
        var playerGo = GameObject.FindGameObjectWithTag("Player");
        PlayerProgression player = playerGo != null ? playerGo.GetComponent<PlayerProgression>() : null;
        if (player == null)
        {
            Debug.LogError("No Player found to apply tome!");
            return;
        }

        PlayerModifiers mods = playerGo.GetComponent<PlayerModifiers>();
        if (mods == null) mods = playerGo.AddComponent<PlayerModifiers>();

        float inc = tome.percentIncrease;

        switch (tome.type)
        {
            case TomeType.AttackSpeed:
                // Base stat (other systems can use it too)
                player.attackSpeed *= 1 + inc;
                break;

            case TomeType.Damage:
                mods.damageMultiplier *= 1 + inc;
                break;

            case TomeType.ProjectileCount:
                mods.bonusProjectiles += Mathf.Max(1, Mathf.RoundToInt(inc * 1f));
                break;

            case TomeType.Size:
                mods.sizeMultiplier *= 1 + inc;
                break;

            case TomeType.XPGain:
                mods.xpGainMultiplier *= 1 + inc;
                break;

            case TomeType.Chaos:
                // Broken tome: big boosts.
                mods.damageMultiplier *= 1 + (inc * 2.5f);
                mods.attackSpeedMultiplier *= 1 + (inc * 1.5f);
                mods.sizeMultiplier *= 1 + (inc * 1.5f);
                mods.cooldownMultiplier *= Mathf.Max(0.05f, 1f - (inc * 0.75f));
                mods.bonusProjectiles += Mathf.Max(2, Mathf.RoundToInt(2 + inc * 3f));
                mods.xpGainMultiplier *= 1 + (inc * 2f);
                break;
        }

        player.UpdateUI();

        // Hide UI
        HideLevelUpUI();
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
            if (txt != null) txt.text = opt.optionName;

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
