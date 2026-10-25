using UnityEngine;

namespace DoHwa.World
{
    public sealed class WorldBuilding : MonoBehaviour
    {
        public string InstanceId { get; private set; }
        public BuildingSpec Spec { get; private set; }
        public void Bind(PlacedBuilding state, BuildingSpec spec)
        {
            InstanceId = state.id;
            Spec = spec;
            if (TryGetComponent<GardenPresentation>(out var garden))
                garden.Bind(WorldRuntime.Instance, this);
        }
    }
}
