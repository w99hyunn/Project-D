using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DoHwa.World
{
    [DefaultExecutionOrder(-900)]
    public sealed class WorldMapSession : MonoBehaviour
    {
        [SerializeField] private WorldRuntime world;
        [SerializeField] private HeroMotor hero;
        [SerializeField] private string[] mapScenes = { "PeachValley", "Moonshadow" };
        private readonly HashSet<string> loaded = new();
        private readonly Dictionary<string, WorldMapEnvironment> environments = new();
        public WorldMapEnvironment CurrentEnvironment => environments.GetValueOrDefault(world.LocalPlayer.mapScene);
        private bool changing;
        private bool shuttingDown;
        private float refreshAt;
        private string visibleMap;
        public static WorldMapSession Instance { get; private set; }
        public bool LocalMapReady => !shuttingDown && world.LocalPlayer != null && loaded.Contains(world.LocalPlayer.mapScene);
        public static string LegacyMap(Vector3 position) => position.y > 50 ? "Moonshadow" : "PeachValley";

        private void Awake()
        {
            Instance = this; hero.enabled = false;
            foreach (string name in mapScenes)
                if (SceneManager.GetSceneByName(name).isLoaded) loaded.Add(name);
        }
        private void Start()
        {
            foreach (string name in loaded) RegisterEnvironment(name);
            world.RefreshMapObjects();
            world.Changed += Refresh; Refresh();
        }
        private void RegisterEnvironment(string name)
        {
            foreach (var root in SceneManager.GetSceneByName(name).GetRootGameObjects())
                if (root.TryGetComponent<WorldMapEnvironment>(out var environment)) environments[name] = environment;
        }
        private void Update()
        {
            if (!LocalMapReady) hero.enabled = false;
            if (Time.unscaledTime < refreshAt) return;
            refreshAt = Time.unscaledTime + .5f;
            Refresh();
        }
        private HashSet<string> RequiredMaps()
        {
            var maps = new HashSet<string> { world.LocalPlayer.mapScene };
            if (world.IsAuthority)
                foreach (var player in world.State.players)
                    if (world.ActivePlayers.Contains(player.id)) maps.Add(player.mapScene);
            return maps;
        }
        private async void Refresh()
        {
            if (changing || shuttingDown || world.LocalPlayer == null) return;
            var wanted = RequiredMaps();
            if (wanted.SetEquals(loaded) && visibleMap == world.LocalPlayer.mapScene) return;
            changing = true;
            bool paused = !LocalMapReady;
            if (paused) hero.enabled = false;
            try
            {
                foreach (string name in wanted)
                {
                    if (loaded.Contains(name)) continue;
                    if (Array.IndexOf(mapScenes, name) < 0)
                        throw new InvalidOperationException("Unknown world map: " + name);
                    if (!SceneManager.GetSceneByName(name).isLoaded)
                        await Awaitable.FromAsyncOperation(SceneManager.LoadSceneAsync(name, LoadSceneMode.Additive), destroyCancellationToken);
                    loaded.Add(name);
                    RegisterEnvironment(name);
                    world.RefreshMapObjects();
                }
                var required = RequiredMaps();
                foreach (string name in new List<string>(loaded))
                {
                    if (required.Contains(name)) continue;
                    loaded.Remove(name);
                    environments.Remove(name);
                    await Awaitable.FromAsyncOperation(SceneManager.UnloadSceneAsync(name), destroyCancellationToken);
                    world.RefreshMapObjects();
                }
                foreach (string name in loaded)
                    foreach (var root in SceneManager.GetSceneByName(name).GetRootGameObjects())
                    {
                        bool visible = name == world.LocalPlayer.mapScene;
                        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true)) renderer.forceRenderingOff = !visible;
                        foreach (var light in root.GetComponentsInChildren<Light>(true)) light.enabled = visible;
                        foreach (var volume in root.GetComponentsInChildren<UnityEngine.Rendering.Volume>(true)) volume.enabled = visible;
                    }
                visibleMap = world.LocalPlayer.mapScene;
                if (paused && LocalMapReady)
                {
                    hero.TryGetComponent<CharacterController>(out var body);
                    body.enabled = false; hero.transform.position = world.LocalPlayer.position; body.enabled = true;
                    hero.enabled = true;
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception e) { Debug.LogException(e); }
            finally { changing = false; }
        }
        public async Awaitable UnloadMapsAsync()
        {
            shuttingDown = true;
            hero.enabled = false;
            while (changing) await Awaitable.NextFrameAsync(destroyCancellationToken);
            foreach (string name in new List<string>(loaded))
                await Awaitable.FromAsyncOperation(SceneManager.UnloadSceneAsync(name), destroyCancellationToken);
            loaded.Clear();
            environments.Clear();
            world.RefreshMapObjects();
        }
        private void OnDestroy() { world.Changed -= Refresh; Instance = null; }
    }
}
