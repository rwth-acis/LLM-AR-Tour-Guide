using i5.LLM_AR_Tourguide.Audio;
using i5.LLM_AR_Tourguide.Evaluation;
using i5.LLM_AR_Tourguide.GeospatialAPI;
using i5.LLM_AR_Tourguide.Prefab_Scripts;
using UnityEngine;
using UnityEngine.AdaptivePerformance;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Button = UnityEngine.UI.Button;
using Slider = UnityEngine.UI.Slider;
using Toggle = UnityEngine.UI.Toggle;


namespace i5.LLM_AR_Tourguide.UI_Scripts
{
    public class Settings : MonoBehaviour
    {
        [SerializeField] private Button activateOcclusion;
        [SerializeField] private AROcclusionManager arOcclusionManager;

        [SerializeField] private ARPlaneManager arPlaneManager;
        [SerializeField] private Button deactivateOcclusion;

        public bool DebugMode;
        [SerializeField] private Toggle debugModeToggle;

        [SerializeField] private GameObject[] debugObjects;
        [SerializeField] private Button decreaseThreshold;

        [SerializeField] private Toggle disableVoice;
        [SerializeField] private Toggle fasterRevealSpeed;

        [SerializeField] private Button increaseThreshold;
        [SerializeField] private Slider maxDistance;

        [SerializeField] private Slider minDistance;
        [SerializeField] private Toggle normalRevealSpeed;

        [SerializeField] private ScrollRect scrollRect;
        private readonly WarningLevel lastWarningLevel = WarningLevel.NoWarning;
        private RectTransform _rectTransform;
        private TextToSpeech _textToSpeech;

        private IAdaptivePerformance ap;

        private int i = -1;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void Start()
        {
            _rectTransform = scrollRect.GetComponent<RectTransform>();
            Application.targetFrameRate = 60;
            if (PlayerPrefs.GetInt("revealSpeed", 0) == 1)
            {
                fasterRevealSpeed.isOn = true;
                normalRevealSpeed.isOn = false;
            }
            else
            {
                fasterRevealSpeed.isOn = false;
                normalRevealSpeed.isOn = true;
            }

            ap = Holder.Instance;

            DebugModeToggle(false, true);
            debugModeToggle.isOn = false;
            fasterRevealSpeed.onValueChanged.AddListener(delegate { SetRevealSpeed(); });
            debugModeToggle.onValueChanged.AddListener(delegate { DebugModeToggle(debugModeToggle.isOn); });

            increaseThreshold.onClick.AddListener(IncreaseThreshold);
            decreaseThreshold.onClick.AddListener(DecreaseThreshold);

            activateOcclusion.onClick.AddListener(delegate { ActivateOcclusion(); });
            deactivateOcclusion.onClick.AddListener(delegate { DeactivateOcclusion(); });

            ReloadOcclusion();

            ap.ThermalStatus.ThermalEvent += OnThermalEvent;

            _textToSpeech = GetComponent<TextToSpeech>();
            disableVoice.isOn = PlayerPrefs.GetInt("disableVoice", 0) == 1;
            disableVoice.onValueChanged.AddListener(SetVoice);
            SetVoice(disableVoice.isOn);

            minDistance.onValueChanged.AddListener(delegate { OnMinDistanceChanged(); });
            maxDistance.onValueChanged.AddListener(delegate { OnMaxDistanceChanged(); });
        }

        private void Update()
        {
            // Check if the scroll view has top of 35 and bottom of 0 in Rect Transform
            if (_rectTransform.offsetMin.y < 0)
            {
                if (DebugMode)
                    PermissionRequest.ShowToastMessage("Fixed scroll rect transform" + _rectTransform.offsetMin.y);
                _rectTransform.offsetMin = new Vector2(_rectTransform.offsetMin.x, 0);
            }

            if (_rectTransform.offsetMax.y > 35)
            {
                if (DebugMode)
                    PermissionRequest.ShowToastMessage("Fixed scroll rect transform" + _rectTransform.offsetMax.y);
                PermissionRequest.ShowToastMessage("Fixed scroll rect transform");
            }
        }

        private void SetVoice(bool value)
        {
            _textToSpeech.testingMode = value;
            if (value) _textToSpeech.testMessage = "Voice disabled";
            PlayerPrefs.SetInt("disableVoice", value ? 1 : 0);
        }

        public void OnMinDistanceChanged()
        {
            var distance = minDistance.value;
            GeospatialController.minDistance = distance;
        }

        public void OnMaxDistanceChanged()
        {
            var distance = maxDistance.value;
            GeospatialController.maxDistance = distance;
        }

