using UnityEngine;

namespace DoHwa.World
{
    [RequireComponent(typeof(Animator))]
    public sealed class GardenPresentation : MonoBehaviour
    {
        private WorldRuntime world;
        private WorldBuilding building;
        private Animator animator;
        private void Start()
        {
            world = FindFirstObjectByType<WorldRuntime>();
            TryGetComponent<WorldBuilding>(out building);
            TryGetComponent<Animator>(out animator);
        }
        private void Update()
        {
            var state = world.State.buildings.Find(b => b.id == building.InstanceId);
            // A removed building is destroyed at the end of the frame.
            if (state == null) return;
            animator.SetFloat("Growth", state.plantedAt < 0 ? 0 : .08f + SettlementRules.Growth(world.State, state, building.Spec.crop) * .92f);
        }
    }
}
