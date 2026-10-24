using System;
using UnityEngine;

namespace DoHwa.World
{
    public sealed class SeededResourceRegion : MonoBehaviour
    {
        private WorldRuntime world;
        [SerializeField] private Collider terrain;
        [SerializeField] private WorldResource[] nodes;
        [SerializeField] private Vector2 size = new(8, 14);
        [SerializeField] private int regionSalt = 173;
        [SerializeField] private float minimumSpacing = 1.8f;
        private int appliedSeed = -1;
        private void Awake() => world = WorldRuntime.Instance;
        private void Start() { world.Changed += Refresh; Refresh(); }
        private void OnDestroy() { world.Changed -= Refresh; }
        private void Refresh()
        {
            if (appliedSeed == world.State.seed) return;
            appliedSeed = world.State.seed;
            var random = new System.Random(appliedSeed ^ regionSalt);
            var positions = new Vector3[nodes.Length];
            for (int i = 0; i < nodes.Length; i++)
            {
                for (int attempt = 0; attempt < 96; attempt++)
                {
                    var candidate = transform.position + new Vector3(((float)random.NextDouble()-.5f)*size.x, 0, ((float)random.NextDouble()-.5f)*size.y);
                    var ray = new Ray(candidate + Vector3.up * 100, Vector3.down);
                    if (!terrain.Raycast(ray, out var hit, 200) || Vector3.Angle(hit.normal,Vector3.up)>25) continue;
                    bool overlaps = false;
                    for (int j = 0; j < i; j++) if (Vector3.Distance(positions[j],hit.point)<minimumSpacing) { overlaps=true;break; }
                    if (overlaps) continue;
                    nodes[i].transform.position = hit.point;
                    break;
                }
                positions[i] = nodes[i].transform.position;
            }
        }
    }
}