        public void SetRevealSpeed()
        {
            if (fasterRevealSpeed.isOn)
                PlayerPrefs.SetInt("revealSpeed", 1);
            else if (normalRevealSpeed.isOn) PlayerPrefs.SetInt("revealSpeed", 0);
        }

        private void DebugModeToggle(bool value, bool bypass = false)
        {
            if (value)
                i++;
            if (i == 1 && !bypass)
            {
                PermissionRequest.ShowToastMessage("Are you sure you want to enable debug mode?");
            }
            else if (i == 2 && !bypass)
            {
                PermissionRequest.ShowToastMessage("Are you really sure you want to enable debug mode?");
            }
            else if (i == 3 && !bypass)
            {
                PermissionRequest.ShowToastMessage("Ok, I will trust you.");
            }
            else if (i > 3 || bypass)
            {
                UploadManager.UploadData("DebugMode" + value, "DebugMode");
                DebugMode = value;
                foreach (var debugObject in debugObjects) debugObject.SetActive(DebugMode);
            }
        }

        public void DebugModeToggle()
        {
            DebugModeToggle(!DebugMode);
        }

        private void IncreaseThreshold()
        {
            var tourIntroductionUi =
                FindObjectsByType<TourIntroductionUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var VARIABLE in tourIntroductionUi) VARIABLE.IncreaseDistanceThreshold(1000);
        }

        private void DecreaseThreshold()
        {
            var tourIntroductionUi =
                FindObjectsByType<TourIntroductionUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var VARIABLE in tourIntroductionUi) VARIABLE.IncreaseDistanceThreshold(-1000);
        }

        public void DeactivateOcclusion(bool silent = false)
        {
            if (!arOcclusionManager.isActiveAndEnabled) return;
            if (arOcclusionManager.currentEnvironmentDepthMode == EnvironmentDepthMode.Disabled)
                return;
            arOcclusionManager.requestedEnvironmentDepthMode = EnvironmentDepthMode.Disabled;
            arOcclusionManager.requestedOcclusionPreferenceMode = OcclusionPreferenceMode.NoOcclusion;
            PlayerPrefs.SetInt("occlusionManager", 0);
            if (silent == false)
                PermissionRequest.ShowToastMessage("AR Object Occlusion deactivated");
            ;
        }

        public void ActivateOcclusion(bool silent = false)
        {
            if (!arOcclusionManager.isActiveAndEnabled) return;
            if (arOcclusionManager.currentEnvironmentDepthMode == EnvironmentDepthMode.Fastest)
                return;
            arOcclusionManager.requestedEnvironmentDepthMode = EnvironmentDepthMode.Fastest;
            arOcclusionManager.requestedOcclusionPreferenceMode = OcclusionPreferenceMode.PreferEnvironmentOcclusion;
            PlayerPrefs.SetInt("occlusionManager", 1);
            if (silent == false)
                PermissionRequest.ShowToastMessage("AR Object Occlusion activated");
        }

        public void ReloadOcclusion()
        {
            var occ = PlayerPrefs.GetInt("occlusionManager", 0);
            if (occ == 0)
                DeactivateOcclusion(true);
            else
                ActivateOcclusion(true);
        }

        public void ActivateARPlaneDetection(bool silent = false)
        {
            if (arPlaneManager.currentDetectionMode == PlaneDetectionMode.Horizontal)
                return;
            arPlaneManager.requestedDetectionMode = PlaneDetectionMode.Horizontal;
            if (silent == false)
                PermissionRequest.ShowToastMessage("Changed AR Plane Detection to activated");
        }

        public void DeactivateARPlaneDetection(bool silent = false)
        {
            if (arPlaneManager.currentDetectionMode == PlaneDetectionMode.None)
                return;
            arPlaneManager.requestedDetectionMode = PlaneDetectionMode.None;
            if (silent == false)
                PermissionRequest.ShowToastMessage("Changed AR Plane Detection to deactivated");
        }

        private void OnThermalEvent(ThermalMetrics ev)
        {
            if (ev.WarningLevel == lastWarningLevel) return;
            var silent = !DebugMode;

            switch (ev.WarningLevel)
            {
                case WarningLevel.NoWarning:
                    ActivateARPlaneDetection(silent);
                    break;
                case WarningLevel.ThrottlingImminent:
                    DeactivateOcclusion(silent);
                    break;
                case WarningLevel.Throttling:
                    DeactivateOcclusion(silent);
                    DeactivateARPlaneDetection(silent);
                    break;
            }
        }
    }
}