using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Uralstech.UGemini;
using Uralstech.UGemini.Models.Content;
using Uralstech.UGemini.Models.Generation.Chat;
using Uralstech.UGemini.Models.Generation.Tools.Declaration;
using Uralstech.UGemini.Models.Generation.Tools.Declaration.GoogleSearch;

namespace i5.LLM_AR_Tourguide.Gemini
{
    public class GoogleGroundingChatManager : MonoBehaviour
    {
        private readonly GeminiTool _geminiSearchGrounding = new()
        {
            //GoogleSearch = new GeminiGoogleSearchRetrieval()
            //{
            //    DynamicRetrievalConfig = new GeminiDynamicRetrievalConfig
            //    {
            //        DynamicThreshold = 0.5f,
            //        Mode = GeminiDynamicRetrievalMode.Dynamic
            //    }
            //}
        };


        public List<GeminiContent> _chatHistory = new();

        private GeminiRole _senderRole = GeminiRole.User;
        private bool _settingSystemPrompt;
        private GeminiContent _systemPrompt;
#if UNITY_EDITOR
        [ContextMenu("TestThisGoogle")]
#endif
        private async Task test()
        {
            DebugEditor.Log("Testing");
            var response = await OnChat("Who won the latest F1 grand prix?");
            DebugEditor.Log(response);
        }

        public void SetRoleToSystemPrompt()
        {
            _settingSystemPrompt = true;
        }

        public void SetRole(GeminiRole role)
        {
            _senderRole = role;
        }

        public void SetRole(int role)
        {
            if (role > (int)GeminiRole.ToolResponse)
            {
                _settingSystemPrompt = true;
                return;
            }

            _senderRole = (GeminiRole)role;
        }

        public async Task<string> OnChat(string text)
        {
            GeminiContent addedContent;
            if (_settingSystemPrompt)
            {
                _systemPrompt = GeminiContent.GetContent(text);
            }
            else
            {
                addedContent = GeminiContent.GetContent(text, GeminiRole.User);
                _chatHistory.Add(addedContent);
            }

            if (_chatHistory.Count == 0)
                return "Something went wrong!";

            DebugEditor.Log("Requesting sending...");
            try
            {
                var response = await GeminiManager.Instance.Request<GeminiChatResponse>(
                    new GeminiChatRequest("gemini-2.0-flash", true)
                    {
                        Contents = _chatHistory.ToArray(),
                        SystemInstruction = _systemPrompt,
                        //Tools = new[] { _geminiSearchGrounding }
                    });
                DebugEditor.Log("Received response.");
                var responseText = string.Join(", ",
                    Array.ConvertAll(response.Parts,
                        part =>
                            $"{part.Text}"));

                addedContent = GeminiContent.GetContent(responseText, GeminiRole.Assistant);

                _chatHistory.Add(addedContent);

                return responseText;
            }
            catch (Exception e)
            {
                DebugEditor.LogError($"Error: {e.Message}");
                throw;
            }
        }
    }
}