using i5.LLM_AR_Tourguide.Guide_Scripts;
using UnityEngine;

namespace i5.LLM_AR_TourGuide
{
    public class ARArrow : MonoBehaviour
    {
        public Transform rotationPoint;
        public float rotationSpeed = 5f;

        [Tooltip("How quickly the object moves. Larger values are faster.")]
        public float moveResponsiveness = 2f;

        [Tooltip("How far off-screen the object should move.  A value of 1 means fully off-screen.")]
        public float offScreenDistance = 1.5f;

        [Tooltip(
            "Dot product threshold to determine if the object should be on-screen or off-screen.  Higher values mean the camera must be looking more directly at the guide.")]
        [Range(-1f, 1f)]
        public float baseLookAtThreshold = 0.8f; // Renamed for clarity

        [Tooltip("Distance at which the threshold starts to reduce.")]
        public float thresholdReductionStartDistance = 10f;

        [Tooltip(
            "How much the threshold reduces per unit distance beyond the start distance.  Larger values mean a faster reduction.")]
        public float thresholdReductionRate = 0.05f;

        public Vector3 pointingAxis = Vector3.up;

        public Transform target;

        private GuideManager guideManager;
        private Vector3 initialLocalPosition;
        private Transform initialParent;

        private Camera mainCamera;

        private void Start()
        {
            mainCamera = Camera.main;
            if (!rotationPoint)
            {
                rotationPoint = transform;
                DebugEditor.LogWarning("Rotation Point not assigned. Using object's center.");
            }

            guideManager = FindAnyObjectByType<GuideManager>();
            initialLocalPosition = transform.localPosition;
            initialParent = transform.parent;
        }

        private void Update()
        {
            if (!target || !rotationPoint) return;

            var targetDirection =
                target.position + Vector3.up * 1f - rotationPoint.position; // Use guideTransform variable, add up

            // Calculate the target rotation using LookRotation, but specify the "up" direction.
            var targetRotation =
                Quaternion.LookRotation(targetDirection, transform.up); //Initially assume up is world up

            // Calculate a rotation that orients the desired axis towards the target.
            // We get the current "forward" vector of the desired pointing axis.
            var currentPointingDirection = transform.TransformDirection(pointingAxis);

            // Find the rotation needed to align the current pointing direction with the target direction.
            var adjustRotation = Quaternion.FromToRotation(currentPointingDirection, targetDirection);

            // Combine the rotations: First apply the adjustment, THEN the LookRotation.
            targetRotation = adjustRotation * transform.rotation;


            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);


            // --- Movement Logic (Threshold-Based, with Distance Reduction) ---

            var cameraToGuideDirection = (target.position - mainCamera.transform.position).normalized;
            var dotProduct = Vector3.Dot(mainCamera.transform.forward, cameraToGuideDirection);
            var distance = Vector3.Distance(mainCamera.transform.position, target.position);

            // Calculate the adjusted threshold.
            var adjustedThreshold = baseLookAtThreshold;
            if (distance > thresholdReductionStartDistance)
            {
                var distanceBeyondStart = distance - thresholdReductionStartDistance;
                adjustedThreshold += distanceBeyondStart * thresholdReductionRate;
                adjustedThreshold = Mathf.Clamp(adjustedThreshold, -1f, 1f); // Ensure it stays within valid range
            }

            // DebugEditor.Log($"Adjusted Threshold: {adjustedThreshold}");

            Vector3 targetWorldPosition;
            if (dotProduct > adjustedThreshold)
            {
                // Move off-screen
                targetWorldPosition = initialParent
                    ? initialParent.TransformPoint(initialLocalPosition)
                    : initialLocalPosition;
                targetWorldPosition += -mainCamera.transform.right * offScreenDistance;
            }
            else
            {
                // Move on-screen
                targetWorldPosition = initialParent
                    ? initialParent.TransformPoint(initialLocalPosition)
                    : initialLocalPosition;
            }

            var targetLocalPosition = initialParent
                ? initialParent.InverseTransformPoint(targetWorldPosition)
                : targetWorldPosition;
            transform.localPosition =
                Vector3.Lerp(transform.localPosition, targetLocalPosition, moveResponsiveness * Time.deltaTime);


            //DebugEditor.Log($"Dot: {dotProduct}, Distance: {distance}, Adjusted Threshold: {adjustedThreshold}, isOffScreen: {isOffScreen}");
        }
    }
}