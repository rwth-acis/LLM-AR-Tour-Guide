using i5.LLM_AR_Tourguide.UI_Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace i5.LLM_AR_Tourguide.Prefab_Scripts
{
    public class ChipButtons : MonoBehaviour
    {
        public Button QuestionButton;
        public Button NavigationButton;
        public Button ContinueButton;
        public Button GenerateButton;
        public Button GoBackButton;
        private InformationController informationController;

        private UISwitcher uiSwitcher;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void Start()
        {
            uiSwitcher = FindAnyObjectByType<UISwitcher>();
            informationController = FindAnyObjectByType<InformationController>();
            QuestionButton.onClick.AddListener(() => { uiSwitcher.OnOpenFAB(); });
        }

        public void SetTextContinueButton(string text)
        {
            ContinueButton.GetComponentInChildren<TextMeshProUGUI>().text = text;
        }

        public void SetupToNextMessage(UnityAction continueAction)
        {
            GoBackButton.gameObject.SetActive(false);
            QuestionButton.gameObject.SetActive(true);
            NavigationButton.gameObject.SetActive(false);
            ContinueButton.gameObject.SetActive(true);
            GenerateButton.gameObject.SetActive(false);
            ContinueButton.onClick.AddListener(continueAction);
            ContinueButton.onClick.AddListener(() => { ContinueButton.interactable = false; });
        }

        public void SetupToGenerateMessage(UnityAction generateAction, string nextPOIName)
        {
            GoBackButton.gameObject.SetActive(true);
            QuestionButton.gameObject.SetActive(false);
            NavigationButton.gameObject.SetActive(true);
            ContinueButton.gameObject.SetActive(false);
            GenerateButton.gameObject.SetActive(true);
            GenerateButton.onClick.AddListener(generateAction);
            GenerateButton.onClick.AddListener(() =>
            {
                GenerateButton.interactable = false;
                ContinueButton.interactable = false;
                GoBackButton.interactable = false;
                NavigationButton.interactable = false;
            });
            NavigationButton.onClick.AddListener(() => { uiSwitcher.OnStartNavigation(nextPOIName); });
            GoBackButton.onClick.AddListener(() =>
            {
                informationController.DecreaseCurrentInformationIndex();
                PermissionRequest.ShowToastMessage(
                    "Tip: You can skip forward just by pressing the continue button");
            });
        }

        public void SetupToNextPOI(UnityAction continueAction = null)
        {
            GoBackButton.gameObject.SetActive(false);
            QuestionButton.gameObject.SetActive(true);
            NavigationButton.gameObject.SetActive(false);
            GenerateButton.gameObject.SetActive(false);
            if (continueAction != null)
            {
                ContinueButton.gameObject.SetActive(true);
                ContinueButton.onClick.AddListener(continueAction);
            }
            else
            {
                ContinueButton.gameObject.SetActive(false);
            }
        }

        public void SetupToFinishMessage()
        {
            GoBackButton.gameObject.SetActive(false);
            QuestionButton.gameObject.SetActive(true);
            NavigationButton.gameObject.SetActive(false);
            ContinueButton.gameObject.SetActive(true);
            GenerateButton.gameObject.SetActive(false);
            ContinueButton.onClick.AddListener(() => { uiSwitcher.OnStartEvaluation(); });
            ContinueButton.GetComponentInChildren<TextMeshProUGUI>().text = "Finish";
            QuestionButton.GetComponentInChildren<TextMeshProUGUI>().text = "Ask last question";
        }
    }
}