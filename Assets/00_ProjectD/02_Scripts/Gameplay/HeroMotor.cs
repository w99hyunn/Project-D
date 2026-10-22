using UnityEngine;
using UnityEngine.InputSystem;

namespace DoHwa
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class HeroMotor : MonoBehaviour
    {
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private Animator animator;
        [SerializeField] private ModularCharacter modularCharacter;
        [SerializeField] private DohwaLayeredCharacter layeredCharacter;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private bool useOriginalFrontArt = true;
        [SerializeField] private bool useEightWayAnimations;
        [SerializeField] private float walkSpeed = 2.3f;
        [SerializeField] private float runSpeed = 4.8f;
        [SerializeField] private float acceleration = 24f;
        [SerializeField, Min(1f)] private float braking = 40f;
        [SerializeField, Min(1f)] private float turnAcceleration = 48f;
        [SerializeField, Range(0f, .2f)] private float dashInputBuffer = .1f;
        [SerializeField] private float dashSpeed = 12f;
        [SerializeField] private float dashDuration = 0.28f;
        [SerializeField] private float dashCooldown = 0.65f;

        private bool useModularCharacter;
        private CharacterController controller;
        private InputAction move;
        private InputAction run;
        private InputAction dash;
        private Vector3 velocity;
        private Vector3 facing = Vector3.back;
        public Vector3 Facing => facing;
        private Vector3 dashDirection;
        private float verticalSpeed;
        private float dashRemaining;
        private float cooldownRemaining;
        private float bufferedDashUntil = float.NegativeInfinity;
        public Vector3 PlanarVelocity => velocity;
        public bool IsDashing => dashRemaining > 0f;
        private int currentState;
        private int facingRow;
        private bool flip;
        private bool wasDashing;
        private static readonly int[,] States = CreateStates(new[] { "Front", "Side", "Back" });
        private static readonly int[,] EightWayStates = CreateStates(new[]
            { "South", "SouthEast", "East", "NorthEast", "North", "NorthWest", "West", "SouthWest" });

        public void Configure(Transform view, Animator animationController, SpriteRenderer renderer)
        {
            cameraTransform = view;
            animator = animationController;
            spriteRenderer = renderer;
        }

        private static int[,] CreateStates(string[] directions)
        {
            var states = new int[directions.Length, 4];
            string[] motions = { "Idle", "Walk", "Run", "Dash" };
            for (int direction = 0; direction < directions.Length; direction++)
                for (int motion = 0; motion < 4; motion++)
                    states[direction, motion] = Animator.StringToHash("Base Layer." + directions[direction] + motions[motion]);
            return states;
        }

        private void Awake()
        {
            TryGetComponent<CharacterController>(out controller);
            useModularCharacter = modularCharacter;
            move = new InputAction("Move", InputActionType.Value);
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            move.AddBinding("<Gamepad>/leftStick");
            run = new InputAction("Run", InputActionType.Button, "<Keyboard>/leftShift");
            run.AddBinding("<Keyboard>/rightShift");
            run.AddBinding("<Gamepad>/rightTrigger");
            dash = new InputAction("Dash", InputActionType.Button, "<Keyboard>/space");
            dash.AddBinding("<Gamepad>/buttonSouth");
        }

        private void OnEnable()
        {
            move.Enable();
            run.Enable();
            dash.Enable();
        }

        private void OnDisable()
        {
            move.Disable();
            run.Disable();
            dash.Disable();
            velocity = Vector3.zero;
            dashRemaining = 0f;
            wasDashing = false;
            currentState = 0;
            verticalSpeed = 0f;
            bufferedDashUntil = float.NegativeInfinity;
        }

        private void OnDestroy()
        {
            move.Dispose();
            run.Dispose();
            dash.Dispose();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            var input = Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f);
            var right = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
            var forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            var desired = right * input.x + forward * input.y;
            cooldownRemaining = Mathf.Max(0f, cooldownRemaining - dt);

            if (dashRemaining <= 0f && desired.sqrMagnitude > 0.01f)
                facing = desired.normalized;

            if (dash.WasPressedThisFrame()) bufferedDashUntil = Time.time + dashInputBuffer;
            if (Time.time <= bufferedDashUntil && cooldownRemaining <= 0f && dashRemaining <= 0f && controller.isGrounded)
            {
                dashDirection = facing;
                dashRemaining = dashDuration;
                cooldownRemaining = dashDuration + dashCooldown;
                bufferedDashUntil = float.NegativeInfinity;
            }

            bool dashing = dashRemaining > 0f;
            float dashStep = Mathf.Min(dt, dashRemaining);
            if (dashing)
            {
                velocity = dashDirection * dashSpeed;
                dashRemaining = Mathf.Max(0f, dashRemaining - dt);
            }
            else
            {
                if (wasDashing) velocity = desired * (run.IsPressed() ? runSpeed : walkSpeed);
                float response = desired.sqrMagnitude < .0001f ? braking
                    : Vector3.Dot(velocity, desired) < 0f ? turnAcceleration : acceleration;
                velocity = Vector3.MoveTowards(velocity, desired * (run.IsPressed() ? runSpeed : walkSpeed), response * dt);
            }

            verticalSpeed = controller.isGrounded ? -2f : Mathf.Max(-35f, verticalSpeed - 25f * dt);
            var before = transform.position;
            controller.Move(velocity * (dashing ? dashStep : dt) + Vector3.up * (verticalSpeed * dt));
            var displacement = Vector3.ProjectOnPlane(transform.position - before, Vector3.up);
            // A wall must end the dodge instead of retaining full dash velocity against it.
            if (dashing && dashStep > 0f && displacement.magnitude < dashSpeed * dashStep * .15f)
            { dashRemaining = 0f; velocity = Vector3.zero; }
            bool moving = displacement.sqrMagnitude > 0.000001f;

            if (!dashing || !wasDashing)
            {
                float x = Vector3.Dot(facing, right);
                float y = Vector3.Dot(facing, forward);
                facingRow = layeredCharacter || useModularCharacter || useEightWayAnimations
                    ? Mathf.RoundToInt(Mathf.Atan2(x, -y) * Mathf.Rad2Deg / 45f + 8f) % 8
                    : Mathf.Abs(x) > Mathf.Abs(y) * 0.85f ? 1 : y > 0f ? 2 : 0;
                flip = facingRow == 1 && x < 0f;
            }
            int motion = dashing ? 3 : !moving ? 0 : run.IsPressed() ? 2 : 1;
            if (layeredCharacter)
            {
                float layeredPlaybackRate = dashing ? 1f
                    : moving ? Mathf.Clamp(displacement.magnitude / Mathf.Max(dt, 0.0001f)
                        / (run.IsPressed() ? runSpeed : walkSpeed), 0f, 1f) : 1f;
                layeredCharacter.Animate((AvatarFacing)facingRow, (AvatarMotion)motion, dt, layeredPlaybackRate);
                wasDashing = dashing;
                return;
            }
            if (useModularCharacter)
            {
                float playbackRate = dashing ? 0.32f / dashDuration
                    : moving ? Mathf.Clamp(displacement.magnitude / Mathf.Max(dt, 0.0001f) / (run.IsPressed() ? runSpeed : walkSpeed), 0f, 1f) : 1f;
                modularCharacter.Animate((AvatarFacing)facingRow, (AvatarMotion)motion, dt, playbackRate);
                wasDashing = dashing;
                return;
            }
            int state = useEightWayAnimations ? EightWayStates[facingRow, motion] : States[useOriginalFrontArt ? 0 : facingRow, motion];
            if (state != currentState || (dashing && !wasDashing))
            {
                animator.speed = dashing ? (useEightWayAnimations ? 0.25f : 8f / 30f) / dashDuration : 1f;
                animator.Play(state, 0, 0f);
                currentState = state;
            }
            spriteRenderer.flipX = useEightWayAnimations ? facingRow >= 5 : !useOriginalFrontArt && flip;
            wasDashing = dashing;
        }
    }
}





