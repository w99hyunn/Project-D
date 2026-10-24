using UnityEngine;

namespace DoHwa
{
    public sealed class BlossomDrift : MonoBehaviour
    {
        private Transform target;
        private System.Threading.CancellationTokenRegistration heroLifetime;
        [SerializeField] private float height = 6f;

        public void Configure(Transform followTarget) => target = followTarget;
        private void Start()
        {
            var world=World.WorldRuntime.Instance;
            if(!world || !world.Hero){enabled=false;return;}
            target=world.Hero.transform;
            heroLifetime=world.Hero.destroyCancellationToken.Register(()=>enabled=false);
        }
        private void OnDestroy() => heroLifetime.Dispose();

        private void LateUpdate()
        {
            // World-space particles keep their flight path when the emitter follows the hero.
            transform.position = target.position + Vector3.up * height;
        }
    }
}
