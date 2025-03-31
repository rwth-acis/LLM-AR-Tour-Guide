using System.Threading.Tasks;
using i5.LLM_AR_Tourguide.GeospatialAPI;
using i5.LLM_AR_Tourguide.UI_Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace i5.LLM_AR_Tourguide
{
    public class ARPointOfInterest : MonoBehaviour
    {
        private static readonly int F = Animator.StringToHash("Flip");
        private static readonly int R = Animator.StringToHash("Restart");
        private static readonly int E = Animator.StringToHash("End");
        [SerializeField] private GameObject[] meshes;
        [SerializeField] private Canvas canvas;

        [FormerlySerializedAs("canvas")] [SerializeField]
        private LookAtConstraint canvasConstraint;

        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private bool isSmallAnchor;

        private Animator _animator;
        private Camera _camera;

        private bool firstTImeActivate;
        private bool firstTImeDeactivate;
        private Vector3 originalScale;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void Start()
        {
            _camera = Camera.main;
            _animator = GetComponent<Animator>();
            _animator.enabled = false;
            firstTImeDeactivate = true;
            canvasConstraint.AddSource(new ConstraintSource
                { sourceTransform = _camera.gameObject.transform, weight = 1 });
            canvasConstraint.constraintActive = false;
            canvas.worldCamera = _camera;
            foreach (var mesh in meshes)
                mesh.SetActive(false);
            if (isSmallAnchor)
            {
                var button = canvas.gameObject.AddComponent<Button>();
                button.onClick.AddListener(OnClick);
            }

            originalScale = gameObject.transform.parent.localScale;

            if (!isSmallAnchor)
            {
                var oP = transform.localPosition;
                transform.localPosition = new Vector3(oP.x, 11, oP.z);
            }
            else
            {
                var oP = transform.localPosition;
                transform.localPosition = new Vector3(oP.x, 5.5f, oP.z);
            }
        }

        // Update is called once per frame
        private void Update()
        {
            if (!_camera) _camera = Camera.main;
            if (!_camera) return;
            if (_animator.isActiveAndEnabled)
                if (Time.frameCount % (Random.Range(15, 60) * 60) == 0) // Approximately every 5 to 15 seconds
                    _animator.SetBool(F, true);

            // Check if camera is looking at the object
            var viewportPoint = _camera.WorldToViewportPoint(transform.position);

            if (viewportPoint is { z: > 0, x: > 0 and < 1, y: > 0 and < 1 })
                Activate();
            else
                _ = Deactivate();
        }

        public void SetText(string text)
        {
            if (this.text)
                this.text.text = text;
        }

        public void OnClick()
        {
            var uiSwitcher = FindAnyObjectByType<UISwitcher>();
            if (uiSwitcher)
                uiSwitcher.OnARElementPressed(text.text);
        }

        private Task Activate()
        {
            if (!firstTImeActivate)
            {
                AdjustScale();
                firstTImeActivate = true;
                firstTImeDeactivate = false;
                _animator.enabled = true;
                canvasConstraint.constraintActive = true;
                //_animator.SetBool(R, true);
                // Skip current animation
                _animator.CrossFade("Base Layer.BUILDON", 0);
                foreach (var mesh in meshes)
                    mesh.SetActive(true);
            }

            return Task.CompletedTask;
        }

        private async Task Deactivate()
        {
            if (!firstTImeDeactivate)
            {
                firstTImeDeactivate = true;
                _animator.CrossFade("Base Layer.BUILDONR", 0.5f);
                // Wait for the animation to finish
                await Task.Delay(2600);
                firstTImeActivate = false;
                _animator.enabled = false;
                canvasConstraint.constraintActive = false;
                foreach (var mesh in meshes)
                    mesh.SetActive(false);
            }
        }

        /// <summary>
        ///     Adjusts the scale of the anchor based on the distance to the camera and adjusts the position to compensate for the
        ///     scale change, so that the bottom of the gameObject stays at the same height
        /// </summary>
        private void AdjustScale()
        {
            if (!transform.parent)
            {
                DebugEditor.LogWarning("Books need to have a parent object to adjust the scale");
                return;
            }

            var newScale = originalScale * GeospatialController.GetRooftopAnchorScale(
                transform.parent.position,
                _camera.gameObject.transform.position, isSmallAnchor);

            transform.parent.localScale = newScale;
        }
    }
}