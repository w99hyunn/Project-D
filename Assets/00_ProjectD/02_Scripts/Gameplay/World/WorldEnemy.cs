using System;
using UnityEngine;

namespace DoHwa.World
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class WorldEnemy : MonoBehaviour
    {
        private WorldRuntime world;
        [SerializeField] private string enemyId;
        [SerializeField] private bool boss;
        [SerializeField] private float maximumHealth = 60;
        [SerializeField] private float speed = 1.5f;
        [SerializeField] private float aggroRadius = 9;
        [SerializeField] private float damage = 8;
        [SerializeField] private ItemAmount reward = new(5, 2);
        [SerializeField] private Transform visual;
        [SerializeField] private Animator animator;
        [SerializeField] private Transform telegraph;
        private CharacterController controller;
        private Vector3 home;
        private float health;
        private float attackAt;
        private readonly System.Collections.Generic.Dictionary<string, float> hitAt = new();
        private float strikeAt;
        private Vector3 strikePoint;
        private bool windingUp;
        private EnemyState state;
        public string Id => enemyId;
        public float Health => health;
        public bool IsBoss => boss;
        public event Action<WorldEnemy> Defeated;

        private void Awake()
        {
            world = WorldRuntime.Instance;
            TryGetComponent<CharacterController>(out controller);
            home = transform.position; health = maximumHealth;
        }

        private void Start()
        {
            state = world.State.enemies.Find(e => e.id == enemyId);
            if (state == null)
            {
                state = new EnemyState { id = enemyId, position = home, health = maximumHealth };
                world.State.enemies.Add(state);
            }
            health = state.health;
            transform.position = state.position;
            if (health <= 0 || (boss && world.State.defeatedBosses.Contains(enemyId))) gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            visual.rotation = Camera.main.transform.rotation;
        }

        private void Update()
        {
            if (!world.IsAuthority)
            {
                var replicated = world.State.enemies.Find(e => e.id == enemyId);
                if (replicated == null) return;
                health = replicated.health;
                controller.enabled = false;
                Face(replicated.position-transform.position);
                transform.position = Vector3.Lerp(transform.position, replicated.position, 12 * Time.deltaTime);
                visual.gameObject.SetActive(replicated.health > 0);
                telegraph.gameObject.SetActive(replicated.windingUp && replicated.health > 0);
                telegraph.position = replicated.strikePoint + Vector3.up * .04f;
                return;
            }
            if (health <= 0) return;
            var current = world.State.enemies.Find(e => e.id == enemyId);
            if (current != state && current != null) { state = current; health = state.health; }
            state.position = transform.position; state.health = health;
            state.windingUp = windingUp; state.strikePoint = strikePoint;
            SurvivorState target = null;
            float distance = aggroRadius;
            foreach (var player in world.State.players)
            {
                if (!world.ActivePlayers.Contains(player.id)) continue;
                float next = Vector3.Distance(transform.position, player.position);
                if (player.health > 0 && next < distance) { target = player; distance = next; }
            }
            if (windingUp)
            {
                if (Time.time < strikeAt) return;
                windingUp = false; telegraph.gameObject.SetActive(false);
                foreach (var player in world.State.players)
                    if (world.ActivePlayers.Contains(player.id) && Vector3.Distance(player.position, strikePoint) < (boss ? 3f : 1.5f))
                    {
                        SettlementRules.Damage(world.State, player, world.Catalog, damage);
                    }
                animator.SetTrigger("Attack"); world.Commit(); return;
            }
            if (Vector3.Distance(transform.position, home) >= 18) target = null;
            Vector3 destination = target == null ? home : target.position;
            Vector3 direction = Vector3.ProjectOnPlane(destination - transform.position, Vector3.up);
            Face(direction);
            bool moving = direction.magnitude > (target == null ? .2f : 1.7f);
            if (moving)
                controller.Move(direction.normalized * (speed * Time.deltaTime) + Vector3.down * (8 * Time.deltaTime));
            animator.SetBool("Moving", moving);
            if (target != null && !moving && Time.time >= attackAt)
            {
                strikePoint = target.position; strikeAt = Time.time + (boss ? 1.05f : .65f);
                attackAt = strikeAt + (boss ? 1.8f : 1.2f); windingUp = true;
                telegraph.position = strikePoint + Vector3.up * .04f;
                telegraph.gameObject.SetActive(true); animator.SetTrigger("Windup");
            }
        }

        private void Face(Vector3 direction)
        {
            if (direction.sqrMagnitude < .001f) return;
            var camera = Camera.main.transform;
            float x = Vector3.Dot(direction, Vector3.ProjectOnPlane(camera.right, Vector3.up).normalized);
            float y = Vector3.Dot(direction, Vector3.ProjectOnPlane(camera.forward, Vector3.up).normalized);
            animator.SetFloat("Facing", Mathf.Abs(x) > Mathf.Abs(y) ? x > 0 ? 1 : 3 : y > 0 ? 2 : 0);
        }

        public bool TryHit(string playerId)
        {
            if (!world.IsAuthority || health <= 0 || !world.ActivePlayers.Contains(playerId)
                || (hitAt.TryGetValue(playerId, out float nextHit) && Time.time < nextHit)) return false;
            var player = world.State.Player(playerId);
            if (player == null || Vector3.Distance(player.position, transform.position) > 2.8f) return false;
            hitAt[playerId] = Time.time + .35f;
            int weapon = player.equipment[0];
            health -= 15 + (weapon >= 0 ? player.upgrades[weapon] * 5 : 0);
            state.health = health;
            animator.SetTrigger("Hit");
            if (health > 0) return true;
            SettlementRules.Grant(player, world.Catalog, reward);
            if (boss) world.State.defeatedBosses.Add(enemyId);
            world.Commit(); Defeated?.Invoke(this);
            gameObject.SetActive(false);
            return true;
        }
    }
}
