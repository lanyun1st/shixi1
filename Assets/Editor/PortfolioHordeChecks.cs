using System;
using System.Collections;
using System.IO;
using System.Reflection;
using Portfolio.Domain;
using Portfolio.UI;
using UnityEngine;

public static class PortfolioHordeChecks
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static object Get(object o,string n)=>o.GetType().GetField(n,Flags).GetValue(o);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,Flags).SetValue(o,v);
    static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,Flags).Invoke(o,args);
    static int checks;
    static void Check(bool ok,string message){checks++;if(!ok)throw new Exception("Horde/map: "+message);}
    static void Equal(float a,float b,string message)=>Check(Mathf.Abs(a-b)<.02f,message+" actual="+a+" expected="+b);
    static IList Enemies(RuinsGame g)=>(IList)Get(g,"enemies");
    public static void AddCollisionFixture(RuinsGame g)
    {
        Call(g,"AddObstacle","column",new Vector2(520,440),new Vector2(62,30),-1,75f);
        Call(g,"AddObstacle","barrel",new Vector2(420,650),new Vector2(52,28),2,66f);
    }
    static void Reset(RuinsGame g)
    {
        Set(g,"shieldHero",true);Call(g,"StartRun");Call(g,"ClearActors");
        ((WaveDirector)Get(g,"waves")).CancelReinforcements();Set(g,"player",new Vector2(1600,1000));
    }
    static object Foe(RuinsGame g,Vector2 at,float hp=500)=>Call(g,"SpawnSlime",at,hp,false,0f);
    static void Swing(RuinsGame g)
    {
        Set(g,"cooldown",0f);Set(g,"meleeRemaining",0f);
        Call(g,"BeginMelee",(Vector2)Get(g,"player")+Vector2.right*200);
        Call(g,"TickShield",.2f,(Vector2)Get(g,"player")+Vector2.right*200);
    }
    public static void Run(RuinsGame g)
    {
        checks=0;Reset(g);
        Equal(ArenaLayout.Width,3200,"world width");Equal(ArenaLayout.Height,2240,"world height");
        Equal(((IList)Get(g,"mapTiles")).Count,1120,"tile reuse count");
        Check(((IList)Get(g,"floorSprites")).Count>=10,"tile palette diversity");
        var camera=g.GetComponentInChildren<Camera>();camera.orthographicSize=400;camera.aspect=16f/9;
        Set(g,"cameraFocus",new Vector2(2100,1400));Call(g,"TickFeedback",0f);
        Equal(camera.transform.position.x,2100,"camera follows x");Equal(camera.transform.position.y,720-1400,"camera follows y");
        Set(g,"cameraFocus",new Vector2(1,1));Call(g,"TickFeedback",0f);
        Check(camera.transform.position.x-camera.orthographicSize*camera.aspect>=-.01f,"camera left clamp");
        Equal(camera.transform.position.y,720-400,"camera top clamp");
        var world=new Vector3(1000,-100,0);var restored=camera.ScreenToWorldPoint(camera.WorldToScreenPoint(world));
        Check(Vector3.Distance(world,restored)<.01f,"scrolled aim round trip");
        // Test real spawn adapter at peak budget, not merely configured caps.
        for(int room=1;room<=3;room++)
        {
            Reset(g);Set(g,"room",room);Set(g,"player",new Vector2(1600,1200));Call(g,"SpawnRoom");Call(g,"ClearActors");
            var w=new WaveDirector(room);while(w.Wave<4){while(w.Pending>0)w.CommitSpawn();w.Tick(1,0,false);}Set(g,"waves",w);
            for(int i=0;i<100;i++)Call(g,"TickWaves",.2f);
            Equal(g.CaptureUiState().remaining,w.Cap,"actual peak room "+room);
            foreach(var e in Enemies(g))
            {
                var p=(Vector2)Get(e,"p");Check(Vector2.Distance(p,(Vector2)Get(g,"player"))>=320,"spawn safety radius");
                Check(p==ArenaLayout.Clamp(p,48),"spawn inside map");
                foreach(var o in (IList)Get(g,"obstacles"))Check(!ArenaLayout.CircleOverlaps(p,40,(Rect)Get(o,"footprint")),"spawn outside props");
            }
            Debug.Log("HORDE_PEAK room="+room+" alive="+g.CaptureUiState().remaining);
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-ui-captures")>=0)
            {
                Call(g,"TickEntryWaves",1.1f);Set(g,"cameraFocus",Get(g,"player"));Call(g,"TickFeedback",0f);
                Capture(g,"Room"+room);
            }
        }
        Reset(g);var loadout=(SkillLoadout)Get(g,"loadout");loadout.Add(1);loadout.Add(2);
        var foe=Foe(g,new Vector2(1680,1000));Swing(g);Equal((float)Get(foe,"hp"),464,"first combo swing");
        Swing(g);Equal((float)Get(foe,"hp"),428,"second combo swing");Swing(g);Equal((float)Get(foe,"hp"),363.2f,"third heavy swing");
        Check((float)Get(foe,"armor")>0,"heavy marks armor");Swing(g);Equal((float)Get(foe,"hp"),318.2f,"armor benefits following melee");
        Check((float)Get(foe,"slow")==0 && (float)Get(foe,"burn")==0,"shield never applies ranged elements");
        Reset(g);loadout.Add(0);loadout.Add(3);Set(g,"room",3);
        var vitality=(HealthModel)Get(g,"vitality");vitality.TakeDamage(50);
        Set(g,"momentum",3);Set(g,"momentumTime",6f);foe=Foe(g,new Vector2(1680,1000));var far=Foe(g,new Vector2(1860,1000));
        Swing(g);Equal((float)Get(foe,"hp"),429.8f,"one empowered hit plus one shield shock");
        Equal((float)Get(far,"hp"),500,"no remote bounce");Equal(vitality.Current,103,"counter heal once");
        Equal(((ShieldDefense)Get(g,"shieldDefense")).RecoveryBudget,27,"shared healing budget");Equal((int)Get(g,"momentum"),0,"momentum consumed");
        Reset(g);loadout.Add(5);loadout.Add(4);Set(g,"room",3);AddCollisionFixture(g);Set(g,"player",new Vector2(400,440));
        foe=Foe(g,new Vector2(440,440),200);Set(g,"dashDirection",Vector2.right);Call(g,"BeginShieldDash",Vector2.right);
        Call(g,"TickShieldDash",new Vector2(400,440),new Vector2(470,440));Equal((float)Get(foe,"hp"),146,"dash plus wall collision");
        Call(g,"TickShieldDash",new Vector2(400,440),new Vector2(470,440));Equal((float)Get(foe,"hp"),146,"dash collision once");
        Check((float)Get(foe,"breach")>0,"dash mark");
        // Poison a lease, keep a live projectile reference, then recycle and verify clean reuse.
        Reset(g);foe=Foe(g,new Vector2(1680,1000));Set(foe,"burn",8f);Set(foe,"breach",2f);Set(foe,"armor",4f);Set(foe,"frozen",2f);
        Call(g,"SpawnShot",new Vector2(1600,1000),Vector2.right,1f,3,false);
        var shot=((IList)Get(g,"shots"))[0];var hit=Get(shot,"hit");hit.GetType().GetMethod("Add").Invoke(hit,new[]{foe});
        Set(foe,"hp",0f);Set(foe,"deathFade",2f);Call(g,"RecycleDefeated",0f);
        var reused=Foe(g,new Vector2(1700,1000),24);Check(ReferenceEquals(foe,reused),"slime reused");
        foreach(var field in new[]{"burn","breach","armor","frozen","deathFade"})Equal((float)Get(reused,field),0,"clean lease "+field);
        Check(!(bool)hit.GetType().GetMethod("Contains").Invoke(hit,new[]{reused}),"old projectile hit identity removed");
        Call(g,"ClearActors");var pool=Get(g,"enemyPool");int created=(int)pool.GetType().GetProperty("Created").GetValue(pool);
        for(int i=0;i<500;i++){Foe(g,new Vector2(1700,1000),24);Call(g,"ClearActors");}
        Equal((int)pool.GetType().GetProperty("Created").GetValue(pool),created,"500 cycles no new slime objects");
        Reset(g);Set(g,"cameraFocus",Get(g,"player"));Call(g,"TickFeedback",0f);
        foe=Foe(g,new Vector2(150,400),17);Set(foe,"burn",2f);var close=Foe(g,new Vector2(1680,1000),24);
        int count=Enemies(g).Count;Call(g,"RegroupDistantSlimes",2f);
        Check(Vector2.Distance((Vector2)Get(foe,"p"),(Vector2)Get(g,"player"))<520,"offscreen straggler regroups");
        Equal((float)Get(foe,"hp"),17,"regroup preserves health");Equal((float)Get(foe,"burn"),2,"regroup preserves status");
        Equal((float)Get(foe,"spawnDelay"),1,"regroup telegraph");Equal(Enemies(g).Count,count,"regroup no extra budget");
        Check((Vector2)Get(close,"p")==new Vector2(1680,1000),"visible enemy never relocated");
        Reset(g);loadout.Add(1);Check(ShieldSkillCatalog.Available(2,loadout) && !ShieldSkillCatalog.Available(3,loadout),"component prerequisites");
        for(int i=0;i<20;i++){Call(g,"PrepareChoices");foreach(int id in (int[])Get(g,"choices"))Check(ShieldSkillCatalog.Available(id,loadout),"no orphan component");}
        Call(g,"StartRun");
        Debug.Log("PORTFOLIO_HORDE_MAP_OK: "+checks+" checks; actual caps 24/40/60, world, camera, skills, pool reuse. Not an FPS benchmark.");
        Directory.CreateDirectory("HordeReview");File.WriteAllText("HordeReview/checks.txt","PASS "+checks+" checks. Deterministic integration, not human balance or FPS validation.");
    }
    static void Capture(RuinsGame g,string name)
    {
        Directory.CreateDirectory("HordeReview");var camera=g.GetComponentInChildren<Camera>();var canvas=UnityEngine.Object.FindObjectOfType<PortfolioCanvasUI>().GetComponent<Canvas>();
        var target=new RenderTexture(1280,720,24);target.Create();var oldTarget=camera.targetTexture;var oldMode=canvas.renderMode;var oldCamera=canvas.worldCamera;var oldActive=RenderTexture.active;
        try
        {
            camera.targetTexture=target;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
            Call(g,"AnimatePlayer");Call(g,"RefreshUi");Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
            var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
            File.WriteAllBytes("HordeReview/"+name+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
        }
        finally{RenderTexture.active=oldActive;camera.targetTexture=oldTarget;canvas.renderMode=oldMode;canvas.worldCamera=oldCamera;target.Release();UnityEngine.Object.DestroyImmediate(target);}
    }
}
