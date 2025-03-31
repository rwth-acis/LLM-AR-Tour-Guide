using System.IO;
using UnityEngine;

namespace i5.LLM_AR_Tourguide
{
    public class AutoEraser : MonoBehaviour
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void Start()
        {
            // Delete all player prefs
            PlayerPrefs.DeleteAll();
            // Delete all saved data in Application.persistentDataPath
            var di = new DirectoryInfo(Application.persistentDataPath);
            Debug.Log("Deleting all files in " + di.FullName);
            foreach (var file in di.GetFiles()) file.Delete();
        }

        // Update is called once per frame
        private void Update()
        {
        }
    }
}