using System;
using System.Collections.Generic;
using UnityEngine;

namespace DoHwa.World
{
    // Every economic operation is validated before committing; callers cannot spend partial costs.
    public static class WorldRules
    {
        public static bool OpenCache(WorldState world, SurvivorState player, GameCatalog catalog,
            string id, SupplyCacheDefinition definition, out string reason)
        {
            if (world.openedCaches.Contains(id)) { reason = "이미 열린 상자입니다"; return false; }
            if (!string.IsNullOrEmpty(definition.requiredBoss) && !world.defeatedBosses.Contains(definition.requiredBoss))
            { reason = definition.lockedHint; return false; }
            if (!Transact(player, catalog, Array.Empty<ItemAmount>(), definition.rewards, out reason)) return false;
            world.openedCaches.Add(id);
            return true;
        }
        public static bool Transact(SurvivorState player, GameCatalog catalog,
            IReadOnlyList<ItemAmount> costs, IReadOnlyList<ItemAmount> gains, out string reason)
        {
            var next = (int[])player.items.Clone();
            foreach (var cost in costs)
            {
                if (!catalog.ValidItem(cost.item) || cost.amount <= 0 || next[cost.item] < cost.amount)
                { reason = "재료가 부족합니다"; return false; }
                next[cost.item] -= cost.amount;
            }
            foreach (var gain in gains)
            {
                if (!catalog.ValidItem(gain.item) || gain.amount <= 0 || gain.amount > catalog.stackLimit - next[gain.item])
                { reason = "가방에 공간이 부족합니다"; return false; }
                next[gain.item] += gain.amount;
            }
            int occupied = 0;
            foreach (var amount in next) if (amount > 0) occupied++;
            if (occupied > catalog.inventorySlots) { reason = "가방에 공간이 부족합니다"; return false; }
            player.items = next;
            reason = "";
            return true;
        }

        public static bool Craft(SurvivorState player, GameCatalog catalog, CraftRecipe recipe,
            StationKind station, out string reason)
            => Craft(player, catalog, recipe, station, 1, out reason);

        public static bool Craft(SurvivorState player, GameCatalog catalog, CraftRecipe recipe,
            StationKind station, int quantity, out string reason)
        {
            if (recipe == null || (recipe.station != StationKind.Hand && recipe.station != station))
            { reason = "맞는 제작 시설이 필요합니다"; return false; }
            if (quantity < 1 || quantity > catalog.stackLimit || recipe.result.amount <= 0
                || (long)recipe.result.amount * quantity > catalog.stackLimit)
            { reason = "제작 수량이 올바르지 않습니다"; return false; }
            var costs = new ItemAmount[recipe.ingredients.Length];
            for (int i = 0; i < costs.Length; i++)
            {
                long amount = (long)recipe.ingredients[i].amount * quantity;
                if (amount <= 0 || amount > int.MaxValue)
                { reason = "제작 수량이 올바르지 않습니다"; return false; }
                costs[i] = new ItemAmount(recipe.ingredients[i].item, (int)amount);
            }
            return Transact(player, catalog, costs,
                new[] { new ItemAmount(recipe.result.item, recipe.result.amount * quantity) }, out reason);
        }

        public static int MaxCraftCount(SurvivorState player, GameCatalog catalog, CraftRecipe recipe, StationKind station)
        {
            if (recipe == null || recipe.result.amount <= 0 || !catalog.ValidItem(recipe.result.item)
                || (recipe.station != StationKind.Hand && recipe.station != station)) return 0;
            var totals = new long[catalog.itemCount];
            foreach (var cost in recipe.ingredients)
            {
                if (!catalog.ValidItem(cost.item) || cost.amount <= 0) return 0;
                totals[cost.item] += cost.amount;
            }
            int maximum = catalog.stackLimit / recipe.result.amount;
            for (int i = 0; i < totals.Length; i++)
                if (totals[i] > 0) maximum = Math.Min(maximum, (int)(player.items[i] / totals[i]));
            long netGain = recipe.result.amount - totals[recipe.result.item];
            if (netGain > 0) maximum = Math.Min(maximum, (int)((catalog.stackLimit - player.items[recipe.result.item]) / netGain));
            // Test the final transaction too: consuming ingredients can free a full inventory slot.
            var preview = new SurvivorState { items = player.items };
            for (int quantity = maximum; quantity > 0; quantity--)
                if (Craft(preview, catalog, recipe, station, quantity, out _)) return quantity;
            return 0;
        }

        public static bool Upgrade(SurvivorState player, GameCatalog catalog, int item, StationKind station, out string reason)
        {
            if (station != StationKind.Forge || !catalog.ValidItem(item) || player.items[item] <= 0 || Array.IndexOf(player.equipment, item) < 0)
            { reason = "대장간에서 착용 장비를 강화하세요"; return false; }
            int level = player.upgrades[item];
            if (level >= catalog.upgradeLimit) { reason = "최대 강화 단계입니다"; return false; }
            if (!Transact(player, catalog, new[] { new ItemAmount(catalog.ingotItem, 2 + level * 2) }, Array.Empty<ItemAmount>(), out reason)) return false;
            player.upgrades[item]++;
            return true;
        }

        public static Vector2Int Footprint(BuildingSpec spec, int turns) => (turns & 1) == 0
            ? spec.footprint : new Vector2Int(spec.footprint.y, spec.footprint.x);

        public static bool Overlaps(Vector3 a, Vector2Int sizeA, Vector3 b, Vector2Int sizeB) =>
            Mathf.Abs(a.x - b.x) < (sizeA.x + sizeB.x) * .5f - .01f &&
            Mathf.Abs(a.z - b.z) < (sizeA.y + sizeB.y) * .5f - .01f;

        public static bool Place(WorldState world, SurvivorState player, GameCatalog catalog, string definition,
            Vector3 position, int turns, out string reason)
        {
            var spec = catalog.Building(definition);
            if (spec == null || !float.IsFinite(position.x) || !float.IsFinite(position.y) || !float.IsFinite(position.z)
                || Vector3.Distance(player.position, position) > 6f)
            { reason = "가까운 평지에 설치하세요"; return false; }
            turns = ((turns % 4) + 4) % 4;
            position.x = Mathf.Round(position.x); position.z = Mathf.Round(position.z);
            foreach (var existing in world.buildings)
                if (Overlaps(position, Footprint(spec, turns), existing.position, Footprint(catalog.Building(existing.definition), existing.quarterTurns)))
                { reason = "다른 건물과 겹칩니다"; return false; }
            if (!Transact(player, catalog, spec.cost, Array.Empty<ItemAmount>(), out reason)) return false;
            world.buildings.Add(new PlacedBuilding { id = Guid.NewGuid().ToString("N"), definition = definition,
                owner = player.id, position = position, quarterTurns = turns });
            return true;
        }

        public static bool Dismantle(WorldState world, SurvivorState player, GameCatalog catalog, string id, out string reason)
        {
            var building = world.buildings.Find(b => b.id == id);
            if (building == null || building.owner != player.id || Vector3.Distance(player.position, building.position) > 4f)
            { reason = "가까운 내 건물만 회수할 수 있습니다"; return false; }
            if (catalog.Building(building.definition).use == BuildingUse.Garden && building.plantedAt >= 0)
            { reason = "작물을 수확한 뒤 회수하세요"; return false; }
            if (!Transact(player, catalog, Array.Empty<ItemAmount>(), catalog.Building(building.definition).cost, out reason)) return false;
            world.buildings.Remove(building);
            return true;
        }
    }
}
