using UnityEngine;
using UnityEngine.SceneManagement;

public static class PersistentBootstrap
{
    private const string MenuScene = "MainMenu";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInit()
    {
        if (SceneManager.GetActiveScene().name == MenuScene) return;
        Ensure(); // pressing Play in any room spawns everything
    }

    public static void Ensure()
    {
        Spawn("Canvas_HUD", PersistentHUD.Instance == null);
        Spawn("GameManager", GameManager.Instance == null);
        Spawn("Player", PlayerController.Instance == null);
    }

    private static void Spawn(string prefabName, bool needed)
    {
        if (!needed) return;
        var prefab = Resources.Load<GameObject>(prefabName);
        if (prefab == null) { Debug.LogError($"Missing prefab in a Resources folder: {prefabName}"); return; }
        Object.Instantiate(prefab);
    }
}