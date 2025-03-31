using UnityEngine;

namespace i5.LLM_AR_Tourguide.UI_Scripts
{
    public class RotationAnimation : MonoBehaviour
    {
        private readonly float rotationSpeed = 360f;
        private bool increasing = true;
        private float speedMultiplier = 1f;

        // Update is called once per frame
        private void Update()
        {
            transform.Rotate(Vector3.forward, rotationSpeed * speedMultiplier * Time.deltaTime);

            // Adjust speed multiplier
            if (increasing)
            {
                speedMultiplier += Time.deltaTime;
                if (speedMultiplier >= 1.7f) increasing = false;
            }
            else
            {
                speedMultiplier -= Time.deltaTime;
                if (speedMultiplier <= 0.8f) increasing = true;
            }
        }
    }
}