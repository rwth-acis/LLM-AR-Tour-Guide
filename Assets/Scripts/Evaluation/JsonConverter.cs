#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using Uralstech.UGemini;
using Uralstech.UGemini.Models.Content;
using Uralstech.UGemini.Models.Generation;
using Uralstech.UGemini.Models.Generation.Chat;
using Debug = UnityEngine.Debug;

namespace i5.LLM_AR_Tourguide.Evaluation
{
    public static class JsonConverter
    {
        public static List<GeminiContent> _chatHistory = new();

        private static GeminiRole _senderRole = GeminiRole.User;
        private static bool _settingSystemPrompt;
        private static GeminiContent _systemPrompt;


        private static readonly string _folderName = "ConvertedFiles6Gemini2_5WithEmbedding~";

        private static bool stopGeneration;
        private static CancellationTokenSource cancellationTokenSource;

        public static void SetRoleToSystemPrompt()
        {
            _settingSystemPrompt = true;
        }

        [RuntimeInitializeOnLoadMethod]
        public static void AfterInitializing()
        {
            stopGeneration = false;
        }

        public static void SetRole(GeminiRole role)
        {
            _senderRole = role;
            _settingSystemPrompt = false;
        }

        [MenuItem("Debug/ExcelPreparation/StopGenerating")]
        public static void stopGenerating()
        {
            // Stop the generation
            stopGeneration = true;
            // Cancel delay task
            cancellationTokenSource?.Cancel();
        }

        public static async Task<string> OnChat(string text)
        {
            GeminiContent addedContent;
            if (_settingSystemPrompt)
            {
                _systemPrompt = GeminiContent.GetContent(text);
                _settingSystemPrompt = false;
            }
            else
            {
                addedContent = GeminiContent.GetContent(text, GeminiRole.User);
                _chatHistory.Add(addedContent);
            }

            if (_chatHistory.Count == 0)
                return "Something went wrong!";

            DebugEditor.Log("Requesting sending...");
            var gotResponse = false;
            var s_geminiGenerationConfiguration =
                new GeminiGenerationConfiguration
                {
                    Temperature = 1.5f
                };
            var tries = 0;
            while (!gotResponse)
            {
                if (stopGeneration)
                {
                    Debug.Log("Stop generation");
                    return "Stopped";
                }

                try
                {
                    //gemini-2.0-flash-thinking-exp-01-21
                    //gemini-2.5-pro-exp-03-25
                    tries++;
                    DebugEditor.Log("Requesting sending...");
                    var response = await GeminiManager.Instance.Request<GeminiChatResponse>(
                        new GeminiChatRequest("gemini-2.5-pro-exp-03-25", true)
                        {
                            Contents = _chatHistory.ToArray(),
                            SystemInstruction = _systemPrompt,
                            GenerationConfig = s_geminiGenerationConfiguration
                        });
                    gotResponse = true;
                    DebugEditor.Log("Received response.");
                    var responseText = string.Join(", ",
                        Array.ConvertAll(response.Parts,
                            part =>
                                $"{part.Text}"));

                    var citations = "";
                    if (response.Candidates != null)
                        foreach (var candidate in response.Candidates)
                            if (candidate.CitationMetadata != null)
                                foreach (var citation in candidate.CitationMetadata.CitationSources)
                                    citations += "Source found: " + citation.StartIndex + " " + citation.EndIndex +
                                                 " " + citation.Uri + " " + citation.License;

                    addedContent = GeminiContent.GetContent(responseText, GeminiRole.Assistant);

                    _chatHistory.Add(addedContent);

                    return responseText + citations;
                }
                catch (Exception e)
                {
                    DebugEditor.LogError($"Error: {e.Message}");
                    // Wait for at least 10 seconds
                    Debug.Log("Waiting for " + Math.Min(10000 * tries, 60000 * 10) / 1000 + "s");
                    try
                    {
                        cancellationTokenSource = new CancellationTokenSource();
                        await Task.Delay(Math.Min(10000 * tries, 60000 * 10), cancellationTokenSource.Token);
                        DebugEditor.Log("Waiting is done.");
                    }
                    catch (Exception e2)
                    {
                        DebugEditor.LogError($"Error: {e2.Message}");
                        stopGeneration = true;
                        await Task.Delay(5000);
                    }

                    if (stopGeneration)
                    {
                        Debug.Log("Stop generation");
                        return "Stopped";
                    }
                }
            }

            DebugEditor.Log("Stop generation because of error");
            return "[Incorrect][Error]";
        }

        [MenuItem("Debug/ExcelPreparation/ConvertAllFilesToExcelCompatibleJson")]
        public static async Task ConvertAllFilesToExcelCompatibleJson()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            stopGeneration = false;

            var files = new List<string>();

            var baseDirectory = "Assets/DebugBackUps/FilesToGoTrough";

            // Iterate through all folders in the base directory
            foreach (var folderPath in Directory.GetDirectories(baseDirectory))
            {
                var pointOfInterestManagerPath = Path.Combine(folderPath, "PointOfInterestManager");

                if (Directory.Exists(pointOfInterestManagerPath))
                {
                    var filesInFolder = Directory.GetFiles(pointOfInterestManagerPath);

                    if (filesInFolder.Length > 0)
                    {
                        // Find the largest file (by length) in the PointOfInterestManager subfolder.
                        var largestFile = filesInFolder
                            .Select(f => new FileInfo(f)) // Convert the file paths to FileInfo objects
                            .OrderByDescending(fi => fi.Length) // Order by size (largest first)
                            .FirstOrDefault(); // Take the first (largest) file, or null if the folder was empty

                        if (largestFile != null) // Check if a largest file was actually found
                            files.Add(largestFile.FullName); // Add the full path of the largest file
                    }
                }
            }

            var skipFirstFiles = 15;
            foreach (var filepath in files)
            {
                if (!filepath.EndsWith(".txt") || stopGeneration || skipFirstFiles > 0)
                {
                    if (skipFirstFiles > 0) skipFirstFiles--;
                    Debug.Log("Skipping file: " + filepath);
                    continue;
                }

                // remove everything after PointOfInterestManager 
                var output = Regex.Replace(filepath, @"\\PointOfInterestManager.*", "");
                output += "_converted.json";
                output = output.Replace("FilesToGoTrough", _folderName);
                if (!Directory.Exists("Assets/DebugBackUps/" + _folderName))
                    Directory.CreateDirectory("Assets/DebugBackUps/" + _folderName);

                //Debug.Log("Inout: " + filepath);
                //Debug.Log("Output: " + output);


                await LoadPointOfInterestManagerFromFileAsOneMessage(filepath, output);

                // Debug current index of total
                Debug.Log(
                    $"Processed {files.IndexOf(filepath) + 1} of {files.Count} files at time {stopwatch.ElapsedMilliseconds}. Estimated time left: " +
                    TimeSpan.FromMilliseconds(
                            stopwatch.ElapsedMilliseconds / (files.IndexOf(filepath) + 1) *
                            (files.Count - files.IndexOf(filepath)))
                        .TotalMinutes.ToString("F2") + " minutes");
            }

            putJsonPropertyOrderInString();

            // Wait for all tasks to finish
            stopwatch.Stop();
            Debug.Log($"Total operation time: {stopwatch.ElapsedMilliseconds} ms");
        }


        [MenuItem("Debug/ExcelPreparation/AddSortHelper")]
        public static void putJsonPropertyOrderInString()
        {
            var excelJsonConverter = new ExcelJsonConverter();
            var fields = excelJsonConverter.GetType()
                .GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
                if (field.GetCustomAttributes(typeof(JsonPropertyAttribute), false).FirstOrDefault() is
                    JsonPropertyAttribute jsonProperty)
                    // Access the property and write the order to it
                    if (excelJsonConverter.GetType().GetField(field.Name)
                            ?.FieldType == typeof(int))
                        excelJsonConverter.GetType().GetField(field.Name)
                            ?.SetValue(excelJsonConverter, jsonProperty.Order);
                    else
                        excelJsonConverter.GetType().GetField(field.Name)
                            ?.SetValue(excelJsonConverter, jsonProperty.Order.ToString());


            var json = JsonConvert.SerializeObject(excelJsonConverter, Formatting.Indented);
            // Check if the folder exists
            if (!Directory.Exists("Assets/DebugBackUps/" + _folderName))
                Directory.CreateDirectory("Assets/DebugBackUps/" + _folderName);
            File.WriteAllText("Assets/DebugBackUps/" + _folderName + "/AAASortHelper.json", json);
        }


