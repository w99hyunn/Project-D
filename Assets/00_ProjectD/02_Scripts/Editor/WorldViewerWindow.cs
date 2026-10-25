using System;
using System.Collections.Generic;
using System.Linq;
using DoHwa.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace OpenFrame.Editor
{
    public sealed class WorldViewerWindow : EditorWindow
    {
        [SerializeField] private int seed=1729;
        [SerializeField] private int region;
        [SerializeField] private int radius=3;
        private OpenWorldDefinition definition;
        private Scene previewScene;
        private readonly List<Scene> openedScenes=new();
        private readonly List<Mesh> overviewMeshes=new();
        private readonly List<OpenWorldStreamer> generators=new();
        private readonly Queue<Vector2Int> pending=new();
        private readonly Dictionary<SceneView,(bool fog,bool effects)> viewStates=new();
        private GameObject overviewRoot,detailRoot;
        private bool overviewMode,paused;
        private int total,completed;
        private Label status;
        private ProgressBar progress;
        private VisualElement controls;

        [MenuItem("OPENFRAME/월드/월드 보기")]
        public static void Open()=>GetWindow<WorldViewerWindow>("월드 보기");

        private void OnEnable()
        {
            definition=AssetDatabase.LoadAssetAtPath<OpenWorldDefinition>("Assets/Resources/World/OpenWorld.asset");
            EditorApplication.update+=Tick;
            EditorApplication.playModeStateChanged+=PlayChanged;
            AssemblyReloadEvents.beforeAssemblyReload+=Clear;
            EditorSceneManager.sceneSaving+=BeforeSave;
        }
        private void OnDisable()
        {
            Clear();EditorApplication.update-=Tick;
            EditorApplication.playModeStateChanged-=PlayChanged;
            AssemblyReloadEvents.beforeAssemblyReload-=Clear;
            EditorSceneManager.sceneSaving-=BeforeSave;
        }
        private void PlayChanged(PlayModeStateChange state)
        {
            if(state==PlayModeStateChange.ExitingEditMode)Clear();
            controls?.SetEnabled(!EditorApplication.isPlayingOrWillChangePlaymode);
        }
        private void BeforeSave(Scene scene,string path)
        {
            if(previewScene.IsValid() && scene==previewScene)Clear();
        }
        public void CreateGUI()
        {
            rootVisualElement.Clear();
            var scroll=new ScrollView();rootVisualElement.Add(scroll);
            scroll.style.paddingLeft=12;scroll.style.paddingRight=12;scroll.style.paddingTop=12;
            scroll.Add(new Label("OPENFRAME / 월드 보기"){style={fontSize=18,unityFontStyleAndWeight=FontStyle.Bold,marginBottom=12}});
            scroll.Add(new HelpBox("전체 보기: 연결된 지형과 맵 씬의 배치 오브젝트\n상세 보기: 선택 위치 주변의 실제 청크·나무·바위·수풀\n미리보기는 저장되지 않으며 창을 닫거나 플레이하면 제거됩니다.",HelpBoxMessageType.Info));
            controls=new VisualElement();scroll.Add(controls);
            if(!definition){controls.Add(new HelpBox("OpenWorld.asset을 찾을 수 없습니다.",HelpBoxMessageType.Error));return;}
            var seedField=new IntegerField("월드 시드"){value=seed};seedField.RegisterValueChangedCallback(e=>seed=e.newValue);controls.Add(seedField);
            var regions=new PopupField<string>("지역",definition.regions.Select(r=>r.title).ToList(),Mathf.Clamp(region,0,definition.regions.Length-1));
            regions.RegisterValueChangedCallback(_=>region=regions.index);controls.Add(regions);
            var size=new SliderInt("상세 반경 (청크)",1,6){value=radius,showInputField=true};size.RegisterValueChangedCallback(e=>radius=e.newValue);controls.Add(size);
            AddButton("전체 월드 불러오기",ShowOverview);
            AddButton("선택 지역 상세 불러오기",ShowRegion);
            AddButton("Scene 뷰 중심 상세 불러오기",ShowViewCenter);
            AddButton("선택 지역으로 시점 이동",()=>FrameRegion());
            AddButton("전체 월드로 시점 이동",()=>FrameWorld());
            AddButton("생성 일시정지 / 계속",()=>{paused=!paused;UpdateStatus();});
            AddButton("미리보기 모두 제거",Clear);
            progress=new ProgressBar{title="대기",lowValue=0,highValue=1};scroll.Add(progress);
            status=new Label("미리보기 없음"){style={whiteSpace=WhiteSpace.Normal,marginTop=8}};scroll.Add(status);
            controls.SetEnabled(!EditorApplication.isPlayingOrWillChangePlaymode);
        }
        private void AddButton(string title,Action action)=>controls.Add(new Button(action){text=title,style={height=28,marginTop=4}});

        private void Prepare()
        {
            if(previewScene.IsValid())return;
            var active=SceneManager.GetActiveScene();
            foreach(var r in definition.regions)
            {
                var scene=SceneManager.GetSceneByName(r.sceneName);
                if(scene.IsValid() && scene.isLoaded)continue;
                string path=AssetDatabase.FindAssets(r.sceneName+" t:Scene").Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault(p=>System.IO.Path.GetFileNameWithoutExtension(p)==r.sceneName);
                if(path==null)throw new InvalidOperationException("맵 씬 없음: "+r.sceneName);
                openedScenes.Add(EditorSceneManager.OpenScene(path,OpenSceneMode.Additive));
            }
            previewScene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            previewScene.name="OPENFRAME — 임시 월드 보기";
            SceneManager.SetActiveScene(active);
            SceneVisibilityManager.instance.DisablePicking(previewScene);
            foreach(SceneView view in SceneView.sceneViews)
            {
                viewStates[view]=(view.sceneViewState.showFog,view.sceneViewState.showImageEffects);
                view.sceneViewState.showFog=false;view.sceneViewState.showImageEffects=false;
            }
        }
        private GameObject Root(string name)
        {
            var go=new GameObject(name){hideFlags=HideFlags.DontSave};SceneManager.MoveGameObjectToScene(go,previewScene);return go;
        }
        public void ShowOverview()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            Prepare();ClearDetails();pending.Clear();paused=false;overviewMode=true;
            if(overviewRoot)DestroyImmediate(overviewRoot);
            foreach(var mesh in overviewMeshes)DestroyImmediate(mesh);overviewMeshes.Clear();
            overviewRoot=Root("전체 지형 (저해상도)");
            int count=Mathf.CeilToInt(definition.worldSize/512f);
            for(int z=0;z<count;z++)for(int x=0;x<count;x++)pending.Enqueue(new Vector2Int(x,z));
            total=pending.Count;completed=0;FrameWorld();UpdateStatus();
        }
        public void ShowRegion()
        {
            var p=definition.regions[region].center;ShowDetails(new Vector3(p.x,0,p.y));FrameRegion();
        }
        private void ShowViewCenter()
        {
            var view=SceneView.lastActiveSceneView;
            if(!view){status.text="Scene 뷰를 먼저 열어 주세요.";return;}
            ShowDetails(view.pivot);
        }
        public void ShowDetails(Vector3 center)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            Prepare();pending.Clear();ClearDetails();paused=false;overviewMode=false;
            if(overviewRoot)overviewRoot.SetActive(false);
            detailRoot=Root("실제 청크 · 오브젝트");
            for(int i=0;i<definition.regions.Length;i++)
            {
                var go=new GameObject(definition.regions[i].title);go.transform.SetParent(detailRoot.transform,false);
                var generator=go.AddComponent<OpenWorldStreamer>();generator.enabled=false;
                generator.PrepareEditorPreview(definition,seed,i);generators.Add(generator);
            }
            int half=definition.worldSize/2;
            center.x=Mathf.Clamp(center.x,-half+1,half-1);center.z=Mathf.Clamp(center.z,-half+1,half-1);
            var cell=new Vector2Int(Mathf.FloorToInt(center.x/definition.chunkSize),Mathf.FloorToInt(center.z/definition.chunkSize));
            var cells=new List<Vector2Int>();int limit=half/definition.chunkSize;
            for(int z=-radius;z<=radius;z++)for(int x=-radius;x<=radius;x++)
            {
                var p=cell+new Vector2Int(x,z);
                if(p.x>=-limit&&p.x<limit&&p.y>=-limit&&p.y<limit)cells.Add(p);
            }
            foreach(var p in cells.OrderBy(p=>(p-cell).sqrMagnitude))pending.Enqueue(p);
            total=pending.Count;completed=0;UpdateStatus();
        }
        private void Tick()
        {
            if(paused || pending.Count==0 || EditorApplication.isPlayingOrWillChangePlaymode)return;
            try
            {
                var cell=pending.Dequeue();
                if(overviewMode)BuildOverview(cell);
                else
                {
                    int biome=definition.Biome((cell.x+.5f)*definition.chunkSize,(cell.y+.5f)*definition.chunkSize,seed);
                    generators[biome].BuildEditorPreview(cell);
                }
                completed++;UpdateStatus();SceneView.RepaintAll();
            }
            catch(Exception e){pending.Clear();status.text=e.Message;Debug.LogException(e);}
        }
        private void BuildOverview(Vector2Int cell)
        {
            float x0=-definition.worldSize*.5f+cell.x*512,z0=-definition.worldSize*.5f+cell.y*512;
            float width=Mathf.Min(512,definition.worldSize*.5f-x0),depth=Mathf.Min(512,definition.worldSize*.5f-z0);
            var vertices=new Vector3[289];var colors=new Color[289];var triangles=new int[1536];int index=0;
            for(int z=0;z<=16;z++)for(int x=0;x<=16;x++)
            {
                int n=z*17+x;float wx=x0+x*width/16,wz=z0+z*depth/16;
                vertices[n]=new Vector3(wx,definition.Height(wx,wz,seed),wz);colors[n]=definition.GroundColor(wx,wz,seed);
                if(x==16||z==16)continue;
                triangles[index++]=n;triangles[index++]=n+17;triangles[index++]=n+1;
                triangles[index++]=n+1;triangles[index++]=n+17;triangles[index++]=n+18;
            }
            var mesh=new Mesh{name="월드 조망 지형",hideFlags=HideFlags.DontSave};mesh.vertices=vertices;mesh.colors=colors;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();overviewMeshes.Add(mesh);
            var go=new GameObject($"지형 {cell}");go.transform.SetParent(overviewRoot.transform,false);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=definition.ground;
        }
        public void FrameWorld()
        {
            var view=SceneView.lastActiveSceneView;
            if(view)view.LookAt(Vector3.zero,Quaternion.Euler(65,0,0),definition.worldSize*.58f);
        }
        public void FrameRegion()
        {
            var p=definition.regions[region].center;
            var view=SceneView.lastActiveSceneView;
            if(view)view.LookAt(new Vector3(p.x,definition.Height(p.x,p.y,seed),p.y),Quaternion.Euler(55,0,0),definition.chunkSize*(radius+.5f));
        }
        private void UpdateStatus()
        {
            if(progress!=null){progress.highValue=Mathf.Max(1,total);progress.value=completed;progress.title=$"{completed} / {total}";}
            if(status!=null)status.text=(paused?"일시정지":pending.Count>0?"불러오는 중":"불러오기 완료")+(overviewMode?" — 전체 지형 + 원본 맵 오브젝트":" — 실제 청크와 생성 오브젝트");
        }
        private void ClearDetails()
        {
            foreach(var generator in generators)if(generator)generator.ReleaseEditorPreview();generators.Clear();
            if(detailRoot)DestroyImmediate(detailRoot);
        }
        public void Clear()
        {
            pending.Clear();ClearDetails();
            if(overviewRoot)DestroyImmediate(overviewRoot);
            foreach(var mesh in overviewMeshes)DestroyImmediate(mesh);overviewMeshes.Clear();
            if(previewScene.IsValid()&&previewScene.isLoaded)EditorSceneManager.CloseScene(previewScene,true);
            previewScene=default;
            foreach(var scene in openedScenes)if(scene.IsValid()&&scene.isLoaded&&!scene.isDirty)EditorSceneManager.CloseScene(scene,true);
            openedScenes.Clear();
            foreach(var pair in viewStates)if(pair.Key){pair.Key.sceneViewState.showFog=pair.Value.fog;pair.Key.sceneViewState.showImageEffects=pair.Value.effects;}
            viewStates.Clear();completed=0;total=0;
            if(status!=null)status.text="미리보기 없음";
            if(progress!=null){progress.value=0;progress.title="대기";}
            SceneView.RepaintAll();
        }
    }
}
