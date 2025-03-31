using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using i5.LLM_AR_Tourguide.Guide_Scripts;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using Uralstech.UGemini;
using Uralstech.UGemini.Models.Content;
using Uralstech.UGemini.Models.Generation;
using Uralstech.UGemini.Models.Generation.Chat;
using Uralstech.UGemini.Models.Generation.Schema;
using Uralstech.UGemini.Models.Generation.Tools.Declaration;

namespace i5.LLM_AR_Tourguide.Gemini
{
    public class TourGenerationChatManager : MonoBehaviour
    {
        [SerializeField] private bool _useBeta = true;
        [SerializeField] public string _output;

        public List<GeminiContent> _chatHistory = new();
        private Dropdown _fileType;

        private GeminiRole _senderRole = GeminiRole.User;
        private bool _settingSystemPrompt;
        private GeminiContent _systemPrompt;
        private List<GeminiContentPart> _uploadedData = new();
        private GuideManager guideManager;

        private GeminiGenerationConfiguration s_geminiGenerationConfiguration;

        private GeminiToolConfiguration s_geminiToolConfiguration;

        public void Reset()
        {
            _chatHistory = new List<GeminiContent>();
            _uploadedData = new List<GeminiContentPart>();
        }

        public void Start()
        {
            guideManager = FindAnyObjectByType<GuideManager>();
        }

        public GeminiTool getCurrentGeminiTools()
        {
            return new GeminiTool
            {
                FunctionDeclarations = new[]
                {
                    new GeminiFunctionDeclaration
                    {
                        Name = "pointInDirectionOfPointOfInterest",
                        Description =
                            "Makes you, the tour guide, point in the direction of one point of interest that might be interesting to the user. Use this when talking about one of the options that are not the current point of interest. The point of interest is specified by the 'pointOfInterest' parameter, options include the point of interests that are available in the tour, as well as points of interest surrounding them. Try to use those to point out features or nearby buildings to the user. Use this many times.",
                        Parameters = new GeminiSchema
                        {
                            Type = GeminiSchemaDataType.Object,
                            Properties = new Dictionary<string, GeminiSchema>
                            {
                                {
                                    "pointOfInterest", new GeminiSchema
                                    {
                                        Type = GeminiSchemaDataType.String,
                                        Description = "The point of interest on the tour to point at",
                                        Format = GeminiSchemaDataFormat.Enum,
                                        Enum = FindAnyObjectByType<PointOfInterestManager>()
                                            .getAllPOIAndActiveSubPoiTitlesAsString()
                                            .ToArray(),
                                        Nullable = false
                                    }
                                }
                            },
                            Required = new[] { "pointOfInterest" }
                        }
                    },
                    new GeminiFunctionDeclaration
                    {
                        Name = "waiveAtParticipant",
                        Description =
                            "Makes you, the tour guide, waive at the tour participant for 30 seconds. Use this function to greet the user."
                    },
                    new GeminiFunctionDeclaration
                    {
                        Name = "growBigger",
                        Description =
                            "Makes you, the tour guide, temporarily grow in size. Use this function to show how big buildings are or when the user asks you to do something funny."
                    },
                    new GeminiFunctionDeclaration
                    {
                        Name = "danceSilly",
                        Description =
                            "Makes you, the tour guide, dance silly. Only use this function when the user ask you to dance or to do something funny."
                    },
                    new GeminiFunctionDeclaration
                    {
                        Name = "shrinkSmaller",
                        Description =
                            "Makes you, the tour guide, temporarily shrink in size. Use this function to show how small things like flowers are."
                    },
                    new GeminiFunctionDeclaration
                    {
                        Name = "shakeHead",
                        Description =
                            "Makes you, the tour guide, temporarily shake your head. Use this function to show disagreement."
                    },
                    new GeminiFunctionDeclaration
                    {
                        Name = "gestureWithHands",
                        Description =
                            "Makes you, the tour guide, gesture with your hands. Only use this rarely."
                    }
                }
            };
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

        public async Task<(string, GeminiContentPart[])> OnChat(string message, bool generateMethod)
        {
            var text = message;
            if (string.IsNullOrWhiteSpace(text))
            {
                DebugEditor.LogError("Chat text should not be null or whitespace!");
                return ("Chat text should not be null or whitespace!", null);
            }

            GeminiContent addedContent;

            if (_settingSystemPrompt)
            {
                if (!_useBeta)
                {
                    DebugEditor.LogError("System prompts are not yet supported in the non-beta API!");
                    return ("System prompts are not yet supported in the non-beta API!", null);
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
                return ("Something went wrong!", null);


            GeminiChatResponse response = null;

            if (generateMethod)
            {
                s_geminiToolConfiguration = GeminiToolConfiguration.GetConfiguration(GeminiFunctionCallingMode.Any);
                s_geminiGenerationConfiguration =
                    new GeminiGenerationConfiguration
                    {
                        Temperature = 1f,
                        MaxOutputTokens = 100
                    };
            }
            else
            {
                s_geminiToolConfiguration = GeminiToolConfiguration.GetConfiguration(GeminiFunctionCallingMode.None);
                s_geminiGenerationConfiguration =
                    new GeminiGenerationConfiguration
                    {
                        Temperature = 1.3f,
                        MaxOutputTokens = 500
                    };
            }

            var maxRetries = 8;
            var retryCount = 0;

            while (retryCount < maxRetries)
                try
                {
                    response = await GeminiManager.Instance.Request<GeminiChatResponse>(
                        new GeminiChatRequest("gemini-2.0-flash", _useBeta)
                        {
                            Contents = _chatHistory.ToArray(),
                            SystemInstruction = _systemPrompt,
                            Tools = new[] { getCurrentGeminiTools() },
                            ToolConfig = s_geminiToolConfiguration,
                            GenerationConfig = s_geminiGenerationConfiguration
                        });
                    break; // Exit the loop if the request is successful
                }
                catch (Exception e)
                {
                    retryCount++;
                    if (e.Message.Contains("429"))
                    {
                        // Wait 10 seconds and try again
                        PermissionRequest.ShowToastMessage(
                            "We are expiring high demand at the moment. Please wait...");
                        DebugEditor.LogWarning("We are expiring high demand at the moment. Please wait...");
                        await Task.Delay(10000);
                    }
                    else
                    {
                        PermissionRequest.ShowToastMessage(
                            "This might just take a little bit longer than usual. Please wait...");
                        DebugEditor.LogWarning("A little bit longer than usual. Please wait...");
                        await Task.Delay(10000);
                    }
                }

            if (retryCount == maxRetries || response == null)
            {
                PermissionRequest.ShowToastMessage(
                    "Max retries reached. Please restart the app or contact developer.");
                DebugEditor.LogError("Max retries reached. Aborting request.");
                return ("Max retries reached. Aborting request.", null);
            }


            DebugEditor.Log("XYZ:" + JsonConvert.SerializeObject(response));

            var allFunctionCalls = Array.FindAll(response.Parts, part => part.FunctionCall != null);


            _chatHistory.Add(response.Candidates[0].Content);
            AddMessage(response.Candidates[0].Content);
            foreach (var functionCall in allFunctionCalls)
            {
                DebugEditor.Log($"Function call: {functionCall.FunctionCall.Name}");
                DebugEditor.Log($"Function call parameters: {functionCall.FunctionCall.Arguments}");
            }

            var answer = string.Empty;
            if (!string.IsNullOrWhiteSpace(response.Candidates[0].Content.Parts[0].Text))
                answer = Regex.Replace(response.Candidates[0].Content.Parts[0].Text, @"\[.*?\]", string.Empty);

            return (answer, allFunctionCalls);
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