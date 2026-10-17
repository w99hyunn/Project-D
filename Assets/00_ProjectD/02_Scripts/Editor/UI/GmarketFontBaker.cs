using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace DoHwa.Editor
{
    public static class GmarketFontBaker
    {
        private static readonly string FontDirectory = "Assets/Plugins/TextMesh Pro/Fonts/";
        private static readonly string AssetDirectory = "Assets/Plugins/TextMesh Pro/Resources/Fonts & Materials/";

        [MenuItem("Tools/Project D/Bake Gmarket Light and Bold")]
        public static void BakeLightAndBold()
        {
            var characters = Enumerable.Range(32, 95)
                .Concat(Enumerable.Range(44032, 11172))
                .Concat(Enumerable.Range(12593, 51))
                .Concat(Enumerable.Range(8200, 1701))
                .Select(value => (uint)value)
                .ToArray();

            Bake("Light", characters);
            Bake("Bold", characters);
            AssetDatabase.SaveAssets();
        }

        public static void ApplyBoldTitles()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.name != "Ingame")
                throw new InvalidOperationException("Ingame scene must be active.");

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDirectory + "GmarketSansBold SDF.asset");
            var root = GameObject.Find("RpgCanvas").transform;
            foreach (var path in new[]
                     {
                         "Inventory/Header/Title",
                         "Map/Workspace/Header/Title",
                         "Journal/Workspace/Header/Title",
                         "Settings/Header/Title",
                         "Character/Header/Title"
                     })
            {
                var titleTransform = root.Find(path);
                titleTransform.TryGetComponent<TMP_Text>(out var title);
                title.font = font;
                title.fontSharedMaterial = font.material;
                title.fontStyle = FontStyles.Normal;
                title.fontWeight = FontWeight.Bold;
                EditorUtility.SetDirty(title);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void Bake(string weight, uint[] characters)
        {
            var name = "GmarketSans" + weight;
            var source = AssetDatabase.LoadAssetAtPath<Font>(FontDirectory + name + ".otf");
            var path = AssetDirectory + name + " SDF.asset";
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path) != null)
                throw new InvalidOperationException("Font asset already exists: " + path);

            var font = TMP_FontAsset.CreateFontAsset(source, 36, 4, GlyphRenderMode.SDF16, 2048, 2048, AtlasPopulationMode.Dynamic, true);
            font.name = name + " SDF";
            font.TryAddCharacters(characters, out uint[] missing);
            font.atlasPopulationMode = AtlasPopulationMode.Static;

            AssetDatabase.CreateAsset(font, path);
            AssetDatabase.AddObjectToAsset(font.material, font);
            for (var index = 0; index < font.atlasTextureCount; index++)
            {
                var atlas = font.atlasTextures[index];
                atlas.name = font.name + " Atlas " + index;
                AssetDatabase.AddObjectToAsset(atlas, font);
            }

            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssets();
            Debug.Log($"{font.name}: {font.characterTable.Count} characters, {missing.Length} missing, {font.atlasTextureCount} atlases");
        }
    }
}
