using System;
using System.Reflection;
using Portfolio.UI;
using UnityEngine;
using UnityEngine.UI;

public static class PortfolioReticleChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    static int checks;
    static void Check(bool ok,string why){checks++;if(!ok)throw new Exception("Reticle: "+why);}
    static void Capture(RuinsGame game,string name)=>typeof(PortfolioPresentationChecks).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{game,"ReticleReview/"+name+".png",1280,720,true});
    public static void Run(RuinsGame game)
    {
        checks=0;System.IO.Directory.CreateDirectory("ReticleReview");
        var ui=(PortfolioCanvasUI)typeof(RuinsGame).GetField("canvasUI",F).GetValue(game);
        var cursor=ui.GetComponentInChildren<AimCursorView>(true);var graphic=cursor.GetComponent<AimReticleGraphic>();
        bool native=Cursor.visible;Vector2 point=new Vector2(Screen.width*.5f,Screen.height*.5f);
        var state=game.CaptureUiState();state.mode=UiMode.Playing;ui.Render(state,new[]{0,1,2});
        try
        {
            cursor.Present(false,false,false,0);cursor.RenderPointer(point,true,false,0);
            cursor.Present(true,false,false,0);cursor.RenderPointer(point,true,false,0);
            Check(graphic.enabled && !graphic.raycastTarget,"visible without blocking input");
            Check(graphic.Kick==0,"entering combat does not simulate a shot");
            cursor.Present(true,false,false,3);cursor.RenderPointer(point,true,false,0);
            Check(graphic.Kick==1,"accepted multishot pulses once");
            cursor.RenderPointer(point,true,false,.2f);Check(graphic.Kick==0,"pulse settles");
            cursor.Present(true,false,false,3);cursor.RenderPointer(point,true,false,0);Check(graphic.Kick==0,"unchanged shot count does not pulse");
            Check(cursor.transform.localScale==Vector3.one && cursor.transform.localRotation==Quaternion.identity,"center never scales or rotates");
            var canvas=ui.GetComponent<Canvas>();var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
            Check(Vector2.Distance(RectTransformUtility.WorldToScreenPoint(camera,cursor.transform.position),point)<.1f,"center matches mouse position");
            cursor.Present(true,false,true,4);cursor.RenderPointer(point,true,false,0);Check(!graphic.enabled && Cursor.visible==native,"HUD hover restores native pointer");
            cursor.Present(true,true,false,4);cursor.RenderPointer(point,true,false,0);Check(graphic.Melee && graphic.Kick==0,"role switch resets pulse and shape");
            cursor.RenderPointer(new Vector2(-1,-1),true,false,0);Check(!graphic.enabled && Cursor.visible==native,"outside window restores pointer");
            cursor.RenderPointer(point,false,false,0);Check(!graphic.enabled && Cursor.visible==native,"focus loss restores pointer");
            cursor.Present(true,false,false,4,true,.65f);cursor.RenderPointer(point,true,false,.2f);Capture(game,"CombatCharged");
            cursor.Present(false,false,false);cursor.RenderPointer(point,true,false,0);
            Check(!graphic.enabled && Cursor.visible==native,"modal restores pointer");
            Preview(game,ui);
        }
        finally{cursor.Present(false,false,false);cursor.RenderPointer(point,false,false,0);}
        Debug.Log("PORTFOLIO_RETICLE_OK: "+checks+" checks; accepted-shot pulse, pointer mapping, role shape, cursor restoration.");
    }
    static void Preview(RuinsGame game,PortfolioCanvasUI ui)
    {
        var board=new GameObject("Reticle review",typeof(RectTransform),typeof(Image));board.transform.SetParent(ui.transform,false);
        var rt=(RectTransform)board.transform;rt.anchorMin=rt.anchorMax=rt.pivot=Vector2.one*.5f;rt.sizeDelta=new Vector2(850,360);board.GetComponent<Image>().color=new Color(.045f,.065f,.075f);
        try
        {
            UiArtSkin.Label(rt,"Title",30,18,790,38,26,UiArtSkin.Paper,TextAnchor.MiddleCenter).text="瞄准标记 · 中心始终对准鼠标";
            string[] labels={"远程 / 常态","远程 / 蓄力","近战 / 指向"};
            for(int i=0;i<3;i++)
            {
                float x=145+i*280;
                UiArtSkin.Label(rt,"Caption"+i,x-110,76,220,28,16,UiArtSkin.Gold,TextAnchor.MiddleCenter).text=labels[i];
                for(int scale=1;scale<=3;scale+=2)
                {
                    var go=new GameObject("Reticle",typeof(RectTransform),typeof(AimReticleGraphic));go.transform.SetParent(rt,false);
                    UiArtSkin.Rect(go.transform,x-16,scale==1?123:216,32,32);
                    var r=(RectTransform)go.transform;r.pivot=Vector2.one*.5f;r.anchoredPosition=new Vector2(x,-(scale==1?138:247));r.localScale=Vector3.one*scale;
                    var g=go.GetComponent<AimReticleGraphic>();g.raycastTarget=false;g.SetState(i==2,0,i==1,.72f);
                }
            }
            UiArtSkin.Label(rt,"Legend",30,306,790,28,13,UiArtSkin.Muted,TextAnchor.MiddleCenter).text="上排：原始尺寸    下排：3 倍细节预览 · 无新增贴图";
            Capture(game,"ReticleStyles");
            foreach(var graphic in board.GetComponentsInChildren<AimReticleGraphic>())
            {
                var mesh=graphic.canvasRenderer.GetMesh();
                Check(mesh!=null && mesh.vertexCount>0,"visible reticle mesh generated");
                Check(!graphic.canvasRenderer.cull && graphic.canvasRenderer.GetAlpha()>0,"reticle renderer visible");
            }
        }
        finally{UnityEngine.Object.DestroyImmediate(board);}
    }
}
