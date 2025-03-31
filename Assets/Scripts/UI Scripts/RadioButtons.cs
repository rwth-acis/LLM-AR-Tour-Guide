using UnityEngine;
using UnityEngine.UI;

namespace i5.LLM_AR_Tourguide.UI_Scripts
{
    public class RadioButtons : MonoBehaviour
    {
        public Button button;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private Toggle[] radioButtons;

        private void Start()
        {
            radioButtons = GetComponentsInChildren<Toggle>();
            foreach (var toggle in radioButtons)
                toggle.onValueChanged.AddListener(delegate { OnToggleValueChanged(toggle); });
        }

        private void Update()
        {
            // Check if one radio button is selected
            var isAnyToggleSelected = false;
            foreach (var toggle in radioButtons)
                if (toggle.isOn)
                {
                    isAnyToggleSelected = true;
                    break;
                }

            // Enable the button if one radio button is selected
            button.interactable = isAnyToggleSelected;
        }

        private void OnToggleValueChanged(Toggle changedToggle)
        {
            if (changedToggle.isOn)
                foreach (var toggle in radioButtons)
                    if (toggle != changedToggle)
                        toggle.isOn = false;
        }
    }
}