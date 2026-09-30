using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameFlow
{
    public static void StartNewGame(string firstScene)
    {
        PersistentBootstrap.Ensure();

        GameManager.Instance.transitionedFromScene = ""; // forget the last door
        var player = PlayerController.Instance;
        player.Health = player.maxHealth;
        player.Mana = 0f;

        SceneManager.LoadScene(firstScene);
    }
}