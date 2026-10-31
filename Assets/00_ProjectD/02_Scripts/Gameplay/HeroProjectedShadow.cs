using UnityEngine;
using UnityEngine.Rendering;

namespace DoHwa
{
    [DefaultExecutionOrder(150)]
    public sealed class HeroProjectedShadow : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer[] sources;
        [SerializeField] private SpriteRenderer[] shadows;
        [SerializeField] private HeroCamera heroCamera;
        [SerializeField, Min(.1f)] private float contactRadius = .43f;
        private readonly RaycastHit[] groundHits = new RaycastHit[16];
        private Mesh contactMesh;
        private MeshRenderer contactRenderer;
        private Vector3[] vertices;
        private Vector3 previousGround = Vector3.positiveInfinity;
        private float nextSurfaceUpdate;
        private MaterialPropertyBlock environmentProperties;
        private static readonly int ActorGround = Shader.PropertyToID("_ActorGround");

        public void Configure(SpriteRenderer[] sourceLayers, SpriteRenderer[] shadowLayers)
        {
            sources = sourceLayers;
            shadows = shadowLayers;
        }

        private void Awake()
        {
            environmentProperties = new MaterialPropertyBlock();
            foreach (var source in sources)
            {
                source.shadowCastingMode = ShadowCastingMode.Off;
                source.receiveShadows = true;
            }
            foreach (var shadow in shadows)
            {
                shadow.enabled = false;
                shadow.shadowCastingMode = ShadowCastingMode.Off;
            }
            var contact = new GameObject("Contact Shadow", typeof(MeshFilter), typeof(MeshRenderer));
            contact.transform.SetParent(transform, false);
            contact.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            contact.transform.localScale = Vector3.one;
            contact.TryGetComponent<MeshFilter>(out var filter);
            contact.TryGetComponent<MeshRenderer>(out contactRenderer);
            contactRenderer.sharedMaterial = shadows[0].sharedMaterial;
            contactRenderer.shadowCastingMode = ShadowCastingMode.Off;
            contactRenderer.receiveShadows = false;
            contactRenderer.lightProbeUsage = LightProbeUsage.Off;
            contactRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            contactMesh = new Mesh { name = "Terrain contact shadow" };
            contactMesh.MarkDynamic();
            filter.sharedMesh = contactMesh;
            vertices = new Vector3[81];
            var uv = new Vector2[81];
            var triangles = new int[384];
            for (int z = 0; z <= 8; z++)
                for (int x = 0; x <= 8; x++) uv[z * 9 + x] = new Vector2(x / 8f, z / 8f);
            int index = 0;
            for (int z = 0; z < 8; z++)
                for (int x = 0; x < 8; x++)
                {
                    int corner = z * 9 + x;
                    triangles[index++] = corner;
                    triangles[index++] = corner + 9;
                    triangles[index++] = corner + 1;
                    triangles[index++] = corner + 1;
                    triangles[index++] = corner + 9;
                    triangles[index++] = corner + 10;
                }
            contactMesh.vertices = vertices;
            contactMesh.uv = uv;
            contactMesh.triangles = triangles;
        }

        private Vector3 GroundAt(Vector3 point)
        {
            int count = Physics.RaycastNonAlloc(point + Vector3.up * .4f, Vector3.down,
                groundHits, 1.2f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            float nearest = float.PositiveInfinity;
            Vector3 ground = point;
            for (int i = 0; i < count; i++)
            {
                var hit = groundHits[i];
                if (hit.collider.transform.IsChildOf(transform) || hit.distance >= nearest) continue;
                nearest = hit.distance;
                ground = hit.point;
            }
            return ground;
        }

        private void LateUpdate()
        {
            bool visible = false;
            foreach (var source in sources) visible |= source.enabled && source.gameObject.activeInHierarchy;
            foreach (var shadow in shadows) shadow.enabled = false;
            contactRenderer.enabled = visible;
            Vector3 ground = heroCamera ? heroCamera.GroundPoint : GroundAt(transform.position);
            foreach (var source in sources)
            {
                source.GetPropertyBlock(environmentProperties);
                environmentProperties.SetVector(ActorGround, new Vector4(ground.x, ground.y + .04f, ground.z, 1f));
                source.SetPropertyBlock(environmentProperties);
            }
            if ((ground - previousGround).sqrMagnitude < .0004f && Time.unscaledTime < nextSurfaceUpdate) return;
            nextSurfaceUpdate = Time.unscaledTime + .2f;
            previousGround = ground;
            var contact = contactRenderer.transform;
            contact.SetPositionAndRotation(ground, Quaternion.identity);
            for (int z = 0; z <= 8; z++)
                for (int x = 0; x <= 8; x++)
                {
                    Vector3 point = ground + new Vector3(x / 4f - 1f, 0f, z / 4f - 1f) * contactRadius;
                    vertices[z * 9 + x] = contact.InverseTransformPoint(GroundAt(point) + Vector3.up * .012f);
                }
            contactMesh.vertices = vertices;
            contactMesh.RecalculateBounds();
        }

        private void OnDisable()
        {
            if (contactRenderer) contactRenderer.enabled = false;
        }

        private void OnDestroy()
        {
            Destroy(contactMesh);
        }
    }
}