        public static async Task LoadPointOfInterestManagerFromFile(string inputPath, string outPutPath)
        {
            SetRoleToSystemPrompt();
            _chatHistory = new List<GeminiContent>();
            var systemPrompt = "SYSTEM PROMPT:\n\n";
            systemPrompt +=
                "You are an expert in Aachen architecture and history. You will receive text describing tours of Aachen, specifically focusing on the Super C, Aachen Townhall, Aachen Cathedral, and Aachen Elisenbrunnen. Your task is to identify errors, misleading information, and filler content.\n\n";
            systemPrompt +=
                "For each sentence you receive, respond by first repeating the sentence verbatim, then adding  [Explanation][X] where:\n\n";
            systemPrompt += "* X is one of the following classifications:\n";
            systemPrompt +=
                "    * Correct: The sentence is factually accurate and relevant to the context of a tour. [Explanation: A very short confirmation of accuracy, max 5 words]\n";
            systemPrompt +=
                "    * Fluff: The sentence is unnecessary, provides no significant information or is a forced analogie, and is essentially filler. [Explanation: A very short confirmation of accuracy, max 5 words]\n";
            systemPrompt +=
                "    * Misleading: The sentence is factually correct but presents information in a way that could be misinterpreted or creates a false impression in the context of the tour or explanation.\n";
            systemPrompt +=
                "    * Incorrect: The sentence contains a factual error. [Explanation: The actual correct information.]\n";
            systemPrompt +=
                "    * Missing Context: The sentence is not wrong but requires additional information to be understandable for the purpose of a tour.\n\n";
            systemPrompt += "Add a new line for each new sentence.\n\n";
            systemPrompt +=
                "* Explanation: A brief explanation of why you chose that classification. Be specific about the error or misleading aspect, unless the classification is \"Correct.\"\n\n";
            systemPrompt += "---\n\n";
            systemPrompt += "EXAMPLES:\n\n";
            systemPrompt += "---\n";
            systemPrompt += "INPUT: Charlemagne laid the first stone of the Aachen Cathedral.\n";
            systemPrompt +=
                "OUTPUT: Charlemagne laid the first stone of the Aachen Cathedral. [While Charlemagne commissioned the Palatine Chapel, which is the core of the Cathedral, he didn't lay the first stone. It was built over time by multiple builders.][Incorrect]\n";
            systemPrompt += "---\n";
            systemPrompt +=
                "INPUT: The Elisenbrunnen had the architects Johann Peter Cremer and Karl Friedrich Schinkel.\n";
            systemPrompt +=
                "OUTPUT: The Elisenbrunnen had the architects Johann Peter Cremer and Karl Friedrich Schinkel. [Right architects.][Correct]\n";
            systemPrompt += "---\n";
            systemPrompt += "INPUT: The Super C is a visually striking building with a lot of glass.\n";
            systemPrompt +=
                "OUTPUT: The Super C is a visually striking building with a lot of glass. [Known for its glass.][Correct]\n";
            systemPrompt += "---\n";
            systemPrompt += "INPUT: The Elisenbrunnen dispenses water that cures all diseases.\n";
            systemPrompt +=
                "OUTPUT: The Elisenbrunnen dispenses water that cures all diseases. [The water has a high sulfur content and was once considered medicinal, but it doesn't cure *all* diseases. This statement exaggerates its benefits.][Misleading]\n";
            systemPrompt += "---\n";
            systemPrompt += "INPUT: The Rathaus is pretty.\n";
            systemPrompt +=
                "OUTPUT: The Rathaus is pretty. [While subjective, it offers no historical or architectural insight.][Fluff]\n";
            systemPrompt += "---\n";
            systemPrompt += "INPUT: The Town Hall was built in the 14th century.\n";
            systemPrompt += "OUTPUT: The Town Hall was built in the 14th century. [Built in 14th century.][Correct]\n";
            systemPrompt += "---\n";
            systemPrompt += "INPUT: Inside the Aachen Cathedral, you can find many interesting things.\n";
            systemPrompt +=
                "OUTPUT: Inside the Aachen Cathedral, you can find many interesting things. [This statement is vague and doesn't specify what is interesting or important for a tour.][Missing Context]\n";
            systemPrompt += "---\n";
            systemPrompt += "INPUT: Aachen is a lovely city to visit for a computer scientist like you.\n";
            systemPrompt +=
                "OUTPUT: Aachen is a lovely city to visit for a computer scientist like you. [This is a general statement about Aachen and the user with no information.][Fluff]\n";
            systemPrompt += "---\n";
            systemPrompt +=
                "INPUT: User question: \"When was Elisenbrunnen build?\" \n Guide Response:\"The Elisenbrunnen as it is today was built between 1822 and 1827.\"";
            systemPrompt +=
                "OUTPUT: The Elisenbrunnen as it is today was built between 1822 and 1827.[Years are correct.][Correct]\n";
            systemPrompt += "---";
            await OnChat(systemPrompt);
            SetRole(GeminiRole.User);
            var pointOfInterestManagerData =
                File.ReadAllText(inputPath);
            var saveData =
                JsonConvert.DeserializeObject<SaveDataPointOfInterestManager>(pointOfInterestManagerData);
            var excelJsonConverter = new ExcelJsonConverter();
            var totalNumberOfQuestions = 0;
            foreach (var poi in saveData.pointOfInterests)
                if (poi.title == saveData.pointOfInterests[0].title)
                {
                    excelJsonConverter.firstInfoSuperC = poi.tourContent[0].Text;
                    excelJsonConverter.firstInfoSuperCActions = poi.tourContent[0].GetFunctionAndParameters();
                    excelJsonConverter.CheckTruthfulnessSuperCFirst = await OnChat(poi.tourContent[0].Text);
                    excelJsonConverter.secondInfoSuperC = poi.tourContent[1].Text;
                    excelJsonConverter.secondInfoSuperCActions = poi.tourContent[1].GetFunctionAndParameters();
                    excelJsonConverter.CheckTruthfulnessSuperCSecond = await OnChat(poi.tourContent[1].Text);
                    excelJsonConverter.thirdInfoSuperC = poi.tourContent[2].Text;
                    excelJsonConverter.thirdInfoSuperCActions = poi.tourContent[2].GetFunctionAndParameters();
                    excelJsonConverter.CheckTruthfulnessSuperCThird = await OnChat(poi.tourContent[2].Text);
                    excelJsonConverter.fourthInfoSuperC = poi.tourContent[3].Text;
                    excelJsonConverter.fourthInfoSuperCActions = poi.tourContent[3].GetFunctionAndParameters();
                    excelJsonConverter.CheckTruthfulnessSuperCFourth = await OnChat(poi.tourContent[3].Text);


                    var i = 0;
                    foreach (var qAndA in poi.qandA)
                    {
                        if (qAndA.answer == "...") continue;
                        totalNumberOfQuestions++;
                        switch (i)
                        {
                            case 0:
                                excelJsonConverter.firstQuestionSuperCWithPosition1 = qAndA.question;
                                excelJsonConverter.firstAnswerSuperCWithPosition1 =
                                    await CheckAnswer(qAndA.question, qAndA.answer);
                                break;
                            case 1:
                                excelJsonConverter.firstQuestionSuperCWithPosition2 = qAndA.question;
                                excelJsonConverter.firstAnswerSuperCWithPosition2 =
                                    await CheckAnswer(qAndA.question, qAndA.answer);
                                break;
                            case 2:
                                excelJsonConverter.firstQuestionSuperCWithPosition3 = qAndA.question;
                                excelJsonConverter.firstAnswerSuperCWithPosition3 =
                                    await CheckAnswer(qAndA.question, qAndA.answer);
                                break;
                            case 3:
                                excelJsonConverter.firstQuestionSuperCWithPosition4 = qAndA.question;
                                excelJsonConverter.firstAnswerSuperCWithPosition4 =
                                    await CheckAnswer(qAndA.question, qAndA.answer);
                                break;
                            case 4:
                                excelJsonConverter.firstQuestionSuperCWithPosition5 = qAndA.question;
                                excelJsonConverter.firstAnswerSuperCWithPosition5 =
                                    await CheckAnswer(qAndA.question, qAndA.answer);
                                break;
                            case 5:
                                excelJsonConverter.firstQuestionSuperCWithPosition6 = qAndA.question;
                                excelJsonConverter.firstAnswerSuperCWithPosition6 =
                                    await CheckAnswer(qAndA.question, qAndA.answer);
                                break;
                            default:
                                Debug.Log("There are more than 8 questions in the first superC");
                                break;
                        }

                        i++;
                    }
                }
                else if (poi.title == saveData.pointOfInterests[1].title)
                {
                    excelJsonConverter.firstInfoTownHall = poi.tourContent[0].Text;
                    excelJsonConverter.firstInfoTownHallActions = poi.tourContent[0].GetFunctionAndParameters();
                    excelJsonConverter.CheckTruthfulnessTownHallFirst = await OnChat(poi.tourContent[0].Text);
                    excelJsonConverter.secondInfoTownHall = poi.tourContent[1].Text;
                    excelJsonConverter.secondInfoTownHallActions = poi.tourContent[1].GetFunctionAndParameters();
                    excelJsonConverter.CheckTruthfulnessTownHallSecond = await OnChat(poi.tourContent[1].Text);
                    excelJsonConverter.thirdInfoTownHall = poi.tourContent[2].Text;
                    excelJsonConverter.thirdInfoTownHallActions = poi.tourContent[2].GetFunctionAndParameters();
                    excelJsonConverter.CheckTruthfulnessTownHallThird = await OnChat(poi.tourContent[2].Text);
                    excelJsonConverter.fourthInfoTownHall = poi.tourContent[3].Text;
                    excelJsonConverter.fourthInfoTownHallActions = poi.tourContent[3].GetFunctionAndParameters();
                    excelJsonConverter.CheckTruthfulnessTownHallFourth = await OnChat(poi.tourContent[3].Text);


                    var i = 0;
                    foreach (var qAndA in poi.qandA)
                    {
                        if (qAndA.answer == "...") continue;
                        totalNumberOfQuestions++;
                        switch (i)
                        {
                            case 0:
                                excelJsonConverter.firstQuestionTownHallWithPosition1 = qAndA.question;
                                excelJsonConverter.firstAnswerTownHallWithPosition1 =
                                    await CheckAnswer(qAndA.question, qAndA.answer);
                                break;
                            case 1:
                                excelJsonConverter.firstQuestionTownHallWithPosition2 = qAndA.question;
                                excelJsonConverter.firstAnswerTownHallWithPosition2 =
                                    await CheckAnswer(qAndA.question, qAndA.answer);
                                break;
                            case 2:
                                excelJsonConverter.firstQuestionTownHallWithPosition3 = qAndA.question;
                                excelJsonConverter.firstAnswerTownHallWithPosition3 =
                                    await CheckAnswer(qAndA.question, qAndA.answer);
                                break;
                            case 3:
                                excelJsonConverter.firstQuestionTownHallWithPosition4 = qAndA.question;
                                excelJsonConverter.firstAnswerTownHallWithPosition4 =
                                    await CheckAnswer(qAndA.question, qAndA.answer);
                                break;
                            case 4:
                                excelJsonConverter.firstQuestionTownHallWithPosition5 = qAndA.question;
                                excelJsonConverter.firstAnswerTownHallWithPosition5 =
                                    await CheckAnswer(qAndA.question, qAndA.answer);
                                break;
                            case 5:
                                excelJsonConverter.firstQuestionTownHallWithPosition6 = qAndA.question;
                                excelJsonConverter.firstAnswerTownHallWithPosition6 =
                                    await CheckAnswer(qAndA.question, qAndA.answer);
                                break;
                            default:
                                Debug.LogWarning("There are more than 6 questions in the first TownHall");
                                break;
                        }

                        i++;
                    }
                }
                else if (poi.title == saveData.pointOfInterests[2].title)
                {
                    excelJsonConverter.firstInfoCathedral = poi.tourContent[0].Text;
                    excelJsonConverter.firstInfoCathedralActions = poi.tourContent[0].GetFunctionAndParameters();
                    excelJsonConverter.CheckTruthfulnessCathedralFirst = await OnChat(poi.tourContent[0].Text);
                    excelJsonConverter.secondInfoCathedral = poi.tourContent[1].Text;
                    excelJsonConverter.secondInfoCathedralActions = poi.tourContent[1].GetFunctionAndParameters();
                    excelJsonConverter.CheckTruthfulnessCathedralSecond = await OnChat(poi.tourContent[1].Text);
                    excelJsonConverter.thirdInfoCathedral = poi.tourContent[2].Text;
                    excelJsonConverter.thirdInfoCathedralActions = poi.tourContent[2].GetFunctionAndParameters();
                    excelJsonConverter.CheckTruthfulnessCathedralThird = await OnChat(poi.tourContent[2].Text);
                    excelJsonConverter.fourthInfoCathedral = poi.tourContent[3].Text;
                    excelJsonConverter.fourthInfoCathedralActions = poi.tourContent[3].GetFunctionAndParameters();
                    excelJsonConverter.CheckTruthfulnessCathedralFourth = await OnChat(poi.tourContent[3].Text);

                    var i = 0;
                    foreach (var qAndA in poi.qandA)
                    {
                        if (qAndA.answer == "...") continue;
                        totalNumberOfQuestions++;
                        switch (i)
                        {
                            case 0:
                                excelJsonConverter.firstQuestionCathedralWithPosition1 = qAndA.question;
                                excelJsonConverter.firstAnswerCathedralWithPosition1 =
                                    await CheckAnswer(qAndA.question, qAndA.answer);
                                break;
                            case 1:
                                excelJsonConverter.firstQuestionCathedralWithPosition2 = qAndA.question;
                                excelJsonConverter.firstAnswerCathedralWithPosition2 =
                                    await CheckAnswer(qAndA.question, qAndA.answer);
                                break;
                            case 2:
                                excelJsonConverter.firstQuestionCathedralWithPosition3 = qAndA.question;
                                excelJsonConverter.firstAnswerCathedralWithPosition3 =
                                    await CheckAnswer(qAndA.question, qAndA.answer);
                                break;
                            case 3:
                                excelJsonConverter.firstQuestionCathedralWithPosition4 = qAndA.question;
                                excelJsonConverter.firstAnswerCathedralWithPosition4 =
                                    await CheckAnswer(qAndA.question, qAndA.answer);
                                break;
                            case 4:
                                excelJsonConverter.firstQuestionCathedralWithPosition5 = qAndA.question;
                                excelJsonConverter.firstAnswerCathedralWithPosition5 =
                                    await CheckAnswer(qAndA.question, qAndA.answer);
                                break;
                            case 5:
                                excelJsonConverter.firstQuestionCathedralWithPosition6 = qAndA.question;
                                excelJsonConverter.firstAnswerCathedralWithPosition6 =
                                    await CheckAnswer(qAndA.question, qAndA.answer);
                                break;
                            default:
                                Debug.LogWarning("There are more than 6 questions in the first Cathedral");
                                break;
                        }

                        i++;
                    }
                }
                else if (poi.title == saveData.pointOfInterests[3].title)
                {
                    excelJsonConverter.firstInfoElisenbrunnen = poi.tourContent[0].Text;
                    excelJsonConverter.firstInfoElisenbrunnenActions = poi.tourContent[0].GetFunctionAndParameters();
                    excelJsonConverter.CheckTruthfulnessElisenbrunnenFirst = await OnChat(poi.tourContent[0].Text);
                    excelJsonConverter.secondInfoElisenbrunnen = poi.tourContent[1].Text;
                    excelJsonConverter.secondInfoElisenbrunnenActions = poi.tourContent[1].GetFunctionAndParameters();
                    excelJsonConverter.CheckTruthfulnessElisenbrunnenSecond = await OnChat(poi.tourContent[1].Text);
                    excelJsonConverter.thirdInfoElisenbrunnen = poi.tourContent[2].Text;
                    excelJsonConverter.thirdInfoElisenbrunnenActions = poi.tourContent[2].GetFunctionAndParameters();
                    excelJsonConverter.CheckTruthfulnessElisenbrunnenThird = await OnChat(poi.tourContent[2].Text);
                    excelJsonConverter.fourthInfoElisenbrunnen = poi.tourContent[3].Text;
                    excelJsonConverter.fourthInfoElisenbrunnenActions = poi.tourContent[3].GetFunctionAndParameters();
                    excelJsonConverter.CheckTruthfulnessElisenbrunnenFourth = await OnChat(poi.tourContent[3].Text);

                    var i = 0;
                    foreach (var qAndA in poi.qandA)
                    {
                        if (qAndA.answer == "...") continue;
                        totalNumberOfQuestions++;
                        switch (i)
                        {
                            case 0:
                                excelJsonConverter.firstQuestionElisenbrunnenWithPosition1 = qAndA.question;
                                excelJsonConverter.firstAnswerElisenbrunnenWithPosition1 =
                                    await CheckAnswer(qAndA.question, qAndA.answer);
                                break;
                            case 1:
                                excelJsonConverter.firstQuestionElisenbrunnenWithPosition2 = qAndA.question;
                                excelJsonConverter.firstAnswerElisenbrunnenWithPosition2 =
                                    await CheckAnswer(qAndA.question, qAndA.answer);
                                break;
                            case 2:
                                excelJsonConverter.firstQuestionElisenbrunnenWithPosition3 = qAndA.question;
                                excelJsonConverter.firstAnswerElisenbrunnenWithPosition3 =
                                    await CheckAnswer(qAndA.question, qAndA.answer);
                                break;
                            case 3:
                                excelJsonConverter.firstQuestionElisenbrunnenWithPosition4 = qAndA.question;
                                excelJsonConverter.firstAnswerElisenbrunnenWithPosition4 =
                                    await CheckAnswer(qAndA.question, qAndA.answer);
                                break;
                            case 4:
                                excelJsonConverter.firstQuestionElisenbrunnenWithPosition5 = qAndA.question;
                                excelJsonConverter.firstAnswerElisenbrunnenWithPosition5 =
                                    await CheckAnswer(qAndA.question, qAndA.answer);
                                break;
                            case 5:
                                excelJsonConverter.firstQuestionElisenbrunnenWithPosition6 = qAndA.question;
                                excelJsonConverter.firstAnswerElisenbrunnenWithPosition6 =
                                    await CheckAnswer(qAndA.question, qAndA.answer);
                                break;
                            case 6:
                                excelJsonConverter.firstQuestionElisenbrunnenWithPosition7 = qAndA.question;
                                excelJsonConverter.firstAnswerElisenbrunnenWithPosition7 =
                                    await CheckAnswer(qAndA.question, qAndA.answer);
                                break;
                            case 7:
                                excelJsonConverter.firstQuestionElisenbrunnenWithPosition8 = qAndA.question;
                                excelJsonConverter.firstAnswerElisenbrunnenWithPosition8 =
                                    await CheckAnswer(qAndA.question, qAndA.answer);
                                break;
                            case 8:
                                excelJsonConverter.firstQuestionElisenbrunnenWithPosition9 = qAndA.question;
                                excelJsonConverter.firstAnswerElisenbrunnenWithPosition9 =
                                    await CheckAnswer(qAndA.question, qAndA.answer);
                                break;
                            case 9:
                                excelJsonConverter.firstQuestionElisenbrunnenWithPosition10 = qAndA.question;
                                excelJsonConverter.firstAnswerElisenbrunnenWithPosition10 =
                                    await CheckAnswer(qAndA.question, qAndA.answer);
                                break;

                            default:
                                Debug.LogWarning("There are more than 10 questions in the Elisenbrunnen");
                                break;
                        }

                        i++;
                    }
                }


            excelJsonConverter.totalNumberOfCorrectInformation =
                CountAllTermsInCheckInformation(excelJsonConverter, "[Correct]");
            excelJsonConverter.totalNumberOfFluffInformation =
                CountAllTermsInCheckInformation(excelJsonConverter, "[Fluff]");
            excelJsonConverter.totalNumberOfMisleadingInformation =
                CountAllTermsInCheckInformation(excelJsonConverter, "[Misleading]");
            excelJsonConverter.totalNumberOfIncorrectInformation =
                CountAllTermsInCheckInformation(excelJsonConverter, "[Incorrect]");
            excelJsonConverter.totalNumberOfMissingContextInformation =
                CountAllTermsInCheckInformation(excelJsonConverter, "[Missing Context]");

            excelJsonConverter.totalNumberOfPredefinedQuestions =
                CountAllTermsInAnswers(excelJsonConverter, "[Correct]");
            excelJsonConverter.totalNumberOfFluffInformationQuestions =
                CountAllTermsInAnswers(excelJsonConverter, "[Fluff]");
            excelJsonConverter.totalNumberOfMisleadingInformationQuestions =
                CountAllTermsInAnswers(excelJsonConverter, "[Misleading]");
            excelJsonConverter.totalNumberOfIncorrectInformationQuestions =
                CountAllTermsInAnswers(excelJsonConverter, "[Incorrect]");
            excelJsonConverter.totalNumberOfMissingContextInformationQuestions =
                CountAllTermsInAnswers(excelJsonConverter, "[Missing Context]");
            Debug.Log("Done 1.");

            excelJsonConverter.totalNumberOfQuestions = totalNumberOfQuestions;
            var json = JsonConvert.SerializeObject(excelJsonConverter, Formatting.Indented);
            File.WriteAllText(outPutPath, json);
            Debug.Log("Done 2.");
        }

