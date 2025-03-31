using System;
using Google.XR.ARCoreExtensions;
using i5.LLM_AR_Tourguide.GeospatialAPI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.AdaptivePerformance;
using UnityEngine.XR.ARFoundation;

public class DebugOutput : MonoBehaviour
{
    [SerializeField] private AROcclusionManager occlusionManager;
    [SerializeField] private ARPlaneManager planeManager;
    private TextMeshProUGUI text;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        text = GetComponent<TextMeshProUGUI>();
    }

    // Update is called once per frame
    private void OnEnable()
    {
        if (text == null) text = GetComponent<TextMeshProUGUI>();

        var debugText = "";
        debugText += "Current Time:" + DateTime.Now + "\n";
        debugText += "finishedOnboarding: " + PlayerPrefs.GetInt("HasFinishedOnboardingOnceKey") + "\n";
        debugText += "User Information: " + PlayerPrefs.GetString("UserInformationKey") + "\n";
        debugText += "WarningLevel: " + Holder.Instance.ThermalStatus.ThermalMetrics.WarningLevel + "\n";
        debugText += "Temp: " + Holder.Instance.ThermalStatus.ThermalMetrics.TemperatureLevel + "\n";
        debugText += "FPS: " + Holder.Instance.PerformanceStatus.FrameTiming.AverageFrameTime + "\n";
        debugText += "Occ: " + (occlusionManager.isActiveAndEnabled ? "Active" : "Inactive") + "\n";
        debugText += "Plane: " + (planeManager.isActiveAndEnabled ? "Active" : "Inactive") + "\n";
        debugText += "Screen Width: " + Screen.width + "\n";
        debugText += "Screen Height: " + Screen.height + "\n";
        debugText += "MinDistance: " + GeospatialController.minDistance + "\n";
        debugText += "MaxDistance: " + GeospatialController.maxDistance + "\n";
        //debugText += "Information Controller: " + PlayerPrefs.GetString("InformationControllerKey") + "\n";
        text.text = debugText;
    }
#if UNITY_EDITOR
    [MenuItem("Debug/Reset PlayerPrefs")]
#endif
    public static void DeleteAllPlayerPrefs()
    {
        if (PlayerPrefs.HasKey("numberOfCharactersUsed"))
        {
            var userID = PlayerPrefs.GetString("userID");
            var numberOfCharactersUsed = PlayerPrefs.GetInt("numberOfCharactersUsed");
            PlayerPrefs.DeleteAll();
            PlayerPrefs.SetInt("numberOfCharactersUsed", numberOfCharactersUsed);
            PlayerPrefs.SetString("userID", userID);
            PlayerPrefs.Save();
        }
        else
        {
            PlayerPrefs.DeleteAll();
        }

        Application.Quit();
    }

#if UNITY_EDITOR
    [MenuItem("Debug/DisableGeospatialErrors")]
    public static void DisableGeospatialErrors()
    {
        var geospatialController = FindAnyObjectByType<GeospatialController>();
        var arStreetscapeGeometryManager = FindAnyObjectByType<ARStreetscapeGeometryManager>();

        geospatialController.gameObject.SetActive(false);
        arStreetscapeGeometryManager.enabled = false;
    }

    [MenuItem("Debug/EnableGeospatialErrors")]
    public static void EnableGeospatialErrors()
    {
        var geospatialController = FindAnyObjectByType<GeospatialController>();
        var arStreetscapeGeometryManager = FindAnyObjectByType<ARStreetscapeGeometryManager>();

        geospatialController.gameObject.SetActive(true);
        geospatialController.enabled = true;
        arStreetscapeGeometryManager.enabled = true;
    }


#endif
}