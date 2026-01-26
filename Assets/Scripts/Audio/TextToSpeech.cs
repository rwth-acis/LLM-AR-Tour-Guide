using System;
using System.Collections;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using i5.LLM_AR_Tourguide.Guide_Scripts;
using i5.LLM_AR_Tourguide.Ready_Player_Me.Scripts;
using UnityEngine;
using UnityEngine.Networking;
using Uralstech.UCloud.TextToSpeech;
using Uralstech.UCloud.TextToSpeech.Synthesis;

namespace i5.LLM_AR_Tourguide.Audio
{
    public class TextToSpeech : MonoBehaviour
    {
        private const string CacheDirectory = "AudioCache";

        [SerializeField] private AudioSource _audioSource;


        public int numberOfCharactersUsed;
        public int delayResponseTimeInMs;

        public bool testingMode = true;
        public string testMessage = "A";

        [SerializeField] [Range(0.5f, 2f)] private float pitchMultiplier = 1;
        private readonly int maxCharactersAllowed = 50000;

        private GuideManager guideManager;

        protected void Start()
        {
            guideManager = FindAnyObjectByType<GuideManager>();
            if (_audioSource == null)
                if (!TryGetComponent(out _audioSource))
                    _audioSource = gameObject.AddComponent<AudioSource>();

            _audioSource.playOnAwake = false;
            _audioSource.loop = false;
            _audioSource.clip = null;

            // Ensure the cache directory exists
            if (!Directory.Exists(Path.Combine(Application.persistentDataPath, CacheDirectory)))
                Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, CacheDirectory));

