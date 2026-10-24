using UnityEngine;

namespace DoHwa.World
{
    [CreateAssetMenu(menuName = "DoHwa/World/Camp")]
    public sealed class CampDefinition : ScriptableObject
    {
        [Min(0)] public float combatQuietSeconds = 8;
        [Min(1)] public float restCooldown = 15;
        [Min(1)] public float healing = 100;
        [Min(0)] public float respawnProtection = 3;
    }
}
