using System;
using System.Collections;
using System.Reflection;
using Portfolio.Domain;
using UnityEngine;
using UnityEditor;

public static class PortfolioPoolChecks
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static object Get(object o,string f)=>o.GetType().GetField(f,Flags).GetValue(o);
    static void Set(object o,string f,object value)=>o.GetType().GetField(f,Flags).SetValue(o,value);
    static object Call(object o,string m,params object[] a)=>o.GetType().GetMethod(m,Flags).Invoke(o,a);
    static int Stat(object pool,string name)=>(int)pool.GetType().GetProperty(name).GetValue(pool);
    static void Check(bool b,string reason){if(!b)throw new Exception("Pool: "+reason);}
    public static void Run(RuinsGame game)
    {
        Call(game,"StartRun");var shots=(IList)Get(game,"shots");
        Call(game,"SpawnShot",new Vector2(700,800),Vector2.right*720,65f,6,true);
        var old=shots[0];var view=(SpriteRenderer)Get(old,"view");
        var hit=Get(old,"hit");Call(hit,"Add",((IList)Get(game,"enemies"))[0]);
        view.color=Color.red;view.flipX=view.flipY=true;view.enabled=false;view.transform.localScale=Vector3.one*7;
        Call(game,"ReleaseShotAt",0);
        Check(!view.gameObject.activeSelf,"return did not deactivate");
        var shotPool=Get(game,"shotPool");Check(!(bool)Call(shotPool,"Return",old),"double return accepted");
        Call(game,"SpawnShot",new Vector2(300,300),Vector2.up*720,24f,1,false);
        Check(ReferenceEquals(old,shots[0]),"shot instance not reused");
        Check(!(bool)Get(old,"heavy")&&(int)Get(old,"remaining")==1&&(float)Get(old,"damage")==24&&(float)Get(old,"life")==1.5f,"heavy state leaked");
        Check((int)hit.GetType().GetProperty("Count").GetValue(hit)==0,"hit targets leaked");
        Check(view.enabled&&view.color==Color.white&&!view.flipX&&!view.flipY&&view.sortingOrder==5000,"visual state leaked");
        Check(Mathf.Abs(view.transform.eulerAngles.z-270)<.1f,"rotation not reset to new heading");
        Call(game,"ClearActors");
        var pools=new[]{Get(game,"shotPool"),Get(game,"impactPool"),Get(game,"firePool"),Get(game,"linePool")};
        int before=0;foreach(var pool in pools)before+=Stat(pool,"Created");
        for(int i=0;i<500;i++)
        {
            Call(game,"SpawnShot",new Vector2(800,800),Vector2.up*720,24f,1,false);
            Call(game,"SpawnImpact",new Vector2(800,600),95f);
            Call(game,"SpawnFirePatch",new Vector2(800,600));
            Call(game,"DrawRing",new Vector2(800,600),35f,Color.yellow);
            Call(game,"ClearActors");
        }
        int after=0;foreach(var pool in pools){after+=Stat(pool,"Created");Check(Stat(pool,"Active")==0,"clear leaked active leases");}
        Check(after==before,"sequential warm stress created more render objects");
        for(int i=0;i<160;i++)Call(game,"SpawnShot",new Vector2(800,800),Vector2.zero,24f,1,false);
        Check(Stat(shotPool,"Active")==160,"burst expansion lost projectiles");Call(game,"ClearActors");
        Check(Stat(shotPool,"Active")==0&&Stat(shotPool,"Inactive")==128&&Stat(shotPool,"Destroyed")>=32,"burst retention exceeded cap");
        Call(game,"DrawRing",new Vector2(500,500),30f,Color.red);Call(game,"TickPooledLines",.23f);
        Check(((IList)Get(game,"skillLines")).Count==0,"line timeout failed");
        Call(game,"SkillLink",Vector2.zero,Vector2.one,Color.cyan);Call(game,"TickPooledLines",.1f);
        Check(((IList)Get(game,"skillLines")).Count==1,"reused line expired early");
        var phase=game.GetType().GetField("phase",Flags);phase.SetValue(game,Enum.Parse(phase.FieldType,"Paused"));
        Call(game,"Update");Check(((IList)Get(game,"skillLines")).Count==1,"pause advanced line lifetime");
        Call(game,"StartRun");
        // Gameplay return paths: wall impact and lifetime expiry.
        // Batch tests may have dt=0: begin outside the boundary to test the return path deterministically.
        Call(game,"SpawnShot",new Vector2(70,500),Vector2.left*720,24f,1,false);Call(game,"Update");
        Check(shots.Count==0,"wall did not return projectile");
        Call(game,"SpawnShot",new Vector2(800,800),Vector2.zero,24f,1,false);Set(shots[0],"life",-1f);Call(game,"Update");
        Check(shots.Count==0,"expiry did not return projectile");
        Call(game,"StartRun");var enemy=((IList)Get(game,"enemies"))[0];Set(enemy,"spawnDelay",0f);
        Call(game,"SpawnShot",(Vector2)Get(enemy,"p"),Vector2.zero,24f,1,false);Call(game,"Update");
        Check(shots.Count==0&&(float)Get(enemy,"hp")<=0,"enemy hit did not return projectile");
        var impacts=(IList)Get(game,"impacts");foreach(var effect in impacts)Set(effect,"age",1f);Call(game,"Update");
        Check(impacts.Count==0,"impact expiry did not return");
        Call(game,"SpawnFirePatch",new Vector2(800,800));Call(game,"TickFire",3f);
        Check(((IList)Get(game,"firePatches")).Count==0&&Stat(Get(game,"firePool"),"Active")==0,"fire expiry did not return");
        Call(game,"StartRun");
        Debug.Log("PORTFOLIO_POOL_OK: dirty heavy-to-normal reuse, hit-set reset, double return rejection, 500 cycles per pool (2000 leases), created delta="+(after-before)+", burst 160 -> retained 128, clear, line expiry/pause, wall/hit/timeout, impact/fire expiry. Not a GC or FPS benchmark.\n"+game.PoolDiagnostics);
    }
}

public sealed class PortfolioPoolWindow:EditorWindow
{
    [MenuItem("Portfolio/Diagnostics/Object Pools")]
    static void Open()=>GetWindow<PortfolioPoolWindow>("对象池统计");
    void OnInspectorUpdate()=>Repaint();
    void OnGUI()
    {
        EditorGUILayout.HelpBox("进入 Play 后观察创建、活跃、峰值与归还计数。创建数稳定不等于全游戏零 GC；请另外使用 Unity Profiler。",MessageType.Info);
        var game=UnityEngine.Object.FindObjectOfType<RuinsGame>();
        EditorGUILayout.TextArea(Application.isPlaying&&game!=null?game.PoolDiagnostics:"请运行 Ruins 场景。",GUILayout.MinHeight(100));
    }
}
