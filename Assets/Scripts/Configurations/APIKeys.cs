using UnityEngine;

namespace i5.LLM_AR_Tourguide
{
    [CreateAssetMenu(fileName = "APIKeys", menuName = "TourGuide/APIKeys", order = 0)]
    public class APIKeys : ScriptableObject
    {
        public string _GoogleCloudTextToSpeechAPIKey;
        public string _GoogleCloudGeminiAPIKey;
    }
}
