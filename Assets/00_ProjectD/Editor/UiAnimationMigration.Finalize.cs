using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace DoHwa.UI.Editor
{
    public static partial class UiAnimationMigration
    {
        public static string FinalizeAuthoredAssets()
        {
            foreach(var name in new[]{"CancelButton","ConfirmButton"})
            {
                var path="Assets/00_ProjectD/04_UI/Modules/"+name+".prefab";
                var root=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    foreach(var module in root.GetComponentsInChildren<UiModuleButton>(true))
                    {
                        ButtonMotion(module.gameObject,Field<Image>(module,"surface"),Field<Image>(module,"iconMedallion").rectTransform,1.06f,.96f);
                        var so=new SerializedObject(module);so.FindProperty("authoredMotion").boolValue=true;so.ApplyModifiedPropertiesWithoutUndo();
                    }
                    PrefabUtility.SaveAsPrefabAsset(root,path);
                }
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            foreach(var id in AssetDatabase.FindAssets("t:AnimationClip",new[]{"Assets/00_ProjectD/04_UI/Animations"}))
            {
                var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(id));
                if(clip.name.EndsWith("_Close") && clip.name!="CraftResult_Close")
                    AnimationUtility.SetAnimationEvents(clip,new[]{new AnimationEvent{time=clip.length,functionName="OnClosed"}});
            }
            AssetDatabase.SaveAssets();
            return "Reusable button prefabs and panel close Animation Events saved";
        }
    }
}
