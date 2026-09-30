using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public string transitionedFromScene;
    public static GameManager Instance { get; private set; }

    //[Header("UI & Player References")]
    // Add other scene references here if needed (e.g., general UI/systems)

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        // Subscribe to Unity's scene loaded event
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // This runs automatically every time a new scene loads!
        Debug.Log("Loaded new scene: " + scene.name);

        // Re-find or re-link other scene-local elements automatically if needed:
        RebindUIReferences();
    }

    private void RebindUIReferences()
    {
        // Mana is now automatically handled independently by ManaController.cs on the UI object itself!

        // If your Player script or other scene-local systems need re-linking, you can add them here:
        // Example: 
        // PlayerController player = FindObjectOfType<PlayerController>();
        // if (player != null) { /* do something */ }
    }
}