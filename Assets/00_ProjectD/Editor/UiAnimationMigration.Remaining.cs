using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;
namespace DoHwa.UI.Editor
{
    public static partial class UiAnimationMigration
    {
        private static AnimatorControllerLayer Layer(AnimatorController controller,string name)
        {
            var machine=new AnimatorStateMachine{name=name};
            AssetDatabase.AddObjectToAsset(machine,controller);
            var layer=new AnimatorControllerLayer{name=name,stateMachine=machine,defaultWeight=1};
            controller.AddLayer(layer); return layer;
        }
        private static AnimatorState State(AnimatorStateMachine machine,string name,AnimationClip clip)
        {var state=machine.AddState(name);state.motion=clip;state.writeDefaultValues=false;return state;}
        private static void BoolLayer(AnimatorController controller,string layerName,string parameter,AnimationClip off,AnimationClip on,float duration=.1f)
        {
            controller.AddParameter(parameter,AnimatorControllerParameterType.Bool);
            var machine=Layer(controller,layerName).stateMachine;
            var a=State(machine,"Off",off);var b=State(machine,"On",on);
            var enter=a.AddTransition(b);enter.hasExitTime=false;enter.duration=duration;enter.AddCondition(AnimatorConditionMode.If,0,parameter);
            var leave=b.AddTransition(a);leave.hasExitTime=false;leave.duration=duration;leave.AddCondition(AnimatorConditionMode.IfNot,0,parameter);
        }
        private static void PulseLayer(AnimatorController controller,string prefix,Type type,string property,float duration,float peak)
        {
            controller.AddParameter("ItemChanged",AnimatorControllerParameterType.Trigger);
            var machine=Layer(controller,"Result emphasis").stateMachine;
            var idle=Clip(prefix+"_EmphasisIdle");Curve(idle,"",type,property,0,0,0,.1f);
            var pulse=Clip(prefix+"_Emphasis");
            AnimationUtility.SetEditorCurve(pulse,EditorCurveBinding.FloatCurve("",type,property),
                new AnimationCurve(new Keyframe(0,0),new Keyframe(duration*.5f,peak),new Keyframe(duration,0)));
            var a=State(machine,"Idle",idle);var b=State(machine,"Pulse",pulse);
            var tr=machine.AddAnyStateTransition(b);tr.hasExitTime=false;tr.duration=0;tr.canTransitionToSelf=true;tr.AddCondition(AnimatorConditionMode.If,0,"ItemChanged");
            var exit=b.AddTransition(a);exit.hasExitTime=true;exit.exitTime=1;exit.duration=0;
        }
        private static void ColorCurve(AnimationClip clip,string path,Type type,string property,Color color)
        {
            for(int i=0;i<4;i++)Curve(clip,path,type,property+"."+"rgba"[i],color[i],color[i],0,.12f);
        }
        private static Animator Fresh(GameObject go,string name)
        {return Controller(go,name,new Dictionary<string,AnimationClip>{{"Normal",Clip(name+"_Idle")}});}
        public static string MigrateRemainingIngame()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode first");
            var root=GameObject.Find("RpgCanvas");
            foreach(var item in root.GetComponentsInChildren<RpgItemButton>(true)) BakeCard(item);
            foreach(var slot in root.GetComponentsInChildren<EquipmentSlotFeedback>(true)) BakeEquipment(slot);
            foreach(var gauge in root.GetComponentsInChildren<UiGaugeMotion>(true)) BakeGauge(gauge);
            foreach(var blur in root.GetComponentsInChildren<UiBackdropBlur>(true)) BakeBlur(blur);
            foreach(var legacy in root.GetComponentsInChildren<UiInteractionMotion>(true))
            {
                var group=Field<CanvasGroup>(legacy,"visualGroup");
                legacy.TryGetComponent<Selectable>(out var control);
                if(group)
                {
                    if(!legacy.TryGetComponent<Animator>(out var animator))animator=Fresh(legacy.gameObject,legacy.name+"_Control");
                    var controller=(AnimatorController)animator.runtimeAnimatorController;
                    foreach(var trigger in new[]{"Normal","Highlighted","Pressed","Selected","Disabled"})
                    {
                        if(!Array.Exists(controller.parameters,p=>p.name==trigger))controller.AddParameter(trigger,AnimatorControllerParameterType.Trigger);
                        var clip=Clip(legacy.name+"_"+trigger);Alpha(clip,Path(group.transform,legacy.transform),trigger=="Disabled"?.42f:1,trigger=="Disabled"?.42f:1,0,.1f);
                        var state=State(controller.layers[0].stateMachine,trigger,clip);
                        var tr=controller.layers[0].stateMachine.AddAnyStateTransition(state);tr.hasExitTime=false;tr.duration=.1f;tr.AddCondition(AnimatorConditionMode.If,0,trigger);
                    }
                    control.transition=Selectable.Transition.Animation;control.animationTriggers=new AnimationTriggers();
                }
                UnityEngine.Object.DestroyImmediate(legacy);
            }
            AssetDatabase.SaveAssets();UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(root.scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(root.scene);
            return "44 item cards, 9 equipment slots, gauges, blur and remaining controls authored";
        }
        private static void BakeCard(RpgItemButton item)
        {
            var so=new SerializedObject(item);bool quick=so.FindProperty("quick").boolValue;
            var art=Field<Image>(item,"art");var border=Field<Image>(item,"border");
            var face=Field<Image>(item,"cardFace");var band=Field<Image>(item,"quantityBand");
            item.TryGetComponent<Animator>(out var animator);
            var controllerPath=AssetDatabase.GenerateUniqueAssetPath(folder+"/"+(quick?"Quick":"Card")+"_Appearance.controller");
            AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(animator.runtimeAnimatorController),controllerPath);
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            animator.runtimeAnimatorController=controller;
            Ref(item,"animator",animator);
            var machine=Layer(controller,"Item appearance").stateMachine;
            foreach(var p in new[]{"ItemSelected","ItemHovered","ItemPressed"})controller.AddParameter(p,AnimatorControllerParameterType.Bool);
            var states=new List<(AnimatorState state,bool selected,int input)>();
            for(int selected=0;selected<2;selected++)for(int input=0;input<3;input++)
            {
                bool chosen=selected==1;string name=(chosen?"Chosen":"Default")+new[]{"Rest","Hover","Press"}[input];
                var clip=Clip((quick?"Quick":"Card")+"_"+name);
                Color edge=chosen?new Color(.68f,.82f,.94f):input==1?new Color(.89f,.94f,.98f):Color.white;
                if(input==2)edge*=new Color(.90f,.93f,.97f,1);
                ColorCurve(clip,Path(border.transform,item.transform),typeof(Image),"m_Color",edge);
                if(!quick)
                {
                    ColorCurve(clip,Path(face.transform,item.transform),typeof(Image),"m_Color",chosen?new Color(.59f,.76f,.90f):input==1?new Color(.86f,.90f,.95f):new Color(.82f,.86f,.92f));
                    ColorCurve(clip,Path(band.transform,item.transform),typeof(Image),"m_Color",chosen?new Color(.90f,.95f,.99f):new Color(.98f,.98f,.97f));
                }
                float y=art.rectTransform.anchoredPosition.y+(input==2?-2:0);
                Curve(clip,Path(art.transform,item.transform),typeof(RectTransform),"m_AnchoredPosition.y",y,y,0,.1f);
                states.Add((State(machine,name,clip),chosen,input));
            }
            foreach(var target in states)
            {
                var tr=machine.AddAnyStateTransition(target.state);tr.hasExitTime=false;tr.duration=.09f;tr.canTransitionToSelf=false;
                tr.AddCondition(target.selected?AnimatorConditionMode.If:AnimatorConditionMode.IfNot,0,"ItemSelected");
                tr.AddCondition(target.input==2?AnimatorConditionMode.If:AnimatorConditionMode.IfNot,0,"ItemPressed");
                if(target.input!=2)tr.AddCondition(target.input==1?AnimatorConditionMode.If:AnimatorConditionMode.IfNot,0,"ItemHovered");
            }
            // Each highlight follows its original sprite, so masks and sliced corners retain their proportions.
            var pulse=Clip((quick?"Quick":"Card")+"_ResultPulse");var idle=Clip((quick?"Quick":"Card")+"_ResultIdle");
            foreach(var source in quick?new[]{border}:new[]{border,face})
            {
                var go=new GameObject(source.name+"ResultHighlight",typeof(RectTransform),typeof(Image));go.transform.SetParent(source.transform,false);
                var rt=(RectTransform)go.transform;rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=rt.offsetMax=Vector2.zero;
                go.TryGetComponent<Image>(out var overlay);overlay.sprite=source.sprite;overlay.type=source.type;overlay.pixelsPerUnitMultiplier=source.pixelsPerUnitMultiplier;overlay.raycastTarget=false;overlay.color=new Color(1,1,1,0);
                var path=Path(go.transform,item.transform);
                Curve(idle,path,typeof(Image),"m_Color.a",0,0,0,.1f);
                AnimationUtility.SetEditorCurve(pulse,EditorCurveBinding.FloatCurve(path,typeof(Image),"m_Color.a"),new AnimationCurve(new Keyframe(0,0),new Keyframe(.31f,.7f),new Keyframe(.625f,0)));
            }
            controller.AddParameter("ItemChanged",AnimatorControllerParameterType.Trigger);
            var pm=Layer(controller,"Result emphasis").stateMachine;var pa=State(pm,"Idle",idle);var pb=State(pm,"Pulse",pulse);
            var enter=pm.AddAnyStateTransition(pb);enter.hasExitTime=false;enter.duration=0;enter.canTransitionToSelf=true;enter.AddCondition(AnimatorConditionMode.If,0,"ItemChanged");
            var exit=pb.AddTransition(pa);exit.hasExitTime=true;exit.exitTime=1;exit.duration=0;
        }
        private static void BakeEquipment(EquipmentSlotFeedback slot)
        {
            var animator=Fresh(slot.gameObject,slot.name+"_Equipment");Ref(slot,"animator",animator);
            var ctrl=(AnimatorController)animator.runtimeAnimatorController;
            foreach(var pair in new[]{("Hover","SlotHovered","focus"),("Press","SlotPressed","press"),("Selection","EquippedSelected","selection")})
            {
                var off=Clip(slot.name+"_"+pair.Item1+"Off");var on=Clip(slot.name+"_"+pair.Item1+"On");
                Curve(off,"",typeof(EquipmentSlotFeedback),pair.Item3,0,0,0,.1f);Curve(on,"",typeof(EquipmentSlotFeedback),pair.Item3,1,1,0,.1f);
                BoolLayer(ctrl,pair.Item1,pair.Item2,off,on);
            }
            var rest=Clip(slot.name+"_Rest");var drift=Clip(slot.name+"_Float");
            Curve(rest,"",typeof(EquipmentSlotFeedback),"floatOffset",0,0,0,.1f);
            var curve=new AnimationCurve();
            float phase=slot.transform.GetSiblingIndex()*.73f;
            for(int i=0;i<=32;i++){float t=i/32f*Mathf.PI*2/1.25f;curve.AddKey(new Keyframe(t,Mathf.Sin(t*1.25f+phase)*3,Mathf.Cos(t*1.25f+phase)*3.75f,Mathf.Cos(t*1.25f+phase)*3.75f));}
            AnimationUtility.SetEditorCurve(drift,EditorCurveBinding.FloatCurve("",typeof(EquipmentSlotFeedback),"floatOffset"),curve);
            var settings=AnimationUtility.GetAnimationClipSettings(drift);settings.loopTime=true;AnimationUtility.SetAnimationClipSettings(drift,settings);
            BoolLayer(ctrl,"Floating","Occupied",rest,drift,.15f);
            PulseLayer(ctrl,slot.name,typeof(EquipmentSlotFeedback),"resultPulse",1/1.7f,1);
            slot.TryGetComponent<Button>(out var button);button.transition=Selectable.Transition.None;
        }
        private static void BakeGauge(UiGaugeMotion gauge)
        {
            var value=Clip(gauge.name+"_ValueChange");Curve(value,"",typeof(UiGaugeMotion),"valueProgress",0,1,0,.3f);
            var animator=Controller(gauge.gameObject,gauge.name+"_Gauge",new Dictionary<string,AnimationClip>{{"ValueChange",value}});
            Ref(gauge,"animator",animator);var ctrl=(AnimatorController)animator.runtimeAnimatorController;
            PulseLayer(ctrl,gauge.name,typeof(UiGaugeMotion),"resultPulse",1/1.4f,.75f);
            var normal=Clip(gauge.name+"_Healthy");Curve(normal,"",typeof(UiGaugeMotion),"warningOpacity",1,1,0,.1f);
            var low=Clip(gauge.name+"_Low");
            AnimationUtility.SetEditorCurve(low,EditorCurveBinding.FloatCurve("",typeof(UiGaugeMotion),"warningOpacity"),new AnimationCurve(new Keyframe(0,1),new Keyframe(.785f,.64f),new Keyframe(1.57f,1)));
            var settings=AnimationUtility.GetAnimationClipSettings(low);settings.loopTime=true;AnimationUtility.SetAnimationClipSettings(low,settings);
            BoolLayer(ctrl,"Warning","Low",normal,low,.12f);
        }
        private static void BakeBlur(UiBackdropBlur blur)
        {
            var image=Field<RawImage>(blur,"backdrop");var path=Path(image.transform,blur.transform);
            var hidden=Clip("BackdropHidden");var visible=Clip("BackdropVisible");
            Curve(hidden,path,typeof(RawImage),"m_Color.a",0,0,0,.12f);Curve(visible,path,typeof(RawImage),"m_Color.a",1,1,0,.18f);
            AnimationUtility.SetAnimationEvents(hidden,new[]{new AnimationEvent{time=.12f,functionName="OnHidden"}});
            Ref(blur,"animator",Controller(blur.gameObject,"Backdrop",new Dictionary<string,AnimationClip>{{"Hidden",hidden},{"Visible",visible}}));
        }
    }
}
