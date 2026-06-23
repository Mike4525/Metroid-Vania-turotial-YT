using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
    public enum DoorType { Left_Entry, Right_Exit } // List of door types to distinguish between entry and exit doors

    [Header("Door Settings")]
    [SerializeField] private DoorType doorType; // Type of door set in Unity

    [Header("Spawn Settings (Only for Left_Entry doors)")]
    [SerializeField] private Transform startPoint;
    [SerializeField] private Vector2 exitDirection;
    [SerializeField] private float exitTime;

    private bool transitioning = false;

    private void Start()
    {
        if (GameManager.Instance == null) return;

        // If this is a Left Entry door, and the GameManager remembers we just left through a Right Exit...
        if (doorType == DoorType.Left_Entry && GameManager.Instance.transitionedFromScene == "Right_Exit_Triggered")
        {
            if (PlayerController.Instance != null && startPoint != null)
            {
                Rigidbody2D rb = PlayerController.Instance.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                }

                // Move player to the left entry door's spawn point
                PlayerController.Instance.transform.position = startPoint.position;

                // Execute the walk-in sequence (which handles turning cutscene back to false when done)
                StartCoroutine(PlayerController.Instance.WalkIntoNewScene(exitDirection, exitTime));
            }

            // Clear the state so it doesn't accidentally trigger elsewhere
            GameManager.Instance.transitionedFromScene = "";
        }
    }

    private void OnTriggerEnter2D(Collider2D _other)
    {
        if (transitioning) return;

        // CRITICAL: ONLY Right doors can be entered to exit a room!
        // Left doors are exclusively for entry and will ignore collisions when moving rooms.
        if (doorType == DoorType.Right_Exit && _other.CompareTag("Player"))
        {
            // If the player is already in a cutscene state, ignore the door
            if (PlayerController.Instance != null && PlayerController.Instance.pState.cutscene)
            {
                return;
            }

            transitioning = true;

            // Mark that we explicitly left out of a RIGHT exit door
            GameManager.Instance.transitionedFromScene = "Right_Exit_Triggered";

            PlayerController.Instance.pState.cutscene = true;

            // Generate the random room choice
            string randomNextScene = "Cave_1_" + Random.Range(1, 4).ToString();

            SceneManager.LoadScene(randomNextScene);
        }
    }
}