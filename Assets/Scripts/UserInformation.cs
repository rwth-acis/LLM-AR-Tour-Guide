using System;
using System.Text;
using System.Threading.Tasks;
using i5.LLM_AR_Tourguide.Gemini;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Uralstech.UGemini.Models.Content;

namespace i5.LLM_AR_Tourguide
{
    public class UserInformation : MonoBehaviour
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is created

        [SerializeField] public TMP_InputField userAgeInput;

        [SerializeField] public TMP_InputField userInterestsInput;

        public TMP_InputField userGenderInput;
        public TMP_InputField userCountryInput;
        public TMP_InputField userCityInput;
        public TMP_InputField userEducationInput;
        public TMP_InputField userOccupationInput;
        public TMP_InputField userHobbiesInput;
        public TMP_InputField userPersonalityInput;
        public TMP_InputField userPreferredLengthOfTourInput;
        public TMP_InputField userPreferredLanguageInput;
        public Toggle userIsStudent;

        [SerializeField] public int userAge;


        public string userSummery;
        public string userGender;
        public string userCountry;
        public string userCity;
        public string userEducation;
        public string userOccupation;
        public string userInterests;
        public string userHobbies;
        public string userPersonality;
        public string userPreferredLengthOfTour;
        public string userPreferredLanguage;
        public ChatManager chatManager;

        private InformationController informationController;
        private PointOfInterestManager poiM;

        private string userID;

        private void Start()
        {
            if (PlayerPrefs.HasKey("userID")) userID = PlayerPrefs.GetString("userID");
            if (string.IsNullOrWhiteSpace(userID))
            {
                userID = DateTime.Now.ToString("dd-MM-yyyy-HH-mm-ss") + Guid.NewGuid();
                PlayerPrefs.SetString("userID", userID);
                PlayerPrefs.Save();
            }

            informationController = GetComponent<InformationController>();
            chatManager = GetComponent<ChatManager>();
        }

        [ContextMenu("Test Update Summery")]
        public async void TestUpdateSummery()
        {
            await UpdateSummery("I like this place");
            await UpdateSummery("Can you show me where Aachen Cathedral is?");
            await UpdateSummery("I am tired");
            await UpdateSummery("Please tell me more jokes.");
            await UpdateSummery("What is the history of this building?");
            await UpdateSummery("Where can I find a good restaurant nearby?");
            await UpdateSummery("How old is this monument?");
            await UpdateSummery("Can you tell me more about the local culture?");
            await UpdateSummery("What are the opening hours of this museum?");
            await UpdateSummery("Is there a souvenir shop around here?");
            await UpdateSummery("What is the best time to visit this place?");
            await UpdateSummery("Are there any special events happening today?");
            await UpdateSummery("Can you recommend a good hotel nearby?");
            await UpdateSummery("What is the significance of this statue?");
            await UpdateSummery("Can you grow again?");
            await UpdateSummery("I am waiving at you");
        }

        public async Task<bool> UpdateSummery(string userComment)
        {
            await Task.Delay(5000); // Wait a bit to not have to many messages at the same time
            var onboardingChatManager = GetComponent<OnboardingChatManager>();
            if (!onboardingChatManager)
                DebugEditor.LogError("OnboardingChatManager not found");

            var pointOfInterestManager = GetComponent<PointOfInterestManager>();
            if (!pointOfInterestManager)
                DebugEditor.LogError("PointOfInterestManager not found");

            onboardingChatManager.readOutResponses = false;
            onboardingChatManager.deleteChatHistory();

            onboardingChatManager.SetRole(10);
            await onboardingChatManager.OnUpdateSummery(
                "You are an internal tool of a tour guide app. Your only task is to update the summery of the user. You are not allowed to ask questions or give any information. Just update the summery based on the user's comment. This summery should include interest, characteristics and preferences of the user. If there is nothing to add or change in the summery just answer with [No]" +
                "\n You can add types of information that have not been mentioned yet. For example, if the user asks for a restaurant, you can add that they are interested in food. If the user asks for the opening hours of a museum, you can add that they are interested in culture. If the user says the tour is boring, you can add that they want a more exiting tour." +
                " If the user says they want longer explanations you should add \"User wants longer explanations.\". When the user directly or indirectly asks for changes to the tour include these preferences with full sentences.");
            onboardingChatManager.SetRole((int)GeminiRole.User);

            var message = "The user of a tour guide app is currently seeing" +
                          pointOfInterestManager.getCurrentPOI().title + ". \n" +
                          "The current user summery is: \n" +
                          userSummery + "\n" +
                          "The user said the following to their tour guide: " +
                          userComment + ".\n" +
                          "Update the summery accordingly. Keep the summery short and summarized." +
                          " If there is nothing to add or change in the summery just answer with [No]";
            var summery = await onboardingChatManager.OnUpdateSummery(message);
            DebugEditor.Log("Summery: " + summery);
            if (summery.ToLower().Contains("no")) return false;

            userSummery = summery;
            PermissionRequest.ShowToastMessage("Your guide will remember you said that");
            UpdatePersonalization();
            return true;
        }

