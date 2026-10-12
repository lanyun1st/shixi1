using System;
using System.Collections;
using System.IO;
using System.Reflection;
using Portfolio.Presentation;
using Portfolio.UI;
using UnityEngine;
using UnityEngine.UI;

public static class PortfolioPresentationChecks
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static object Get(object o,string n)=>o.GetType().GetField(n,Flags).GetValue(o);
    static int checks;
    static void Check(bool value,string label){checks++;if(!value)throw new Exception("Presentation: "+label);}
    public static void Run(RuinsGame game)
    {
        checks=0;Directory.CreateDirectory("PresentationReview");
        typeof(RuinsGame).GetField("player",Flags).SetValue(game,new Vector2(1500,1180));
        typeof(RuinsGame).GetField("cameraFocus",Flags).SetValue(game,new Vector2(1600,1120));
        typeof(RuinsGame).GetMethod("TickFeedback",Flags).Invoke(game,new object[]{0f});
        typeof(RuinsGame).GetMethod("AnimatePlayer",Flags).Invoke(game,null);
        var ui=UnityEngine.Object.FindObjectOfType<PortfolioCanvasUI>();var state=game.CaptureUiState();
        state.mode=UiMode.Playing;state.room=3;state.regionName="中央 · 行军大道";state.upgrade=3;state.upgradeMask=9;state.routeName="雷火过载 · 进阶";
        state.playerPosition=new Vector2(1600,1120);state.cameraPosition=state.playerPosition;state.cameraSize=new Vector2(1422,800);
        state.enemyPositions=new[]{new Vector2(1200,800),new Vector2(1800,900),new Vector2(2000,1300),new Vector2(2200,1500)};
        state.enemyKinds=new[]{0,1,2,3};state.enemyPositionCount=4;
        state.mapObstacles=new[]{new Rect(1000,600,80,80),new Rect(1800,1300,100,60)};state.mapObstacleCount=2;
        ui.Render(state,new[]{0,1,3});Canvas.ForceUpdateCanvases();
        var map=ui.hud.transform.Find("World navigation/Map surface");
        Check(map!=null,"map surface exists");
        var player=(RectTransform)map.Find("Player");Check(player.anchoredPosition.sqrMagnitude<.001f,"world center maps to map center");
        Check(map.Find("Obstacle 0").gameObject.activeSelf && !map.Find("Obstacle 2").gameObject.activeSelf,"obstacle snapshot count");
        Check(((RectTransform)map.Find("Enemy 2")).sizeDelta.x==7,"elite marker differentiated");
        foreach(var graphic in ui.hud.transform.Find("World navigation").GetComponentsInChildren<Graphic>())Check(!graphic.raycastTarget,"minimap cannot block attacks");
        state.cameraPosition=Vector2.zero;ui.Render(state,new[]{0,1,3});
        var viewport=(RectTransform)map.Find("Visible area");var mapRect=(RectTransform)map;
        Check(Mathf.Abs(viewport.anchoredPosition.x)+viewport.sizeDelta.x/2<=mapRect.sizeDelta.x/2+.01f,"viewport stays within map");
        state.cameraPosition=state.playerPosition;
        foreach(var mode in new[]{UiMode.Playing,UiMode.Title,UiMode.Upgrade,UiMode.Paused,UiMode.Result})
        {state.mode=mode;state.won=true;ui.Render(state,new[]{0,1,3});Capture(game,"PresentationReview/"+mode+".png",1280,720,true);}
        state.mode=UiMode.Playing;ui.Render(state,new[]{0,1,3});Capture(game,"PresentationReview/Hud_4x3.png",1024,768,true);
        var feedback=(ReactionFeedback)Get(game,"reactionFeedback");feedback.Clear();
        var cue=new ReactionCue(ReactionKind.Chain,new Vector2(1520,1100),new Vector2(1780,1100));feedback.Play(cue);
        var effects=(IList)Get(feedback,"active");var effect=effects[0];
        var core=(LineRenderer)Get(effect,"sparks");var branches=(LineRenderer[])Get(effect,"branches");
        Check(core.positionCount==25 && branches.Length==3 && branches[0].positionCount==7,"main trunk and branches");
        Check(Vector3.Distance(core.GetPosition(0),new Vector3(1520,-380,0))<.01f && Vector3.Distance(core.GetPosition(24),new Vector3(1780,-380,0))<.01f,"electric endpoints pinned to real hit");
        var initial=core.GetPosition(12);feedback.Tick(.025f);Capture(game,"PresentationReview/Arc_First.png",1280,720,false);
        feedback.Tick(.045f);float dim=core.startColor.a;Capture(game,"PresentationReview/Arc_Gap.png",1280,720,false);
        feedback.Tick(.04f);Check(core.startColor.a>dim,"second discharge brighter than gap");Check(core.GetPosition(12)!=initial,"arc topology changes");Capture(game,"PresentationReview/Arc_Second.png",1280,720,false);
        feedback.Tick(.2f);Check(feedback.Active==0 && branches[0].positionCount==0,"branches reset on pool return");
        Check(Resources.Load<Shader>("Map/ReactionArc").isSupported,"arc shader compiled");
        ui.Render(game.CaptureUiState(),new[]{0,1,3});
        File.WriteAllText("PresentationReview/checks.txt","PASS "+checks+" presentation checks; five UI states, 4:3 HUD, three arc phases. Fixed fixtures, not a manual playthrough or FPS benchmark.");
        Debug.Log("PORTFOLIO_PRESENTATION_OK: "+checks+" checks");
    }
    static void Capture(RuinsGame game,string path,int width,int height,bool withUi)
    {
        var camera=game.GetComponentInChildren<Camera>();var canvas=UnityEngine.Object.FindObjectOfType<PortfolioCanvasUI>().GetComponent<Canvas>();
        var previous=camera.targetTexture;var active=RenderTexture.active;var mode=canvas.renderMode;var previousCamera=canvas.worldCamera;bool visible=canvas.enabled;float aspect=camera.aspect;
        var rt=new RenderTexture(width,height,24);rt.Create();var texture=new Texture2D(width,height,TextureFormat.RGBA32,false);
        try
        {
            camera.targetTexture=rt;camera.aspect=width/(float)height;canvas.enabled=withUi;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
            Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());
        }
        finally{camera.targetTexture=previous;camera.aspect=aspect;RenderTexture.active=active;canvas.enabled=visible;canvas.renderMode=mode;canvas.worldCamera=previousCamera;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(texture);}
    }
}
