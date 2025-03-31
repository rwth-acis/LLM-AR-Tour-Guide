using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using i5.LLM_AR_Tourguide.Prefab_Scripts;
using UnityEngine;
using UnityEngine.UI;
using Uralstech.UGemini;
using Uralstech.UGemini.Models;
using Uralstech.UGemini.Models.Content;
using Uralstech.UGemini.Models.Generation.Chat;

namespace i5.LLM_AR_Tourguide.Gemini
{
    public class OnboardingChatManager : MonoBehaviour
    {
        [SerializeField] private bool _useBeta = true;
        [SerializeField] private Dropdown _fileType;
        [SerializeField] private Transform _chatMessages;
        [SerializeField] private MessageBlock _chatMessagePrefab;

        public bool readOutResponses = true;

        private readonly List<GeminiContent> _chatHistory = new();
        private readonly List<GeminiContentPart> _uploadedData = new();


        private GeminiRole _senderRole = GeminiRole.User;
        private bool _settingSystemPrompt;
        private GeminiContent _systemPrompt;

        public void SetRole(int role)
        {
            if (role > (int)GeminiRole.ToolResponse)
            {
                _settingSystemPrompt = true;
                return;
            }

            _senderRole = (GeminiRole)role;
        }

        public async void OnAddFile(string filePath)
        {
            byte[] data;
            try
            {
                data = await File.ReadAllBytesAsync(filePath);
            }
            catch (SystemException exception)
            {
                DebugEditor.LogError($"Failed to load file: {exception.Message}");
                return;
            }

            _uploadedData.Add(new GeminiContentPart
            {
                InlineData = new GeminiContentBlob
                {
                    MimeType = (GeminiContentType)_fileType.value,
                    Data = Convert.ToBase64String(data)
                }
            });

            DebugEditor.Log("Added file!");
        }

        /// <summary>
        ///     Sends a chat message to the Gemini API and returns the response.
        /// </summary>
        /// <param name="userInput">The user message which will be shown to the user.</param>
        /// <param name="modifiedUserInput">The message that will be sent to Gemini</param>
        /// <returns></returns>
        public async Task<MessageBlock[]> OnChat(string userInput, string modifiedUserInput = "")
        {
            var text = userInput;
            if (!string.IsNullOrWhiteSpace(modifiedUserInput)) text = modifiedUserInput;
            if (string.IsNullOrWhiteSpace(text))
            {
                DebugEditor.LogError("Chat text should not be null or whitespace!");
                return null;
            }

            GeminiContent addedContent;
            GeminiContent addedContentUser;

            if (_settingSystemPrompt)
            {
                if (!_useBeta)
                {
                    DebugEditor.LogError("System prompts are not yet supported in the non-beta API!");
                    return null;
                }

                addedContent = _systemPrompt = GeminiContent.GetContent(text);
                addedContentUser = GeminiContent.GetContent(userInput);
            }
            else
            {
                addedContent = GeminiContent.GetContent(text, _senderRole);
                addedContentUser = GeminiContent.GetContent(userInput, GeminiRole.User);
                if (_uploadedData.Count > 0)
                {
                    addedContent.Parts = addedContent.Parts.Concat(_uploadedData).ToArray();
                    addedContentUser.Parts = addedContentUser.Parts.Concat(_uploadedData).ToArray();
                    _uploadedData.Clear();
                }

                _chatHistory.Add(addedContent);
            }

            MessageBlock first;
            MessageBlock second;

            first = AddMessage(addedContentUser, _settingSystemPrompt);

            _settingSystemPrompt = false;
            if (_chatHistory.Count == 0)
                return new[] { first };
            GeminiChatResponse response;
            try
            {
                response = await GeminiManager.Instance.Request<GeminiChatResponse>(
                    new GeminiChatRequest("gemini-2.0-flash", _useBeta)
                    {
                        Contents = _chatHistory.ToArray(),
                        SystemInstruction = _systemPrompt
                    });
            }
            catch (Exception e)
            {
                PermissionRequest.ShowToastMessage("Please wait...");
                DebugEditor.LogError($"Failed to send chat message: {e.Message}");
                try
                {
                    response = await GeminiManager.Instance.Request<GeminiChatResponse>(
                        new GeminiChatRequest(GeminiModel.Gemini1_5Flash, _useBeta)
                        {
                            Contents = _chatHistory.ToArray(),
                            SystemInstruction = _systemPrompt
                        });
                }
                catch (Exception secondE)
                {
                    PermissionRequest.ShowToastMessage("Failed to send chat message: " + e.Message);
                    DebugEditor.LogError($"Failed to send chat message: {secondE.Message}");
                    return new[] { first };
                }
            }


            _chatHistory.Add(response.Candidates[0].Content);
            second = AddMessage(response.Candidates[0].Content);

            return new[] { first, second };
        }

