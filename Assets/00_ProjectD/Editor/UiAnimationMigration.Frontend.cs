using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;
namespace DoHwa.UI.Editor
{
    public static partial class UiAnimationMigration
    {
        public static string MigrateFrontend()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode first");
            var login=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/00_ProjectD/01_Scenes/Production/Login.unity",UnityEditor.SceneManagement.OpenSceneMode.Additive);
            try
            {
                var root=login.GetRootGameObjects().First(x=>x.name=="LoginCanvas");
                foreach(var module in root.GetComponentsInChildren<UiModuleButton>(true))
                {
                    module.ApplyTheme();ButtonMotion(module.gameObject,Field<Image>(module,"surface"),Field<Image>(module,"iconMedallion").rectTransform,1.06f,.96f);
                    var so=new SerializedObject(module);so.FindProperty("authoredMotion").boolValue=true;so.ApplyModifiedPropertiesWithoutUndo();
                }
                foreach(var close in root.GetComponentsInChildren<CloseButtonFeedback>(true))
                {
                    var circle=Field<Image>(close,"circle");ButtonMotion(close.gameObject,circle,circle.rectTransform,1.09f,.90f);
                    UnityEngine.Object.DestroyImmediate(close);
                }
                foreach(var reveal in root.GetComponentsInChildren<UiPanelReveal>(true))BakePanel(reveal);
                foreach(var pulse in root.GetComponentsInChildren<UiIdlePulse>(true))
                {
                    var clip=Clip("LoginPromptPulse");
                    AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve("",typeof(CanvasGroup),"m_Alpha"),
                        new AnimationCurve(new Keyframe(0,1),new Keyframe(Mathf.PI/2.2f,.68f),new Keyframe(Mathf.PI*2/2.2f,1)));
                    var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=true;AnimationUtility.SetAnimationClipSettings(clip,settings);
                    Controller(pulse.gameObject,"LoginPrompt",new Dictionary<string,AnimationClip>{{"Pulse",clip}});
                    UnityEngine.Object.DestroyImmediate(pulse);
                }
                FinishInteractions(root);
                foreach(var legacy in root.GetComponentsInChildren<UiInteractionMotion>(true))
                {
                    var group=Field<CanvasGroup>(legacy,"visualGroup");
                    var states=new Dictionary<string,AnimationClip>();
                    foreach(var name in new[]{"Normal","Highlighted","Pressed","Selected","Disabled"})
                    {
                        var clip=Clip(legacy.name+"_"+name);
                        if(group)Alpha(clip,Path(group.transform,legacy.transform),name=="Disabled"?.42f:1,name=="Disabled"?.42f:1,0,.1f);
                        states.Add(name,clip);
                    }
                    Controller(legacy.gameObject,legacy.name+"_Control",states,true);
                    legacy.TryGetComponent<Selectable>(out var control);control.transition=Selectable.Transition.Animation;control.animationTriggers=new AnimationTriggers();
                    UnityEngine.Object.DestroyImmediate(legacy);
                }
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(login);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(login);
            }
            finally{UnityEditor.SceneManagement.EditorSceneManager.CloseScene(login,true);}
            var splash=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/00_ProjectD/01_Scenes/Production/Splash.unity",UnityEditor.SceneManagement.OpenSceneMode.Additive);
            try
            {
                var owner=splash.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<SplashTransition>(true)).Single();
                var so=new SerializedObject(owner);
                float fade=so.FindProperty("fadeDuration").floatValue;
                float logoHold=so.FindProperty("logoDuration").floatValue;float noticeHold=so.FindProperty("noticeDuration").floatValue;
                var clip=Clip("SplashSequence");
                var logo=Field<CanvasGroup>(owner,"logo");var icons=Field<CanvasGroup>(owner,"ratingIcons");var notice=Field<CanvasGroup>(owner,"ratingNotice");
                float logoEnd=1+fade+logoHold;float ratingStart=logoEnd+fade+1;float ratingEnd=ratingStart+fade+noticeHold;float end=ratingEnd+fade;
                AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(Path(logo.transform,owner.transform),typeof(CanvasGroup),"m_Alpha"),
                    new AnimationCurve(new Keyframe(0,0),new Keyframe(1,0),new Keyframe(1+fade,1),new Keyframe(logoEnd,1),new Keyframe(logoEnd+fade,0),new Keyframe(end,0)));
                foreach(var group in new[]{icons,notice})
                {
                    group.gameObject.SetActive(true);group.alpha=0;
                    AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(Path(group.transform,owner.transform),typeof(CanvasGroup),"m_Alpha"),
                        new AnimationCurve(new Keyframe(0,0),new Keyframe(ratingStart,0),new Keyframe(ratingStart+fade,1),new Keyframe(ratingEnd,1),new Keyframe(end,0)));
                }
                AnimationUtility.SetAnimationEvents(clip,new[]{new AnimationEvent{time=end,functionName="OnSplashFinished"}});
                Ref(owner,"animator",Controller(owner.gameObject,"Splash",new Dictionary<string,AnimationClip>{{"Sequence",clip}}));
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(splash);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(splash);
            }
            finally{UnityEditor.SceneManagement.EditorSceneManager.CloseScene(splash,true);}
            CreateSceneCurtain();
            AssetDatabase.SaveAssets();
            return "Login, Splash and scene transition prefab authored";
        }
        private static void CreateSceneCurtain()
        {
            var go=new GameObject("SceneTransition",typeof(RectTransform),typeof(Canvas),typeof(CanvasGroup),typeof(GraphicRaycaster),typeof(UiSceneTransition));
            try
            {
                go.TryGetComponent<Canvas>(out var canvas);canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=32000;
                go.TryGetComponent<CanvasGroup>(out var group);group.alpha=0;
                var surface=new GameObject("Surface",typeof(RectTransform),typeof(Image));surface.transform.SetParent(go.transform,false);
                var rt=(RectTransform)surface.transform;rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=rt.offsetMax=Vector2.zero;
                surface.TryGetComponent<Image>(out var image);image.color=new Color(.035f,.045f,.065f,1);
                var cover=Clip("SceneCover");Alpha(cover,"",0,1,0,.22f);var reveal=Clip("SceneReveal");Alpha(reveal,"",1,0,0,.28f);
                foreach(var clip in new[]{cover,reveal})AnimationUtility.SetAnimationEvents(clip,new[]{new AnimationEvent{time=clip.length,functionName="OnFadeFinished"}});
                go.TryGetComponent<UiSceneTransition>(out var owner);
                Ref(owner,"animator",Controller(go,"SceneTransition",new Dictionary<string,AnimationClip>{{"Cover",cover},{"Reveal",reveal}}));
                PrefabUtility.SaveAsPrefabAsset(go,"Assets/00_ProjectD/Resources/UI/SceneTransition.prefab");
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }
    }
}
