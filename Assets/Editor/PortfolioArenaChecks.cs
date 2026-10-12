using System;
using System.Collections;
using System.Reflection;
using Portfolio.Domain;
using UnityEngine;

public static class PortfolioArenaChecks
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static object Get(object o,string f)=>o.GetType().GetField(f,Flags).GetValue(o);
    static void Set(object o,string f,object v)=>o.GetType().GetField(f,Flags).SetValue(o,v);
    static object Call(object o,string m,params object[] a)=>o.GetType().GetMethod(m,Flags).Invoke(o,a);
    static void Check(bool b,string reason){if(!b)throw new Exception("Arena/skills: "+reason);}
    public static void Run(RuinsGame game)
    {
        Call(game,"StartRun");
        for(int room=1;room<=3;room++)
        {
            Set(game,"room",room);Call(game,"SpawnRoom");var enemies=(IList)Get(game,"enemies");
            foreach(var e in enemies)
            {
                var p=(Vector2)Get(e,"p");Check(Vector2.Distance(p,ArenaLayout.Entrance)>=260,"spawn too close to player");
                Check(ArenaLayout.Bounds.Contains(p),"spawn out of bounds");
                foreach(var o in (IList)Get(game,"obstacles"))Check(!ArenaLayout.CircleOverlaps(p,30,(Rect)Get(o,"footprint")),"spawn overlaps obstacle");
                foreach(var other in enemies)if(other!=e)Check(Vector2.Distance(p,(Vector2)Get(other,"p"))>90,"spawn overlap");
            }
        }
        foreach(var o in (IList)Get(game,"obstacles"))
        {
            var p=(Vector2)Get(o,"p");var view=(SpriteRenderer)Get(o,"view");
            Check(Vector2.Distance(new Vector2(view.transform.position.x,720-view.transform.position.y),p)<.01f,"prop double height offset");
            var r=(Rect)Get(o,"footprint");
            var resolved=(Vector2)Call(game,"MoveSolid",r.center,new Vector2(.01f,0),16f);
            Check(!ArenaLayout.CircleOverlaps(resolved,15.99f,r),"embedded actor not resolved");
        }
        Call(game,"StartRun");var build=(SkillLoadout)Get(game,"loadout");var foes=(IList)Get(game,"enemies");
        var a=foes[0];var b=foes[1];Set(a,"p",new Vector2(750,600));Set(b,"p",new Vector2(820,600));
        build.Add(0);build.Add(1);Call(game,"Damage",a,1f,true);
        Check((float)Get(a,"burn")>0 && (float)Get(a,"slow")>0,"dual elements missing");
        Check((float)Get(b,"hp")==52,"thermal AoE missing");
        Call(game,"Damage",a,1f,true);Check((float)Get(b,"hp")==52,"thermal cooldown bypassed");
        Call(game,"StartRun");build.Add(0);build.Add(3);foes=(IList)Get(game,"enemies");a=foes[0];b=foes[1];
        Set(a,"p",new Vector2(750,600));Set(b,"p",new Vector2(850,600));Call(game,"Damage",a,1f,true);
        Check((float)Get(b,"hp")==56 && (float)Get(b,"burn")>0,"chain failed to transfer burn");
        Call(game,"StartRun");build.Add(2);build.Add(4);Call(game,"Fire",new Vector2(800,400));
        var shots=(IList)Get(game,"shots");Check(shots.Count==3,"multishot count");
        foreach(var s in shots)Check((int)Get(s,"remaining")==3 && (float)Get(s,"damage")==17,"multishot/pierce composition");
        Check((int)Get(game,"fired")==3,"multishot accuracy denominator");
        Call(game,"StartRun");build.Add(1);build.Add(5);a=((IList)Get(game,"enemies"))[0];Set(a,"p",ArenaLayout.Entrance+new Vector2(60,0));
        Call(game,"TryDash",Vector2.up);Check((float)Get(a,"hp")==48 && (float)Get(a,"slow")>0,"dash nova composition");
        Call(game,"StartRun");Check(build.Mask==0,"build survives restart");
        Debug.Log("PORTFOLIO_ARENA_SKILLS_OK: safe spawns, foot-aligned props, footprints, thermal cooldown, status chain, multishot/pierce, dash nova, reset.");
    }
}
