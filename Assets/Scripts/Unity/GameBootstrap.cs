using UnityEngine;

namespace MinhaVila.Unity
{
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateGameController()
        {
            if (Object.FindFirstObjectByType<GameController>() != null) return;

            GameObject application = new GameObject("Minha Vila — Aplicação");
            Object.DontDestroyOnLoad(application);
            application.AddComponent<GameController>();
        }
    }
}
