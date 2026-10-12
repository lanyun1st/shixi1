using System;
using System.Collections;
using System.IO;
using System.Reflection;
using Portfolio.UI;
using Portfolio.Domain;
using UnityEngine;
using UnityEngine.UI;

public static class PortfolioPresentationPolishChecks
{
    const BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    static object Get(object o,string key)=>o.GetType().GetField(key,F).GetValue(o);
    static void Set(object o,string key,object value)=>o.GetType().GetField(key,F).SetValue(o,value);
    static object Call(object o,string key,params object[] args)=>o.GetType().GetMethod(key,F).Invoke(o,args);
    static int checks;
    static void Check(bool pass,string message){checks++;if(!pass)throw new Exception("Presentation polish: "+message);}
    static void Capture(RuinsGame game,string name,bool ui)
    {typeof(PortfolioPresentationChecks).GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{game,"PresentationPolishReview/"+name+".png",1280,720,ui});}
    public static void Run(RuinsGame game)
    {
        checks=0;Directory.CreateDirectory("PresentationPolishReview");Call(game,"StartRun");Call(game,"ClearActors");
        var frames=Resources.LoadAll<Sprite>("Art/elite/walk_cycle");Check(frames.Length==16,"16 walk sprites imported");
        foreach(var frame in frames)Check(frame.pivot.y>=0 && frame.pivot.y<frame.rect.height*.3f,"foot anchor "+frame.name);
        var directions=new[]{Vector2.down,Vector2.up,Vector2.left,Vector2.right};
        foreach(var direction in directions)
        {
            var go=new GameObject("Walk verification");var renderer=go.AddComponent<SpriteRenderer>();
            var actor=new EliteGuardActor(renderer,key=>Resources.Load<Sprite>("Art/"+key),(p,d,r)=>p+d);
            try
            {
                var p=new Vector2(1600,1100);var seen=new System.Collections.Generic.HashSet<string>();
                Set(game,"cameraFocus",p);Call(game,"TickFeedback",0f);
                for(int i=0;i<4;i++){p=actor.Tick(.21f,p,p+direction*600,1);seen.Add(renderer.sprite.name);Capture(game,"Walk_"+direction+"_"+i,false);}
                Check(seen.Count==4,"four distinct walk frames "+direction);Check(actor.Brain.State==EliteChargeBrain.Mode.Chase,"walk does not alter brain");
                actor.Tick(.6f,p,p+direction*600,1);Check(renderer.sprite.name.EndsWith("windup"),"windup overrides walking");
            }
            finally{actor.Dispose();UnityEngine.Object.DestroyImmediate(go);}
        }
        var blockedGo=new GameObject("Blocked walk");var blockedView=blockedGo.AddComponent<SpriteRenderer>();var blockedActor=new EliteGuardActor(blockedView,key=>Resources.Load<Sprite>("Art/"+key),(p,d,r)=>p);
        blockedActor.Tick(.2f,Vector2.zero,Vector2.right,1);Check(blockedView.sprite.name=="right_idle","blocked actor does not walk in place");blockedActor.Dispose();UnityEngine.Object.DestroyImmediate(blockedGo);
        Call(game,"DrawSpawnMarker",new Vector2(1780,1080),30f);
        var lines=(IList)Get(game,"skillLines");Check(lines.Count==2,"two persistent cross strokes without grit");
        foreach(var entry in lines){var line=(LineRenderer)Get(entry,"view");Check(line.positionCount<=4 && !line.loop,"no circular spawn warning");}
        Set(game,"cameraFocus",new Vector2(1600,1100));Call(game,"TickFeedback",0f);Capture(game,"SpawnFissures",false);Call(game,"ClearActors");
        var ui=UnityEngine.Object.FindObjectOfType<PortfolioCanvasUI>();
        foreach(var panel in new[]{ui.titleScreen,ui.pauseScreen,ui.upgradeScreen,ui.resultScreen})
        {var frame=panel.transform.Find("Painted stone frame").GetComponent<Image>();Check(frame.sprite!=null && frame.type==Image.Type.Sliced,"painted sliced frame "+panel.name);Check(!frame.raycastTarget,"frame does not steal clicks");}
        var state=game.CaptureUiState();state.mode=UiMode.Upgrade;state.upgradeMask=3;ui.Render(state,new[]{3,2,4});Canvas.ForceUpdateCanvases();Capture(game,"FramedUpgrade",true);
        state.mode=UiMode.Paused;ui.Render(state,new[]{3,2,4});Canvas.ForceUpdateCanvases();Capture(game,"FramedPause",true);
        var cursor=ui.GetComponentInChildren<AimCursorView>(true);var graphic=cursor.GetComponent<AimReticleGraphic>();bool oldVisible=Cursor.visible;
        try
        {
            state.mode=UiMode.Playing;ui.Render(state,new[]{3,2,4});Canvas.ForceUpdateCanvases();
            cursor.Present(true,false,false);cursor.RenderPointer(new Vector2(Screen.width*.6f,Screen.height*.5f),true,true,0);
            Check(graphic.enabled && !graphic.raycastTarget && !Cursor.visible,"combat aim visible, native cursor hidden, no input interception");
            Check(graphic.Kick>0 && cursor.transform.localScale==Vector3.one,"shot expands strokes without scaling center");
            Capture(game,"CombatAim",true);
            cursor.Present(false,false,false);cursor.RenderPointer(Vector2.one,true,false,0);Check(!graphic.enabled && Cursor.visible==oldVisible,"modal restores native cursor");
            cursor.Present(true,true,false);cursor.RenderPointer(Vector2.one,true,false,1);Check(graphic.Melee,"shield aiming distinct");
            cursor.RenderPointer(Vector2.one,false,false,0);Check(!graphic.enabled && Cursor.visible==oldVisible,"focus loss restores native cursor");
            cursor.Present(true,false,true);cursor.RenderPointer(Vector2.one,true,false,0);Check(!graphic.enabled,"HUD hover hides aim");
        }
        finally{cursor.Present(false,false,false);cursor.RenderPointer(Vector2.zero,false,false,0);}
        File.WriteAllText("PresentationPolishReview/checks.txt","PASS "+checks+" checks. Walk frames/pivots, brain priority, blocked movement, fissures, modal frames, cursor focus/pointer/modal restoration. Fixed screenshots are not a manual playthrough.");
        Debug.Log("PORTFOLIO_PRESENTATION_POLISH_OK: "+checks+" checks");Call(game,"StartRun");
    }
}
