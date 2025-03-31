using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class NavMeshRuntime : MonoBehaviour
{
    [SerializeField] private ARPlaneManager arPlaneManager;
    private NavMeshSurface surface;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        surface = GetComponent<NavMeshSurface>();
        //arPlaneManager = GetComponent<ARPlaneManager>();
        arPlaneManager.trackablesChanged.AddListener(OnTrackablesChanged);
    }

    private void OnTrackablesChanged(ARTrackablesChangedEventArgs<ARPlane> changes)
    {
        if (changes.updated.Count > 0)
        {
            surface.BuildNavMesh();

            DebugEditor.Log("NavMesh built due to ARPlane boundary change in");
        }
        /*
        foreach (var plane in changes.added)
        {
            // handle added planes
        }

        foreach (var plane in changes.updated)
        {
            // handle updated planes
        }

        foreach (var plane in changes.removed)
        {
            // handle removed planes
        }
        */
    }
}