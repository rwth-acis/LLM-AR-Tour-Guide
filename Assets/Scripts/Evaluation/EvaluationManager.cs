using System;
using System.Collections.Generic;
using i5.LLM_AR_Tourguide.Prefab_Scripts;
using Newtonsoft.Json;
using UnityEngine;

namespace i5.LLM_AR_Tourguide.Evaluation
{
    public class EvaluationManager : MonoBehaviour
    {
        private int questionareIndex;
        private Questionnaire[] questionares;

        public void Start()
        {
            //var uiSwitcher = GetComponent<UISwitcher>();
            //questionares = uiSwitcher.EvaluationPage.gameObject.GetComponentsInChildren<Questionnaire>();
            //DebugEditor.Log("Found " + questionares.Length + " questionares");
            //if (questionares.Length > 0) questionares[0].secondaryButton.gameObject.SetActive(false);
            //foreach (var que in questionares)
            //{
            //    // Find button in children and add listener
            //    que.gameObject.SetActive(false);
            //    que.primaryButton.onClick.AddListener(NextQuestionare);
            //    que.secondaryButton.onClick.AddListener(PreiviousQuestionare);
            //}
        }

        public void OnLinkClick()
        {
            var link = GenerateLink();
            DebugEditor.Log("Link: " + link);
            Application.OpenURL(link);
        }

        private string GenerateLink()
        {
            var userInformation = GetComponent<UserInformation>();
            var result = "";
            result += "https://limesurvey.tech4comp.dbis.rwth-aachen.de/index.php/123152?newtest=Y&lang=en";
            result += "&EntryCode=" + PlayerPrefs.GetString("userID");
            if (!string.IsNullOrWhiteSpace(userInformation.userGender))
                result += "&Gender=" + userInformation.userGender;
            if (userInformation.userAge > 0)
                result += "&Age=" + userInformation.userAge;
            if (!string.IsNullOrWhiteSpace(userInformation.userOccupation))
                result += "&Occupation=" + userInformation.userOccupation;
            return result;
        }

        public void startEvaluation()
        {
            //questionares[0].gameObject.SetActive(true);
        }

        public void PreiviousQuestionare()
        {
            questionares[questionareIndex].gameObject.SetActive(false);
            questionareIndex--;
            if (questionareIndex < 0) questionareIndex = 0;
            questionares[questionareIndex].gameObject.SetActive(true);
        }

        public void NextQuestionare()
        {
            questionares[questionareIndex].gameObject.SetActive(false);
            questionareIndex++;
            if (questionareIndex >= questionares.Length)
            {
                finishEvaluation();
                return;
            }

            questionares[questionareIndex].gameObject.SetActive(true);
        }

        public void finishEvaluation()
        {
            // Save results
            collectAllResults();
            // Go to next scene
        }

        public void collectAllResults()
        {
            var results = new Dictionary<string, int>();
            foreach (var que in questionares)
            {
                var partialResults = que.collectAllResults();
                var i = 0;
                foreach (var entry in partialResults)
                    try
                    {
                        try
                        {
                            results.Add(entry.Key, entry.Value);
                        }
                        catch (ArgumentException e)
                        {
                            // In case the key already exists, add a number to the key, this should only happen when the same question is asked multiple times
                            DebugEditor.LogWarning("Key already exists: " + entry.Key + e.Message);
                            results.Add(entry.Key + i, entry.Value);
                            i++;
                        }
                    }
                    catch (Exception e)
                    {
                        DebugEditor.LogWarning("Error while adding results: " + e.Message);
                    }
            }

            UploadManager.UploadData("EvaluationResults", JsonConvert.SerializeObject(results));
        }
    }
}