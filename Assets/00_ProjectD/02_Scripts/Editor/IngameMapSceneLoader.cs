using System.IO;
using DoHwa.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DoHwa.Editor
{
    [InitializeOnLoad]
    internal static class IngameMapSceneLoader
    {
        static IngameMapSceneLoader()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorApplication.delayCall += OpenPlayerMap;
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (scene.name == "Ingame") EditorApplication.delayCall += OpenPlayerMap;
        }

        private static void OpenPlayerMap()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var ingame = SceneManager.GetSceneByName("Ingame");
            if (!ingame.IsValid() || !ingame.isLoaded) return;
            foreach (var root in ingame.GetRootGameObjects())
            foreach (var session in root.GetComponentsInChildren<WorldMapSession>(true))
            {
                var fields = new SerializedObject(session);
                var hero = (DoHwa.HeroMotor)fields.FindProperty("hero").objectReferenceValue;
                var surface = (OpenWorldDefinition)fields.FindProperty("surface").objectReferenceValue;
                var position = hero.transform.position;
                int seed = File.Exists(WorldSave.PathFor(WorldSelection.Slot))
                    ? WorldSave.Read(WorldSelection.Slot).seed : 0;
                string map = WorldMapSession.LegacyMap(position) == "Moonshadow" ? "Moonshadow"
                    : surface.regions[surface.Biome(position.x, position.z, seed)].sceneName;
                if (SceneManager.GetSceneByName(map).isLoaded) return;
                foreach (string guid in AssetDatabase.FindAssets(map + " t:Scene"))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (Path.GetFileNameWithoutExtension(path) != map) continue;
                    var active = SceneManager.GetActiveScene();
                    EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                    SceneManager.SetActiveScene(active);
                    return;
                }
                Debug.LogWarning("플레이어 위치의 맵 씬을 찾을 수 없습니다: " + map, session);
            }
        }
    }
}
