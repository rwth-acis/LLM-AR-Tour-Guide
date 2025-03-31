using System;
using i5.LLM_AR_Tourguide.Audio;
using i5.LLM_AR_Tourguide.Guide_Scripts;
using Newtonsoft.Json;
using Uralstech.UGemini.Models.Content;
using Object = UnityEngine.Object;

namespace i5.LLM_AR_Tourguide.TourGeneration
{
    [Serializable]
    public class TourContent
    {
        private GuideManager _guideManager;
        private PointOfInterestManager _pointOfInterestManager;
        private TextToSpeech _textToSpeech;
        [JsonProperty] public GeminiContentPart[] EndFunctionCalls;
        [JsonProperty] public GeminiContentPart[] StartFunctionCalls;


        public TourContent()
        {
        }


        public TourContent(string text)
        {
            Text = text;
        }

        public TourContent(
            (string text, GeminiContentPart[] startFunctionCalls, GeminiContentPart[] endFunctionCalls) content)
        {
            Text = content.text;
            StartFunctionCalls = content.startFunctionCalls;
            EndFunctionCalls = content.endFunctionCalls;
        }

        public TourContent(
            (string text, GeminiContentPart[] functionCalls) content)
        {
            Text = content.text;
            StartFunctionCalls = content.functionCalls;
        }

        [JsonProperty] public string Text { get; private set; }

        public string GetText()
        {
            return Text;
        }

        public string GetFunctionAndParameters()
        {
            var functionAndParameters = "";
            if(StartFunctionCalls != null)
                foreach (var f in StartFunctionCalls)
                {
                    var functionCall = f.FunctionCall;
                    functionAndParameters += functionCall.Name + " with parameters:  {";
                    foreach (var arg in functionCall.Arguments) functionAndParameters += arg.Key + ":" + arg.Value + " ";
                    functionAndParameters += "}";
                }
            if(EndFunctionCalls != null)
                foreach (var f in EndFunctionCalls)
                {
                    functionAndParameters += " and ";
                    var functionCall = f.FunctionCall;
                    functionAndParameters += functionCall.Name + " with parameters:  {";
                    foreach (var arg in functionCall.Arguments) functionAndParameters += arg.Key + ":" + arg.Value + " ";
                    functionAndParameters += "}";
                }

            return functionAndParameters;
        }

        public void CacheTextToSpeech()
        {
            if (!_textToSpeech) _textToSpeech = Object.FindAnyObjectByType<TextToSpeech>();
            _ = _textToSpeech.SaveToCache(Text);
        }


        private void ExecuteFunctionCalls(GeminiContentPart[] functionCalls)
        {
            if (functionCalls == null || functionCalls.Length == 0)
            {
                DebugEditor.LogWarning("No function calls found");
                return;
            }

            foreach (var f in functionCalls)
            {
                var functionCall = f.FunctionCall;
                DebugEditor.Log("Function call: " + functionCall.Name);
                switch (functionCall.Name)
                {
                    case "pointInDirectionOfPointOfInterest":
                        TryPointing(functionCall.Arguments["pointOfInterest"]?.ToObject<string>());
                        break;
                    case "walkInDirectionOfPointOfInterest":
                        TryWalking(functionCall.Arguments["pointOfInterest"]?.ToObject<string>());
                        break;
                    case "waiveAtParticipant":
                        TryWaiving();
                        break;
                    case "gestureWithHands":
                        TryTalking();
                        break;
                    case "danceSilly":
                        TryDancing();
                        break;
                    case "growBigger":
                        TryGrowing();
                        break;
                    case "shrinkSmaller":
                        TryShrinking();
                        break;
                    case "shakeHead":
                        TryShakingHead();
                        break;
                    default:
                        DebugEditor.LogError("Function call not found!");

                        break;
                }
            }
        }

        public void ScheduleAssociatedAgentTasks(bool onlyStart = false, bool onlyEnd = false)
        {
            if (_guideManager == null) _guideManager = Object.FindAnyObjectByType<GuideManager>();
            if (onlyStart)
            {
                ExecuteFunctionCalls(StartFunctionCalls);
            }
            else if (onlyEnd)
            {
                ExecuteFunctionCalls(EndFunctionCalls);
            }
            else
            {
                ExecuteFunctionCalls(StartFunctionCalls);
                ExecuteFunctionCalls(EndFunctionCalls);
            }
        }

        private bool TryPointing(string pointOfInterestTitle)
        {
            if (!_guideManager) _guideManager = Object.FindAnyObjectByType<GuideManager>();
            if (!_pointOfInterestManager)
                _pointOfInterestManager = Object.FindAnyObjectByType<PointOfInterestManager>();

            var poi = _pointOfInterestManager.GetPointOfInterestTransformByTitle(pointOfInterestTitle);

            // Validate by checking if the point of interest has a title
            if (string.IsNullOrEmpty(pointOfInterestTitle))
            {
                DebugEditor.LogError("Point of interest not found!");
                return false;
            }

            _guideManager.DoPointingTask(poi.gameObject);
            return true;
        }

        private bool TryWalking(string pointOfInterestTitle)
        {
            if (!_guideManager) _guideManager = Object.FindAnyObjectByType<GuideManager>();
            if (!_pointOfInterestManager)
                _pointOfInterestManager = Object.FindAnyObjectByType<PointOfInterestManager>();

            var poi = _pointOfInterestManager.GetPointOfInterestTransformByTitle(pointOfInterestTitle);

            // Validate by checking if the point of interest has a title
            if (string.IsNullOrEmpty(pointOfInterestTitle))
            {
                DebugEditor.LogError("Point of interest not found!");
                return false;
            }

            _guideManager.DoWalkingTask(poi.gameObject.transform);
            return true;
        }

        private bool TryWaiving()
        {
            if (!_guideManager) _guideManager = Object.FindAnyObjectByType<GuideManager>();

            _guideManager.DoWaiveAnimation();

            return true;
        }

        private bool TryTalking()
        {
            if (!_guideManager) _guideManager = Object.FindAnyObjectByType<GuideManager>();

            _guideManager.DoTalkAnimation();

            return true;
        }

        private bool TryDancing()
        {
            if (!_guideManager) _guideManager = Object.FindAnyObjectByType<GuideManager>();

            _guideManager.DoDancingAnimation();

            return true;
        }

        private bool TryGrowing()
        {
            if (!_guideManager) _guideManager = Object.FindAnyObjectByType<GuideManager>();

            DebugEditor.Log("Growing now");
            _guideManager.DoGrowingAnimation();

            return true;
        }

        private bool TryShrinking()
        {
            if (!_guideManager) _guideManager = Object.FindAnyObjectByType<GuideManager>();

            DebugEditor.Log("Shrinking now");
            _guideManager.DoGrowingAnimation(default, true);

            return true;
        }

        private bool TryShakingHead()
        {
            if (!_guideManager) _guideManager = Object.FindAnyObjectByType<GuideManager>();

            DebugEditor.Log("Shaking head now");
            _guideManager.DoShakingHeadAnimation();

            return true;
        }
    }
}