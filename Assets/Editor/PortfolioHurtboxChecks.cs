using System;
using System.Collections;
using System.Reflection;
using Portfolio.Domain;
using UnityEngine;

public static class PortfolioHurtboxChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,F).Invoke(o,a);
    static int checks;
    static void Check(bool value,string message){checks++;if(!value)throw new Exception("Hurtbox: "+message);}
    public static void Run(RuinsGame game)
    {
        checks=0;
        foreach(int kind in new[]{0,1,2,3})
        {
            var body=EnemyHurtbox.Create(Vector2.zero,kind==1,kind==2,kind==3);
            float y=body.Top.y-body.Radius*.5f;
            Check(!float.IsPositiveInfinity(body.Entry(new Vector2(-200,y),new Vector2(200,y))),"upper body sweep "+kind);
            Check(float.IsPositiveInfinity(body.Entry(new Vector2(-200,body.Top.y-body.Radius-8),new Vector2(200,body.Top.y-body.Radius-8),5)),"above silhouette miss "+kind);
            Check(float.IsPositiveInfinity(body.Entry(new Vector2(-200,body.Bottom.y+body.Radius+8),new Vector2(200,body.Bottom.y+body.Radius+8),5)),"below feet miss "+kind);
            Check(body.Entry(body.Center,body.Center)==0,"stationary inside "+kind);
            Check(float.IsPositiveInfinity(body.Entry(new Vector2(200,200),new Vector2(200,200))),"stationary outside "+kind);
            Check(body.Overlaps(body.Center+Vector2.right*(body.Radius+50),50),"AoE tangency "+kind);
            Check(!body.Overlaps(body.Center+Vector2.right*(body.Radius+51),50),"AoE outside "+kind);
        }
        var edge=EnemyHurtbox.Create(new Vector2(125,24),false,false,false);
        Check(edge.OverlapsSector(Vector2.zero,Vector2.right,110,.5f),"melee touches body before feet enter reach");
        Check(!edge.OverlapsSector(Vector2.zero,Vector2.left,110,.5f),"no backwards melee");
        Check(!EnemyHurtbox.Create(new Vector2(180,24),false,false,false).OverlapsSector(Vector2.zero,Vector2.right,110,.5f),"no distant melee");
        var diagonal=EnemyHurtbox.Create(new Vector2(65,137),false,false,false);
        Check(diagonal.OverlapsSector(Vector2.zero,Vector2.right,150,.5f),"angular edge body overlap");
        Call(game,"StartRun");Call(game,"ClearActors");
        var position=new Vector2(1600,1100);
        var enemy=Call(game,"SpawnSlime",position,100f,false,0f);
        var from=position+new Vector2(-70,-36);var to=position+new Vector2(70,-36);
        Check((bool)Call(game,"ClearSkillPath",from,to),"fixture clear of walls");
        Call(game,"SpawnShot",from,Vector2.right*720,10f,1,false);
        var shots=(IList)Get(game,"shots");
        Check((bool)Call(game,"ResolveShot",shots[0],from,to),"upper body shot consumed");
        Check((float)Get(enemy,"hp")==90,"upper body loses health");
        Call(game,"ClearActors");enemy=Call(game,"SpawnSlime",position,100f,false,1f);
        Call(game,"SpawnShot",from,Vector2.right*720,10f,1,false);shots=(IList)Get(game,"shots");
        Call(game,"ResolveShot",shots[0],from,to);
        Check((float)Get(enemy,"hp")==100,"spawn warning cannot take damage");
        Call(game,"StartRun");
        Debug.Log("PORTFOLIO_HURTBOX_OK: "+checks+" checks; body sweep, empty-space misses, sector edges, area overlap, real damage and spawn immunity.");
    }
}
