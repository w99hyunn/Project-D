using System;
using System.Collections.Generic;
using UnityEngine;

namespace DoHwa.World
{
    public enum WorldAction { Gather, Craft, Build, Dismantle, Upgrade, Consume, Equip, Attack }

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

    public sealed class WorldRuntime : MonoBehaviour
    {
        [SerializeField] private GameCatalog catalog;
        [SerializeField] private string saveSlot = "peach-valley";
        [SerializeField] private Transform buildingRoot;
        private readonly Dictionary<string, GameObject> buildingViews = new();
        private readonly List<WorldResource> resources = new();
        public IReadOnlyList<WorldResource> Resources => resources;
        public void RegisterResource(WorldResource node)
        {
            resources.Add(node);
            node.SetAvailable(!State.resources.Exists(r=>r.id==node.Id));
        }
        public void UnregisterResource(WorldResource node) => resources.Remove(node);
        private float saveAt;

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
            saveSlot = WorldSelection.Slot;
            resources.AddRange(FindObjectsByType<WorldResource>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            State = WorldSave.Read(saveSlot);
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

        private void Update()
        {
            if (!IsAuthority) return;
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            bool respawned = State.resources.RemoveAll(r => r.availableAt <= now) > 0;
            if (respawned) Commit();
            if (Time.unscaledTime >= saveAt) Save();
        }

        private void OnApplicationQuit() { if (IsAuthority) Save(); }
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
                case WorldAction.Attack:
                    var enemy = Array.Find(FindObjectsByType<WorldEnemy>(FindObjectsInactive.Include, FindObjectsSortMode.None), e => e.Id == command.target);
                    success = enemy && enemy.TryHit(playerId);
                    reason = success ? "" : "공격할 수 없습니다";
                    break;
                case WorldAction.Gather:
                    var node = resources.Find(r => r.Id == command.target);
                    if (node == null || Vector3.Distance(player.position, node.transform.position) > 3f || State.resources.Exists(r => r.id == node.Id))
                    { reason = "가까운 채집물을 선택하세요"; return false; }
                    success = WorldRules.Transact(player, catalog, Array.Empty<ItemAmount>(), new[] { node.Yield }, out reason);
                    if (success) State.resources.Add(new ResourceState { id = node.Id, availableAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + node.RespawnSeconds });
                    break;
                case WorldAction.Craft:
                    var recipe = catalog.Recipe(command.target);
                    var station = recipe != null && HasStation(player.position, recipe.station) ? recipe.station : StationKind.Hand;
                    success = WorldRules.Craft(player, catalog, recipe, station, command.quantity == 0 ? 1 : command.quantity, out reason);
                    break;
                case WorldAction.Build:
                    if (!CanPlace(command.target, command.position, command.rotation, out reason)) return false;
                    success = WorldRules.Place(State, player, catalog, command.target, command.position, command.rotation, out reason);
                    break;
                case WorldAction.Dismantle:
                    success = WorldRules.Dismantle(State, player, catalog, command.target, out reason);
                    break;
                case WorldAction.Upgrade:
                    success = WorldRules.Upgrade(player, catalog, command.item,
                        HasStation(player.position, StationKind.Forge) ? StationKind.Forge : StationKind.Hand, out reason);
                    break;
                case WorldAction.Consume:
                    success = Consume(player, command.item, out reason);
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

        private bool Consume(SurvivorState player, int item, out string reason)
        {
            if ((item != 2 && item != 3 && item != 7) || (item == 3 ? player.health >= 100 : player.food >= 100))
            { reason = "지금은 사용할 수 없습니다"; return false; }
            if (!WorldRules.Transact(player, catalog, new[] { new ItemAmount(item, 1) }, Array.Empty<ItemAmount>(), out reason)) return false;
            player.health = Mathf.Min(100, player.health + (item == 3 ? 35 : 5));
            if (item != 3) player.food = Mathf.Min(100, player.food + 20);
            return true;
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
            foreach (var node in resources) node.SetAvailable(!State.resources.Exists(r => r.id == node.Id));
            var removed = new List<string>();
            foreach (var view in buildingViews)
                if (!State.buildings.Exists(b => b.id == view.Key)) { Destroy(view.Value); removed.Add(view.Key); }
            foreach (string id in removed) buildingViews.Remove(id);
            foreach (var state in State.buildings)
            {
                if (buildingViews.ContainsKey(state.id)) continue;
                var spec = catalog.Building(state.definition);
                var instance = Instantiate(spec.prefab, state.position, Quaternion.Euler(0, state.quarterTurns * 90, 0), buildingRoot);
                instance.AddComponent<WorldBuilding>().Bind(state, spec);
                buildingViews.Add(state.id, instance);
            }
        }
    }
}

