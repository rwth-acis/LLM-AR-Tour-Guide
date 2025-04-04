#if !ENABLE_LEGACY_INPUT_MANAGER
// Input.location will not work at runtime with out the old input system.
// Given that sample has not been ported to support new input
// Check that Project Settings > Player > Other Settings > Active Input Handling
// is set to Both or Input Manager (Old)
#error Input.location API requires Active Input Handling to be set to Input Manager (Old) or Both
#endif

#if !ENABLE_INPUT_SYSTEM
// The camera's pose driver in ARF5 needs Input System (New) but given we need Input Manager
// (Old) for Input.location (see above) ARF5 needs both.
// Check that Project Settings > Player > Other Settings > Active Input Handling
// is set to Both
#error The camera's pose driver needs Input System (New) so set Active Input Handling to Both
#endif // !ENABLE_INPUT_SYSTEM
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Google.XR.ARCoreExtensions;
using i5.LLM_AR_Tourguide.Configurations;
using i5.LLM_AR_Tourguide.UI_Scripts;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace i5.LLM_AR_Tourguide.GeospatialAPI
{
    /// <summary>
    ///     Controller for Geospatial sample.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1118:ParameterMustNotSpanMultipleLines",
        Justification = "Bypass source check.")]
    public class GeospatialController : MonoBehaviour
    {
        /// <summary>
        ///     Help message shown while localizing.
        /// </summary>
        private const string _localizingMessage = "Localizing your device to set anchor.";

        /// <summary>
        ///     Help message shown while initializing Geospatial functionalities.
        /// </summary>
        private const string _localizationInitializingMessage =
            "Initializing Geospatial functionalities.";

        /// <summary>
        ///     Help message shown when <see cref="AREarthManager.EarthTrackingState" /> is not tracking
        ///     or the pose accuracies are beyond thresholds.
        /// </summary>
        private const string _localizationInstructionMessage =
            "Point your camera at buildings, stores, and signs near you.";

        /// <summary>
        ///     Help message shown when location fails or hits timeout.
        /// </summary>
        private const string _localizationFailureMessage =
            "Localization not possible.\n" +
            "Close and open the app to restart the session.";

        /// <summary>
        ///     Help message shown when localization is completed.
        /// </summary>
        private const string _localizationSuccessMessage = "Localization completed.";

        /// <summary>
        ///     The timeout period waiting for localization to be completed.
        /// </summary>
        private const float _timeoutSeconds = 10000;

        /// <summary>
        ///     Indicates how long a pointOfInterests text will display on the screen before terminating.
        /// </summary>
        private const float _errorDisplaySeconds = 2;

        /// <summary>
        ///     The key name used in PlayerPrefs which indicates whether the privacy prompt has
        ///     displayed at least one time.
        /// </summary>
        private const string _hasDisplayedPrivacyPromptKey = "HasDisplayedGeospatialPrivacyPrompt";

        /// <summary>
        ///     The key name used in PlayerPrefs which stores geospatial anchor history data.
        ///     The earliest one will be deleted once it hits storage limit.
        /// </summary>
        private const string _persistentGeospatialAnchorsStorageKey = "PersistentGeospatialAnchors";

        /// <summary>
        ///     The limitation of how many Geospatial Anchors can be stored in local storage.
        /// </summary>
        private const int _storageLimit = 30;

        /// <summary>
        ///     Accuracy threshold for orientation yaw accuracy in degrees that can be treated as
        ///     localization completed.
        /// </summary>
        public const double orientationYawAccuracyThreshold = 5;


        /// <summary>
        ///     Accuracy threshold for altitude and longitude that can be treated as localization
        ///     completed.
        /// </summary>
        public const double horizontalAccuracyThreshold = 5;

        public const double altitudeAboveRoofTopForRoofTopAnchors = 10;

        public static float minDistance = 100f;
        public static float maxDistance = 500f;


        [Header("AR Components")]
        /// <summary>
        ///     The XROrigin used in the sample.
        /// </summary>
        public XROrigin Origin;

        /// <summary>
        ///     The ARSession used in the sample.
        /// </summary>
        public ARSession Session;

        /// <summary>
        ///     The ARAnchorManager used in the sample.
        /// </summary>
        public ARAnchorManager AnchorManager;

        /// <summary>
        ///     The ARRaycastManager used in the sample.
        /// </summary>
        public ARRaycastManager RaycastManager;

        /// <summary>
        ///     The AREarthManager used in the sample.
        /// </summary>
        public AREarthManager EarthManager;

        /// <summary>
        ///     The ARStreetscapeGeometryManager used in the sample.
        /// </summary>
        public ARStreetscapeGeometryManager StreetscapeGeometryManager;

        /// <summary>
        ///     The ARCoreExtensions used in the sample.
        /// </summary>
        public ARCoreExtensions ARCoreExtensions;

        /// <summary>
        ///     The StreetscapeGeometry materials for rendering geometry building meshes.
        /// </summary>
        public List<Material> StreetscapeGeometryMaterialBuilding;

        /// <summary>
        ///     The StreetscapeGeometry material for rendering geometry terrain meshes.
        /// </summary>
        public Material StreetscapeGeometryMaterialTerrain;

        [Header("UI Elements")]
        /// <summary>
        ///     A 3D object that presents a Geospatial Anchor.
        /// </summary>
        public GameObject GeospatialPrefab;

        /// <summary>
        ///     A 3D object that presents a Geospatial Terrain anchor.
        /// </summary>
        public GameObject TerrainPrefab;

        /// <summary>
        ///     A 3D object that presents a Rooftop anchor.
        /// </summary>
        public GameObject RoofTopPrefab;

        /// <summary>
        ///     A 3D object that presents a Rooftop anchor for a small point of interest.
        /// </summary>
        public GameObject SmallRoofTopPrefab;

        /// <summary>
        ///     UI element containing all AR view contents.
        /// </summary>
        public GameObject ARViewCanvas;

        /// <summary>
        ///     UI element for clearing all anchors, including history.
        /// </summary>
        public Button ClearAllButton;

        /// <summary>
        ///     UI element that enables streetscape geometry visibility.
        /// </summary>
        public Toggle GeometryToggle;

        /// <summary>
        ///     UI element to display or hide the Anchor Settings panel.
        /// </summary>
        public Button AnchorSettingButton;

        /// <summary>
        ///     UI element for the Anchor Settings panel.
        /// </summary>
        public GameObject AnchorSettingPanel;

        /// <summary>
        ///     UI element that toggles anchor type to Geometry.
        /// </summary>
        public Toggle GeospatialAnchorToggle;

        /// <summary>
        ///     UI element that toggles anchor type to Terrain.
        /// </summary>
        public Toggle TerrainAnchorToggle;

        /// <summary>
        ///     UI element that toggles anchor type to Rooftop.
        /// </summary>
        public Toggle RooftopAnchorToggle;

        /// <summary>
        ///     UI element to display pointOfInterests at runtime.
        /// </summary>
        public GameObject InfoPanel;

        /// <summary>
        ///     Text displaying <see cref="GeospatialPose" /> pointOfInterests at runtime.
        /// </summary>
        public Text InfoText;

        /// <summary>
        ///     Text displaying in a snack bar at the bottom of the screen.
        /// </summary>
        public Text SnackBarText;

        /// <summary>
        ///     Text displaying debug pointOfInterests, only activated in debug build.
        /// </summary>
        public Text DebugText;


        public LocalizationProgress localizationProgress;

        private readonly List<GameObject> _anchorObjects = new();

        /// <summary>
        ///     Dictionary of streetscapegeometry handles to render objects for rendering
        ///     streetscapegeometry meshes.
        /// </summary>
        private readonly Dictionary<TrackableId, GameObject> _streetscapegeometryGOs = new();

        /// <summary>
        ///     ARStreetscapeGeometries added in the last Unity Update.
        /// </summary>
        private List<ARStreetscapeGeometry> _addedStreetscapeGeometries = new();

        /// <summary>
        ///     Represents the current anchor type of the anchor being placed in the scene.
        /// </summary>
        private AnchorType _anchorType = AnchorType.Geospatial;

        private IEnumerator _asyncCheck;

        /// <summary>
        ///     Determines which building material will be used for the current building mesh.
        /// </summary>
        private int _buildingMatIndex;

        /// <summary>
        ///     Determines if streetscape geometry should be removed from the scene.
        /// </summary>
        private bool _clearStreetscapeGeometryRenderObjects;

        private float _configurePrepareTime = 3f;
        private bool _enablingGeospatial;
        private GeospatialAnchorHistoryCollection _historyCollection;
        private bool _isInARView;
        private bool _isLocalizing;
        private bool _isReturning;
        private float _localizationPassedTime;

        /// <summary>
        ///     ARStreetscapeGeometries removed in the last Unity Update.
        /// </summary>
        private List<ARStreetscapeGeometry> _removedStreetscapeGeometries = new();

        private bool _shouldResolvingHistory = true;

        /// <summary>
        ///     Determines if the anchor settings panel is visible in the UI.
        /// </summary>
        private bool _showAnchorSettingsPanel;

        private IEnumerator _startLocationService;

        /// <summary>
        ///     Determines if streetscape geometry is rendered in the scene.
        /// </summary>
        private bool _streetscapeGeometryVisibility;

        private FeatureSupported _supported = FeatureSupported.Unknown;

        /// <summary>
        ///     ARStreetscapeGeometries updated in the last Unity Update.
        /// </summary>
        private List<ARStreetscapeGeometry> _updatedStreetscapeGeometries = new();

        private bool _waitingForLocationService;


        private FeatureSupported featureSupport = FeatureSupported.Unknown;
        private string LoadedInformation;

        /// <summary>
        ///     Unity's Awake() method.
        /// </summary>
        public void Awake()
        {
            // Lock screen to portrait.
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.orientation = ScreenOrientation.Portrait;

            // Enable geospatial sample to target 60fps camera capture frame rate
            // on supported devices.
            // Note, Application.targetFrameRate is ignored when QualitySettings.vSyncCount != 0.
            Application.targetFrameRate = 60;

            if (Origin == null) Debug.LogError("Cannot find XROrigin.");

            if (Session == null) Debug.LogError("Cannot find ARSession.");

            if (ARCoreExtensions == null) Debug.LogError("Cannot find ARCoreExtensions.");
        }

        /// <summary>
        ///     Unity's Update() method.
        /// </summary>
        public void Update()
        {
            if (!_isInARView) return;

#if UNITY_EDITOR
            try
            {
#endif
                UpdateDebugInfo();
#if UNITY_EDITOR
            }
            catch (Exception e)
            {
                // Discard e to disable the warning
                _ = e;
            }
#endif

            // Check session error status.
            LifecycleUpdate();
            if (_isReturning) return;

            if (ARSession.state != ARSessionState.SessionInitializing &&
                ARSession.state != ARSessionState.SessionTracking)
                return;

            // Check feature support and enable Geospatial API when it's supported.
            if (featureSupport == FeatureSupported.Unknown)
                featureSupport = EarthManager.IsGeospatialModeSupported(GeospatialMode.Enabled);
            switch (featureSupport)
            {
                case FeatureSupported.Unknown:
                    return;
                case FeatureSupported.Unsupported:
                    ReturnWithReason("The Geospatial API is not supported by this device.");
                    return;
                case FeatureSupported.Supported:
                    if (ARCoreExtensions.ARCoreExtensionsConfig.GeospatialMode ==
                        GeospatialMode.Disabled)
                    {
                        Debug.Log("Geospatial sample switched to GeospatialMode.Enabled.");
                        ARCoreExtensions.ARCoreExtensionsConfig.GeospatialMode =
                            GeospatialMode.Enabled;
                        ARCoreExtensions.ARCoreExtensionsConfig.StreetscapeGeometryMode =
                            StreetscapeGeometryMode.Enabled;
                        _configurePrepareTime = 3.0f;
                        _enablingGeospatial = true;
                        return;
                    }

                    break;
            }

            // Waiting for new configuration to take effect.
            if (_enablingGeospatial)
            {
                _configurePrepareTime -= Time.deltaTime;
                if (_configurePrepareTime < 0)
                    _enablingGeospatial = false;
                else
                    return;
            }

            // Check earth state.
            var earthState = EarthManager.EarthState;
            if (earthState == EarthState.ErrorEarthNotReady)
            {
                SnackBarText.text = _localizationInitializingMessage;
                return;
            }

            if (earthState != EarthState.Enabled)
            {
                var errorMessage =
                    "Geospatial sample encountered an EarthState error: " + earthState;
                Debug.LogWarning(errorMessage);
                SnackBarText.text = errorMessage;
                return;
            }

            // Check earth localization.
            var isSessionReady = ARSession.state == ARSessionState.SessionTracking &&
                                 Input.location.status == LocationServiceStatus.Running;
            var earthTrackingState = EarthManager.EarthTrackingState;
            var pose = earthTrackingState == TrackingState.Tracking
                ? EarthManager.CameraGeospatialPose
                : new GeospatialPose();
            if (!isSessionReady || earthTrackingState != TrackingState.Tracking ||
                pose.OrientationYawAccuracy > orientationYawAccuracyThreshold ||
                pose.HorizontalAccuracy > horizontalAccuracyThreshold)
            {
                // Lost localization during the session.
                if (!_isLocalizing)
                {
                    string reason = string.Empty;
                    if(isSessionReady == false)
                        reason += "Session is not ready. ARSession State: " + ARSession.state + ", locationStatus: "+ Input.location.status;
                    if (earthTrackingState != TrackingState.Tracking)
                        reason += "EarthTrackingState is not tracking.";
                    if (pose.OrientationYawAccuracy > orientationYawAccuracyThreshold)
                        reason += "OrientationYawAccuracy is not tracking.";
                    if (pose.HorizontalAccuracy > horizontalAccuracyThreshold)
                        reason += "HorizontalAccuracy is not tracking.";
                    
                    Debug.Log("XXXX Lost localization during the session. Reason: " + reason);
                        
                    localizationProgress.IsLocalizationComplete = false;
                    _isLocalizing = true;
                    _localizationPassedTime = 0f;
                    GeometryToggle.gameObject.SetActive(false);
                    AnchorSettingButton.gameObject.SetActive(false);
                    AnchorSettingPanel.gameObject.SetActive(false);
                    GeospatialAnchorToggle.gameObject.SetActive(false);
                    TerrainAnchorToggle.gameObject.SetActive(false);
                    RooftopAnchorToggle.gameObject.SetActive(false);
                    ClearAllButton.gameObject.SetActive(false);
                    foreach (var go in _anchorObjects) go.SetActive(false);
                }

                if (_localizationPassedTime > _timeoutSeconds)
                {
                    Debug.LogError("Geospatial sample localization timed out.");
                    ReturnWithReason(_localizationFailureMessage);
                }
                else
                {
                    Debug.Log("XXX Localization in progress.");
                    _localizationPassedTime += Time.deltaTime;
                    SnackBarText.text = _localizationInstructionMessage;
                    localizationProgress.gameObject.SetActive(true);
                    localizationProgress.IsLocalizationComplete = false;
                }
            }
            else if (_isLocalizing)
            {
                Debug.Log("XXXX Localization complete.");
                // Finished localization.
                _isLocalizing = false;
                _localizationPassedTime = 0f;
                GeometryToggle.gameObject.SetActive(true);
                AnchorSettingButton.gameObject.SetActive(true);
                ClearAllButton.gameObject.SetActive(_anchorObjects.Count > 0);
                SnackBarText.text = _localizationSuccessMessage;
                foreach (var go in _anchorObjects) go.SetActive(true);

                ResolveHistory();
            }
            else
            {
                if (_streetscapeGeometryVisibility)
                {
                    foreach (
                        var streetscapegeometry in _addedStreetscapeGeometries)
                        InstantiateRenderObject(streetscapegeometry);

                    foreach (
                        var streetscapegeometry in _updatedStreetscapeGeometries)
                    {
                        // This second call to instantiate is required if geometry is toggled on
                        // or off after the app has started.
                        InstantiateRenderObject(streetscapegeometry);
                        UpdateRenderObject(streetscapegeometry);
                    }

                    foreach (
                        var streetscapegeometry in _removedStreetscapeGeometries)
                        DestroyRenderObject(streetscapegeometry);
                }
                else if (_clearStreetscapeGeometryRenderObjects)
                {
                    DestroyAllRenderObjects();
                    _clearStreetscapeGeometryRenderObjects = false;
                }

                if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began
                                         && !EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)
                                         && _anchorObjects.Count < _storageLimit)
                    // Set anchor on screen tap.
                    //PlaceAnchorByScreenTap(Input.GetTouch(0).position);

                    // Hide anchor settings and toggles if the storage limit has been reached.
                    if (_anchorObjects.Count >= _storageLimit)
                    {
                        AnchorSettingButton.gameObject.SetActive(false);
                        AnchorSettingPanel.gameObject.SetActive(false);
                        GeospatialAnchorToggle.gameObject.SetActive(false);
                        TerrainAnchorToggle.gameObject.SetActive(false);
                        RooftopAnchorToggle.gameObject.SetActive(false);
                    }
                    else
                    {
                        AnchorSettingButton.gameObject.SetActive(true);
                    }
            }

            InfoPanel.SetActive(true);
            if (earthTrackingState == TrackingState.Tracking)
                InfoText.text = string.Format(
                    "Latitude/Longitude: {1:F6}°, {2:F6}°{0}" +
                    "Horizontal Accuracy: {3:F6}m{0}" +
                    "Altitude: {4:F2}m{0}" +
                    "Vertical Accuracy: {5:F2}m{0}" +
                    "Eun Rotation: {6}{0}" +
                    "Orientation Yaw Accuracy: {7:F1}°",
                    Environment.NewLine, pose.Latitude, pose.Longitude, pose.HorizontalAccuracy, pose.Altitude,
                    pose.VerticalAccuracy,
                    pose.EunRotation.ToString("F1"), pose.OrientationYawAccuracy);
            else
                InfoText.text = "GEOSPATIAL POSE: not tracking";
            if (_isLocalizing)
                localizationProgress.gameObject.SetActive(true);
        }

        /// <summary>
        ///     Unity's OnEnable() method.
        /// </summary>
        public void OnEnable()
        {
            _startLocationService = StartLocationService();
            StartCoroutine(_startLocationService);

            _isReturning = false;
            _enablingGeospatial = false;
            InfoPanel.SetActive(false);
            GeometryToggle.gameObject.SetActive(false);
            AnchorSettingButton.gameObject.SetActive(false);
            AnchorSettingPanel.gameObject.SetActive(false);
            GeospatialAnchorToggle.gameObject.SetActive(false);
            TerrainAnchorToggle.gameObject.SetActive(false);
            RooftopAnchorToggle.gameObject.SetActive(false);
            ClearAllButton.gameObject.SetActive(false);
            DebugText.gameObject.SetActive(Debug.isDebugBuild && EarthManager != null);
            GeometryToggle.onValueChanged.AddListener(OnGeometryToggled);
            AnchorSettingButton.onClick.AddListener(OnAnchorSettingButtonClicked);
            GeospatialAnchorToggle.onValueChanged.AddListener(OnGeospatialAnchorToggled);
            TerrainAnchorToggle.onValueChanged.AddListener(OnTerrainAnchorToggled);
            RooftopAnchorToggle.onValueChanged.AddListener(OnRooftopAnchorToggled);

            _localizationPassedTime = 0f;
            Debug.Log("XXXX Starting localization.");
            _isLocalizing = true;
            SnackBarText.text = _localizingMessage;

            LoadGeospatialAnchorHistory();
            _shouldResolvingHistory = true;
            //_shouldResolvingHistory = _historyCollection.Collection.Count > 0;

            SwitchToARView(PlayerPrefs.HasKey(_hasDisplayedPrivacyPromptKey));

            if (StreetscapeGeometryManager == null)
                Debug.LogWarning("StreetscapeGeometryManager must be set in the " +
                                       "GeospatialController Inspector to render StreetscapeGeometry.");

            if (StreetscapeGeometryMaterialBuilding.Count == 0)
            {
                Debug.LogWarning("StreetscapeGeometryMaterialBuilding in the " +
                                       "GeospatialController Inspector must contain at least one material " +
                                       "to render StreetscapeGeometry.");
                return;
            }

            if (StreetscapeGeometryMaterialTerrain == null)
                Debug.LogWarning("StreetscapeGeometryMaterialTerrain must be set in the " +
                                       "GeospatialController Inspector to render StreetscapeGeometry.");

            // get access to ARstreetscapeGeometries in ARStreetscapeGeometryManager
            //if (StreetscapeGeometryManager)
            //StreetscapeGeometryManager.StreetscapeGeometriesChanged += GetStreetscapeGeometry;
        }

        /// <summary>
        ///     Unity's OnDisable() method.
        /// </summary>
        public void OnDisable()
        {
            if (_asyncCheck != null)
            {
                StopCoroutine(_asyncCheck);
                _asyncCheck = null;
            }

            if (_startLocationService != null)
            {
                StopCoroutine(_startLocationService);
                _startLocationService = null;
            }

            Debug.Log("Stop location services.");
            Input.location.Stop();

            foreach (var anchor in _anchorObjects) Destroy(anchor);

            _anchorObjects.Clear();
            SaveGeospatialAnchorHistory();

            //if (StreetscapeGeometryManager)
            //    StreetscapeGeometryManager.StreetscapeGeometriesChanged -=
            //        GetStreetscapeGeometry;
        }

        /// <summary>
        ///     Callback handling "Get Started" button click event in Privacy Prompt.
        /// </summary>
        public void OnGetStartedClicked()
        {
            PlayerPrefs.SetInt(_hasDisplayedPrivacyPromptKey, 1);
            PlayerPrefs.Save();
            SwitchToARView(true);
        }

        /// <summary>
        ///     Callback handling "Learn More" Button click event in Privacy Prompt.
        /// </summary>
        public void OnLearnMoreClicked()
        {
            Application.OpenURL(
                "https://developers.google.com/ar/data-privacy");
        }

        /// <summary>
        ///     Callback handling "Clear All" button click event in AR View.
        /// </summary>
        public void OnClearAllClicked()
        {
            foreach (var anchor in _anchorObjects) Destroy(anchor);

            _anchorObjects.Clear();
            _historyCollection.Collection.Clear();
            SnackBarText.text = "Anchor(s) cleared!";
            ClearAllButton.gameObject.SetActive(false);
            SaveGeospatialAnchorHistory();
        }


        /// <summary>
        ///     Callback handling "Geometry" toggle event in AR View.
        /// </summary>
        /// <param name="enabled">Whether to enable Streetscape Geometry visibility.</param>
        public void OnGeometryToggled(bool enabled)
        {
            _streetscapeGeometryVisibility = enabled;
            if (!_streetscapeGeometryVisibility) _clearStreetscapeGeometryRenderObjects = true;
        }

        /// <summary>
        ///     Callback handling the  "Anchor Setting" panel display or hide event in AR View.
        /// </summary>
        public void OnAnchorSettingButtonClicked()
        {
            _showAnchorSettingsPanel = !_showAnchorSettingsPanel;
            if (_showAnchorSettingsPanel)
                SetAnchorPanelState(true);
            else
                SetAnchorPanelState(false);
        }

        /// <summary>
        ///     Callback handling Geospatial anchor toggle event in AR View.
        /// </summary>
        /// <param name="enabled">Whether to enable Geospatial anchors.</param>
        public void OnGeospatialAnchorToggled(bool enabled)
        {
            // GeospatialAnchorToggle.GetComponent<Toggle>().isOn = true;;
            _anchorType = AnchorType.Geospatial;
            SetAnchorPanelState(false);
        }

        /// <summary>
        ///     Callback handling Terrain anchor toggle event in AR View.
        /// </summary>
        /// <param name="enabled">Whether to enable Terrain anchors.</param>
        public void OnTerrainAnchorToggled(bool enabled)
        {
            // TerrainAnchorToggle.GetComponent<Toggle>().isOn = true;
            _anchorType = AnchorType.Terrain;
            SetAnchorPanelState(false);
        }

        /// <summary>
        ///     Callback handling Rooftop anchor toggle event in AR View.
        /// </summary>
        /// <param name="enabled">Whether to enable Rooftop anchors.</param>
        public void OnRooftopAnchorToggled(bool enabled)
        {
            // RooftopAnchorToggle.GetComponent<Toggle>().isOn = true;
            _anchorType = AnchorType.Rooftop;
            SetAnchorPanelState(false);
        }

        /// <summary>
        ///     Connects the <c>ARStreetscapeGeometry</c> to the specified lists for access.
        /// </summary>
        /// <param name="eventArgs">
        ///     The
        ///     <c>
        ///         <see cref="ARStreetscapeGeometriesChangedEventArgs" />
        ///     </c>
        ///     containing the
        ///     <c>ARStreetscapeGeometry</c>.
        /// </param>
        private void GetStreetscapeGeometry(ARStreetscapeGeometriesChangedEventArgs eventArgs)
        {
            _addedStreetscapeGeometries = eventArgs.Added;
            _updatedStreetscapeGeometries = eventArgs.Updated;
            _removedStreetscapeGeometries = eventArgs.Removed;
        }

        /// <summary>
        ///     Sets up a render object for this <c>ARStreetscapeGeometry</c>.
        /// </summary>
        /// <param name="streetscapegeometry">
        ///     The
        ///     <c>
        ///         <see cref="ARStreetscapeGeometry" />
        ///     </c>
        ///     object containing the mesh
        ///     to be rendered.
        /// </param>
        private void InstantiateRenderObject(ARStreetscapeGeometry streetscapegeometry)
        {
            if (streetscapegeometry.mesh == null) return;

            // Check if a render object already exists for this streetscapegeometry and
            // create one if not.
            if (_streetscapegeometryGOs.ContainsKey(streetscapegeometry.trackableId)) return;

            GameObject renderObject = new(
                "StreetscapeGeometryMesh", typeof(MeshFilter), typeof(MeshRenderer));

            if (renderObject)
            {
                renderObject.transform.position = new Vector3(0, 0.5f, 0);
                renderObject.GetComponent<MeshFilter>().mesh = streetscapegeometry.mesh;

                // Add a material with transparent diffuse shader.
                if (streetscapegeometry.streetscapeGeometryType ==
                    StreetscapeGeometryType.Building)
                {
                    renderObject.GetComponent<MeshRenderer>().material =
                        StreetscapeGeometryMaterialBuilding[_buildingMatIndex];
                    _buildingMatIndex =
                        (_buildingMatIndex + 1) % StreetscapeGeometryMaterialBuilding.Count;
                }
                else
                {
                    renderObject.GetComponent<MeshRenderer>().material =
                        StreetscapeGeometryMaterialTerrain;
                }

                renderObject.transform.position = streetscapegeometry.pose.position;
                renderObject.transform.rotation = streetscapegeometry.pose.rotation;

                _streetscapegeometryGOs.Add(streetscapegeometry.trackableId, renderObject);
            }
        }

        /// <summary>
        ///     Updates the render object transform based on this StreetscapeGeometries pose.
        ///     It must be called every frame to update the mesh.
        /// </summary>
        /// <param name="streetscapegeometry">
        ///     The
        ///     <c>
        ///         <see cref="ARStreetscapeGeometry" />
        ///     </c>
        ///     object containing the mesh to be rendered.
        /// </param>
        private void UpdateRenderObject(ARStreetscapeGeometry streetscapegeometry)
        {
            if (_streetscapegeometryGOs.ContainsKey(streetscapegeometry.trackableId))
            {
                var renderObject = _streetscapegeometryGOs[streetscapegeometry.trackableId];
                renderObject.transform.position = streetscapegeometry.pose.position;
                renderObject.transform.rotation = streetscapegeometry.pose.rotation;
            }
        }

        /// <summary>
        ///     Destroys the render object associated with the
        ///     <c>
        ///         <see cref="ARStreetscapeGeometry" />
        ///     </c>
        ///     .
        /// </summary>
        /// <param name="streetscapegeometry">
        ///     The
        ///     <c>
        ///         <see cref="ARStreetscapeGeometry" />
        ///     </c>
        ///     containing the render object to be destroyed.
        /// </param>
        private void DestroyRenderObject(ARStreetscapeGeometry streetscapegeometry)
        {
            if (_streetscapegeometryGOs.ContainsKey(streetscapegeometry.trackableId))
            {
                var geometry = _streetscapegeometryGOs[streetscapegeometry.trackableId];
                _streetscapegeometryGOs.Remove(streetscapegeometry.trackableId);
                Destroy(geometry);
            }
        }

        /// <summary>
        ///     Destroys all stored
        ///     <c>
        ///         <see cref="ARStreetscapeGeometry" />
        ///     </c>
        ///     render objects.
        /// </summary>
        private void DestroyAllRenderObjects()
        {
            var keys = _streetscapegeometryGOs.Keys;
            foreach (var key in keys)
            {
                var renderObject = _streetscapegeometryGOs[key];
                Destroy(renderObject);
            }

            _streetscapegeometryGOs.Clear();
        }

        /// <summary>
        ///     Activate or deactivate all UI elements on the anchor setting Panel.
        /// </summary>
        /// <param name="state">A bool value to determine if the anchor settings panel is visible.
        private void SetAnchorPanelState(bool state)
        {
            AnchorSettingPanel.gameObject.SetActive(state);
            GeospatialAnchorToggle.gameObject.SetActive(state);
            TerrainAnchorToggle.gameObject.SetActive(state);
            RooftopAnchorToggle.gameObject.SetActive(state);
        }

        private IEnumerator CheckRooftopPromise(ResolveAnchorOnRooftopPromise promise,
            GeospatialAnchorHistory history)
        {
            yield return promise;

            var result = promise.Result;
            if (result.RooftopAnchorState == RooftopAnchorState.Success &&
                result.Anchor)
            {
                // Adjust the scale of the prefab anchor object to maintain visibility when it is
                // far away.
                // result.Anchor.gameObject.transform.localScale *= GetRooftopAnchorScale(result.Anchor.gameObject.transform.position,Camera.main.transform.position);


                // Inserted Code
                GameObject theAnchorObject = null;
                var poiM = FindAnyObjectByType<PointOfInterestManager>();
                var pointOfInterests = poiM.poiData.pointOfInterests;
                var name = "";
                foreach (var loc in pointOfInterests)
                {
                    Debug.Log("Checking X: " + loc.title);
                    Debug.Log("Checking X: " + loc.coordinates.latitude + ", " + loc.coordinates.longitude);
                    Debug.Log("Checking X: " + history.Latitude + ", " + history.Longitude);
                    Debug.Log("Checking X Result: " +
                                    (Math.Abs(loc.coordinates.latitude - history.Latitude) < 0.000001 &&
                                     Math.Abs(loc.coordinates.longitude - history.Longitude) < 0.000001));
                    if (Math.Abs(loc.coordinates.latitude - history.Latitude) < 0.000001 &&
                        Math.Abs(loc.coordinates.longitude - history.Longitude) < 0.000001)
                        name = loc.title;

                    foreach (var subloc in loc.subPointOfInterests)
                        if (Math.Abs(subloc.coordinates.latitude - history.Latitude) < 0.000001 &&
                            Math.Abs(subloc.coordinates.longitude - history.Longitude) < 0.000001)
                        {
                            theAnchorObject = subloc.gameObjectLocation;
                            name = subloc.title;
                            break;
                        }
                }

                GameObject anchorGO;
                if (theAnchorObject)
                {
                    anchorGO = Instantiate(SmallRoofTopPrefab,
                        result.Anchor.gameObject.transform);

                    theAnchorObject.SetActive(true);
                    theAnchorObject.transform.position = anchorGO.transform.position;
                    theAnchorObject.transform.parent = anchorGO.transform;
                }
                else
                {
                    // Inserted Code end
                    anchorGO = Instantiate(RoofTopPrefab,
                        result.Anchor.gameObject.transform);
                }

                anchorGO.GetComponent<ARPointOfInterest>()?.SetText(name);
                anchorGO.transform.parent = result.Anchor.gameObject.transform;
                _anchorObjects.Add(result.Anchor.gameObject);
                _historyCollection.Collection.Add(history);

                SnackBarText.text = GetDisplayStringForAnchorPlacedSuccess();

                ClearAllButton.gameObject.SetActive(_anchorObjects.Count > 0);
                SaveGeospatialAnchorHistory();
            }
            else
            {
                SnackBarText.text = GetDisplayStringForAnchorPlacedFailure();
            }
        }

        private IEnumerator CheckTerrainPromise(ResolveAnchorOnTerrainPromise promise,
            GeospatialAnchorHistory history)
        {
            yield return promise;

            var result = promise.Result;
            if (result.TerrainAnchorState == TerrainAnchorState.Success &&
                result.Anchor)
            {
                var anchorGO = Instantiate(TerrainPrefab,
                    result.Anchor.gameObject.transform);
                anchorGO.transform.parent = result.Anchor.gameObject.transform;


                // Inserted Code
                GameObject theAnchorObject = null;
                var poiM = FindAnyObjectByType<PointOfInterestManager>();
                var pointOfInterests = poiM.poiData.pointOfInterests;
                foreach (var loc in pointOfInterests)
                {
                    DebugEditor.Log("Checking: " + loc.title);
                    DebugEditor.Log("Checking: " + loc.coordinates.latitude + ", " + loc.coordinates.longitude);
                    DebugEditor.Log("Checking: " + history.Latitude + ", " + history.Longitude);
                    DebugEditor.Log("Checking Result: " +
                                    (Math.Abs(loc.coordinates.latitude - history.Latitude) < 0.000001 &&
                                     Math.Abs(loc.coordinates.longitude - history.Longitude) < 0.000001));
                    if (Math.Abs(loc.coordinates.latitude - history.Latitude) < 0.000001 &&
                        Math.Abs(loc.coordinates.longitude - history.Longitude) < 0.000001)
                    {
                        theAnchorObject = loc.gameObjectLocation;
                        DebugEditor.Log("Checking Relocated: " + loc.title);
                        break;
                    }
                    /*
                    foreach (var subloc in loc.subPointOfInterests)
                        if (subloc.coordinates.latitude == history.Latitude &&
                            subloc.coordinates.longitude == history.Longitude)
                        {
                            theAnchorObject = subloc.gameObjectLocation;
                            Debug.Log("Checking Relocated: " + subloc.title);
                            break;
                        }
                        */
                }

                if (theAnchorObject)
                {
                    theAnchorObject.SetActive(true);
                    theAnchorObject.transform.position = anchorGO.transform.position;
                    theAnchorObject.transform.parent = anchorGO.transform;
                }
                // Inserted Code end

                _anchorObjects.Add(result.Anchor.gameObject);
                _historyCollection.Collection.Add(history);

                SnackBarText.text = GetDisplayStringForAnchorPlacedSuccess();

                ClearAllButton.gameObject.SetActive(_anchorObjects.Count > 0);
                SaveGeospatialAnchorHistory();
            }
            else
            {
                ReturnWithReason(GetDisplayStringForAnchorPlacedFailure());
                SnackBarText.text = GetDisplayStringForAnchorPlacedFailure();
            }
        }

        public static float GetRooftopAnchorScale(Vector3 anchor, Vector3 camera, bool isSmallAnchor)
        {
            // Return the scale in range [1, 2] after mapping a distance between camera and anchor
            // to [50, 200].
            var distance =
                Mathf.Sqrt(
                    Mathf.Pow(anchor.x - camera.x, 2.0f)
                    + Mathf.Pow(anchor.y - camera.y, 2.0f)
                    + Mathf.Pow(anchor.z - camera.z, 2.0f));
            var mapDistance = Mathf.Min(Mathf.Max(minDistance, distance), maxDistance);
            DebugEditor.Log("Map Distance: " + mapDistance);
            return (mapDistance - minDistance) / (maxDistance - minDistance) + 1f;
        }

        private void PlaceAnchorByScreenTap(Vector2 position)
        {
            if (_streetscapeGeometryVisibility)
            {
                // Raycast against streetscapeGeometry.
                List<XRRaycastHit> hitResults = new();
                if (RaycastManager.RaycastStreetscapeGeometry(position, ref hitResults))
                {
                    if (_anchorType == AnchorType.Rooftop || _anchorType == AnchorType.Terrain)
                    {
                        var streetscapeGeometry =
                            StreetscapeGeometryManager.GetStreetscapeGeometry(
                                hitResults[0].trackableId);
                        if (streetscapeGeometry == null) return;

                        if (_streetscapegeometryGOs.ContainsKey(streetscapeGeometry.trackableId))
                        {
                            Pose modifiedPose = new(hitResults[0].pose.position,
                                Quaternion.LookRotation(Vector3.right, Vector3.up));

                            var history =
                                CreateHistory(modifiedPose, _anchorType);

                            // Anchor returned will be null, the coroutine will handle creating
                            // the anchor when the promise is done.
                            PlaceARAnchor(history, modifiedPose, hitResults[0].trackableId);
                        }
                    }
                    else
                    {
                        var history = CreateHistory(hitResults[0].pose,
                            _anchorType);
                        var anchor = PlaceARAnchor(history, hitResults[0].pose,
                            hitResults[0].trackableId);
                        if (anchor != null) _historyCollection.Collection.Add(history);

                        ClearAllButton.gameObject.SetActive(_anchorObjects.Count > 0);
                        SaveGeospatialAnchorHistory();
                    }
                }

                return;
            }

            // Raycast against detected planes.
            List<ARRaycastHit> planeHitResults = new();
            RaycastManager.Raycast(
                position, planeHitResults, TrackableType.Planes | TrackableType.FeaturePoint);
            if (planeHitResults.Count > 0)
            {
                var history = CreateHistory(planeHitResults[0].pose,
                    _anchorType);

                if (_anchorType == AnchorType.Rooftop)
                {
                    // The coroutine will create the anchor when the promise is done.
                    var eunRotation = CreateRotation(history);
                    var rooftopPromise =
                        AnchorManager.ResolveAnchorOnRooftopAsync(
                            history.Latitude, history.Longitude,
                            0, eunRotation);

                    StartCoroutine(CheckRooftopPromise(rooftopPromise, history));
                    return;
                }

                var anchor = PlaceGeospatialAnchor(history);
                if (anchor != null) _historyCollection.Collection.Add(history);

                ClearAllButton.gameObject.SetActive(_anchorObjects.Count > 0);
                SaveGeospatialAnchorHistory();
            }
        }

        private GeospatialAnchorHistory CreateHistory(Pose pose, AnchorType anchorType)
        {
            var geospatialPose = EarthManager.Convert(pose);

            GeospatialAnchorHistory history = new(
                geospatialPose.Latitude, geospatialPose.Longitude, geospatialPose.Altitude,
                anchorType, geospatialPose.EunRotation);
            return history;
        }

        private Quaternion CreateRotation(GeospatialAnchorHistory history)
        {
            var eunRotation = history.EunRotation;
            if (eunRotation == Quaternion.identity)
                // This history is from a previous app version and EunRotation was not used.
                eunRotation =
                    Quaternion.AngleAxis(180f - (float)history.Heading, Vector3.up);

            return eunRotation;
        }

        private ARAnchor PlaceARAnchor(GeospatialAnchorHistory history, Pose pose = new(),
            TrackableId trackableId = new())
        {
            var eunRotation = CreateRotation(history);
            ARAnchor anchor = null;
            switch (history.AnchorType)
            {
                case AnchorType.Rooftop:
                    var rooftopPromise =
                        AnchorManager.ResolveAnchorOnRooftopAsync(
                            history.Latitude, history.Longitude,
                            history.Altitude, eunRotation);

                    StartCoroutine(CheckRooftopPromise(rooftopPromise, history));
                    return null;

                case AnchorType.Terrain:
                    var terrainPromise =
                        AnchorManager.ResolveAnchorOnTerrainAsync(
                            history.Latitude, history.Longitude,
                            history.Altitude, eunRotation);

                    StartCoroutine(CheckTerrainPromise(terrainPromise, history));
                    return null;

                case AnchorType.Geospatial:
                    var streetscapegeometry =
                        StreetscapeGeometryManager.GetStreetscapeGeometry(trackableId);
                    if (streetscapegeometry != null)
                        anchor = StreetscapeGeometryManager.AttachAnchor(
                            streetscapegeometry, pose);

                    if (anchor)
                    {
                        _anchorObjects.Add(anchor.gameObject);
                        _historyCollection.Collection.Add(history);
                        ClearAllButton.gameObject.SetActive(_anchorObjects.Count > 0);
                        SaveGeospatialAnchorHistory();

                        SnackBarText.text = GetDisplayStringForAnchorPlacedSuccess();
                    }
                    else
                    {
                        SnackBarText.text = GetDisplayStringForAnchorPlacedFailure();
                    }

                    break;
            }

            return anchor;
        }

        private ARGeospatialAnchor PlaceGeospatialAnchor(
            GeospatialAnchorHistory history)
        {
            var terrain = history.AnchorType == AnchorType.Terrain;
            var eunRotation = CreateRotation(history);
            ARGeospatialAnchor anchor = null;

            GameObject theAnchorObject = null;
            if (terrain)
            {
                // Anchor returned will be null, the coroutine will handle creating the
                // anchor when the promise is done.
                var promise =
                    AnchorManager.ResolveAnchorOnTerrainAsync(
                        history.Latitude, history.Longitude,
                        history.Altitude, eunRotation);

                StartCoroutine(CheckTerrainPromise(promise, history));
                return null;
            }

            anchor = AnchorManager.AddAnchor(
                history.Latitude, history.Longitude, history.Altitude, eunRotation);

            var poiM = FindAnyObjectByType<PointOfInterestManager>();
            var pointOfInterests = poiM.poiData.pointOfInterests;
            foreach (var loc in pointOfInterests)
            {
                DebugEditor.Log("Checking: " + loc.title);
                DebugEditor.Log("Checking: " + loc.coordinates.latitude + ", " + loc.coordinates.longitude);
                DebugEditor.Log("Checking: " + history.Latitude + ", " + history.Longitude);
                DebugEditor.Log("Checking Result: " + (loc.coordinates.latitude == history.Latitude &&
                                                       loc.coordinates.longitude == history.Longitude));
                if (loc.coordinates.latitude == history.Latitude && loc.coordinates.longitude == history.Longitude)
                {
                    theAnchorObject = loc.gameObjectLocation;
                    DebugEditor.Log("Checking Relocated: " + loc.title);
                    break;
                }
            }

            if (anchor)
            {
                var anchorGO = history.AnchorType == AnchorType.Geospatial
                    ? Instantiate(GeospatialPrefab, anchor.transform)
                    : Instantiate(TerrainPrefab, anchor.transform);
                if (theAnchorObject)
                {
                    theAnchorObject.SetActive(true);
                    theAnchorObject.transform.position = anchorGO.transform.position;
                    theAnchorObject.transform.parent = anchorGO.transform;
                }

                anchor.gameObject.SetActive(!terrain);
                anchorGO.transform.parent = anchor.gameObject.transform;

                _anchorObjects.Add(anchor.gameObject);
                SnackBarText.text = GetDisplayStringForAnchorPlacedSuccess();
            }
            else
            {
                SnackBarText.text = GetDisplayStringForAnchorPlacedFailure();
            }

            return anchor;
        }

        private void ResolveHistory()
        {
            if (!_shouldResolvingHistory) return;

            _shouldResolvingHistory = false;
            foreach (var history in _historyCollection.Collection)
                switch (history.AnchorType)
                {
                    case AnchorType.Rooftop:
                        PlaceARAnchor(history);
                        break;
                    case AnchorType.Terrain:
                        PlaceARAnchor(history);
                        break;
                    default:
                        PlaceGeospatialAnchor(history);
                        break;
                }

            ClearAllButton.gameObject.SetActive(_anchorObjects.Count > 0);
            SnackBarText.text = string.Format("{0} anchor(s) set from history.",
                _anchorObjects.Count);
        }

        private void AddMyAnchorTerrain(PointOfInterestData.Coordinates myAnchor)
        {
            GeospatialAnchorHistory myAnchorHistory = new(myAnchor.latitude, myAnchor.longitude,
                myAnchor.metersOverGround,
                AnchorType.Terrain, Quaternion.identity);
            _historyCollection.Collection.Add(myAnchorHistory);
        }

        private void AddMyAnchorRoofTop(PointOfInterestData.Coordinates myAnchor)
        {
            GeospatialAnchorHistory myAnchorHistory = new(myAnchor.latitude, myAnchor.longitude,
                myAnchor.metersOverGround,
                AnchorType.Rooftop, Quaternion.identity);
            _historyCollection.Collection.Add(myAnchorHistory);
        }

        private void AddMyAnchorGeospatial(PointOfInterestData.Coordinates myAnchor)
        {
            GeospatialAnchorHistory myAnchorHistory = new(myAnchor.latitude, myAnchor.longitude, myAnchor.altitude,
                AnchorType.Geospatial, Quaternion.identity);
            _historyCollection.Collection.Add(myAnchorHistory);
        }

        private void LoadMyLocations()
        {
            var poiM = FindAnyObjectByType<PointOfInterestManager>();
            var pointOfInterests = poiM.poiData.pointOfInterests;

            foreach (var location in pointOfInterests)
            {
                DebugEditor.Log("Checking Loading: " + location.title);

                AddMyAnchorTerrain(location.coordinates);
                AddMyAnchorRoofTop(location.coordinates);
                foreach (var subPointOfInterest in location.subPointOfInterests)
                    AddMyAnchorRoofTop(subPointOfInterest.coordinates);
            }
        }

        private void LoadGeospatialAnchorHistory()
        {
            DebugEditor.Log("Checking Loading Geospatial Anchor History");
            if (PlayerPrefs.HasKey(_persistentGeospatialAnchorsStorageKey))
            {
                _historyCollection = new GeospatialAnchorHistoryCollection();
                //_historyCollection = JsonUtility.FromJson<GeospatialAnchorHistoryCollection>(
                //    PlayerPrefs.GetString(_persistentGeospatialAnchorsStorageKey));

                LoadMyLocations();
                LoadedInformation = JsonUtility.ToJson(_historyCollection);

                // Remove all records created more than 24 hours and update stored history.
                //var current = DateTime.Now;
                //_historyCollection.Collection.RemoveAll(
                //    data => current.Subtract(data.CreatedTime).Days > 0);
                //PlayerPrefs.SetString(_persistentGeospatialAnchorsStorageKey,
                //    JsonUtility.ToJson(_historyCollection));
                PlayerPrefs.Save();
            }
            else
            {
                DebugEditor.Log("Checking No Geospatial Anchor History Found");
                _historyCollection = new GeospatialAnchorHistoryCollection();
                LoadMyLocations();
            }
        }

        private void SaveGeospatialAnchorHistory()
        {
            // Sort the data from latest record to earliest record.
            _historyCollection.Collection.Sort((left, right) =>
                right.CreatedTime.CompareTo(left.CreatedTime));

            // Remove the earliest data if the capacity exceeds storage limit.
            if (_historyCollection.Collection.Count > _storageLimit)
                _historyCollection.Collection.RemoveRange(
                    _storageLimit, _historyCollection.Collection.Count - _storageLimit);

            PlayerPrefs.SetString(
                _persistentGeospatialAnchorsStorageKey, JsonUtility.ToJson(_historyCollection));
            PlayerPrefs.Save();
        }

        private void SwitchToARView(bool enable)
        {
            _isInARView = enable;
            Origin.gameObject.SetActive(enable);
            Session.gameObject.SetActive(enable);
            ARCoreExtensions.gameObject.SetActive(enable);
            ARViewCanvas.SetActive(enable);
            if (enable && _asyncCheck == null)
            {
                _asyncCheck = AvailabilityCheck();
                StartCoroutine(_asyncCheck);
            }
        }

        private IEnumerator AvailabilityCheck()
        {
            if (ARSession.state == ARSessionState.None) yield return ARSession.CheckAvailability();

            // Waiting for ARSessionState.CheckingAvailability.
            yield return null;

            if (ARSession.state == ARSessionState.NeedsInstall) yield return ARSession.Install();

            // Waiting for ARSessionState.Installing.
            yield return null;
#if UNITY_ANDROID

            if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                DebugEditor.Log("Requesting camera permission.");
                Permission.RequestUserPermission(Permission.Camera);
                yield return new WaitForSeconds(3.0f);
            }

            if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                // User has denied the request.
                DebugEditor.LogWarning(
                    "Failed to get the camera permission. VPS availability check isn't available.");
                yield break;
            }
