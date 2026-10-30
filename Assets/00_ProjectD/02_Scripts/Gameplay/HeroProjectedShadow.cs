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
        [System.Serializable]
        private struct ShadowFrames
        {
            public float[] times;
            public Sprite[] sprites;
        }
        [System.Serializable]
        private struct ShadowMotion
        {
            public string state;
            public ShadowFrames[] directions;
        }
        [SerializeField] private ShadowMotion[] motions = System.Array.Empty<ShadowMotion>();
        private MaterialPropertyBlock properties;
        private MaterialPropertyBlock shadowBodyMasks;
        private MaterialPropertyBlock shadowSkinMasks;
        private DohwaLayeredCharacter layeredCharacter;
        private HeroMotor heroMotor;
        private World.WorldEnemy enemy;
        private Animator animator;
        private SpriteRenderer[] outfitShadows;
        private bool hasLayeredCharacter;
        private bool hasHeroMotor;
        private bool hasEnemy;
        private int lightFacing;
        public int LightFacing => lightFacing;
        private readonly RaycastHit[] groundHits = new RaycastHit[16];
        private static readonly int UseMask = Shader.PropertyToID("_UseMask");
        private static readonly int ShowCovered = Shader.PropertyToID("_ShowCovered");

        public void Configure(SpriteRenderer[] sourceLayers, SpriteRenderer[] shadowLayers)
        {
            sources = sourceLayers;
            shadows = shadowLayers;
        }

        private void Awake()
        {
            properties = new MaterialPropertyBlock();
            shadowBodyMasks = new MaterialPropertyBlock();
            shadowSkinMasks = new MaterialPropertyBlock();
            hasLayeredCharacter = TryGetComponent<DohwaLayeredCharacter>(out layeredCharacter);
            if (!hasLayeredCharacter)
                for (int i = 0; i < sources.Length; i++)
                    if (sources[i].transform.parent.TryGetComponent<DohwaLayeredCharacter>(out layeredCharacter))
                    {
                        hasLayeredCharacter = true;
                        break;
                    }
            hasHeroMotor = TryGetComponent<HeroMotor>(out heroMotor);
            hasEnemy = TryGetComponent<World.WorldEnemy>(out enemy);
            sources[0].TryGetComponent<Animator>(out animator);
            if (hasLayeredCharacter)
            {
                outfitShadows = new SpriteRenderer[11];
                System.Array.Copy(shadows, shadows.Length - 11, outfitShadows, 0, 11);
            }
            for (int i = 0; i < shadows.Length; i++)
            {
                sources[i].shadowCastingMode = ShadowCastingMode.Off;
                shadows[i].shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                shadows[i].receiveShadows = false;
                shadows[i].ResetBounds();
            }
        }

        private void LateUpdate()
        {
            var visualRoot = sources[0].transform;
            Vector3 groundPoint;
            float footHeight;
            if (heroCamera)
            {
                groundPoint = heroCamera.GroundPoint;
                footHeight = heroCamera.FootHeight;
            }
            else
            {
                groundPoint = visualRoot.position - Vector3.up * .003f;
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
                }
            }

            // Keep the visible billboard unchanged. Only the invisible caster stands upright.
            Vector3 lightDirection = -RenderSettings.sun.transform.forward;
            Vector3 lightForward = -Vector3.ProjectOnPlane(lightDirection, Vector3.up).normalized;
            Quaternion casterRotation = Quaternion.LookRotation(lightForward, Vector3.up);
            Vector3 worldFacing = hasHeroMotor ? heroMotor.Facing : hasEnemy ? enemy.WorldFacing
                : Quaternion.Euler(0f, Camera.main.transform.eulerAngles.y, 0f)
                    * (Quaternion.Euler(0f, -(int)layeredCharacter.Facing * 45f, 0f) * Vector3.back);
            Vector3 lightRight = Vector3.Cross(Vector3.up, lightForward);
            lightFacing = (Mathf.RoundToInt(Mathf.Atan2(Vector3.Dot(worldFacing, lightRight),
                -Vector3.Dot(worldFacing, lightForward)) * Mathf.Rad2Deg / 45f) + 8) % 8;
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

                // Copy the current outfit masks, not just the unclothed body silhouette.
                source.GetPropertyBlock(properties);
                bool masked = source.sharedMaterial.shader.name == "DoHwa/Layered Body";
                properties.SetFloat(UseMask, masked ? 1f : 0f);
                properties.SetFloat(ShowCovered, masked ? source.sharedMaterial.GetFloat(ShowCovered) : 0f);
                shadow.SetPropertyBlock(properties);
            }
            if (hasLayeredCharacter)
            {
                layeredCharacter.RenderShadow(outfitShadows, (AvatarFacing)lightFacing,
                    shadowBodyMasks, shadowSkinMasks);
                for (int i = 0; i < outfitShadows.Length; i++)
                {
                    var shadow = outfitShadows[i];
                    shadow.GetPropertyBlock(properties);
                    bool masked = i == 1 || i == 10;
                    properties.SetFloat(UseMask, masked ? 1f : 0f);
                    properties.SetFloat(ShowCovered, i == 10 ? 1f : 0f);
                    shadow.SetPropertyBlock(properties);
                }
            }
            else if (hasEnemy)
            {
                var state = animator.GetCurrentAnimatorStateInfo(0);
                float phase = state.loop ? Mathf.Repeat(state.normalizedTime, 1f)
                    : Mathf.Clamp01(state.normalizedTime);
                for (int i = 0; i < motions.Length; i++)
                {
                    if (Animator.StringToHash(motions[i].state) != state.shortNameHash) continue;
                    var frames = motions[i].directions[lightFacing];
                    int frame = 0;
                    while (frame + 1 < frames.times.Length && frames.times[frame + 1] <= phase) frame++;
                    shadows[0].sprite = frames.sprites[frame];
                    break;
                }
            }
        }
    }
}
