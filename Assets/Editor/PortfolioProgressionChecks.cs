using System;
using System.Collections;
using System.Reflection;
using Portfolio.Domain;
using Portfolio.UI;
using UnityEngine;

// Real scene adapter tests with deterministic damage, not a balance simulation.
public static class PortfolioProgressionChecks
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static object Get(object o,string f)=>o.GetType().GetField(f,Flags).GetValue(o);
    static void Set(object o,string f,object v)=>o.GetType().GetField(f,Flags).SetValue(o,v);
    static object Call(object o,string m,params object[] a)=>o.GetType().GetMethod(m,Flags).Invoke(o,a);
    static void Check(bool b,string message){if(!b)throw new Exception("Progression: "+message);}
    static IList Enemies(RuinsGame g)=>(IList)Get(g,"enemies");
    static void Phase(RuinsGame g,string p){var f=g.GetType().GetField("phase",Flags);f.SetValue(g,Enum.Parse(f.FieldType,p));}
    public static void Run(RuinsGame g)
    {
        var ui=UnityEngine.Object.FindObjectOfType<PortfolioCanvasUI>();
        Phase(g,"Title");ui.startButton.onClick.Invoke();
        Check(g.CaptureUiState().mode==UiMode.Upgrade,"start must offer core");
        ((IGameUiCommands)g).RequestUpgrade(8);Check(g.CaptureUiState().upgradeMask==0,"invalid card accepted");
        ui.cards[0].button.onClick.Invoke();Check(g.CaptureUiState().upgradeMask==1,"core button failed");
        ((IGameUiCommands)g).RequestPause(true);var w=(WaveDirector)Get(g,"waves");int wave=w.Wave;Call(g,"Update");Check(w.Wave==wave,"pause advanced waves");((IGameUiCommands)g).RequestPause(false);
        int[] totals={100,180,260};
        for(int room=1;room<=3;room++)
        {
            int mid=0,peak=0;bool sawGuard=false,sawFast=false,sawSentry=false;
            for(int step=0;step<1500;step++)
            {
                w=(WaveDirector)Get(g,"waves");
                Call(g,"TickWaves",.25f);Call(g,"TickEntryWaves",.25f);
                var snapshot=g.CaptureUiState();peak=Mathf.Max(peak,snapshot.remaining);
                Check(snapshot.remaining<=w.Cap,"concurrency cap exceeded");
                int liveSentries=0;
                foreach(var e in Enemies(g))if(Get(e,"sentry")!=null && (float)Get(e,"hp")>0)liveSentries++;
                Check(liveSentries<=2,"ranged cap exceeded");
                foreach(var e in Enemies(g))
                {
                    if((float)Get(e,"spawnDelay")>0)
                    {
                        var p=(Vector2)Get(e,"p");Check(Vector2.Distance(p,(Vector2)Get(g,"player"))>=280,"unsafe spawn");
                        foreach(Vector2 decoration in (IList)Get(g,"spawnExclusions"))Check(Vector2.Distance(p,decoration)>=80,"spawn obscured by decoration");
                        Check(!((SpriteRenderer)Get(e,"view")).enabled,"telegraph enemy visible");
                    }
                    if((bool)Get(e,"fast")){sawFast=true;Check((float)Get(e,"max")==30,"fast hp");}
                    if(Get(e,"sentry")!=null)sawSentry=true;
                    if(Get(e,"elite")!=null){sawGuard=true;continue;}
                    // Leave enemies alive until the entire current wave has entered, exercising the cap.
                    if((w.Pending==0 || step%16==15) && (float)Get(e,"spawnDelay")<=0)Call(g,"Damage",e,999f,false);
                }
                if(g.CaptureUiState().mode==UiMode.Upgrade)
                {
                    mid++;int before=g.CaptureUiState().room;Call(g,"RefreshUi");
                    ui.cards[0].button.onClick.Invoke();Check(g.CaptureUiState().room==before,"mid reward advanced room");
                }
                if(w.Finished && w.Pending==0)
                {
                    bool pending=false;foreach(var e in Enemies(g))if((float)Get(e,"spawnDelay")>0)pending=true;
                    if(!pending){foreach(var e in Enemies(g))Call(g,"Damage",e,999f,false);break;}
                }
            }
            Check(Enemies(g).Count==totals[room-1],"room "+room+" total="+Enemies(g).Count);
            Check(mid==(room<3?1:0),"mid reward count");Check(room<2||sawFast,"fast variant absent");Check(room<3||sawGuard,"guard absent");
            Check(sawSentry==(room>=2),"ranged room progression");
            Call(g,"Update");Check(g.CaptureUiState().cleared,"room not clear");
            Set(g,"player",ArenaLayout.Entrance);Call(g,"TryPortal");Check(g.CaptureUiState().mode==UiMode.Playing,"distant portal accepted");
            Set(g,"player",ArenaLayout.Portal);Call(g,"TryPortal");Call(g,"RefreshUi");
            if(room<3){Check(g.CaptureUiState().mode==UiMode.Upgrade,"exit reward missing");ui.cards[0].button.onClick.Invoke();Check(g.CaptureUiState().room==room+1,"exit reward did not advance");}
            Debug.Log("WAVE_CHECK room="+room+" total="+totals[room-1]+" peak="+peak);
        }
        Check(g.CaptureUiState().won && g.CaptureUiState().mode==UiMode.Result,"victory missing");
        ui.restartResultButton.onClick.Invoke();Check(g.CaptureUiState().mode==UiMode.Upgrade && g.CaptureUiState().upgradeMask==0,"restart leaked build");
        CheckRoutesAndHits(g);
        CheckEarlyGuardDeath(g);
        PortfolioPoolChecks.Run(g);
        Debug.Log("PORTFOLIO_PROGRESSION_OK: 100/180/260 budgets, caps, telegraphs, rewards, guard continuation, ranged routes, collisions, restart, pools.");
    }
    static void Pair(RuinsGame g,out object a,out object b)
    {
        Call(g,"StartRun");Call(g,"TickWaves",1f);
        PortfolioHordeChecks.AddCollisionFixture(g);
        a=Enemies(g)[0];b=Enemies(g)[1];
        Set(a,"spawnDelay",0f);Set(b,"spawnDelay",0f);
        Set(a,"p",new Vector2(750,560));Set(b,"p",new Vector2(820,560));
        Set(a,"hp",200f);Set(b,"hp",200f);
    }
    static void CheckRoutesAndHits(RuinsGame g)
    {
        object a,b;Pair(g,out a,out b);var loadout=(SkillLoadout)Get(g,"loadout");loadout.Add(0);loadout.Add(3);
        Set(a,"burn",2f);Call(g,"Damage",a,24f,true);Check((float)Get(b,"hp")==164,"overload + chain damage");
        Pair(g,out a,out b);loadout.Add(1);loadout.Add(2);Set(a,"slow",1f);Call(g,"Damage",a,24f,true);
        Check((float)Get(b,"hp")==180 && (float)Get(b,"frozen")>0,"ice shatter");
        Pair(g,out a,out b);loadout.Add(4);loadout.Add(5);Set(g,"player",new Vector2(750,560));Call(g,"TryDash",Vector2.down);
        float afterNova=(float)Get(a,"hp");Call(g,"TickEcho",1f);Check((float)Get(a,"hp")<afterNova,"echo missing");
        Pair(g,out a,out b);Set(a,"p",new Vector2(800,560));Set(b,"p",new Vector2(700,560));
        Call(g,"SpawnShot",new Vector2(600,560),Vector2.right*720,24f,1,false);
        var shots=(IList)Get(g,"shots");Call(g,"ResolveShot",shots[0],new Vector2(600,560),new Vector2(900,560));
        Check((float)Get(a,"hp")==200 && (float)Get(b,"hp")==176,"list order defeated distance order");
        Pair(g,out a,out b);Set(a,"p",new Vector2(520,500));Set(b,"p",new Vector2(520,380));
        Call(g,"SpawnShot",new Vector2(520,570),Vector2.up*720,24f,3,false);
        shots=(IList)Get(g,"shots");Check((bool)Call(g,"ResolveShot",shots[0],new Vector2(520,570),new Vector2(520,350)),"pillar missed");
        Check((float)Get(a,"hp")==176 && (float)Get(b,"hp")==200,"wall/enemy order wrong");
        Set(g,"bufferedDash",true);((IGameUiCommands)g).RequestPause(true);Check(!(bool)Get(g,"bufferedDash"),"paused input leaked");
        Call(g,"StartRun");Set(g,"bufferedRelease",true);Call(g,"StartRun");Check(!(bool)Get(g,"bufferedRelease"),"restart input leaked");
        var charge=(ChargeAttack)Get(g,"chargeAttack");charge.Begin();charge.Tick(.7f);
        Set(g,"bufferedRelease",true);Set(g,"bufferedAim",new Vector2(800,500));Set(g,"hitStop",.1f);
        Call(g,"Update");Check((bool)Get(g,"bufferedRelease") && charge.Charging,"hit-stop dropped release");
        Set(g,"hitStop",0f);Call(g,"Update");
        Check(!(bool)Get(g,"bufferedRelease") && charge.Cooldown>0 && ((IList)Get(g,"shots")).Count==1,"buffered release did not fire once");
        Call(g,"StartRun");
        PortfolioHordeChecks.AddCollisionFixture(g);
        var stop=(Vector2)Call(g,"MoveSolid",new Vector2(430,440),new Vector2(200,0),16f);Check(stop.x<475,"dash crossed pillar");
        Call(g,"BlockShot",new Vector2(420,720),new Vector2(420,600));Call(g,"BlockShot",new Vector2(420,720),new Vector2(420,600));
        Check(!(bool)Call(g,"BlockShot",new Vector2(420,720),new Vector2(420,600)),"barrel still blocks");
        var vitality=(HealthModel)Get(g,"vitality");vitality.TakeDamage(99);a=Enemies(g)[0];Set(a,"spawnDelay",0f);Set(a,"p",Get(g,"player"));Set(g,"invincible",0f);Call(g,"Update");
        Check(g.CaptureUiState().mode==UiMode.Dying,"death failed");Set(g,"deathAge",1f);Call(g,"Update");Check(g.CaptureUiState().mode==UiMode.Result,"death result failed");
    }
    static void CheckEarlyGuardDeath(RuinsGame g)
    {
        Call(g,"StartRun");Set(g,"room",3);Call(g,"SpawnRoom");
        PortfolioHordeChecks.AddCollisionFixture(g);
        object guard=null;
        for(int step=0;step<100 && guard==null;step++)
        {
            Call(g,"TickWaves",.25f);Call(g,"TickEntryWaves",1f);
            foreach(var e in Enemies(g))if(Get(e,"elite")!=null)guard=e;else Call(g,"Damage",e,999f,false);
        }
        Check(guard!=null,"guard never spawned");
        var actor=(EliteGuardActor)Get(guard,"elite");actor.Brain.Tick(2,Vector2.right);actor.Brain.Tick(1,Vector2.right);
        var at=new Vector2(430,440);
        for(int step=0;step<10 && actor.Brain.State==EliteChargeBrain.Mode.Charge;step++)at=actor.Tick(.05f,at,new Vector2(1000,440),1);
        Check(actor.Brain.State==EliteChargeBrain.Mode.Stunned && at.x<465,"actual pillar did not stop/stun guard");
        float hp=(float)Get(guard,"hp");Call(g,"Damage",guard,20f,false);Check((float)Get(guard,"hp")==hp-35,"guard vulnerability");
        Call(g,"Damage",guard,999f,false);Call(g,"TickWaves",.25f);
        int count=Enemies(g).Count;for(int i=0;i<20;i++)Call(g,"TickWaves",1f);
        Check(!((WaveDirector)Get(g,"waves")).Cancelled && Enemies(g).Count>count,"guard death incorrectly deletes finite budget");
    }
}
