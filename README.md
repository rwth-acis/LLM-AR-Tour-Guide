# ARchen Guide: Personal Tours

![Screenshots](https://github.com/user-attachments/assets/226d9f8e-caa9-486b-8300-d9b4d1dd2283)

This repository contains a Unity Project that was used to build a Tour Guide App that utelizes Augmented Reality and Largle Language Models together to offer an engaging and personalized tour experience.


## Prompt Engineering 
I want to point out a few interesting scripts regarding the prompting I employed.

### Tour Generation Prompts
The generateTourForPointOfInteresting() method generates the tour content here:
[TourGenerator.cs](https://github.com/rwth-acis/LLM-AR-Tour-Guide/blob/main/Assets/Scripts/TourGeneration/TourGenerator.cs)

### Guide Memory Prompt
[](https://github.com/rwth-acis/LLM-AR-Tour-Guide/blob/main/Assets/Scripts/TourGeneration/TourSummery.cs)

### Guide Prompts
The prompts that include the personality description of the guides can be found at the top of this file, specifically the long_description attributes:
[GuideManager.cs](https://github.com/rwth-acis/LLM-AR-Tour-Guide/blob/main/Assets/Scripts/Guide%20Scripts/GuideManager.cs)

### Onboarding
The prompts used during the initial onboarding conversation can be found in this file:
[OnboardingManager.cs](https://github.com/rwth-acis/LLM-AR-Tour-Guide/blob/main/Assets/Scripts/OnboardingManager.cs)

### Question Answer Prompt / Main Script
The prompt used when questions are answered can be found in this script, more specifically in the Method OnAskQuestionAsync(). Apart from that the InformationController is also the script that handles presenting the tour content and answers to the user.
[InformationController.cs](https://github.com/rwth-acis/LLM-AR-Tour-Guide/blob/main/Assets/Scripts/InformationController.cs)
