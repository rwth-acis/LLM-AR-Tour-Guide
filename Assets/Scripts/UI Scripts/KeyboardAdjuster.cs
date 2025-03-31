using System.Collections;
using UnityEngine;

namespace i5.LLM_AR_Tourguide.UI_Scripts
{
    public class KeyboardAdjuster : MonoBehaviour
    {
        public GameObject[] objectsMovedUpByKeyboard;

#if UNITY_EDITOR
        private bool keyboardWasVisibleDebug;
        public bool keyboardisVisibleDebug;
#else
    private bool keyboardWasVisible;
#endif

        [SerializeField] private int keyboardHeight = 700;
        [SerializeField] private float animationDuration = 0.2f;


        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void Start()
        {
#if UNITY_EDITOR
            keyboardWasVisibleDebug = false;
#else
        keyboardWasVisible = false;
#endif
        }

        // Update is called once per frame
        private void Update()
        {
#if UNITY_EDITOR
            if (keyboardisVisibleDebug && !keyboardWasVisibleDebug)
            {
                foreach (var obj in objectsMovedUpByKeyboard)
                    StartCoroutine(AnimatePosition(obj, obj.transform.position,
                        new Vector3(obj.transform.position.x, obj.transform.position.y + keyboardHeight,
                            obj.transform.position.z), animationDuration));
                keyboardWasVisibleDebug = true;
            }
            else if (!keyboardisVisibleDebug && keyboardWasVisibleDebug)
            {
                foreach (var obj in objectsMovedUpByKeyboard)
                    StartCoroutine(AnimatePosition(obj, obj.transform.position,
                        new Vector3(obj.transform.position.x, obj.transform.position.y - keyboardHeight,
                            obj.transform.position.z), animationDuration));
                keyboardWasVisibleDebug = false;
            }
#else
        if (TouchScreenKeyboard.visible && !keyboardWasVisible)
        {
            foreach (GameObject obj in objectsMovedUpByKeyboard)
            {
                StartCoroutine(AnimatePosition(obj, obj.transform.position, new Vector3(obj.transform.position.x, obj.transform.position.y + keyboardHeight, obj.transform.position.z), animationDuration));
            }
            keyboardWasVisible = true;
        }
        else if (!TouchScreenKeyboard.visible && keyboardWasVisible)
        {
            foreach (GameObject obj in objectsMovedUpByKeyboard)
            {
                StartCoroutine(AnimatePosition(obj, obj.transform.position, new Vector3(obj.transform.position.x, obj.transform.position.y - keyboardHeight, obj.transform.position.z), animationDuration));
            }
            keyboardWasVisible = false;
        }
#endif
        }

        private IEnumerator AnimatePosition(GameObject obj, Vector3 start, Vector3 end, float duration)
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                var t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                obj.transform.position = Vector3.Lerp(start, end, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            obj.transform.position = end;
        }
    }
}