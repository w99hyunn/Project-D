using UnityEngine;

namespace DoHwa.World
{
    // Scene-authored communal garden; crop progress uses the same saved state as player-built beds.
    public sealed class WorldGardenSite : MonoBehaviour
    {
        [SerializeField] private string persistentId;
        public string PersistentId => persistentId;
    }
}
