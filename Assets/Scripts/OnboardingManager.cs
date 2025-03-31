using System;
using System.Collections.Generic;
using System.Linq;
using i5.LLM_AR_Tourguide;
using i5.LLM_AR_Tourguide.Gemini;
using i5.LLM_AR_Tourguide.Guide_Scripts;
using i5.LLM_AR_Tourguide.Prefab_Scripts;
using i5.LLM_AR_Tourguide.UI_Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Uralstech.UGemini.Models.Content;

public class OnboardingManager : MonoBehaviour
{
    public PossibleAnswerPanel possibleAnswerPrefab;

    public Transform optionsPanel;
    public TMP_InputField answerTextField;

    // words that the AI might use the write a button for a custom response, the last possible answer will always be the custom answer button in other languages
    private readonly string[] customWordsButton = { "specify", "other", "custom", "alternative", "something else" };

    private readonly int maxNumberOfUserAnswears = 3;
    private readonly List<PossibleAnswerPanel> possibleAnswerPanels = new();
    private int currentNumberOfUserAnswers;
    private GuideManager guideManager;
    private OnboardingChatManager onboardingChatManager;


    private OnboardingUI onboardingUI;
    private PointOfInterestManager poiM;

    public void Start()
    {
        onboardingUI = FindAnyObjectByType<OnboardingUI>();
        guideManager = FindAnyObjectByType<GuideManager>();
        onboardingChatManager = GetComponent<OnboardingChatManager>();
        poiM = GetComponent<PointOfInterestManager>();
    }

    public async void OnboardingAnswerQuestionAsync()
    {
        var userInformation = GetComponent<UserInformation>();
        userInformation.UpdatePersonalization();
        var languageText = "";
        if (!LanguageManager.IsEnglish()) languageText = "Only answer in " + LanguageManager.GetLanguageWord() + "!";
        onboardingChatManager.SetRole(4);
        var userInformationString = userInformation.stringifyUserInformation();
        var systemPromptMessage =
            "You are a personal tour guide in Aachen. A tour always consists of the following points of interest: " +
            poiM.getAllPOIsAsString() + ". \\r\\n Follow these rules and description: " +
            guideManager.GetChosenGuideInfo().longDescription
            + languageText
            + " The user has given you the following information about themself: " + userInformationString +
            " Try to incorporate this information if possible.";
        var systemPrompt = await onboardingChatManager.OnChat(systemPromptMessage);

        onboardingChatManager.SetRole((int)GeminiRole.User);
        var response2 = await onboardingChatManager.OnChat("Your guide is getting ready...",
            $"Before the start of a tour, you have to ask the participant some questions about who he or she is and what they are interested in. Try to ask specific questions and give examples of possible answers to make it easier for the participant, but keep it short and only ask one question per message. The user has {maxNumberOfUserAnswears} opportunities to respond. Keep your messages short. Use as much text formating as possible like this **bold** *italic*. Your next message will be shown directly to the user. After each message provide possible options that the user could use to answer in the following schema:\r\n\r\n[First option; Second option; Third option; etc...] Give the user the option to specify a custom answer. " +
            languageText);

        if (response2 is { Length: > 1 })
        {
            response2[0].gameObject.SetActive(false);
            UpdatePossibleAnswers(response2[1]);
        }
        else
        {
            response2[0].SetText("Something went wrong. Please check your connection and restart the app.");
            UpdatePossibleAnswers(null, "[Restart the app]");

            foreach (var panel in possibleAnswerPanels)
            {
                panel.GetComponent<Button>().onClick.RemoveAllListeners();
                panel.GetComponent<Button>().onClick.AddListener(Application.Quit);
            }
        }
    }

    public void OnCustomAnswerSent()
    {
        if (!string.IsNullOrEmpty(answerTextField.text))
            OnAnswerOnboardingQuestionAsync(answerTextField.text);
        answerTextField.text = "";
    }

    public async void OnAnswerOnboardingQuestionAsync(string userAnswer)
    {
        foreach (var option in possibleAnswerPanels) option.DeactivateButton();
        var languageText = "";
        if (!LanguageManager.IsEnglish()) languageText = "Only answer in " + LanguageManager.GetLanguageWord() + "!";

        if (string.IsNullOrEmpty(userAnswer))
            return;
        var modifiedAnswer =
            $"{currentNumberOfUserAnswers} out of {maxNumberOfUserAnswears} User responses are used up. Remember to not get into to much detail and ask general question to get to know the user and to keep the messages short. " +
            languageText + " The user responded: " +
            userAnswer;
        if (currentNumberOfUserAnswers >= maxNumberOfUserAnswears - 1)
            modifiedAnswer =
                "This is the users last response. To the end of your tour add a button in the schema like [X; Y] where X and Y represent sayings to start the tour. Ony include buttons to start. Keep the message short. The user responded: " +
                userAnswer;
        var response = await onboardingChatManager.OnChat(userAnswer, modifiedAnswer);
        foreach (var option in possibleAnswerPanels) option.ActivateButton();
        if (response[1])
        {
            UpdatePossibleAnswers(response[1]);

            if (currentNumberOfUserAnswers >= maxNumberOfUserAnswears - 1)
                foreach (var panel in possibleAnswerPanels)
                {
                    panel.GetComponent<Button>().onClick.RemoveAllListeners();
                    panel.GetComponent<Button>().onClick.AddListener(finishOnboardingDialog);
                }

            //DebugEditor.Log(currentNumberOfUserAnswers);
            //DebugEditor.Log(maxNumberOfUserAnswears);
            currentNumberOfUserAnswers++;
        }
    }

