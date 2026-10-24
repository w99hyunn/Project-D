using DoHwa.UI;
using TMPro;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace DoHwa.World
{
    public sealed class WorldPlayerInteraction : MonoBehaviour
    {
        [SerializeField] private WorldRuntime world;
        [SerializeField] private HeroMotor hero;
        [SerializeField] private RpgUiController ui;
        [SerializeField] private Camera view;
        [SerializeField] private TMP_Text interactionLabel;
        [SerializeField] private Transform placementMarker;
        private WorldResource target;
        private WorldBuilding building;
        private WorldSupplyCache cache;
        private DokkaebiGate gate;
        private int travelRevision;
        private int buildingIndex = -1;
        private int turns;
        private Vector3 placement;
        private float attackAt;
        private float resultUntil;
        private string result;
        private WorldBuilding[] buildings;
        private int respawns;

        private void Start()
        {
            ui.BindWorld(world);
            world.Changed += Refresh;
            world.Result += OnResult;
            hero.TryGetComponent<CharacterController>(out var body);
            body.enabled = false; hero.transform.position = world.LocalPlayer.position; body.enabled = true;
            Refresh();
        }

        private void Refresh()
        {
            if (world.LocalPlayer == null) return;
            buildings = FindObjectsByType<WorldBuilding>(FindObjectsSortMode.None);
            ui.ApplyWorldState(world.LocalPlayer);
            if (WorldMapSession.Instance.LocalMapReady && (respawns != world.LocalPlayer.respawns || travelRevision != world.LocalPlayer.travelRevision))
            {
                travelRevision = world.LocalPlayer.travelRevision;
                respawns = world.LocalPlayer.respawns;
                hero.TryGetComponent<CharacterController>(out var body);
                body.enabled = false; hero.transform.position = world.LocalPlayer.position; body.enabled = true;
            }
        }

        private void OnResult(WorldCommand command, bool success, string reason)
        {
            ui.ShowWorldResult(command, success, reason);
            if (ui.IsModalOpen) return;
            result = success ? command.action switch
            {
                WorldAction.Gather => "채집 완료",
                WorldAction.Build => "설치 완료",
                WorldAction.Dismantle => "건물을 회수했습니다",
                WorldAction.Upgrade => "강화 완료",
                WorldAction.UseGate => reason,
                WorldAction.UseBuilding => reason,
                WorldAction.ClaimRewards => reason,
                WorldAction.Attack => reason,
                _ => ""
            } : reason;
            if (success && command.action == WorldAction.OpenCache)
            {
                var opened = world.Caches.First(c => c.Id == command.target);
                result = string.Join("\n", opened.Definition.rewards.Select(r => ui.Catalog[r.item].title + " ×" + r.amount));
            }
            resultUntil = Time.unscaledTime + (command.action == WorldAction.OpenCache || command.action == WorldAction.UseGate ? 3.5f : 1.5f);
        }

        private void Update()
        {
            if (world.LocalPlayer == null) return;
            if (!WorldMapSession.Instance.LocalMapReady) { interactionLabel.text = ""; return; }
            world.LocalPlayer.position = hero.transform.position;
            if (ui.IsModalOpen || !hero.enabled)
            { interactionLabel.text = ""; placementMarker.gameObject.SetActive(false); return; }
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null || mouse == null) return;
            if (keyboard.gKey.wasPressedThisFrame) buildingIndex = buildingIndex < 0 ? 0 : -1;
            if (buildingIndex >= 0) { Placement(keyboard, mouse); return; }
            placementMarker.gameObject.SetActive(false);
            target = null; building = null; cache = null; gate = null;
            float closest = 2.8f;
            foreach (var resource in world.Resources)
            {
                if (world.State.resources.Exists(r => r.id == resource.Id)) continue;
                float distance = Vector3.Distance(hero.transform.position, resource.transform.position);
                if (distance < closest) { closest = distance; target = resource; }
            }
            foreach (var candidate in buildings)
                if (candidate &&
                    Vector3.Distance(hero.transform.position, candidate.transform.position) < closest)
                { closest = Vector3.Distance(hero.transform.position, candidate.transform.position); building = candidate; target = null; }
            foreach (var candidate in world.Caches)
            {
                if (!candidate.isActiveAndEnabled || candidate.Opened) continue;
                float distance = Vector3.Distance(hero.transform.position, candidate.transform.position);
                if (distance < closest) { closest = distance; cache = candidate; target = null; building = null; }
            }
            foreach (var candidate in world.Gates)
            {
                if (!candidate.isActiveAndEnabled) continue;
                float distance = Vector3.Distance(hero.transform.position, candidate.transform.position);
                if (distance < closest) { closest = distance; gate = candidate; cache = null; target = null; building = null; }
            }
            interactionLabel.text = Time.unscaledTime < resultUntil ? result : gate ? gate.Hint : cache
                ? cache.Unlocked ? "[E] " + cache.Definition.title : cache.Definition.lockedHint
                : target ? "[E] " + target.Title : building ? BuildingHint(building) : "";
            if (keyboard.eKey.wasPressedThisFrame)
            {
                if (gate) world.Submit(new WorldCommand { action = WorldAction.UseGate, target = gate.Id });
                else if (cache) world.Submit(new WorldCommand { action = WorldAction.OpenCache, target = cache.Id });
                else if (target) world.Submit(new WorldCommand { action = WorldAction.Gather, target = target.Id });
                else if (building)
                {
                    if (building.Spec.use == BuildingUse.Crafting) ui.OpenCrafting();
                    else world.Submit(new WorldCommand { action = WorldAction.UseBuilding, target = building.InstanceId });
                }
            }
            if (building && building.Spec.use == BuildingUse.Camp && keyboard.rKey.wasPressedThisFrame)
                world.Submit(new WorldCommand { action = WorldAction.ClaimRewards, target = building.InstanceId });
            if (building && building.Spec.station == StationKind.Forge && keyboard.uKey.wasPressedThisFrame)
                world.Submit(new WorldCommand { action = WorldAction.Upgrade, item = world.LocalPlayer.equipment[0] });
            if (building && keyboard.deleteKey.wasPressedThisFrame)
                world.Submit(new WorldCommand { action = WorldAction.Dismantle, target = building.InstanceId });
            if (mouse.leftButton.wasPressedThisFrame && Time.time >= attackAt && !EventSystem.current.IsPointerOverGameObject())
            {
                attackAt = Time.time + .45f;
                foreach (var enemy in world.Enemies)
                    if (enemy.isActiveAndEnabled && Vector3.Distance(hero.transform.position, enemy.transform.position) < 2.5f)
                        world.Submit(new WorldCommand { action = WorldAction.Attack, target = enemy.Id });
            }
        }

        private void Placement(Keyboard keyboard, Mouse mouse)
        {
            if (keyboard.escapeKey.wasPressedThisFrame) { buildingIndex = -1; return; }
            if (keyboard.tabKey.wasPressedThisFrame) buildingIndex = (buildingIndex + 1) % world.Catalog.buildings.Length;
            if (keyboard.rKey.wasPressedThisFrame) turns = (turns + 1) % 4;
            var ray = view.ScreenPointToRay(mouse.position.ReadValue());
            if (Physics.Raycast(ray, out var hit, 120, ~0, QueryTriggerInteraction.Ignore))
                placement = new Vector3(Mathf.Round(hit.point.x), hit.point.y, Mathf.Round(hit.point.z));
            var spec = world.Catalog.buildings[buildingIndex];
            var footprint = WorldRules.Footprint(spec, turns);
            placementMarker.gameObject.SetActive(true);
            placementMarker.position = placement + Vector3.up * .035f;
            placementMarker.localScale = new Vector3(footprint.x, .025f, footprint.y);
            bool valid = world.CanPlace(spec.id, placement, turns, out string reason) && Vector3.Distance(hero.transform.position, placement) < 6;
            interactionLabel.text = spec.title + "    [Tab] 변경    [R] 회전    [G] 종료" + (valid ? "" : "\n" + reason);
            if (mouse.leftButton.wasPressedThisFrame && valid && !EventSystem.current.IsPointerOverGameObject())
                world.Submit(new WorldCommand { action = WorldAction.Build, target = spec.id, position = placement, rotation = turns });
        }

        private void OnDestroy() { world.Changed -= Refresh; world.Result -= OnResult; }

        private string BuildingHint(WorldBuilding view)
        {
            if (view.Spec.use == BuildingUse.Camp)
                return "[E] 모닥불에서 휴식" + (world.LocalPlayer.pendingRewards.Count > 0 ? "    [R] 보관 전리품 받기" : "");
            if (view.Spec.use == BuildingUse.Garden)
            {
                var state = world.State.buildings.Find(b => b.id == view.InstanceId);
                if (state.plantedAt < 0) return "[E] " + view.Spec.crop.title + " 심기";
                float growth = SettlementRules.Growth(world.State, state, view.Spec.crop);
                if (growth >= 1) return "[E] " + view.Spec.crop.title + " 수확";
                return view.Spec.crop.title + " 성장 중  " + Mathf.CeilToInt((1 - growth) * view.Spec.crop.growthSeconds) + "초";
            }
            return "[E] " + view.Spec.title + (view.Spec.station == StationKind.Forge ? "    [U] 강화" : "");
        }
    }
}
