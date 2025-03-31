using i5.LLM_AR_Tourguide.UI_Scripts;
using UnityEngine;

namespace i5.LLM_AR_TourGuide
{
    [RequireComponent(typeof(PointOfInterestManager))]
    [RequireComponent(typeof(UISwitcher))]
    public class TourListManager : MonoBehaviour
    {
        public TourListItem tourListItemPrefab;

        public Transform content;


        private bool isTourListInstantiated;

        private PointOfInterestManager poiManager;
        private UISwitcher uiSwitcher;

        public void Start()
        {
            poiManager = GetComponent<PointOfInterestManager>();
            uiSwitcher = GetComponent<UISwitcher>();
        }

        private void InstantiateTourList()
        {
            Transform last = null;
            // Clear all TourListItem objects
            foreach (Transform child in content)
                if (child.GetComponent<TourListItem>())
                    Destroy(child.gameObject);
                else
                    last = child;

            if (poiManager == null)
            {
                DebugEditor.LogError("PointOfInterestManager is not set in TourListManager");
                return;
            }

            var i = 0;
            // Instantiate the tour list
            foreach (var tour in poiManager.getAllPointOfInterests())
            {
                var item = Instantiate(tourListItemPrefab, content);
                var y = i;
                item.AddListener(() => { uiSwitcher.OnPOVDetailedPressed(y); });
                i++;
                item.SetUp(tour, i);
                item.SetChecked(tour.isCompletedVisit);
            }

            // Set last object to be the last element in the list
            if (last != null)
                last.SetAsLastSibling();
        }


        public void UpdateTourList(bool forceUpdate = false)
        {
            if (!isTourListInstantiated || forceUpdate)
            {
                InstantiateTourList();
            }

            else
            {
                var i = 0;
                var pointOfInterests = poiManager.getAllPointOfInterests();
                foreach (Transform child in content)
                    if (child.TryGetComponent(out TourListItem item))
                    {
                        var tour = pointOfInterests[i];
                        item.SetChecked(tour.isCompletedVisit);
                        i++;
                    }
            }
        }
    }
}