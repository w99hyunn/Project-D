using System;
using UnityEngine;

namespace DoHwa.World
{
    [Serializable] public struct ItemAmount
    {
        public int item;
        [Min(1)] public int amount;
        public ItemAmount(int item, int amount) { this.item = item; this.amount = amount; }
    }

    public enum StationKind { Hand, Workbench, Furnace, Forge, Alchemy }

    [Serializable] public sealed class CraftRecipe
    {
        public string id;
        public string title;
        public StationKind station;
        public ItemAmount[] ingredients;
        public ItemAmount result;
    }

    [Serializable] public sealed class BuildingSpec
    {
        public string id;
        public string title;
        public GameObject prefab;
        public Vector2Int footprint = Vector2Int.one;
        public StationKind station;
        public ItemAmount[] cost;
    }

    [CreateAssetMenu(menuName = "DoHwa/World/Game Catalog")]
    public sealed class GameCatalog : ScriptableObject
    {
        [Min(1)] public int itemCount = 12;
        [Min(1)] public int stackLimit = 999;
        [Min(1)] public int inventorySlots = 36;
        public ItemAmount[] starterItems;
        public int[] equipmentParts;
        public CraftRecipe[] recipes;
        public BuildingSpec[] buildings;
        public int woodItem = 4;
        public int oreItem = 5;
        public int ingotItem = 11;
        [Min(1)] public int upgradeLimit = 5;
        public Vector3 spawnPoint;

        public bool ValidItem(int id) => id >= 0 && id < itemCount;
        public CraftRecipe Recipe(string id) => Array.Find(recipes, r => r.id == id);
        public BuildingSpec Building(string id) => Array.Find(buildings, b => b.id == id);
    }
}
