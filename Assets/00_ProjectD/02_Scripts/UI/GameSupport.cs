using UnityEngine;
using UnityEngine.SceneManagement;

namespace DoHwa
{
    public static class GameSupport
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureLoaded();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => EnsureLoaded();

        public static void EnsureLoaded()
        {
            if (SceneManager.GetSceneByName("System").isLoaded) return;
            SceneManager.LoadScene("System", LoadSceneMode.Additive);
        }
    }
}