        private MessageBlock AddMessage(GeminiContent content, bool isSystemPrompt = false)
        {
            var message = Instantiate(_chatMessagePrefab, _chatMessages);
            if (!isSystemPrompt)
            {
                if (content.Role == GeminiRole.User)
                    message.SetupUserMessage(content.Parts[0].Text, null);
                else
                    message.SetupGuideMessage(content.Parts[0].Text, null, readOutResponses);

                message.gameObject.SetActive(true);
            }
            else
            {
                message.gameObject.SetActive(false);
            }

            var velocity = content.Parts[0].Text.Length * 5;
            velocity = velocity < 100 ? 100 : velocity;
            velocity = velocity > 500 ? 500 : velocity;
            var parentScrollRect = _chatMessages.parent.GetComponent<ScrollRect>();
            if (parentScrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                parentScrollRect.velocity = new Vector2(0, velocity);
            }

            var grandparentScrollRect = _chatMessages.parent.parent.GetComponent<ScrollRect>();
            if (grandparentScrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                grandparentScrollRect.velocity = new Vector2(0, velocity);
            }

            return message;
        }

        public async Task<string> OnUpdateSummery(string userInput)
        {
            var text = userInput;
            if (string.IsNullOrWhiteSpace(text))
            {
                DebugEditor.LogError("Chat text should not be null or whitespace!");
                return null;
            }

            GeminiContent addedContent;
            GeminiContent addedContentUser;

            if (_settingSystemPrompt)
            {
                if (!_useBeta)
                {
                    DebugEditor.LogError("System prompts are not yet supported in the non-beta API!");
                    return null;
                }

                addedContent = _systemPrompt = GeminiContent.GetContent(text);
                addedContentUser = GeminiContent.GetContent(userInput);
            }
            else
            {
                addedContent = GeminiContent.GetContent(text, _senderRole);
                addedContentUser = GeminiContent.GetContent(userInput, GeminiRole.User);
                if (_uploadedData.Count > 0)
                {
                    addedContent.Parts = addedContent.Parts.Concat(_uploadedData).ToArray();
                    addedContentUser.Parts = addedContentUser.Parts.Concat(_uploadedData).ToArray();
                    _uploadedData.Clear();
                }

                _chatHistory.Add(addedContent);
            }

            _settingSystemPrompt = false;
            if (_chatHistory.Count == 0)
                return "[No]";
            GeminiChatResponse response;
            try
            {
                response = await GeminiManager.Instance.Request<GeminiChatResponse>(
                    new GeminiChatRequest("gemini-2.0-flash", _useBeta)
                    {
                        Contents = _chatHistory.ToArray(),
                        SystemInstruction = _systemPrompt
                    });
            }
            catch (Exception e)
            {
                PermissionRequest.ShowToastMessage("Please wait...");
                DebugEditor.LogError($"Failed to send chat message: {e.Message}");
                try
                {
                    response = await GeminiManager.Instance.Request<GeminiChatResponse>(
                        new GeminiChatRequest(GeminiModel.Gemini1_5Flash, _useBeta)
                        {
                            Contents = _chatHistory.ToArray(),
                            SystemInstruction = _systemPrompt
                        });
                }
                catch (Exception secondE)
                {
                    PermissionRequest.ShowToastMessage("Failed to send chat message: " + e.Message);
                    DebugEditor.LogError($"Failed to send chat message: {secondE.Message}");
                    return "[No]";
                }
            }

            _chatHistory.Add(response.Candidates[0].Content);

            return response.Parts[0].Text;
        }

        public string GetChatHistoryAsString()
        {
            return string.Join("\n", _chatHistory.Select(content => content.Parts[0].Text));
        }

        public void deleteChatHistory()
        {
            _chatHistory.Clear();
        }
    }
}