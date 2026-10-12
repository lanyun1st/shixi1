using UnityEditor;
using UnityEngine;

// Fixed settings for the new UI-only icons. Does not touch world/animation textures.
public sealed class SkillIconImporter:AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if(!assetPath.StartsWith("Assets/Resources/UI/SkillIcons/"))return;
        var importer=(TextureImporter)assetImporter;
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.spritePixelsPerUnit=100;importer.alphaIsTransparency=true;
        importer.mipmapEnabled=false;importer.isReadable=false;importer.maxTextureSize=256;
        importer.textureCompression=TextureImporterCompression.Uncompressed;importer.filterMode=FilterMode.Bilinear;
    }
}
