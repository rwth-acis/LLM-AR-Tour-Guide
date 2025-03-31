using UnityEngine;

#pragma warning disable IDE0090 // Use 'new(...)'

namespace i5.LLM_AR_Tourguide.Gemini
{
    public class FunctionCallingChatManager : MonoBehaviour
    {
        /*
        private static readonly GeminiTool s_geminiFunctions = new()
        {
            FunctionDeclarations = new[]
            {
                new GeminiFunctionDeclaration
                {
                    Name = "pointInDirectionOfPointOfInterest",
                    Description =
                        "Makes you, the tour guide, point in the direction of one of the six points of interest stops of the tour. Use this will explaining the point of interest to the user.",
                    Parameters = new GeminiSchema
                    {
                        Type = GeminiSchemaDataType.Object,
                        Properties = new Dictionary<string, GeminiSchema>
                        {
                            {
                                "pointOfInterest", new GeminiSchema
                                {
                                    Type = GeminiSchemaDataType.String,
                                    Description = "The point of interest to point at. in aachen e.g. \"THEATER\"",
                                    Format = GeminiSchemaDataFormat.Enum,
                                    Enum = new[]
                                    {
                                        "THEATER",
                                        "CATHEDRAL",
                                        "ELISENBRUNNEN",
                                        "PONTTOR",
                                        "TOWNHALL",
                                        "SUPERC"
                                    },
                                    Nullable = false
                                }
                            }
                        },
                        Required = new[] { "pointOfInterest" }
                    }
                },
                new GeminiFunctionDeclaration
                {
                    Name = "walkInDirectionOfPointOfInterest",
                    Description =
                        "Makes you, the tour guide, walk in the direction of one of the six points of interest of the tour. Use this function to guide the user to the next point of interest.",
                    Parameters = new GeminiSchema
                    {
                        Type = GeminiSchemaDataType.Object,
                        Properties = new Dictionary<string, GeminiSchema>
                        {
                            {
                                "pointOfInterest", new GeminiSchema
                                {
                                    Type = GeminiSchemaDataType.String,
                                    Description = "The point of interest to walk to in aachen e.g. \"THEATER\"",
                                    Format = GeminiSchemaDataFormat.Enum,
                                    Enum = new[]
                                    {
                                        "THEATER",
                                        "CATHEDRAL",
                                        "ELISENBRUNNEN",
                                        "PONTTOR",
                                        "TOWNHALL",
                                        "SUPERC"
                                    },
                                    Nullable = false
                                }
                            }
                        },
                        Required = new[] { "pointOfInterest" }
                    }
                }
            }
        };

        [SerializeField] private int _maxFunctionCalls = 3;
        [SerializeField] private string _chatResponse;

        [SerializeField] private GameObject theater;

        [SerializeField] private GameObject cathedral;

        [SerializeField] private GameObject elisenbrunnen;

        [SerializeField] private GameObject ponttor;

        [SerializeField] private GameObject townhall;

        [SerializeField] private GameObject superC;

        [SerializeField] private GuideManager guideManager;

        public async Task<string> OnChat(string message)
        {
            var text = message;
            if (string.IsNullOrWhiteSpace(text))
            {
                DebugEditor.LogError("Chat text should not be null or whitespace!");
                return "";
            }

            List<GeminiContent> contents = new()
            {
                GeminiContent.GetContent(text, GeminiRole.User)
            };

            GeminiChatResponse response;
            GeminiFunctionCall functionCall;
            var responseIterations = 0;

            do
            {
                response = await GeminiManager.Instance.Request<GeminiChatResponse>(
                    new GeminiChatRequest(GeminiModel.Gemini1_5Flash, true)
                    {
                        Contents = contents.ToArray(),
                        Tools = new[] { s_geminiFunctions }
                    });

                contents.Add(response.Candidates[0].Content);

                _chatResponse = Array.Find(response.Parts, part => !string.IsNullOrEmpty(part.Text))?.Text;
                var allFunctionCalls = Array.FindAll(response.Parts, part => part.FunctionCall != null);

                functionCall = null;
                for (var i = 0; i < allFunctionCalls.Length; i++)
                {
                    functionCall = allFunctionCalls[i].FunctionCall;
                    JObject functionResponse = null;

                    switch (functionCall.Name)
                    {
                        case "pointInDirectionOfPointOfInterest":
                            if (!TryPointing(functionCall.Arguments["pointOfInterest"].ToObject<string>()))
                                functionResponse = new JObject
                                {
                                    ["result"] = "Unknown pointOfInterest."
                                };

                            break;
                        case "walkInDirectionOfPointOfInterest":
                            if (!TryPointing(functionCall.Arguments["pointOfInterest"].ToObject<string>()))
                                functionResponse = new JObject
                                {
                                    ["result"] = "Walking in the direction of the point of interest."
                                };

                            break;
                        default:
                            functionResponse = new JObject
                            {
                                ["result"] = "Sorry, but that function does not exist."
                            };

                            break;
                    }

                    contents.Add(GeminiContent.GetContent(functionCall.GetResponse(functionResponse ?? new JObject
                    {
                        ["result"] = "Completed executing function successfully."
                    })));
                }

                responseIterations++;
            } while (functionCall != null && responseIterations <= _maxFunctionCalls);

            return _chatResponse;
        }
/*
        private bool TryPointing(string pointOfInterest)
        {
            if (guideManager == null)
            {
                DebugEditor.LogError("GuideManager is not set in FunctionCallingChatManager.");
                return false;
            }

            switch (pointOfInterest)
            {
                case "THEATER":
                    guideManager.DoPointingTask(theater);
                    break;

                case "CATHEDRAL":
                    guideManager.DoPointingTask(cathedral);
                    break;

                case "ELISENBRUNNEN":
                    guideManager.DoPointingTask(elisenbrunnen);
                    break;

                case "PONTTOR":
                    guideManager.DoPointingTask(ponttor);
                    break;

                case "TOWNHALL":
                    guideManager.DoPointingTask(townhall);
                    break;

                case "SUPERC":
                    guideManager.DoPointingTask(superC);
                    break;

                default:
                    return false;
            }

            DebugEditor.Log("Pointing!");
            return true;
        }

        private bool TryWalking(string pointOfInterest)
        {
            if (guideManager == null)
            {
                DebugEditor.LogError("GuideManager is not set in FunctionCallingChatManager.");
                return false;
            }

            switch (pointOfInterest)
            {
                case "THEATER":
                    guideManager.DoPointingTask(theater);
                    break;

                case "CATHEDRAL":
                    guideManager.DoPointingTask(cathedral);
                    break;

                case "ELISENBRUNNEN":
                    guideManager.DoPointingTask(elisenbrunnen);
                    break;

                case "PONTTOR":
                    guideManager.DoPointingTask(ponttor);
                    break;

                case "TOWNHALL":
                    guideManager.DoPointingTask(townhall);
                    break;

                case "SUPERC":
                    guideManager.DoPointingTask(superC);
                    break;

                default:
                    return false;
            }

            DebugEditor.Log("Walking!");
            return true;
        }
        */
    }
}

#pragma warning restore IDE0090 // Use 'new(...)'