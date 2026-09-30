using UnityEngine;
using UnityEngine.UI;

public class HeartController : MonoBehaviour
{
    public Transform heartsParent;
    public GameObject heartContainerPrefab;

    private GameObject[] heartContainers;
    private Image[] heartFills;
    private bool bound;

    private void OnEnable() => Bind();
    private void Start() => Bind();
    private void OnDisable() => Unbind();

    private void Bind()
    {
        if (bound || PlayerController.Instance == null) return;
        PlayerController.Instance.onHealthChangedCallback += UpdateHeartsHUD;
        bound = true;
        BuildHearts();
        UpdateHeartsHUD();
    }

    private void Unbind()
    {
        if (!bound) return;
        if (PlayerController.Instance != null)
            PlayerController.Instance.onHealthChangedCallback -= UpdateHeartsHUD;
        bound = false;
    }

    private void BuildHearts()
    {
        foreach (Transform child in heartsParent) Destroy(child.gameObject);

        int max = PlayerController.Instance.maxHealth;
        heartContainers = new GameObject[max];
        heartFills = new Image[max];

        for (int i = 0; i < max; i++)
        {
            heartContainers[i] = Instantiate(heartContainerPrefab, heartsParent);
            Transform fill = heartContainers[i].transform.Find("HeartFill");
            if (fill != null) heartFills[i] = fill.GetComponent<Image>();
        }
    }

    public void UpdateHeartsHUD()
    {
        if (heartFills == null || PlayerController.Instance == null) return;

        for (int i = 0; i < heartFills.Length; i++)
        {
            if (heartContainers[i] != null)
                heartContainers[i].SetActive(i < PlayerController.Instance.maxHealth);
            if (heartFills[i] != null)
                heartFills[i].fillAmount = i < PlayerController.Instance.Health ? 1f : 0f;
        }
    }
}