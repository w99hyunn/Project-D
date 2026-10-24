using System;
using UnityEngine;

namespace DoHwa.World
{
    [Serializable] public sealed class ConsumableEffect
    {
        public int item;
        [Min(0)] public float healing;
        [Min(0)] public float food;
    }

    [CreateAssetMenu(menuName = "DoHwa/World/Survival")]
    public sealed class SurvivalDefinition : ScriptableObject
    {
        [Min(0)] public float foodPerSecond = .05f;
        [Range(0, 100)] public float regenerationFood = 60;
        [Min(0)] public float regenerationPerSecond = .5f;
        [Min(0)] public float combatQuietSeconds = 8;
        public ConsumableEffect[] consumables;
    }
}
