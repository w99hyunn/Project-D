using UnityEngine;

namespace DoHwa.World
{
    [RequireComponent(typeof(Animator))]
    public sealed class GardenPresentation : MonoBehaviour
    {
        private WorldRuntime world;
        private WorldBuilding building;
        private Animator animator;
        private bool bound;
        private bool wasPlanted;
        public void Bind(WorldRuntime runtime, WorldBuilding view)
        {
            TryGetComponent<Animator>(out animator);
            world = runtime;
            building = view;
            bound = true;
            var state = world.State.buildings.Find(b => b.id == building.InstanceId);
            wasPlanted = state.plantedAt >= 0;
            animator.SetFloat("Growth", wasPlanted ? .08f + SettlementRules.Growth(world.State, state, building.Spec.crop) * .92f : 0);
        }
        private void Update()
        {
            if (!bound) return;
            var state = world.State.buildings.Find(b => b.id == building.InstanceId);
            // A removed building is destroyed at the end of the frame.
            if (state == null) return;
            bool planted = state.plantedAt >= 0;
            if (planted != wasPlanted)
            {
                animator.ResetTrigger(planted ? "Harvest" : "Plant");
                animator.SetTrigger(planted ? "Plant" : "Harvest");
                wasPlanted = planted;
            }
            animator.SetFloat("Growth", planted ? .08f + SettlementRules.Growth(world.State, state, building.Spec.crop) * .92f : 0);
        }
    }
}
