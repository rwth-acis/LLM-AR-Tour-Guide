using UnityEngine;
using UnityEngine.UI;

namespace i5.LLM_AR_Tourguide.UI_Scripts
{
    public class CheckAllTogglesAreSelected : MonoBehaviour
    {
        public Toggle[] toggles;
        public Button button;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void Start()
        {
        }

        // Update is called once per frame
        private void Update()
        {
            // Check if all toggles are selected
            var allTogglesAreSelected = true;
            foreach (var toggle in toggles)
                if (!toggle.isOn)
                {
                    allTogglesAreSelected = false;
                    break;
                }

            // Enable the button if all toggles are selected
            button.interactable = allTogglesAreSelected;
        }
    }
}