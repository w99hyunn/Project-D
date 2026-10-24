using UnityEngine;
using UnityEngine.InputSystem;

namespace DoHwa
{
    [DefaultExecutionOrder(100)]
    public sealed class HeroCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Transform spriteVisual;
        [SerializeField] private float distance = 24.5f;
        [SerializeField] private float pitch = 50f;
        [SerializeField] private float yaw;
        [SerializeField] private float followSharpness = 12f;
        [SerializeField] private float nearDistance = 8f;
        [SerializeField] private float farDistance = 24.5f;
        [SerializeField] private float nearPitch = 36f;
        [SerializeField] private float farPitch = 50f;
        [SerializeField] private float zoomStep = 0.14f;
        [SerializeField] private float rotationSensitivity = 0.18f;
        [SerializeField] private float zoomSharpness = 10f;
        [SerializeField] private float footHeight = .02f;
        [SerializeField, Min(1f)] private float orbitSharpness = 18f;
        [SerializeField, Min(1f)] private float heightSharpness = 6f;
        [SerializeField, Min(1f)] private float teleportDistance = 8f;
        private readonly RaycastHit[] groundHits = new RaycastHit[16];
        public Vector3 GroundPoint { get; private set; }
        public Vector3 GroundNormal { get; private set; } = Vector3.up;
        public float FootHeight => footHeight;
        private float zoom;
        private float desiredZoom;
        private InputAction scroll;
        private InputAction orbit;
        private InputAction pointerDelta;
        private Vector3 focus;
        private Vector3 previousTarget;
        private float desiredYaw;

        private void Awake()
        {
            zoom = desiredZoom = Mathf.InverseLerp(nearDistance, farDistance, distance);
            desiredYaw = yaw;
            scroll = new InputAction("Camera Zoom", InputActionType.Value, "<Mouse>/scroll/y");
            orbit = new InputAction("Camera Orbit", InputActionType.Button, "<Mouse>/rightButton");
            pointerDelta = new InputAction("Camera Pointer Delta", InputActionType.Value, "<Mouse>/delta");
        }

        private void OnEnable()
        {
            scroll.Enable();
            orbit.Enable();
            pointerDelta.Enable();
        }

        private void OnDisable()
        {
            scroll.Disable();
            orbit.Disable();
            pointerDelta.Disable();
        }

        private void OnDestroy()
        {
            scroll.Dispose();
            orbit.Dispose();
            pointerDelta.Dispose();
        }

        public void SetZoom(float amount) => desiredZoom = Mathf.Clamp01(amount);

        public void Rotate(float horizontalPixels) => desiredYaw = Mathf.Repeat(desiredYaw + horizontalPixels * rotationSensitivity, 360f);

        public void Configure(Transform hero, Transform visual)
        {
            target = hero;
            spriteVisual = visual;
            Snap();
        }

        private void Start() => Snap();

        private void Snap()
        {
            focus = target.position + Vector3.up * 0.65f;
            previousTarget = target.position;
            distance = Mathf.Lerp(nearDistance, farDistance, zoom);
            pitch = Mathf.Lerp(nearPitch, farPitch, Mathf.SmoothStep(0f, 1f, zoom));
            ApplyPose();
        }

        private void LateUpdate()
        {
            bool overUi = UnityEngine.EventSystems.EventSystem.current
                && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
            if (!overUi)
            {
                float wheel = scroll.ReadValue<float>();
                float steps = Mathf.Abs(wheel) > 10f ? wheel / 120f : wheel;
                SetZoom(desiredZoom - Mathf.Clamp(steps, -3f, 3f) * zoomStep);
                if (orbit.IsPressed()) Rotate(pointerDelta.ReadValue<Vector2>().x);
            }
            yaw = Mathf.LerpAngle(yaw, desiredYaw, 1f - Mathf.Exp(-orbitSharpness * Time.deltaTime));
            zoom = Mathf.Lerp(zoom, desiredZoom, 1f - Mathf.Exp(-zoomSharpness * Time.deltaTime));
            distance = Mathf.Lerp(nearDistance, farDistance, zoom);
            pitch = Mathf.Lerp(nearPitch, farPitch, Mathf.SmoothStep(0f, 1f, zoom));
            var destination = target.position + Vector3.up * .65f;
            if ((target.position - previousTarget).sqrMagnitude > teleportDistance * teleportDistance) focus = destination;
            else
            {
                float height = Mathf.Lerp(focus.y, destination.y, 1f - Mathf.Exp(-heightSharpness * Time.deltaTime));
                focus = Vector3.Lerp(focus, destination, 1f - Mathf.Exp(-followSharpness * Time.deltaTime));
                focus.y = height;
            }
            previousTarget = target.position;
            ApplyPose();
        }

        private void ApplyPose()
        {
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            transform.position = focus - transform.forward * distance;
            // Camera-aligned sprite plane keeps pixel proportions intact in the perspective view.
            spriteVisual.rotation = transform.rotation;
            GroundPoint = target.position;
            GroundNormal = Vector3.up;
            float nearest = float.PositiveInfinity;
            int count = Physics.RaycastNonAlloc(target.position + Vector3.up * .25f,
                Vector3.down, groundHits, .45f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var hit = groundHits[i];
                if (hit.collider.transform.IsChildOf(target) || hit.distance >= nearest) continue;
                nearest = hit.distance;
                GroundPoint = hit.point;
                GroundNormal = hit.normal;
            }
            // Anchor the visible soles, rather than the controller's skin or sprite canvas origin.
            spriteVisual.position = GroundPoint + Vector3.up * .003f - spriteVisual.up * footHeight;
        }
    }
}

