using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
namespace DoHwa.UI.Editor
{
    public static partial class UiAnimationMigration
    {
        public static string AuthorEquipmentTransforms()
        {
            var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/00_ProjectD/01_Scenes/Production/Ingame.unity",UnityEditor.SceneManagement.OpenSceneMode.Additive);
            try
            {
                foreach(var slot in scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<EquipmentSlotFeedback>(true)))
                {
                    slot.TryGetComponent<Animator>(out var animator);
                    var controller=(AnimatorController)animator.runtimeAnimatorController;
                    var machine=controller.layers.Single(x=>x.name=="Floating").stateMachine;
                    var original=machine.states.Select(x=>x.state).ToArray();
                    var art=(RectTransform)slot.transform.Find("Art");
                    foreach(var state in original)
                    {
                        foreach(var transition in state.transitions)state.RemoveTransition(transition);
                        var clip=(AnimationClip)state.motion;
                        var binding=AnimationUtility.GetCurveBindings(clip).Single();
                        var curve=AnimationUtility.GetEditorCurve(clip,binding);
                        AnimationUtility.SetEditorCurve(clip,binding,null);
                        var keys=curve.keys;for(int i=0;i<keys.Length;i++)keys[i].value+=art.anchoredPosition.y;curve.keys=keys;
                        var target=EditorCurveBinding.FloatCurve("Art",typeof(RectTransform),"m_AnchoredPosition.y");
                        AnimationUtility.SetEditorCurve(clip,target,curve);
                        var pressed=UnityEngine.Object.Instantiate(clip);pressed.name=clip.name+"_Pressed";
                        AssetDatabase.CreateAsset(pressed,AssetDatabase.GenerateUniqueAssetPath(folder+"/"+pressed.name+".anim"));
                        keys=curve.keys;for(int i=0;i<keys.Length;i++)keys[i].value-=2;curve.keys=keys;
                        AnimationUtility.SetEditorCurve(pressed,target,curve);
                        State(machine,state.name+"Pressed",pressed);
                    }
                    foreach(var state in machine.states)
                    {
                        bool occupied=state.state.name.StartsWith("On");bool pressed=state.state.name.EndsWith("Pressed");
                        var transition=machine.AddAnyStateTransition(state.state);transition.hasExitTime=false;transition.canTransitionToSelf=false;transition.duration=.1f;
                        transition.AddCondition(occupied?AnimatorConditionMode.If:AnimatorConditionMode.IfNot,0,"Occupied");
                        transition.AddCondition(pressed?AnimatorConditionMode.If:AnimatorConditionMode.IfNot,0,"SlotPressed");
                    }
                }
                AssetDatabase.SaveAssets();
            }
            finally{UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);}
            return "Equipment floating and press position now animate RectTransform directly";
        }
    }
}
