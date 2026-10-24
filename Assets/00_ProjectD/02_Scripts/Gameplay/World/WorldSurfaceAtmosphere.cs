using UnityEngine;
using UnityEngine.Rendering;

namespace DoHwa.World
{
    // Surface scenes own geometry; this session-owned rig keeps lighting continuous.
    public sealed class WorldSurfaceAtmosphere : MonoBehaviour
    {
        [SerializeField] private WorldRuntime world;
        [SerializeField] private OpenWorldDefinition definition;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Light sunlight;
        [SerializeField] private Volume atmosphereVolume;
        private Color originalBackground, originalFog, originalAmbient;
        private bool wasSurface;

        private void Awake()
        {
            originalBackground=worldCamera.backgroundColor;
            originalFog=RenderSettings.fogColor;
            originalAmbient=RenderSettings.ambientLight;
        }
        private void LateUpdate()
        {
            if(world.LocalPlayer==null)return;
            bool surface=world.LocalPlayer.mapScene!="Moonshadow";
            sunlight.enabled=surface;atmosphereVolume.enabled=surface;
            if(!surface){
                if(wasSurface){worldCamera.backgroundColor=originalBackground;RenderSettings.fogColor=originalFog;RenderSettings.ambientLight=originalAmbient;}
                wasSurface=false;return;
            }
            var position=world.Hero.transform.position;
            Color sky=Color.clear,light=Color.clear;
            for(int i=0;i<definition.regions.Length;i++){
                float weight=definition.RegionWeight(position.x,position.z,i);
                sky+=definition.regions[i].atmosphere*weight;
                light+=definition.regions[i].sunlight*weight;
            }
            // Spatial blending handles boundaries; damping also covers travel and corrections.
            float t=wasSurface?1-Mathf.Exp(-Time.deltaTime*2):1;
            worldCamera.backgroundColor=Color.Lerp(worldCamera.backgroundColor,sky,t);
            RenderSettings.fogColor=Color.Lerp(RenderSettings.fogColor,sky,t);
            RenderSettings.ambientLight=Color.Lerp(RenderSettings.ambientLight,Color.Lerp(originalAmbient,sky,.25f),t);
            sunlight.color=Color.Lerp(sunlight.color,light,t);
            RenderSettings.sun=sunlight;
            wasSurface=true;
        }
        private void OnDestroy()
        {
            RenderSettings.fogColor=originalFog;RenderSettings.ambientLight=originalAmbient;
        }
    }
}