#endif

            while (_waitingForLocationService) yield return null;

            if (Input.location.status != LocationServiceStatus.Running)
            {
                DebugEditor.LogWarning(
                    "Location services aren't running. VPS availability check is not available.");
                yield break;
            }

            // Update event is executed before coroutines so it checks the latest error states.
            if (_isReturning) yield break;

            var location = Input.location.lastData;
            var vpsAvailabilityPromise =
                AREarthManager.CheckVpsAvailabilityAsync(location.latitude, location.longitude);
            yield return vpsAvailabilityPromise;

            Debug.LogFormat("VPS Availability at ({0}, {1}): {2}",
                location.latitude, location.longitude, vpsAvailabilityPromise.Result);
        }

        private IEnumerator StartLocationService()
        {
            _waitingForLocationService = true;
#if UNITY_ANDROID
            if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
            {
                Debug.Log("Requesting the fine location permission.");
                Permission.RequestUserPermission(Permission.FineLocation);
                yield return new WaitForSeconds(3.0f);
            }
#endif

            if (!Input.location.isEnabledByUser)
            {
                Debug.Log("Location service is disabled by the user.");
                _waitingForLocationService = false;
                yield break;
            }

            Debug.Log("Starting location service.");
            Input.location.Start();

            while (Input.location.status == LocationServiceStatus.Initializing) yield return null;

            _waitingForLocationService = false;
            if (Input.location.status != LocationServiceStatus.Running)
            {
                Debug.LogWarningFormat(
                    "Location service ended with {0} status.", Input.location.status);
                Input.location.Stop();
            }
        }

        private void LifecycleUpdate()
        {
            // Pressing 'back' button quits the app.
            if (Input.GetKeyUp(KeyCode.Escape))
            {
                PlayerPrefs.Save();
                Application.Quit();
            }

            if (_isReturning) return;

            // Only allow the screen to sleep when not tracking.
            var sleepTimeout = SleepTimeout.NeverSleep;
            if (ARSession.state != ARSessionState.SessionTracking) sleepTimeout = SleepTimeout.SystemSetting;

            Screen.sleepTimeout = sleepTimeout;

            // Quit the app if ARSession is in an error status.
            var returningReason = string.Empty;
            if (ARSession.state != ARSessionState.CheckingAvailability &&
                ARSession.state != ARSessionState.Ready &&
                ARSession.state != ARSessionState.SessionInitializing &&
                ARSession.state != ARSessionState.SessionTracking)
                returningReason =
                    $"Geospatial sample encountered an ARSession error state {ARSession.state.ToString()}.\n" +
                    "Please restart the app.";
            else if (Input.location.status == LocationServiceStatus.Failed)
                returningReason =
                    "Geospatial sample failed to start location service.\n" +
                    "Please restart the app and grant the fine location permission.";
            else if (!Origin || !Session || !ARCoreExtensions)
                returningReason = "Geospatial sample failed due to missing AR Components.";

            ReturnWithReason(returningReason);
        }

        private void ReturnWithReason(string reason)
        {
            if (string.IsNullOrEmpty(reason)) return;

            GeometryToggle.gameObject.SetActive(false);
            AnchorSettingButton.gameObject.SetActive(false);
            AnchorSettingPanel.gameObject.SetActive(false);
            GeospatialAnchorToggle.gameObject.SetActive(false);
            TerrainAnchorToggle.gameObject.SetActive(false);
            RooftopAnchorToggle.gameObject.SetActive(false);
            ClearAllButton.gameObject.SetActive(false);
            InfoPanel.SetActive(false);

            Debug.LogError(reason);
            SnackBarText.text = reason;
            PermissionRequest.ShowToastMessage(reason + "\n Restarting app now.");
            _isReturning = true;
            Invoke(nameof(QuitApplication), _errorDisplaySeconds);
        }

        private void QuitApplication()
        {
            Application.Quit();
        }

        private void UpdateDebugInfo()
        {
            if (!Debug.isDebugBuild || !EarthManager) return;
            if (!DebugText.isActiveAndEnabled) return;

            var pose = EarthManager.EarthState == EarthState.Enabled &&
                       EarthManager.EarthTrackingState == TrackingState.Tracking
                ? EarthManager.CameraGeospatialPose
                : new GeospatialPose();
            if (_supported == FeatureSupported.Unknown)
                _supported = EarthManager.IsGeospatialModeSupported(GeospatialMode.Enabled);
            DebugText.text =
                $"IsReturning: {_isReturning}\n" +
                $"IsLocalizing: {_isLocalizing}\n" +
                $"SessionState: {ARSession.state}\n" +
                $"LocationServiceStatus: {Input.location.status}\n" +
                $"FeatureSupported: {_supported}\n" +
                $"EarthState: {EarthManager.EarthState}\n" +
                $"EarthTrackingState: {EarthManager.EarthTrackingState}\n" +
                $"  LAT/LNG: {pose.Latitude:F6}, {pose.Longitude:F6}\n" +
                $"  HorizontalAcc: {pose.HorizontalAccuracy:F6}\n" +
                $"  ALT: {pose.Altitude:F2}\n" +
                $"  VerticalAcc: {pose.VerticalAccuracy:F2}\n" +
                $". EunRotation: {pose.EunRotation:F2}\n" +
                $"  OrientationYawAcc: {pose.OrientationYawAccuracy:F2}" +
                $". Json: \n{LoadedInformation}\n" +
                $". HistoryCount: \n{_historyCollection.Collection.Count}\n";
        }

        /// <summary>
        ///     Generates the placed anchor success string for the UI display.
        /// </summary>
        /// <returns> The string for the UI display for successful anchor placement.</returns>
        private string GetDisplayStringForAnchorPlacedSuccess()
        {
            return string.Format(
                "{0} / {1} Anchor(s) Set!", _anchorObjects.Count, _storageLimit);
        }

        /// <summary>
        ///     Generates the placed anchor failure string for the UI display.
        /// </summary>
        /// <returns> The string for the UI display for a failed anchor placement.</returns>
        private string GetDisplayStringForAnchorPlacedFailure()
        {
            return $"Failed to set a {_anchorType} anchor!";
        }

        [Serializable]
        private struct location
        {
            public string name;
            public GameObject obj;
        }
    }
}