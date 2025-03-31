using i5.LLM_AR_Tourguide.UI_Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.AdaptivePerformance;

namespace i5.LLM_AR_Tourguide
{
    public class FramesPerSecond : MonoBehaviour
    {
        private Settings _settings;
        private IAdaptivePerformance ap;

        private TextMeshProUGUI text;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void Start()
        {
            ap = Holder.Instance;
            _settings = FindAnyObjectByType<Settings>();
            if (ap is not { Active: true })
            {
                Debug.Log("[AP] Adaptive Performance not active.");
                return;
            }

            text = GetComponent<TextMeshProUGUI>();
            text.text = "";
        }

        // Update is called once per frame
        private void Update()
        {
            if (_settings.DebugMode) text.text = (1 / ap.PerformanceStatus.FrameTiming.AverageFrameTime).ToString("F2");
        }
    }
}