using System.Diagnostics;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

public static class DebugEditor
{
    // ReSharper disable Unity.PerformanceAnalysis
    // ReSharper disable Unity.PerformanceAnalysis
    public static void Log(object message, Object context = null)
    {
        if(Debug.isDebugBuild) Debug.Log(message, context);
    }
    // ReSharper disable Unity.PerformanceAnalysis
    // ReSharper disable Unity.PerformanceAnalysis
    public static void LogWarning(object message, Object context = null)
    {
        if(Debug.isDebugBuild) Debug.LogWarning(message, context);
    } 
    
    // ReSharper disable Unity.PerformanceAnalysis
    // ReSharper disable Unity.PerformanceAnalysis
    public static void LogError(object message, Object context = null)
    { 
        Debug.LogError(message, context);
    }
}