using i5.LLM_AR_Tourguide.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace i5.LLM_AR_Tourguide.Guide_Scripts
{
    public class GuideSelector : MonoBehaviour
    {
        [SerializeField] private GuideManager.GeneralGuideIdentifier selectedGuide;

        [SerializeField] private TextMeshProUGUI title;

        [SerializeField] private TextMeshProUGUI subtitle;

        [SerializeField] private Image avatar;

        private GuideManager guideManager;

        private TextToSpeech textToSpeech;

        private Toggle toggle;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void Start()
        {
            guideManager = FindAnyObjectByType<GuideManager>();
            title.text = guideManager.GetGuideInfo(selectedGuide).descriptionTitle;
            subtitle.text = guideManager.GetGuideInfo(selectedGuide).descriptionSubtitle;
            avatar.sprite = guideManager.GetGuideInfo(selectedGuide).avatarImage;
            textToSpeech = FindAnyObjectByType<TextToSpeech>();
            // Find toggle in children
            toggle = GetComponentInChildren<Toggle>();
            if (toggle == null)
            {
                DebugEditor.LogError("Toggle not found in children.");
                return;
            }

            GetComponent<Button>().onClick.AddListener(delegate { toggle.isOn = true; });

            if (toggle.isOn)
            {
                DebugEditor.Log("Toggle is on for " + selectedGuide);
                OnSelectedGuide();
            }

            toggle.onValueChanged.AddListener(delegate
            {
                if (toggle.isOn) OnSelectedGuide();
            });
        }

        public void OnSelectedGuide()
        {
            guideManager.ChangeGuide(selectedGuide);
            OnSpeakRandomCatchPhrase();
        }

        private void OnSpeakRandomCatchPhrase()
        {
            string[] catchphrases;
            var voiceName = "";
            if (PlayerPrefs.GetString("Language") == "de")
            {
                catchphrases = guideManager.GetGuideInfo(selectedGuide).catchphrasesGerman;
                voiceName = guideManager.GetGuideInfo(selectedGuide).voiceNameGerman;
            }
            else
            {
                catchphrases = guideManager.GetGuideInfo(selectedGuide).catchphrasesEnglish;
                voiceName = guideManager.GetGuideInfo(selectedGuide).voiceNameEnglish;
            }

            var randomIndex = Random.Range(0, catchphrases.Length);
            var catchphrase = catchphrases[randomIndex];

            textToSpeech.OnSpeak(catchphrase, voiceName);
        }
    }
}