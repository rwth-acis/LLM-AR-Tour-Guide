using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace i5.LLM_AR_Tourguide.Prefab_Scripts
{
    public class PossibleAnswerPanel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _text;
        [SerializeField] private Button button;
        private OnboardingManager _onboardingManager;

        public void Start()
        {
            _onboardingManager = FindAnyObjectByType<OnboardingManager>();
        }

        public void Setup(string text)
        {
            _text.text = text;
            button.onClick.AddListener(OnClick);
        }

        public void OnClick()
        {
            _onboardingManager.OnAnswerOnboardingQuestionAsync(_text.text);
        }

        public string GetText()
        {
            return _text.text;
        }

        public void DeactivateButton()
        {
            button.interactable = false;
        }

        public void ActivateButton()
        {
            button.interactable = true;
        }
    }
}