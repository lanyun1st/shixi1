using System;
using System.Collections.Generic;
using System.Reflection;
using Portfolio.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class PortfolioSkillIconChecks
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    static int checks;
    static void Check(bool valid,string detail){checks++;if(!valid)throw new Exception("Skill icons: "+detail);}
    static void Capture(RuinsGame game,string name)
    {typeof(PortfolioPresentationChecks).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{game,"SkillIconReview/"+name+".png",1280,720,true});}
    public static void Run(RuinsGame game)
    {
        checks=0;System.IO.Directory.CreateDirectory("SkillIconReview");
        var unique=new HashSet<Sprite>();
        for(int role=0;role<2;role++)for(int id=0;id<9;id++)
        {
            var sprite=UiText.UpgradeIcon(id,role==1);Check(sprite!=null,"asset present "+role+"/"+id);
            Check(unique.Add(sprite),"each of 18 upgrades has its own art");
            Check(sprite.texture.width<=256 && sprite.texture.height<=256,"bounded UI texture size");
            var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sprite));
            Check(importer.alphaIsTransparency && !importer.isReadable && !importer.mipmapEnabled,"alpha and memory import settings");
            Check(UiText.UpgradeIcon(id,role==1)==sprite,"cached lookup returns same sprite");
        }
        Check(UiText.UpgradeIcon(-1)==null && UiText.UpgradeIcon(9,true)==null,"invalid IDs return no misleading icon");
        var ui=(PortfolioCanvasUI)typeof(RuinsGame).GetField("canvasUI",Flags).GetValue(game);
        var state=game.CaptureUiState();state.upgradeMask=511;state.upgrade=0;state.rewardTitle="选择流派组件";state.rewardHint="每个强化拥有独立图标 · 两名角色分别设计";
        for(int role=0;role<2;role++)
        {
            state.shieldHero=role==1;state.mode=UiMode.Upgrade;ui.Render(state,new[]{0,3,5});
            for(int i=0;i<3;i++)Check(ui.cards[i].icon.sprite==UiText.UpgradeIcon(new[]{0,3,5}[i],role==1),"card role mapping");
            Capture(game,role==0?"RangedCards":"ShieldCards");
            state.mode=UiMode.Playing;ui.Render(state,new[]{0,3,5});
            var strip=ui.hud.GetComponentInChildren<LoadoutStripView>(true);
            for(int i=0;i<9;i++)Check(strip.icons[i].sprite==UiText.UpgradeIcon(i,role==1),"HUD matches card catalog");
            Check(ui.dash.transform.Find("Dash Icon").GetComponent<Image>().sprite==SkillIconCatalog.Dash(role==1),"dash role icon");
            Capture(game,role==0?"RangedHud":"ShieldHud");
        }
        state.mode=UiMode.Upgrade;state.shieldHero=false;ui.Render(state,new[]{0,3,5});state.shieldHero=true;ui.Render(state,new[]{0,3,5});
        Check(ui.cards[0].icon.sprite==UiText.UpgradeIcon(0,true),"role switch refreshes unchanged skill IDs");
        CatalogPreview(game,ui);
        typeof(RuinsGame).GetMethod("StartRun",Flags).Invoke(game,null);
        Debug.Log("PORTFOLIO_SKILL_ICONS_OK: "+checks+" checks; 18 unique art assets, bounded textures, role mapping, card/HUD agreement.");
    }
    static void CatalogPreview(RuinsGame game,PortfolioCanvasUI ui)
    {
        var root=new GameObject("Icon catalog review",typeof(RectTransform),typeof(Image));root.transform.SetParent(ui.transform,false);
        var r=(RectTransform)root.transform;r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.sizeDelta=new Vector2(1280,720);
        root.GetComponent<Image>().color=UiArtSkin.Ink;root.GetComponent<Image>().raycastTarget=false;
        try
        {
            UiArtSkin.Label(root.transform,"Title",48,22,1184,42,28,UiArtSkin.Paper).text="技能图标  /  18 项独立设计";
            UiArtSkin.Label(root.transform,"Hint",48,66,1184,24,13,UiArtSkin.Muted).text="大图为强化卡展示尺寸；下方小图为战斗栏辨识检查";
            for(int role=0;role<2;role++)
            {
                int top=role==0?110:394;
                UiArtSkin.Label(root.transform,"Role "+role,48,top,1184,28,18,UiArtSkin.Gold).text=role==0?"修复师 · 远程与元素":"盾卫 · 格挡、重锤与冲阵";
                for(int id=0;id<9;id++)
                {
                    int x=48+id*132;
                    var icon=UiArtSkin.Plate(root.transform,"Icon "+role+" "+id,x+22,top+48,76,76,Color.white);icon.sprite=UiText.UpgradeIcon(id,role==1);icon.preserveAspect=true;
                    UiArtSkin.Label(root.transform,"Name "+role+" "+id,x,top+136,124,28,13,UiArtSkin.Paper,TextAnchor.MiddleCenter).text=UiText.UpgradeName(id,role==1);
                    var small=UiArtSkin.Plate(root.transform,"Small "+role+" "+id,x+46,top+180,28,28,Color.white);small.sprite=icon.sprite;small.preserveAspect=true;
                }
            }
            Capture(game,"Catalog");
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
}