        [MenuItem("Debug/ExcelPreparation/reloadQuestions")]
        public static async Task reloadQuestions()
        {
            var files = new List<string>();

            var baseDirectory = "Assets/DebugBackUps/FilesToGoTrough";

            // Iterate through all folders in the base directory
            foreach (var folderPath in Directory.GetDirectories(baseDirectory))
            {
                var pointOfInterestManagerPath = Path.Combine(folderPath, "PointOfInterestManager");

                if (Directory.Exists(pointOfInterestManagerPath))
                {
                    var filesInFolderInput = Directory.GetFiles(pointOfInterestManagerPath);

                    if (filesInFolderInput.Length > 0)
                    {
                        // Find the largest file (by length) in the PointOfInterestManager subfolder.
                        var largestFile = filesInFolderInput
                            .Select(f => new FileInfo(f)) // Convert the file paths to FileInfo objects
                            .OrderByDescending(fi => fi.Length) // Order by size (largest first)
                            .FirstOrDefault(); // Take the first (largest) file, or null if the folder was empty

                        if (largestFile != null) // Check if a largest file was actually found
                            files.Add(largestFile.FullName); // Add the full path of the largest file
                    }
                }
            }


            var filesInFolder = Directory.GetFiles("Assets/DebugBackUps/ConvertedFiles6Gemini2_5WithEmbedding~");
            foreach (var filepath in filesInFolder)
            {
                var excelJsonConverter =
                    JsonConvert.DeserializeObject<ExcelJsonConverter>(
                        await File.ReadAllTextAsync(filepath));
                if (filepath.Contains("Helper"))
                {
                }
                else
                {
                    // get filename from filepath
                    var fileName = Path.GetFileName(filepath);
                    // remove the last 14 characters from the filename
                    fileName = fileName.Remove(fileName.Length - 15);

                    // find the file with the same name in the files list
                    var file = files.Find(f => f.Contains(fileName));

                    if (string.IsNullOrEmpty(file))
                    {
                        Debug.LogWarning("Could not find file: " + file);
                        continue;
                    }

                    excelJsonConverter = deleteAllQuestionContent(excelJsonConverter);

                    var pointOfInterestManagerData =
                        await File.ReadAllTextAsync(file);
                    var saveData =
                        JsonConvert.DeserializeObject<SaveDataPointOfInterestManager>(pointOfInterestManagerData);
                    var totalNumberOfQuestions = 0;
                    var totalNumberOfPredefinedQuestions = 0;

                    foreach (var poi in saveData.pointOfInterests)

                        if (poi.title == saveData.pointOfInterests[0].title)
                        {
                            var questions = new List<string>();
                            var i = 0;
                            foreach (var qAndA in poi.qandA)
                            {
                                if (qAndA.answer == "...") continue;
                                if (!questions.Contains(qAndA.question))
                                {
                                    totalNumberOfQuestions++;
                                    if (qAndA.question.Contains("Can you tell me something about"))
                                        totalNumberOfPredefinedQuestions++;
                                }

                                questions.Add(qAndA.question);
                                switch (i)
                                {
                                    case 0:
                                        excelJsonConverter.firstQuestionSuperCWithPosition1 = qAndA.question;
                                        excelJsonConverter.firstAnswerSuperCWithPosition1 = qAndA.answer;
                                        break;
                                    case 1:
                                        excelJsonConverter.firstQuestionSuperCWithPosition2 = qAndA.question;
                                        excelJsonConverter.firstAnswerSuperCWithPosition2 = qAndA.answer;
                                        break;
                                    case 2:
                                        excelJsonConverter.firstQuestionSuperCWithPosition3 = qAndA.question;
                                        excelJsonConverter.firstAnswerSuperCWithPosition3 = qAndA.answer;
                                        break;
                                    case 3:
                                        excelJsonConverter.firstQuestionSuperCWithPosition4 = qAndA.question;
                                        excelJsonConverter.firstAnswerSuperCWithPosition4 = qAndA.answer;
                                        break;
                                    case 4:
                                        excelJsonConverter.firstQuestionSuperCWithPosition5 = qAndA.question;
                                        excelJsonConverter.firstAnswerSuperCWithPosition5 = qAndA.answer;
                                        break;
                                    case 5:
                                        excelJsonConverter.firstQuestionSuperCWithPosition6 = qAndA.question;
                                        excelJsonConverter.firstAnswerSuperCWithPosition6 = qAndA.answer;
                                        break;
                                    default:
                                        Debug.Log("There are more than 8 questions in the first superC");
                                        break;
                                }

                                i++;
                            }
                        }
                        else if (poi.title == saveData.pointOfInterests[1].title)
                        {
                            var questions = new List<string>();
                            var i = 0;
                            foreach (var qAndA in poi.qandA)
                            {
                                if (qAndA.answer == "...") continue;
                                if (!questions.Contains(qAndA.question))
                                {
                                    totalNumberOfQuestions++;
                                    if (qAndA.question.Contains("Can you tell me something about"))
                                        totalNumberOfPredefinedQuestions++;
                                }

                                questions.Add(qAndA.question);
                                switch (i)
                                {
                                    case 0:
                                        excelJsonConverter.firstQuestionTownHallWithPosition1 = qAndA.question;
                                        excelJsonConverter.firstAnswerTownHallWithPosition1 = qAndA.answer;
                                        break;
                                    case 1:
                                        excelJsonConverter.firstQuestionTownHallWithPosition2 = qAndA.question;
                                        excelJsonConverter.firstAnswerTownHallWithPosition2 = qAndA.answer;
                                        break;
                                    case 2:
                                        excelJsonConverter.firstQuestionTownHallWithPosition3 = qAndA.question;
                                        excelJsonConverter.firstAnswerTownHallWithPosition3 = qAndA.answer;
                                        break;
                                    case 3:
                                        excelJsonConverter.firstQuestionTownHallWithPosition4 = qAndA.question;
                                        excelJsonConverter.firstAnswerTownHallWithPosition4 = qAndA.answer;
                                        break;
                                    case 4:
                                        excelJsonConverter.firstQuestionTownHallWithPosition5 = qAndA.question;
                                        excelJsonConverter.firstAnswerTownHallWithPosition5 = qAndA.answer;
                                        break;
                                    case 5:
                                        excelJsonConverter.firstQuestionTownHallWithPosition6 = qAndA.question;
                                        excelJsonConverter.firstAnswerTownHallWithPosition6 = qAndA.answer;
                                        break;
                                    default:
                                        Debug.LogWarning("There are more than 6 questions in the first TownHall");
                                        break;
                                }

                                i++;
                            }
                        }
                        else if (poi.title == saveData.pointOfInterests[2].title)
                        {
                            var questions = new List<string>();
                            var i = 0;
                            foreach (var qAndA in poi.qandA)
                            {
                                if (qAndA.answer == "...") continue;
                                if (!questions.Contains(qAndA.question))
                                {
                                    totalNumberOfQuestions++;
                                    if (qAndA.question.Contains("Can you tell me something about"))
                                        totalNumberOfPredefinedQuestions++;
                                }

                                questions.Add(qAndA.question);
                                switch (i)
                                {
                                    case 0:
                                        excelJsonConverter.firstQuestionCathedralWithPosition1 = qAndA.question;
                                        excelJsonConverter.firstAnswerCathedralWithPosition1 = qAndA.answer;

                                        break;
                                    case 1:
                                        excelJsonConverter.firstQuestionCathedralWithPosition2 = qAndA.question;
                                        excelJsonConverter.firstAnswerCathedralWithPosition2 = qAndA.answer;

                                        break;
                                    case 2:
                                        excelJsonConverter.firstQuestionCathedralWithPosition3 = qAndA.question;
                                        excelJsonConverter.firstAnswerCathedralWithPosition3 = qAndA.answer;

                                        break;
                                    case 3:
                                        excelJsonConverter.firstQuestionCathedralWithPosition4 = qAndA.question;
                                        excelJsonConverter.firstAnswerCathedralWithPosition4 = qAndA.answer;

                                        break;
                                    case 4:
                                        excelJsonConverter.firstQuestionCathedralWithPosition5 = qAndA.question;
                                        excelJsonConverter.firstAnswerCathedralWithPosition5 = qAndA.answer;
                                        break;
                                    case 5:
                                        excelJsonConverter.firstQuestionCathedralWithPosition6 = qAndA.question;
                                        excelJsonConverter.firstAnswerCathedralWithPosition6 = qAndA.answer;
                                        break;
                                    default:
                                        Debug.LogWarning("There are more than 6 questions in the first Cathedral");
                                        break;
                                }

                                i++;
                            }
                        }
                        else if (poi.title == saveData.pointOfInterests[3].title)
                        {
                            var questions = new List<string>();
                            var i = 0;
                            foreach (var qAndA in poi.qandA)
                            {
                                if (qAndA.answer == "...") continue;
                                if (!questions.Contains(qAndA.question))
                                {
                                    totalNumberOfQuestions++;
                                    if (qAndA.question.Contains("Can you tell me something about"))
                                        totalNumberOfPredefinedQuestions++;
                                }

                                questions.Add(qAndA.question);
                                switch (i)
                                {
                                    case 0:
                                        excelJsonConverter.firstQuestionElisenbrunnenWithPosition1 = qAndA.question;
                                        excelJsonConverter.firstAnswerElisenbrunnenWithPosition1 = qAndA.answer;
                                        break;
                                    case 1:
                                        excelJsonConverter.firstQuestionElisenbrunnenWithPosition2 = qAndA.question;
                                        excelJsonConverter.firstAnswerElisenbrunnenWithPosition2 = qAndA.answer;
                                        break;
                                    case 2:
                                        excelJsonConverter.firstQuestionElisenbrunnenWithPosition3 = qAndA.question;
                                        excelJsonConverter.firstAnswerElisenbrunnenWithPosition3 = qAndA.answer;
                                        break;
                                    case 3:
                                        excelJsonConverter.firstQuestionElisenbrunnenWithPosition4 = qAndA.question;
                                        excelJsonConverter.firstAnswerElisenbrunnenWithPosition4 = qAndA.answer;
                                        break;
                                    case 4:
                                        excelJsonConverter.firstQuestionElisenbrunnenWithPosition5 = qAndA.question;
                                        excelJsonConverter.firstAnswerElisenbrunnenWithPosition5 = qAndA.answer;
                                        break;
                                    case 5:
                                        excelJsonConverter.firstQuestionElisenbrunnenWithPosition6 = qAndA.question;
                                        excelJsonConverter.firstAnswerElisenbrunnenWithPosition6 = qAndA.answer;
                                        break;
                                    case 6:
                                        excelJsonConverter.firstQuestionElisenbrunnenWithPosition7 = qAndA.question;
                                        excelJsonConverter.firstAnswerElisenbrunnenWithPosition7 = qAndA.answer;
                                        break;
                                    case 7:
                                        excelJsonConverter.firstQuestionElisenbrunnenWithPosition8 = qAndA.question;
                                        excelJsonConverter.firstAnswerElisenbrunnenWithPosition8 = qAndA.answer;
                                        break;
                                    case 8:
                                        excelJsonConverter.firstQuestionElisenbrunnenWithPosition9 = qAndA.question;
                                        excelJsonConverter.firstAnswerElisenbrunnenWithPosition9 = qAndA.answer;
                                        break;
                                    case 9:
                                        excelJsonConverter.firstQuestionElisenbrunnenWithPosition10 = qAndA.question;
                                        excelJsonConverter.firstAnswerElisenbrunnenWithPosition10 = qAndA.answer;
                                        break;

                                    default:
                                        Debug.LogWarning("There are more than 10 questions in the Elisenbrunnen");
                                        break;
                                }

                                i++;
                            }
                        }

                    excelJsonConverter.totalNumberOfQuestions = totalNumberOfQuestions;
                    excelJsonConverter.totalNumberOfPredefinedQuestions = totalNumberOfPredefinedQuestions;
                    var json = JsonConvert.SerializeObject(excelJsonConverter, Formatting.Indented);
                    await File.WriteAllTextAsync(filepath, json);
                    Debug.Log("Wrote to file: " + filepath);
                }
            }

            Debug.Log("Done with reloading questions.");
        }

