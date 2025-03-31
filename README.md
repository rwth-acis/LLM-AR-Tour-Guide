# ARchen Guide: Personal Tours

![Screenshots](https://github.com/user-attachments/assets/226d9f8e-caa9-486b-8300-d9b4d1dd2283)

This repository contains a Unity Project that was used to build a Tour Guide App that utelizes Augmented Reality and Largle Language Models together to offer an engaging and personalized tour experience.

Currently, the project is incompletle due to file size limitations, which do not allow me to upload the entire UI. In the mean time I want to point out a few interesting scripts, mainly the LLM ones with prompts.

## Prompts

### Tour Generation Prompts:
The generateTourForPointOfInteresting() method generates the tour content here:
https://github.com/rwth-acis/LLM-AR-Tour-Guide/blob/main/Assets/Scripts/TourGeneration/TourGenerator.cs

### Guide Memory Prompt:
https://github.com/rwth-acis/LLM-AR-Tour-Guide/blob/main/Assets/Scripts/TourGeneration/TourSummery.cs

### Guide Prompts
The prompts that include the personality description of the guides can be found at the top of this file:
https://github.com/rwth-acis/LLM-AR-Tour-Guide/blob/main/Assets/Scripts/Guide%20Scripts/GuideManager.cs

### Onboarding
The prompts used during the inital onboarding conversation can be found in thid file:
https://github.com/rwth-acis/LLM-AR-Tour-Guide/blob/main/Assets/Scripts/OnboardingManager.cs

### The main script of the project, that also includes the prompoting for questions can be found here:
https://github.com/rwth-acis/LLM-AR-Tour-Guide/blob/main/Assets/Scripts/InformationController.cs
