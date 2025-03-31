using UnityEngine;
using UnityEngine.UI;

public class LanguageSelector : MonoBehaviour
{
    public Language thisLanguage;

    private Toggle toggle;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        toggle = GetComponentInChildren<Toggle>();
        if (toggle == null)
        {
            DebugEditor.LogError("Toggle not found in children.");
            return;
        }

        if (toggle.isOn) OnLanguageSelected();

        toggle.onValueChanged.AddListener(delegate
        {
            if (toggle.isOn) OnLanguageSelected();
        });
    }

    private void OnLanguageSelected()
    {
        PlayerPrefs.SetString(LanguageManager.saveKey, thisLanguage.ToString());
        StartCoroutine(LanguageManager.UpdateLocalization());
    }
}