        private static ExcelJsonConverter deleteAllQuestionContent(ExcelJsonConverter excelJsonConverter)
        {
            excelJsonConverter.firstQuestionSuperCWithPosition1 = "";
            excelJsonConverter.firstAnswerSuperCWithPosition1 = "";
            excelJsonConverter.firstQuestionSuperCWithPosition2 = "";
            excelJsonConverter.firstAnswerSuperCWithPosition2 = "";
            excelJsonConverter.firstQuestionSuperCWithPosition3 = "";
            excelJsonConverter.firstAnswerSuperCWithPosition3 = "";
            excelJsonConverter.firstQuestionSuperCWithPosition4 = "";
            excelJsonConverter.firstAnswerSuperCWithPosition4 = "";
            excelJsonConverter.firstQuestionSuperCWithPosition5 = "";
            excelJsonConverter.firstAnswerSuperCWithPosition5 = "";
            excelJsonConverter.firstQuestionSuperCWithPosition6 = "";
            excelJsonConverter.firstAnswerSuperCWithPosition6 = "";
            excelJsonConverter.firstQuestionTownHallWithPosition1 = "";
            excelJsonConverter.firstAnswerTownHallWithPosition1 = "";
            excelJsonConverter.firstQuestionTownHallWithPosition2 = "";
            excelJsonConverter.firstAnswerTownHallWithPosition2 = "";
            excelJsonConverter.firstQuestionTownHallWithPosition3 = "";
            excelJsonConverter.firstAnswerTownHallWithPosition3 = "";
            excelJsonConverter.firstQuestionTownHallWithPosition4 = "";
            excelJsonConverter.firstAnswerTownHallWithPosition4 = "";
            excelJsonConverter.firstQuestionTownHallWithPosition5 = "";
            excelJsonConverter.firstAnswerTownHallWithPosition5 = "";
            excelJsonConverter.firstQuestionTownHallWithPosition6 = "";
            excelJsonConverter.firstAnswerTownHallWithPosition6 = "";
            excelJsonConverter.firstQuestionCathedralWithPosition1 = "";
            excelJsonConverter.firstAnswerCathedralWithPosition1 = "";
            excelJsonConverter.firstQuestionCathedralWithPosition2 = "";
            excelJsonConverter.firstAnswerCathedralWithPosition2 = "";
            excelJsonConverter.firstQuestionCathedralWithPosition3 = "";
            excelJsonConverter.firstAnswerCathedralWithPosition3 = "";
            excelJsonConverter.firstQuestionCathedralWithPosition4 = "";
            excelJsonConverter.firstAnswerCathedralWithPosition4 = "";
            excelJsonConverter.firstQuestionCathedralWithPosition5 = "";
            excelJsonConverter.firstAnswerCathedralWithPosition5 = "";
            excelJsonConverter.firstQuestionCathedralWithPosition6 = "";
            excelJsonConverter.firstAnswerCathedralWithPosition6 = "";
            excelJsonConverter.firstQuestionElisenbrunnenWithPosition1 = "";
            excelJsonConverter.firstAnswerElisenbrunnenWithPosition1 = "";
            excelJsonConverter.firstQuestionElisenbrunnenWithPosition2 = "";
            excelJsonConverter.firstAnswerElisenbrunnenWithPosition2 = "";
            excelJsonConverter.firstQuestionElisenbrunnenWithPosition3 = "";
            excelJsonConverter.firstAnswerElisenbrunnenWithPosition3 = "";
            excelJsonConverter.firstQuestionElisenbrunnenWithPosition4 = "";
            excelJsonConverter.firstAnswerElisenbrunnenWithPosition4 = "";
            excelJsonConverter.firstQuestionElisenbrunnenWithPosition5 = "";
            excelJsonConverter.firstAnswerElisenbrunnenWithPosition5 = "";
            excelJsonConverter.firstQuestionElisenbrunnenWithPosition6 = "";
            excelJsonConverter.firstAnswerElisenbrunnenWithPosition6 = "";
            excelJsonConverter.firstQuestionElisenbrunnenWithPosition7 = "";
            excelJsonConverter.firstAnswerElisenbrunnenWithPosition7 = "";
            excelJsonConverter.firstQuestionElisenbrunnenWithPosition8 = "";
            excelJsonConverter.firstAnswerElisenbrunnenWithPosition8 = "";
            excelJsonConverter.firstQuestionElisenbrunnenWithPosition9 = "";
            excelJsonConverter.firstAnswerElisenbrunnenWithPosition9 = "";
            excelJsonConverter.firstQuestionElisenbrunnenWithPosition10 = "";
            excelJsonConverter.firstAnswerElisenbrunnenWithPosition10 = "";

            excelJsonConverter.totalNumberOfQuestions = 0;

            return excelJsonConverter;

            return excelJsonConverter;
        }

