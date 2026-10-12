using System;
using System.Collections;
using System.IO;
using System.Reflection;
using Portfolio.Domain;
using Portfolio.UI;
using UnityEngine;

// Render actual map edges, not just the central combat area. No player build required.
public static class PortfolioMapChecks
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static object Get(object o,string name)=>o.GetType().GetField(name,Flags).GetValue(o);
    static void Set(object o,string name,object value)=>o.GetType().GetField(name,Flags).SetValue(o,value);
    static object Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name,Flags).Invoke(o,args);
    static int checks;
    static void Check(bool ok,string message){checks++;if(!ok)throw new Exception("Map seam: "+message);}
    public static void Run(RuinsGame game)
    {
        checks=0;Directory.CreateDirectory("MapReview");
        var shader=Resources.Load<Shader>("Map/RuinsGround");Check(shader!=null && shader.isSupported,"ground shader supported");
        Call(game,"StartRun");Call(game,"ClearActors");
        var camera=game.GetComponentInChildren<Camera>();camera.orthographicSize=400;camera.aspect=16f/9;
        foreach(SpriteRenderer tile in (IList)Get(game,"mapTiles"))
        {
            Check(tile.color.a==1,"tile opacity, including border");
            Check(tile.sharedMaterial.shader==shader,"continuous ground material");
            Check(tile.bounds.size.x>=80 && tile.bounds.size.y>=80,"no tile geometry gap");
        }
        int coping=0;
        foreach(GameObject decor in (IList)Get(game,"mapDecor"))
        {
            if(decor.name!="Boundary coping")continue;coping++;
            var r=decor.GetComponent<SpriteRenderer>().bounds;
            // Coping is outside the playable rectangle (with <=1px overlap at segment joins).
            bool outside=r.max.x<=ArenaLayout.Bounds.xMin+1 || r.min.x>=ArenaLayout.Bounds.xMax-1 || 720-r.max.y>=ArenaLayout.Bounds.yMax-1;
            Check(outside,"curb must meet, not intrude into, walkable area");
        }
        Check(coping>25,"three sides have physical-looking closure");
        foreach(var p in new[]{new Vector2(0,0),new Vector2(3200,0),new Vector2(0,2240),new Vector2(3200,2240),new Vector2(1600,0),new Vector2(1600,2240),new Vector2(0,1120),new Vector2(3200,1120)})
        {
            Set(game,"cameraFocus",p);Call(game,"TickFeedback",0f);
            string name="Edge_"+(int)p.x+"_"+(int)p.y;
            Capture(game,name,false,true);
        }
        for(int room=1;room<=3;room++)
        {
            Set(game,"room",room);Call(game,"SpawnRoom");Call(game,"ClearActors");
            Set(game,"player",new Vector2(1450,1160));Set(game,"cameraFocus",new Vector2(1450,1160));Call(game,"TickFeedback",0f);
            Capture(game,"Transition_Room"+room,true,true);
        }
        // Very wide windows must not look beyond the finite ground rectangle.
        camera.aspect=5;Call(game,"Update");
        Check(camera.orthographicSize*camera.aspect<=ArenaLayout.Width*.5f+.1f,"ultrawide viewport fits floor");
        camera.aspect=16f/9;camera.orthographicSize=400;
        Call(game,"StartRun");
        File.WriteAllText("MapReview/checks.txt","PASS "+checks+" map checks: opaque tiles, tile coverage, curb placement, 8 edge/corner renders, 3 transitions, wide-screen clamp. Screenshots are deterministic fixtures.");
        Debug.Log("PORTFOLIO_MAP_SEAMS_OK: "+checks+" checks, eight edge/corner views and three room transitions.");
    }
    static void Capture(RuinsGame game,string name,bool withUi,bool save)
    {
        var camera=game.GetComponentInChildren<Camera>();var ui=UnityEngine.Object.FindObjectOfType<PortfolioCanvasUI>();var canvas=ui.GetComponent<Canvas>();
        var target=new RenderTexture(1280,720,24);target.Create();
        var oldTarget=camera.targetTexture;var oldColor=camera.backgroundColor;var oldMode=canvas.renderMode;var oldCamera=canvas.worldCamera;var oldActive=RenderTexture.active;bool oldEnabled=canvas.enabled;
        try
        {
            camera.targetTexture=target;camera.backgroundColor=Color.magenta;
            canvas.enabled=withUi;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
            Call(game,"AnimatePlayer");Call(game,"RefreshUi");Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
            var image=new Texture2D(1280,720,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
            try
            {
                int exposed=0;
                foreach(var c in image.GetPixels32())if(c.r>245 && c.b>245 && c.g<10)exposed++;
                Check(exposed==0,name+" uncovered background or shader error pixels="+exposed);
                if(save)File.WriteAllBytes("MapReview/"+name+".png",image.EncodeToPNG());
            }
            finally{UnityEngine.Object.DestroyImmediate(image);}
        }
        finally
        {
            RenderTexture.active=oldActive;camera.targetTexture=oldTarget;camera.backgroundColor=oldColor;canvas.enabled=oldEnabled;canvas.renderMode=oldMode;canvas.worldCamera=oldCamera;
            target.Release();UnityEngine.Object.DestroyImmediate(target);
        }
    }
}
