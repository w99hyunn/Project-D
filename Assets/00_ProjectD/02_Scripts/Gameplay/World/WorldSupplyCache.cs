using UnityEngine;

namespace DoHwa.World
{
    public sealed class WorldSupplyCache : MonoBehaviour
    {
        private WorldRuntime world;
        private bool subscribed;
        private System.Threading.CancellationTokenRegistration worldLifetime;
        [SerializeField] private string cacheId;
        [SerializeField] private SupplyCacheDefinition definition;
        [SerializeField] private Animator animator;
        public string Id => cacheId;
        public SupplyCacheDefinition Definition => definition;
        public bool Opened => world.State.openedCaches.Contains(cacheId);
        public bool Unlocked => string.IsNullOrEmpty(definition.requiredBoss)
            || world.State.defeatedBosses.Contains(definition.requiredBoss);
        private void Awake()
        {
            world = WorldRuntime.Instance;
            if (!world) { enabled = false; return; }
            worldLifetime = world.destroyCancellationToken.Register(StopSession);
        }
        private void StopSession()
        {
            enabled = false;
            if (!subscribed) return;
            world.Changed -= Refresh;
            subscribed = false;
        }
        private void Start() { world.Changed += Refresh; subscribed = true; Refresh(); }
        private void OnDestroy() { worldLifetime.Dispose(); StopSession(); }
        private void Refresh() => animator.SetBool("Opened", Opened);
    }
}


