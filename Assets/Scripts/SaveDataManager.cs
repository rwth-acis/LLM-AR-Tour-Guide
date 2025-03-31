using System;
using System.IO;
using i5.LLM_AR_Tourguide;
using i5.LLM_AR_Tourguide.Configurations;
using i5.LLM_AR_Tourguide.Evaluation;
using Newtonsoft.Json;
using UnityEngine;

public class SaveDataManager : MonoBehaviour
{
    private readonly string _informationControllerKey = "InformationControllerKey";

    private readonly string _userInformationKey = "UserInformationKey";

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        LoadUserInformation();
        LoadPointOfInterestManager();
    }

    public void SaveUserInformation()
    {
        var userInformation = GetComponent<UserInformation>();
        // Create a new DTO instance and populate it with data from the UserInformation component
        SaveDataUserInformation saveData = new();
        saveData.userAge = userInformation.userAge;
        saveData.userGender = userInformation.userGender;
        saveData.userCountry = userInformation.userCountry;
        saveData.userCity = userInformation.userCity;
        saveData.userEducation = userInformation.userEducation;
        saveData.userOccupation = userInformation.userOccupation;
        saveData.userInterests = userInformation.userInterests;
        saveData.userHobbies = userInformation.userHobbies;
        saveData.userPersonality = userInformation.userPersonality;
        saveData.userPreferredLengthOfTour = userInformation.userPreferredLengthOfTour;
        saveData.userPreferredLanguage = userInformation.userPreferredLanguage;
        saveData.userSummery = userInformation.userSummery;

        var UserInformationData = JsonUtility.ToJson(saveData);
        UploadManager.UploadData("UserInformation", UserInformationData);
        PlayerPrefs.SetString(_userInformationKey, UserInformationData);
    }

    public void SavePointOfInterestManager()
    {
        var pointOfInterestManager = GetComponent<PointOfInterestManager>();
        var data = pointOfInterestManager.poiData;
        SaveDataPointOfInterestManager saveData = new();

        saveData.pointOfInterests = new PointOfInterestData.POI[data.pointOfInterests.Length];
        for (var i = 0; i < data.pointOfInterests.Length; i++)
            saveData.pointOfInterests[i] = data.pointOfInterests[i];
        var pointOfInterestManagerData = JsonConvert.SerializeObject(saveData);
        PlayerPrefs.SetString(_informationControllerKey, pointOfInterestManagerData);

        // Delete unneseccary information
        for (var i = 0; i < saveData.pointOfInterests.Length; i++) saveData.pointOfInterests[i].information = "";
        pointOfInterestManagerData = JsonConvert.SerializeObject(saveData);
        UploadManager.UploadData("PointOfInterestManager", pointOfInterestManagerData);
    }

    public void LoadPointOfInterestManager()
    {
        if (PlayerPrefs.HasKey(_informationControllerKey))
        {
            var pointOfInterestManagerData = PlayerPrefs.GetString(_informationControllerKey);

            var saveData =
                JsonConvert.DeserializeObject<SaveDataPointOfInterestManager>(pointOfInterestManagerData);
            var pointOfInterestManager = GetComponent<PointOfInterestManager>();
            pointOfInterestManager.poiData.pointOfInterests = saveData.pointOfInterests;
        }
    }

    [ContextMenu("Debug/LoadPointOfInterestManagerFromFile")]
    public void LoadPointOfInterestManagerFromFile()
    {
        var pointOfInterestManagerData = File.ReadAllText("Assets/DebugBackUps/DebugPointOfInterestData.txt");
        var saveData =
            JsonConvert.DeserializeObject<SaveDataPointOfInterestManager>(pointOfInterestManagerData);
        var pointOfInterestManager = GetComponent<PointOfInterestManager>();
        pointOfInterestManager.poiData.pointOfInterests = saveData.pointOfInterests;
    }

    public void LoadUserInformation()
    {
        if (PlayerPrefs.HasKey(_userInformationKey))
        {
            var UserInformationData = PlayerPrefs.GetString(_userInformationKey);
            var saveData = JsonUtility.FromJson<SaveDataUserInformation>(UserInformationData);
            var userInformation = GetComponent<UserInformation>();
            userInformation.userAge = saveData.userAge;
            userInformation.userGender = saveData.userGender;
            userInformation.userCountry = saveData.userCountry;
            userInformation.userCity = saveData.userCity;
            userInformation.userEducation = saveData.userEducation;
            userInformation.userOccupation = saveData.userOccupation;
            userInformation.userInterests = saveData.userInterests;
            userInformation.userHobbies = saveData.userHobbies;
            userInformation.userPersonality = saveData.userPersonality;
            userInformation.userPreferredLengthOfTour = saveData.userPreferredLengthOfTour;
            userInformation.userPreferredLanguage = saveData.userPreferredLanguage;
            userInformation.userSummery = saveData.userSummery;
        }
    }
}


[Serializable]
public class SaveDataUserInformation
{
    public int userAge;
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
    public string userSummery;
}

[Serializable]
public class SaveDataPointOfInterestManager
{
    public PointOfInterestData.POI[] pointOfInterests;
}