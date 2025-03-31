using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace i5.LLM_AR_Tourguide.Audio
{
    public class SpeechToTextComponent : MonoBehaviour, ISpeechToTextListener
    {
        public TMP_InputField SpeechText;
        public Button StartAndStopSpeechToTextButton;
        public Slider VoiceLevelSlider;
        public bool PreferOfflineRecognition;
        private bool isActive;

        private float normalizedVoiceLevel;

        private void Awake()
        {
            SpeechToText.Initialize("en-US");
            VoiceLevelSlider.gameObject.SetActive(false);
            StartAndStopSpeechToTextButton.onClick.AddListener(StartAndStopSpeechToText);
        }

        private void Update()
        {
            if (!isActive)
                StartAndStopSpeechToTextButton.interactable =
                    SpeechToText.IsServiceAvailable(PreferOfflineRecognition) && !SpeechToText.IsBusy();
            else
                StartAndStopSpeechToTextButton.interactable = SpeechToText.IsBusy();

            // You may also apply some noise to the voice level for a more fluid animation (e.g. via Mathf.PerlinNoise)
            VoiceLevelSlider.value =
                Mathf.Lerp(VoiceLevelSlider.value, normalizedVoiceLevel, 15f * Time.unscaledDeltaTime);
        }

        void ISpeechToTextListener.OnReadyForSpeech()
        {
            isActive = true;
            VoiceLevelSlider.gameObject.SetActive(true);
            DebugEditor.Log("OnReadyForSpeech");
        }

        void ISpeechToTextListener.OnBeginningOfSpeech()
        {
            VoiceLevelSlider.gameObject.SetActive(true);
            DebugEditor.Log("OnBeginningOfSpeech");
        }

        void ISpeechToTextListener.OnVoiceLevelChanged(float normalizedVoiceLevel)
        {
            // Note that On Android, voice detection starts with a beep sound and it can trigger this callback. You may want to ignore this callback for ~0.5s on Android.
            this.normalizedVoiceLevel = normalizedVoiceLevel;
        }

        void ISpeechToTextListener.OnPartialResultReceived(string spokenText)
        {
            DebugEditor.Log("OnPartialResultReceived: " + spokenText);
            SpeechText.text = spokenText;
        }

        void ISpeechToTextListener.OnResultReceived(string spokenText, int? errorCode)
        {
            isActive = false;
            VoiceLevelSlider.gameObject.SetActive(false);
            DebugEditor.Log("OnResultReceived: " + spokenText + (errorCode.HasValue ? " --- Error: " + errorCode : ""));
            SpeechText.text = spokenText;
            normalizedVoiceLevel = 0f;

            // Recommended approach:
            // - If errorCode is 0, session was aborted via SpeechToText.Cancel. Handle the case appropriately.
            // - If errorCode is 9, notify the user that they must grant Microphone permission to the Google app and call SpeechToText.OpenGoogleAppSettings.
            // - If the speech session took shorter than 1 seconds (should be an error) or a null/empty spokenText is returned, prompt the user to try again (note that if
            //   errorCode is 6, then the user hasn't spoken and the session has timed out as expected).
        }

        public void ChangeLanguage(string preferredLanguage)
        {
            if (!SpeechToText.Initialize(preferredLanguage))
                SpeechText.text = "Couldn't initialize with language: " + preferredLanguage;
        }

        public void StartAndStopSpeechToText()
        {
            if (!isActive)
                SpeechToText.RequestPermissionAsync(permission =>
                {
                    if (permission == SpeechToText.Permission.Granted)
                    {
                        if (SpeechToText.Start(this, preferOfflineRecognition: PreferOfflineRecognition))
                            SpeechText.text = "";
                        else
                            SpeechText.text = "Couldn't start speech recognition session!";
                    }
                    else
                    {
                        SpeechText.text = "Permission is denied!";
                    }
                });
            else
                SpeechToText.ForceStop();
        }
    }
}