using UnityEngine;

namespace DoHwa.World
{
    [CreateAssetMenu(menuName = "DoHwa/Scenic Valley")]
    public sealed class ScenicValley : ScriptableObject
    {
        public Vector3[] river;
        public float bankWidth = 48;
        public float riverHalfWidth = 5;
        public Vector4[] landings = System.Array.Empty<Vector4>();
        [Tooltip("Authored path points: X, Z, elevation, half width.")]
        public Vector4[] restAreaPath = System.Array.Empty<Vector4>();
        [Tooltip("Clearing surface: X, Z, radius, edge feather.")]
        public Vector4 restAreaClearing;

        public float RestPathWeight(float x, float z, out float elevation)
        {
            float weight = 0;
            elevation = 0;
            var position = new Vector2(x, z);
            for (int i = 0; i + 1 < restAreaPath.Length; i++)
            {
                var a = restAreaPath[i];
                var b = restAreaPath[i + 1];
                var start = new Vector2(a.x, a.y);
                var segment = new Vector2(b.x - a.x, b.y - a.y);
                if (segment.sqrMagnitude < .001f) continue;
                float t = Mathf.Clamp01(Vector2.Dot(position - start, segment) / segment.sqrMagnitude);
                float distance = Vector2.Distance(position, start + segment * t);
                float width = Mathf.Lerp(a.w, b.w, t);
                float influence = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(width, width + 3, distance));
                if (influence <= weight) continue;
                weight = influence;
                elevation = Mathf.Lerp(a.z, b.z, t);
            }
            return weight;
        }

        public float RestAreaSurface(float x, float z)
        {
            float path = RestPathWeight(x, z, out _);
            if (restAreaClearing.z <= 0) return path;
            float distance = Vector2.Distance(new Vector2(x, z), new Vector2(restAreaClearing.x, restAreaClearing.y));
            float clearing = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(restAreaClearing.z, restAreaClearing.z + restAreaClearing.w, distance));
            return Mathf.Max(path, clearing);
        }

        public Vector3 Center(float z)
        {
            for (int i = 0; i < river.Length - 1; i++)
            {
                if (z > river[i + 1].z) continue;
                var a = river[Mathf.Max(0, i - 1)];
                var b = river[i]; var c = river[i + 1];
                var d = river[Mathf.Min(river.Length - 1, i + 2)];
                float t = Mathf.Clamp01((z - b.z) / (c.z - b.z));
                float x = .5f * ((2*b.x) + (-a.x+c.x)*t + (2*a.x-5*b.x+4*c.x-d.x)*t*t + (-a.x+3*b.x-3*c.x+d.x)*t*t*t);
                return new Vector3(x, Mathf.Lerp(b.y,c.y,Mathf.SmoothStep(0,1,t)), z);
            }
            return river[^1];
        }
        public float Width(float z) => Mathf.Lerp(1.625f, riverHalfWidth + 2 * Mathf.Sin(z*.013f), Mathf.SmoothStep(0,1,(z-river[0].z)/45));
        public float Weight(float x,float z)
        {
            if(z<river[0].z || z>river[^1].z)return 0;
            return 1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(bankWidth-12,bankWidth,Mathf.Abs(x-Center(z).x)));
        }
        public float Shape(float x,float z,float original)
        {
            float w=Weight(x,z);if(w<=0)return original;
            var c=Center(z);float distance=Mathf.Abs(x-c.x), width=Width(z);
            float shore=Mathf.SmoothStep(0,1,Mathf.InverseLerp(width*.65f,width+5,distance));
            float height=c.y-1.6f+shore*3.2f;
            float ridgeCenter=38+9*Mathf.Sin(z*.017f);
            float ridge=Mathf.Exp(-Mathf.Pow((distance-ridgeCenter)/17,2));
            height+=ridge*(7+9*Mathf.PerlinNoise(z*.012f,x*.014f));
            // A walkable towpath winds with the left bank rather than a straight road grid.
            float path=Mathf.Abs(x-(c.x-width-9));
            height=Mathf.Lerp(c.y+1.65f,height,Mathf.SmoothStep(0,1,Mathf.InverseLerp(2.5f,6,path)));
            height = Mathf.Lerp(original,height,w);
            foreach(var landing in landings)
            {
                float landingDistance=Vector2.Distance(new Vector2(x,z),new Vector2(landing.x,landing.y));
                float blend=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(landing.w,landing.w+4,landingDistance));
                height=Mathf.Lerp(height,landing.z,blend);
            }
            float pathWeight = RestPathWeight(x, z, out float pathElevation);
            return Mathf.Lerp(height, pathElevation, pathWeight);
        }
    }
}

