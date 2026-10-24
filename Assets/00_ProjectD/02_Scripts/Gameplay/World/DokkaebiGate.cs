using UnityEngine;

namespace DoHwa.World
{
    public sealed class DokkaebiGate : MonoBehaviour
    {
        private WorldRuntime world;
        private bool subscribed;
        private System.Threading.CancellationTokenRegistration worldLifetime;
        [SerializeField] private DokkaebiPactDefinition pact;
        [SerializeField] private string gateId;
        [SerializeField] private bool returnGate;
        [SerializeField] private string destinationScene;
        [SerializeField] private Vector3 arrivalPosition;
        [SerializeField] private ParticleSystem flames;
        public string Id => gateId;
        public bool Open => world.State.pacts.Contains(pact.id);
        public string Hint => returnGate ? "[E] 도화림으로 돌아가기" : Open
            ? "[E] 달그늘 길로 들어가기" : "[E] " + pact.offeringHint;

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
        private void Refresh()
        {
            var emission = flames.emission;
            emission.rateOverTime = Open ? 28 : 5;
        }
        public bool Interact(SurvivorState player, out string reason)
        {
            if (!returnGate && !Open)
            {
                if (!WorldRules.Transact(player, world.Catalog, pact.offering,
                    System.Array.Empty<ItemAmount>(), out reason)) return false;
                world.State.pacts.Add(pact.id);
                reason = "약속이 맺어졌습니다. 달그늘 길이 열렸습니다";
                return true;
            }
            if (!Open) { reason = "아직 열리지 않은 길입니다"; return false; }
            player.mapScene = destinationScene;
            player.position = arrivalPosition;
            player.travelRevision++;
            reason = returnGate ? "도화림으로 돌아왔습니다" : "달그늘 길 — 파수꾼의 봉인을 풀어 주세요";
            return true;
        }
    }
}


