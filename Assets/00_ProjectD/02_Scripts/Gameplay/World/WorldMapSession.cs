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
        [SerializeField] private OpenWorldDefinition surface;
        [SerializeField] private string[] mapScenes = { "PeachValley", "Moonshadow" };
        private readonly HashSet<string> loaded = new();
        private readonly Dictionary<string, WorldMapEnvironment> environments = new();
        public WorldMapEnvironment CurrentEnvironment => environments.GetValueOrDefault(world.LocalPlayer.mapScene);
        private bool changing;
        private bool shuttingDown;
        private bool debugTravelling;
        private float refreshAt;
        private string visibleMap;
        public static WorldMapSession Instance { get; private set; }
        public bool LocalMapReady => !shuttingDown && !debugTravelling && world.LocalPlayer != null
            && visibleMap == world.LocalPlayer.mapScene && loaded.Contains(world.LocalPlayer.mapScene);
        public async Awaitable DebugTravelAsync(string map, Vector3 position)
        {
            if (!world.IsAuthority || debugTravelling) return;
            var token = destroyCancellationToken;
            while (changing) await Awaitable.NextFrameAsync(token);
            var previousMap = world.LocalPlayer.mapScene;
            var previousPosition = hero.transform.position;
            bool wasEnabled = hero.enabled;
            debugTravelling = true;
            hero.enabled = false;
            try
            {
                world.LocalPlayer.mapScene = map;
                world.LocalPlayer.position = position;
                world.Commit();
                float deadline = Time.realtimeSinceStartup + 45;
                while (true)
                {
                    token.ThrowIfCancellationRequested();
                    if (Time.realtimeSinceStartup > deadline) throw new TimeoutException("지형 로딩 시간이 초과되었습니다.");
                    if (!changing && loaded.Contains(map) && Physics.Raycast(position + Vector3.up * 15, Vector3.down, out var hit, 40, ~0, QueryTriggerInteraction.Ignore) && hit.collider.gameObject.scene.name == map)
                    {
                        position.y = hit.point.y + .3f;
                        break;
                    }
                    await Awaitable.NextFrameAsync(token);
                }
                hero.TryGetComponent<CharacterController>(out var body);
                body.enabled = false;
                hero.transform.position = position;
                body.enabled = true;
                world.LocalPlayer.position = position;
                world.LocalPlayer.travelRevision++;
            }
            catch
            {
                world.LocalPlayer.mapScene = previousMap;
                world.LocalPlayer.position = previousPosition;
                throw;
            }
            finally
            {
                debugTravelling = false;
                if (!token.IsCancellationRequested) { hero.enabled = wasEnabled; world.Commit(); Refresh(); }
            }
        }
        public static string LegacyMap(Vector3 position) => position.y > 70 && position.y < 95 && Mathf.Abs(position.x)<15 && Mathf.Abs(position.z-10)<15 ? "Moonshadow" : "PeachValley";
        public string SurfaceScene(Vector3 position) => surface.regions[surface.Biome(position.x,position.z,world.State.seed)].sceneName;

        private void Awake()
        {
            Instance = this; hero.enabled = false;
            foreach (string name in mapScenes)
                if (SceneManager.GetSceneByName(name).isLoaded) loaded.Add(name);
        }
        private void Start()
        {
            foreach(var building in world.State.buildings)
                if(building.mapScene!="Moonshadow")building.mapScene=SurfaceScene(building.position);
            foreach (string name in loaded) RegisterEnvironment(name);
            world.RefreshMapObjects();
            world.Changed += Refresh; Refresh();
            if (LocalMapReady) hero.enabled = true;
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
            var maps = new HashSet<string>();
            void Include(SurvivorState player)
            {
                if(player.mapScene=="Moonshadow"){maps.Add("Moonshadow");return;}
                player.mapScene=SurfaceScene(player.position);
                maps.Add(player.mapScene);
                int cx=Mathf.FloorToInt(player.position.x/surface.chunkSize),cz=Mathf.FloorToInt(player.position.z/surface.chunkSize);
                int radius=surface.viewRadius+1;
                for(int z=-radius;z<=radius;z++)for(int x=-radius;x<=radius;x++)
                    maps.Add(SurfaceScene(new Vector3((cx+x+.5f)*surface.chunkSize,0,(cz+z+.5f)*surface.chunkSize)));
            }
            Include(world.LocalPlayer);
            if (world.IsAuthority)
                foreach (var player in world.State.players)
                    if (world.ActivePlayers.Contains(player.id)) Include(player);
            return maps;
        }
        private async void Refresh()
        {
            if (changing || shuttingDown || world.LocalPlayer == null) return;
            var wanted = RequiredMaps();
            if (wanted.SetEquals(loaded) && visibleMap == world.LocalPlayer.mapScene && hero.enabled) return;
            changing = true;
            bool paused = !hero.enabled || !LocalMapReady;
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
                        bool surfaceVisible = name!="Moonshadow" && world.LocalPlayer.mapScene!="Moonshadow";
                        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true)) renderer.forceRenderingOff = !(visible || (surfaceVisible && renderer is not ParticleSystemRenderer));
                        foreach (var light in root.GetComponentsInChildren<Light>(true)) {
                            light.enabled = visible && name=="Moonshadow";
                            if(light.enabled && light.type==LightType.Directional)RenderSettings.sun=light;
                        }
                        foreach (var volume in root.GetComponentsInChildren<UnityEngine.Rendering.Volume>(true)) volume.enabled = visible && name=="Moonshadow";
                    }
                if (paused && loaded.Contains(world.LocalPlayer.mapScene))
                {
                    // Map activation completes before its streamers build their initial colliders in Start.
                    await Awaitable.NextFrameAsync(destroyCancellationToken);
                    Physics.SyncTransforms();
                    var position = world.LocalPlayer.position;
                    float groundHeight = world.LocalPlayer.mapScene == "Moonshadow"
                        ? 83f : surface.Height(position.x, position.z, world.State.seed);
                    var hits = Physics.RaycastAll(new Vector3(position.x, groundHeight + 15f, position.z),
                        Vector3.down, 40f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                    float nearest = float.PositiveInfinity;
                    bool found = false;
                    foreach (var hit in hits)
                    {
                        if (hit.collider.transform.IsChildOf(hero.transform) || hit.normal.y < .5f
                            || hit.collider.gameObject.scene.name != world.LocalPlayer.mapScene
                            || hit.distance >= nearest) continue;
                        nearest = hit.distance;
                        groundHeight = hit.point.y;
                        found = true;
                    }
                    // Keep movement paused and retry loading readiness instead of falling through empty ground.
                    if (!found) return;
                    hero.TryGetComponent<CharacterController>(out var body);
                    position.y = groundHeight + body.height * .5f - body.center.y + body.skinWidth;
                    body.enabled = false;
                    hero.transform.position = position;
                    body.enabled = true;
                    world.LocalPlayer.position = position;
                    hero.enabled = true;
                }
                visibleMap = world.LocalPlayer.mapScene;
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
