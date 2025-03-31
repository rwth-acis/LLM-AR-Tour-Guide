using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using i5.LLM_AR_Tourguide;
using i5.LLM_AR_Tourguide.Audio;
using i5.LLM_AR_Tourguide.Gemini;
using i5.LLM_AR_Tourguide.Guide_Scripts;
using i5.LLM_AR_Tourguide.Prefab_Scripts;
using i5.LLM_AR_Tourguide.TourGeneration;
using i5.LLM_AR_Tourguide.UI_Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Uralstech.UGemini.Models.Content;

public class InformationController : MonoBehaviour
{
    private const string CurrentInformationIndexKey = "currentInformationIndex";
    private const string MainOnboardingCompleteKey = "mainOnboardingComplete";
    private const string ProgressInPopulationMainInformationKey = "progressInPopulationMainInformation";

    private const string IntroductionText =
        "Each time you fire up the app, you'll be asked to film your surroundings briefly. Just a quick peek at where you are. This helps me to know exactly where in Aachen's twisting streets you stand! Then I can tell you all about the stories that live there.\n" +
        "Down at the bottom, you'll see three tabs. Currently, you're in the **Explore** tab. That's where I reside and where you'll find the current points of interest of our itinerary. The itinerary can be found in the **Tour** tab, there you can also review the information again.\n" +
        "In the **Settings** tab on the right you can change settings and find helpful tips and hints on how to use the app. \nTo start with the tour you can click the **Continue** button below. \nAnd always remember, if you have any questions or want me to present the information differently, **just let me know!**";

    public TextMeshProUGUI titleText;
    [FormerlySerializedAs("POITitle")] public TextMeshProUGUI poiTitle;
    [FormerlySerializedAs("POISubtitle")] public TextMeshProUGUI poiSubtitle;


    [FormerlySerializedAs("_interactionWindowMain")] [SerializeField]
    private Transform interactionWindowMain;

    [FormerlySerializedAs("_interactionWindowPOIDetail")] [SerializeField]
    private Transform interactionWindowPoiDetail;

    [FormerlySerializedAs("_interactionPrefab")] [SerializeField]
    private MessageBlock interactionPrefab;

    [SerializeField] private ChipButtons buttonsUnderMessage;

    [SerializeField] public Button questionSendButton;

    public bool isShowingAnswer;

    public TMP_InputField questionTextField;

    [SerializeField] private TourIntroductionUI _tourIntroductionPrefab;
    [SerializeField] private Button startNavigationButton;

    private readonly List<MessageBlock> _detailTexts = new();
    private int _countOfInitializedMessages;

    private bool _currentlyProcessingQuestion;
    private GoogleGroundingChatManager _groundingChatManager;
    private GuideManager _guideManager;
    private bool _isInMainSummery;

    private bool _mainMenuOnboardingComplete;

    private MessageBlock _mainOnboardingUI;
    private bool _mainSummeryComplete;
    private int _oldIndex = 1000;
    private OnboardingChatManager _onboardingChatManager;

    private OnboardingUI _onboardingUI;

    private PointOfInterestManager _poiM;
    private int _progressInPopulationMainInformation = 1;
    private TextToSpeech _textToSpeech;
    private ChipButtons _theFirstButton;

    private TourIntroductionUI _tourIntroduction;

    private TourSummery _tourSummery;
    private UserInformation _userInformation;

    public int CurrentInformationIndex { get; private set; }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        _poiM = GetComponent<PointOfInterestManager>();
        if (!_poiM) DebugEditor.LogError("PointOfInterestManager is not set in InformationController");

        _groundingChatManager = GetComponent<GoogleGroundingChatManager>();
        _userInformation = GetComponent<UserInformation>();
        LoadProgress();
        if (_textToSpeech == null) _textToSpeech = FindAnyObjectByType<TextToSpeech>();
        //_textToSpeech.SaveToCache(IntroductionText);

