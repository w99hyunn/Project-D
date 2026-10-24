using UnityEngine;

namespace DoHwa.World
{
    [CreateAssetMenu(menuName = "DoHwa/World/Crop")]
    public sealed class CropDefinition : ScriptableObject
    {
        public string title;
        public ItemAmount[] seeds;
        public ItemAmount[] harvest;
        [Min(1)] public float growthSeconds = 180;
    }
}
