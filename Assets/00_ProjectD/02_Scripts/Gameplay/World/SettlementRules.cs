using System;
using UnityEngine;

namespace DoHwa.World
{
    public static class SettlementRules
    {
        public static float Growth(WorldState world, PlacedBuilding bed, CropDefinition crop) =>
            bed.plantedAt < 0 ? 0 : Mathf.Clamp01((float)((world.elapsedSeconds - bed.plantedAt) / crop.growthSeconds));

        public static bool Use(WorldState world, SurvivorState player, GameCatalog catalog, string id, out string reason)
        {
            var building = world.buildings.Find(b => b.id == id);
            if (building == null || Vector3.Distance(player.position, building.position) > 3f)
            { reason = "시설 가까이에서 이용하세요"; return false; }
            var spec = catalog.Building(building.definition);
            if (spec.use == BuildingUse.Camp)
            {
                if (world.elapsedSeconds - player.lastDamageAt < spec.camp.combatQuietSeconds)
                { reason = "전투를 벗어난 뒤 휴식하세요"; return false; }
                bool newCamp = player.campId != id;
                bool canHeal = world.elapsedSeconds - player.lastRestAt >= spec.camp.restCooldown && player.health < 100;
                if (!newCamp && !canHeal) { reason = player.health >= 100 ? "이미 충분히 쉬었습니다" : "잠시 후 다시 휴식하세요"; return false; }
                player.campId = id;
                player.campPosition = player.position;
                if (canHeal) { player.health = Mathf.Min(100, player.health + spec.camp.healing); player.lastRestAt = world.elapsedSeconds; }
                reason = newCamp ? "이 모닥불을 부활 거점으로 등록했습니다" : "체력을 회복했습니다";
                return true;
            }
            if (spec.use != BuildingUse.Garden) { reason = "이용할 수 없는 시설입니다"; return false; }
            if (building.plantedAt < 0)
            {
                if (!WorldRules.Transact(player, catalog, spec.crop.seeds, Array.Empty<ItemAmount>(), out reason)) return false;
                building.plantedAt = world.elapsedSeconds;
                reason = spec.crop.title + " 씨앗을 심었습니다";
                return true;
            }
            if (Growth(world, building, spec.crop) < 1) { reason = "아직 자라고 있습니다"; return false; }
            if (!WorldRules.Transact(player, catalog, Array.Empty<ItemAmount>(), spec.crop.harvest, out reason)) return false;
            building.plantedAt = -1;
            foreach (var reward in spec.crop.harvest)
            {
                JourneyDefinition.Record(world, WorldAction.Gather, "", reward.item, reward.amount);
                JourneyDefinition.Record(world, WorldAction.UseBuilding, building.definition, reward.item, reward.amount);
            }
            reason = spec.crop.title + " 수확 완료";
            return true;
        }

        // Rewards stay with their recipient if the bag cannot accept the entire grant.
        public static void Grant(SurvivorState player, GameCatalog catalog, ItemAmount reward)
        {
            if (WorldRules.Transact(player, catalog, Array.Empty<ItemAmount>(), new[] { reward }, out _)) return;
            player.pendingRewards.Add(reward);
        }

        public static bool Claim(WorldState world, SurvivorState player, GameCatalog catalog, string campId, out string reason)
        {
            var camp = world.buildings.Find(b => b.id == campId);
            if (camp == null || catalog.Building(camp.definition).use != BuildingUse.Camp || Vector3.Distance(player.position, camp.position) > 3f)
            { reason = "모닥불 가까이에서 보상을 받으세요"; return false; }
            int claimed = 0;
            for (int i = player.pendingRewards.Count - 1; i >= 0; i--)
                if (WorldRules.Transact(player, catalog, Array.Empty<ItemAmount>(), new[] { player.pendingRewards[i] }, out _))
                { player.pendingRewards.RemoveAt(i); claimed++; }
            reason = claimed > 0 ? "보관된 전리품을 받았습니다" : player.pendingRewards.Count > 0 ? "가방에 공간이 부족합니다" : "보관된 전리품이 없습니다";
            return claimed > 0;
        }

        public static void Damage(WorldState world, SurvivorState player, GameCatalog catalog, float damage)
        {
            if (world.elapsedSeconds < player.protectedUntil || player.health <= 0) return;
            player.lastDamageAt = world.elapsedSeconds;
            player.health = Mathf.Max(0, player.health - damage);
            if (player.health > 0) return;
            var camp = world.buildings.Find(b => b.id == player.campId && catalog.Building(b.definition).use == BuildingUse.Camp);
            player.position = camp == null ? catalog.spawnPoint : player.campPosition;
            player.health = 100;
            player.protectedUntil = world.elapsedSeconds + (camp == null ? 3 : catalog.Building(camp.definition).camp.respawnProtection);
            player.respawns++;
        }
    }
}
