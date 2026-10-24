using UnityEngine;

namespace DoHwa.World
{
    // Scene-view overview only. Detailed terrain is owned by OpenWorldStreamer during play.
    public sealed class WorldSceneOverview : MonoBehaviour
    {
        private void Awake() => gameObject.SetActive(false);
    }
}
