using System;
using System.Collections.Generic;
using UnityEngine;

namespace DoHwa.World
{
    public enum WorldAction { Gather, Craft, Build, Dismantle, Upgrade, Consume, Equip, Attack, OpenCache, UseGate, UseBuilding, ClaimRewards }

    [Serializable] public struct WorldCommand
    {
        public WorldAction action;
        public string target;
        public int item;
        public int part;
        public Vector3 position;
        public int rotation;
        public int quantity;
    }

    [DefaultExecutionOrder(-1000)]
    public sealed class WorldRuntime : MonoBehaviour
    {
        [SerializeField] private GameCatalog catalog;
        [SerializeField] private HeroMotor hero;
        public HeroMotor Hero => hero;
        public static WorldRuntime Instance { get; private set; }
        public WorldEnemy[] Enemies { get; private set; } = Array.Empty<WorldEnemy>();
        [SerializeField] private string saveSlot = "peach-valley";
        private readonly Dictionary<string, GameObject> buildingViews = new();
        private readonly List<WorldResource> resources = new();
        public IReadOnlyList<WorldResource> Resources => resources;
        private WorldSupplyCache[] caches = Array.Empty<WorldSupplyCache>();
        public DokkaebiGate[] Gates { get; private set; } = Array.Empty<DokkaebiGate>();
        public System.Collections.Generic.IReadOnlyList<WorldSupplyCache> Caches => caches;
        public void RegisterResource(WorldResource node)
        {
            resources.Add(node);
            node.SetAvailable(!State.resources.Exists(r=>r.id==node.Id));
        }
        public void UnregisterResource(WorldResource node) => resources.Remove(node);
        private float saveAt;
        private float survivalElapsed;

        public GameCatalog Catalog => catalog;
        public WorldState State { get; private set; }
        public bool IsAuthority { get; set; } = true;
        public string LocalPlayerId { get; set; } = "local";
        public readonly HashSet<string> ActivePlayers = new() { "local" };
        public SurvivorState LocalPlayer => State.Player(LocalPlayerId);
        public event Action Changed;
        public event Action<WorldCommand, bool, string> Result;
        public event Action<WorldCommand> RemoteCommand;

        private void Awake()
        {
            Instance = this;
            saveSlot = WorldSelection.Slot;
            State = WorldSave.Read(saveSlot);
            foreach (var player in State.players)
                if (string.IsNullOrEmpty(player.mapScene)) player.mapScene = WorldMapSession.LegacyMap(player.position);
            foreach (var building in State.buildings)
                if (string.IsNullOrEmpty(building.mapScene)) building.mapScene = WorldMapSession.LegacyMap(building.position);
            LocalPlayerId = PlayerIdentity.Get();
            var legacy = State.Player(PlayerPrefs.GetString("WorldPlayerIdentity", "local")) ?? State.Player("local");
            if (State.Player(LocalPlayerId) == null && legacy != null)
            {
                foreach (var building in State.buildings)
                    if (building.owner == legacy.id) building.owner = LocalPlayerId;
                legacy.id = LocalPlayerId;
            }
            ActivePlayers.Clear(); ActivePlayers.Add(LocalPlayerId);
            State.GetOrCreatePlayer(LocalPlayerId, catalog);
            Reconcile();
        }