            if (PlayerPrefs.HasKey("numberOfCharactersUsed"))
                numberOfCharactersUsed = PlayerPrefs.GetInt("numberOfCharactersUsed");
        }

        public void OnSpeak(string input, GameObject speakerIcon, GameObject loadingIcon, GameObject playingIcon)
        {
            if (testingMode) input = testMessage;
            Debug.Log("TTS On speak:" + input);
            if (!speakerIcon.activeSelf) return;
            speakerIcon.SetActive(false);
            StartCoroutine(WaitForAudioToStart(speakerIcon, loadingIcon, playingIcon, _audioSource.clip));
            Speak(input, speakerIcon, loadingIcon, playingIcon);
        }

        public void OnSpeak(string input, string voiceName = "")
        {
            if (testingMode) input = testMessage;
            _ = Speak(input);
        }

        private async void Speak(string text, GameObject speakerIcon, GameObject loadingIcon, GameObject playingItem,
            string voiceName = "")
        {
            if (voiceName == "")
                voiceName = GetVoiceName();
            const TextToSpeechSynthesisAudioEncoding encoding = TextToSpeechSynthesisAudioEncoding.WavLinear16;
            var cacheFileName = GenerateCacheFileName(text, voiceName, encoding);
            var cacheFilePath = Path.Combine(Application.persistentDataPath, CacheDirectory, cacheFileName);

            AudioClip clip;

#if !DEVELOPMENT_BUILD && !UNITY_EDITOR
        delayResponseTimeInMs = 0;
#endif


            await Task.Delay(delayResponseTimeInMs);

            if (File.Exists(cacheFilePath))
            {
                // Load from cache
                DebugEditor.Log("Loading audio from cache: " + text);
                clip = await LoadAudioClipFromCache(cacheFilePath);
            }
            else
            {
                if (numberOfCharactersUsed + text.Length > maxCharactersAllowed &&
                    !text.Equals("My voice is tired. I can't speak anymore."))
                {
                    DebugEditor.LogError("Max characters allowed reached.");
                    text = "My voice is tired. I can't speak anymore.";
                    Speak(text, speakerIcon, loadingIcon, playingItem, voiceName);
                    return;
                }

                text = cleanString(text);

                // Request from API and save to cache
                DebugEditor.Log("Sending TTS request with text: " + text);
                var languageCode = LanguageManager.GetLanguageCode();
                var response =
                    await TextToSpeechManager.Instance.Request<TextToSpeechSynthesisResponse>(
                        new TextToSpeechSynthesisRequest
                        {
                            Input = new TextToSpeechSynthesisInput(text),
                            Voice = new TextToSpeechSynthesisVoiceSelection(languageCode) { Name = voiceName },
                            AudioConfiguration = new TextToSpeechSynthesisAudioConfiguration(encoding)
                        });
                numberOfCharactersUsed += text.Length;

                DebugEditor.Log("TTS response received, saving to cache and playing audio.");
                DebugEditor.Log("Remaining characters: " + (maxCharactersAllowed - numberOfCharactersUsed));
                PlayerPrefs.SetInt("numberOfCharactersUsed", numberOfCharactersUsed);
                clip = await response.ToAudioClip(encoding);
                SaveAudioClipToCache(clip, cacheFilePath);
            }

            //DebugEditor.Log("Playing audio with voice: " + voiceName);
            PlayOnGuideIfPossible(clip);

            StartCoroutine(WaitForAudioToEnd(speakerIcon, loadingIcon, playingItem, clip));
        }


        public async Task<bool> SaveToCache(string input, string voiceName = "")
        {
            if (testingMode) input = testMessage;
            if (voiceName == "")
                voiceName = GetVoiceName();
            const TextToSpeechSynthesisAudioEncoding encoding = TextToSpeechSynthesisAudioEncoding.WavLinear16;
            var cacheFileName = GenerateCacheFileName(input, voiceName, encoding);
            var cacheFilePath = Path.Combine(Application.persistentDataPath, CacheDirectory, cacheFileName);

            AudioClip clip;

            if (File.Exists(cacheFilePath)) return true;
            if (numberOfCharactersUsed + input.Length > maxCharactersAllowed) return false;

            input = cleanString(input);

            // Request from API and save to cache
            DebugEditor.Log("Sending TTS request with text: " + input);
            var languageCode = LanguageManager.GetLanguageCode();
            var response =
                await TextToSpeechManager.Instance.Request<TextToSpeechSynthesisResponse>(
                    new TextToSpeechSynthesisRequest
                    {
                        Input = new TextToSpeechSynthesisInput(input),
                        Voice = new TextToSpeechSynthesisVoiceSelection(languageCode) { Name = voiceName },
                        AudioConfiguration = new TextToSpeechSynthesisAudioConfiguration(encoding)
                    });
            numberOfCharactersUsed += input.Length;

            DebugEditor.Log("TTS response received, saving to cache and playing audio.");
            DebugEditor.Log("Remaining characters: " + (maxCharactersAllowed - numberOfCharactersUsed));
            PlayerPrefs.SetInt("numberOfCharactersUsed", numberOfCharactersUsed);
            clip = await response.ToAudioClip(encoding);
            SaveAudioClipToCache(clip, cacheFilePath);
            return true;
        }

        public void PlayOnGuideIfPossible(AudioClip clip)
        {
            var guide = guideManager.GetChosenGuideInfo().guideGameObject;
            if (guide.activeSelf)
            {
                var vH = guide.GetComponent<VoiceHandler>();
                if (vH)
                {
                    vH.PlayAudioClip(clip);
                    _audioSource = vH.AudioSource;
                    _audioSource.pitch = pitchMultiplier;
                    return;
                }
            }


            // Otherwise play on the default audio source
            _audioSource.clip = clip;
            _audioSource.pitch = pitchMultiplier;
            _audioSource.Play();
        }

        private string GetVoiceName()
        {
            if (guideManager) guideManager = FindAnyObjectByType<GuideManager>();
            if (guideManager.GetChosenGuideInfo() == null) return "";
            if (PlayerPrefs.GetString("Language") == "de")
                return guideManager.GetChosenGuideInfo().voiceNameGerman;
            return guideManager.GetChosenGuideInfo().voiceNameEnglish;
        }

        private async Task<bool> Speak(string text, string voiceName = "")
        {
            if (voiceName == "")
                voiceName = GetVoiceName();
            if (voiceName == "") return false;
            const TextToSpeechSynthesisAudioEncoding encoding = TextToSpeechSynthesisAudioEncoding.WavLinear16;
            var cacheFileName = GenerateCacheFileName(text, voiceName, encoding);
            var cacheFilePath = Path.Combine(Application.persistentDataPath, CacheDirectory, cacheFileName);

            AudioClip clip;

#if !DEVELOPMENT_BUILD && !UNITY_EDITOR
        delayResponseTimeInMs = 0;
#endif
            await Task.Delay(delayResponseTimeInMs);


            if (File.Exists(cacheFilePath))
            {
                // Load from cache
                DebugEditor.Log("Loading audio from cache.");
                clip = await LoadAudioClipFromCache(cacheFilePath);
            }
            else
            {
                var languageCode = LanguageManager.GetLanguageCode();

                text = cleanString(text);
                // Request from API and save to cache
                DebugEditor.Log("Sending TTS request with text: " + text);
                var response =
                    await TextToSpeechManager.Instance.Request<TextToSpeechSynthesisResponse>(
                        new TextToSpeechSynthesisRequest
                        {
                            Input = new TextToSpeechSynthesisInput(text),
                            Voice = new TextToSpeechSynthesisVoiceSelection(languageCode) { Name = voiceName },
                            AudioConfiguration = new TextToSpeechSynthesisAudioConfiguration(encoding)
                        });

                DebugEditor.Log("TTS response received, saving to cache and playing audio.");
                clip = await response.ToAudioClip(encoding);
                SaveAudioClipToCache(clip, cacheFilePath);
            }

            //DebugEditor.Log("Playing audio with voice: " + voiceName);
            PlayOnGuideIfPossible(clip);
            return true;
        }

        private IEnumerator WaitForAudioToStart(GameObject speakerIcon, GameObject loadingIcon, GameObject playingIcon,
            AudioClip oldAudioClip)
        {
            if (!speakerIcon || !playingIcon || !loadingIcon) yield break;
            while (!_audioSource.isPlaying || _audioSource.clip == oldAudioClip)
            {
                if (!speakerIcon || !playingIcon || !loadingIcon) yield break;
                speakerIcon.SetActive(false);
                loadingIcon.SetActive(true);
                yield return null;
            }

            // It started playing
            playingIcon?.SetActive(false);
            loadingIcon?.SetActive(false);
            playingIcon?.SetActive(true);
        }

        private IEnumerator WaitForAudioToEnd(GameObject speakerIcon, GameObject loadingIcon, GameObject playingIcon,
            AudioClip audioClip)
        {
            if (!speakerIcon || !playingIcon || !loadingIcon) yield break;
            while (_audioSource.isPlaying && _audioSource.clip == audioClip)
            {
                if (!speakerIcon || !playingIcon) yield break;
                speakerIcon.SetActive(false);
                yield return null;
            }

            // It stopped playing
            loadingIcon?.SetActive(false);
            playingIcon?.SetActive(false);
            speakerIcon?.SetActive(true);
        }

        private string GenerateCacheFileName(string text, string voiceName, TextToSpeechSynthesisAudioEncoding encoding)
        {
            // Generate a unique file name based on input text, voice, and encoding
            var hash = ComputeHash(text + voiceName + encoding);
            return $"{hash}.wav";
        }

        private string ComputeHash(string input)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(input);
                var hash = sha256.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }

        private async Task<AudioClip> LoadAudioClipFromCache(string filePath)
        {
            using var www = UnityWebRequestMultimedia.GetAudioClip("file:///" + filePath, AudioType.WAV);
            await www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success) return DownloadHandlerAudioClip.GetContent(www);
            DebugEditor.LogError($"Error loading audio clip from cache: {www.error}");
            return null;

        }

        public void Stop()
        {
            if (_audioSource.isPlaying) _audioSource.Stop();
        }

        private void SaveAudioClipToCache(AudioClip clip, string filePath)
        {
            SavWav.Save(filePath, clip);
            DebugEditor.Log($"Audio clip saved to cache: {filePath}");
        }

        private string cleanString(string text)
        {
            text = Regex.Replace(text, @"\[.*?\]", string.Empty);
            var sb = new StringBuilder();
            foreach (var c in text)
                switch (c)
                {
                    case '*':
                    case '_':
                    case '~':
                    case '`':
                    case '>':
                    case '<':
                    case '#':
                        // Skip these characters (remove them)
                        break;
                    default:
                        sb.Append(c); // Append other characters
                        break;
                }

            var tempResult = sb.ToString();
            // Handle newline replacements in sequence, starting with \r\n
            tempResult = tempResult.Replace("\r\n", "...");
            tempResult = tempResult.Replace("\n", "...");
            tempResult = tempResult.Replace("\r", "...");
            tempResult = tempResult.Replace("?", "?...");

            return tempResult;
        }
    }
}