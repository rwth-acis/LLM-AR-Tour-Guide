using System;
using System.Collections.Generic;
using Google.XR.ARCoreExtensions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARSubsystems;

namespace i5.LLM_AR_Tourguide
{
    public class SceneSwitcher : MonoBehaviour
    {
        public List<SceneLocation> sceneLocations;
        public AREarthManager EarthManager;
        public float sceneLoadThreshold = 10f; // Distance in arbitrary units (needs tuning based on your world scale)

        public string currentLoadedScene = "";

        private void Start()
        {
            sceneLocations = new List<SceneLocation>
            {
                new() { sceneName = "Beginenstra�e", coordinates = new Vector3(50.77604f, 6.078353f, 218f) },
                new() { sceneName = "Aachen Cathedral", coordinates = new Vector3(50.77476f, 6.083226f, 215f) }
            };

            if (sceneLocations == null || sceneLocations.Count == 0)
                DebugEditor.LogError("No scene locations defined in the SceneSwitcher!");
            currentLoadedScene = SceneManager.GetActiveScene().name;
        }

        private void Update()
        {
            DebugEditor.Log("XXXXXXXXXXXXXXXXX Current scene: " + currentLoadedScene);
            if (EarthManager.EarthTrackingState == TrackingState.Tracking &&
                EarthManager.CameraGeospatialPose.HorizontalAccuracy <
                5) // Increased accuracy threshold for practical use
            {
                var cameraPosition = new Vector3(
                    (float)EarthManager.CameraGeospatialPose.Latitude,
                    (float)EarthManager.CameraGeospatialPose.Longitude,
                    (float)EarthManager.CameraGeospatialPose.Altitude
                );

                var closestLocation = FindClosestScene(cameraPosition);

                DebugEditor.Log("XXXXXXXXXXXXXXXXX Closest scene: " + closestLocation.sceneName);

                if (closestLocation.sceneName != null)
                {
                    var distanceToClosest = Vector3.Distance(cameraPosition, closestLocation.coordinates);

                    if (distanceToClosest < sceneLoadThreshold && closestLocation.sceneName != currentLoadedScene)
                        LoadScene(closestLocation.sceneName);
                }
            }

            DebugEditor.Log("XXXXXXXXXXXXXXXXX" + EarthManager.CameraGeospatialPose.HorizontalAccuracy);
        }

        private SceneLocation FindClosestScene(Vector3 currentPosition)
        {
            var closestLocation = new SceneLocation { sceneName = null };
            var closestDistance = Mathf.Infinity;

            foreach (var location in sceneLocations)
            {
                var distance = Vector3.Distance(currentPosition, location.coordinates);
                if (distance < closestDistance)
                {
                    DebugEditor.Log(location.sceneName + ": " + distance);
                    closestDistance = distance;
                    closestLocation = location;
                }
            }

            return closestLocation;
        }

        private void LoadScene(string sceneName)
        {
            DebugEditor.Log($"Loading scene: {sceneName}");
            SceneManager.LoadScene(sceneName);
            currentLoadedScene = sceneName;
        }

        [Serializable]
        public struct SceneLocation
        {
            public string sceneName;
            public Vector3 coordinates; // Latitude, Longitude, Altitude
        }
    }
}