using Portfolio.UI;
using UnityEditor;
using UnityEngine;

public static class PortfolioUiArtBake
{
    [MenuItem("Portfolio/UI/Bake Authored Art Into Existing Canvas")]
    public static void Bake()
    {
        foreach(var name in new[]{"button_dark","button_gold","tooltip_panel"})
        {
            string path="Assets/Resources/Art/ui/"+name+".png";var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.spritePixelsPerUnit=100;importer.spriteBorder=name=="tooltip_panel"?new Vector4(50,42,50,48):new Vector4(60,26,60,26);
            importer.SaveAndReimport();
        }
        System.IO.Directory.CreateDirectory("Assets/Resources/UI/Skin");AssetDatabase.Refresh();
        // Metadata-only crop excludes the stray fragment in the supplied tooltip sheet.
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Art/ui/tooltip_panel.png");
        var slice=Sprite.Create(texture,new Rect(20,30,texture.width-27,texture.height-43),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(38,30,38,37));
        const string skinPath="Assets/Resources/UI/Skin/tooltip_panel.asset";
        var saved=AssetDatabase.LoadAssetAtPath<Sprite>(skinPath);slice.name="Clean tooltip frame";
        if(saved==null)AssetDatabase.CreateAsset(slice,skinPath);else{EditorUtility.CopySerialized(slice,saved);Object.DestroyImmediate(slice);}
        AssetDatabase.SaveAssets();
        GeneratedSkin("ruins_panel_v2","panel_v2",new Vector4(100,100,100,100));
        GeneratedSkin("ruins_button_v2","button_v2",new Vector4(95,60,95,60));
        MenuPortrait("hero_movement/idle_down","hero_ranged_v2");
        MenuPortrait("shield/down_idle_0","hero_shield_v2");
        var root=PrefabUtility.LoadPrefabContents(UiPrefabBuilder.Path);
        try{UiPresentationStyle.Apply(root.GetComponent<PortfolioCanvasUI>());PrefabUtility.SaveAsPrefabAsset(root,UiPrefabBuilder.Path);}
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();Debug.Log("PORTFOLIO_AUTHORED_UI_BAKED");
    }
    static void GeneratedSkin(string source,string name,Vector4 border)
    {
        string path="Assets/Resources/UI/Generated/"+source+".png";
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.spritePixelsPerUnit=100;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;
        importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;importer.filterMode=FilterMode.Bilinear;
        importer.isReadable=true;importer.SaveAndReimport();
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);var pixels=texture.GetPixels32();
        int minX=texture.width,minY=texture.height,maxX=-1,maxY=-1;
        for(int y=0;y<texture.height;y++)for(int x=0;x<texture.width;x++)if(pixels[y*texture.width+x].a>64)
        {minX=Mathf.Min(x,minX);minY=Mathf.Min(y,minY);maxX=Mathf.Max(x,maxX);maxY=Mathf.Max(y,maxY);}
        if(maxX<minX)throw new System.Exception("Generated UI has no visible pixels: "+source);
        var sprite=Sprite.Create(texture,new Rect(minX,minY,maxX-minX+1,maxY-minY+1),Vector2.one*.5f,100,0,SpriteMeshType.FullRect,border);
        sprite.name=name;string target="Assets/Resources/UI/Skin/"+name+".asset";
        var existing=AssetDatabase.LoadAssetAtPath<Sprite>(target);
        if(existing==null)AssetDatabase.CreateAsset(sprite,target);else{EditorUtility.CopySerialized(sprite,existing);Object.DestroyImmediate(sprite);}
        AssetDatabase.SaveAssets();importer.isReadable=false;importer.SaveAndReimport();
    }
    static void MenuPortrait(string source,string name)
    {
        string path="Assets/Resources/Art/"+source+".png";
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        // Separate sprite metadata removes animation-sheet padding only for menu portraits.
        // Original texture, world sprite pivot and animation bounds are untouched.
        var readable=new Texture2D(2,2);readable.LoadImage(System.IO.File.ReadAllBytes(path));
        var pixels=readable.GetPixels32();int minX=readable.width,minY=readable.height,maxX=-1,maxY=-1;
        for(int y=0;y<readable.height;y++)for(int x=0;x<readable.width;x++)if(pixels[y*readable.width+x].a>64)
        {minX=Mathf.Min(x,minX);minY=Mathf.Min(y,minY);maxX=Mathf.Max(x,maxX);maxY=Mathf.Max(y,maxY);}
        float sx=(float)texture.width/readable.width,sy=(float)texture.height/readable.height;
        var sprite=Sprite.Create(texture,new Rect(minX*sx,minY*sy,(maxX-minX+1)*sx,(maxY-minY+1)*sy),Vector2.one*.5f,100);Object.DestroyImmediate(readable);
        sprite.name=name;string target="Assets/Resources/UI/Skin/"+name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Sprite>(target);
        if(existing==null)AssetDatabase.CreateAsset(sprite,target);else{EditorUtility.CopySerialized(sprite,existing);Object.DestroyImmediate(sprite);}
        AssetDatabase.SaveAssets();
    }
    public static void Run(){Bake();PortfolioSmokeTest.Run();}
}
