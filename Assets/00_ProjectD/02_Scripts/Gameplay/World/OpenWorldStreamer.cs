using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;

namespace DoHwa.World
{
    public sealed class OpenWorldStreamer : MonoBehaviour
    {
        private WorldRuntime world;
        private HeroMotor hero;
        [SerializeField] private OpenWorldDefinition definition;
        [SerializeField] private bool landmarksAuthoredInScene;
        [SerializeField] private int regionIndex;
        private readonly Dictionary<Vector2Int,GameObject> chunks=new();
        private readonly Dictionary<Vector2Int,Mesh> meshes=new();
        private readonly Dictionary<Vector2Int,List<Mesh>> detailMeshes=new();
        private readonly HashSet<Vector2Int> wanted=new();
        private readonly List<Vector2Int> pending=new();
        private int activeSeed;
        private float nextRefresh;
        private CancellationTokenRegistration heroLifetime;
        private CancellationTokenRegistration worldLifetime;
        private bool bound;
#if UNITY_EDITOR
        private bool editorPreview;
        public void PrepareEditorPreview(OpenWorldDefinition source,int seed,int region)
        {
            editorPreview=true;definition=source;activeSeed=seed;regionIndex=region;landmarksAuthoredInScene=true;
        }
        public void BuildEditorPreview(Vector2Int cell)
        {
            if(!Application.isPlaying && editorPreview && !chunks.ContainsKey(cell))Build(cell);
        }
        public void ReleaseEditorPreview()
        {
            foreach(var mesh in meshes.Values)DestroyImmediate(mesh);
            foreach(var list in detailMeshes.Values)foreach(var mesh in list)DestroyImmediate(mesh);
            meshes.Clear();detailMeshes.Clear();chunks.Clear();
        }
#endif
        public int LoadedChunks=>chunks.Count;
        public OpenWorldDefinition Definition=>definition;
        private void Awake()
        {
            world = WorldRuntime.Instance;
            // An additive map may finish activating after its gameplay scene has closed.
            // Never request a destruction token from an already destroyed Unity object.
            if (!world || !world.Hero) { enabled=false; return; }
            hero = world.Hero;
            bound=true;
            heroLifetime=hero.destroyCancellationToken.Register(StopStreaming);
            worldLifetime=world.destroyCancellationToken.Register(StopStreaming);
        }
        private void StopStreaming()
        {
            enabled=false;
            pending.Clear();
            if(bound)world.Changed-=StateChanged;
        }
        private void Start()
        {
            activeSeed=world.State.seed;world.Changed+=StateChanged;
            var cell=Cell(world.LocalPlayer.position);
            for(int z=-1;z<=1;z++)for(int x=-1;x<=1;x++)Build(cell+new Vector2Int(x,z));
        }
        private Vector2Int Cell(Vector3 p)=>new(Mathf.FloorToInt(p.x/definition.chunkSize),Mathf.FloorToInt(p.z/definition.chunkSize));
        private void StateChanged(){if(activeSeed==world.State.seed)return;Clear();activeSeed=world.State.seed;nextRefresh=0;}
        private void Update()
        {
            if(Time.unscaledTime>=nextRefresh){nextRefresh=Time.unscaledTime+.3f;RefreshWanted();}
            int budget=1;while(budget-->0&&pending.Count>0){var cell=pending[0];pending.RemoveAt(0);if(wanted.Contains(cell)&&!chunks.ContainsKey(cell))Build(cell);}
        }
        private void RefreshWanted()
        {
            wanted.Clear();int limit=definition.worldSize/definition.chunkSize/2;
            void Include(Vector3 position){var center=Cell(position);for(int z=-definition.viewRadius;z<=definition.viewRadius;z++)for(int x=-definition.viewRadius;x<=definition.viewRadius;x++){var p=center+new Vector2Int(x,z);if(p.x>=-limit&&p.x<limit&&p.y>=-limit&&p.y<limit && definition.Biome((p.x+.5f)*definition.chunkSize,(p.y+.5f)*definition.chunkSize,activeSeed)==regionIndex)wanted.Add(p);}}
            if (world.LocalPlayer.mapScene != "Moonshadow") Include(hero.transform.position);
            if(world.IsAuthority)foreach(var player in world.State.players)if(world.ActivePlayers.Contains(player.id) && player.mapScene != "Moonshadow")Include(player.position);
            var remove=new List<Vector2Int>();foreach(var pair in chunks)if(!wanted.Contains(pair.Key))remove.Add(pair.Key);
            foreach(var cell in remove)Unload(cell);
            pending.Clear();foreach(var cell in wanted)if(!chunks.ContainsKey(cell))pending.Add(cell);
            var focus=Cell(hero.transform.position);pending.Sort((a,b)=>(a-focus).sqrMagnitude.CompareTo((b-focus).sqrMagnitude));
        }
        private void LateUpdate()
        {
            Shader.SetGlobalVector("_WorldHero",new Vector4(hero.transform.position.x,hero.transform.position.y+1,hero.transform.position.z,1));
            if(world.LocalPlayer.mapScene != gameObject.scene.name)return;
            float edge=definition.worldSize*.5f-3;
            var position=hero.transform.position;
            var bounded=new Vector3(Mathf.Clamp(position.x,-edge,edge),position.y,Mathf.Clamp(position.z,-edge,edge));
            if(position==bounded)return;
            hero.TryGetComponent<CharacterController>(out var body);body.enabled=false;hero.transform.position=bounded;body.enabled=true;
        }
        private void Build(Vector2Int cell)
        {
            if(definition.Biome((cell.x+.5f)*definition.chunkSize,(cell.y+.5f)*definition.chunkSize,activeSeed)!=regionIndex)return;
            var go=new GameObject($"World [{cell.x},{cell.y}]");go.transform.SetParent(transform,false);go.transform.position=new Vector3(cell.x*definition.chunkSize,0,cell.y*definition.chunkSize);
            int n=definition.resolution;float step=definition.chunkSize/(float)n;var vertices=new Vector3[(n+1)*(n+1)];var normals=new Vector3[vertices.Length];var colors=new Color[vertices.Length];var triangles=new List<int>(n*n*6);
            for(int z=0;z<=n;z++)for(int x=0;x<=n;x++){
                int i=z*(n+1)+x;float wx=go.transform.position.x+x*step,wz=go.transform.position.z+z*step;vertices[i]=new Vector3(x*step,definition.Height(wx,wz,activeSeed),z*step);
                colors[i]=definition.GroundColor(wx,wz,activeSeed);
                if(x==n||z==n)continue;
                if(definition.retainStarterTerrain && definition.starter.Contains(new Vector2(wx+step*.5f,wz+step*.5f))&&wx>definition.starter.xMin+step&&wx+step<definition.starter.xMax-step&&wz>definition.starter.yMin+step&&wz+step<definition.starter.yMax-step)continue;
                triangles.Add(i);triangles.Add(i+n+1);triangles.Add(i+1);triangles.Add(i+1);triangles.Add(i+n+1);triangles.Add(i+n+2);
            }
            for(int z=0;z<=n;z++)for(int x=0;x<=n;x++){
                int i=z*(n+1)+x;float wx=go.transform.position.x+x*step,wz=go.transform.position.z+z*step;
                float left=x>0?vertices[i-1].y:definition.Height(wx-step,wz,activeSeed);
                float right=x<n?vertices[i+1].y:definition.Height(wx+step,wz,activeSeed);
                float down=z>0?vertices[i-n-1].y:definition.Height(wx,wz-step,activeSeed);
                float up=z<n?vertices[i+n+1].y:definition.Height(wx,wz+step,activeSeed);
                normals[i]=new Vector3(left-right,2*step,down-up).normalized;
            }
            var mesh=new Mesh{name=$"Terrain {cell}"};mesh.vertices=vertices;mesh.normals=normals;mesh.colors=colors;mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=definition.ground;renderer.shadowCastingMode=ShadowCastingMode.Off;go.AddComponent<MeshCollider>().sharedMesh=mesh;
            chunks.Add(cell,go);meshes.Add(cell,mesh);
            var random=new System.Random(unchecked(activeSeed^(cell.x*73856093)^(cell.y*19349663)));
            for(int i=0;i<48;i++){
                float x=go.transform.position.x+(float)random.NextDouble()*definition.chunkSize,z=go.transform.position.z+(float)random.NextDouble()*definition.chunkSize;
                if((definition.retainStarterTerrain && definition.starter.Contains(new Vector2(x,z)))||definition.IsAuthoredPlace(x,z,3))continue;
                if(definition.scenicValley && definition.scenicValley.Weight(x,z)>.05f)continue;
                float road=definition.RoadDistance(x,z,out _);if(road<5)continue;
                bool reserved=false;foreach(var p in definition.landmarks)if(Vector2.Distance(new Vector2(x,z),p)<14){reserved=true;break;}if(reserved)continue;
                int biome=definition.VegetationBiome(x,z,activeSeed);var region=definition.regions[biome];if(i>=region.vegetationCount)continue;bool ore=i%7==0;var prefab=ore?definition.boulder:region.vegetation;
                var art=Instantiate(prefab,new Vector3(x,definition.Height(x,z,activeSeed),z),Quaternion.Euler(0,random.Next(360),0),go.transform);art.transform.localScale*=.8f+(float)random.NextDouble()*.6f;
#if UNITY_EDITOR
                if(editorPreview)continue;
#endif
                if((region.timber&&i%3==0)||ore){var node=new GameObject("Gatherable");node.transform.SetParent(go.transform,false);node.transform.position=art.transform.position;art.transform.SetParent(node.transform,true);var resource=node.AddComponent<WorldResource>();resource.Configure($"chunk:{cell.x}:{cell.y}:{i}",ore?"철광석":"목재",new ItemAmount(ore?5:4,ore?2:3),art);world.RegisterResource(resource);}
            }
            detailMeshes[cell]=new List<Mesh>();
            AddWater(cell,go);
            AddRoadsideProps(go,random);
            for(int i=0;i<3;i++){
                float x=go.transform.position.x+(float)random.NextDouble()*definition.chunkSize,z=go.transform.position.z+(float)random.NextDouble()*definition.chunkSize;
                if((definition.retainStarterTerrain && definition.starter.Contains(new Vector2(x,z)))||definition.IsAuthoredPlace(x,z,8)||definition.RoadDistance(x,z,out _)<18)continue;
                if(definition.scenicValley && definition.scenicValley.Weight(x,z)>.05f)continue;
                var region=definition.regions[definition.VegetationBiome(x,z,activeSeed)];
                if(region.scenery){var art=Instantiate(region.scenery,new Vector3(x,definition.Height(x,z,activeSeed),z),Quaternion.Euler(0,random.Next(360),0),go.transform);art.transform.localScale*=.7f+(float)random.NextDouble()*.6f;}
            }

            AddGroundCover(cell,go,random,definition.grass,240,false);
            AddGroundCover(cell,go,random,definition.petals,60,true);
            if(!landmarksAuthoredInScene)for(int i=0;i<definition.landmarks.Length;i++){var p=definition.landmarks[i];if(Cell(new Vector3(p.x,0,p.y))!=cell)continue;var region=definition.regions[definition.Biome(p.x,p.y,activeSeed)];Instantiate(region.landmark?region.landmark:definition.shrine,new Vector3(p.x,definition.Height(p.x,p.y,activeSeed),p.y),Quaternion.identity,go.transform);}
#if UNITY_EDITOR
            if(editorPreview)return;
#endif
            foreach(var draw in go.GetComponentsInChildren<Renderer>())draw.forceRenderingOff=world.LocalPlayer.mapScene=="Moonshadow";
        }
        private void AddRoadsideProps(GameObject chunk,System.Random random)
        {
            for(int i=0;i<10;i++){
                float x=chunk.transform.position.x+(float)random.NextDouble()*definition.chunkSize,z=chunk.transform.position.z+(float)random.NextDouble()*definition.chunkSize;
                if((definition.retainStarterTerrain && definition.starter.Contains(new Vector2(x,z)))||definition.IsAuthoredPlace(x,z,3))continue;
                if(definition.scenicValley && definition.scenicValley.Weight(x,z)>.05f)continue;
                float distance=definition.RoadDistance(x,z,out _);if(distance<9||distance>14)continue;
                var region=definition.regions[definition.Biome(x,z,activeSeed)];if(region.roadsideProps.Length==0)continue;
                float h=definition.Height(x,z,activeSeed);
                if(Mathf.Abs(definition.Height(x+1,z,activeSeed)-h)>.2f||Mathf.Abs(definition.Height(x,z+1,activeSeed)-h)>.2f)continue;
                Instantiate(region.roadsideProps[random.Next(region.roadsideProps.Length)],new Vector3(x,h,z),Quaternion.Euler(0,random.Next(4)*90,0),chunk.transform);
            }
        }
        private void AddWater(Vector2Int cell,GameObject chunk)
        {
            foreach(var region in definition.regions){
                if(region.lakeRadius<=0)continue;
                var center=region.center+new Vector2(160,80);
                if(Cell(new Vector3(center.x,0,center.y))!=cell)continue;
                if(!landmarksAuthoredInScene&&region.pier)Instantiate(region.pier,new Vector3(center.x,region.elevation+.15f,center.y-region.lakeRadius*.91f),Quaternion.Euler(0,180,0),chunk.transform);
                var vertices=new Vector3[65];var triangles=new int[63*3];
                vertices[0]=Vector3.zero;
                for(int i=0;i<64;i++){float angle=i/63f*Mathf.PI*2;vertices[i+1]=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*region.lakeRadius*.92f;if(i<63){triangles[i*3]=0;triangles[i*3+1]=i+2;triangles[i*3+2]=i+1;}}
                var mesh=new Mesh{name="Reed lake"};mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();detailMeshes[cell].Add(mesh);
                var lake=new GameObject("옥빛 여울",typeof(MeshFilter),typeof(MeshRenderer));lake.transform.SetParent(chunk.transform,false);lake.transform.position=new Vector3(center.x,region.elevation-.08f,center.y);
                lake.TryGetComponent<MeshFilter>(out var filter);filter.sharedMesh=mesh;lake.TryGetComponent<MeshRenderer>(out var renderer);renderer.sharedMaterial=definition.water;renderer.shadowCastingMode=ShadowCastingMode.Off;
            }
        }
        private void AddGroundCover(Vector2Int cell,GameObject chunk,System.Random random,GameObject prefab,int count,bool onlyPeach)
        {
            prefab.TryGetComponent<MeshFilter>(out var filter);prefab.TryGetComponent<MeshRenderer>(out var renderer);
            var instances=new List<CombineInstance>();
            for(int i=0;i<count;i++)
            {
                float x=chunk.transform.position.x+(float)random.NextDouble()*definition.chunkSize,z=chunk.transform.position.z+(float)random.NextDouble()*definition.chunkSize;
                var region=definition.regions[definition.VegetationBiome(x,z,activeSeed)];
                if(!region.groundCover||(definition.retainStarterTerrain && definition.starter.Contains(new Vector2(x,z)))||definition.IsAuthoredPlace(x,z)||(onlyPeach&&!region.blossoms))continue;
                if(definition.scenicValley && definition.scenicValley.Weight(x,z)>.05f)continue;
                float road=definition.RoadDistance(x,z,out _);
                if(!onlyPeach&&road<3)continue;
                float h=definition.Height(x,z,activeSeed);
                var position=new Vector3(x-chunk.transform.position.x,h+.025f,z-chunk.transform.position.z);
                var scale=prefab.transform.localScale*(.7f+(float)random.NextDouble()*1.2f);
                instances.Add(new CombineInstance{mesh=filter.sharedMesh,transform=Matrix4x4.TRS(position,Quaternion.Euler(0,random.Next(360),0),scale)});
            }
            if(instances.Count==0)return;
            var mesh=new Mesh{name="Ground cover",indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(instances.ToArray());
            var detail=new GameObject(onlyPeach?"Fallen blossoms":"Forest understory",typeof(MeshFilter),typeof(MeshRenderer));detail.transform.SetParent(chunk.transform,false);
            detail.TryGetComponent<MeshFilter>(out var target);target.sharedMesh=mesh;detail.TryGetComponent<MeshRenderer>(out var draw);draw.sharedMaterial=renderer.sharedMaterial;draw.shadowCastingMode=ShadowCastingMode.Off;
            detailMeshes[cell].Add(mesh);
        }
        private void Unload(Vector2Int cell)
        {
            foreach(var node in chunks[cell].GetComponentsInChildren<WorldResource>(true))world.UnregisterResource(node);
            chunks[cell].SetActive(false);Destroy(chunks[cell]);Destroy(meshes[cell]);foreach(var mesh in detailMeshes[cell])Destroy(mesh);detailMeshes.Remove(cell);chunks.Remove(cell);meshes.Remove(cell);
        }
        private void Clear(){foreach(var cell in new List<Vector2Int>(chunks.Keys))Unload(cell);pending.Clear();}
        private void OnDestroy(){heroLifetime.Dispose();worldLifetime.Dispose();if(bound)world.Changed-=StateChanged;foreach(var mesh in meshes.Values)Destroy(mesh);foreach(var list in detailMeshes.Values)foreach(var mesh in list)Destroy(mesh);}
    }
}











