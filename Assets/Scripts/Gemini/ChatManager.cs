using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Uralstech.UGemini;
using Uralstech.UGemini.Models;
using Uralstech.UGemini.Models.Content;
using Uralstech.UGemini.Models.Generation.Chat;

namespace i5.LLM_AR_Tourguide.Gemini
{
    public class ChatManager : MonoBehaviour
    {
        //[SerializeField] private Transform _chatMessages;

        private readonly List<GeminiContent> _chatHistory = new();
        private readonly List<GeminiContentPart> _uploadedData = new();
        private readonly bool _useBeta = true;
        private string _chatInput;

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

        public async Task<string> OnChat()
        {
            var text = _chatInput;
            if (string.IsNullOrWhiteSpace(text))
            {
                DebugEditor.LogError("Chat text should not be null or whitespace!");
                return string.Empty;
            }

            GeminiContent addedContent;

            if (_settingSystemPrompt)
            {
                if (!_useBeta)
                {
                    DebugEditor.LogError("System prompts are not yet supported in the non-beta API!");
                    return string.Empty;
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

            _settingSystemPrompt = false;
            if (_chatHistory.Count == 0)
                return string.Empty;

            var response = await GeminiManager.Instance.Request<GeminiChatResponse>(
                new GeminiChatRequest(GeminiModel.Gemini1_5Flash, _useBeta)
                {
                    Contents = _chatHistory.ToArray(),
                    SystemInstruction = _systemPrompt
                });

            _chatHistory.Add(response.Candidates[0].Content);
            DebugEntireChatHistory();
            // Return response message
            return response.Candidates[0].Content.Parts[0].Text;
        }

        //private void AddMessage(GeminiContent content, bool isSystemPrompt = false)
        //{
        //    UIChatMessage message = Instantiate(_chatMessagePrefab, _chatMessages);
        //    message.Setup(content, isSystemPrompt);
        //}

        public async Task<string> sendOneMessageAsync(string message)
        {
            _chatInput = message;
            var response = await OnChat();
            if (_settingSystemPrompt) return "System prompt set.";
            return response;
        }

        private void DebugEntireChatHistory()
        {
            DebugEditor.Log("Chat History:");
            foreach (var content in _chatHistory)
            foreach (var part in content.Parts)
                DebugEditor.Log(part.Text);
        }
    }
}