using System;
using System.Collections;
using Google.XR.ARCoreExtensions;
using i5.LLM_AR_Tourguide.Configurations;
using i5.LLM_AR_Tourguide.GeospatialAPI;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.XR.ARSubsystems;

namespace i5.LLM_AR_Tourguide.UI_Scripts
{
    public class LocalizationProgress : MonoBehaviour
    {
        [Range(0, 100)] public float percentage;

        [SerializeField] private TextMeshProUGUI descriptionText;

        public bool IsLocalizationComplete;
#if UNITY_EDITOR
        [SerializeField] private float progress;
#endif

        [SerializeField] private GeospatialController geospatialController;

        private float _lastDisplayedPercentage;
        private float _lastMessageTime;
        private float _lastMessageTime2;

        private CanvasGroup canvasGroup;

        private GeospatialPose GeospatialPose;
        private bool isCurrentlyChecking;
        private string localizationProgressString;
        private string localizationErrorString;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void Start()
        {
            IsLocalizationComplete = false;
            canvasGroup = GetComponent<CanvasGroup>();
            localizationProgressString = LocalizationSettings.StringDatabase.GetLocalizedString("LocalizationProgress");
            localizationErrorString = LocalizationSettings.StringDatabase.GetLocalizedString("PleaseMakeSureThatYouAreConnectedToTheInternet");
        }

        // Update is called once per frame
        private void Update()
        {
            if (IsLocalizationComplete) return;
            if (Time.time - _lastMessageTime2 > 5.0f)
            {
                _lastMessageTime2 = Time.time;
                if (Application.internetReachability == NetworkReachability.NotReachable)
                {
                    descriptionText.text = localizationErrorString;
                    return;
                }
            }

            if (Mathf.Approximately(percentage, 100)) StartCoroutine(CheckAgain());

            if (localizationProgressString == null)
                localizationProgressString =
                    LocalizationSettings.StringDatabase.GetLocalizedString("LocalizationProgress");
            if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = percentage > 99 ? -(percentage - 100) / 30f : 1f;
            if (Mathf.Abs(_lastDisplayedPercentage - percentage) > 0.3f)
            {
                _lastDisplayedPercentage = percentage; // Store the last value
                descriptionText.text = $"{localizationProgressString}\n{percentage:F1}%";
            }
#if UNITY_EDITOR
            percentage += progress;
            percentage = Mathf.Clamp(percentage, 0, 100);
#endif


            if (!geospatialController.EarthManager) return;
            var EarthManager = geospatialController.EarthManager;
            try
            {
                var pose = EarthManager.EarthState == EarthState.Enabled &&
                           EarthManager.EarthTrackingState == TrackingState.Tracking
                    ? (GeospatialPose?)EarthManager.CameraGeospatialPose
                    : null;
                if (!pose.HasValue) return;

                GeospatialPose = pose.Value;
                var vA = Math.Max(0,
                    GeospatialPose.OrientationYawAccuracy - GeospatialController.orientationYawAccuracyThreshold);
                var hA = Math.Max(0,
                    GeospatialPose.HorizontalAccuracy - GeospatialController.horizontalAccuracyThreshold);

                // Calculate the percentage based on the accuracy with higher impact for values closer to 0
                var accuracySum = (float)(vA + hA);
                percentage = 100 * Mathf.Pow((float)0.97, accuracySum);
            }
            catch (Exception e)
            {
#if !UNITY_EDITOR
                throw e;
#endif
                _ = e; // Suppress warning
            }
        }

        private IEnumerator CheckAgain()
        {
            if (isCurrentlyChecking) yield break;
            isCurrentlyChecking = true;
            //wait three frames
            if (Mathf.Approximately(percentage, 100))
            {
                if (Time.time - _lastMessageTime < 2.0f)
                {
                    isCurrentlyChecking = false;
                    yield break;
                }

                //PermissionRequest.ShowAndroidToastMessage(
                //    LocalizationSettings.StringDatabase.GetLocalizedString("LocalizationComplete"));
                IsLocalizationComplete = true;
                _lastMessageTime = Time.time;
            }

            isCurrentlyChecking = false;
        }

        public PointOfInterestData.Coordinates GetCurrentPosition()
        {
            var temp = new PointOfInterestData.Coordinates();
            if (!geospatialController) geospatialController = FindAnyObjectByType<GeospatialController>();

            if (!geospatialController) return temp;
            var EarthManager = geospatialController.EarthManager;

            var pose = EarthManager.EarthState == EarthState.Enabled &&
                       EarthManager.EarthTrackingState == TrackingState.Tracking
                ? (GeospatialPose?)EarthManager.CameraGeospatialPose
                : null;
            if (!pose.HasValue) return temp;

            GeospatialPose = pose.Value;


            return new PointOfInterestData.Coordinates
            {
                latitude = GeospatialPose.Latitude, longitude = GeospatialPose.Longitude,
                altitude = GeospatialPose.Altitude
            };
        }
    }
}