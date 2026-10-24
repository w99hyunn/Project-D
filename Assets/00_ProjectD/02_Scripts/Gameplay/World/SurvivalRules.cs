using System;
using UnityEngine;

namespace DoHwa.World
{
    public static class SurvivalRules
    {
        public static bool Tick(WorldState world, SurvivorState player, SurvivalDefinition definition, float seconds)
        {
            if (player.health <= 0 || seconds <= 0) return false;
            float beforeFood = player.food, beforeHealth = player.health;
            player.food = Mathf.Max(0, player.food - definition.foodPerSecond * seconds);
            if (player.food >= definition.regenerationFood)
            {
                float safeTime = Mathf.Clamp((float)(world.elapsedSeconds - player.lastDamageAt - definition.combatQuietSeconds), 0, seconds);
                player.health = Mathf.Min(100, player.health + definition.regenerationPerSecond * safeTime);
            }
            return beforeFood != player.food || beforeHealth != player.health;
        }

        public static bool Consume(SurvivorState player, GameCatalog catalog, int item, out string reason)
        {
            var effect = Array.Find(catalog.survival.consumables, e => e.item == item);
            if (effect == null || (effect.healing <= 0 || player.health >= 100) && (effect.food <= 0 || player.food >= 100))
            { reason = "지금은 사용할 수 없습니다"; return false; }
            if (!WorldRules.Transact(player, catalog, new[] { new ItemAmount(item, 1) }, Array.Empty<ItemAmount>(), out reason)) return false;
            player.health = Mathf.Min(100, player.health + effect.healing);
            player.food = Mathf.Min(100, player.food + effect.food);
            return true;
        }
    }
}
