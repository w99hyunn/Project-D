using UnityEngine;

namespace DoHwa.World
{
    [CreateAssetMenu(menuName="DoHwa/Open World Definition")]
    public sealed class OpenWorldDefinition : ScriptableObject
    {
        public int worldSize=16384;
        public int chunkSize=64;
        public int resolution=32;
        public int viewRadius=2;
        public Material ground;
        public Material water;
        public GameObject peachTree,bamboo,pine,grass,petals,boulder,shrine;
        public WorldRegion[] regions;
        public Vector2Int[] routes;
        public Rect starter=new(-37,-25,74,90);
        public int starterWidth=75,starterDepth=91;
        public float[] starterHeights;
        public Vector2[] landmarks;
        public WorldPlace[] places=System.Array.Empty<WorldPlace>();
        public ScenicValley scenicValley;
        public bool retainStarterTerrain = true;
        [Min(1)] public float transitionWidth=220;

        public float RegionWeight(float x,float z,int index)
        {
            var point=new Vector2(x,z);
            float nearest=float.MaxValue;
            foreach(var region in regions)nearest=Mathf.Min(nearest,Vector2.Distance(point,region.center));
            float sum=0,selected=0;
            for(int i=0;i<regions.Length;i++){
                float weight=Mathf.Exp(-(Vector2.Distance(point,regions[i].center)-nearest)/transitionWidth);
                sum+=weight;if(i==index)selected=weight;
            }
            return selected/sum;
        }

        public bool IsAuthoredPlace(float x,float z,float margin=0)
        {
            var point=new Vector2(x,z);
            foreach(var place in places)
                if((point-place.center).sqrMagnitude<(place.radius+margin)*(place.radius+margin))return true;
            return false;
        }

