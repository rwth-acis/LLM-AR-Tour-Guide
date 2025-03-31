using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using i5.LLM_AR_Tourguide.Gemini;
using i5.LLM_AR_Tourguide.Guide_Scripts;
using i5.LLM_AR_Tourguide.UI_Scripts;
using UnityEngine;
using Uralstech.UGemini.Models.Content;
using Random = UnityEngine.Random;

namespace i5.LLM_AR_Tourguide.TourGeneration
{
    public class TourGenerator : MonoBehaviour
    {
        public GuideManager guideManager;
        private readonly int maxNumberOfParagraphs = 4;

        private InformationController informationController;
        private PointOfInterestManager pointOfInterestManager;

        private ProgressBar progressionSlider;
        private TourGenerationChatManager tourGenerationChatManager;
        private UserInformation userInformation;

        public void Start()
        {
            tourGenerationChatManager = GetComponent<TourGenerationChatManager>();
            if (tourGenerationChatManager == null)
                DebugEditor.LogError("TourGenerationChatManager is not set in the TourGenerator");
            userInformation = GetComponent<UserInformation>();
            if (userInformation == null) DebugEditor.LogError("UserInformation is not set in the TourGenerator");
            if (guideManager == null) guideManager = GetComponent<GuideManager>();
            if (guideManager == null) DebugEditor.LogError("GuideManager is not set in the TourGenerator");
            informationController = GetComponent<InformationController>();
            if (informationController == null)
                DebugEditor.LogError("InformationController is not set in the TourGenerator");
            pointOfInterestManager = GetComponent<PointOfInterestManager>();
            if (pointOfInterestManager == null)
                DebugEditor.LogError("PointOfInterestManager is not set in the TourGenerator");
        }
#if UNITY_EDITOR
        [ContextMenu("DebugMethod")]
#endif
        public async void DebugTest()
        {
            var informationController = GetComponent<InformationController>();
            informationController.ChangeCurrentInformationIndex(4);
            var debug = await generateTourForPointOfInteresting();

            foreach (var content in debug)
                DebugEditor.Log(content.GetText());
        }

        public void SetProgressbar(ProgressBar progressBar)
        {
            progressionSlider = progressBar;
        }

        /*
        public async Task<TourContent[]> generateNextParagrahForPointOfInteresting()
        {
            if (generatedParagraphs >= maxNumberOfParagraphs) return null;
            if (generatedParagraphs == 0)
            {
                var introduction = await GenerateTourIntroduction();
                generatedParagraphs++;
                return new[] { new TourContent(introduction) };
            }

            if (generatedParagraphs == maxNumberOfParagraphs - 1)
            {
                var closing = await GenerateClosingParagraph();
                generatedParagraphs++;
                return new[] { new TourContent(closing) };
            }

            var middle = await GenerateMiddleParagraph(generatedParagraphs);
            generatedParagraphs++;
            return new[] { new TourContent(middle) };
        }
*/
        public async Task<bool> preperationForPointOfInterest()
        {
            tourGenerationChatManager.Reset();
            tourGenerationChatManager.SetRoleToSystemPrompt();
            var systemPrompt = guideManager.GetChosenGuideInfo().longDescription;
            systemPrompt += "The user is currently at the point of interest " +
                            pointOfInterestManager.getCurrentPOI().title +
                            ".";
            systemPrompt += "The user already visited the tour points " + pointOfInterestManager.getVisitedPOIs() +
                            ". ";
            systemPrompt += "The user will visit the following points of interest afterwards " +
                            pointOfInterestManager.getUpcomingPOIs() +
                            ". ";
            systemPrompt += userInformation.userSummery;
            systemPrompt +=
                "Your goal for this chat is to provide the user with detailed information about the point of interest in multiple engaging paragraphs that relate to the user. Ensure that your response includes actionable insights and is concise, clear, and directly related to the point of interest.";
            await tourGenerationChatManager.OnChat(systemPrompt, false);
            tourGenerationChatManager.SetRole(GeminiRole.User);

            var outlineMessage =
                "First write a brief outline about the information and topics you want to cover in the " +
                maxNumberOfParagraphs + "paragraphs when talking about " +
                pointOfInterestManager.getCurrentPOI().title +
                ". Take the user information and preferences into account. This message will not be shown to the user. " +
                "You can use the following information as a source. Try to extend beyond the provided information: " +
                pointOfInterestManager.getCurrentPOI().information;
            DebugEditor.Log("Request data for outline");
            var outline = await tourGenerationChatManager.OnChat(outlineMessage, false);
            DebugEditor.Log("Outline: " + outline);
            return true;
        }

        private void calculatePercentageAndShow(int i)
        {
            var percentage = 0;
            // Random number between 1 and 4 to make the percentage look more realistic
            var randomNumber = Random.Range(1, 5);
            if (i == maxNumberOfParagraphs + 1)
                percentage = 100;
            else
                percentage = randomNumber + (int)(100f / (maxNumberOfParagraphs + 1)) * i;
            DebugEditor.Log("Percentage: " + percentage);
            progressionSlider.SetProgression(percentage / 100f, true);
            //PermissionRequest.ShowAndroidToastMessage(percentage + "%");
        }