        public static async Task LoadPointOfInterestManagerFromFileAsOneMessage(string inputPath, string outPutPath)
        {
            SetRoleToSystemPrompt();
            _chatHistory = new List<GeminiContent>();
            var systemPrompt = "SYSTEM PROMPT:\n\n";
            systemPrompt +=
                "You are an expert in Aachen architecture and history. You will receive text describing tours of Aachen, specifically focusing on the Super C, Aachen Townhall, Aachen Cathedral, and Aachen Elisenbrunnen. Your task is to identify errors, misleading information, and filler content.\n\n";
            systemPrompt +=
                "For each sentence you receive, respond by first repeating the sentence verbatim, then adding  [Explanation][X] where:\n\n";
            systemPrompt += "* X is one of the following classifications:\n";
            systemPrompt +=
                "    * Correct: The sentence is factually accurate and relevant to the context of a tour. [Explanation: A very short confirmation of accuracy, max 5 words]\n";
            systemPrompt +=
                "    * Fluff: The sentence is unnecessary, provides no significant information, and is essentially filler. [Explanation: A very short confirmation of accuracy, max 5 words]\n";
            systemPrompt +=
                "    * Misleading: The sentence is factually correct but presents information in a way that could be misinterpreted or creates a false impression in the context of the tour or explanation.\n";
            systemPrompt +=
                "    * Incorrect: The sentence contains a factual error. [Explanation: The actual correct information.]\n";
            systemPrompt +=
                "    * Missing Context: The sentence is not wrong but requires additional information to be understandable for the purpose of a tour.\n\n";
            systemPrompt +=
                "* Explanation: A brief explanation of why you chose that classification. Be specific about the error or misleading aspect, unless the classification is \"Correct.\"\n\n";
            systemPrompt += "---\n\n";
            systemPrompt += "EXAMPLES:\n\n";
            systemPrompt += "---\n";
            systemPrompt += "INPUT: Charlemagne laid the first stone of the Aachen Cathedral.\n";
            systemPrompt +=
                "OUTPUT: Charlemagne laid the first stone of the Aachen Cathedral. [While Charlemagne commissioned the Palatine Chapel, which is the core of the Cathedral, he didn't lay the first stone. It was built over time by multiple builders.][Incorrect]\n";
            systemPrompt += "---\n";
            systemPrompt +=
                "INPUT: The Elisenbrunnen had the architects Johann Peter Cremer and Karl Friedrich Schinkel.\n";
            systemPrompt +=
                "OUTPUT: The Elisenbrunnen had the architects Johann Peter Cremer and Karl Friedrich Schinkel. [Right architects.][Correct]\n";
            systemPrompt += "---\n";
            systemPrompt += "INPUT: The Super C is a visually striking building with a lot of glass.\n";
            systemPrompt +=
                "OUTPUT: The Super C is a visually striking building with a lot of glass. [Known for its glass.][Correct]\n";
            systemPrompt += "---\n";
            systemPrompt += "INPUT: The Elisenbrunnen dispenses water that cures all diseases.\n";
            systemPrompt +=
                "OUTPUT: The Elisenbrunnen dispenses water that cures all diseases. [The water has a high sulfur content and was once considered medicinal, but it doesn't cure *all* diseases. This statement exaggerates its benefits.][Misleading]\n";
            systemPrompt += "---\n";
            systemPrompt += "INPUT: The Rathaus is pretty.\n";
            systemPrompt +=
                "OUTPUT: The Rathaus is pretty. [While subjective, it offers no historical or architectural insight.][Fluff]\n";
            systemPrompt += "---\n";
            systemPrompt += "INPUT: The Town Hall was built in the 14th century.\n";
            systemPrompt += "OUTPUT: The Town Hall was built in the 14th century. [Built in 14th century.][Correct]\n";
            systemPrompt += "---\n";
            systemPrompt += "INPUT: Inside the Aachen Cathedral, you can find many interesting things.\n";
            systemPrompt +=
                "OUTPUT: Inside the Aachen Cathedral, you can find many interesting things. [This statement is vague and doesn't specify what is interesting or important for a tour.][Missing Context]\n";
            systemPrompt += "---\n";
            systemPrompt += "INPUT: Aachen is a lovely city to visit for a computer scientist like you.\n";
            systemPrompt +=
                "OUTPUT: Aachen is a lovely city to visit for a computer scientist like you. [This is a general statement about Aachen and the user with no information.][Fluff]\n";
            systemPrompt += "---\n";
            systemPrompt +=
                "INPUT: User question in front of Elisenbrunnen: \"When was Elisenbrunnen build?\" \n Guide Response:\"The Elisenbrunnen as it is today was built between 1822 and 1827.\"";
            systemPrompt +=
                "OUTPUT: The Elisenbrunnen as it is today was built between 1822 and 1827.[Years are correct.][Correct]\n";
            systemPrompt += "---";
            await OnChat(systemPrompt);
            SetRole(GeminiRole.User);
            var pointOfInterestManagerData =
                File.ReadAllText(inputPath);
            var saveData =
                JsonConvert.DeserializeObject<SaveDataPointOfInterestManager>(pointOfInterestManagerData);
            var excelJsonConverter = new ExcelJsonConverter();
            var totalNumberOfQuestions = 0;
            var allText = "";
            foreach (var poi in saveData.pointOfInterests)
                if (poi.title == saveData.pointOfInterests[0].title)
                {
                    excelJsonConverter.firstInfoSuperC = poi.tourContent[0].Text;
                    excelJsonConverter.firstInfoSuperCActions = poi.tourContent[0].GetFunctionAndParameters();
                    allText += "First paragraph SuperC: " + poi.tourContent[0].Text;
                    excelJsonConverter.secondInfoSuperC = poi.tourContent[1].Text;
                    excelJsonConverter.secondInfoSuperCActions = poi.tourContent[1].GetFunctionAndParameters();
                    allText += "Second paragraph SuperC: " + poi.tourContent[1].Text;
                    excelJsonConverter.thirdInfoSuperC = poi.tourContent[2].Text;
                    excelJsonConverter.thirdInfoSuperCActions = poi.tourContent[2].GetFunctionAndParameters();
                    allText += "Third paragraph SuperC: " + poi.tourContent[2].Text;
                    excelJsonConverter.fourthInfoSuperC = poi.tourContent[3].Text;
                    excelJsonConverter.fourthInfoSuperCActions = poi.tourContent[3].GetFunctionAndParameters();
                    allText += "Fourth paragraph SuperC: " + poi.tourContent[3].Text;


                    var i = 0;
                    foreach (var qAndA in poi.qandA)
                    {
                        if (qAndA.answer == "...") continue;
                        totalNumberOfQuestions++;
                        switch (i)
                        {
                            case 0:
                                excelJsonConverter.firstQuestionSuperCWithPosition1 = qAndA.question;
                                excelJsonConverter.firstAnswerSuperCWithPosition1 = qAndA.answer;
                                allText += "User question in front of SuperC: \"" + qAndA.question +
                                           "\"\n Guide Response:\"" + qAndA.answer + "\"";
                                break;
                            case 1:
                                excelJsonConverter.firstQuestionSuperCWithPosition2 = qAndA.question;
                                excelJsonConverter.firstAnswerSuperCWithPosition2 = qAndA.answer;
                                allText += "User question in front of SuperC: \"" + qAndA.question +
                                           "\"\n Guide Response:\"" + qAndA.answer + "\"";
                                break;
                            case 2:
                                excelJsonConverter.firstQuestionSuperCWithPosition3 = qAndA.question;
                                excelJsonConverter.firstAnswerSuperCWithPosition3 = qAndA.answer;
                                allText += "User question in front of SuperC: \"" + qAndA.question +
                                           "\"\n Guide Response:\"" + qAndA.answer + "\"";
                                break;
                            case 3:
                                excelJsonConverter.firstQuestionSuperCWithPosition4 = qAndA.question;
                                excelJsonConverter.firstAnswerSuperCWithPosition4 = qAndA.answer;
                                allText += "User question in front of SuperC: \"" + qAndA.question +
                                           "\"\n Guide Response:\"" + qAndA.answer + "\"";
                                break;
                            case 4:
                                excelJsonConverter.firstQuestionSuperCWithPosition5 = qAndA.question;
                                excelJsonConverter.firstAnswerSuperCWithPosition5 = qAndA.answer;
                                allText += "User question in front of SuperC: \"" + qAndA.question +
                                           "\"\n Guide Response:\"" + qAndA.answer + "\"";
                                break;
                            case 5:
                                excelJsonConverter.firstQuestionSuperCWithPosition6 = qAndA.question;
                                excelJsonConverter.firstAnswerSuperCWithPosition6 = qAndA.answer;
                                allText += "User question in front of SuperC: \"" + qAndA.question +
                                           "\"\n Guide Response:\"" + qAndA.answer + "\"";
                                break;
                            default:
                                Debug.Log("There are more than 8 questions in the first superC");
                                break;
                        }

                        i++;
                    }
                }
                else if (poi.title == saveData.pointOfInterests[1].title)
                {
                    excelJsonConverter.firstInfoTownHall = poi.tourContent[0].Text;
                    excelJsonConverter.firstInfoTownHallActions = poi.tourContent[0].GetFunctionAndParameters();
                    allText += "First paragraph Aachen Town Hall: " + poi.tourContent[0].Text;
                    excelJsonConverter.secondInfoTownHall = poi.tourContent[1].Text;
                    excelJsonConverter.secondInfoTownHallActions = poi.tourContent[1].GetFunctionAndParameters();
                    allText += "Second paragraph Aachen Town Hall:: " + poi.tourContent[1].Text;
                    excelJsonConverter.thirdInfoTownHall = poi.tourContent[2].Text;
                    excelJsonConverter.thirdInfoTownHallActions = poi.tourContent[2].GetFunctionAndParameters();
                    allText += "Third paragraph Aachen Town Hall:: " + poi.tourContent[2].Text;
                    excelJsonConverter.fourthInfoTownHall = poi.tourContent[3].Text;
                    excelJsonConverter.fourthInfoTownHallActions = poi.tourContent[3].GetFunctionAndParameters();
                    allText += "Fourth paragraph Aachen Town Hall:: " + poi.tourContent[3].Text;


                    var i = 0;
                    foreach (var qAndA in poi.qandA)
                    {
                        if (qAndA.answer == "...") continue;
                        totalNumberOfQuestions++;
                        switch (i)
                        {
                            case 0:
                                excelJsonConverter.firstQuestionTownHallWithPosition1 = qAndA.question;
                                excelJsonConverter.firstAnswerTownHallWithPosition1 = qAndA.answer;
                                allText += "User question in front of Aachen Town Hall: \"" + qAndA.question +
                                           "\"\n Guide Response:\"" +
                                           qAndA.answer + "\"";
                                break;
                            case 1:
                                excelJsonConverter.firstQuestionTownHallWithPosition2 = qAndA.question;
                                excelJsonConverter.firstAnswerTownHallWithPosition2 = qAndA.answer;
                                allText += "User question in front of Aachen Town Hall: \"" + qAndA.question +
                                           "\"\n Guide Response:\"" +
                                           qAndA.answer + "\"";
                                break;
                            case 2:
                                excelJsonConverter.firstQuestionTownHallWithPosition3 = qAndA.question;
                                excelJsonConverter.firstAnswerTownHallWithPosition3 = qAndA.answer;
                                allText += "User question in front of Aachen Town Hall: \"" + qAndA.question +
                                           "\"\n Guide Response:\"" +
                                           qAndA.answer + "\"";
                                break;
                            case 3:
                                excelJsonConverter.firstQuestionTownHallWithPosition4 = qAndA.question;
                                excelJsonConverter.firstAnswerTownHallWithPosition4 = qAndA.answer;
                                allText += "User question in front of Aachen Town Hall: \"" + qAndA.question +
                                           "\"\n Guide Response:\"" +
                                           qAndA.answer + "\"";
                                break;
                            case 4:
                                excelJsonConverter.firstQuestionTownHallWithPosition5 = qAndA.question;
                                excelJsonConverter.firstAnswerTownHallWithPosition5 = qAndA.answer;
                                allText += "User question in front of Aachen Town Hall: \"" + qAndA.question +
                                           "\"\n Guide Response:\"" +
                                           qAndA.answer + "\"";
                                break;
                            case 5:
                                excelJsonConverter.firstQuestionTownHallWithPosition6 = qAndA.question;
                                excelJsonConverter.firstAnswerTownHallWithPosition6 = qAndA.answer;
                                allText += "User question in front of Aachen Town Hall: \"" + qAndA.question +
                                           "\"\n Guide Response:\"" +
                                           qAndA.answer + "\"";
                                break;
                            default:
                                Debug.LogWarning("There are more than 6 questions in the first TownHall");
                                break;
                        }

                        i++;
                    }
                }
                else if (poi.title == saveData.pointOfInterests[2].title)
                {
                    excelJsonConverter.firstInfoCathedral = poi.tourContent[0].Text;
                    excelJsonConverter.firstInfoCathedralActions = poi.tourContent[0].GetFunctionAndParameters();
                    allText += "First paragraph Aachen Cathedral: " + poi.tourContent[0].Text;
                    excelJsonConverter.secondInfoCathedral = poi.tourContent[1].Text;
                    excelJsonConverter.secondInfoCathedralActions = poi.tourContent[1].GetFunctionAndParameters();
                    allText += "Second paragraph Aachen Cathedral: " + poi.tourContent[1].Text;
                    excelJsonConverter.thirdInfoCathedral = poi.tourContent[2].Text;
                    excelJsonConverter.thirdInfoCathedralActions = poi.tourContent[2].GetFunctionAndParameters();
                    allText += "Third paragraph Aachen Cathedral: " + poi.tourContent[2].Text;
                    excelJsonConverter.fourthInfoCathedral = poi.tourContent[3].Text;
                    excelJsonConverter.fourthInfoCathedralActions = poi.tourContent[3].GetFunctionAndParameters();
                    allText += "Fourth paragraph Aachen Cathedral: " + poi.tourContent[3].Text;

                    var i = 0;
                    foreach (var qAndA in poi.qandA)
                    {
                        if (qAndA.answer == "...") continue;
                        totalNumberOfQuestions++;
                        switch (i)
                        {
                            case 0:
                                excelJsonConverter.firstQuestionCathedralWithPosition1 = qAndA.question;
                                excelJsonConverter.firstAnswerCathedralWithPosition1 = qAndA.answer;
                                allText += "User question in front of Aachen Cathedral: \"" + qAndA.question +
                                           "\"\n Guide Response:\"" + qAndA.answer + "\"";
                                break;
                            case 1:
                                excelJsonConverter.firstQuestionCathedralWithPosition2 = qAndA.question;
                                excelJsonConverter.firstAnswerCathedralWithPosition2 = qAndA.answer;
                                allText += "User question in front of Aachen Cathedral: \"" + qAndA.question +
                                           "\"\n Guide Response:\"" + qAndA.answer + "\"";
                                break;
                            case 2:
                                excelJsonConverter.firstQuestionCathedralWithPosition3 = qAndA.question;
                                excelJsonConverter.firstAnswerCathedralWithPosition3 = qAndA.answer;
                                allText += "User question in front of Aachen Cathedral: \"" + qAndA.question +
                                           "\"\n Guide Response:\"" + qAndA.answer + "\"";
                                break;
                            case 3:
                                excelJsonConverter.firstQuestionCathedralWithPosition4 = qAndA.question;
                                excelJsonConverter.firstAnswerCathedralWithPosition4 = qAndA.answer;
                                allText += "User question in front of Aachen Cathedral: \"" + qAndA.question +
                                           "\"\n Guide Response:\"" + qAndA.answer + "\"";
                                break;
                            case 4:
                                excelJsonConverter.firstQuestionCathedralWithPosition5 = qAndA.question;
                                excelJsonConverter.firstAnswerCathedralWithPosition5 = qAndA.answer;
                                allText += "User question in front of Aachen Cathedral: \"" + qAndA.question +
                                           "\"\n Guide Response:\"" +
                                           qAndA.answer + "\"";
                                break;
                            case 5:
                                excelJsonConverter.firstQuestionCathedralWithPosition6 = qAndA.question;
                                excelJsonConverter.firstAnswerCathedralWithPosition6 = qAndA.answer;
                                allText += "User question in front of Aachen Cathedral: \"" + qAndA.question +
                                           "\"\n Guide Response:\"" +
                                           qAndA.answer + "\"";
                                break;
                            default:
                                Debug.LogWarning("There are more than 6 questions in the first Cathedral");
                                break;
                        }

                        i++;
                    }
                }
                else if (poi.title == saveData.pointOfInterests[3].title)
                {
                    excelJsonConverter.firstInfoElisenbrunnen = poi.tourContent[0].Text;
                    excelJsonConverter.firstInfoElisenbrunnenActions = poi.tourContent[0].GetFunctionAndParameters();
                    allText += "First paragraph Elisenbrunnen: " + poi.tourContent[0].Text;
                    excelJsonConverter.secondInfoElisenbrunnen = poi.tourContent[1].Text;
                    excelJsonConverter.secondInfoElisenbrunnenActions = poi.tourContent[1].GetFunctionAndParameters();
                    allText += "Second paragraph Elisenbrunnen: " + poi.tourContent[1].Text;
                    excelJsonConverter.thirdInfoElisenbrunnen = poi.tourContent[2].Text;
                    excelJsonConverter.thirdInfoElisenbrunnenActions = poi.tourContent[2].GetFunctionAndParameters();
                    allText += "Third paragraph Elisenbrunnen: " + poi.tourContent[2].Text;
                    excelJsonConverter.fourthInfoElisenbrunnen = poi.tourContent[3].Text;
                    excelJsonConverter.fourthInfoElisenbrunnenActions = poi.tourContent[3].GetFunctionAndParameters();
                    allText += "Fourth paragraph Elisenbrunnen: " + poi.tourContent[3].Text;

                    var i = 0;
                    foreach (var qAndA in poi.qandA)
                    {
                        totalNumberOfQuestions++;
                        switch (i)
                        {
                            case 0:
                                excelJsonConverter.firstQuestionElisenbrunnenWithPosition1 = qAndA.question;
                                excelJsonConverter.firstAnswerElisenbrunnenWithPosition1 = qAndA.answer;
                                allText += "User question in front of Elisenbrunnen: \"" + qAndA.question +
                                           "\"\n Guide Response:\"" + qAndA.answer + "\"";
                                break;
                            case 1:
                                excelJsonConverter.firstQuestionElisenbrunnenWithPosition2 = qAndA.question;
                                excelJsonConverter.firstAnswerElisenbrunnenWithPosition2 = qAndA.answer;
                                allText += "User question in front of Elisenbrunnen:: \"" + qAndA.question +
                                           "\"\n Guide Response:\"" + qAndA.answer + "\"";
                                break;
                            case 2:
                                excelJsonConverter.firstQuestionElisenbrunnenWithPosition3 = qAndA.question;
                                excelJsonConverter.firstAnswerElisenbrunnenWithPosition3 = qAndA.answer;
                                allText += "User question in front of Elisenbrunnen:: \"" + qAndA.question +
                                           "\"\n Guide Response:\"" + qAndA.answer + "\"";
                                break;
                            case 3:
                                excelJsonConverter.firstQuestionElisenbrunnenWithPosition4 = qAndA.question;
                                excelJsonConverter.firstAnswerElisenbrunnenWithPosition4 = qAndA.answer;
                                allText += "User question in front of Elisenbrunnen:: \"" + qAndA.question +
                                           "\"\n Guide Response:\"" + qAndA.answer + "\"";
                                break;
                            case 4:
                                excelJsonConverter.firstQuestionElisenbrunnenWithPosition5 = qAndA.question;
                                excelJsonConverter.firstAnswerElisenbrunnenWithPosition5 = qAndA.answer;
                                allText += "User question in front of Elisenbrunnen:: \"" + qAndA.question +
                                           "\"\n Guide Response:\"" + qAndA.answer + "\"";
                                break;
                            case 5:
                                excelJsonConverter.firstQuestionElisenbrunnenWithPosition6 = qAndA.question;
                                excelJsonConverter.firstAnswerElisenbrunnenWithPosition6 = qAndA.answer;
                                allText += "User question in front of Elisenbrunnen:: \"" + qAndA.question +
                                           "\"\n Guide Response:\"" + qAndA.answer + "\"";
                                break;
                            case 6:
                                excelJsonConverter.firstQuestionElisenbrunnenWithPosition7 = qAndA.question;
                                excelJsonConverter.firstAnswerElisenbrunnenWithPosition7 = qAndA.answer;
                                allText += "User question in front of Elisenbrunnen:: \"" + qAndA.question +
                                           "\"\n Guide Response:\"" + qAndA.answer + "\"";
                                break;
                            case 7:
                                excelJsonConverter.firstQuestionElisenbrunnenWithPosition8 = qAndA.question;
                                excelJsonConverter.firstAnswerElisenbrunnenWithPosition8 = qAndA.answer;
                                allText += "User question in front of Elisenbrunnen:: \"" + qAndA.question +
                                           "\"\n Guide Response:\"" + qAndA.answer + "\"";
                                break;
                            case 8:
                                excelJsonConverter.firstQuestionElisenbrunnenWithPosition9 = qAndA.question;
                                excelJsonConverter.firstAnswerElisenbrunnenWithPosition9 = qAndA.answer;
                                allText += "User question in front of Elisenbrunnen:: \"" + qAndA.question +
                                           "\"\n Guide Response:\"" + qAndA.answer + "\"";
                                break;
                            case 9:
                                excelJsonConverter.firstQuestionElisenbrunnenWithPosition10 = qAndA.question;
                                excelJsonConverter.firstAnswerElisenbrunnenWithPosition10 = qAndA.answer;
                                allText += "User question in front of Elisenbrunnen:: \"" + qAndA.question +
                                           "\"\n Guide Response:\"" + qAndA.answer + "\"";
                                break;

                            default:
                                Debug.LogWarning("There are more than 10 questions in the Elisenbrunnen");
                                break;
                        }

                        i++;
                    }
                }

            Debug.Log(allText);
            excelJsonConverter.CheckTruthfulnessSuperCFirst = await OnChat(allText);

            excelJsonConverter.totalNumberOfCorrectInformation =
                CountAllTermsInCheckInformation(excelJsonConverter, "[Correct]");
            excelJsonConverter.totalNumberOfFluffInformation =
                CountAllTermsInCheckInformation(excelJsonConverter, "[Fluff]");
            excelJsonConverter.totalNumberOfMisleadingInformation =
                CountAllTermsInCheckInformation(excelJsonConverter, "[Misleading]");
            excelJsonConverter.totalNumberOfIncorrectInformation =
                CountAllTermsInCheckInformation(excelJsonConverter, "[Incorrect]");
            excelJsonConverter.totalNumberOfMissingContextInformation =
                CountAllTermsInCheckInformation(excelJsonConverter, "[Missing Context]");

            //excelJsonConverter.totalNumberOfPredefinedQuestions =
            //    CountAllTermsInAnswers(excelJsonConverter, "[Correct]");
            excelJsonConverter.totalNumberOfFluffInformationQuestions =
                CountAllTermsInAnswers(excelJsonConverter, "[Fluff]");
            excelJsonConverter.totalNumberOfMisleadingInformationQuestions =
                CountAllTermsInAnswers(excelJsonConverter, "[Misleading]");
            excelJsonConverter.totalNumberOfIncorrectInformationQuestions =
                CountAllTermsInAnswers(excelJsonConverter, "[Incorrect]");
            excelJsonConverter.totalNumberOfMissingContextInformationQuestions =
                CountAllTermsInAnswers(excelJsonConverter, "[Missing Context]");

            Debug.Log("Done 1.");
            excelJsonConverter.CheckTruthfulnessTownHallFirst =
                await Embedding.CompareEntireTextWithWikipediaSource(excelJsonConverter.CheckTruthfulnessSuperCFirst,
                    false);
            excelJsonConverter.CheckTruthfulnessCathedralFirst =
                await Embedding.CompareEntireTextWithWikipediaSource(excelJsonConverter.CheckTruthfulnessSuperCFirst);
            excelJsonConverter.totalNumberOfQuestions = totalNumberOfQuestions;
            var json = JsonConvert.SerializeObject(excelJsonConverter, Formatting.Indented);
            File.WriteAllText(outPutPath, json);
            Debug.Log("Done 2.");
        }

