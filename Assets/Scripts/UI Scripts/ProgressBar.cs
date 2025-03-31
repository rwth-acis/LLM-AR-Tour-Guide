using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace i5.LLM_AR_Tourguide.UI_Scripts
{
    public class ProgressBar : MonoBehaviour
    {
        [SerializeField] private Slider progressionSlider;
        [SerializeField] private TextMeshProUGUI text;

        private void Start()
        {
            progressionSlider.interactable = false;
        }

        /// <summary>
        /// </summary>
        /// <param name="value">between 0 and 1</param>
        /// <param name="showText">if true, the percentage text is shown</param>
        public void SetProgression(float value, bool showText)
        {
            if (value < 1) gameObject.SetActive(true);
            if (isActiveAndEnabled)
                StartCoroutine(AnimateSlider(value, showText));
        }

        private IEnumerator AnimateSlider(float targetValue, bool showText)
        {
            text.gameObject.SetActive(showText);

            var startValue = progressionSlider.value;
            var elapsedTime = 0f;
            var duration = 0.5f; // Duration of the animation in seconds

            // If the target value is smaller than the current value, the animation should be instant
            if (targetValue < progressionSlider.value) duration = 0f;
            // If the target value is 1, the animation should be very fast, so that the slider is deactivated quickly
            if (targetValue >= 1) duration = 0.01f;


            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                var newValue = Mathf.SmoothStep(startValue, targetValue, elapsedTime / duration);
                if (showText) text.text = Mathf.RoundToInt(newValue * 100) + "%";
                progressionSlider.value = newValue;
                yield return null;
            }

            if (showText) text.text = Mathf.RoundToInt(targetValue * 100) + "%";
            progressionSlider.value = targetValue;

            if (targetValue >= 1) gameObject.SetActive(false);
        }
    }
}