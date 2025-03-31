using System.Collections;
using i5.LLM_AR_TourGuide;
using i5.LLM_AR_Tourguide.Configurations;
using i5.LLM_AR_Tourguide.TourGeneration;
using i5.LLM_AR_Tourguide.UI_Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace i5.LLM_AR_Tourguide.Prefab_Scripts
{
    public class TourIntroductionUI : MonoBehaviour
    {
        [SerializeField] private TourListItem tourListItem;
        [SerializeField] private ChipButtons chipButtons;
        [SerializeField] private ProgressBar progressBar;
        [SerializeField] private TextMeshProUGUI text;
        private ARManager arManager;
        private int DistanceThreshold = 75;

        public void OnEnable()
        {
            StartCoroutine(CheckIfDistanceIsCloseEnough());
        }

        public void OnDisable()
        {
            StopAllCoroutines();
        }

        public void IncreaseDistanceThreshold(int newDistance)
        {
            DistanceThreshold += newDistance;
            PermissionRequest.ShowToastMessage("Distance threshold is now " + DistanceThreshold + "m");
        }

        private IEnumerator CheckIfDistanceIsCloseEnough()
        {
            yield return null; // wait for the tourListItem to be set up
            while (isActiveAndEnabled)
            {
                DebugEditor.Log("CheckIfDistanceIsCloseEnough " + GetDistance());
                if (GetDistance() > DistanceThreshold || GetDistance() < 0)
                {
                    chipButtons.GenerateButton.interactable = false;
                    text.text = "You need to be within <b>" + DistanceThreshold +
                                "m</b> of " + tourListItem.GetTitle() +
                                " to continue. \n \n Press <b>\"Start navigation\"</b> to start your default map app";
                    if (SystemInfo.batteryLevel < 0.25f) text.text += " to <b>save battery</b>";
                    text.text += ".";
                    if (!arManager) arManager = FindAnyObjectByType<ARManager>();
                    arManager.ActivateARNavigation(tourListItem.GetTarget());
                    yield return new WaitForSeconds(2);
                }
                else
                {
                    text.text =
                        "Press the <b>\"Generate Tour\"</b> button to generate this section of the tour and start the explanation of this destination.";
                    chipButtons.GenerateButton.interactable =
                        chipButtons.NavigationButton
                            .interactable; // Copy the state of the navigation button, i.e. when currently generating a tour, disable the generate button
                    if (!arManager) arManager = FindAnyObjectByType<ARManager>();
                    arManager.ActivateARGuide(tourListItem.GetTarget());
                    yield return new WaitForSeconds(10);
                }
            }

            DebugEditor.Log("CheckIfDistanceIsCloseEnough " + isActiveAndEnabled);
        }

        public void SetUp(PointOfInterestData.POI poi, int number,
            UnityAction generateButton)
        {
            tourListItem.SetUp(poi, number);
            chipButtons.SetupToGenerateMessage(generateButton, poi.title);

            var tourGenerator = FindAnyObjectByType<TourGenerator>();
            progressBar.gameObject.SetActive(false);
            tourGenerator.SetProgressbar(progressBar);
        }

        public float GetDistance()
        {
            return tourListItem.GetDistance();
        }
    }
}