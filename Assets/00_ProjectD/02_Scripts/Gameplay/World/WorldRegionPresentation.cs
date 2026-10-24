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
        [SerializeField] private TMP_Text climate;
        [SerializeField] private GameObject blossomWeather;
        private int previous=-1;
        private float refreshAt;
        private void Update()
        {
            if(Time.unscaledTime<refreshAt)return;refreshAt=Time.unscaledTime+.5f;
            int biome=definition.Biome(hero.transform.position.x,hero.transform.position.z,world.State.seed);
            if(biome==previous)return;previous=biome;
            region.text=definition.regions[biome].title;
            climate.text=definition.regions[biome].climate;
            blossomWeather.SetActive(definition.regions[biome].blossoms);
        }
    }
}


