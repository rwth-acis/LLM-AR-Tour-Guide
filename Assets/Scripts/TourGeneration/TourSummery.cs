using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using i5.LLM_AR_Tourguide.Guide_Scripts;
using Newtonsoft.Json;
using UnityEngine;
using Uralstech.UGemini;
using Uralstech.UGemini.Models.Content;
using Uralstech.UGemini.Models.Generation.Chat;

namespace i5.LLM_AR_Tourguide.TourGeneration
{
    public class TourSummery : MonoBehaviour
    {
        private readonly string summeryHistoryKey = "summeryHistory";

        private GuideManager _guideManager;
        private GeminiContent[][] chatHistories;

        public void Start()
        {
            if (!_guideManager) _guideManager = FindAnyObjectByType<GuideManager>();

            var pointOfInterestManager = FindAnyObjectByType<PointOfInterestManager>();
            chatHistories = new GeminiContent[pointOfInterestManager.getAllPointOfInterests().Length][];
            // Load chatHistory from PlayerPrefs
            if (PlayerPrefs.HasKey(summeryHistoryKey))
                chatHistories =
                    JsonConvert.DeserializeObject<GeminiContent[][]>(PlayerPrefs.GetString(summeryHistoryKey));
        }

        public void AddChatHistory(List<GeminiContent> chat, int index)
        {
            if (index < 0 || index >= chatHistories.Length) return;

            chatHistories[index] = chat.ToArray();
            PlayerPrefs.SetString(summeryHistoryKey, JsonConvert.SerializeObject(chatHistories));
        }

        public async Task<string> GenerateSummery()
        {
            if (!_guideManager) _guideManager = FindAnyObjectByType<GuideManager>();

            var history = new List<GeminiContent>();
            // Add all chat histories to the list
            foreach (var chatHistory in chatHistories)
            {
                if (chatHistory == null)
                    continue;
                history.AddRange(chatHistory);
            }

            DebugEditor.Log("Summery messages: " + history.ToArray());

            var message = GeminiContent.GetContent(
                "Write a summery to conclude the current tour. Address the user directly. Afterwards the user will have to fill out a questioner on the next page. Thank them for their participation.",
                GeminiRole.User);
            history.Add(message);
            var prompt = _guideManager.GetChosenGuideInfo().longDescription;
            var systemPrompt =
                GeminiContent.GetContent(prompt +
                                         " You goal is to summarize the tour, based on the previous messages.");
            GeminiChatResponse chatResponse;
            try
            {
                chatResponse = await GeminiManager.Instance.Request<GeminiChatResponse>(
                    new GeminiChatRequest("gemini-2.0-flash", true)
                    {
                        Contents = history.ToArray(),
                        SystemInstruction = systemPrompt
                    });
            }
            catch (Exception e)
            {
                DebugEditor.LogError(e);
                DebugEditor.LogError(history.ToArray());
                throw e;
            }


            var responseText = string.Join(", ",
                Array.ConvertAll(chatResponse.Parts,
                    part =>
                        $"{part.Text}"));

            return responseText;
        }
    }
}