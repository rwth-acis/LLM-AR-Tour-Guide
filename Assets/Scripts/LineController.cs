using UnityEngine;

public class LineController : MonoBehaviour
{
    public Transform LineStart;

    public Transform LineEnd;

    public LineRenderer lineRenderer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    // Update is called once per frame
    private void Update()
    {
        if (lineRenderer && LineStart && LineEnd)
            lineRenderer.SetPositions(new[] { LineStart.transform.position, LineEnd.transform.position });
    }
}