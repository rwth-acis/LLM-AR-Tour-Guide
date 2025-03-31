using System;
using UnityEngine;

namespace i5.LLM_AR_Tourguide.Evaluation
{
    public class UploadManager : MonoBehaviour
    {
        public static void UploadData(string foldername, string data)
        {
            string userID;
            if (PlayerPrefs.HasKey("userID"))
            {
                userID = PlayerPrefs.GetString("userID");
            }
            else
            {
                userID = DateTime.Now.ToString("dd-MM-yyyy-HH-mm-ss") + Guid.NewGuid();
                PlayerPrefs.SetString("userID", userID);
                PlayerPrefs.Save();
            }

            var deviceIdentifier =
                (SystemInfo.deviceName ?? "UnknownDevice") + (SystemInfo.deviceModel ?? "UnknownModel");
            var folder = userID + deviceIdentifier + "/" + foldername;
            FirebaseCloudStorageUpload.uploadString(folder, data);
        }
    }
}