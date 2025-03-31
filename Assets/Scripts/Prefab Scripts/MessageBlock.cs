using System;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using i5.LLM_AR_Tourguide.Audio;
using i5.LLM_AR_Tourguide.Guide_Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace i5.LLM_AR_Tourguide.Prefab_Scripts
{
    public class MessageBlock : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _senderText;
        [SerializeField] private TextMeshProUGUI _messageText;
        [SerializeField] private GameObject _imageCointainer;
        [SerializeField] private RawImage _messageImage;

        [SerializeField] private GameObject _avatarIcon;
        [SerializeField] private Image _avatarIconImage;
        [SerializeField] private GameObject _letterIcon;
        [SerializeField] private float revealSpeed = 0.02f;

        private TextToSpeechCaller _textToSpeechCaller;

        /// <summary>
        ///     The current complete message text, with Markdown formatting. Text is only visible after ConvertMarkdownToRichText
        ///     is called
        /// </summary>
        private string currentCompleteMessage;

        private GuideManager guideManager;


        private float leftOver;

        private bool revealMessageOverTime;

        private ScrollRect theScrollRect;

        private void Start()
        {
            var revealSpeedFaster = PlayerPrefs.GetInt("revealSpeed", 0);
            if (revealSpeedFaster == 1)
                revealSpeed = 0.01f;
            else
                revealSpeed = 0.02f;
            guideManager = FindAnyObjectByType<GuideManager>();
        }

        private void OnDisable()
        {
            _messageText.text = ConvertMarkdownToRichText(currentCompleteMessage);
        }

        public void CircumventText(string message)
        {
            currentCompleteMessage = message;
        }

        public string CircumventGetText()
        {
            return currentCompleteMessage;
        }


        public void stopRevealingMessage()
        {
            revealMessageOverTime = false;
        }

        public void stopReavealingAllOthers()
        {
            var container = transform.parent.gameObject;
            foreach (Transform child in container.transform)
                if (child.gameObject != gameObject)
                {
                    var block = child.GetComponent<MessageBlock>();
                    if (block)
                        block.stopRevealingMessage();
                }
        }

        public void SetupUserMessage(string message, string imageLocation)
        {
            DebugEditor.Log("Setting up user message");
            Start();
            _senderText.text = "You";
            currentCompleteMessage = message;
            _messageText.text = message;
            _avatarIcon.SetActive(false);
            _letterIcon.SetActive(true);

            // Image not tested
            if (!string.IsNullOrEmpty(imageLocation))
            {
                _imageCointainer.gameObject.SetActive(true);
                Texture2D texture = new(1, 1);
                texture.LoadImage(Convert.FromBase64String(imageLocation));
                _messageImage.texture = texture;
            }
            else
            {
                _imageCointainer.SetActive(false);
            }

            stopReavealingAllOthers();
        }

        public bool isUserMessage()
        {
            return _letterIcon.activeSelf;
        }

        public void SetupGuideMessage(string message, string imageLocation, bool readOutMessage = false)
        {
            DebugEditor.Log("Setting up guide message");
            Start();
            if (string.IsNullOrEmpty(message))
                return;
            if (isActiveAndEnabled)
            {
                revealMessageOverTime = readOutMessage;
                StartCoroutine(RevealMessage(message));
            }
            else
            {
                revealMessageOverTime = false;
                currentCompleteMessage = message;
                _messageText.text = ConvertMarkdownToRichText(message);
            }

            _senderText.text = "";
            _avatarIcon.SetActive(true);
            _letterIcon.SetActive(false);

            if (!guideManager)
                guideManager = FindAnyObjectByType<GuideManager>();

            var image = _avatarIconImage;
            if (image)
                image.sprite = guideManager.GetChosenGuideInfo().avatarImage;

            // Image not tested
            if (!string.IsNullOrEmpty(imageLocation))
            {
                _imageCointainer.gameObject.SetActive(true);
                Texture2D texture = new(1, 1);
                texture.LoadImage(Convert.FromBase64String(imageLocation));
                _messageImage.texture = texture;
            }
            else
            {
                _imageCointainer.SetActive(false);
            }

            if (readOutMessage)
                this.readOutMessage();

            stopReavealingAllOthers();
        }

        private static string ConvertMarkdownToRichText(string markdownText)
        {
            if (string.IsNullOrEmpty(markdownText)) return markdownText; // Handle null or empty input

            var richText = markdownText;

            // 0. Remove any actions that might have been generated
            richText = Regex.Replace(richText, @"\[.*?\]", string.Empty);

            // 1. Bold (**bold** or __bold__) - Process first to avoid conflicts with italic
            richText = Regex.Replace(richText, @"\*\*([^*]+)\*\*|__([^_]+)__", "<b>$1$2</b>");

            // 2. Italic (*italic* or _italic_) - Process after bold
            richText = Regex.Replace(richText, @"\*([^*]+)\*|_([^_]+)_", "<i>$1$2</i>");

            // 3. Simple Headings (H1-H3) -  Bolding for simplicity.  You could expand this or handle sizes differently.
            richText = Regex.Replace(richText, @"^# (.*)$", "<b>$1</b>", RegexOptions.Multiline);
            richText = Regex.Replace(richText, @"^## (.*)$", "<b>$1</b>", RegexOptions.Multiline);
            richText = Regex.Replace(richText, @"^### (.*)$", "<b>$1</b>", RegexOptions.Multiline);

            // 5. Line Breaks (simplest newline to <br>) -  Consider more sophisticated handling if needed (paragraphs, etc.)
            richText = richText.Replace("\n", "<br>");

            richText = richText.Replace("<br><br>", "<br>");

            // 6. Remove Line Break at the beginning of the text
            richText = richText.Trim();
            richText = Regex.Replace(richText, @"^(<br>)+", string.Empty);

            return richText;
        }

        private IEnumerator RevealMessage(string message)
        {
            // Use TextMeshPro's rich text parsing (no need for Regex)
            currentCompleteMessage = message;
            _messageText.text = ""; // Start with an empty string


            var processedMessage = ConvertMarkdownToRichText(message);

            ScrollDown(50);

            if (!revealMessageOverTime || !isActiveAndEnabled)
            {
                DebugEditor.Log("Revealing message instantly");
                _messageText.text = processedMessage;
                yield break;
            }

            var sb = new StringBuilder(processedMessage.Length * 2);
            var visibleCharacterIndex = 0; // Keep track of visible characters

            while (visibleCharacterIndex <= processedMessage.Length)
            {
                // Build the displayed text.  Use rich text tags, but control visibility.
                _messageText.text = processedMessage[..visibleCharacterIndex];
                var messageTextText = _messageText.text;
                var isInTag = 0;
                foreach (var c in messageTextText)
                {
                    if (c == '<') isInTag++;
                    if (c == '>') isInTag--;
                }

                if (isInTag < 0)
                    DebugEditor.LogWarning("Something is wrong with the tags: " + messageTextText);

                // Append invisible characters for the remainder of the string.  Key change!
                sb.Clear();
                sb.Append(_messageText.text);
                sb.Append("<color=#00000000>");
                sb.Append(processedMessage[visibleCharacterIndex..]);
                sb.Append("</color>");
                _messageText.text = sb.ToString();


                //DebugEditor.Log(
                //    $"Revealing message: visibleCharacterIndex={visibleCharacterIndex}, leftOver={leftOver}, Time={Time.deltaTime}");
                visibleCharacterIndex += (int)((Time.deltaTime + leftOver) / revealSpeed);
                leftOver = Time.deltaTime % revealSpeed;

                // Check length so it doesn't try to wait after it's done
                if (revealMessageOverTime &&
                    visibleCharacterIndex <= processedMessage.Length)
                {
                    if (isInTag == 0) yield return null; // Wait for next frame
                }
                else
                {
                    _messageText.text = processedMessage;
                    yield break; // Exit the coroutine if not revealing over time.
                }
            }
        }

        private void ScrollDown(float velocity = 100)
        {
            DebugEditor.Log("Scrolling down by " + velocity);
            if (!theScrollRect)
            {
                var parentScrollRect = gameObject.transform.parent.parent.GetComponent<ScrollRect>();
                if (parentScrollRect) theScrollRect = parentScrollRect;

                var grandparentScrollRect = gameObject.transform.parent.parent.parent.GetComponent<ScrollRect>();
                if (grandparentScrollRect) theScrollRect = grandparentScrollRect;
            }

            if (!theScrollRect) return;
            Canvas.ForceUpdateCanvases();
            if (theScrollRect.normalizedPosition.x > 0.01f)
                theScrollRect.velocity += new Vector2(0, velocity);
            else
                theScrollRect.velocity -= new Vector2(0, 5);
        }

        public void UpdateMessage(string message)
        {
            SetText(message);
        }

        public string GetCompleteText()
        {
            if (string.IsNullOrEmpty(currentCompleteMessage))
                return _messageText.text;
            return currentCompleteMessage;
        }

        public void SetText(string text)
        {
            StopAllCoroutines();
            StartCoroutine(RevealMessage(text));
        }

        private void readOutMessage()
        {
            if (!_textToSpeechCaller)
                _textToSpeechCaller = GetComponentInChildren<TextToSpeechCaller>();
            if (_textToSpeechCaller)
                _textToSpeechCaller.OnSpeak();
            else
                DebugEditor.LogError("TextToSpeechCaller not found");
        }
    }
}