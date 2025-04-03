# ARchen Guide: Personal Tours

![Screenshots](https://github.com/user-attachments/assets/226d9f8e-caa9-486b-8300-d9b4d1dd2283)

This repository contains a Unity 6 project that was used to build a tour guide app that utelizes Augmented Reality and Largle Language Models together to offer an engaging and personalized tour experience.

---

## Features
* **Personalized Tours:** Tour content is tailored based on user interests gathered via an initial questionnaire and an interactive onboarding chat with the LLM guide. Gathered interests are updated continuously during the tour based on user questions.
* **AR Tour Guide:** A [Mixed Reality Agent](https://github.com/rwth-acis/Virtual-Agents-Framework) is visualized in the user's real-world environment using AR. Users can choose from different guide characters.
* **Interactive Agent:** The guide provides explanations, answers user questions, and performs animations like pointing at points of interest (POI), gesturing, or walking the user to the next POI.
* **LLM-Powered Content:** Tour narratives and answers are generated in real-time by Google's [Gemini LLM](https://deepmind.google/technologies/gemini/), using Wikipedia content and Google Search grounding (RAG) to enhance accuracy and relevance. Guide animations are also choosen dynamically by the LLM.
* **AR Navigation & POIs:** AR elements mark nearby main POIs and optional sub-POIs. An AR arrow assists with navigation towards the next POI or locating the guide.
* **Text-to-Speech & Speech-to-Text:** Natural interaction is enabled through synthesized [voice output](https://cloud.google.com/text-to-speech) for the guide and [voice input](https://github.com/yasirkula/UnitySpeechToText) for user questions.

## Manual
For detailed instructions on project setup and tour configuration, please see to **[manual](https://github.com/rwth-acis/LLM-AR-Tour-Guide/blob/main/manual.md)**.

---


##  Prompt Engineering Highlights
This section points out key scripts and methods involved in constructing the prompts used to interact with the LLM:

* **Tour Generation Prompts:**
The ``generateTourForPointOfInteresting()`` method generates the tour content here:
[TourGenerator.cs](https://github.com/rwth-acis/LLM-AR-Tour-Guide/blob/main/Assets/Scripts/TourGeneration/TourGenerator.cs)

* **Guide Memory Prompt:**
The ``UpdateSummery()`` method of the ``UserInfromation.cs`` script handles the internal memory the guide keeps about the user:
[UserInformation.cs](https://github.com/rwth-acis/LLM-AR-Tour-Guide/blob/main/Assets/Scripts/UserInformation.cs)

* **Guide Prompts:**
The prompts that include the personality description of the guides can be found at the top of this file, specifically the ``longDescription`` attributes:
[GuideManager.cs](https://github.com/rwth-acis/LLM-AR-Tour-Guide/blob/main/Assets/Scripts/Guide%20Scripts/GuideManager.cs)

* **Onboarding:**
The prompts used during the initial onboarding conversation can be found in this file:
[OnboardingManager.cs](https://github.com/rwth-acis/LLM-AR-Tour-Guide/blob/main/Assets/Scripts/OnboardingManager.cs)

* **Question Answer Prompt / Main Script:**
The prompt used when questions are answered can be found in this script, more specifically in the Method ``OnAskQuestionAsync()``. Apart from that the ``InformationController.cs`` script is also the script that handles presenting the tour content and answers to the user.
[InformationController.cs](https://github.com/rwth-acis/LLM-AR-Tour-Guide/blob/main/Assets/Scripts/InformationController.cs)
