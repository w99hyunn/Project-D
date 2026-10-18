using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
namespace DoHwa.UI.Editor
{
    public static partial class UiAnimationMigration
    {
        private static string CardSignature(AnimatorController controller)
        {
            var text=new StringBuilder();
            foreach(var layer in controller.layers)
            {
                text.Append(layer.name);
                foreach(var s in layer.stateMachine.states)
                {
                    text.Append(s.state.name);var clip=(AnimationClip)s.state.motion;
                    foreach(var binding in AnimationUtility.GetCurveBindings(clip).OrderBy(b=>b.path+b.propertyName))
                    {
                        text.Append(binding.path+binding.type+binding.propertyName);
                        foreach(var key in AnimationUtility.GetEditorCurve(clip,binding).keys)
                            text.Append(key.time.ToString("R")+":"+key.value.ToString("R")+":"+key.inTangent+":"+key.outTangent);
                    }
                }
            }
            return text.ToString();
        }
        public static string ShareCardControllers()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode first");
            var root=GameObject.Find("RpgCanvas");var cards=root.GetComponentsInChildren<RpgItemButton>(true);
            foreach(var card in cards)
            {
                card.TryGetComponent<Animator>(out var animator);
                foreach(var clip in animator.runtimeAnimatorController.animationClips)
                    foreach(var binding in AnimationUtility.GetCurveBindings(clip))
                    {
                        var parts=binding.path.Split('/');
                        if(!parts.Any(x=>x.EndsWith("ResultHighlight")))continue;
                        var curve=AnimationUtility.GetEditorCurve(clip,binding);AnimationUtility.SetEditorCurve(clip,binding,null);
                        var normalized=binding;normalized.path=string.Join("/",parts.Select(x=>x.EndsWith("ResultHighlight")?"ResultHighlight":x));
                        AnimationUtility.SetEditorCurve(clip,normalized,curve);
                    }
                foreach(var child in card.GetComponentsInChildren<Transform>(true).Where(x=>x.name.EndsWith("ResultHighlight")))child.name="ResultHighlight";
            }
            var groups=cards.GroupBy(c=>{c.TryGetComponent<Animator>(out var a);return CardSignature((AnimatorController)a.runtimeAnimatorController);}).ToArray();
            if(groups.Length!=2)throw new Exception("Expected two equivalent card systems, got "+groups.Length);
            var candidates=new HashSet<string>();
            foreach(var group in groups)
            {
                var first=group.First();first.TryGetComponent<Animator>(out var canonical);
                var name=new SerializedObject(first).FindProperty("quick").boolValue?"QuickSlot":"InventoryCard";
                AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(canonical.runtimeAnimatorController),name);
                foreach(var card in group.Skip(1))
                {
                    card.TryGetComponent<Animator>(out var animator);
                    candidates.Add(AssetDatabase.GetAssetPath(animator.runtimeAnimatorController));
                    foreach(var clip in animator.runtimeAnimatorController.animationClips)
                    {
                        var path=AssetDatabase.GetAssetPath(clip);
                        if(System.IO.Path.GetFileName(path).StartsWith("Card_") || System.IO.Path.GetFileName(path).StartsWith("Quick_"))candidates.Add(path);
                    }
                    animator.runtimeAnimatorController=canonical.runtimeAnimatorController;
                }
            }
            AssetDatabase.SaveAssets();UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(root.scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(root.scene);
            File.WriteAllLines("Tools/UIReview/DuplicateCardAnimations.txt",candidates.OrderBy(x=>x));
            return "Shared controllers: "+string.Join(", ",groups.Select(g=>g.Count()))+"; duplicate candidates="+candidates.Count;
        }
        public static string RemoveUnreferencedCardCopies()
        {
            var candidates=new HashSet<string>(File.ReadAllLines("Tools/UIReview/DuplicateCardAnimations.txt"));
            if(candidates.Any(p=>!p.StartsWith(folder+"/") || !(p.EndsWith(".anim") || p.EndsWith(".controller"))))throw new Exception("Invalid cleanup path");
            var roots=AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/") && !candidates.Contains(p) &&
                (p.EndsWith(".unity") || p.EndsWith(".prefab") || p.EndsWith(".asset") || p.EndsWith(".controller") || p.EndsWith(".overrideController"))).ToArray();
            var referenced=new HashSet<string>(AssetDatabase.GetDependencies(roots,true));
            var removable=candidates.Where(p=>!referenced.Contains(p)).ToArray();
            foreach(var path in removable)AssetDatabase.DeleteAsset(path);
            AssetDatabase.SaveAssets();
            return "Removed "+removable.Length+" verified unreferenced generated copies; retained "+(candidates.Count-removable.Length)+" referenced assets";
        }
    }
}

