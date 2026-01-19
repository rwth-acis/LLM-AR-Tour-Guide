using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace i5.LLM_AR_Tourguide.DataSources
{
    public class WikipediaAPI : MonoBehaviour
    {
        private static readonly Dictionary<string, string> cache = new();
#if UNITY_EDITOR
        [MenuItem("Debug/WikipediaAPITest")]
#endif
        public static async void GetInformationForAllPOIs()
        {
            var poiM = FindAnyObjectByType<PointOfInterestManager>();
            var pageTitles = poiM.getAllPOITitlesAsString();

            var content = new string[pageTitles.Length];
            var contentGerman = new string[pageTitles.Length];
            var finalContent = new string[pageTitles.Length];
            for (var i = 0; i < pageTitles.Length; i++)
            {
                GetInformationForSubPOIs(i);
                content[i] = await GetWikipediaPageContent(pageTitles[i]);
                contentGerman[i] = await GetWikipediaPageContent(pageTitles[i], false);
                if (string.IsNullOrEmpty(content[i]))
                {
                    if (string.IsNullOrEmpty(contentGerman[i]))
                    {
                        DebugEditor.LogWarning("Could not retrieve Wikipedia page content for " + pageTitles[i]);
                        finalContent[i] = "";
                    }
                    else
                    {
                        finalContent[i] = contentGerman[i];
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(contentGerman[i]))
                    {
                        finalContent[i] = content[i];
                    }
                    else
                    {
                        if (content[i].Length > contentGerman[i].Length)

                            finalContent[i] = content[i];
                        else
                            finalContent[i] = contentGerman[i];
                    }
                }
            }

            poiM.setAllPOIDescriptions(finalContent);
        }

        public static async void GetInformationForSubPOIs(int index)
        {
            var poiM = FindAnyObjectByType<PointOfInterestManager>();
            var pageTitles = poiM.getAllSubPOITitlesAsString(index);

            var content = new string[pageTitles.Length];
            var contentGerman = new string[pageTitles.Length];
            var finalContent = new string[pageTitles.Length];
            for (var i = 0; i < pageTitles.Length; i++)
            {
                content[i] = await GetWikipediaPageContent(pageTitles[i]);
                contentGerman[i] = await GetWikipediaPageContent(pageTitles[i], false);
                if (string.IsNullOrEmpty(content[i]))
                {
                    if (string.IsNullOrEmpty(contentGerman[i]))
                    {
                        DebugEditor.LogWarning("Could not retrieve Wikipedia page content for " + pageTitles[i]);
                        finalContent[i] = "";
                    }
                    else
                    {
                        finalContent[i] = contentGerman[i];
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(contentGerman[i]))
                    {
                        finalContent[i] = content[i];
                    }
                    else
                    {
                        if (content[i].Length > contentGerman[i].Length)

                            finalContent[i] = content[i];
                        else
                            finalContent[i] = contentGerman[i];
                    }
                }
            }

            poiM.setAllSubPOIDescriptions(finalContent, index);
        }

        public static async Task<string> GetWikipediaPageContent(string pageTitle, bool englishWikipedia = true)
        {
            var cacheKey = $"{pageTitle}_{englishWikipedia}_WikipediaAPIContent";
            if (cache.TryGetValue(cacheKey, out var cachedContent)) return cachedContent;
            if (PlayerPrefs.HasKey(cacheKey)) return PlayerPrefs.GetString(cacheKey);

            // 1. Encode the page title for URL
            var encodedTitle = HttpUtility.UrlEncode(pageTitle);

            // 2. Construct the API URL
            var apiUrl = englishWikipedia
                ? $"https://en.wikipedia.org/w/api.php?action=query&prop=extracts&format=json&explaintext&titles={encodedTitle}"
                : $"https://de.wikipedia.org/w/api.php?action=query&prop=extracts&format=json&explaintext&titles={encodedTitle}";

            // 3. Make the API call
            using (HttpClient client = new())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "LLM-AR-Tour-Guide");
                
                try
                {
                    var response = await client.GetAsync(apiUrl);
                    response.EnsureSuccessStatusCode(); // Throw if not successful

                    var json = await response.Content.ReadAsStringAsync();

                    // 4. Parse the JSON response using Newtonsoft.Json
                    var jsonObject = JObject.Parse(json);

                    // Extract the page content. This structure is a bit tricky as Wikipedia's API
                    // nests the content within a dynamic key representing the page ID.
                    if (jsonObject["query"]?["pages"] is JObject pages)
                        foreach (var page in pages.Properties()) // Iterate through pages (usually just one)
                            if (page.Value is JObject pageData && pageData["extract"] is JValue extract)
                            {
                                var content = extract.ToString();
                                cache[cacheKey] = content;
                                PlayerPrefs.SetString(cacheKey, content);
                                PlayerPrefs.Save();
                                return content;
                            }

                    return null; // Page not found or error parsing
                }
                catch (HttpRequestException ex)
                {
                    DebugEditor.LogError($"HttpRequest Error: {ex.Message}, {ex}");
                    return null;
                }
                catch (Exception ex)
                {
                    DebugEditor.LogError($"Other Error: {ex.Message}, {ex}");
                    return null;
                }
            }
        }
    }
}