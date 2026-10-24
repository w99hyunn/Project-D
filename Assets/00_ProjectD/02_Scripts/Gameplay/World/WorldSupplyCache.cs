using UnityEngine;

namespace DoHwa.World
{
    public sealed class WorldSupplyCache : MonoBehaviour
    {
        private WorldRuntime world;
        [SerializeField] private string cacheId;
        [SerializeField] private SupplyCacheDefinition definition;
        [SerializeField] private Animator animator;
        public string Id => cacheId;
        public SupplyCacheDefinition Definition => definition;
        public bool Opened => world.State.openedCaches.Contains(cacheId);
        public bool Unlocked => string.IsNullOrEmpty(definition.requiredBoss)
            || world.State.defeatedBosses.Contains(definition.requiredBoss);
        private void Awake() => world = WorldRuntime.Instance;
        private void Start() { world.Changed += Refresh; Refresh(); }
        private void OnDestroy() { world.Changed -= Refresh; }
        private void Refresh() => animator.SetBool("Opened", Opened);
    }
}
