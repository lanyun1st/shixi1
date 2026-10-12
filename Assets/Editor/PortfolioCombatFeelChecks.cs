using System;
using System.Collections;
using System.Reflection;
using Portfolio.Domain;
using UnityEngine;

public static class PortfolioCombatFeelChecks
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static object Get(object o,string f)=>o.GetType().GetField(f,Flags).GetValue(o);
    static void Set(object o,string f,object v)=>o.GetType().GetField(f,Flags).SetValue(o,v);
    static object Call(object o,string m,params object[] a)=>o.GetType().GetMethod(m,Flags).Invoke(o,a);
    static void Check(bool b,string reason){if(!b)throw new Exception("Combat feel: "+reason);}
    public static void Run(RuinsGame game)
    {
        Call(game,"StartRun");var c=(ChargeAttack)Get(game,"chargeAttack");
        c.Begin();c.Tick(.3f);Call(game,"ReleaseHeavy",new Vector2(800,300));Check(((IList)Get(game,"shots")).Count==0,"partial charge fired");
        c.Begin();c.Tick(.7f);Call(game,"ReleaseHeavy",new Vector2(800,300));
        var shots=(IList)Get(game,"shots");Check(shots.Count==1 && (bool)Get(shots[0],"heavy") && (int)Get(shots[0],"remaining")==4,"heavy projectile");
        Check(!c.Begin(),"charge cooldown bypass");
        Call(game,"StartRun");c.Begin();Call(game,"TryDash",Vector2.right);Check(!c.Charging,"dash does not cancel charge");
        Call(game,"StartRun");var build=(SkillLoadout)Get(game,"loadout");build.Add(0);build.Add(1);
        var e=((IList)Get(game,"enemies"))[0];Call(game,"HeavyHit",e,Vector2.right);
        Check((float)Get(e,"frozen")>0 && ((IList)Get(game,"firePatches")).Count==1,"heavy elements");
        float hp=(float)Get(e,"hp");Call(game,"TickFire",.4f);Check((float)Get(e,"hp")<hp,"fire patch damage");
        Set(game,"room",3);Call(game,"SpawnRoom");var foes=(IList)Get(game,"enemies");int pending=0;object elite=null;
        foreach(var foe in foes){if((float)Get(foe,"spawnDelay")>0)pending++;if(Get(foe,"elite")!=null)elite=foe;}
        Check(pending==2 && elite!=null,"third room wave schedule");
        Call(game,"TickEntryWaves",6f);pending=0;foreach(var foe in foes)if((float)Get(foe,"spawnDelay")>0)pending++;
        Check(pending==1,"first reinforcement timing");Call(game,"TickEntryWaves",4f);
        foreach(var foe in foes)Check((float)Get(foe,"spawnDelay")==0,"second reinforcement missing");
        var actor=(EliteGuardActor)Get(elite,"elite");actor.Brain.Tick(2,Vector2.right);actor.Brain.Tick(1,Vector2.right);actor.Brain.HitObstacle();
        hp=(float)Get(elite,"hp");Call(game,"Damage",elite,20f,false);Check(Mathf.Abs(hp-(float)Get(elite,"hp")-35)<.001f,"weak point damage");
        Call(game,"StartRun");e=((IList)Get(game,"enemies"))[0];Set(e,"p",new Vector2(455,650));var before=(Vector2)Get(e,"p");
        Call(game,"BlockShot",new Vector2(420,720),new Vector2(420,600));Call(game,"BlockShot",new Vector2(420,720),new Vector2(420,600));
        Check(Vector2.Distance(before,(Vector2)Get(e,"p"))>30,"barrel knockback missing");
        Call(game,"StartRun");Check(((IList)Get(game,"firePatches")).Count==0&&!c.Charging&&c.Cooldown==0,"restart leaked combat state");
        Debug.Log("PORTFOLIO_COMBAT_FEEL_OK: charge/release/cancel/cooldown, heavy elements, timed reinforcements, exposed damage, barrel push, reset.");
    }
}
