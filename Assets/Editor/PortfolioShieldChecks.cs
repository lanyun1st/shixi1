using System;
using System.Collections;
using System.IO;
using System.Reflection;
using Portfolio.Domain;
using Portfolio.UI;
using UnityEngine;

public static class PortfolioShieldChecks
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    static object Get(object o,string n)=>o.GetType().GetField(n,Flags).GetValue(o);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,Flags).SetValue(o,v);
    static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,Flags).Invoke(o,args);
    static int checks;
    static void Check(bool pass,string reason){checks++;if(!pass)throw new Exception("Shield: "+reason);}
    static void Equal(float a,float b,string reason)=>Check(Math.Abs(a-b)<.01f,reason+" actual="+a+" expected="+b);
    static void Reset(RuinsGame game)
    {
        Set(game,"shieldHero",true);Call(game,"StartRun");Call(game,"ClearActors");Call(game,"ResetShield");
        PortfolioHordeChecks.AddCollisionFixture(game);
        ((WaveDirector)Get(game,"waves")).CancelReinforcements();
        Set(game,"player",new Vector2(800,500));Set(game,"invincible",0f);
    }
    public static void Run(RuinsGame game)
    {
        checks=0;
        Set(game,"phase",Enum.Parse(Get(game,"phase").GetType(),"Title"));
        var selection=UnityEngine.Object.FindObjectOfType<PortfolioCanvasUI>();
        selection.titleScreen.transform.Find("Select Shield").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
        Check(game.CaptureUiState().shieldHero && game.CaptureUiState().mode==UiMode.Title,"shield selection button");
        selection.titleScreen.transform.Find("Select Ranged").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
        Check(!game.CaptureUiState().shieldHero,"ranged selection button");
        var h=new HealthModel(150);var d=new ShieldDefense();bool perfect;
        d.SetGuard(true);Equal(d.Resolve(20,1,false,out perfect),0,"perfect negates");Check(perfect,"perfect flag");
        Equal(d.Resolve(20,1,false,out perfect),5,"same window only parries once");
        Equal(d.Resolve(20,0,false,out perfect),20,"side bypass");
        Equal(d.Resolve(20,-1,false,out perfect),20,"back bypass");
        Equal(d.Resolve(24,1,true,out perfect),24,"elite breaks guard");
        Check(d.ConsumeCounter() && !d.ConsumeCounter(),"counter consumed once");
        d.SetGuard(false);d.SetGuard(true);Equal(d.Resolve(20,1,false,out perfect),5,"spam cannot refresh parry");
        d.Reset();d.SetGuard(true);d.Tick(.21f,h,true);Equal(d.Resolve(20,1,false,out perfect),5,"window expires");
        h.TakeDamage(100);d.Reset();d.Tick(4,h,true);Equal(h.Current,50,"regen delay");
        d.Tick(2,h,true);Equal(h.Current,54,"regen rate");
        d.Tick(100,h,true);Equal(h.Current,80,"room budget");Equal(d.RecoveryBudget,0,"budget exhausted");
        d.Reset();h.Reset();h.TakeDamage(65);d.Tick(10,h,true);Equal(h.Current,90,"60 percent ceiling");Equal(d.RecoveryBudget,25,"only actual healing spent");
        d.Reset();h.TakeDamage(20);d.Tick(10,h,false);Equal(h.Current,70,"pause and clear freeze regeneration");
        h.TakeDamage(999);d.Tick(100,h,true);Equal(h.Current,0,"dead cannot heal");
        h.Reset(100);Equal(h.Maximum,100,"max health switches");
        d.Reset();d.SetGuard(true);d.Resolve(12,1,false,out perfect);d.Tick(3.1f,h,true);Check(!d.ConsumeCounter(),"counter expires");

        foreach(var direction in new[]{"down","up","left","right"})
            foreach(var action in new[]{"idle","move","attack","block","dash"})
                for(int i=0;i<(action=="idle"?1:action=="attack"?3:2);i++)
                {
                    var sprite=Resources.Load<Sprite>("Art/shield/"+direction+"_"+action+"_"+i);
                    Check(sprite!=null && Math.Abs(sprite.pivot.y-80)<.1,"sprite and foot pivot "+direction+action+i);
                }
        Reset(game);Equal(((HealthModel)Get(game,"vitality")).Maximum,150,"shield maximum");
        var defense=(ShieldDefense)Get(game,"shieldDefense");Set(game,"shieldAim",Vector2.right);defense.SetGuard(true);
        Call(game,"SpawnHostileShot",new Vector2(880,500),Vector2.left);Call(game,"TickHostileShots",.4f);
        Equal(((HealthModel)Get(game,"vitality")).Current,150,"projectile uses incoming direction");
        Check(defense.CounterRemaining>0 && ((IList)Get(game,"hostileShots")).Count==0,"parry consumes bolt and grants counter");
        Set(game,"invincible",0f);Call(game,"ReceivePlayerHit",20f,Vector2.left,false);
        Equal(((HealthModel)Get(game,"vitality")).Current,130,"rear hit gets through");
        Set(game,"invincible",0f);Call(game,"ReceivePlayerHit",24f,Vector2.right,true);
        Equal(((HealthModel)Get(game,"vitality")).Current,106,"unblockable integration");

        Reset(game);var foe=Call(game,"SpawnSentry",new Vector2(880,500),0f);
        Call(game,"BeginMelee",new Vector2(900,500));Call(game,"TickShield",.19f,new Vector2(900,500));
        Equal((float)Get(foe,"hp"),24,"melee damage");
        Call(game,"TickShield",.05f,new Vector2(900,500));Equal((float)Get(foe,"hp"),24,"no repeated swing damage");
        Check(((IList)Get(game,"shots")).Count==0,"melee does not spawn player bullets");
        Reset(game);foe=Call(game,"SpawnSentry",new Vector2(800,600),0f);
        Call(game,"BeginMelee",new Vector2(900,500));Call(game,"TickShield",.19f,new Vector2(900,500));
        Equal((float)Get(foe,"hp"),24,"arc includes overlapping body even when feet are outside");
        Reset(game);foe=Call(game,"SpawnSentry",new Vector2(800,650),0f);
        Call(game,"BeginMelee",new Vector2(900,500));Call(game,"TickShield",.19f,new Vector2(900,500));
        Equal((float)Get(foe,"hp"),60,"arc excludes flank");
        Reset(game);((SkillLoadout)Get(game,"loadout")).Add(7);foe=Call(game,"SpawnSentry",new Vector2(930,500),0f);
        Call(game,"BeginMelee",new Vector2(950,500));Call(game,"TickShield",.19f,new Vector2(950,500));
        Equal((float)Get(foe,"hp"),24,"reach upgrade");
        Reset(game);foe=Call(game,"SpawnSentry",new Vector2(880,500),0f);
        defense=(ShieldDefense)Get(game,"shieldDefense");defense.SetGuard(true);
        defense.Resolve(20,1,false,out perfect);defense.SetGuard(false);
        Call(game,"BeginMelee",new Vector2(900,500));Call(game,"TickShield",.19f,new Vector2(900,500));
        Equal((float)Get(foe,"hp"),6,"counter deals 54");
        Check(defense.CounterRemaining==0,"swing consumes counter");
        Reset(game);Set(game,"player",new Vector2(520,380));foe=Call(game,"SpawnSentry",new Vector2(520,490),0f);
        ((SkillLoadout)Get(game,"loadout")).Add(2);
        Call(game,"BeginMelee",new Vector2(520,520));Call(game,"TickShield",.19f,new Vector2(520,520));
        Equal((float)Get(foe,"hp"),60,"melee blocked by pillar");
        Reset(game);((SkillLoadout)Get(game,"loadout")).Add(1);Set(game,"combo",2);Set(game,"comboTime",1f);foe=Call(game,"SpawnSentry",new Vector2(840,590),0f);
        Call(game,"BeginMelee",new Vector2(900,500));Call(game,"TickShield",.19f,new Vector2(900,500));
        Check((float)Get(foe,"hp")<=0,"third heavy swing hits side arc");
        Reset(game);Set(game,"player",new Vector2(520,380));Call(game,"TryDash",Vector2.up);
        Call(game,"MovePlayer",Vector2.zero,.2f);
        Equal((float)Get(game,"dashTime"),0,"pillar stops dash");
        Check(((Vector2)Get(game,"player")).y<425,"dash does not pass through pillar");
        Reset(game);var model=(HealthModel)Get(game,"vitality");model.TakeDamage(100);
        Call(game,"TickShield",4f,new Vector2(900,500));
        ((IGameUiCommands)game).RequestPause(true);Call(game,"TickShield",10f,new Vector2(900,500));Equal(model.Current,50,"pause integration");
        ((IGameUiCommands)game).RequestPause(false);Call(game,"TickShield",1f,new Vector2(900,500));Equal(model.Current,52,"resume regen");
        Set(game,"roomCleared",true);Call(game,"TickShield",10f,new Vector2(900,500));Equal(model.Current,52,"no clear-room healing");
        Set(game,"room",2);Call(game,"SpawnRoom");Equal(model.Current,52,"room reset does not refill health");
        Equal(((ShieldDefense)Get(game,"shieldDefense")).RecoveryBudget,30,"new room budget");
        Reset(game);foe=Call(game,"SpawnSentry",new Vector2(880,500),0f);
        Call(game,"TryDash",Vector2.right);Equal((float)Get(game,"dashTime"),.24f,"shield dash duration");
        Equal((float)Get(game,"invincible"),0,"dash not invulnerable");
        Call(game,"TickShieldDash",new Vector2(800,500),new Vector2(890,500));
        float hp=(float)Get(foe,"hp");Call(game,"TickShieldDash",new Vector2(800,500),new Vector2(890,500));
        Equal((float)Get(foe,"hp"),hp,"one dash hits once");
        defense=(ShieldDefense)Get(game,"shieldDefense");Check(defense.Guarding,"dash frontal protection");
        Call(game,"StartRun");Check(!defense.Guarding && defense.CounterRemaining==0 && defense.RecoveryBudget==30,"restart resets state");
        Equal(((HealthModel)Get(game,"vitality")).Current,150,"restart full health");
        // Character changes are rejected during combat, accepted at the selection screen.
        ((ICharacterUiCommands)game).RequestCharacter(false);Check(game.CaptureUiState().shieldHero,"midrun switch rejected");
        Set(game,"phase",Enum.Parse(Get(game,"phase").GetType(),"Result"));
        ((ICharacterUiCommands)game).RequestCharacter(false);Equal(((HealthModel)Get(game,"vitality")).Maximum,100,"switch restores ranged maximum");
        ((ICharacterUiCommands)game).RequestCharacter(true);
        Call(game,"StartRun");Call(game,"BeginCoreChoice");Call(game,"RefreshUi");
        Check(UiText.UpgradeName(2,true)=="碎甲锤头" && UiText.UpgradeName(4,true)=="破绽追击","melee card names");
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-ui-captures")>=0)
        {
            Capture(game,"ShieldCards");
            ((IGameUiCommands)game).RequestUpgrade(1);Set(game,"player",new Vector2(800,500));
            Set(game,"shieldAim",Vector2.right);((ShieldDefense)Get(game,"shieldDefense")).SetGuard(true);
            Set(game,"facing","right");Call(game,"SpawnSentry",new Vector2(1050,500),0f);
            Capture(game,"ShieldGuard");
            Set(game,"phase",Enum.Parse(Get(game,"phase").GetType(),"Title"));Capture(game,"ShieldSelection");
        }
        Set(game,"shieldHero",false);Call(game,"StartRun");
        Directory.CreateDirectory("ShieldReview");
        File.WriteAllText("ShieldReview/checks.txt","PASS "+checks+" checks: rules, regeneration budget, directions, parry cooldown, assets, melee, dash, UI, restart.");
        Debug.Log("PORTFOLIO_SHIELD_OK: "+checks+" checks");
    }
    static void Capture(RuinsGame game,string name)
    {
        Directory.CreateDirectory("ShieldReview");
        var camera=game.GetComponentInChildren<Camera>();var canvas=UnityEngine.Object.FindObjectOfType<PortfolioCanvasUI>().GetComponent<Canvas>();
        var target=new RenderTexture(1280,720,24);target.Create();
        var oldTarget=camera.targetTexture;var oldMode=canvas.renderMode;var oldCamera=canvas.worldCamera;var oldActive=RenderTexture.active;
        try
        {
            camera.targetTexture=target;camera.orthographicSize=500;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
            Call(game,"AnimatePlayer");Call(game,"RefreshUi");Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
            var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();
            File.WriteAllBytes("ShieldReview/"+name+".png",texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
        }
        finally{RenderTexture.active=oldActive;camera.targetTexture=oldTarget;canvas.renderMode=oldMode;canvas.worldCamera=oldCamera;target.Release();UnityEngine.Object.DestroyImmediate(target);}
    }
}
