using UnityEngine;

namespace DoHwa.World
{
    [CreateAssetMenu(menuName = "DoHwa/World/Supply Cache")]
    public sealed class SupplyCacheDefinition : ScriptableObject
    {
        public string title;
        public ItemAmount[] rewards;
        public string requiredBoss;
        public string lockedHint = "수호자 처치 후 개방";
    }
}