        questionSendButton.onClick.AddListener(OnAskQuestionAsync);
    }

    private void LoadProgress()
    {
        _mainMenuOnboardingComplete = PlayerPrefs.GetInt(MainOnboardingCompleteKey, 0) == 1;
        CurrentInformationIndex = PlayerPrefs.GetInt(CurrentInformationIndexKey, 0);
        _progressInPopulationMainInformation = PlayerPrefs.GetInt(ProgressInPopulationMainInformationKey, 1);
    }

    public IEnumerator StartUp()
    {
        // Wait for other things to initialize
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        ChangeCurrentInformationIndex(CurrentInformationIndex);
        PopulateMainInformation();
    }

    public void ChangeCurrentInformationIndex(int index)
    {
        CurrentInformationIndex = index;
        PlayerPrefs.SetInt(CurrentInformationIndexKey, index);
        for (var i = 0; i < _poiM.getAllPointOfInterests().Length; i++)
            if (i < index)
                _poiM.getAllPointOfInterests()[i].isCompletedVisit = true;
            else
                _poiM.getAllPointOfInterests()[i].isCompletedVisit = false;
        _poiM.UpdateVisibilityOfSubPov(index);
    }

    public void IncreaseCurrentInformationIndex()
    {
        ChangeCurrentInformationIndex(CurrentInformationIndex + 1);
        PopulateMainInformation();
    }

    public void DecreaseCurrentInformationIndex()
    {
        if (CurrentInformationIndex == 0)
        {
            if (_tourIntroduction)
                if (_tourIntroduction.gameObject)
                    Destroy(_tourIntroduction.gameObject);
            _tourIntroduction = null;
            _mainMenuOnboardingComplete = false;
        }
        else
        {
            ChangeCurrentInformationIndex(CurrentInformationIndex - 1);
        }

        PopulateMainInformation();
    }

    public async void OnAskQuestionAsync()
    {
        if (_currentlyProcessingQuestion)
        {
            PermissionRequest.ShowToastMessage("Please wait for the current question to be processed.");
            return;
        }

        _currentlyProcessingQuestion = true;
        if (_tourIntroduction || !_mainMenuOnboardingComplete)
        {
            PermissionRequest.ShowToastMessage(
                "You need to be close to the point of interest and generate a tour to ask a question.");
            _currentlyProcessingQuestion = false;
            return;
        }

        try
        {
            questionSendButton.interactable = false;
            PermissionRequest.ShowToastMessage("Sending...");
            var tourGenerationChatManager = GetComponent<TourGenerationChatManager>();
            var question = questionTextField.text;
            questionTextField.text = "";
            var relevantInformation = "";
            if (question.StartsWith("Can you tell me something about "))
            {
                var locationName = question.Replace("Can you tell me something about ", "");
                relevantInformation = _poiM.GetPointOfInterestInformationByTitle(locationName);
            }

            _ = _userInformation.UpdateSummery(question);
            var action = new TourContent(await tourGenerationChatManager.OnChat(
                "The user is asking the following question. Before you answer, choose an action to do based on the question: " +
                question, true));
            var calledFunction = action.GetFunctionAndParameters();

            if (string.IsNullOrEmpty(relevantInformation)) relevantInformation = _poiM.getCurrentPOI().information;
            await AddChatHistory(relevantInformation);
            _poiM.getAllPointOfInterests()[CurrentInformationIndex].qandA
                .Add(new QAndA
                    { question = question, answer = "...", position = _progressInPopulationMainInformation });
            PopulateMainInformation();

            var questionMessage = "The user asked the following: \"" + question +
                                  "\" based on that question you are already doing this action: " +
                                  calledFunction +
                                  " Now answer the users question accordingly, use your tools to research and write a maximum of 9 sentences as an answer.";

            var answer = await _groundingChatManager.OnChat(questionMessage);
            action.ScheduleAssociatedAgentTasks(true);
            // Remove any actions from the answer
            _poiM.getAllPointOfInterests()[CurrentInformationIndex].qandA[^1] =
                new QAndA { question = question, answer = answer, position = _progressInPopulationMainInformation };
            PopulateMainInformation();
            GetComponent<SaveDataManager>().SavePointOfInterestManager();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }

        _currentlyProcessingQuestion = false;
        questionSendButton.interactable = true;
    }


    private async Task AddChatHistory(string relevantInformation)
    {
        if (!_guideManager) _guideManager = FindAnyObjectByType<GuideManager>();
        if (!_userInformation) _userInformation = GetComponent<UserInformation>();
        if (!_poiM) _poiM = GetComponent<PointOfInterestManager>();

        var systemPrompt = _guideManager.GetChosenGuideInfo().longDescription;
        systemPrompt += "The user is currently at the point of interest " +
                        _poiM.getCurrentPOI().title +
                        ".";
        systemPrompt += "The user already visited the tour points " + _poiM.getVisitedPOIs() +
                        ". ";
        systemPrompt += "The user will also visit the following points of interest today " +
                        _poiM.getUpcomingPOIs() +
                        ". ";
        systemPrompt += "You know the following about the user: " + _userInformation.userSummery +
                        ". Let the user know what you know when they ask. Your memory updates automatically based on what the user tells you and will taken into account for the next tour stop. ";
        systemPrompt +=
            "You goal for this chat is to provide the user with a detailed answer to the question that relates to the user. Ensure that your response includes actionable insights and is concise, clear, and directly related to the question. Write any other actions or non-verbal ques is square brackets for example [smile]." +
            " If the user directly or indirectly request a change to the tour let them know that you will take that into account fot the next point of interest. Changing the tour itinerary or points of interest is not possible at this point of time. Try not to mention what the next point of interest will be.";
        ;
        systemPrompt +=
            "If the user is asking what the red or blue books floating in AR on their screen are, you can say that blue books represent the point of interest on this tour and red books represent other points of interest that are not on the tour but they can ask about by taping on them.";
        systemPrompt +=
            "You can use the tools made available to you to get relevant and up-to-date information, for example the latest winner of the Charlemagne Prize or information about a specific building.";
        if (!string.IsNullOrEmpty(relevantInformation))
            systemPrompt += "You know the following: " + relevantInformation;


        await _groundingChatManager.OnChat(systemPrompt);
        _groundingChatManager.SetRole(GeminiRole.User);

        foreach (Transform content in interactionWindowMain)
        {
            var message = content.gameObject.GetComponent<MessageBlock>();
            if (message == null) continue;
            var role = message.isUserMessage() ? GeminiRole.User : GeminiRole.Assistant;
            _groundingChatManager._chatHistory.Add(GeminiContent.GetContent(message.GetCompleteText(), role));
        }
    }

    private void SaveForSummery()
    {
        if (!_tourSummery) _tourSummery = GetComponent<TourSummery>();
        var tempChatHistory = new List<GeminiContent>();
        foreach (Transform content in interactionWindowMain)
        {
            var message = content.gameObject.GetComponent<MessageBlock>();
            if (message == null) continue;
            var role = message.isUserMessage() ? GeminiRole.User : GeminiRole.Assistant;
            tempChatHistory.Add(GeminiContent.GetContent(message.GetCompleteText(), role));
        }

        _tourSummery.AddChatHistory(tempChatHistory, CurrentInformationIndex - 1);
    }

    private void finishMainMenuOnboarding()
    {
        _mainMenuOnboardingComplete = true;
        PlayerPrefs.SetInt(MainOnboardingCompleteKey, 1);
        Destroy(_mainOnboardingUI.gameObject);
        ChangeCurrentInformationIndex(0);
        PopulateMainInformation();
    }

    private void ShowMainMenuOnboarding()
    {
        ChangeCurrentInformationIndex(0);
        titleText.text = "Welcome to the AR Tour Guide!";
        _mainOnboardingUI = Instantiate(interactionPrefab, interactionWindowMain);
        var message = IntroductionText;
        _mainOnboardingUI.SetupGuideMessage(message, null, true);
        _theFirstButton = Instantiate(buttonsUnderMessage, interactionWindowMain);
        _theFirstButton.SetupToNextMessage(finishMainMenuOnboarding);
        _theFirstButton.QuestionButton.gameObject.SetActive(false);
    }

    private async Task ShowMainSummery()
    {
        titleText.text = "Conclusion";
        if (!_tourSummery) _tourSummery = GetComponent<TourSummery>();
        var summery = await _tourSummery.GenerateSummery();
        var summeryUI = Instantiate(interactionPrefab, interactionWindowMain);
        summeryUI.SetupGuideMessage(summery, null, true);
        var button = Instantiate(buttonsUnderMessage, interactionWindowMain);
        button.SetupToNextMessage(finishMainSummery);
        button.QuestionButton.gameObject.SetActive(false);
    }

    private void finishMainSummery()
    {
        var uiSwitcher = GetComponent<UISwitcher>();

        uiSwitcher.OnStartEvaluation();
    }

    private void CleanupUI()
    {
        if (_mainOnboardingUI && _mainOnboardingUI.gameObject)
            Destroy(_mainOnboardingUI.gameObject);
        if (_theFirstButton && _theFirstButton.gameObject)
            Destroy(_theFirstButton.gameObject);
    }

    private void ResetProgress()
    {
        _countOfInitializedMessages = 0;
        _progressInPopulationMainInformation = 1;
        PlayerPrefs.SetInt(ProgressInPopulationMainInformationKey, _progressInPopulationMainInformation);
    }

    private void ClearInteractionWindow()
    {
        foreach (Transform child in interactionWindowMain) Destroy(child.gameObject);
    }

    private void ShowTourGenerationPrompt()
    {
        var previous = _tourIntroduction;
        titleText.text = "Our next destination";
        _tourIntroduction = Instantiate(_tourIntroductionPrefab, interactionWindowMain);
        _tourIntroduction.SetUp(_poiM.getAllPointOfInterests()[CurrentInformationIndex],
            CurrentInformationIndex + 1, GenerateTourForCurrentPointOfInterest);


        _progressInPopulationMainInformation = 1;
        PlayerPrefs.SetInt(ProgressInPopulationMainInformationKey, _progressInPopulationMainInformation);

        if (previous)
            if (previous.gameObject)
                Destroy(previous.gameObject);
    }

    private List<string> GetCachedMessages()
    {
        List<string> cachedMessages = new() { "..." };
        for (var i = 0; i < interactionWindowMain.childCount; i++)
        {
            var child = interactionWindowMain.GetChild(i);
            var qaBlock = child.GetComponent<MessageBlock>();

            if (qaBlock)
                cachedMessages.Add(qaBlock.GetCompleteText());
        }

        return cachedMessages;
    }

    private MessageBlock AddQandA(List<string> cachedMessages, MessageBlock latestUIBlock, int messageProgression,
        Transform interactionWindow, int pointOfInterestProgression, bool addToDetail = false)
    {
        foreach (var qandA in _poiM.getAllPointOfInterests()[pointOfInterestProgression].qandA)
        {
            if (qandA.position != messageProgression) continue;

            DebugEditor.Log("QandA: " + qandA.question + " " + qandA.answer);
            if (!cachedMessages.Contains(qandA.question))
            {
                latestUIBlock = Instantiate(interactionPrefab, interactionWindow);
                latestUIBlock.SetupUserMessage(qandA.question, null);
                if (addToDetail) _detailTexts.Add(latestUIBlock);
            }

            if (!cachedMessages.Contains(qandA.answer))
            {
                latestUIBlock = Instantiate(interactionPrefab, interactionWindow);
                latestUIBlock.SetupGuideMessage(qandA.answer, null);
                if (addToDetail) _detailTexts.Add(latestUIBlock);
            }
        }

        return latestUIBlock;
    }

    private void AddNavigationButton()
    {
        if (_theFirstButton) Destroy(_theFirstButton.gameObject);
        _theFirstButton = Instantiate(buttonsUnderMessage, interactionWindowMain);
        if (_progressInPopulationMainInformation ==
            _poiM.getAllPointOfInterests()[CurrentInformationIndex].tourContent.Length)
        {
            if (_poiM.getAllPointOfInterests().Length > CurrentInformationIndex + 1)
            {
                // If we are at the end of one point of interest, but not at the end of the tour
                _theFirstButton.SetupToNextPOI(IncreaseCurrentInformationIndex);
                _poiM.getAllPointOfInterests()[CurrentInformationIndex].isCompletedVisit = true;
                var saveDataManager = GetComponent<SaveDataManager>();
                if (saveDataManager) saveDataManager.SavePointOfInterestManager();
            }
            else
            {
                // If we are at the end of the tour
                _theFirstButton.SetupToNextMessage(IncreaseCurrentInformationIndex);
            }
        }
        else
        {
            _theFirstButton.SetupToNextMessage(AdvanceProgressInPopulatingMain);
        }
    }

    public void PopulateMainInformation()
    {
        DebugEditor.Log("PopulateMainInformation");
        if (!_mainMenuOnboardingComplete)
        {
            ShowMainMenuOnboarding();
            return;
        }

        CleanupUI();

        if (_oldIndex != CurrentInformationIndex && _oldIndex != 1000)
        {
            SaveForSummery();
            ResetProgress();
            ClearInteractionWindow();
        }

        _oldIndex = CurrentInformationIndex;

        if (CurrentInformationIndex >= _poiM.getAllPointOfInterests().Length)
        {
            _ = ShowMainSummery();
            return;
        }

        var content = _poiM.getAllPointOfInterests()[CurrentInformationIndex].tourContent;
        titleText.text = _poiM.getAllPointOfInterests()[CurrentInformationIndex].title;
        var cachedMessages = GetCachedMessages();

        if (_theFirstButton) Destroy(_theFirstButton.gameObject);
        if (content == null || content.Length == 0)
        {
            ShowTourGenerationPrompt();
            return;
        }

        if (_tourIntroduction)
            if (_tourIntroduction.gameObject)
                Destroy(_tourIntroduction.gameObject);

        MessageBlock latestUIBlock = null;

        for (var i = _countOfInitializedMessages; i <= content.Length; i++)
        {
            latestUIBlock = AddQandA(cachedMessages, latestUIBlock, i, interactionWindowMain, CurrentInformationIndex);

            // If content has already ended, there might have been questions before though
            if (i != content.Length)
            {
                // Add tour content
                var tourContent = content[i];
                if (_countOfInitializedMessages < _progressInPopulationMainInformation)
                    if (!cachedMessages.Contains(tourContent.GetText()))
                    {
                        _countOfInitializedMessages++;
                        latestUIBlock = Instantiate(interactionPrefab, interactionWindowMain);
                        latestUIBlock.SetupGuideMessage(tourContent.GetText(), null);
                        tourContent.ScheduleAssociatedAgentTasks();
                    }
            }

            // If we have reached the end of the content, we need to add the button
            AddNavigationButton();
        }

        // Only read out the last guide message, when multiple are present
        if (!latestUIBlock) return;
        if (!latestUIBlock.isUserMessage())
            latestUIBlock.SetupGuideMessage(latestUIBlock.GetCompleteText(), null, true);
    }

    public void AdvanceProgressInPopulatingMain()
    {
        _progressInPopulationMainInformation++;
        PlayerPrefs.SetInt(ProgressInPopulationMainInformationKey, _progressInPopulationMainInformation);
        _poiM.UpdateVisibilityOfSubPov(CurrentInformationIndex);
        var uiSwitcher = GetComponent<UISwitcher>();
        uiSwitcher.OnCloseFAB();
        PopulateMainInformation();
        PlayerPrefs.Save();
    }

    public void UpdatePovDetailPage(int i)
    {
        foreach (var block in _detailTexts) Destroy(block.gameObject);
        _detailTexts.Clear();
        poiTitle.text = _poiM.getAllPointOfInterests()[i].title;
        poiSubtitle.text = _poiM.getAllPointOfInterests()[i]
            .subtitle[..Mathf.Min(50, _poiM.getAllPointOfInterests()[i].subtitle.Length)] + "...";

        var uiSwitcher = GetComponent<UISwitcher>();
        startNavigationButton.onClick.AddListener(() => { uiSwitcher.OnStartNavigation(poiTitle.text); });

        var content = _poiM.getAllPointOfInterests()[i].tourContent;
        if (content == null || content.Length == 0)
        {
            var questionUI = Instantiate(interactionPrefab, interactionWindowPoiDetail);
            questionUI.SetupGuideMessage(
                "Come back here after you heard the explanation of this point of interest in the <b>\"Tour\"</b> tab.",
                null);
            _detailTexts.Add(questionUI);
            return;
        }

        UpdatePovDetailPageUI(content, i);
    }

    private void UpdatePovDetailPageUI(TourContent[] content, int poiIndex)
    {
        if (content == null)
        {
            DebugEditor.LogError("Content is null");
            return;
        }

        for (var i = 0; i < content.Length; i++)
        {
            var questionUI = Instantiate(interactionPrefab, interactionWindowPoiDetail);
            _detailTexts.Add(questionUI);
            questionUI.SetupGuideMessage(content[i].GetText(), null);
            AddQandA(new List<string>(), null, i, interactionWindowPoiDetail, poiIndex, true);
        }
    }


    private async void GenerateTourForCurrentPointOfInterest()
    {
        await GenerateTourForCurrentPointOfInterestAsync();
        PopulateMainInformation();
    }

    private async Task<bool> GenerateTourForCurrentPointOfInterestAsync()
    {
        var tourGenerator = GetComponent<TourGenerator>();
        var content = await tourGenerator.generateTourForPointOfInteresting();
        if (content == null)
        {
            DebugEditor.LogError("Content is null");
            return false;
        }

        _poiM.SetTourContent(CurrentInformationIndex, content);
        return true;
    }

    public void ToggleAnswerVisibility()
    {
        isShowingAnswer = !isShowingAnswer;
        /*
        if (isShowingAnswer)
            // Scroll to the bottom
            //scrollRect.normalizedPosition = new Vector2(0, 1);
        else
            // Scroll to the top
            //scrollRect.normalizedPosition = new Vector2(0, 0);
    */
    }

    [Serializable]
    public struct QAndA
    {
        public string question;
        public string answer;
        public int position;
    }
}