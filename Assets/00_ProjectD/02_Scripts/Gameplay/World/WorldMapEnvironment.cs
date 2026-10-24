using UnityEngine;

namespace DoHwa.World
{
    public sealed class WorldMapEnvironment : MonoBehaviour
    {
        [SerializeField] private GameObject blossomWeather;
        public void SetBlossoms(bool visible) { if(blossomWeather) blossomWeather.SetActive(visible); }
    }
}
