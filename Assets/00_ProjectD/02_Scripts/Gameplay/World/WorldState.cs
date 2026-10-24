using System;
using System.Collections.Generic;
using UnityEngine;

namespace DoHwa.World
{
    public static class PlayerIdentity
    {
        public static string Get()
        {
            if (SteamRuntime.Ready)
            {
                string steamIdentity = "steam:" + Steamworks.SteamUser.GetSteamID().m_SteamID;
                if (PlayerPrefs.GetString("LastSteamPlayerIdentity", "") != steamIdentity)
                { PlayerPrefs.SetString("LastSteamPlayerIdentity", steamIdentity); PlayerPrefs.Save(); }
                return steamIdentity;
            }
            string cachedSteamIdentity = PlayerPrefs.GetString("LastSteamPlayerIdentity", "");
            if (cachedSteamIdentity.Length > 0) return cachedSteamIdentity;
            string id = PlayerPrefs.GetString("WorldPlayerIdentity", "");
            if (id.Length > 0) return id;
            id = Guid.NewGuid().ToString("N");
            PlayerPrefs.SetString("WorldPlayerIdentity", id); PlayerPrefs.Save();
            return id;
        }
    }
    [Serializable] public sealed class SurvivorState
    {
        public string id;
        public int[] items;
        public int[] upgrades;
        public int[] equipment = { 0, -1, -1, -1, -1, -1, -1, -1, -1 };
        public float health = 100;
        public float food = 100;
        public Vector3 position;
        public int respawns;
        public int travelRevision;
        public string mapScene;
        public string campId;
        public Vector3 campPosition;
        public double lastDamageAt = -100;
        public double lastRestAt = -100;
        public double protectedUntil;
        public List<ItemAmount> pendingRewards = new();
    }

    [Serializable] public sealed class PlacedBuilding
    {
        public string mapScene;
        public string id;
        public string definition;
        public string owner;
        public Vector3 position;
        public int quarterTurns;
        public double plantedAt = -1;
    }

    [Serializable] public sealed class ResourceState
    {
        public string id;
        public long availableAt;
    }

    [Serializable] public sealed class WorldState
    {
        public int version = 1;
        public int revision;
        public int seed = Guid.NewGuid().GetHashCode() & int.MaxValue;
        public string id = "peach-valley";
        public string title = "도화림";
        public long createdAt;
        public double elapsedSeconds;
        public List<SurvivorState> players = new();
        public List<PlacedBuilding> buildings = new();
        public List<ResourceState> resources = new();
        public List<string> defeatedBosses = new();
        public List<EnemyState> enemies = new();
        public List<JourneyCounter> journey = new();
        public List<string> openedCaches = new();
        public List<string> pacts = new();

        public SurvivorState Player(string id) => players.Find(p => p.id == id);
        public SurvivorState GetOrCreatePlayer(string id, GameCatalog catalog)
        {
            var player = Player(id);
            if (player != null) return player;
            player = new SurvivorState { id = id, items = new int[catalog.itemCount],
                upgrades = new int[catalog.itemCount], position = catalog.spawnPoint, mapScene = "PeachValley" };
            foreach (var amount in catalog.starterItems) player.items[amount.item] += amount.amount;
            players.Add(player);
            return player;
        }
    }

    [Serializable] public sealed class EnemyState
    {
        public string id;
        public Vector3 position;
        public float health;
        public bool windingUp;
        public Vector3 strikePoint;
    }
}
