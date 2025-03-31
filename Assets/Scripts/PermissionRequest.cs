using System.Collections;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.UI;

public class PermissionRequest : MonoBehaviour
{
    public Toggle cameraPermissionToggle;
    public Toggle locationPermissionToggle;


    public void OnRequestLocationPermission()
    {
        StartCoroutine(RequestLocationPermission());
    }

    public void OnRequestCameraPermission()
    {
        StartCoroutine(RequestCameraPermission());
    }

    internal void PermissionCallbacks_PermissionDeniedAndDontAskAgain(string permissionName)
    {
        permissionName = permissionName.Replace("android.permission.", "").Trim().ToLower();
        DebugEditor.Log($"{permissionName} PermissionDeniedAndDontAskAgain");
        locationPermissionToggle.isOn = Permission.HasUserAuthorizedPermission(Permission.FineLocation);
        cameraPermissionToggle.isOn = Permission.HasUserAuthorizedPermission(Permission.Camera);
        ShowToastMessage($"Please enable {permissionName} permissions in system settings.");
    }

    internal void PermissionCallbacks_PermissionGranted(string permissionName)
    {
        permissionName = permissionName.Replace("android.permission.", "").Trim().ToLower();
        DebugEditor.Log($"{permissionName} PermissionCallbacks_PermissionGranted");
        locationPermissionToggle.isOn = Permission.HasUserAuthorizedPermission(Permission.FineLocation);
        cameraPermissionToggle.isOn = Permission.HasUserAuthorizedPermission(Permission.Camera);
        ShowToastMessage("Thanks!");
    }

    internal void PermissionCallbacks_PermissionDenied(string permissionName)
    {
        permissionName = permissionName.Replace("android.permission.", "").Trim().ToLower();
        DebugEditor.Log($"{permissionName} PermissionCallbacks_PermissionDenied");
        locationPermissionToggle.isOn = Permission.HasUserAuthorizedPermission(Permission.FineLocation);
        cameraPermissionToggle.isOn = Permission.HasUserAuthorizedPermission(Permission.Camera);
        ShowToastMessage($"Please enable {permissionName} permissions in system settings.");
    }

    internal void PermissionCallbacks_PermissionDismissed(string permissionName)
    {
        permissionName = permissionName.Replace("android.permission.", "").Trim().ToLower();
        DebugEditor.Log($"{permissionName} PermissionCallbacks_PermissionDismissed");
        locationPermissionToggle.isOn = Permission.HasUserAuthorizedPermission(Permission.FineLocation);
        cameraPermissionToggle.isOn = Permission.HasUserAuthorizedPermission(Permission.Camera);
    }

    // Method to request location permission on Android and iOS, called by permission checkbox
    private IEnumerator RequestLocationPermission()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            PermissionCallbacks callbacks = new();
            callbacks.PermissionDenied += PermissionCallbacks_PermissionDenied;
            callbacks.PermissionGranted += PermissionCallbacks_PermissionGranted;
            //callbacks.PermissionDeniedAndDontAskAgain += PermissionCallbacks_PermissionDeniedAndDontAskAgain;
            callbacks.PermissionRequestDismissed += PermissionCallbacks_PermissionDismissed;
            Permission.RequestUserPermission(Permission.FineLocation, callbacks);
        }

#elif UNITY_IOS && !UNITY_EDITOR
                // iOS location permission request
                if (!Input.location.isEnabledByUser)
                {
                    // iOS specific code to request location permission
                    Input.location.Start();
                    yield return new WaitForEndOfFrame();
                    DebugEditor.Log("Location permission: " + Input.location.isEnabledByUser);
                }
                locationPermissionToggle.isOn = Input.location.isEnabledByUser;
                else
                {
                    yield return new WaitForEndOfFrame();
                    locationPermissionToggle.isOn = true;
                }
#endif
        yield return null;
    }

    // Method to request camera permission on Android and iOS, called by permission checkbox
    private IEnumerator RequestCameraPermission()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            PermissionCallbacks callbacks = new();
            callbacks.PermissionDenied += PermissionCallbacks_PermissionDenied;
            callbacks.PermissionGranted += PermissionCallbacks_PermissionGranted;
            //callbacks.PermissionDeniedAndDontAskAgain += PermissionCallbacks_PermissionDeniedAndDontAskAgain;
            callbacks.PermissionRequestDismissed += PermissionCallbacks_PermissionDismissed;
            Permission.RequestUserPermission(Permission.Camera, callbacks);
        }

#elif UNITY_IOS && !UNITY_EDITOR
            // iOS camera permission request
            if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
            {
                Application.RequestUserAuthorization(UserAuthorization.WebCam);
                yield return new WaitForEndOfFrame();
                DebugEditor.Log("Camera permission: " + Application.HasUserAuthorization(UserAuthorization.WebCam));
                cameraPermissionToggle.isOn = Application.HasUserAuthorization(UserAuthorization.WebCam);
            }
            else
            {
                yield return new WaitForEndOfFrame();
                cameraPermissionToggle.isOn = true;
            }
#endif
        yield return null;
    }


    /// <param name="message">Message string to show in the toast.</param>
    public static void ShowToastMessage(string message)
    {
        DebugEditor.Log("Toast: " + message);
#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidJavaClass unityPlayer = new("com.unity3d.player.UnityPlayer");
        AndroidJavaObject unityActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

        if (unityActivity != null)
        {
            AndroidJavaClass toastClass = new("android.widget.Toast");
            unityActivity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
            {
                AndroidJavaObject toastObject =
                    toastClass.CallStatic<AndroidJavaObject>("makeText", unityActivity, message, 0);
                toastObject.Call("show");
            }));
        }
#endif
#if UNITY_IOS
        Debug.LogError("iOS toast support not implemented.");
#endif
    }
}