        public static int CountAllTermsInCheckInformation(ExcelJsonConverter excelJsonConverter,
            string searchForCommand)
        {
            var count = 0;
            // Check how often all truthfulness checks include the searchForCommand
            count += SafeSplitAndCount(excelJsonConverter.CheckTruthfulnessSuperCFirst, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.CheckTruthfulnessSuperCSecond, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.CheckTruthfulnessSuperCThird, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.CheckTruthfulnessSuperCFourth, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.CheckTruthfulnessTownHallFirst, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.CheckTruthfulnessTownHallSecond, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.CheckTruthfulnessTownHallThird, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.CheckTruthfulnessTownHallFourth, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.CheckTruthfulnessCathedralFirst, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.CheckTruthfulnessCathedralSecond, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.CheckTruthfulnessCathedralThird, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.CheckTruthfulnessCathedralFourth, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.CheckTruthfulnessElisenbrunnenFirst, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.CheckTruthfulnessElisenbrunnenSecond, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.CheckTruthfulnessElisenbrunnenThird, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.CheckTruthfulnessElisenbrunnenFourth, searchForCommand);
            return count;
        }

        public static int CountAllTermsInAnswers(ExcelJsonConverter excelJsonConverter,
            string searchForCommand)
        {
            var count = 0;
            // Check how often all truthfulness checks include the searchForCommand
            // Cathedral
            count += SafeSplitAndCount(excelJsonConverter.firstAnswerCathedralWithPosition1, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.firstAnswerCathedralWithPosition2, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.firstAnswerCathedralWithPosition3, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.firstAnswerCathedralWithPosition4, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.firstAnswerCathedralWithPosition5, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.firstAnswerCathedralWithPosition6, searchForCommand);

            // Elisenbrunnen
            count += SafeSplitAndCount(excelJsonConverter.firstAnswerElisenbrunnenWithPosition1, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.firstAnswerElisenbrunnenWithPosition2, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.firstAnswerElisenbrunnenWithPosition3, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.firstAnswerElisenbrunnenWithPosition4, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.firstAnswerElisenbrunnenWithPosition5, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.firstAnswerElisenbrunnenWithPosition6, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.firstAnswerElisenbrunnenWithPosition7, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.firstAnswerElisenbrunnenWithPosition8, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.firstAnswerElisenbrunnenWithPosition9, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.firstAnswerElisenbrunnenWithPosition10, searchForCommand);

            count += SafeSplitAndCount(excelJsonConverter.secondAnswerElisenbrunnenWithPosition1, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.secondAnswerElisenbrunnenWithPosition2, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.secondAnswerElisenbrunnenWithPosition3, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.secondAnswerElisenbrunnenWithPosition4, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.secondAnswerElisenbrunnenWithPosition5, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.secondAnswerElisenbrunnenWithPosition6, searchForCommand);

            count += SafeSplitAndCount(excelJsonConverter.thirdAnswerElisenbrunnenWithPosition1, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.thirdAnswerElisenbrunnenWithPosition2, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.thirdAnswerElisenbrunnenWithPosition3, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.thirdAnswerElisenbrunnenWithPosition4, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.thirdAnswerElisenbrunnenWithPosition5, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.thirdAnswerElisenbrunnenWithPosition6, searchForCommand);

            count += SafeSplitAndCount(excelJsonConverter.fourthAnswerElisenbrunnenWithPosition1, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.fourthAnswerElisenbrunnenWithPosition2, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.fourthAnswerElisenbrunnenWithPosition3, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.fourthAnswerElisenbrunnenWithPosition4, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.fourthAnswerElisenbrunnenWithPosition5, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.fourthAnswerElisenbrunnenWithPosition6, searchForCommand);

            // SuperC
            count += SafeSplitAndCount(excelJsonConverter.firstAnswerSuperCWithPosition1, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.firstAnswerSuperCWithPosition2, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.firstAnswerSuperCWithPosition3, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.firstAnswerSuperCWithPosition4, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.firstAnswerSuperCWithPosition5, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.firstAnswerSuperCWithPosition6, searchForCommand);

            count += SafeSplitAndCount(excelJsonConverter.secondAnswerSuperCWithPosition1, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.secondAnswerSuperCWithPosition2, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.secondAnswerSuperCWithPosition3, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.secondAnswerSuperCWithPosition4, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.secondAnswerSuperCWithPosition5, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.secondAnswerSuperCWithPosition6, searchForCommand);

            count += SafeSplitAndCount(excelJsonConverter.thirdAnswerSuperCWithPosition1, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.thirdAnswerSuperCWithPosition2, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.thirdAnswerSuperCWithPosition3, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.thirdAnswerSuperCWithPosition4, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.thirdAnswerSuperCWithPosition5, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.thirdAnswerSuperCWithPosition6, searchForCommand);

            count += SafeSplitAndCount(excelJsonConverter.fourthAnswerSuperCWithPosition1, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.fourthAnswerSuperCWithPosition2, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.fourthAnswerSuperCWithPosition3, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.fourthAnswerSuperCWithPosition4, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.fourthAnswerSuperCWithPosition5, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.fourthAnswerSuperCWithPosition6, searchForCommand);

            // TownHall
            count += SafeSplitAndCount(excelJsonConverter.firstAnswerTownHallWithPosition1, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.firstAnswerTownHallWithPosition2, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.firstAnswerTownHallWithPosition3, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.firstAnswerTownHallWithPosition4, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.firstAnswerTownHallWithPosition5, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.firstAnswerTownHallWithPosition6, searchForCommand);

            count += SafeSplitAndCount(excelJsonConverter.secondAnswerTownHallWithPosition1, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.secondAnswerTownHallWithPosition2, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.secondAnswerTownHallWithPosition3, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.secondAnswerTownHallWithPosition4, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.secondAnswerTownHallWithPosition5, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.secondAnswerTownHallWithPosition6, searchForCommand);

            count += SafeSplitAndCount(excelJsonConverter.thirdAnswerTownHallWithPosition1, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.thirdAnswerTownHallWithPosition2, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.thirdAnswerTownHallWithPosition3, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.thirdAnswerTownHallWithPosition4, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.thirdAnswerTownHallWithPosition5, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.thirdAnswerTownHallWithPosition6, searchForCommand);

            count += SafeSplitAndCount(excelJsonConverter.fourthAnswerTownHallWithPosition1, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.fourthAnswerTownHallWithPosition2, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.fourthAnswerTownHallWithPosition3, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.fourthAnswerTownHallWithPosition4, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.fourthAnswerTownHallWithPosition5, searchForCommand);
            count += SafeSplitAndCount(excelJsonConverter.fourthAnswerTownHallWithPosition6, searchForCommand);

            return count;
        }

