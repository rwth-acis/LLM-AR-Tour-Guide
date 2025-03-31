using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Uralstech.UGemini;
using Uralstech.UGemini.Models;
using Uralstech.UGemini.Models.Content;
using Uralstech.UGemini.Models.Generation.Chat;

namespace i5.LLM_AR_Tourguide.Gemini
{
    public class MultiTurnChatManager : MonoBehaviour
    {
        [SerializeField] private bool _useBeta = true;
        [SerializeField] public string _output;


        private readonly List<GeminiContent> _chatHistory = new();
        private readonly List<GeminiContentPart> _uploadedData = new();
        private Dropdown _fileType;

        private GeminiRole _senderRole = GeminiRole.User;
        private bool _settingSystemPrompt;
        private GeminiContent _systemPrompt;

        public void SetRole(int role)
        {
            if (role > (int)GeminiRole.ToolResponse)
            {
                _settingSystemPrompt |= true;
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

        public async Task<string> OnChat(string message)
        {
            var text = message;
            if (string.IsNullOrWhiteSpace(text))
            {
                DebugEditor.LogError("Chat text should not be null or whitespace!");
                return "Chat text should not be null or whitespace!";
            }

            GeminiContent addedContent;

            if (_settingSystemPrompt)
            {
                if (!_useBeta)
                {
                    DebugEditor.LogError("System prompts are not yet supported in the non-beta API!");
                    return "System prompts are not yet supported in the non-beta API!";
                }

                addedContent = _systemPrompt = GeminiContent.GetContent(text);
            }
            else
            {
                addedContent = GeminiContent.GetContent(text, _senderRole);
                if (_uploadedData.Count > 0)
                {
                    addedContent.Parts = addedContent.Parts.Concat(_uploadedData).ToArray();
                    _uploadedData.Clear();
                }

                _chatHistory.Add(addedContent);
            }

            AddMessage(addedContent, _settingSystemPrompt);

            _settingSystemPrompt = false;
            if (_chatHistory.Count == 0)
                return "Something went wrong!";

            var response = await GeminiManager.Instance.Request<GeminiChatResponse>(
                new GeminiChatRequest(GeminiModel.Gemini1_5Flash, _useBeta)
                {
                    Contents = _chatHistory.ToArray(),
                    SystemInstruction = _systemPrompt
                });

            _chatHistory.Add(response.Candidates[0].Content);
            AddMessage(response.Candidates[0].Content);
            return response.Candidates[0].Content.Parts[0].Text;
        }

        private void AddMessage(GeminiContent content, bool isSystemPrompt = false)
        {
            if (content.Parts == null)
            {
                DebugEditor.LogError("Content does not contain any parts!");
                return;
            }

            foreach (var part in content.Parts)
                if (part.Text != null)
                    _output += $"{(isSystemPrompt ? "System" : _senderRole.ToString())}: {part.Text}\n";
        }
    }
}