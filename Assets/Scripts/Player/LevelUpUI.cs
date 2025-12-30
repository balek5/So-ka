using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Very simple level-up UI that shows N choices in a grid.
/// Each choice button can display an icon + title + description.
/// </summary>
public sealed class LevelUpUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject panel;
    public Transform choiceParent;
    public GameObject choiceButtonPrefab;

    private bool _active;
    private Action<LevelUpChoice> _onPick;

    public bool IsActive() => _active;

    public void Show(List<LevelUpChoice> choices, Action<LevelUpChoice> onPick)
    {
        if (panel == null || choiceParent == null || choiceButtonPrefab == null)
        {
            Debug.LogError("LevelUpUI: Missing UI references.");
            return;
        }

        _onPick = onPick;
        _active = true;
        panel.SetActive(true);

        foreach (Transform t in choiceParent)
            Destroy(t.gameObject);

        if (choices == null) return;

        foreach (var c in choices)
        {
            var btnObj = Instantiate(choiceButtonPrefab, choiceParent);
            var view = btnObj.GetComponent<LevelUpChoiceView>();
            if (view != null)
                view.Bind(c, Pick);
            else
            {
                // Fallback: try to set TMP text if present.
                var txt = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.text = c != null ? c.kind.ToString() : "";
                var b = btnObj.GetComponent<Button>();
                if (b != null)
                {
                    var local = c;
                    b.onClick.AddListener(() => Pick(local));
                }
            }
        }

        Time.timeScale = 0f;
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
        _active = false;
        _onPick = null;
        Time.timeScale = 1f;
    }

    private void Pick(LevelUpChoice choice)
    {
        if (!_active) return;
        _onPick?.Invoke(choice);
    }
}
