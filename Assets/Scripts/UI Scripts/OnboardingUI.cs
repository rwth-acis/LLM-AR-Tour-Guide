using System;
using i5.LLM_AR_Tourguide.GeospatialAPI;
using UnityEngine;

namespace i5.LLM_AR_Tourguide.UI_Scripts
{
    public class OnboardingUI : MonoBehaviour
    {
        /// <summary>
        ///     The key name used in PlayerPrefs which indicates whether the onboarding dialogsArray have
        ///     been finished at least one time.
        /// </summary>
        private const string _hasFinishedOnboardingOnceKey = "HasFinishedOnboardingOnceKey";

        [SerializeField] private Dialogs[] dialogs;

        [SerializeField] private int currentDialogIndex;

        [SerializeField] private GameObject OnboardingQandAGameobject;

        [SerializeField] private ProgressBar progressionSlider;


        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void Start()
        {
            if (PlayerPrefs.HasKey(_hasFinishedOnboardingOnceKey) &&
                PlayerPrefs.GetInt(_hasFinishedOnboardingOnceKey) == 1)
            {
                FinishOnboarding(false);
                return;
            }
#if !DEVELOPMENT_BUILD
            currentDialogIndex = 0;
#endif
            UpdateDialog();
        }

        // Update is called once per frame
        private void Update()
        {
        }

        public void ShowNextDialog()
        {
            currentDialogIndex++;
            UpdateDialog();
        }

        public void ShowPreviousDialog()
        {
            currentDialogIndex--;
            UpdateDialog();
        }

        private void UpdateDialog()
        {
            //DebugEditor.Log("Current Dialog Index: " + currentDialogIndex);
            //DebugEditor.Log("Dialogs Length: " + dialogs.Length);
            updateSliderBasedOnDialogs();
            if (currentDialogIndex < 0) currentDialogIndex = 0;
            if (currentDialogIndex >= dialogs.Length) FinishOnboarding(true);
            for (var i = 0; i < dialogs.Length; i++)
                if (i == currentDialogIndex)
                    foreach (var dialog in dialogs[i].allDialogObjects)
                    {
                        dialog.SetActive(true);
                        if (dialog.gameObject == OnboardingQandAGameobject.gameObject)
                        {
                            var onboardingManager = GetComponent<OnboardingManager>();
                            onboardingManager.OnboardingAnswerQuestionAsync();
                        }
                    }
                else
                    foreach (var dialog in dialogs[i].allDialogObjects)
                        dialog.SetActive(false);
        }

        private void updateSliderBasedOnDialogs()
        {
            if (currentDialogIndex < dialogs.Length - 1)
                progressionSlider.gameObject.SetActive(true);
            else
                progressionSlider.gameObject.SetActive(false);
            progressionSlider.SetProgression(currentDialogIndex / (float)(dialogs.Length - 1), false);
        }

        private void FinishOnboarding(bool firstTime)
        {
            var uiSwitcher = GetComponent<UISwitcher>();

            if (progressionSlider != null) progressionSlider.gameObject.SetActive(false);
            foreach (var dialog in dialogs)
            foreach (var obj in dialog.allDialogObjects)
                obj.SetActive(false);

            if (uiSwitcher != null) uiSwitcher.OnFirstTapPress();

            if (firstTime)
            {
                PlayerPrefs.SetInt(_hasFinishedOnboardingOnceKey, 1);
                PlayerPrefs.Save();
            }

            var geospatialController = FindAnyObjectByType<GeospatialController>();

            if (geospatialController != null)
                geospatialController.OnGetStartedClicked();
            else
                DebugEditor.LogError("Error: Geospatial Controller was not found. Please restart.");

            var informationController = GetComponent<InformationController>();
            StartCoroutine(informationController.StartUp());

            if (firstTime)
            {
                PlayerPrefs.Save();
                PermissionRequest.ShowToastMessage("Please restart the application to start the tour.");
                Application.Quit();
            }
        }

        [Serializable]
        private struct Dialogs
        {
            public GameObject[] allDialogObjects;
        }
    }
}