        private static int SafeSplitAndCount(string input, string search)
        {
            if (string.IsNullOrEmpty(input)) return 0;
            return input.Split(new[] { search }, StringSplitOptions.None).Length - 1;
        }

        private static async Task<string> CheckAnswer(string question, string answer)
        {
            if (answer == "..." || question == "...") return "Skipping question because it is empty.";
            return await OnChat("User question: \"" + question + "\"\n Guide Response:\"" + answer + "\"");
        }


        public class ExcelJsonConverter
        {
            [JsonProperty(Order = 123)] public string CheckTruthfulnessCathedralFirst; // Unique name

            [JsonProperty(Order = 168)] public string CheckTruthfulnessCathedralFourth; // Unique name

            [JsonProperty(Order = 138)] public string CheckTruthfulnessCathedralSecond; // Unique name

            [JsonProperty(Order = 153)] public string CheckTruthfulnessCathedralThird; // Unique name

            [JsonProperty(Order = 183)] public string CheckTruthfulnessElisenbrunnenFirst; // Unique name

            [JsonProperty(Order = 228)] public string CheckTruthfulnessElisenbrunnenFourth; // Unique name

            [JsonProperty(Order = 198)] public string CheckTruthfulnessElisenbrunnenSecond; // Unique name

            [JsonProperty(Order = 213)] public string CheckTruthfulnessElisenbrunnenThird; // Unique name

            [JsonProperty(Order = 3)] public string CheckTruthfulnessSuperCFirst; // Unique name

            [JsonProperty(Order = 48)] public string CheckTruthfulnessSuperCFourth; // Unique name

            [JsonProperty(Order = 18)] public string CheckTruthfulnessSuperCSecond; // Unique name

            [JsonProperty(Order = 33)] public string CheckTruthfulnessSuperCThird; // Unique name

            [JsonProperty(Order = 63)] public string CheckTruthfulnessTownHallFirst; // Unique name

            [JsonProperty(Order = 108)] public string CheckTruthfulnessTownHallFourth; // Unique name

            [JsonProperty(Order = 78)] public string CheckTruthfulnessTownHallSecond; // Unique name

            [JsonProperty(Order = 93)] public string CheckTruthfulnessTownHallThird; // Unique name

            [JsonProperty(Order = 125)] public string firstAnswerCathedralWithPosition1;

            [JsonProperty(Order = 127)] public string firstAnswerCathedralWithPosition2;

            [JsonProperty(Order = 129)] public string firstAnswerCathedralWithPosition3;

            [JsonProperty(Order = 131)] public string firstAnswerCathedralWithPosition4;

            [JsonProperty(Order = 133)] public string firstAnswerCathedralWithPosition5;

            [JsonProperty(Order = 135)] public string firstAnswerCathedralWithPosition6;

            [JsonProperty(Order = 185)] public string firstAnswerElisenbrunnenWithPosition1;

            [JsonProperty(Order = 195)] public string firstAnswerElisenbrunnenWithPosition10;

            [JsonProperty(Order = 187)] public string firstAnswerElisenbrunnenWithPosition2;

            [JsonProperty(Order = 189)] public string firstAnswerElisenbrunnenWithPosition3;

            [JsonProperty(Order = 191)] public string firstAnswerElisenbrunnenWithPosition4;

            [JsonProperty(Order = 193)] public string firstAnswerElisenbrunnenWithPosition5;

            [JsonProperty(Order = 195)] public string firstAnswerElisenbrunnenWithPosition6;

            [JsonProperty(Order = 195)] public string firstAnswerElisenbrunnenWithPosition7;

            [JsonProperty(Order = 195)] public string firstAnswerElisenbrunnenWithPosition8;

            [JsonProperty(Order = 195)] public string firstAnswerElisenbrunnenWithPosition9;

            [JsonProperty(Order = 5)] public string firstAnswerSuperCWithPosition1;

            [JsonProperty(Order = 7)] public string firstAnswerSuperCWithPosition2;

            [JsonProperty(Order = 9)] public string firstAnswerSuperCWithPosition3;

            [JsonProperty(Order = 11)] public string firstAnswerSuperCWithPosition4;


            [JsonProperty(Order = 13)] public string firstAnswerSuperCWithPosition5;


            [JsonProperty(Order = 15)] public string firstAnswerSuperCWithPosition6;


            [JsonProperty(Order = 65)] public string firstAnswerTownHallWithPosition1;


            [JsonProperty(Order = 67)] public string firstAnswerTownHallWithPosition2;


            [JsonProperty(Order = 69)] public string firstAnswerTownHallWithPosition3;


            [JsonProperty(Order = 71)] public string firstAnswerTownHallWithPosition4;


            [JsonProperty(Order = 73)] public string firstAnswerTownHallWithPosition5;


            [JsonProperty(Order = 75)] public string firstAnswerTownHallWithPosition6;


            [JsonProperty(Order = 121)] public string firstInfoCathedral;

            [JsonProperty(Order = 122)] public string firstInfoCathedralActions;

            [JsonProperty(Order = 181)] public string firstInfoElisenbrunnen;

            [JsonProperty(Order = 182)] public string firstInfoElisenbrunnenActions;

            [JsonProperty(Order = 1)] public string firstInfoSuperC;

            [JsonProperty(Order = 2)] public string firstInfoSuperCActions;

            [JsonProperty(Order = 61)] public string firstInfoTownHall;

            [JsonProperty(Order = 62)] public string firstInfoTownHallActions;

            [JsonProperty(Order = 124)] public string firstQuestionCathedralWithPosition1;

            [JsonProperty(Order = 126)] public string firstQuestionCathedralWithPosition2;

            [JsonProperty(Order = 128)] public string firstQuestionCathedralWithPosition3;

            [JsonProperty(Order = 130)] public string firstQuestionCathedralWithPosition4;

            [JsonProperty(Order = 132)] public string firstQuestionCathedralWithPosition5;

            [JsonProperty(Order = 134)] public string firstQuestionCathedralWithPosition6;

            [JsonProperty(Order = 184)] public string firstQuestionElisenbrunnenWithPosition1;

            [JsonProperty(Order = 194)] public string firstQuestionElisenbrunnenWithPosition10;

            [JsonProperty(Order = 186)] public string firstQuestionElisenbrunnenWithPosition2;

            [JsonProperty(Order = 188)] public string firstQuestionElisenbrunnenWithPosition3;

            [JsonProperty(Order = 190)] public string firstQuestionElisenbrunnenWithPosition4;

            [JsonProperty(Order = 192)] public string firstQuestionElisenbrunnenWithPosition5;

            [JsonProperty(Order = 194)] public string firstQuestionElisenbrunnenWithPosition6;

            [JsonProperty(Order = 194)] public string firstQuestionElisenbrunnenWithPosition7;

            [JsonProperty(Order = 194)] public string firstQuestionElisenbrunnenWithPosition8;

            [JsonProperty(Order = 194)] public string firstQuestionElisenbrunnenWithPosition9;

            [JsonProperty(Order = 4)] public string firstQuestionSuperCWithPosition1;

            [JsonProperty(Order = 6)] public string firstQuestionSuperCWithPosition2;

            [JsonProperty(Order = 8)] public string firstQuestionSuperCWithPosition3;

            [JsonProperty(Order = 10)] public string firstQuestionSuperCWithPosition4;

            [JsonProperty(Order = 12)] public string firstQuestionSuperCWithPosition5;

            [JsonProperty(Order = 14)] public string firstQuestionSuperCWithPosition6;

            [JsonProperty(Order = 64)] public string firstQuestionTownHallWithPosition1;

            [JsonProperty(Order = 66)] public string firstQuestionTownHallWithPosition2;

            [JsonProperty(Order = 68)] public string firstQuestionTownHallWithPosition3;

            [JsonProperty(Order = 70)] public string firstQuestionTownHallWithPosition4;

            [JsonProperty(Order = 72)] public string firstQuestionTownHallWithPosition5;

            [JsonProperty(Order = 74)] public string firstQuestionTownHallWithPosition6;

            [JsonProperty(Order = 170)] public string fourthAnswerCathedralWithPosition1;


            [JsonProperty(Order = 172)] public string fourthAnswerCathedralWithPosition2;


            [JsonProperty(Order = 174)] public string fourthAnswerCathedralWithPosition3;


            [JsonProperty(Order = 176)] public string fourthAnswerCathedralWithPosition4;

            [JsonProperty(Order = 178)] public string fourthAnswerCathedralWithPosition5;

            [JsonProperty(Order = 180)] public string fourthAnswerCathedralWithPosition6;

            [JsonProperty(Order = 230)] public string fourthAnswerElisenbrunnenWithPosition1;

            [JsonProperty(Order = 232)] public string fourthAnswerElisenbrunnenWithPosition2;

            [JsonProperty(Order = 234)] public string fourthAnswerElisenbrunnenWithPosition3;

            [JsonProperty(Order = 236)] public string fourthAnswerElisenbrunnenWithPosition4;

            [JsonProperty(Order = 238)] public string fourthAnswerElisenbrunnenWithPosition5;

            [JsonProperty(Order = 240)] public string fourthAnswerElisenbrunnenWithPosition6;

            [JsonProperty(Order = 50)] public string fourthAnswerSuperCWithPosition1;

            [JsonProperty(Order = 52)] public string fourthAnswerSuperCWithPosition2;

            [JsonProperty(Order = 54)] public string fourthAnswerSuperCWithPosition3;

            [JsonProperty(Order = 56)] public string fourthAnswerSuperCWithPosition4;

            [JsonProperty(Order = 58)] public string fourthAnswerSuperCWithPosition5;

            [JsonProperty(Order = 60)] public string fourthAnswerSuperCWithPosition6;

            [JsonProperty(Order = 110)] public string fourthAnswerTownHallWithPosition1;

            [JsonProperty(Order = 112)] public string fourthAnswerTownHallWithPosition2;

            [JsonProperty(Order = 114)] public string fourthAnswerTownHallWithPosition3;

            [JsonProperty(Order = 116)] public string fourthAnswerTownHallWithPosition4;

            [JsonProperty(Order = 118)] public string fourthAnswerTownHallWithPosition5;

            [JsonProperty(Order = 120)] public string fourthAnswerTownHallWithPosition6;

            [JsonProperty(Order = 166)] public string fourthInfoCathedral;

            [JsonProperty(Order = 167)] public string fourthInfoCathedralActions;

            [JsonProperty(Order = 226)] public string fourthInfoElisenbrunnen;

            [JsonProperty(Order = 227)] public string fourthInfoElisenbrunnenActions;

            [JsonProperty(Order = 46)] public string fourthInfoSuperC;

            [JsonProperty(Order = 47)] public string fourthInfoSuperCActions;

            [JsonProperty(Order = 106)] public string fourthInfoTownHall;

            [JsonProperty(Order = 107)] public string fourthInfoTownHallActions;

            [JsonProperty(Order = 169)] public string fourthQuestionCathedralWithPosition1;

            [JsonProperty(Order = 171)] public string fourthQuestionCathedralWithPosition2;

            [JsonProperty(Order = 173)] public string fourthQuestionCathedralWithPosition3;

            [JsonProperty(Order = 175)] public string fourthQuestionCathedralWithPosition4;

            [JsonProperty(Order = 177)] public string fourthQuestionCathedralWithPosition5;

            [JsonProperty(Order = 179)] public string fourthQuestionCathedralWithPosition6;

