using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[Serializable] public class SpriteCatalog { public SpriteEntry[] entries; }
[Serializable] public class SpriteEntry { public string group; public string name; public float[] unity_pivot; }

public class PortfolioSpriteImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Resources/Art/")) return;
        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 1;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048;
        importer.filterMode = FilterMode.Bilinear;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = new Vector2(.5f, .5f);
        var catalogPath = "Assets/Editor/SpriteCatalog.json";
        if (File.Exists(catalogPath))
        {
            var catalog = JsonUtility.FromJson<SpriteCatalog>("{\"entries\":" + File.ReadAllText(catalogPath) + "}");
            foreach (var entry in catalog.entries)
                if (assetPath == "Assets/Resources/Art/" + entry.group + "/" + entry.name + ".png")
                    settings.spritePivot = new Vector2(entry.unity_pivot[0], entry.unity_pivot[1]);
        }
        if(assetPath.StartsWith("Assets/Resources/Art/elite/"))settings.spritePivot=new Vector2(.5f,72f/512);
        if(assetPath.StartsWith("Assets/Resources/Art/shield/"))settings.spritePivot=new Vector2(.5f,80f/512);
        if(assetPath.StartsWith("Assets/Resources/Art/sentry/") && !assetPath.EndsWith("/bolt.png"))settings.spritePivot=new Vector2(.5f,92f/512);
        importer.SetTextureSettings(settings);
        if(assetPath=="Assets/Resources/Art/ui/button_dark.png" || assetPath=="Assets/Resources/Art/ui/button_gold.png" || assetPath=="Assets/Resources/Art/ui/tooltip_panel.png")
            importer.spritePixelsPerUnit=100;
    }
}

public static class PortfolioSetup
{
    [MenuItem("Portfolio/Create Playable Scene")]
    public static void CreateScene()
    {
        UiPrefabBuilder.Ensure();
        Directory.CreateDirectory("Assets/Scenes");
        const string path = "Assets/Scenes/Ruins.unity";
        if (!File.Exists(path))
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Ruins Game");
            root.AddComponent<RuinsGame>();
            EditorSceneManager.SaveScene(scene, path);
        }
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(path, true) };
        PlayerSettings.companyName = "Portfolio";
        PlayerSettings.productName = "Ruins Portfolio";
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        AssetDatabase.SaveAssets();
        Debug.Log("PORTFOLIO_SETUP_OK: open Assets/Scenes/Ruins.unity and press Play.");
    }
}
