using System.Linq;
using DoHwa.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace DoHwa.Editor
{
    public static class RpgUiAuthoring
    {
        private static TMP_FontAsset regular, bold;
        private static Sprite round, disk;
        private static readonly Color Ink=new Color(.09f,.13f,.18f);
        private static readonly Color Paper=new Color(.94f,.93f,.88f);
        private static readonly Color White=new Color(.94f,.95f,.92f);
        private static readonly Color Muted=new Color(.60f,.67f,.69f);
        private static readonly Color Jade=new Color(.60f,.84f,.74f);
        private static readonly Color Gold=new Color(.88f,.77f,.52f);
        private static readonly Color Panel=new Color(.08f,.12f,.17f,.91f);

        private static void Resources()
        {
            regular=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Plugins/TextMesh Pro/Resources/Fonts & Materials/LINESeedKR-Regular SDF.asset");
            bold=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Plugins/TextMesh Pro/Resources/Fonts & Materials/LINESeedKR-Bold SDF.asset");
            round=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/00_ProjectD/04_UI/Login/RoundedPanel.png");
            disk=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/00_ProjectD/04_UI/Login/UtilityDisk.png");
        }

        private static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h)
        {
            var go=new GameObject(name,typeof(RectTransform)); go.layer=5;
            var r=(RectTransform)go.transform; r.SetParent(parent,false); r.anchorMin=r.anchorMax=new Vector2(0,1); r.pivot=new Vector2(0,1); r.anchoredPosition=new Vector2(x,-y); r.sizeDelta=new Vector2(w,h); return r;
        }
        private static void Stretch(RectTransform r)
        { r.anchorMin=Vector2.zero; r.anchorMax=Vector2.one; r.offsetMin=r.offsetMax=Vector2.zero; }
        private static void Anchor(RectTransform r,Vector2 anchor,float x,float y)
        { r.anchorMin=r.anchorMax=anchor; r.anchoredPosition=new Vector2(x,y); }
        private static UnityEngine.UI.Image Image(RectTransform r,Color color,Sprite sprite=null)
        {
            var image=r.gameObject.AddComponent<UnityEngine.UI.Image>(); image.sprite=sprite; image.color=color; image.raycastTarget=false;
            if(sprite==round) image.type=UnityEngine.UI.Image.Type.Sliced;
            return image;
        }
        private static RectTransform Plate(string name,Transform parent,float x,float y,float w,float h,Color color,bool rounded=true)
        { var r=Rect(name,parent,x,y,w,h); Image(r,color,rounded?round:null); return r; }
        private static TMP_Text Text(string name,Transform parent,string text,float x,float y,float w,float h,float size,Color color,bool strong=false,TextAlignmentOptions align=TextAlignmentOptions.MidlineLeft)
        {
            var r=Rect(name,parent,x,y,w,h); var t=r.gameObject.AddComponent<TextMeshProUGUI>(); t.font=strong?bold:regular; t.text=text; t.fontSize=size; t.color=color; t.alignment=align; t.raycastTarget=false; t.textWrappingMode=TextWrappingModes.Normal; return t;
        }
        private static UnityEngine.UI.Button Button(string name,Transform parent,string label,float x,float y,float w,float h,Color color,UnityAction callback,Color? textColor=null)
        {
            var r=Plate(name,parent,x,y,w,h,color); r.TryGetComponent<UnityEngine.UI.Image>(out var image); image.raycastTarget=true;
            var b=r.gameObject.AddComponent<UnityEngine.UI.Button>(); b.targetGraphic=image;
            var colors=b.colors; colors.highlightedColor=new Color(1.12f,1.12f,1.12f); colors.pressedColor=new Color(.78f,.88f,.85f); colors.disabledColor=new Color(.5f,.5f,.5f,.65f); b.colors=colors;
            Text("Label",r,label,10,0,w-20,h,23,textColor??White,true,TextAlignmentOptions.Center);
            if(callback!=null) UnityEventTools.AddPersistentListener(b.onClick,callback);
            return b;
        }
        private static void Rule(Transform parent,float x,float y,float w,Color color) => Plate("Rule",parent,x,y,w,1,color,false);
        private static void Header(Transform parent,string title,string subtitle,UnityAction close)
        {
            var header=Rect("Header",parent,0,0,1760,100);
            var heading=Text("Title",header,title,12,5,400,56,38,White,true);
            heading.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Plugins/TextMesh Pro/Resources/Fonts & Materials/GmarketSansBold SDF.asset");
            if (subtitle.Length > 0) Text("Subtitle",header,subtitle,14,62,1000,28,17,Muted);
            var b=Button("Close",header,"",1686,8,62,62,new Color(.22f,.28f,.32f),close);
            var icon=Rect("Icon",b.transform,18,18,26,26).gameObject.AddComponent<LoginLineIcon>(); icon.SetSymbol(LoginLineIcon.Symbol.Close); icon.color=White; icon.raycastTarget=false;
        }
        private static RectTransform Screen(string name,Transform root)
        {
            var r=Rect(name,root,0,0,0,0); Stretch(r); var bg=Image(r,new Color(.055f,.085f,.125f,.97f)); bg.raycastTarget=true;
            return r;
        }
        private static RectTransform Workspace(Transform screen)
        {
            var r=Rect("Workspace",screen,0,0,1760,920); r.anchorMin=r.anchorMax=new Vector2(.5f,.5f); r.pivot=new Vector2(.5f,.5f); r.anchoredPosition=Vector2.zero; return r;
        }
        private static UnityEngine.UI.Image Art(string name,Transform parent,Sprite sprite,float x,float y,float w,float h)
        { var a=Image(Rect(name,parent,x,y,w,h),Color.white,sprite); a.preserveAspect=true; return a; }

        private static Sprite[] ImportIcons()
        {
            string path="Assets/00_ProjectD/04_UI/RPG/WoodlandPixelItems.png";
            var importer=(TextureImporter)AssetImporter.GetAtPath(path); importer.textureType=TextureImporterType.Default; importer.alphaIsTransparency=true; importer.mipmapEnabled=false; importer.filterMode=FilterMode.Point; importer.maxTextureSize=2048; importer.textureCompression=TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path); var sprites=new Sprite[12];
            float w=texture.width/4f,h=texture.height/3f;
            for(int i=0;i<12;i++)
            {
                string spritePath=$"Assets/00_ProjectD/04_UI/RPG/PixelItem{i:00}.asset";
                sprites[i]=AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                if(sprites[i]) continue;
                var rect=new UnityEngine.Rect((i%4)*w,texture.height-(i/4+1)*h,w,h);
                sprites[i]=Sprite.Create(texture,rect,new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect); sprites[i].name=$"Item{i:00}"; AssetDatabase.CreateAsset(sprites[i],spritePath);
            }
            return sprites;
        }

        private static void Slot(Transform parent,RpgUiController controller,int number,float x,float y,float size,bool quick)
        {
            var r=Plate("Slot"+number,parent,x,y,size,size,new Color(.7f,.8f,.8f,.16f)); r.TryGetComponent<UnityEngine.UI.Image>(out var border);
            var fill=Plate("Fill",r,2,2,size-4,size-4,new Color(.13f,.18f,.23f,.97f)); fill.TryGetComponent<UnityEngine.UI.Image>(out var face); face.raycastTarget=true;
            var art=Art("Art",r,null,8,8,size-16,size-20);
            var count=Text("Count",r,"",6,size-28,size-16,23,quick?17:20,White,true,TextAlignmentOptions.Right);
            var status=Text("Equipped",r,"",8,5,size-16,22,15,Jade,true);
            if(quick) Text("Key",r,(number+1).ToString(),6,3,22,22,14,Muted);
            var button=r.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic=face;
            var colors=button.colors; colors.disabledColor=Color.white; button.colors=colors;
            var item=r.gameObject.AddComponent<RpgItemButton>(); item.Configure(controller,quick,art,border,count,status,button); UnityEventTools.AddPersistentListener(button.onClick,item.Click);
        }

        [MenuItem("Tools/Project D/Create RPG Interface")]
        public static void CreateIngame()
        {
            if(EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode first.");
            var scene=SceneManager.GetActiveScene();
            if(scene.name!="Ingame") throw new System.InvalidOperationException("Open Ingame first.");
            if(GameObject.Find("RpgCanvas")) throw new System.InvalidOperationException("RpgCanvas already exists; edit it in place.");
            Resources(); var sprites=ImportIcons();
            var go=new GameObject("RpgCanvas",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster)); go.layer=5;
            go.TryGetComponent<Canvas>(out var canvas); canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=20;
            go.TryGetComponent<UnityEngine.UI.CanvasScaler>(out var scaler); scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1920,1080); scaler.matchWidthOrHeight=1;
            var root=go.transform; var controller=go.AddComponent<RpgUiController>();
            var motor=Object.FindFirstObjectByType<HeroMotor>(); var cameraView=Object.FindFirstObjectByType<HeroCamera>();
            var cameraObject=new GameObject("UI Minimap Camera"); var mini=cameraObject.AddComponent<Camera>(); mini.orthographic=true; mini.orthographicSize=14; mini.transform.rotation=Quaternion.Euler(90,0,0); mini.transform.position=motor.transform.position+Vector3.up*45; mini.nearClipPlane=.1f; mini.farClipPlane=150; mini.clearFlags=CameraClearFlags.SolidColor; mini.backgroundColor=new Color(.14f,.22f,.22f); mini.cullingMask=~(1<<5); mini.allowHDR=false; mini.allowMSAA=false; mini.depth=-10;
            var texture=new RenderTexture(512,512,16); texture.name="WoodlandMinimap"; AssetDatabase.CreateAsset(texture,"Assets/00_ProjectD/04_UI/RPG/WoodlandMinimap.renderTexture"); mini.targetTexture=texture;
            string[] names={"청옥 단검","철 곡괭이","잘 익은 복숭아","회복약","단단한 목재","철광석","싱싱한 약초","계곡 물고기","아마포","씨앗 주머니","청옥 노리개","금속 주괴"};
            string[] descriptions={"푸른 옥으로 손잡이를 감싼 짧은 검.\n가볍고 균형이 좋아 숲길의 동반자가 된다.","단단한 광맥을 캐는 탐험 도구.\n나무 손잡이에 오랜 손길이 남아 있다.","도화림의 햇살을 머금은 달콤한 복숭아.\n\n포만감 +20   /   체력 +5","숲의 약초를 정성껏 달인 붉은 물약.\n\n체력 +35","도구와 가구를 만드는 데 쓰는 목재.","푸른 빛이 도는 단단한 광석.\n제련 재료로 사용한다.","강가 그늘에서 자라는 향긋한 약초.\n회복약을 만드는 재료다.","맑은 계곡에서 잡은 작은 물고기.\n\n포만감 +20   /   체력 +5","옷감과 생활 도구에 쓰는 부드러운 천.","새로운 터전에 심을 씨앗이 든 주머니.","고운 매듭과 푸른 옥을 엮은 장신구.\n숲의 평온을 담고 있다.","단단하게 제련한 작은 주괴.\n도구 제작에 쓰이는 재료다."};
            int[] quantities={1,1,8,3,24,12,15,4,6,10,1,2};
            var catalog=Enumerable.Range(0,12).Select(i=>new RpgUiController.Item{title=names[i],description=descriptions[i],category=i==0||i==1||i==10?"장비":i==2||i==3||i==7?"소모품":"재료",quantity=quantities[i],icon=sprites[i]}).ToArray();
            controller.Configure(catalog,motor,cameraView,mini);

            var hud=Rect("HUD",root,0,0,0,0); Stretch(hud);
            var region=Rect("Region",hud,40,35,340,330);
            var ring=Plate("MapFrame",region,0,0,172,172,Gold);
            var mask=Plate("Mask",ring,3,3,166,166,Color.white); mask.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic=false;
            var mapImage=Rect("Terrain",mask,0,0,166,166).gameObject.AddComponent<UnityEngine.UI.RawImage>(); mapImage.texture=texture; mapImage.raycastTarget=false;
            var marker=Rect("Marker",ring,75,75,22,22).gameObject.AddComponent<LoginLineIcon>(); marker.SetSymbol(LoginLineIcon.Symbol.Star); marker.color=White; marker.raycastTarget=false;
            Text("North",ring,"N",72,4,28,24,16,White,true,TextAlignmentOptions.Center);
            var mapButton=ring.gameObject.AddComponent<UnityEngine.UI.Button>(); ring.TryGetComponent<UnityEngine.UI.Image>(out var mapFace); mapFace.raycastTarget=true; mapButton.targetGraphic=mapFace; UnityEventTools.AddPersistentListener(mapButton.onClick,controller.OpenMap);
            Text("Name",region,"도화림",194,18,150,38,29,White,true);
            Text("Season",region,"봄  /  맑음",196,59,150,28,18,White);
            Text("MapHint",region,"지도  M",196,96,120,25,16,White);
            var objective=Plate("Objective",region,0,199,324,126,new Color(.07f,.12f,.16f,.78f));
            Plate("Accent",objective,0,16,3,90,Jade,false);
            Text("Tag",objective,"탐험 수첩",20,13,260,27,16,Jade,true);
            Text("Title",objective,"숲이 들려주는 이야기",20,41,286,32,23,White,true);
            Text("Hint",objective,"오늘의 준비물을 확인하세요  J",20,83,286,25,16,Muted);
            var nav=Rect("Navigation",hud,0,0,520,58); Anchor(nav,new Vector2(1,1),-558,-38);
            Button("Bag",nav,"배낭  B",0,0,116,52,Panel,controller.OpenInventory);
            Button("Craft",nav,"제작  C",128,0,116,52,Panel,controller.OpenCrafting);
            Button("Journal",nav,"수첩  J",256,0,116,52,Panel,controller.OpenJournal);
            Button("Settings",nav,"설정",384,0,116,52,Panel,controller.OpenSettings);
            var vitals=Plate("Vitals",hud,0,0,306,118,Panel); Anchor(vitals,new Vector2(0,0),40,158);
            Text("HealthText",vitals,"",18,13,270,26,18,White,true);
            var healthTrack=Plate("HealthTrack",vitals,18,44,270,8,new Color(.3f,.36f,.38f)); var hp=Image(Rect("Fill",healthTrack,0,0,270,8),Jade,round); hp.type=UnityEngine.UI.Image.Type.Filled; hp.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;
            Text("FoodText",vitals,"",18,62,270,24,16,White);
            var foodTrack=Plate("FoodTrack",vitals,18,94,270,5,new Color(.3f,.36f,.38f)); var food=Image(Rect("Fill",foodTrack,0,0,270,5),Gold,round); food.type=UnityEngine.UI.Image.Type.Filled; food.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;
            var bar=Rect("Quickbar",hud,0,0,680,78); Anchor(bar,new Vector2(.5f,0),-340,116);
            for(int i=0;i<8;i++) Slot(bar,controller,i,i*85,0,76,true);
            var quickHint=Text("QuickHint",hud,"1–8  도구 선택 / 소모품 사용",0,0,680,24,16,White,false,TextAlignmentOptions.Center); Anchor(quickHint.rectTransform,new Vector2(.5f,0),-340,30);
            var hints=Text("Controls",hud,"이동  W A S D\n달리기  Shift     회피  Space",0,0,330,74,18,White,false,TextAlignmentOptions.Right); Anchor(hints.rectTransform,new Vector2(1,0),-370,110);

            // Inventory nodes are direct children of a centered design area for stable reference binding.
            var inventory=Screen("Inventory",root); var workspace=Workspace(inventory);
            Header(workspace,"배낭","INVENTORY  /  여정에 필요한 것들",controller.Close);
            var filters=Rect("Filters",workspace,14,138,184,360);
            string[] filtersText={"전체","장비","소모품","재료"};
            for(int i=0;i<4;i++) { var b=Button("Filter"+i,filters,filtersText[i],0,i*66,172,54,Panel,null); UnityEventTools.AddIntPersistentListener(b.onClick,controller.SetFilter,i); }
            Button("Craft",filters,"제작하기  C",0,320,172,54,new Color(.26f,.4f,.38f),controller.OpenCrafting);
            Text("Capacity",workspace,"",238,127,500,36,23,White,true);
            Button("Sort",workspace,"정렬",994,124,112,40,new Color(.2f,.25f,.29f),controller.Sort);
            var grid=Rect("Grid",workspace,238,188,882,564); var layout=grid.gameObject.AddComponent<UnityEngine.UI.GridLayoutGroup>(); layout.cellSize=new Vector2(132,132); layout.spacing=new Vector2(14,14); layout.constraint=UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount; layout.constraintCount=6;
            for(int i=0;i<24;i++) Slot(grid,controller,i,0,0,132,false);
            var equipment=Plate("Equipment",workspace,238,794,868,98,new Color(.12f,.18f,.22f)); Art("Icon",equipment,sprites[0],12,9,80,80);
            Text("Equipped",equipment,"",116,13,400,74,23,White,true);
            Text("EquipmentHint",equipment,"장비를 선택해 교체할 수 있습니다",460,27,380,46,17,Muted,false,TextAlignmentOptions.Right);
            var detail=Plate("Detail",workspace,1166,130,580,762,Paper);
            Text("Category",detail,"",34,26,512,30,18,new Color(.36f,.43f,.43f));
            Art("Art",detail,sprites[0],158,80,264,244);
            Rule(detail,34,345,512,new Color(.15f,.2f,.23f,.2f));
            Text("Title",detail,"",34,367,512,52,34,Ink,true);
            var desc=Text("Description",detail,"",34,437,512,115,21,new Color(.3f,.36f,.39f)); desc.alignment=TextAlignmentOptions.TopLeft;
            Text("Count",detail,"",34,561,512,100,19,new Color(.3f,.36f,.39f));
            Button("Action",detail,"",34,680,512,55,new Color(.22f,.38f,.36f),controller.UseSelected);
            // Move design children to the screen while keeping their centered coordinates.
            Flatten(workspace,inventory);
            inventory.gameObject.SetActive(false);

            var map=Screen("Map",root); var mapSpace=Workspace(map); Header(mapSpace,"도화림 지도","MAP  /  현재 위치 주변의 지형",controller.Close);
            var mapFrame=Plate("TerrainFrame",mapSpace,180,136,800,720,new Color(.2f,.29f,.28f));
            var largeMap=Rect("Terrain",mapFrame,40,0,720,720).gameObject.AddComponent<UnityEngine.UI.RawImage>(); largeMap.texture=texture; largeMap.raycastTarget=false;
            var pin=Rect("Position",mapFrame,388,348,24,24).gameObject.AddComponent<LoginLineIcon>(); pin.SetSymbol(LoginLineIcon.Symbol.Star); pin.color=White; pin.raycastTarget=false;
            Text("Info",mapSpace,"도화림\n\n복숭아꽃이 머무는 숲",1080,186,540,130,30,White,true);
            Text("Legend",mapSpace,"현재 위치를 중심으로 표시합니다.\n\n숲길을 따라 강가와 목교를 찾아보세요.",1080,390,540,160,23,Muted);
            Text("MapKey",mapSpace,"M  지도 닫기",1080,748,500,50,20,Jade); map.gameObject.SetActive(false);
            var journal=Screen("Journal",root); var journalSpace=Workspace(journal); Header(journalSpace,"탐험 수첩","",controller.Close);
            var page=Plate("Page",journalSpace,240,144,1280,680,Paper);
            Text("Chapter",page,"첫 번째 여정",56,42,700,40,22,new Color(.36f,.45f,.42f),true);
            Text("Title",page,"숲에 머물 준비",56,104,1000,70,42,Ink,true);
            Rule(page,56,205,1168,new Color(.15f,.2f,.23f,.18f));
            Text("Tasks",page,"01   배낭에서 음식과 회복약 확인하기\n\n02   도구를 선택하고 숲길 둘러보기\n\n03   약초와 복숭아로 회복약 만들기",56,251,1060,280,28,Ink);
            Button("OpenBag",page,"배낭 열기",56,570,340,64,new Color(.22f,.38f,.36f),controller.OpenInventory);
            Button("OpenCrafting",page,"제작 확인",420,570,340,64,new Color(.22f,.38f,.36f),controller.OpenCrafting); journal.gameObject.SetActive(false);

            var settings=Screen("Settings",root); var settingsSpace=Workspace(settings); Header(settingsSpace,"설정","",controller.Close);
            var settingsPanel=Plate("Panel",settingsSpace,390,220,980,420,Paper);
            Text("VolumeTitle",settingsPanel,"전체 음량",48,70,260,50,27,Ink,true);
            var sliderRect=Rect("Volume",settingsPanel,330,72,570,44); var slider=sliderRect.gameObject.AddComponent<UnityEngine.UI.Slider>();
            Plate("Track",sliderRect,0,18,570,8,new Color(.69f,.73f,.71f));
            var fillArea=Rect("FillArea",sliderRect,0,18,570,8); var fill=Rect("Fill",fillArea,0,0,0,0); Image(fill,new Color(.28f,.5f,.44f));
            var handleArea=Rect("HandleArea",sliderRect,0,22,570,0); var handle=Rect("Handle",handleArea,0,0,24,24); handle.pivot=new Vector2(.5f,.5f); var handleImage=Image(handle,Ink,disk); handleImage.raycastTarget=true;
            slider.fillRect=fill; slider.handleRect=handle; slider.targetGraphic=handleImage; slider.value=1; UnityEventTools.AddPersistentListener(slider.onValueChanged,controller.SetVolume);
            Text("Keys",settingsPanel,"배낭 B   /   제작 C   /   지도 M   /   수첩 J",48,170,870,60,22,new Color(.35f,.4f,.42f));
            Button("Confirm",settingsPanel,"돌아가기",48,300,884,66,new Color(.22f,.38f,.36f),controller.Close);
            Flatten(settingsSpace,settings); settings.gameObject.SetActive(false);
            var toast=Plate("Feedback",root,0,0,630,56,new Color(.08f,.14f,.17f,.95f)); Anchor(toast,new Vector2(.5f,0),-315,210); Text("Label",toast,"",20,0,590,56,22,White,false,TextAlignmentOptions.Center); toast.gameObject.SetActive(false);
            var events=new GameObject("EventSystem",typeof(UnityEngine.EventSystems.EventSystem),typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule)); events.TryGetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>(out var input); input.AssignDefaultActions();
            controller.Bind();
            EditorSceneManager.SaveScene(scene);
            var scenes=EditorBuildSettings.scenes.ToList(); if(!scenes.Any(s=>s.path==scene.path)) scenes.Add(new EditorBuildSettingsScene(scene.path,true)); EditorBuildSettings.scenes=scenes.ToArray();
            AssetDatabase.SaveAssets();
        }

        private static void Flatten(RectTransform workspace,RectTransform screen)
        {
            foreach(var child in workspace.Cast<Transform>().ToArray())
            {
                var r=(RectTransform)child; var pos=r.anchoredPosition; r.SetParent(screen,false); r.anchorMin=r.anchorMax=new Vector2(.5f,.5f); r.anchoredPosition=pos+new Vector2(-880,460);
            }
            Object.DestroyImmediate(workspace.gameObject);
        }

        public static void RefineLogin()
        {
            Resources();
            var scene=EditorSceneManager.OpenScene("Assets/00_ProjectD/01_Scenes/Production/Login.unity");
            var root=GameObject.Find("LoginCanvas").transform;
            root.Find("ReadabilityShade").TryGetComponent<UnityEngine.UI.Image>(out var shade); shade.color=new Color(.035f,.055f,.09f,.12f);
            var start=(RectTransform)root.Find("LoginActions/LoginButton"); start.anchorMin=start.anchorMax=new Vector2(.5f,.17f); start.pivot=new Vector2(.5f,.5f); start.anchoredPosition=Vector2.zero; start.sizeDelta=new Vector2(520,66);
            start.TryGetComponent<UnityEngine.UI.Image>(out var startImage); startImage.sprite=round; startImage.type=UnityEngine.UI.Image.Type.Sliced; startImage.color=Paper;
            start.Find("Label").TryGetComponent<TMP_Text>(out var label); label.text="여정 시작"; label.font=bold; label.fontSize=25; label.color=Ink; label.characterSpacing=3; var labelRect=label.rectTransform; Stretch(labelRect);
            start.Find("LeftDiamond").gameObject.SetActive(false); start.Find("RightDiamond").gameObject.SetActive(false);
            var glyph=Rect("StartMark",start,429,21,24,24).gameObject.AddComponent<LoginLineIcon>(); glyph.SetSymbol(LoginLineIcon.Symbol.Star); glyph.color=new Color(.28f,.45f,.41f); glyph.raycastTarget=false;
            start.TryGetComponent<UnityEngine.UI.Button>(out var button); var colors=button.colors; colors.highlightedColor=new Color(.85f,1,.94f); colors.pressedColor=new Color(.65f,.83f,.78f); button.colors=colors;
            var entry=root.gameObject.AddComponent<LoginEntry>(); entry.Configure(button,(TMP_Text)label); UnityEventTools.AddPersistentListener(button.onClick,entry.Enter);
            var mode=Text("PlayMode",root,"로컬 플레이",0,0,520,26,17,White,false,TextAlignmentOptions.Center); Anchor(mode.rectTransform,new Vector2(.5f,.17f),-260,70); mode.characterSpacing=3;
            var hint=Text("EntryHint",root,"달빛이 머무는 숲으로",0,0,520,30,18,White,false,TextAlignmentOptions.Center); Anchor(hint.rectTransform,new Vector2(.5f,.17f),-260,-53);
            var note=Rect("LocationNote",root,72,62,450,140);
            Text("English",note,"P E A C H   F O R E S T",0,0,440,28,16,new Color(.83f,.87f,.82f));
            Text("Location",note,"도화림",0,37,440,54,36,White,true);
            Rule(note,0,109,46,Jade);
            var utilities=(RectTransform)root.Find("UtilityNavigation"); utilities.anchorMin=new Vector2(1,0); utilities.anchorMax=new Vector2(1,1); utilities.anchoredPosition=new Vector2(-90,0); utilities.sizeDelta=new Vector2(100,0);
            string[] names={"LanguageButton","SettingsButton","NoticesButton","ExitButton"}; string[] captions={"언어","설정","공지","종료"};
            for(int i=0;i<4;i++)
            {
                var r=(RectTransform)utilities.Find(names[i]); r.anchorMin=r.anchorMax=new Vector2(.5f,0); r.pivot=new Vector2(.5f,.5f); r.anchoredPosition=new Vector2(0,365-i*92); r.sizeDelta=new Vector2(50,50);
                r.TryGetComponent<UnityEngine.UI.Image>(out var background); background.color=new Color(.08f,.13f,.16f,.72f);
                var icon=(RectTransform)r.Find("Icon"); icon.sizeDelta=new Vector2(30,30);
                Text("Caption",r,captions[i],-13,57,76,23,16,White,false,TextAlignmentOptions.Center);
            }
            root.Find("Version/Label").TryGetComponent<TMP_Text>(out var version); version.fontSize=14; version.color=new Color(.82f,.86f,.85f,.8f);
            EditorSceneManager.SaveScene(scene);
        }
    }
}

