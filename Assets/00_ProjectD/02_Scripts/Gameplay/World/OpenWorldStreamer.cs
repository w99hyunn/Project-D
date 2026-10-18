using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DoHwa.World
{
    public sealed class OpenWorldStreamer : MonoBehaviour
    {
        [SerializeField] private WorldRuntime world;
        [SerializeField] private HeroMotor hero;
        [SerializeField] private OpenWorldDefinition definition;
        private readonly Dictionary<Vector2Int,GameObject> chunks=new();
        private readonly Dictionary<Vector2Int,Mesh> meshes=new();
        private readonly Dictionary<Vector2Int,List<Mesh>> detailMeshes=new();
        private readonly HashSet<Vector2Int> wanted=new();
        private readonly List<Vector2Int> pending=new();
        private int activeSeed;
        private float nextRefresh;
        public int LoadedChunks=>chunks.Count;
        public OpenWorldDefinition Definition=>definition;
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
            void Include(Vector3 position){var center=Cell(position);for(int z=-definition.viewRadius;z<=definition.viewRadius;z++)for(int x=-definition.viewRadius;x<=definition.viewRadius;x++){var p=center+new Vector2Int(x,z);if(p.x>=-limit&&p.x<limit&&p.y>=-limit&&p.y<limit)wanted.Add(p);}}
            Include(hero.transform.position);
            if(world.IsAuthority)foreach(var player in world.State.players)if(world.ActivePlayers.Contains(player.id))Include(player.position);
            var remove=new List<Vector2Int>();foreach(var pair in chunks)if(!wanted.Contains(pair.Key))remove.Add(pair.Key);
            foreach(var cell in remove)Unload(cell);
            pending.Clear();foreach(var cell in wanted)if(!chunks.ContainsKey(cell))pending.Add(cell);
            var focus=Cell(hero.transform.position);pending.Sort((a,b)=>(a-focus).sqrMagnitude.CompareTo((b-focus).sqrMagnitude));
        }
        private void LateUpdate()
        {
            Shader.SetGlobalVector("_WorldHero",new Vector4(hero.transform.position.x,hero.transform.position.y+1,hero.transform.position.z,1));
            float edge=definition.worldSize*.5f-3;
            var position=hero.transform.position;
            var bounded=new Vector3(Mathf.Clamp(position.x,-edge,edge),position.y,Mathf.Clamp(position.z,-edge,edge));
            if(position==bounded)return;
            hero.TryGetComponent<CharacterController>(out var body);body.enabled=false;hero.transform.position=bounded;body.enabled=true;
        }
        private void Build(Vector2Int cell)
        {
            var go=new GameObject($"World [{cell.x},{cell.y}]");go.transform.SetParent(transform,false);go.transform.position=new Vector3(cell.x*definition.chunkSize,0,cell.y*definition.chunkSize);
            int n=definition.resolution;float step=definition.chunkSize/(float)n;var vertices=new Vector3[(n+1)*(n+1)];var normals=new Vector3[vertices.Length];var colors=new Color[vertices.Length];var triangles=new List<int>(n*n*6);
            for(int z=0;z<=n;z++)for(int x=0;x<=n;x++){
                int i=z*(n+1)+x;float wx=go.transform.position.x+x*step,wz=go.transform.position.z+z*step;vertices[i]=new Vector3(x*step,definition.Height(wx,wz,activeSeed),z*step);
                normals[i]=new Vector3(definition.Height(wx-.5f,wz,activeSeed)-definition.Height(wx+.5f,wz,activeSeed),1,definition.Height(wx,wz-.5f,activeSeed)-definition.Height(wx,wz+.5f,activeSeed)).normalized;colors[i]=definition.GroundColor(wx,wz,activeSeed);
                if(x==n||z==n)continue;
                // Keep the authored valley surface; generated border cells bridge its irregular edge.
                if(definition.starter.Contains(new Vector2(wx+step*.5f,wz+step*.5f))&&wx>definition.starter.xMin+step&&wx+step<definition.starter.xMax-step&&wz>definition.starter.yMin+step&&wz+step<definition.starter.yMax-step)continue;
                triangles.Add(i);triangles.Add(i+n+1);triangles.Add(i+1);triangles.Add(i+1);triangles.Add(i+n+1);triangles.Add(i+n+2);
            }
            var mesh=new Mesh{name=$"Terrain {cell}"};mesh.vertices=vertices;mesh.normals=normals;mesh.colors=colors;mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=definition.ground;renderer.shadowCastingMode=ShadowCastingMode.Off;go.AddComponent<MeshCollider>().sharedMesh=mesh;
            chunks.Add(cell,go);meshes.Add(cell,mesh);
            var random=new System.Random(unchecked(activeSeed^(cell.x*73856093)^(cell.y*19349663)));
            for(int i=0;i<48;i++){
                float x=go.transform.position.x+(float)random.NextDouble()*definition.chunkSize,z=go.transform.position.z+(float)random.NextDouble()*definition.chunkSize;
                if(definition.starter.Contains(new Vector2(x,z)))continue;
                float road=Mathf.Min(Mathf.Abs(x-9*Mathf.Sin(z*.006f)),Mathf.Abs(z-20-8*Mathf.Sin(x*.007f)));if(road<5)continue;
                bool reserved=false;foreach(var p in definition.landmarks)if(Vector2.Distance(new Vector2(x,z),p)<14){reserved=true;break;}if(reserved)continue;
                int biome=definition.Biome(x,z,activeSeed);bool ore=i%7==0;var prefab=ore?definition.boulder:biome==1?definition.bamboo:biome==2?definition.pine:definition.peachTree;
                var art=Instantiate(prefab,new Vector3(x,definition.Height(x,z,activeSeed),z),Quaternion.Euler(0,random.Next(360),0),go.transform);art.transform.localScale*=.8f+(float)random.NextDouble()*.6f;
                if(i%3==0||ore){var node=new GameObject("Gatherable");node.transform.SetParent(go.transform,false);node.transform.position=art.transform.position;art.transform.SetParent(node.transform,true);var resource=node.AddComponent<WorldResource>();resource.Configure($"chunk:{cell.x}:{cell.y}:{i}",ore?"철광석":"목재",new ItemAmount(ore?5:4,ore?2:3),art);world.RegisterResource(resource);}
            }
            detailMeshes[cell]=new List<Mesh>();
            AddGroundCover(cell,go,random,definition.grass,240,false);
            AddGroundCover(cell,go,random,definition.petals,60,true);
            for(int i=0;i<definition.landmarks.Length;i++){var p=definition.landmarks[i];if(Cell(new Vector3(p.x,0,p.y))!=cell)continue;Instantiate(definition.shrine,new Vector3(p.x,definition.Height(p.x,p.y,activeSeed),p.y),Quaternion.Euler(0,i*90,0),go.transform);}
        }
        private void AddGroundCover(Vector2Int cell,GameObject chunk,System.Random random,GameObject prefab,int count,bool onlyPeach)
        {
            prefab.TryGetComponent<MeshFilter>(out var filter);prefab.TryGetComponent<MeshRenderer>(out var renderer);
            var instances=new List<CombineInstance>();
            for(int i=0;i<count;i++)
            {
                float x=chunk.transform.position.x+(float)random.NextDouble()*definition.chunkSize,z=chunk.transform.position.z+(float)random.NextDouble()*definition.chunkSize;
                if(definition.starter.Contains(new Vector2(x,z))||(onlyPeach&&definition.Biome(x,z,activeSeed)!=0))continue;
                float road=Mathf.Min(Mathf.Abs(x-9*Mathf.Sin(z*.006f)),Mathf.Abs(z-20-8*Mathf.Sin(x*.007f)));
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
        private void OnDestroy(){world.Changed-=StateChanged;foreach(var mesh in meshes.Values)Destroy(mesh);foreach(var list in detailMeshes.Values)foreach(var mesh in list)Destroy(mesh);}
    }
}

