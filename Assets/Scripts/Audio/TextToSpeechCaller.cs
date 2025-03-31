using System.Text.RegularExpressions;
using i5.LLM_AR_Tourguide.Prefab_Scripts;
using UnityEngine;
using UnityEngine.Serialization;

namespace i5.LLM_AR_Tourguide.Audio
{
    public class TextToSpeechCaller : MonoBehaviour
    {
        public GameObject speakerIcon;
        public GameObject loadingIcon;
        public GameObject playingIcon;

        [FormerlySerializedAs("assosiatedBlock")]
        public MessageBlock associatedBlock;

        private TextToSpeech _textToSpeech;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void Start()
        {
            _textToSpeech = FindAnyObjectByType<TextToSpeech>();
            loadingIcon.SetActive(false);
            playingIcon.SetActive(false);
            speakerIcon.SetActive(true);
        }

        public void OnSpeak()
        {
            if (_textToSpeech == null) _textToSpeech = FindAnyObjectByType<TextToSpeech>();
            if (_textToSpeech != null)
                _textToSpeech.OnSpeak(removeOptions(associatedBlock.GetCompleteText()), speakerIcon, loadingIcon,
                    playingIcon);
            else
                DebugEditor.LogError("TextToSpeech object not found");
        }

        public void OnStop()
        {
            _textToSpeech.Stop();
        }

        // Method that removes everything in square brackets from the input string
        private string removeOptions(string input)
        {
            if (string.IsNullOrEmpty(input)) return "Error";
            input = Regex.Replace(input, @"\[.*?\]", string.Empty);
            return input;
        }
    }
}