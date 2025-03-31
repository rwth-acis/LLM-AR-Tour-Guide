using System.Collections;
using Google.XR.ARCoreExtensions;
using i5.LLM_AR_Tourguide.Guide_Scripts;
using i5.LLM_AR_Tourguide.UI_Scripts;
using i5.VirtualAgents;
using i5.VirtualAgents.AgentTasks;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace i5.LLM_AR_TourGuide
{
    public class ARManager : MonoBehaviour
    {
        [SerializeField] private ARArrow arrow;

        [SerializeField] private LocalizationProgress localizationProgress;
        public ARAnchorManager AnchorManager;

        public GameObject TerrainAnchorPrefab;

        [SerializeField] private GuideManager guideManager;
        private AgentAnimationTask _agentAnimationTask;

        private AgentMovementTask _agentMovementTask;

        private bool activatedOnce;

        private bool deavtivedLast;

        private float lastSpawnTime = -10f;

        private GameObject latestPlatform;
        private Camera mainCamera;

        private void Start()
        {
            if (!arrow) DebugEditor.LogError("Arrow is not set in ARManager");
            if (!localizationProgress) DebugEditor.LogError("LocalizationProgress is not set in ARManager");
            arrow.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!localizationProgress) return;
            if (activatedOnce && localizationProgress.IsLocalizationComplete)
            {
                CheckDistanceToLatestSpawnPlatform();
                return;
            }

            if (!activatedOnce && localizationProgress.IsLocalizationComplete)
            {
                guideManager.ActivateGuide();
                arrow.gameObject.SetActive(true);
                arrow.target = guideManager.GetChosenGuideInfo().guideGameObject.transform;
                activatedOnce = true;
                deavtivedLast = false;
                SpawnPlatform();
            }

            if (!activatedOnce && !localizationProgress.IsLocalizationComplete && !deavtivedLast)
            {
                guideManager.DeactivateGuide();
                arrow.gameObject.SetActive(false);
                activatedOnce = false;
                deavtivedLast = true;
            }
        }

        public void CheckDistanceToLatestSpawnPlatform()
        {
            if (!latestPlatform) return;
            if (!mainCamera) mainCamera = Camera.main;
            if (!mainCamera) return;
            var distance = Vector3.Distance(
                new Vector3(mainCamera.transform.position.x, 0, mainCamera.transform.position.z),
                new Vector3(latestPlatform.transform.position.x, 0, latestPlatform.transform.position.z));
            if (distance > 5) SpawnPlatform();
        }

        public void ActivateARNavigation(Transform target)
        {
            if (!target) return;
            if (!arrow) arrow = FindAnyObjectByType<ARArrow>();
            if (arrow) arrow.target = target.transform;
            if (!guideManager) guideManager = FindAnyObjectByType<GuideManager>();
            if (!guideManager) return;
            if (!target) return;
            if (_agentMovementTask != null && (_agentMovementTask.State == TaskState.Waiting ||
                                               _agentMovementTask.State == TaskState.Running))
                GetPointMetersFromCamera(target, 3); // Changes the goal of running task
            else
                _agentMovementTask = guideManager.DoWalkingTask(GetPointMetersFromCamera(target, 3));
            if (_agentAnimationTask == null || _agentAnimationTask.IsFinished)
                _agentAnimationTask = guideManager.DoPointingTask(target.gameObject, 30);
        }

        public void ActivateARGuide(Transform target)
        {
            if (!target) return;
            if (!arrow) arrow = FindAnyObjectByType<ARArrow>();

            if (!guideManager) guideManager = FindAnyObjectByType<GuideManager>();
            if (!guideManager) return;
            if (!target) return;
            if (arrow) arrow.target = guideManager.GetChosenGuideInfo().guideGameObject.transform;
            if (_agentMovementTask is { IsFinished: false })
                GetPointMetersFromCamera(target, 3); // Changes the goal of running task
            guideManager.TeleportGuideInFrontOfCamera();
            _agentMovementTask = guideManager.DoWalkingTask(GetPointMetersFromCamera(target, 3));
            //if (_agentAnimationTask is { IsFinished: false }) _agentAnimationTask.Abort();
            guideManager.DoRotateToUser();
        }


        private Transform GetPointMetersFromCamera(Transform target, int meters)
        {
            if (!mainCamera) mainCamera = Camera.main;
            if (!mainCamera) return null;
            var direction = (target.position - mainCamera.transform.position).normalized;
            var point = mainCamera.transform.position + direction * meters;
            transform.position = point;
            return transform;
        }

        public void SpawnPlatform()
        {
            if (Time.time - lastSpawnTime < 10f) return;
            lastSpawnTime = Time.time;

#if UNITY_EDITOR
            if (latestPlatform) Destroy(latestPlatform);
            latestPlatform = Instantiate(TerrainAnchorPrefab, Camera.main.transform.position, Quaternion.identity);
            var position = latestPlatform.transform.position;
            position.y = 0;
            latestPlatform.transform.position = position;
            latestPlatform.transform.parent = null;
            return;
#endif
            var coordinates = localizationProgress.GetCurrentPosition();
            if (coordinates.latitude == 0 || coordinates.longitude == 0) return;

            var eunRotation = Quaternion.Euler(0, 0, 0);
            var terrainPromise =
                AnchorManager.ResolveAnchorOnTerrainAsync(
                    coordinates.latitude, coordinates.longitude, 0, eunRotation);

            // The anchor will need to be resolved.
            StartCoroutine(CheckTerrainPromise(terrainPromise));
        }

        private IEnumerator CheckTerrainPromise(ResolveAnchorOnTerrainPromise promise)
        {
            yield return promise;

            var result = promise.Result;
            if (result.TerrainAnchorState == TerrainAnchorState.Success &&
                result.Anchor != null)
            {
                // resolving anchor succeeded
                if (latestPlatform) Destroy(latestPlatform);
                latestPlatform = Instantiate(TerrainAnchorPrefab,
                    result.Anchor.gameObject.transform);
                latestPlatform.transform.parent = result.Anchor.gameObject.transform;
            }
            // resolving anchor failed
        }
    }
}