        public async Task<TourContent[]> generateTourForPointOfInteresting()
        {
            try
            {
                if (pointOfInterestManager.getCurrentPOI().tourContent != null &&
                    pointOfInterestManager.getCurrentPOI().tourContent.Length != 0)
                {
                    DebugEditor.Log("Tour content already exists");
                    return pointOfInterestManager.getCurrentPOI().tourContent;
                }

                DebugEditor.Log("Generating tour content");
                calculatePercentageAndShow(0);
                await preperationForPointOfInterest();
                UISwitcher.ShowHint(true);
                calculatePercentageAndShow(1);
                var tourContent = new TourContent[maxNumberOfParagraphs];
                tourContent[0] = new TourContent(await GenerateTourIntroduction());
                tourContent[0].CacheTextToSpeech();
                calculatePercentageAndShow(2);

                DebugEditor.Log("Request data forIntro: " + tourContent[0]);
                for (var i = 1; i < maxNumberOfParagraphs - 1; i++)
                {
                    tourContent[i] = new TourContent(await GenerateMiddleParagraph(i));
                    DebugEditor.Log("Request data for Middle No" + i + ": " + tourContent[i]);
                    tourContent[i].CacheTextToSpeech();
                    calculatePercentageAndShow(i + 2);
                }

                UISwitcher.ShowHint(true);
                tourContent[maxNumberOfParagraphs - 1] = new TourContent(await GenerateClosingParagraph());
                tourContent[maxNumberOfParagraphs - 1].CacheTextToSpeech();
                DebugEditor.Log("Request data for Closing: " + tourContent[maxNumberOfParagraphs - 1]);
                calculatePercentageAndShow(5);
                return tourContent;
            }
            catch (Exception e)
            {
                DebugEditor.LogError("Error in generating tour content: " + e);
                DebugEditor.LogError("Stacktrace: " + e.StackTrace);
                PermissionRequest.ShowToastMessage(
                    "Error in generating tour content. Please make sure you are connected to the internet.");
                return null;
            }
        }

        public async Task<(string, GeminiContentPart[], GeminiContentPart[])> GenerateTourIntroduction()
        {
            var message =
                "Choose one or more animations from the possible methods that you will act out when you read out the introduction paragraph.";
            var response2 = await tourGenerationChatManager.OnChat(message, true);

            message =
                "Choose one or more animations from the possible methods that you will act out when you finish reading the introduction.";
            var response3 = await tourGenerationChatManager.OnChat(message, true);

            message =
                "Write the first introduction part of your outline now. Your message will be shown to and read out by you to the user directly, while you act out the animations you selected. Write any other actions or non-verbal ques is square brackets for example [smile].";
            var response = await tourGenerationChatManager.OnChat(message, false);
            response.Item1 = Regex.Replace(response.Item1, @"\[.*?\]", string.Empty);

            return (response.Item1, response2.Item2, response3.Item2);
        }

        public async Task<(string, GeminiContentPart[], GeminiContentPart[])> GenerateMiddleParagraph(
            int paragraphNumber)
        {
            var message =
                "Choose one or more animations from the possible methods that you will act out when you start reading out the next paragraph.";
            var response2 = await tourGenerationChatManager.OnChat(message, true);

            message =
                "Write the next part of your outline now. Your message will be shown to and read out by you to the user directly. Write any other actions or non-verbal ques is square brackets for example [smile].";
            var response = await tourGenerationChatManager.OnChat(message, false);
            response.Item1 = Regex.Replace(response.Item1, @"\[.*?\]", string.Empty);
            message =
                "Choose one or more animations from the possible methods that you will act out when you finish reading out the paragraph from the last message.";
            var response3 = await tourGenerationChatManager.OnChat(message, true);

            return (response.Item1, response2.Item2, response3.Item2);
        }

        public async Task<(string, GeminiContentPart[], GeminiContentPart[])> GenerateClosingParagraph()
        {
            var nextPOI = pointOfInterestManager.getNextPOI();
            var nextPOIMessage = ".";
            if (nextPOI.HasValue)
                nextPOIMessage = "Relate it the next point of interest, which is " +
                                 pointOfInterestManager.getNextPOI() + " .";

            var message =
                "Choose one or more animations from the possible methods that you will act out when you start reading out the closing paragraph in the next message.";
            var response2 = await tourGenerationChatManager.OnChat(message, true);

            message =
                "Write the closing of your outline now. " + nextPOIMessage +
                " Your message will be shown to and read out by you to the user directly. Write any other actions or non-verbal ques is square brackets for example [smile]. Briefly remind the user that they can ask questions, request more information or ask for changes in how the information is presented with the ask question button. Also encourage them to look around with their camera now and tap on one of the " +
                pointOfInterestManager.getSubPOICount() +
                " red books around them to get to know more about those locations that are not part of the tour. They can also ask you to point at them.";
            var response = await tourGenerationChatManager.OnChat(message, false);
            response.Item1 = Regex.Replace(response.Item1, @"\[.*?\]", string.Empty);
            message =
                "Choose one or more animations from the possible methods that you will act out when you finish reading out the closing paragraph from the last message.";
            var response3 = await tourGenerationChatManager.OnChat(message, true);

            return (response.Item1, response2.Item2, response3.Item2);
        }
    }
}