            [JsonProperty(Order = 229)] public string fourthQuestionElisenbrunnenWithPosition1;

            [JsonProperty(Order = 231)] public string fourthQuestionElisenbrunnenWithPosition2;

            [JsonProperty(Order = 233)] public string fourthQuestionElisenbrunnenWithPosition3;

            [JsonProperty(Order = 235)] public string fourthQuestionElisenbrunnenWithPosition4;

            [JsonProperty(Order = 237)] public string fourthQuestionElisenbrunnenWithPosition5;

            [JsonProperty(Order = 239)] public string fourthQuestionElisenbrunnenWithPosition6;

            [JsonProperty(Order = 49)] public string fourthQuestionSuperCWithPosition1;

            [JsonProperty(Order = 51)] public string fourthQuestionSuperCWithPosition2;

            [JsonProperty(Order = 53)] public string fourthQuestionSuperCWithPosition3;

            [JsonProperty(Order = 55)] public string fourthQuestionSuperCWithPosition4;

            [JsonProperty(Order = 57)] public string fourthQuestionSuperCWithPosition5;

            [JsonProperty(Order = 59)] public string fourthQuestionSuperCWithPosition6;

            [JsonProperty(Order = 109)] public string fourthQuestionTownHallWithPosition1;

            [JsonProperty(Order = 111)] public string fourthQuestionTownHallWithPosition2;

            [JsonProperty(Order = 113)] public string fourthQuestionTownHallWithPosition3;

            [JsonProperty(Order = 115)] public string fourthQuestionTownHallWithPosition4;

            [JsonProperty(Order = 117)] public string fourthQuestionTownHallWithPosition5;

            [JsonProperty(Order = 119)] public string fourthQuestionTownHallWithPosition6;

            [JsonProperty(Order = 140)] public string secondAnswerCathedralWithPosition1;

            [JsonProperty(Order = 142)] public string secondAnswerCathedralWithPosition2;

            [JsonProperty(Order = 144)] public string secondAnswerCathedralWithPosition3;

            [JsonProperty(Order = 146)] public string secondAnswerCathedralWithPosition4;

            [JsonProperty(Order = 148)] public string secondAnswerCathedralWithPosition5;

            [JsonProperty(Order = 150)] public string secondAnswerCathedralWithPosition6;

            [JsonProperty(Order = 200)] public string secondAnswerElisenbrunnenWithPosition1;

            [JsonProperty(Order = 202)] public string secondAnswerElisenbrunnenWithPosition2;

            [JsonProperty(Order = 204)] public string secondAnswerElisenbrunnenWithPosition3;

            [JsonProperty(Order = 206)] public string secondAnswerElisenbrunnenWithPosition4;

            [JsonProperty(Order = 208)] public string secondAnswerElisenbrunnenWithPosition5;

            [JsonProperty(Order = 210)] public string secondAnswerElisenbrunnenWithPosition6;

            [JsonProperty(Order = 20)] public string secondAnswerSuperCWithPosition1;

            [JsonProperty(Order = 22)] public string secondAnswerSuperCWithPosition2;

            [JsonProperty(Order = 24)] public string secondAnswerSuperCWithPosition3;

            [JsonProperty(Order = 26)] public string secondAnswerSuperCWithPosition4;

            [JsonProperty(Order = 28)] public string secondAnswerSuperCWithPosition5;

            [JsonProperty(Order = 30)] public string secondAnswerSuperCWithPosition6;

            [JsonProperty(Order = 80)] public string secondAnswerTownHallWithPosition1;

            [JsonProperty(Order = 82)] public string secondAnswerTownHallWithPosition2;

            [JsonProperty(Order = 84)] public string secondAnswerTownHallWithPosition3;

            [JsonProperty(Order = 86)] public string secondAnswerTownHallWithPosition4;

            [JsonProperty(Order = 88)] public string secondAnswerTownHallWithPosition5;

            [JsonProperty(Order = 90)] public string secondAnswerTownHallWithPosition6;

            [JsonProperty(Order = 136)] public string secondInfoCathedral;

            [JsonProperty(Order = 137)] public string secondInfoCathedralActions;

            [JsonProperty(Order = 196)] public string secondInfoElisenbrunnen;

            [JsonProperty(Order = 197)] public string secondInfoElisenbrunnenActions;

            [JsonProperty(Order = 16)] public string secondInfoSuperC;

            [JsonProperty(Order = 17)] public string secondInfoSuperCActions;

            [JsonProperty(Order = 76)] public string secondInfoTownHall;

            [JsonProperty(Order = 77)] public string secondInfoTownHallActions;

            [JsonProperty(Order = 139)] public string secondQuestionCathedralWithPosition1;

            [JsonProperty(Order = 141)] public string secondQuestionCathedralWithPosition2;

            [JsonProperty(Order = 143)] public string secondQuestionCathedralWithPosition3;

            [JsonProperty(Order = 145)] public string secondQuestionCathedralWithPosition4;

            [JsonProperty(Order = 147)] public string secondQuestionCathedralWithPosition5;

            [JsonProperty(Order = 149)] public string secondQuestionCathedralWithPosition6;

            [JsonProperty(Order = 199)] public string secondQuestionElisenbrunnenWithPosition1;

            [JsonProperty(Order = 201)] public string secondQuestionElisenbrunnenWithPosition2;

            [JsonProperty(Order = 203)] public string secondQuestionElisenbrunnenWithPosition3;

            [JsonProperty(Order = 205)] public string secondQuestionElisenbrunnenWithPosition4;

            [JsonProperty(Order = 207)] public string secondQuestionElisenbrunnenWithPosition5;

            [JsonProperty(Order = 209)] public string secondQuestionElisenbrunnenWithPosition6;

            [JsonProperty(Order = 19)] public string secondQuestionSuperCWithPosition1;

            [JsonProperty(Order = 21)] public string secondQuestionSuperCWithPosition2;

            [JsonProperty(Order = 23)] public string secondQuestionSuperCWithPosition3;

            [JsonProperty(Order = 25)] public string secondQuestionSuperCWithPosition4;

            [JsonProperty(Order = 27)] public string secondQuestionSuperCWithPosition5;

            [JsonProperty(Order = 29)] public string secondQuestionSuperCWithPosition6;

            [JsonProperty(Order = 79)] public string secondQuestionTownHallWithPosition1;

            [JsonProperty(Order = 81)] public string secondQuestionTownHallWithPosition2;

            [JsonProperty(Order = 83)] public string secondQuestionTownHallWithPosition3;

            [JsonProperty(Order = 85)] public string secondQuestionTownHallWithPosition4;

            [JsonProperty(Order = 87)] public string secondQuestionTownHallWithPosition5;

            [JsonProperty(Order = 89)] public string secondQuestionTownHallWithPosition6;

            [JsonProperty(Order = 155)] public string thirdAnswerCathedralWithPosition1;

            [JsonProperty(Order = 157)] public string thirdAnswerCathedralWithPosition2;

            [JsonProperty(Order = 159)] public string thirdAnswerCathedralWithPosition3;

            [JsonProperty(Order = 161)] public string thirdAnswerCathedralWithPosition4;

            [JsonProperty(Order = 163)] public string thirdAnswerCathedralWithPosition5;

            [JsonProperty(Order = 165)] public string thirdAnswerCathedralWithPosition6;

            [JsonProperty(Order = 215)] public string thirdAnswerElisenbrunnenWithPosition1;

            [JsonProperty(Order = 217)] public string thirdAnswerElisenbrunnenWithPosition2;

            [JsonProperty(Order = 219)] public string thirdAnswerElisenbrunnenWithPosition3;

            [JsonProperty(Order = 221)] public string thirdAnswerElisenbrunnenWithPosition4;

            [JsonProperty(Order = 223)] public string thirdAnswerElisenbrunnenWithPosition5;

            [JsonProperty(Order = 225)] public string thirdAnswerElisenbrunnenWithPosition6;

            [JsonProperty(Order = 35)] public string thirdAnswerSuperCWithPosition1;

            [JsonProperty(Order = 37)] public string thirdAnswerSuperCWithPosition2;

            [JsonProperty(Order = 39)] public string thirdAnswerSuperCWithPosition3;

            [JsonProperty(Order = 41)] public string thirdAnswerSuperCWithPosition4;

            [JsonProperty(Order = 43)] public string thirdAnswerSuperCWithPosition5;

            [JsonProperty(Order = 45)] public string thirdAnswerSuperCWithPosition6;

            [JsonProperty(Order = 95)] public string thirdAnswerTownHallWithPosition1;

            [JsonProperty(Order = 97)] public string thirdAnswerTownHallWithPosition2;

            [JsonProperty(Order = 99)] public string thirdAnswerTownHallWithPosition3;

            [JsonProperty(Order = 101)] public string thirdAnswerTownHallWithPosition4;

            [JsonProperty(Order = 103)] public string thirdAnswerTownHallWithPosition5;

            [JsonProperty(Order = 105)] public string thirdAnswerTownHallWithPosition6;

            [JsonProperty(Order = 151)] public string thirdInfoCathedral;

            [JsonProperty(Order = 152)] public string thirdInfoCathedralActions;

            [JsonProperty(Order = 211)] public string thirdInfoElisenbrunnen;

            [JsonProperty(Order = 212)] public string thirdInfoElisenbrunnenActions;

            [JsonProperty(Order = 31)] public string thirdInfoSuperC;

            [JsonProperty(Order = 32)] public string thirdInfoSuperCActions;

            [JsonProperty(Order = 91)] public string thirdInfoTownHall;

            [JsonProperty(Order = 92)] public string thirdInfoTownHallActions;

            [JsonProperty(Order = 154)] public string thirdQuestionCathedralWithPosition1;

            [JsonProperty(Order = 156)] public string thirdQuestionCathedralWithPosition2;

            [JsonProperty(Order = 158)] public string thirdQuestionCathedralWithPosition3;

            [JsonProperty(Order = 160)] public string thirdQuestionCathedralWithPosition4;

            [JsonProperty(Order = 162)] public string thirdQuestionCathedralWithPosition5;

            [JsonProperty(Order = 164)] public string thirdQuestionCathedralWithPosition6;

            [JsonProperty(Order = 214)] public string thirdQuestionElisenbrunnenWithPosition1;

            [JsonProperty(Order = 216)] public string thirdQuestionElisenbrunnenWithPosition2;

            [JsonProperty(Order = 218)] public string thirdQuestionElisenbrunnenWithPosition3;

            [JsonProperty(Order = 220)] public string thirdQuestionElisenbrunnenWithPosition4;

            [JsonProperty(Order = 222)] public string thirdQuestionElisenbrunnenWithPosition5;

            [JsonProperty(Order = 224)] public string thirdQuestionElisenbrunnenWithPosition6;

            [JsonProperty(Order = 34)] public string thirdQuestionSuperCWithPosition1;

            [JsonProperty(Order = 36)] public string thirdQuestionSuperCWithPosition2;

            [JsonProperty(Order = 38)] public string thirdQuestionSuperCWithPosition3;

            [JsonProperty(Order = 40)] public string thirdQuestionSuperCWithPosition4;

            [JsonProperty(Order = 42)] public string thirdQuestionSuperCWithPosition5;

            [JsonProperty(Order = 44)] public string thirdQuestionSuperCWithPosition6;

            [JsonProperty(Order = 94)] public string thirdQuestionTownHallWithPosition1;

            [JsonProperty(Order = 96)] public string thirdQuestionTownHallWithPosition2;

            [JsonProperty(Order = 98)] public string thirdQuestionTownHallWithPosition3;

            [JsonProperty(Order = 100)] public string thirdQuestionTownHallWithPosition4;

            [JsonProperty(Order = 102)] public string thirdQuestionTownHallWithPosition5;

            [JsonProperty(Order = 104)] public string thirdQuestionTownHallWithPosition6;

            [JsonProperty(Order = 242)] public int totalNumberOfCorrectInformation;
            [JsonProperty(Order = 243)] public int totalNumberOfFluffInformation;
            [JsonProperty(Order = 250)] public int totalNumberOfFluffInformationQuestions;
            [JsonProperty(Order = 244)] public int totalNumberOfIncorrectInformation;
            [JsonProperty(Order = 251)] public int totalNumberOfIncorrectInformationQuestions;
            [JsonProperty(Order = 245)] public int totalNumberOfMisleadingInformation;
            [JsonProperty(Order = 252)] public int totalNumberOfMisleadingInformationQuestions;
            [JsonProperty(Order = 247)] public int totalNumberOfMissingContextInformation;
            [JsonProperty(Order = 253)] public int totalNumberOfMissingContextInformationQuestions;
            [JsonProperty(Order = 249)] public int totalNumberOfPredefinedQuestions;

            [JsonProperty(Order = 248)] public int totalNumberOfQuestions;
        }
    }
}

#endif