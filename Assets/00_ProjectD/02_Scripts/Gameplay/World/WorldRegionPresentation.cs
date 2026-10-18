using UnityEngine;
using TMPro;

namespace DoHwa.World
{
    public sealed class WorldRegionPresentation : MonoBehaviour
    {
        [SerializeField] private WorldRuntime world;
        [SerializeField] private HeroMotor hero;
        [SerializeField] private OpenWorldDefinition definition;
        [SerializeField] private TMP_Text region;
        [SerializeField] private GameObject blossomWeather;
        private int previous=-1;
        private float refreshAt;
        private void Update()
        {
            if(Time.unscaledTime<refreshAt)return;refreshAt=Time.unscaledTime+.5f;
            int biome=definition.Biome(hero.transform.position.x,hero.transform.position.z,world.State.seed);
            if(biome==previous)return;previous=biome;
            region.text=biome==0?"도화림":biome==1?"청죽해":"운무산맥";
            blossomWeather.SetActive(biome==0);
        }
    }
}
