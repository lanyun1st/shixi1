using System;
using System.Collections;
using System.IO;
using System.Reflection;
using Portfolio.Domain;
using Portfolio.UI;
using UnityEngine;

public static class PortfolioSentryChecks
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    static object Get(object o,string f)=>o.GetType().GetField(f,Flags).GetValue(o);
    static void Set(object o,string f,object v)=>o.GetType().GetField(f,Flags).SetValue(o,v);
    static object Call(object o,string m,params object[] a)=>o.GetType().GetMethod(m,Flags).Invoke(o,a);
    static void Check(bool b,string reason){if(!b)throw new Exception("Sentry: "+reason);}
    static IList Bolts(RuinsGame g)=>(IList)Get(g,"hostileShots");
    static void Reset(RuinsGame g)
    {
        Call(g,"StartRun");Call(g,"ClearActors");((WaveDirector)Get(g,"waves")).CancelReinforcements();
        PortfolioHordeChecks.AddCollisionFixture(g);
        Set(g,"player",new Vector2(1000,500));Set(g,"invincible",0f);
    }
    public static void Run(RuinsGame g)
    {
        foreach(var d in new[]{"down","up","left","right"})foreach(var pose in new[]{"idle","windup","fire","recover"})
        {
            var sprite=Resources.Load<Sprite>("Art/sentry/"+d+"_"+pose);
            Check(sprite!=null && sprite.rect.width==512 && Mathf.Abs(sprite.pivot.y-92)<.1f,"sprite/pivot "+d+pose);
        }
        var boltSprite=Resources.Load<Sprite>("Art/sentry/bolt");Check(boltSprite!=null && boltSprite.pivot==new Vector2(128,128),"bolt core pivot");
        Reset(g);var e=Call(g,"SpawnSentry",new Vector2(700,500),0f);var actor=(SentryActor)Get(e,"sentry");
        actor.Tick(.5f,new Vector2(700,500),new Vector2(1000,500),1);
        Check(actor.WarningVisible && Bolts(g).Count==0,"missing telegraph / early bullet");
        actor.Tick(.5f,new Vector2(700,500),new Vector2(700,800),1);Check(Bolts(g).Count==0,"fired before full warning");
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-ui-captures")>=0)Capture(g,"SentryWindup");
        actor.Tick(.41f,new Vector2(700,500),new Vector2(700,800),1);
        Check(!actor.WarningVisible && Bolts(g).Count==1,"fire event");
        Check((Vector2)Get(Bolts(g)[0],"velocity")==Vector2.right*240,"homing after lock");
        Call(g,"TickHostileShots",.35f);
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-ui-captures")>=0)Capture(g,"SentryFire");
        actor.Tick(.01f,new Vector2(700,500),new Vector2(700,800),1);Check(Bolts(g).Count==1,"duplicate bullet");
        Call(g,"Damage",e,999f,false);actor.Tick(3,new Vector2(700,500),new Vector2(1000,500),1);
        Check(!actor.WarningVisible && Bolts(g).Count==1,"post-death fire");
        Reset(g);e=Call(g,"SpawnSentry",new Vector2(700,500),0f);actor=(SentryActor)Get(e,"sentry");
        actor.Tick(.5f,new Vector2(700,500),new Vector2(1000,500),1);Set(e,"frozen",1f);Call(g,"Update");
        Check(!actor.WarningVisible && actor.Brain.State==SentryBrain.Mode.Recover && Bolts(g).Count==0,"freeze did not interrupt");
        Reset(g);Call(g,"SpawnHostileShot",new Vector2(900,500),Vector2.right);var old=Bolts(g)[0];var view=(SpriteRenderer)Get(old,"view");
        ((IGameUiCommands)g).RequestPause(true);Call(g,"TickHostileShots",1f);Check((Vector2)Get(old,"p")==new Vector2(900,500),"pause moved bolt");((IGameUiCommands)g).RequestPause(false);
        Call(g,"TickHostileShots",.5f);Check(Bolts(g).Count==0 && ((HealthModel)Get(g,"vitality")).Current==88,"bolt/player sweep");
        // Invulnerability consumes a bullet without causing another hit.
        Call(g,"SpawnHostileShot",new Vector2(1000,500),Vector2.right);Call(g,"TickHostileShots",0f);
        Check(Bolts(g).Count==0 && ((HealthModel)Get(g,"vitality")).Current==88,"invulnerability ignored");
        Reset(g);Set(g,"player",new Vector2(520,350));Call(g,"SpawnHostileShot",new Vector2(520,550),Vector2.up*-1);Call(g,"TickHostileShots",1f);
        Check(Bolts(g).Count==0 && ((HealthModel)Get(g,"vitality")).Current==100,"pillar failed to shield player");
        Reset(g);Set(g,"player",new Vector2(700,700));e=Call(g,"SpawnSentry",new Vector2(900,500),0f);
        Call(g,"SpawnHostileShot",new Vector2(800,500),Vector2.right);Call(g,"TickHostileShots",.5f);Check((float)Get(e,"hp")==60,"friendly fire");
        Call(g,"ClearHostileShots");Call(g,"SpawnHostileShot",new Vector2(800,500),Vector2.right);
        old=Bolts(g)[0];view=(SpriteRenderer)Get(old,"view");view.color=Color.red;view.flipX=true;Set(old,"radius",99f);Set(old,"damage",999f);Call(g,"ClearHostileShots");
        Call(g,"SpawnHostileShot",new Vector2(700,600),Vector2.down);
        Check(ReferenceEquals(old,Bolts(g)[0]) && (float)Get(old,"radius")==10 && (float)Get(old,"damage")==12 && view.color==Color.white && !view.flipX,"dirty reuse");
        Call(g,"ClearHostileShots");for(int i=0;i<30;i++)Call(g,"SpawnHostileShot",new Vector2(800,500),Vector2.right);Check(Bolts(g).Count==16,"projectile cap");
        Call(g,"ClearHostileShots");Call(g,"SpawnHostileShot",new Vector2(ArenaLayout.Bounds.xMax-10,500),Vector2.right);Call(g,"TickHostileShots",.2f);Check(Bolts(g).Count==0,"boundary return");
        Call(g,"SpawnHostileShot",new Vector2(800,500),Vector2.zero);Set(Bolts(g)[0],"life",-.1f);Call(g,"TickHostileShots",0f);Check(Bolts(g).Count==0,"expiry return");
        var pool=Get(g,"hostilePool");int created=(int)pool.GetType().GetProperty("Created").GetValue(pool);
        for(int i=0;i<500;i++){Call(g,"SpawnHostileShot",new Vector2(800,500),Vector2.right);Call(g,"ClearHostileShots");}
        Check((int)pool.GetType().GetProperty("Created").GetValue(pool)==created,"warmed enemy pool kept allocating");
        Call(g,"SpawnHostileShot",new Vector2(800,500),Vector2.right);Call(g,"StartRun");Check(Bolts(g).Count==0,"restart leak");
        Reset(g);((HealthModel)Get(g,"vitality")).TakeDamage(99);Call(g,"SpawnHostileShot",new Vector2(1000,500),Vector2.right);Call(g,"TickHostileShots",0f);
        Check(g.CaptureUiState().mode==UiMode.Dying,"ranged death transition");
        Call(g,"StartRun");Debug.Log("PORTFOLIO_SENTRY_OK: 17 assets/pivots, warning, locked aim, fire once, freeze/death cancellation, pause, sweep, cover, invulnerability, no friendly fire, pooled reset, cap, expiry, boundary, restart, player death.");
    }
    static void Capture(RuinsGame g,string name)
    {
        Directory.CreateDirectory("SentryReview");var camera=g.GetComponentInChildren<Camera>();var ui=UnityEngine.Object.FindObjectOfType<PortfolioCanvasUI>();var canvas=ui.GetComponent<Canvas>();
        var target=new RenderTexture(1280,720,24);target.Create();var previousTarget=camera.targetTexture;var previousMode=canvas.renderMode;var previousCamera=canvas.worldCamera;var previousActive=RenderTexture.active;
        try
        {
            camera.targetTexture=target;camera.orthographicSize=500;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
            Call(g,"AnimatePlayer");Call(g,"RefreshUi");Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
            var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();
            File.WriteAllBytes("SentryReview/"+name+".png",texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
        }
        finally{RenderTexture.active=previousActive;camera.targetTexture=previousTarget;canvas.renderMode=previousMode;canvas.worldCamera=previousCamera;target.Release();UnityEngine.Object.DestroyImmediate(target);}
    }
}
