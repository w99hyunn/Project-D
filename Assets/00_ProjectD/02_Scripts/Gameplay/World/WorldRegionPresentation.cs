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
        private string previousMap;
        private int previous=-1;
        private float refreshAt;
        private void Update()
        {
            if(Time.unscaledTime<refreshAt)return;refreshAt=Time.unscaledTime+.5f;
            if(!WorldMapSession.Instance.LocalMapReady)return;
            if(previousMap!=world.LocalPlayer.mapScene){previousMap=world.LocalPlayer.mapScene;previous=-1;}
            if(previousMap=="Moonshadow")
            {region.text="달그늘 길";climate.text="도깨비와 맺은 약속";return;}
            int biome=definition.Biome(hero.transform.position.x,hero.transform.position.z,world.State.seed);
            if(biome==previous)return;previous=biome;
            region.text=definition.regions[biome].title;
            climate.text=definition.regions[biome].climate;
            WorldMapSession.Instance.CurrentEnvironment.SetBlossoms(definition.regions[biome].blossoms);
        }
    }
}


