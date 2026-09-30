using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ManaController : MonoBehaviour
{
    private Image manaFillImage;

    private void Awake()
    {
        manaFillImage = GetComponent<Image>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.onManaChangedCallback -= UpdateManaHUD;
        }
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        StartCoroutine(InitializeHUDRoutine());
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(InitializeHUDRoutine());
    }

    private IEnumerator InitializeHUDRoutine()
    {
        // Wait until PlayerController exists AND has completed its own start/load routine
        float timeout = 2f;
        float elapsed = 0f;

        while (PlayerController.Instance == null && elapsed < timeout)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.onManaChangedCallback -= UpdateManaHUD;
            PlayerController.Instance.onManaChangedCallback += UpdateManaHUD;

            // Wait one extra frame to let PlayerController load its saved persistent stats
            yield return null;

            UpdateManaHUD();
        }
    }

    public void UpdateManaHUD()
    {
        if (manaFillImage == null || PlayerController.Instance == null) return;

        // Optional safety: if PlayerController's mana is somehow 0 right at scene load 
        // but shouldn't be, you can catch it here. Otherwise, it updates normally.
        manaFillImage.fillAmount = PlayerController.Instance.Mana;
    }
}