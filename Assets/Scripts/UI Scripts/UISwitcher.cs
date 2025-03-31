using System;
using i5.LLM_AR_TourGuide;
using i5.LLM_AR_Tourguide.Evaluation;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace i5.LLM_AR_Tourguide.UI_Scripts
{
    public class UISwitcher : MonoBehaviour
    {
        private static int _hintIndex;

        /// <summary>
        ///     Hints will be shown when a tour is generated or when the user is interacting with the guide
        ///     They will be shown in an Android Toast message, so they need to be short
        /// </summary>
        private static readonly string[] hints =
        {
            "Tip: Ask your guide to remember or forget things about you",
            "Hint: You can tap on the red books around you",
            "Hint: You can find all hints & tips in the settings tab",
            "Tip: Ask your guide to explain it from a different perspective.",
            "Tip: You don't have to wait for your guide to finish speaking",
            "Hint: Try asking your guide to do something funny"
        };

        [FormerlySerializedAs("WelcomeScreen")] [SerializeField]
        private GameObject welcomeScreen;

        [FormerlySerializedAs("ExploreTab")] [SerializeField]
        private GameObject exploreTab;

        [FormerlySerializedAs("TourTab")] [SerializeField]
        private GameObject tourTab;

        [FormerlySerializedAs("SettingsTab")] [SerializeField]
        private GameObject settingsTab;

        [FormerlySerializedAs("POIDetailedInfo")] [SerializeField]
        private GameObject poiDetailedInfo;

        [FormerlySerializedAs("EvaluationPage")] [SerializeField]
        public GameObject evaluationPage;

        [FormerlySerializedAs("openFAB")] public GameObject openFab;

        [SerializeField] private RectTransform mainMenuSection;

        [FormerlySerializedAs("IncreaseOrDecreaseMenuHeightButton")] [SerializeField]
        private Button increaseOrDecreaseMenuHeightButton;

        [SerializeField] private RectTransform menuHeightArrow;
        [SerializeField] private ScrollRect scrollRect;

        private InformationController _infoController;


        private bool _menuIsIncreased;
        private TourListManager _tourListManager;

        private void Start()
        {
            if (welcomeScreen == null || exploreTab == null || tourTab == null || settingsTab == null ||
                poiDetailedInfo == null || openFab == null) return;
            _infoController = GetComponent<InformationController>();
            _tourListManager = GetComponent<TourListManager>();

            welcomeScreen.SetActive(true);
            exploreTab.SetActive(false);
            tourTab.SetActive(false);
            settingsTab.SetActive(false);
            poiDetailedInfo.SetActive(false);
            evaluationPage.SetActive(false);
            OnCloseFAB();

            increaseOrDecreaseMenuHeightButton.onClick.AddListener(ToggleMenuHeight);

            // Set language to english
            StartCoroutine(LanguageManager.SetToEnglish());
        }


        public void OnFirstTapPress()
        {
            welcomeScreen.SetActive(false);
            exploreTab.SetActive(true);
            tourTab.SetActive(false);
            settingsTab.SetActive(false);
            poiDetailedInfo.SetActive(false);
            UploadManager.UploadData("UISwitcher", "OnFirstTapPress");
        }

        public void OnSecondTapPress()
        {
            welcomeScreen.SetActive(false);
            exploreTab.SetActive(false);
            tourTab.SetActive(true);
            settingsTab.SetActive(false);
            poiDetailedInfo.SetActive(false);
            _tourListManager.UpdateTourList();
            UploadManager.UploadData("UISwitcher", "OnSecondTapPress");
        }

        public void OnThirdTapPress()
        {
            welcomeScreen.SetActive(false);
            exploreTab.SetActive(false);
            tourTab.SetActive(false);
            settingsTab.SetActive(true);
            poiDetailedInfo.SetActive(false);
            UploadManager.UploadData("UISwitcher", "OnThirdTapPress");
        }


        public void OnPOVDetailedPressed(int i)
        {
            welcomeScreen.SetActive(false);
            exploreTab.SetActive(false);
            tourTab.SetActive(false);
            settingsTab.SetActive(false);
            poiDetailedInfo.SetActive(true);
            _infoController.UpdatePovDetailPage(i);
            UploadManager.UploadData("UISwitcher", "OnPOVDetailedPressed" + i);
        }

        public void OnOpenFAB(bool showHint = true)
        {
            OnDecreaseMenuHeight();
            if (showHint) ShowHint();

            openFab.SetActive(true);
            _infoController.ToggleAnswerVisibility();
        }

        public void ToggleMenuHeight()
        {
            if (_menuIsIncreased)
                OnDecreaseMenuHeight();
            else
                OnIncreaseMenuHeight();
        }

        public void OnIncreaseMenuHeight()
        {
            OnCloseFAB();
            // Point arrow down
            menuHeightArrow.localEulerAngles = new Vector3(0, 0, 90);
            // Set height to 600 for mainMenuSection
            mainMenuSection.sizeDelta = new Vector2(mainMenuSection.sizeDelta.x, 450);
            _menuIsIncreased = true;
        }

        public void OnDecreaseMenuHeight()
        {
            // Point arrow up
            menuHeightArrow.localEulerAngles = new Vector3(0, 0, -90);
            // Set height to 245 for mainMenuSection
            mainMenuSection.sizeDelta = new Vector2(mainMenuSection.sizeDelta.x, 245);
            _menuIsIncreased = false;
            // Update canvas
            Canvas.ForceUpdateCanvases();
            // Scroll to bottom
            scrollRect.normalizedPosition = new Vector2(0, 0);
        }


        public void OnARElementPressed(string location)
        {
            OnOpenFAB(false);
            _infoController.questionTextField.text = "Can you tell me something about " + location;
            _infoController.OnAskQuestionAsync();
            UploadManager.UploadData("UISwitcher", "OnARElementPressed" + location);
            OnCloseFAB();
        }

        /// <summary>
        ///     Show a hint every second time it is called
        /// </summary>
        public static void ShowHint(bool force = false)
        {
            while (true)
            {
                if (_hintIndex % (hints.Length * 2) == 0) _hintIndex = 0;
                if (_hintIndex % 2 == 0) PermissionRequest.ShowToastMessage(hints[_hintIndex / 2]);
                _hintIndex++;
                if (force)
                {
                    force = false;
                    continue;
                }

                break;
            }
        }

        public void OnCloseFAB()
        {
            openFab.SetActive(false);
            _infoController.ToggleAnswerVisibility();
        }

        public void OnStartNavigation(string location)
        {
#if UNITY_ANDROID
            // Toast message
            PermissionRequest.ShowToastMessage("Open the app again when you are closer to the destination!");
            // Open navigation
            Application.OpenURL(string.Format("google.navigation:q={0}&mode=w", location));
#elif UNITY_IOS
    Application.OpenURL(string.Format("http://maps.apple.com/?daddr={0}", location)); // Not tested
#else
    DebugEditor.LogError("Navigation is not supported on this platform!");
#endif
        }

        public void OnStartEvaluation()
        {
            welcomeScreen.SetActive(false);
            exploreTab.SetActive(false);
            tourTab.SetActive(false);
            settingsTab.SetActive(false);
            poiDetailedInfo.SetActive(false);
            evaluationPage.SetActive(true);
            var evaluationManager = GetComponent<EvaluationManager>();
            evaluationManager.startEvaluation();

            UploadManager.UploadData("StartedEvaluation", DateTime.Now.ToString("dd-MM-yyyy-HH-mm-ss"));
        }
    }
}