        public void RefreshMapObjects()
        {
            Gates = FindObjectsByType<DokkaebiGate>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            caches = FindObjectsByType<WorldSupplyCache>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Enemies = FindObjectsByType<WorldEnemy>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            resources.Clear();
            resources.AddRange(FindObjectsByType<WorldResource>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            Reconcile(); Changed?.Invoke();
        }
        private void Update()
        {
            if (!IsAuthority) return;
            State.elapsedSeconds += Time.deltaTime;
            survivalElapsed += Time.deltaTime;
            if (survivalElapsed >= 5)
            {
                bool changed = false;
                foreach (var player in State.players)
                    if (ActivePlayers.Contains(player.id)) changed |= SurvivalRules.Tick(State, player, catalog.survival, survivalElapsed);
                survivalElapsed = 0;
                if (changed) Commit();
            }
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            bool respawned = State.resources.RemoveAll(r => r.availableAt <= now) > 0;
            if (respawned) Commit();
            if (Time.unscaledTime >= saveAt) Save();
        }

        private void OnApplicationQuit() { if (IsAuthority) Save(); }
        private void OnDestroy() { if (Instance == this) Instance = null; }
        public void Save() { WorldSave.Write(State, saveSlot); saveAt = Time.unscaledTime + 20; }

        public void Submit(WorldCommand command)
        {
            if (!IsAuthority) { RemoteCommand?.Invoke(command); return; }
            bool success = Execute(LocalPlayerId, command, out string reason);
            Result?.Invoke(command, success, reason);
        }

        public void ReceiveResult(WorldCommand command, bool success, string reason) => Result?.Invoke(command, success, reason);

        public bool Execute(string playerId, WorldCommand command, out string reason)
        {
            if (!IsAuthority) { reason = "방장 연결을 기다리는 중입니다"; return false; }
            var player = State.Player(playerId);
            if (player == null || player.health <= 0) { reason = "행동할 수 없습니다"; return false; }
            bool success;
            switch (command.action)
            {
                case WorldAction.UseBuilding:
                    success = SettlementRules.Use(State, player, catalog, command.target, out reason);
                    break;
                case WorldAction.ClaimRewards:
                    success = SettlementRules.Claim(State, player, catalog, command.target, out reason);
                    break;
                case WorldAction.UseGate:
                    var gate = Array.Find(Gates, g => g.Id == command.target);
                    if (!gate || !gate.isActiveAndEnabled || Vector3.Distance(player.position, gate.transform.position) > 3f)
                    { reason = "도깨비불 가까이에서 거래하세요"; return false; }
                    success = gate.Interact(player, out reason);
                    break;
                case WorldAction.OpenCache:
                    var cache = Array.Find(caches, c => c.Id == command.target);
                    if (!cache || !cache.isActiveAndEnabled || Vector3.Distance(player.position, cache.transform.position) > 3f)
                    { reason = "상자 가까이에서 열어 주세요"; return false; }
                    success = WorldRules.OpenCache(State, player, catalog, cache.Id, cache.Definition, out reason);
                    break;
                case WorldAction.Attack:
                    var enemy = Array.Find(Enemies, e => e.Id == command.target);
                    int pendingBefore = player.pendingRewards.Count;
                    success = enemy && enemy.isActiveAndEnabled && enemy.TryHit(playerId);
                    reason = success ? player.pendingRewards.Count > pendingBefore ? "전리품을 보관했습니다. 모닥불에서 받으세요" : "" : "공격할 수 없습니다";
                    break;
                case WorldAction.Gather:
                    var node = resources.Find(r => r.Id == command.target);
                    if (node == null || Vector3.Distance(player.position, node.transform.position) > 3f || State.resources.Exists(r => r.id == node.Id))
                    { reason = "가까운 채집물을 선택하세요"; return false; }
                    success = WorldRules.Transact(player, catalog, Array.Empty<ItemAmount>(), new[] { node.Yield }, out reason);
                    if (success)
                    {
                        State.resources.Add(new ResourceState { id = node.Id, availableAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + node.RespawnSeconds });
                        JourneyDefinition.Record(State, WorldAction.Gather, "", node.Yield.item, node.Yield.amount);
                    }
                    break;
                case WorldAction.Craft:
                    var recipe = catalog.Recipe(command.target);
                    var station = recipe != null && HasStation(player.position, recipe.station) ? recipe.station : StationKind.Hand;
                    success = WorldRules.Craft(player, catalog, recipe, station, command.quantity == 0 ? 1 : command.quantity, out reason);
                    if (success) JourneyDefinition.Record(State, WorldAction.Craft, recipe.id,
                        recipe.result.item, recipe.result.amount * (command.quantity == 0 ? 1 : command.quantity));
                    break;
                case WorldAction.Build:
                    if (!CanPlace(command.target, command.position, command.rotation, out reason)) return false;
                    success = WorldRules.Place(State, player, catalog, command.target, command.position, command.rotation, out reason);
                    if (success) JourneyDefinition.Record(State, WorldAction.Build, command.target, 0, 1);
                    break;
                case WorldAction.Dismantle:
                    success = WorldRules.Dismantle(State, player, catalog, command.target, out reason);
                    break;
                case WorldAction.Upgrade:
                    success = WorldRules.Upgrade(player, catalog, command.item,
                        HasStation(player.position, StationKind.Forge) ? StationKind.Forge : StationKind.Hand, out reason);
                    break;
                case WorldAction.Consume:
                    success = SurvivalRules.Consume(player, catalog, command.item, out reason);
                    break;
                case WorldAction.Equip:
                    if (command.part < 0 || command.part >= 9 || !catalog.ValidItem(command.item) || player.items[command.item] <= 0
                        || catalog.equipmentParts[command.item] != command.part)
                    { reason = "장착할 수 없는 장비입니다"; return false; }
                    player.equipment[command.part] = player.equipment[command.part] == command.item ? -1 : command.item;
                    success = true; reason = "";
                    break;
                default: reason = "알 수 없는 행동입니다"; return false;
            }
            if (success) Commit();
            return success;
        }

        public StationKind NearestStation(Vector3 position)
        {
            float distance = 4f;
            StationKind station = StationKind.Hand;
            foreach (var building in State.buildings)
            {
                float next = Vector3.Distance(position, building.position);
                var candidate = catalog.Building(building.definition).station;
                if (next < distance && candidate != StationKind.Hand) { station = candidate; distance = next; }
            }
            return station;
        }

        public bool HasStation(Vector3 position, StationKind station)
        {
            if (station == StationKind.Hand) return true;
            foreach (var building in State.buildings)
                if ((building.position - position).sqrMagnitude < 16f && catalog.Building(building.definition).station == station)
                    return true;
            return false;
        }

        public int MaxCraftCount(CraftRecipe recipe) => WorldRules.MaxCraftCount(LocalPlayer, catalog, recipe,
            HasStation(LocalPlayer.position, recipe.station) ? recipe.station : StationKind.Hand);

        public bool CanPlace(string definition, Vector3 position, int turns, out string reason)
        {
            var spec = catalog.Building(definition);
            if (spec == null || !float.IsFinite(position.x) || !float.IsFinite(position.y) || !float.IsFinite(position.z))
            { reason = "설치 위치가 올바르지 않습니다"; return false; }
            var footprint = WorldRules.Footprint(spec, turns);
            if (State.buildings.Count >= 256) { reason = "이 지역의 건설 한도에 도달했습니다"; return false; }
            position.x = Mathf.Round(position.x); position.z = Mathf.Round(position.z);
            foreach (var obstacle in Physics.OverlapBox(position + Vector3.up, new Vector3(footprint.x * .48f, .85f, footprint.y * .48f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
                if (obstacle.bounds.max.y > position.y + .25f)
                { reason = "장애물이 있는 곳에는 설치할 수 없습니다"; return false; }
            if (!Physics.Raycast(position + Vector3.up * 2, Vector3.down, out var hit, 3, ~0, QueryTriggerInteraction.Ignore)
                || Vector3.Angle(hit.normal, Vector3.up) > 12 || Mathf.Abs(hit.point.y - position.y) > .2f)
            { reason = "평평한 지면에 설치하세요"; return false; }
            foreach (float x in new[] { -.45f, .45f }) foreach (float z in new[] { -.45f, .45f })
                if (!Physics.Raycast(position + new Vector3(footprint.x*x, 2, footprint.y*z), Vector3.down, out var corner, 3, ~0, QueryTriggerInteraction.Ignore)
                    || Mathf.Abs(corner.point.y-position.y) > .18f)
                { reason = "건물 전체가 평지 위에 있어야 합니다"; return false; }
            reason = ""; return true;
        }

        public void ApplySnapshot(string json)
        {
            var next = JsonUtility.FromJson<WorldState>(json);
            bool changed = next.id != State.id || next.revision != State.revision || next.seed != State.seed;
            State = next;
            if (changed) { Reconcile(); Changed?.Invoke(); }
        }

        public void Commit()
        {
            State.revision++;

            Reconcile(); Changed?.Invoke();
        }

        private void Reconcile()
        {
            foreach (var node in resources)
                if (node) node.SetAvailable(!State.resources.Exists(r => r.id == node.Id));
            var removed = new List<string>();
            foreach (var view in buildingViews)
                if (!view.Value || !State.buildings.Exists(b => b.id == view.Key)) { Destroy(view.Value); removed.Add(view.Key); }
            foreach (string id in removed) buildingViews.Remove(id);
            foreach (var state in State.buildings)
            {
                if (buildingViews.ContainsKey(state.id)) continue;
                var map = UnityEngine.SceneManagement.SceneManager.GetSceneByName(state.mapScene);
                if (!map.IsValid() || !map.isLoaded) continue;
                var spec = catalog.Building(state.definition);
                var instance = Instantiate(spec.prefab, state.position, Quaternion.Euler(0, state.quarterTurns * 90, 0));
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(instance, map);
                instance.AddComponent<WorldBuilding>().Bind(state, spec);
                buildingViews.Add(state.id, instance);
            }
        }
    }
}

