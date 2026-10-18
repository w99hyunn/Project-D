using UnityEngine;

namespace DoHwa.World
{
    [CreateAssetMenu(menuName = "DoHwa/Steam Settings")]
    public sealed class SteamSettings : ScriptableObject
    {
        public uint appId = 480;
        public string lobbyNamespace = "dohwa-project-d";
        public string protocol = "1";
    }
}
