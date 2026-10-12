using System.IO;
using UnityEditor;
using UnityEngine;

// Restricted to the three authored assets in this presentation update.
public sealed class PresentationAssetImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        bool walk=assetPath=="Assets/Resources/Art/elite/walk_cycle.png";
        bool frame=assetPath=="Assets/Resources/Art/ui/ruins_modal_frame.png";
        bool cursor=assetPath=="Assets/Resources/Art/ui/aim_reticle.png";
        if(!walk && !frame && !cursor)return;
        var importer=(TextureImporter)assetImporter;
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=walk?SpriteImportMode.Multiple:SpriteImportMode.Single;
        importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.wrapMode=TextureWrapMode.Clamp;
        importer.filterMode=FilterMode.Bilinear;importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.maxTextureSize=2048;importer.npotScale=TextureImporterNPOTScale.None;importer.spritePixelsPerUnit=walk?1:100;
        if(frame)importer.spriteBorder=new Vector4(160,140,160,160);
        if(!walk)return;
        // Inspect alpha within each equal cell to align the feet, without modifying source pixels.
        var texture=new Texture2D(2,2);ImageConversion.LoadImage(texture,File.ReadAllBytes(assetPath));
        try
        {
            var pixels=texture.GetPixels32();var slices=new SpriteMetaData[16];var directions=new[]{"down","up","left","right"};
            for(int row=0;row<4;row++)for(int col=0;col<4;col++)
            {
                int x0=col*texture.width/4,x1=(col+1)*texture.width/4,y0=(3-row)*texture.height/4,y1=(4-row)*texture.height/4;
                int minX=x1,maxX=x0,minY=y1;
                for(int y=y0;y<y1;y++)for(int x=x0;x<x1;x++)if(pixels[y*texture.width+x].a>100){minX=Mathf.Min(minX,x);maxX=Mathf.Max(maxX,x);minY=Mathf.Min(minY,y);}
                slices[row*4+col]=new SpriteMetaData{name="walk_"+directions[row]+"_"+col,rect=new Rect(x0,y0,x1-x0,y1-y0),alignment=9,
                    pivot=new Vector2(((minX+maxX)*.5f-x0)/(x1-x0),(minY-y0+3f)/(y1-y0))};
            }
            // Unity 2022 compatibility path: this project intentionally has no 2D Sprite editor package.
            // The 16 imported subassets are verified by PortfolioPresentationPolishChecks.
#pragma warning disable CS0618
            importer.spritesheet=slices;
#pragma warning restore CS0618
        }
        finally{Object.DestroyImmediate(texture);}
    }
}
