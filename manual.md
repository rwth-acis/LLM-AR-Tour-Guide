## Manual 
Find out how to set up the project and how to configure your own tours.

---

### Initial Project Setup

1.  **Clone the Repository:**

2.  **Open in Unity:** Open the cloned project folder using **Unity Hub** with **Unity Editor version 6000.0.39f1** (or newer).

3.  **Automatic Upgrades:** If prompted by Unity upon opening, allow any recommended fixes or project upgrades. Close any other pop-up windows. e.g. "New Scoops have been added" or "Ready Player Me"-windows.

4.  **Set Build Target:** Switch the build platform to **Android**:
    * Go to `File > Build Profiles`
    * Select `Android` from the Platform list.
    * Click `Switch Platform`.

5.  **Open Main Scene:** In the Project window, navigate to `Assets/Scenes/` and open the `AppScene.unity` scene.

6.  **Initial Test (Editor):** Enter **Play Mode** (Ctrl+P or Cmd+P). You should be able to interact with the initial onboarding dialogs until a chat window appears. The chat window does not work until the API keys are defined but the onboarding can be skipped to open the AR experience in a test room. *(Note: Full functionality requires API keys and an Android device)*.

7.  **Reset Player Preferences (optional):** If you have used this project before, reset PlayerPrefs to avoid potential issues:
    * Click `Debug > Reset PlayerPrefs` in the Unity menu.
### API Key Configuration

The app requires API keys from Google Cloud Platform to enable its core features (LLM, AR, Text-to-Speech).

1.  **Google Cloud Project:**
    * If you don't have one, create a project in the [Google Cloud Console](https://console.cloud.google.com/).
    * Enable the following APIs within your project:
        * **Gemini API:** Search for "Gemini API" in the marketplace and enable it. This is **required** for the LLM. Ensure you only activate the free tier. ([Link](https://console.cloud.google.com/marketplace/product/google/generativelanguage.googleapis.com))
        * **ARCore API:** Search for "ARCore API" and enable it. This is **required** for AR functionality. Usage is typically free. ([Link](https://console.cloud.google.com/marketplace/product/google/arcore.googleapis.com))
        * **Text-to-Speech API:** Search for "Cloud Text-to-Speech API" and enable it. This is **optional** but recommended for voice output. The app uses CHIRP-HD voices and the app generates around ~15000 characters of text per tour. Check potential costs, the free tier should cover ~1 million characters/month, but costs might change. ([Link](https://console.cloud.google.com/marketplace/product/google/texttospeech.googleapis.com))

2.  **Create API Keys:**
    * In the Google Cloud Console, navigate to `APIs & Services > Credentials`.
    * Either create three separate API keys or one API key for all three APIs activated beforehand.

3.  **Add Keys to Unity Project:**
    * **Gemini & Text-to-Speech Keys:**
        * In the Unity Project window, navigate to `Assets/Configurations/`.
        * Select the `ApiKeys` ScriptableObject asset.
        * In the Inspector window, paste your API key for the **Gemini API** into the `Gemini Api Key` field.
        * Paste your API key for the **Text-to-Speech API** into the `Text To Speech Api Key` field (leave blank if not using TTS).
    * **ARCore Key:**
        * Go to `Edit > Project Settings...` in the Unity menu.
        * Select `XR Plug-in Management > ARCore Extensions` from the left-hand menu.
        * Paste your API key into the `Android API Key` field.

4.  **Functionality Check:** With API keys configured, the app should be fully functional when built and deployed to a compatible Android device.

5.  **Note on Editor Errors:** When running in **Play Mode** within the Unity Editor, you will likely encounter console errors related to missing ARCore functionality. This is **expected behavior** as ARCore features require a physical Android device. You can use the `Collapse` option in the Console window to group repetitive errors.

---

### Configure Tour Points of Interest (POIs)

Customize the tour route and content by modifying the POI data.

1.  **Locate POI Data:** In the Unity Project window, navigate to `Assets/Configurations/`.
2.  **Select Asset:** Select the `PointOfInterestData` ScriptableObject asset.
3.  **Edit POIs:** In the Inspector window, find and expand the `Points Of Interest` array.
    * You can change the number of POIs using the `Size` field.
    * Expand existing elements to modify them, or fill in details for new elements.
4.  **POI Fields:** Each element in the array represents a POI and requires the following information:
    * **Title:** The primary name of the POI. **Crucially, this should ideally match the exact title of an English or German Wikipedia page** for the LLM to retrieve relevant information.
    * **Subtitle:** A short descriptive text displayed under the title in the itinerary menu of the app.
    * **Coordinates:** The precise geographical location (Latitude, Longitude) of the POI. ([How do I find the coordinates?](https://developers.google.com/ar/develop/unity-arf/geospatial/anchors#use_google_maps))
    * **Image:** An associated image for the POI (see image setup below).
    * **(Optional) Sub Points Of Interest:** You can add an array of `Sub Point Of Interest` entries within a main POI. These offer secondary details or related spots nearby that the user can optionally explore when at the main POI. Each sub-POI typically needs a `Title` and a `Source Title`. The source title should match the exact title of an English or German Wikipedia page that has relevant information about the sub-POI.
5.  **POI Image Setup:**
    * Images used for POIs must be placed in the `Assets/Resources/Images/` folder within your Unity project.
    * **Naming Convention:** Each image file *must* be named exactly the same as the corresponding POI `Title` (case-sensitive, including spaces) and have a standard image file extension (e.g., `My POI Title.png`, `Another Spot.jpg`).

---
