using System;
using System.Collections.Generic;
using UnityEngine;

namespace DoHwa.World
{
    [Serializable] public struct DungeonRoom
    {
        public Vector2Int cell;
        public int parent;
        public bool boss;
        public int theme;
    }

    public static class DungeonLayout
    {
        // A connected tree with a farthest-room boss; no inaccessible random islands.
        public static DungeonRoom[] Generate(int seed, int count)
        {
            if (count < 2 || count > 64) throw new ArgumentOutOfRangeException(nameof(count));
            var random = new System.Random(seed);
            var rooms = new List<DungeonRoom> { new() { cell = Vector2Int.zero, parent = -1 } };
            var occupied = new HashSet<Vector2Int> { Vector2Int.zero };
            var directions = new[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
            while (rooms.Count < count)
            {
                var candidates = new List<(int parent, Vector2Int cell)>();
                for (int i = 0; i < rooms.Count; i++) foreach (var direction in directions)
                    if (!occupied.Contains(rooms[i].cell+direction)) candidates.Add((i,rooms[i].cell+direction));
                var next = candidates[random.Next(candidates.Count)];
                occupied.Add(next.cell);
                rooms.Add(new DungeonRoom { cell = next.cell, parent = next.parent, theme = random.Next(3) });
            }
            int bossIndex = 1, depthMax = 0;
            for (int i = 1; i < rooms.Count; i++)
            {
                int depth=0, p=i;
                while (p>0) { p=rooms[p].parent;depth++; }
                if (depth>depthMax) { depthMax=depth;bossIndex=i; }
            }
            var bossRoom = rooms[bossIndex]; bossRoom.boss=true;rooms[bossIndex]=bossRoom;
            return rooms.ToArray();
        }
    }
}
