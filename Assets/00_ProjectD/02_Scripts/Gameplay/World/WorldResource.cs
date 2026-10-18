using UnityEngine;

namespace DoHwa.World
{
    public sealed class WorldResource : MonoBehaviour
    {
        [SerializeField] private string resourceId;
        [SerializeField] private string title;
        [SerializeField] private ItemAmount yield;
        [SerializeField, Min(1)] private int respawnSeconds = 180;
        [SerializeField] private GameObject visual;
        public string Id => resourceId;
        public string Title => title;
        public ItemAmount Yield => yield;
        public int RespawnSeconds => respawnSeconds;
        public void Configure(string id, string label, ItemAmount reward, GameObject art)
        { resourceId=id; title=label; yield=reward; visual=art; }

        public void SetAvailable(bool available) { visual.SetActive(available); }
    }
}