        public float StarterHeight(float x,float z)
        {
            float u=Mathf.Clamp01((x-starter.xMin)/starter.width)*(starterWidth-1),v=Mathf.Clamp01((z-starter.yMin)/starter.height)*(starterDepth-1);
            int ix=Mathf.Min((int)u,starterWidth-2),iz=Mathf.Min((int)v,starterDepth-2);
            return Mathf.Lerp(Mathf.Lerp(starterHeights[iz*starterWidth+ix],starterHeights[iz*starterWidth+ix+1],u-ix),Mathf.Lerp(starterHeights[(iz+1)*starterWidth+ix],starterHeights[(iz+1)*starterWidth+ix+1],u-ix),v-iz);
        }
        private void RegionBlend(float x,float z,int seed,out int first,out int second,out float blend)
        {
            float offset=seed&4095;
            var p=new Vector2(x+(Mathf.PerlinNoise(x*.002f+offset,z*.002f)-.5f)*100,z+(Mathf.PerlinNoise(z*.002f+offset,x*.002f)-.5f)*100);
            first=0;second=0;float a=float.MaxValue,b=float.MaxValue;
            for(int i=0;i<regions.Length;i++){
                float d=Vector2.Distance(p,regions[i].center);
                if(d<a){b=a;second=first;a=d;first=i;}else if(d<b){b=d;second=i;}
            }
            blend=.5f*(1-Mathf.SmoothStep(0,1,Mathf.Clamp01((b-a)/220)));
        }
        public int Biome(float x,float z,int seed){RegionBlend(x,z,seed,out int a,out _,out _);return a;}
        public int VegetationBiome(float x,float z,int seed)
        {
            RegionBlend(x,z,seed,out int a,out int b,out float blend);
            uint hash=unchecked((uint)(Mathf.FloorToInt(x*13)*73856093^Mathf.FloorToInt(z*13)*19349663^seed));
            hash^=hash>>16;hash*=0x7feb352d;hash^=hash>>15;
            return (hash&65535)/65535f<blend?b:a;
        }
        private void TerrainBlend(float x,float z,int seed,out float elevation,out float relief,out Color color)
        {
            RegionBlend(x,z,seed,out int nearest,out _,out _);
            var p=new Vector2(x,z);float minimum=Vector2.Distance(p,regions[nearest].center);
            float total=0;elevation=0;relief=0;color=Color.clear;
            foreach(var region in regions){
                float weight=Mathf.Exp(-(Vector2.Distance(p,region.center)-minimum)/transitionWidth);
                total+=weight;elevation+=region.elevation*weight;relief+=region.relief*weight;color+=region.ground*weight;
            }
            elevation/=total;relief/=total;color/=total;
        }
        public float RoadDistance(float x,float z,out float elevation)
        {
            var p=new Vector2(x,z);float best=float.MaxValue;elevation=0;
            foreach(var route in routes){
                var a=regions[route.x];var b=regions[route.y];var start=a.center+a.routeOffset;var end=b.center+b.routeOffset;var axis=end-start;
                float t=Mathf.Clamp01(Vector2.Dot(p-start,axis)/axis.sqrMagnitude);
                var q=start+axis*t;
                // A slow lateral bend keeps long roads from reading as straight grid axes.
                var normal=new Vector2(-axis.y,axis.x).normalized;
                q+=normal*(Mathf.Sin(t*Mathf.PI*2)*22);
                float d=Vector2.Distance(p,q);
                if(d<best){best=d;elevation=Mathf.Lerp(a.elevation,b.elevation,t);}
            }
            foreach(var region in regions){
                if(region.routeOffset.sqrMagnitude<1)continue;
                var start=region.center+region.routeOffset;var axis=-region.routeOffset;
                float t=Mathf.Clamp01(Vector2.Dot(p-start,axis)/axis.sqrMagnitude);
                float d=Vector2.Distance(p,start+axis*t);
                if(d<best){best=d;elevation=region.elevation;}
            }
            foreach(var place in places){
                var axis=place.center-place.roadEntry;
                if(axis.sqrMagnitude<1)continue;
                float t=Mathf.Clamp01(Vector2.Dot(p-place.roadEntry,axis)/axis.sqrMagnitude);
                float d=Vector2.Distance(p,place.roadEntry+axis*t);
                if(d<best){best=d;elevation=Mathf.Lerp(place.entryElevation,place.elevation,t);}
            }
            return best;
        }
        public float Height(float x,float z,int seed)
        {
            TerrainBlend(x,z,seed,out float baseHeight,out float relief,out _);
            float offset=(seed&65535)*.127f;
            
            float h=baseHeight;
            h+=(Mathf.PerlinNoise(x*.0018f+offset,z*.0018f+offset*.7f)-.5f)*relief;
            h+=(Mathf.PerlinNoise(x*.009f+offset,z*.009f-offset)-.5f)*relief*.18f;
            float road=RoadDistance(x,z,out float routeHeight);
            h=Mathf.Lerp(routeHeight,h,Mathf.SmoothStep(0,1,Mathf.Clamp01((road-5)/35)));
            foreach(var p in landmarks){float d=Vector2.Distance(new Vector2(x,z),p);if(d<24){int r=Biome(p.x,p.y,seed);h=Mathf.Lerp(regions[r].elevation,h,Mathf.SmoothStep(0,1,Mathf.Clamp01((d-12)/12)));}}
            foreach(var region in regions){
                if(region.lakeRadius<=0)continue;
                float distance=Vector2.Distance(new Vector2(x,z),region.center+new Vector2(160,80));
                float shore=Mathf.SmoothStep(0,1,Mathf.Clamp01((distance-region.lakeRadius*.75f)/(region.lakeRadius*.25f)));
                float basin=region.elevation-.65f+shore*1.1f;
                h=Mathf.Lerp(basin,h,Mathf.SmoothStep(0,1,Mathf.Clamp01((distance-region.lakeRadius)/24)));
            }
            foreach(var place in places){
                float distance=Vector2.Distance(new Vector2(x,z),place.center);
                if(distance<place.radius+18)
                    h=Mathf.Lerp(place.elevation,h,Mathf.SmoothStep(0,1,Mathf.Clamp01((distance-place.radius)/18)));
            }

            if(scenicValley)h=scenicValley.Shape(x,z,h);
            if(retainStarterTerrain){
                float dx=Mathf.Max(starter.xMin-x,0,x-starter.xMax),dz=Mathf.Max(starter.yMin-z,0,z-starter.yMax);
                return Mathf.Lerp(StarterHeight(x,z),h,Mathf.SmoothStep(0,1,Mathf.Clamp01(Mathf.Sqrt(dx*dx+dz*dz)/65)));
            }
            return h;
        }
        public Color GroundColor(float x,float z,int seed)
        {
            TerrainBlend(x,z,seed,out _,out _,out var color);
            float road=RoadDistance(x,z,out _);
            color=Color.Lerp(color*.75f+new Color(.12f,.10f,.07f,0),color,Mathf.SmoothStep(0,1,Mathf.Clamp01((road-3)/4)));
            color*=.94f+.08f*Mathf.PerlinNoise(x*.065f,z*.065f);
            if(scenicValley && scenicValley.Weight(x,z)>0){
                var c=scenicValley.Center(z);float d=Mathf.Abs(x-c.x);
                float path=Mathf.Abs(x-(c.x-scenicValley.Width(z)-9));
                var bank=Color.Lerp(new Color(.39f,.36f,.31f),color,Mathf.SmoothStep(0,1,Mathf.InverseLerp(scenicValley.Width(z),18,d)));
                bank=Color.Lerp(new Color(.46f,.38f,.32f),bank,Mathf.SmoothStep(0,1,Mathf.InverseLerp(2,4,path)));
                color=Color.Lerp(color,bank,scenicValley.Weight(x,z));
            }
            return color.linear;
        }
    }
}





