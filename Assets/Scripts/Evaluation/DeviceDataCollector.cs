using System;
using i5.LLM_AR_Tourguide.UI_Scripts;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.AdaptivePerformance;
using UnityEngine.AdaptivePerformance.Provider;

namespace i5.LLM_AR_Tourguide.Evaluation
{
    public class DeviceDataCollector : MonoBehaviour
    {
        private readonly float updateInterval = 60f; // 1 minute

        private Settings _settings;

        private IAdaptivePerformance ap;
        private string filePath;

        private float timer;

        // Record device data, like model name, name, manufacturer, Android version, etc.
        private void Start()
        {
            ap = Holder.Instance;

            if (ap == null || !ap.Active)
            {
                Debug.Log("[AP ClusterInfo] Adaptive Performance not active.");
                return;
            }

            foreach (Feature feature in Enum.GetValues(typeof(Feature)))
                if (!ap.SupportedFeature(feature))
                    Debug.Log("[AP ClusterInfo] Feature " + feature + " is not supported.");


            _settings = FindAnyObjectByType<Settings>();
            var deviceModel = SystemInfo.deviceModel;
            var deviceName = SystemInfo.deviceName;
            var operatingSystem = SystemInfo.operatingSystem;
            var processorType = SystemInfo.processorType;
            var graphicsDeviceName = SystemInfo.graphicsDeviceName;
            var graphicsDeviceVendor = SystemInfo.graphicsDeviceVendor;
            var graphicsDeviceVersion = SystemInfo.graphicsDeviceVersion;
            var graphicsMemorySize = SystemInfo.graphicsMemorySize.ToString();
            var graphicsShaderLevel = SystemInfo.graphicsShaderLevel.ToString();
            var systemMemorySize = SystemInfo.systemMemorySize.ToString();
            var processorCount = SystemInfo.processorCount.ToString();
            var processorFrequency = SystemInfo.processorFrequency.ToString();
            var supportedRenderTargetCount = SystemInfo.supportedRenderTargetCount.ToString();

            // Create json object from the data
            var deviceData = new DeviceData(deviceModel, deviceName, operatingSystem, processorType, graphicsDeviceName,
                graphicsDeviceVendor, graphicsDeviceVersion, graphicsMemorySize, graphicsShaderLevel, systemMemorySize,
                processorCount, processorFrequency, supportedRenderTargetCount);
            DebugEditor.Log("Device data uploaded" + JsonConvert.SerializeObject(deviceData));
            UploadManager.UploadData("DeviceDataStart", JsonConvert.SerializeObject(deviceData));
            RecordData();
        }

        // Every minute, record the device's location, thermal metrics and other things
        private void Update()
        {
            timer += Time.deltaTime;
            if (timer >= updateInterval)
            {
                timer = 0f;
                RecordData();
            }
        }

        private void RecordData()
        {
            if (!_settings) _settings = FindAnyObjectByType<Settings>();
            var batteryLevel = SystemInfo.batteryLevel;
            var batteryState = SystemInfo.batteryStatus;
            var increasedRevealSpeed = PlayerPrefs.GetInt("revealSpeed", 0);
            var debugMode = false;
            if (_settings)
                debugMode = _settings.DebugMode;
            // Timestamp
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var occlusionActive = PlayerPrefs.GetInt("occlusionManager", 1);

            if (!ap.Active) ap = Holder.Instance;

            var temperatureLevel = ap.ThermalStatus.ThermalMetrics.TemperatureLevel;
            var temperatureTrend = ap.ThermalStatus.ThermalMetrics.TemperatureTrend;
            var temperatureWarningLevel = ap.ThermalStatus.ThermalMetrics.WarningLevel;
            var performanceBottleneck = ap.PerformanceStatus.PerformanceMetrics.PerformanceBottleneck;
            var performanceCPU = ap.PerformanceStatus.PerformanceMetrics.CurrentCpuLevel;
            var performanceGPU = ap.PerformanceStatus.PerformanceMetrics.CurrentGpuLevel;
            var averageFrameTime = ap.PerformanceStatus.FrameTiming.AverageFrameTime;
            var averageCpuFrameTime = ap.PerformanceStatus.FrameTiming.AverageCpuFrameTime;
            var averageGpuFrameTime = ap.PerformanceStatus.FrameTiming.AverageGpuFrameTime;


            var deviceData = new DeviceDataContinuously(timestamp, batteryLevel, batteryState, increasedRevealSpeed,
                debugMode,
                temperatureLevel, temperatureTrend, temperatureWarningLevel, performanceBottleneck, performanceCPU,
                performanceGPU, occlusionActive, averageFrameTime, averageCpuFrameTime, averageGpuFrameTime);
            UploadManager.UploadData("DeviceDataUpdate", JsonConvert.SerializeObject(deviceData));
        }
    }

