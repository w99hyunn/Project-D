using UnityEngine;

namespace DoHwa
{
    [DefaultExecutionOrder(150)]
    public sealed class HeroProjectedShadow : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer[] sources;
        [SerializeField] private SpriteRenderer[] shadows;
        private MaterialPropertyBlock properties;
        private readonly RaycastHit[] groundHits = new RaycastHit[16];
        [SerializeField] private HeroCamera heroCamera;
        private static readonly int GroundPlane = Shader.PropertyToID("_GroundPlane");

        public void Configure(SpriteRenderer[] sourceLayers, SpriteRenderer[] shadowLayers)
        {
            sources = sourceLayers;
            shadows = shadowLayers;
        }

        private void Awake()
        {
            properties = new MaterialPropertyBlock();
        }

        private void LateUpdate()
        {
            var visualRoot = sources[0].transform;
            Vector3 groundPoint;
            Vector3 groundNormal;
            float footHeight;
            if (heroCamera)
            {
                groundPoint = heroCamera.GroundPoint;
                groundNormal = heroCamera.GroundNormal;
                footHeight = heroCamera.FootHeight;
            }
            else
            {
                // Grounded sprite actors use the same receiver-plane calculation as the player.
                groundPoint = visualRoot.position - Vector3.up * .003f;
                groundNormal = Vector3.up;
                footHeight = 0f;
                float nearest = float.PositiveInfinity;
                int count = Physics.RaycastNonAlloc(transform.position + Vector3.up * .25f,
                    Vector3.down, groundHits, .75f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < count; i++)
                {
                    var hit = groundHits[i];
                    if (hit.collider.transform.IsChildOf(transform) || hit.distance >= nearest) continue;
                    nearest = hit.distance;
                    groundPoint = hit.point;
                    groundNormal = hit.normal;
                }
            }
            Vector3 groundOrigin = groundPoint + groundNormal * .008f;
            Vector4 groundPlane = new Vector4(groundNormal.x, groundNormal.y, groundNormal.z,
                -Vector3.Dot(groundNormal, groundOrigin));
            properties.SetVector(GroundPlane, groundPlane);
            Vector3 lightDirection = -RenderSettings.sun.transform.forward;
            Quaternion casterRotation = Quaternion.LookRotation(
                Vector3.ProjectOnPlane(lightDirection, Vector3.up), Vector3.up);
            // Remove the camera-facing rotation before casting the animated silhouette.
            Quaternion rotationDelta = casterRotation * Quaternion.Inverse(visualRoot.rotation);
            Vector3 casterOrigin = groundPoint + Vector3.up * (.003f - footHeight);
            for (int i = 0; i < sources.Length; i++)
            {
                var source = sources[i];
                var shadow = shadows[i];
                shadow.enabled = source.enabled && source.gameObject.activeInHierarchy;
                shadow.sprite = source.sprite;
                shadow.flipX = source.flipX;
                shadow.flipY = source.flipY;
                shadow.color = new Color(1f, 1f, 1f, source.color.a);
                shadow.transform.SetPositionAndRotation(
                    casterOrigin + rotationDelta * (source.transform.position - visualRoot.position),
                    rotationDelta * source.transform.rotation);
                shadow.transform.localScale = source.transform.lossyScale;
                shadow.SetPropertyBlock(properties);
                // The shader moves vertices onto the ground; culling must use that footprint.
                shadow.bounds = ProjectBounds(shadow, groundPlane, lightDirection);
            }
        }

        private static Bounds ProjectBounds(SpriteRenderer shadow, Vector4 groundPlane, Vector3 lightDirection)
        {
            Vector3 normal = new Vector3(groundPlane.x, groundPlane.y, groundPlane.z);
            Bounds local = shadow.localBounds;
            Vector3 minimum = Vector3.one * float.PositiveInfinity;
            Vector3 maximum = Vector3.one * float.NegativeInfinity;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 offset = new Vector3(
                    (corner & 1) == 0 ? -local.extents.x : local.extents.x,
                    (corner & 2) == 0 ? -local.extents.y : local.extents.y,
                    (corner & 4) == 0 ? -local.extents.z : local.extents.z);
                Vector3 point = shadow.transform.TransformPoint(local.center + offset);
                float height = Vector3.Dot(normal, point) + groundPlane.w;
                point -= lightDirection * (height / Mathf.Max(.25f, Vector3.Dot(normal, lightDirection)));
                minimum = Vector3.Min(minimum, point);
                maximum = Vector3.Max(maximum, point);
            }
            var bounds = new Bounds((minimum + maximum) * .5f, maximum - minimum);
            bounds.Expand(.1f);
            return bounds;
        }
    }
}
