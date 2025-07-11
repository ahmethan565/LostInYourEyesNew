using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class QualitySettingToggle : MonoBehaviour
{
    public string settingKey = "QUALITY_SETTING_KEY"; // PlayerPrefs key
    public TextMeshProUGUI settingText;
    public Button leftButton;
    public Button rightButton;

    [Tooltip("Ayarlanabilir seçenekler listesi")]
    public List<string> options;

    private int currentIndex = 0;

    void Awake()
    {
        if (settingText == null) Debug.LogError("Setting Text is not assigned!", this);
        if (leftButton == null) Debug.LogError("Left Button is not assigned!", this);
        if (rightButton == null) Debug.LogError("Right Button is not assigned!", this);
    }

    void OnEnable()
    {
        leftButton.onClick.AddListener(PreviousOption);
        rightButton.onClick.AddListener(NextOption);

        LoadSetting();
        UpdateUI();
    }

    void OnDisable()
    {
        leftButton.onClick.RemoveListener(PreviousOption);
        rightButton.onClick.RemoveListener(NextOption);
    }

    private void UpdateUI()
    {
        if (options == null || options.Count == 0) return;

        settingText.text = options[currentIndex];
        leftButton.interactable = currentIndex > 0;
        rightButton.interactable = currentIndex < options.Count - 1;
    }

    public void PreviousOption()
    {
        if (currentIndex > 0)
        {
            currentIndex--;
            UpdateUI();
        }
    }

    public void NextOption()
    {
        if (currentIndex < options.Count - 1)
        {
            currentIndex++;
            UpdateUI();
        }
    }

    public void SaveSetting()
    {
        PlayerPrefs.SetInt(settingKey, currentIndex);
        Debug.Log($"{settingKey} saved as {options[currentIndex]}");
    }

    public void LoadSetting()
    {
        int savedIndex = PlayerPrefs.GetInt(settingKey, -1);
        if (savedIndex >= 0 && savedIndex < options.Count)
        {
            currentIndex = savedIndex;
        }
    }

    public int GetSelectedIndex()
    {
        return currentIndex;
    }

    public string GetSelectedOption()
    {
        return options[currentIndex];
    }
}