    public class DeviceDataContinuously
    {
        public DeviceDataContinuously(string timestamp, float batteryLevel, BatteryStatus batteryState,
            int increasedRevealSpeed,
            bool debugMode, float temperatureLevel, float temperatureTrend, WarningLevel temperatureWarningLevel,
            PerformanceBottleneck performanceBottleneck, int performanceCPU, int performanceGPU, int occlusionActive,
            float averageFrameTime, float averageCpuFrameTime, float averageGpuFrameTime)
        {
            Timestamp = timestamp;
            BatteryLevel = batteryLevel;
            BatteryState = batteryState;
            IncreasedRevealSpeed = increasedRevealSpeed;
            DebugMode = debugMode;
            TemperatureLevel = temperatureLevel;
            TemperatureTrend = temperatureTrend;
            TemperatureWarningLevel = temperatureWarningLevel;
            PerformanceBottleneck = performanceBottleneck;
            PerformanceCPU = performanceCPU;
            PerformanceGPU = performanceGPU;
            OcclusionActive = occlusionActive;
            AverageFrameTime = averageFrameTime;
            AverageCpuFrameTime = averageCpuFrameTime;
            AverageGpuFrameTime = averageGpuFrameTime;
        }

        public string Timestamp { get; }
        public float BatteryLevel { get; }
        public BatteryStatus BatteryState { get; }
        public int IncreasedRevealSpeed { get; }
        public bool DebugMode { get; }
        public float TemperatureLevel { get; }
        public float TemperatureTrend { get; }
        public WarningLevel TemperatureWarningLevel { get; }
        public PerformanceBottleneck PerformanceBottleneck { get; }
        public int PerformanceCPU { get; }
        public int PerformanceGPU { get; }

        public int OcclusionActive { get; }
        public float AverageFrameTime { get; }
        public float AverageCpuFrameTime { get; }
        public float AverageGpuFrameTime { get; }
    }

    public class DeviceData
    {
        public DeviceData(
            string deviceModel,
            string deviceName,
            string operatingSystem,
            string processorType,
            string graphicsDeviceName,
            string graphicsDeviceVendor,
            string graphicsDeviceVersion,
            string graphicsMemorySize,
            string graphicsShaderLevel,
            string systemMemorySize,
            string processorCount,
            string processorFrequency,
            string supportedRenderTargetCount)
        {
            DeviceModel = deviceModel;
            DeviceName = deviceName;
            OperatingSystem = operatingSystem;
            ProcessorType = processorType;
            GraphicsDeviceName = graphicsDeviceName;
            GraphicsDeviceVendor = graphicsDeviceVendor;
            GraphicsDeviceVersion = graphicsDeviceVersion;
            GraphicsMemorySize = graphicsMemorySize;
            GraphicsShaderLevel = graphicsShaderLevel;
            SystemMemorySize = systemMemorySize;
            ProcessorCount = processorCount;
            ProcessorFrequency = processorFrequency;
            SupportedRenderTargetCount = supportedRenderTargetCount;
        }

        public string DeviceModel { get; }
        public string DeviceName { get; }
        public string OperatingSystem { get; }
        public string ProcessorType { get; }
        public string GraphicsDeviceName { get; }
        public string GraphicsDeviceVendor { get; }
        public string GraphicsDeviceVersion { get; }
        public string GraphicsMemorySize { get; }
        public string GraphicsShaderLevel { get; }
        public string SystemMemorySize { get; }
        public string ProcessorCount { get; }
        public string ProcessorFrequency { get; }
        public string SupportedRenderTargetCount { get; }
    }
}