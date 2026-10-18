using UnityEngine;

namespace DoHwa.World
{
    [CreateAssetMenu(menuName="DoHwa/Open World Definition")]
    public sealed class OpenWorldDefinition : ScriptableObject
    {
        public int worldSize=8192;
        public int chunkSize=64;
        public int resolution=32;
        public int viewRadius=2;
        public Material ground;
        public GameObject peachTree;
        public GameObject bamboo;
        public GameObject pine;
        public GameObject grass;
        public GameObject petals;
        public GameObject boulder;
        public GameObject shrine;
        public Rect starter=new(-37,-25,74,90);
        public int starterWidth=75;
        public int starterDepth=91;
        public float[] starterHeights;
        public Vector2[] landmarks={new(0,300),new(380,100),new(-420,520),new(700,-380),new(-900,-600),new(1200,900)};
        public float StarterHeight(float x,float z)
        {
            float u=Mathf.Clamp01((x-starter.xMin)/starter.width)*(starterWidth-1),v=Mathf.Clamp01((z-starter.yMin)/starter.height)*(starterDepth-1);
            int ix=Mathf.Min((int)u,starterWidth-2),iz=Mathf.Min((int)v,starterDepth-2);
            return Mathf.Lerp(Mathf.Lerp(starterHeights[iz*starterWidth+ix],starterHeights[iz*starterWidth+ix+1],u-ix),Mathf.Lerp(starterHeights[(iz+1)*starterWidth+ix],starterHeights[(iz+1)*starterWidth+ix+1],u-ix),v-iz);
        }
        public float Height(float x,float z,int seed)
        {
            float offset=(seed&65535)*.127f;
            float n=Mathf.PerlinNoise(x*.0018f+offset,z*.0018f+offset*.7f);
            float h=(n-.43f)*48+Mathf.PerlinNoise(x*.008f+offset,z*.008f-offset)*9+Mathf.PerlinNoise(x*.027f+offset,z*.027f+offset)*1.5f;
            float road=Mathf.Min(Mathf.Abs(x-9*Mathf.Sin(z*.006f)),Mathf.Abs(z-20-8*Mathf.Sin(x*.007f)));
            float route=(Mathf.PerlinNoise(x*.0008f+offset,z*.0008f+offset)-.45f)*15;
            h=Mathf.Lerp(route,h,Mathf.SmoothStep(0,1,Mathf.Clamp01((road-4)/22)));
            foreach(var landmark in landmarks){float d=Vector2.Distance(new Vector2(x,z),landmark);if(d<24){float plateau=(Mathf.PerlinNoise(landmark.x*.0018f+offset,landmark.y*.0018f+offset*.7f)-.43f)*48+4;h=Mathf.Lerp(plateau,h,Mathf.SmoothStep(0,1,Mathf.Clamp01((d-12)/12)));}}
            float dx=Mathf.Max(starter.xMin-x,0,x-starter.xMax),dz=Mathf.Max(starter.yMin-z,0,z-starter.yMax);float distance=Mathf.Sqrt(dx*dx+dz*dz);
            return Mathf.Lerp(StarterHeight(x,z),h,Mathf.SmoothStep(0,1,Mathf.Clamp01(distance/40)));
        }
        public int Biome(float x,float z,int seed)
        {
            if(new Vector2(x,z-20).magnitude<240)return 0;
            float n=Mathf.PerlinNoise(x*.0015f+(seed&4095),z*.0015f+(seed&8191));
            return n<.4f?1:n>.6f?2:0;
        }
        public Color GroundColor(float x,float z,int seed)
        {
            int biome=Biome(x,z,seed);Color color=biome==0?new Color(.42f,.32f,.34f):biome==1?new Color(.24f,.35f,.29f):new Color(.48f,.48f,.51f);
            float road=Mathf.Min(Mathf.Abs(x-9*Mathf.Sin(z*.006f)),Mathf.Abs(z-20-8*Mathf.Sin(x*.007f)));
            color=Color.Lerp(new Color(.53f,.44f,.35f),color,Mathf.SmoothStep(0,1,Mathf.Clamp01((road-2)/3)));
            color*=.88f+.18f*Mathf.PerlinNoise(x*.065f,z*.065f);
            return color.linear;
        }
    }
}
