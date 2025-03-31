using System.Collections;
using UnityEngine;
using UnityEngine.Localization.Settings;

/// <summary>
///     Enum for the languages that are supported
/// </summary>
public enum Language
{
    de,
    en
}

public static class LanguageManager
{
    /// <summary>
    ///     The key used to save the language in PlayerPrefs
    /// </summary>
    public static readonly string saveKey = "Language";

    /// <summary>
    ///     Returns the currently active language as an enum
    /// </summary>
    /// <returns></returns>
    public static Language GetLanguage()
    {
        if (PlayerPrefs.HasKey(saveKey))
        {
            var language = PlayerPrefs.GetString(saveKey) switch
            {
                "de" => Language.de,
                "en" => Language.en,
                _ => Language.en
            };
            return language;
        }

        return Language.en;
    }

    /// <summary>
    ///     Return the word for the currently used language, eg. "english" or "german"
    /// </summary>
    /// <returns></returns>
    public static string GetLanguageWord()
    {
        if (PlayerPrefs.HasKey(saveKey))
            return PlayerPrefs.GetString(saveKey) switch
            {
                "de" => "german",
                "en" => "english",
                _ => "english"
            };
        return "english";
    }

    /// <summary>
    ///     Return the language key for currently used language, eg. "de-DE" or "en-US"
    /// </summary>
    /// <returns></returns>
    public static string GetLanguageCode()
    {
        if (PlayerPrefs.HasKey(saveKey))
            return PlayerPrefs.GetString(saveKey) switch
            {
                "de" => "de-DE",
                "en" => "en-US",
                _ => "en-US"
            };
        return "en-US";
    }

    /// <summary>
    ///     Returns true if the current language is english
    /// </summary>
    /// <returns></returns>
    public static bool IsEnglish()
    {
        return PlayerPrefs.GetString(saveKey) == "en";
    }

    public static IEnumerator UpdateLocalization()
    {
        yield return LocalizationSettings.InitializationOperation;
        DebugEditor.Log("Setting language to " + GetLanguageCode());
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale(GetLanguageCode());
    }

    public static IEnumerator SetToEnglish()
    {
        yield return LocalizationSettings.InitializationOperation;
        DebugEditor.Log("Setting language to en-US");
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale("en-US");
    }
}