using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;

namespace DoHwa.UI.Editor
{
    // One-time authoring utility. Generated clips/controllers are the editable source of motion.
    public static partial class UiAnimationMigration
    {
        private static string folder = "Assets/00_ProjectD/04_UI/Animations";
        private static AnimationClip Clip(string name)
        {
            var clip=new AnimationClip {name=name,frameRate=60};
            AssetDatabase.CreateAsset(clip,AssetDatabase.GenerateUniqueAssetPath(folder+"/"+name+".anim"));return clip;
        }
        private static void Curve(AnimationClip clip,string path,Type type,string property,float from,float to,float delay,float end)
        {
            var curve=AnimationCurve.EaseInOut(delay,from,end,to);
            if(delay>0)curve.AddKey(new Keyframe(0,from));
            AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(path,type,property),curve);
        }
        private static void Alpha(AnimationClip clip,string path,float from,float to,float delay,float end) => Curve(clip,path,typeof(CanvasGroup),"m_Alpha",from,to,delay,end);
        private static void Scale(AnimationClip clip,string path,float from,float to,float delay,float end)
        { foreach(var axis in new[]{"x","y","z"})Curve(clip,path,typeof(Transform),"m_LocalScale."+axis,from,to,delay,end); }
        private static Animator Controller(GameObject go,string name,Dictionary<string,AnimationClip> clips,bool button=false)
        {
            var controller=AnimatorController.CreateAnimatorControllerAtPath(AssetDatabase.GenerateUniqueAssetPath(folder+"/"+name+".controller"));
            var machine=controller.layers[0].stateMachine;
            foreach(var pair in clips)
            {
                var state=machine.AddState(pair.Key);state.motion=pair.Value;state.writeDefaultValues=false;
                if(button){controller.AddParameter(pair.Key,AnimatorControllerParameterType.Trigger);var tr=machine.AddAnyStateTransition(state);tr.hasExitTime=false;tr.duration=.10f;tr.canTransitionToSelf=false;tr.AddCondition(AnimatorConditionMode.If,0,pair.Key);}
            }
            if(!go.TryGetComponent<Animator>(out var animator))animator=go.AddComponent<Animator>();
            animator.runtimeAnimatorController=controller;animator.updateMode=AnimatorUpdateMode.UnscaledTime;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;return animator;
        }
        private static void Ref(UnityEngine.Object owner,string field,UnityEngine.Object value)
        { var so=new SerializedObject(owner);so.FindProperty(field).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo(); }
        private static T Field<T>(UnityEngine.Object owner,string field) where T:UnityEngine.Object => (T)new SerializedObject(owner).FindProperty(field).objectReferenceValue;
        private static string Path(Transform t,Transform root) => AnimationUtility.CalculateTransformPath(t,root);
        public static string Run()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode first");
            System.IO.Directory.CreateDirectory(folder);AssetDatabase.Refresh();
            var root=GameObject.Find("RpgCanvas");
            var craft=root.transform.Find("CraftResult").gameObject;craft.TryGetComponent<CraftResultDialog>(out var dialog);
            var open=Clip("CraftResult_Open");var close=Clip("CraftResult_Close");
            Curve(open,"",typeof(Image),"m_Color.a",0,.82f,0,.18f);Curve(close,"",typeof(Image),"m_Color.a",.82f,0,.07f,.23f);
            foreach(var item in new[]{("Result/Title",.10f,.32f), ("Result/Item",.22f,.50f),("Result/ItemName",.32f,.52f),("Result/Confirm",.42f,.60f)})
            { Alpha(open,item.Item1,0,1,item.Item2,item.Item3);Alpha(close,item.Item1,1,0,0,.14f); }
            Curve(open,"Result/Title",typeof(RectTransform),"m_AnchoredPosition.y",168,180,.1f,.32f);
            Curve(open,"Result/Item",typeof(RectTransform),"m_AnchoredPosition.y",23,35,.22f,.50f);
            Curve(open,"Result/Confirm",typeof(RectTransform),"m_AnchoredPosition.y",-194,-188,.42f,.60f);
            Scale(open,"Result/Item",.88f,1,.22f,.50f);Scale(open,"Result",1,1,0,.60f);Scale(close,"Result",1,.98f,0,.14f);
            AnimationUtility.SetAnimationEvents(open,new[]{new AnimationEvent {time=.60f,functionName="OnOpened"}});
            AnimationUtility.SetAnimationEvents(close,new[]{new AnimationEvent {time=.23f,functionName="OnClosed"}});
            Ref(dialog,"animator",Controller(craft,"CraftResult",new(){{"Open",open},{"Close",close}}));
            var inv=root.transform.Find("Inventory");inv.TryGetComponent<InventorySortMenu>(out var sort);
            var dropdown=inv.Find("SortDropdown").gameObject;var drop=Clip("SortDropdown_Open");Alpha(drop,"Options",0,1,0,.16f);Scale(drop,"Options",.97f,1,0,.16f);
            Ref(sort,"animator",Controller(dropdown,"SortDropdown",new(){{"Open",drop}}));
            foreach(var button in root.GetComponentsInChildren<UiModuleButton>(true))
            {
                button.ApplyTheme();var face=Field<Image>(button,"surface");var disk=Field<Image>(button,"iconMedallion");
                ButtonMotion(button.gameObject,face,disk.rectTransform,1.06f,.96f);
                var authored=new SerializedObject(button);authored.FindProperty("authoredMotion").boolValue=true;authored.ApplyModifiedPropertiesWithoutUndo();
            }
            foreach(var feedback in root.GetComponentsInChildren<CloseButtonFeedback>(true))
            { var circle=Field<Image>(feedback,"circle");ButtonMotion(feedback.gameObject,circle,circle.rectTransform,1.09f,.90f);UnityEngine.Object.DestroyImmediate(feedback); }
            foreach(var feedback in root.GetComponentsInChildren<HudMenuFeedback>(true))
            {
                var highlight=Field<Image>(feedback,"highlight");var glyph=Field<RectTransform>(feedback,"glyph");
                ButtonMotion(feedback.gameObject,highlight,glyph,1.045f,.92f,true);UnityEngine.Object.DestroyImmediate(feedback);
            }
            foreach(var reveal in root.GetComponentsInChildren<UiPanelReveal>(true)) BakePanel(reveal);
            craft.SetActive(false);dropdown.SetActive(false);
            EditorUtility.SetDirty(dialog);EditorUtility.SetDirty(sort);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(root.scene);AssetDatabase.SaveAssets();UnityEditor.SceneManagement.EditorSceneManager.SaveScene(root.scene);
            return "Saved authored UI, Animator controllers and Animation Clips";
        }
        private static void ButtonMotion(GameObject go,Image face,RectTransform art,float hover,float press,bool halo=false)
        {
            var clips=new Dictionary<string,AnimationClip>();
            foreach(var state in new[]{"Normal","Highlighted","Pressed","Selected","Disabled"})
            {
                bool over=state=="Highlighted"||state=="Selected";bool down=state=="Pressed";
                var clip=Clip(go.name+"_"+state);
                var color=state=="Disabled"?new Color(.84f,.85f,.87f,.5f):down?new Color(.80f,.83f,.87f):over?new Color(.94f,.97f,1):Color.white;
                if(halo)color=new Color(1,1,1,down?.16f:over?.48f:0);
                var path=Path(face.transform,go.transform);
                Curve(clip,path,typeof(Image),"m_Color.r",color.r,color.r,0,.1f);Curve(clip,path,typeof(Image),"m_Color.g",color.g,color.g,0,.1f);Curve(clip,path,typeof(Image),"m_Color.b",color.b,color.b,0,.1f);Curve(clip,path,typeof(Image),"m_Color.a",color.a,color.a,0,.1f);
                float scale=down?press:over?hover:1;Scale(clip,Path(art,go.transform),scale,scale,0,.1f);clips.Add(state,clip);
            }
            Controller(go,go.name+"_Interaction",clips,true);go.TryGetComponent<Button>(out var button);button.transition=Selectable.Transition.Animation;
            button.animationTriggers=new AnimationTriggers();EditorUtility.SetDirty(button);
        }
        private static void BakePanel(UiPanelReveal reveal)
        {
            var so=new SerializedObject(reveal);var array=so.FindProperty("sections");var sections=new RectTransform[array.arraySize];
            for(int i=0;i<sections.Length;i++)sections[i]=(RectTransform)array.GetArrayElementAtIndex(i).objectReferenceValue;
            var open=Clip(reveal.name+"_Open");var close=Clip(reveal.name+"_Close");Alpha(open,"",0,1,0,.16f);Alpha(close,"",1,0,0,.12f);
            for(int i=0;i<sections.Length;i++)
            {
                var section=sections[i];if(!section.TryGetComponent<CanvasGroup>(out var cg))cg=section.gameObject.AddComponent<CanvasGroup>();
                var path=Path(section,reveal.transform);var pos=section.anchoredPosition;
                var offset=section.name=="Detail"?Vector2.right*38:i==0?Vector2.left*30:Vector2.down*24;
                Alpha(open,path,0,1,i*.045f,i*.045f+.25f);
                foreach(var axis in new[]{"x","y"}){float p=axis=="x"?pos.x:pos.y;float d=axis=="x"?offset.x:offset.y;Curve(open,path,typeof(RectTransform),"m_AnchoredPosition."+axis,p+d,p,i*.045f,i*.045f+.32f);Curve(close,path,typeof(RectTransform),"m_AnchoredPosition."+axis,p,p-d*.35f,0,.12f);}
                int order=0;
                foreach(Transform child in section)
                {
                    if(child is not RectTransform)continue;
                    if(!child.TryGetComponent<CanvasGroup>(out var opacity))opacity=child.gameObject.AddComponent<CanvasGroup>();
                    float delay=i*.045f+Mathf.Min(order++,8)*.014f;Alpha(open,Path(child,reveal.transform),0,1,delay,delay+.30f);
                }
            }
            AnimationUtility.SetAnimationEvents(open,new[]{new AnimationEvent {time=open.length,functionName="OnOpened"}});
            AnimationUtility.SetAnimationEvents(close,new[]{new AnimationEvent {time=close.length,functionName="OnClosed"}});
            var states=new Dictionary<string,AnimationClip>{{"Open",open},{"Close",close}};
            for(int i=0;i<sections.Length;i++)
            {
                var refresh=Clip(reveal.name+"_Refresh"+i);
                foreach(var binding in AnimationUtility.GetCurveBindings(open))
                {
                    var curve=AnimationUtility.GetEditorCurve(open,binding);float value=curve.Evaluate(open.length);
                    AnimationUtility.SetEditorCurve(refresh,binding,AnimationCurve.Constant(0,.22f,value));
                }
                Alpha(refresh,Path(sections[i],reveal.transform),0,1,0,.22f);
                states.Add("Refresh"+i,refresh);
            }
            var animator=Controller(reveal.gameObject,reveal.name+"_Panel",states);
            Ref(reveal,"animator",animator);Ref(reveal,"closeClip",close);
        }
        public static string FinishInteractions(GameObject root=null)
        {
            if(!root)root=GameObject.Find("RpgCanvas");var shared=new Dictionary<string,RuntimeAnimatorController>();int count=0;
            foreach(var legacy in root.GetComponentsInChildren<UiInteractionMotion>(true))
            {
                var face=Field<Image>(legacy,"feedback");if(!face)continue;
                legacy.TryGetComponent<Selectable>(out var selectable);
                var path=Path(face.transform,legacy.transform);
                if(!legacy.TryGetComponent<Animator>(out var animator))
                {
                    if(shared.TryGetValue(path,out var cached)){animator=legacy.gameObject.AddComponent<Animator>();animator.runtimeAnimatorController=cached;animator.updateMode=AnimatorUpdateMode.UnscaledTime;}
                    else
                    {
                        var states=new Dictionary<string,AnimationClip>();foreach(var name in new[]{"Normal","Highlighted","Pressed","Selected","Disabled"})states.Add(name,Clip("Control_"+shared.Count+"_"+name));
                        animator=Controller(legacy.gameObject,"Control_"+shared.Count,states,true);shared.Add(path,animator.runtimeAnimatorController);
                    }
                }
                var ctrl=(AnimatorController)animator.runtimeAnimatorController;
                foreach(var child in ctrl.layers[0].stateMachine.states)
                {
                    var clip=(AnimationClip)child.state.motion;string name=child.state.name;
                    float alpha=name=="Highlighted"?.26f:name=="Pressed"?.30f:name=="Selected"?.18f:0;
                    Curve(clip,path,typeof(Image),"m_Color.a",alpha,alpha,0,.1f);
                }
                selectable.transition=Selectable.Transition.Animation;selectable.animationTriggers=new AnimationTriggers();
                var group=Field<CanvasGroup>(legacy,"visualGroup");if(group)group.alpha=1;
                UnityEngine.Object.DestroyImmediate(legacy);count++;
            }
            foreach(var legacy in root.GetComponentsInChildren<UiHoverCaption>(true))
            {
                var text=Field<TMPro.TMP_Text>(legacy,"caption");legacy.TryGetComponent<Animator>(out var animator);
                var ctrl=(AnimatorController)animator.runtimeAnimatorController;
                foreach(var child in ctrl.layers[0].stateMachine.states)
                {
                    float alpha=child.state.name=="Highlighted"||child.state.name=="Selected"?1:0;
                    Curve((AnimationClip)child.state.motion,Path(text.transform,legacy.transform),typeof(TMPro.TextMeshProUGUI),"m_fontColor.a",alpha,alpha,0,.1f);
                }
                UnityEngine.Object.DestroyImmediate(legacy);
            }
            AssetDatabase.SaveAssets();UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(root.scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(root.scene);
            return count+" control effects moved to Selectable Animation";
        }
    }
}
