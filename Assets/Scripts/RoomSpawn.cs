using UnityEngine;

public class RoomSpawn : MonoBehaviour
{
    [SerializeField] private Transform defaultSpawn;

    [Header("Optional walk-in (e.g. entering the cave)")]
    [SerializeField] private Vector2 walkDirection = Vector2.right;
    [SerializeField] private float walkTime = 0f; // 0 = no walk-in

    private void Start()
    {
        var player = PlayerController.Instance;
        var gm = GameManager.Instance;
        if (player == null || gm == null) return;

        // Arriving through a door? TutorialTransition places the player instead.
        if (!string.IsNullOrEmpty(gm.transitionedFromScene)) return;

        player.transform.position = defaultSpawn.position;

        if (walkTime > 0f)
            player.StartCoroutine(player.WalkIntoNewScene(walkDirection, walkTime));
    }
}