    public async void finishOnboardingDialog()
    {
        onboardingChatManager.readOutResponses = false;
        var summery = await onboardingChatManager.OnChat(
            "",
            "The user finished the onboarding. Write a bullet point summery of what you know about the user in regards to the tour. This summery will be the only thing that you remember about the user for the tour. This message is only for yourself and only you will see it. Write it as if it was a short note for yourself, only include the summery.");
        var summeryText = summery[1].GetCompleteText();
        DebugEditor.Log("Summery: " + summeryText);

        var userInformation = GetComponent<UserInformation>();
        userInformation.userSummery = summeryText;
        userInformation.UpdatePersonalization();
        // string chatHistory = onboardingChatManager.GetChatHistory();
        // JSONChatManager jsonChatManager = FindAnyObjectByType<JSONChatManager>();
        // string jsonChatResponse = await jsonChatManager.OnChat(chatHistory);
        // DebugEditor.Log("Parsed Json:" + jsonChatResponse);

        //SaveDataUserInformation extractedData = JsonUtility.FromJson<SaveDataUserInformation>(jsonChatResponse);
        //UserInformation userInformation = GetComponent<UserInformation>();
        //userInformation.AddToUserInformation(extractedData);


        onboardingUI.ShowNextDialog();
    }

    public void UpdatePossibleAnswers(MessageBlock input, string replaceString = null)
    {
        var startIndex = 0;
        var options = new string[0];
        if (input)
        {
            options = ExtractOptions(input.GetCompleteText());
            foreach (var option in possibleAnswerPanels) Destroy(option.gameObject);
            possibleAnswerPanels.Clear();
            startIndex = input.GetCompleteText().IndexOf('[') + 1;
        }
        else if (!string.IsNullOrEmpty(replaceString))
        {
            options = ExtractOptions(replaceString);
            startIndex = replaceString.IndexOf('[') + 1;
        }

        if (startIndex <= 0)
        {
            optionsPanel.gameObject.SetActive(false);
            return;
        }

        optionsPanel.gameObject.SetActive(true);
        if (input) input.SetText(input.GetCompleteText().Substring(0, startIndex - 1));
        PossibleAnswerPanel lastAnswer = null;
        var isEnglish = LanguageManager.IsEnglish();
        foreach (var o in options)
        {
            var prefab = Instantiate(possibleAnswerPrefab, optionsPanel);
            prefab.Setup(o);
            if (isEnglish) CheckIfIncludeOtherOption(prefab); // This only works for english words
            possibleAnswerPanels.Add(prefab);
            lastAnswer = prefab;
        }

        // The last answer should always the custom answer most of the time
        if (isEnglish) return;
        if (lastAnswer == null) return;
        lastAnswer.GetComponent<Button>().onClick.RemoveAllListeners();
        lastAnswer.GetComponent<Button>().onClick.AddListener(() => answerTextField.ActivateInputField());
    }

    private void CheckIfIncludeOtherOption(PossibleAnswerPanel panel)
    {
        //DebugEditor.Log("Panel Text: " + panel.GetCompleteText());
        //DebugEditor.Log("Contains \"something else\": " + panel.GetCompleteText().Contains("something else", StringComparison.OrdinalIgnoreCase));
        //DebugEditor.Log("Contains \"specify\": " + panel.GetCompleteText().Contains("specify", StringComparison.OrdinalIgnoreCase));
        //DebugEditor.Log("Contains \"other\": " + panel.GetCompleteText().Contains("other", StringComparison.OrdinalIgnoreCase));
        //DebugEditor.Log("Contains \"custom\": " + panel.GetCompleteText().Contains("custom", StringComparison.OrdinalIgnoreCase));
        //DebugEditor.Log("Contains \"alternative\": " + panel.GetCompleteText().Contains("alternative", StringComparison.OrdinalIgnoreCase));

        // If one of the panel button texts includes any of the customWordsButton, then add a listener to the button that opens the keyboard or microphone
        if (customWordsButton.Any(word => panel.GetText().Contains(word, StringComparison.OrdinalIgnoreCase)))
        {
            //DebugEditor.Log("Adding listener to button: " + panel.GetCompleteText());
            panel.GetComponent<Button>().onClick.RemoveAllListeners();
            panel.GetComponent<Button>().onClick.AddListener(() => answerTextField.ActivateInputField());
        }
    }


    public string[] ExtractOptions(string input)
    {
        var startIndex = input.IndexOf('[') + 1;
        var endIndex = input.IndexOf(']');
        if (startIndex == -1 || endIndex == -1) return new string[0];

        var optionsString = input.Substring(startIndex, endIndex - startIndex);

        var options = optionsString.Split(';');

        for (var i = 0; i < options.Length; i++) options[i] = options[i].Trim();

        return options;
    }
}