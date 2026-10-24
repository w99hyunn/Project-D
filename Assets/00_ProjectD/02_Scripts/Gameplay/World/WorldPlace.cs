using System;
using UnityEngine;

namespace DoHwa.World
{
    [Serializable]
    public sealed class WorldPlace
    {
        public string id;
        public string title;
        public Vector2 center;
        public float radius=16;
        public float elevation;
        public Vector2 roadEntry;
        public float entryElevation;
    }
}
