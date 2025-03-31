using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace i5.LLM_AR_Tourguide.Prefab_Scripts
{
    public class Questionnaire : MonoBehaviour
    {
        public Button secondaryButton;
        public Button primaryButton;

        private SevenPointScale[] scales;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void Start()
        {
            scales = GetComponentsInChildren<SevenPointScale>();
            checkIfAllQuestionsAreAnswered();
            foreach (var scale in scales)
            {
                scale.toggle1.onValueChanged.AddListener(delegate { checkIfAllQuestionsAreAnswered(); });
                scale.toggle2.onValueChanged.AddListener(delegate { checkIfAllQuestionsAreAnswered(); });
                scale.toggle3.onValueChanged.AddListener(delegate { checkIfAllQuestionsAreAnswered(); });
                scale.toggle4.onValueChanged.AddListener(delegate { checkIfAllQuestionsAreAnswered(); });
                scale.toggle5.onValueChanged.AddListener(delegate { checkIfAllQuestionsAreAnswered(); });
                scale.toggle6.onValueChanged.AddListener(delegate { checkIfAllQuestionsAreAnswered(); });
                scale.toggle7.onValueChanged.AddListener(delegate { checkIfAllQuestionsAreAnswered(); });
            }
        }

        public Dictionary<string, int> collectAllResults()
        {
            var results = new Dictionary<string, int>();
            foreach (var scale in scales) results.Add(scale.GetQuestion(), scale.GetSelectedValue());

            return results;
        }

        public void checkIfAllQuestionsAreAnswered()
        {
            foreach (var scale in scales)
                if (scale.GetSelectedValue() == 0)
                {
                    primaryButton.interactable = false;
                    return;
                }

            primaryButton.interactable = true;
        }
    }
}