using System.Collections;
using i5.LLM_AR_Tourguide.Configurations;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class TourListItem : MonoBehaviour
{
    [SerializeField] private GameObject CheckmarkAvatar;
    [SerializeField] private TextMeshProUGUI DistanceText;

    [SerializeField] private Image image;
    [SerializeField] private GameObject NumberAvatar;
    [SerializeField] private TextMeshProUGUI NumberText;
    [SerializeField] private TextMeshProUGUI SubTitleText;
    [SerializeField] private TextMeshProUGUI TitleText;

    private readonly float stepSize = 5f;
    private Button button;
    private Transform mainCamera;

    private Transform target;


    private void Start()
    {
        if (Camera.main) mainCamera = Camera.main.transform;
    }

    public void OnEnable()
    {
        if (target)
            StartCoroutine(UpdateDistanceText());
    }

    public void OnDisable()
    {
        StopAllCoroutines();
    }

    public Transform GetTarget()
    {
        return target;
    }

    private IEnumerator UpdateDistanceText()
    {
        float i = 0;
        while (true)
        {
            var distance = GetDistance();
            if (distance < 0)
            {
                if (i > stepSize) i = stepSize; // Gradually increase the time between checks
                DistanceText.text = "??m";
                yield return new WaitForSeconds(i);
                i++;
                continue;
            }

            DistanceText.text =
                distance < stepSize ? $"<{stepSize}m" : $"~{Mathf.Floor(distance / stepSize) * stepSize}m";
            yield return new WaitForSeconds(stepSize);
        }
    }

    public float GetDistance()
    {
        if (!target || !mainCamera || mainCamera.position.Equals(Vector3.zero) ||
            target.position.Equals(Vector3.zero)) return -1;
        return Vector3.Distance(target.position, mainCamera.position);
    }

    public void SetNumber(int number)
    {
        NumberText.text = number.ToString();
    }

    public void SetChecked(bool isChecked)
    {
        NumberAvatar.SetActive(!isChecked);
        CheckmarkAvatar.SetActive(isChecked);
    }

    public void SetUp(string title, string subTitle, int number, Sprite itemImage, Transform target)
    {
        this.target = target;
        if (this.target)
            StartCoroutine(UpdateDistanceText());
        SetNumber(number);
        SetChecked(false);
        TitleText.text = title;
        SubTitleText.text = subTitle;
        image.sprite = itemImage;
    }

    public void SetUp(PointOfInterestData.POI tour, int number)
    {
        if (tour.gameObjectLocation && tour.gameObjectLocation.transform)
            SetUp(tour.title, tour.subtitle, number, loadImage(tour.title), tour.gameObjectLocation.transform);
        else
            SetUp(tour.title, tour.subtitle, number, loadImage(tour.title), null);
    }

    public static Sprite loadImage(string title)
    {
        var path = "Images/" + title.Replace(" ", "");
        var image = Resources.Load<Sprite>(path);
        if (image == null)
            Debug.LogError("Failed to load image for point of interest at: Assets/Resources/" + path);
        return image;
    }

    public void AddListener(UnityAction action)
    {
        if (button == null) button = GetComponent<Button>();
        button.onClick.AddListener(action);
    }

    public string GetTitle()
    {
        return TitleText.text;
    }
}