        public void UpdatePersonalization()
        {
            if (userAgeInput != null && !string.IsNullOrEmpty(userAgeInput.text))
            {
                var ageText = userAgeInput.text;

                if (!int.TryParse(ageText, out userAge))
                    DebugEditor.LogError("Invalid User Age format: " + ageText);
            }

            if (userGenderInput != null) userGender = userGenderInput.text.Trim();

            if (userCountryInput != null) userCountry = userCountryInput.text.Trim();

            if (userCityInput != null) userCity = userCityInput.text.Trim();

            if (userEducationInput != null) userEducation = userEducationInput.text.Trim();

            if (userOccupationInput != null && userIsStudent != null)
                if (!string.IsNullOrEmpty(userOccupationInput.text))
                {
                    if (userIsStudent.isOn)
                        userOccupation = "Student: " + userOccupationInput.text.Trim();
                    else
                        userOccupation = userOccupationInput.text.Trim();
                }

            if (userInterestsInput != null) userInterests = userInterestsInput.text.Trim();

            if (userHobbiesInput != null) userHobbies = userHobbiesInput.text.Trim();

            if (userPersonalityInput != null) userPersonality = userPersonalityInput.text.Trim();
            if (userPreferredLanguageInput != null) userPreferredLanguage = userPreferredLanguageInput.text.Trim();
            if (userPreferredLengthOfTourInput != null)
                userPreferredLengthOfTour = userPreferredLengthOfTourInput.text.Trim();

            var dataManager = GetComponent<SaveDataManager>();
            dataManager.SaveUserInformation();
        }

        public string stringifyUserInformation()
        {
            StringBuilder userInformation =
                new("");
            if (userAge > 0) userInformation.Append($" Age: {userAge}. ");
            if (!string.IsNullOrEmpty(userGender)) userInformation.Append($" Gender: {userGender}. ");
            if (!string.IsNullOrEmpty(userCity)) userInformation.Append($" City: {userCity}. ");
            else if (!string.IsNullOrEmpty(userCountry)) userInformation.Append($" Country: {userCountry}. ");
            if (!string.IsNullOrEmpty(userEducation)) userInformation.Append($" Education level: {userEducation}. ");
            if (!string.IsNullOrEmpty(userOccupation)) userInformation.Append($" Occupation: {userOccupation}. ");
            if (!string.IsNullOrEmpty(userInterests)) userInformation.Append($" Interests: {userInterests}. ");
            if (!string.IsNullOrEmpty(userHobbies)) userInformation.Append($" Hobbies: {userHobbies}. ");
            if (!string.IsNullOrEmpty(userPersonality)) userInformation.Append($" Personality: {userPersonality}. ");
            if (!string.IsNullOrEmpty(userPreferredLanguage))
                userInformation.Append($" Preferred Language: {userPreferredLanguage}. ");
            if (!string.IsNullOrEmpty(userPreferredLengthOfTour))
                userInformation.Append($" Preferred Length of Tour: {userPreferredLengthOfTour}. ");

            return userInformation.ToString();
        }
    }
}