using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using Portfolio.Domain;
using Portfolio.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Repeatable editor CPU/allocation microbenchmark, not a player FPS claim.
public static class PortfolioPerformanceProbe
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static double started;
    static bool oldEnabled;static EnterPlayModeOptions oldOptions;
    static bool allocationCounterValid;
    static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,F).Invoke(o,a);
    static T Bind<T>(object o,string n) where T:class =>Delegate.CreateDelegate(typeof(T),o,o.GetType().GetMethod(n,F)) as T;
    public static void Run()
    {
        PortfolioSetup.CreateScene();EditorSceneManager.OpenScene("Assets/Scenes/Ruins.unity");
        oldEnabled=EditorSettings.enterPlayModeOptionsEnabled;oldOptions=EditorSettings.enterPlayModeOptions;
        EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
        started=EditorApplication.timeSinceStartup;EditorApplication.update+=Tick;EditorApplication.isPlaying=true;
    }
    static void Tick()
    {
        if(EditorApplication.timeSinceStartup-started<3 || !EditorApplication.isPlaying)return;
        EditorApplication.update-=Tick;int exit=0;
        try
        {
            var game=UnityEngine.Object.FindObjectOfType<RuinsGame>();game.enabled=false;Call(game,"StartRun");Set(game,"room",3);Call(game,"SpawnRoom");Call(game,"ClearActors");
            long calibrationStart=GC.GetAllocatedBytesForCurrentThread();var allocationSample=new byte[4096];GC.KeepAlive(allocationSample);
            allocationCounterValid=GC.GetAllocatedBytesForCurrentThread()-calibrationStart>=4096;
            ((WaveDirector)Get(game,"waves")).CancelReinforcements();Set(game,"invincible",10000f);Set(game,"player",new Vector2(1600,1100));
            var skills=(SkillLoadout)Get(game,"loadout");for(int i=0;i<9;i++)skills.Add(i);
            for(int i=0;i<60;i++)Call(game,"SpawnSlime",new Vector2(1200+(i%10)*80,800+(i/10)*100),1000000f,false,0f);
            var path=Bind<Func<Vector2,Vector2,bool>>(game,"ClearSkillPath");var snapshot=Bind<Func<GameUiSnapshot>>(game,"CaptureUiState");
            var ui=UnityEngine.Object.FindObjectOfType<PortfolioCanvasUI>();var choices=new[]{0,1,5};
            var from=new Vector2[1024];var to=new Vector2[1024];var rng=new System.Random(4701);
            for(int i=0;i<from.Length;i++){from[i]=new Vector2(100+rng.Next(3000),250+rng.Next(1800));to[i]=from[i]+new Vector2(rng.Next(-270,271),rng.Next(-270,271));}
            string report="Unity "+Application.unityVersion+" | "+SystemInfo.processorType+"\nEditor synchronous microbench. Room 3, 60 alive slimes, all 9 ranged skills; rendering/GPU/frame pacing excluded. Same fixture before/after. Warmup excluded.\n";
            int blocked=0;
            report+=Measure("1024 skill visibility queries",()=>{for(int i=0;i<from.Length;i++)if(!path(from[i],to[i]))blocked++;},40);
            report+=Measure("100 UI snapshots",()=>{for(int i=0;i<100;i++)snapshot();},40);
            var state=snapshot();
            report+=Measure("30 unchanged HUD renders + Canvas rebuild",()=>{for(int i=0;i<30;i++)ui.Render(state,choices);Canvas.ForceUpdateCanvases();},30);
            report+="Allocation counter self-test="+(allocationCounterValid?"supported":"UNAVAILABLE: Unity Mono returns no allocation delta; do not interpret as zero GC")+"\nblocked checksum (incl warmup)="+blocked+"\n"+game.PoolDiagnostics+"\n";
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-probe-output");var file=at>=0?args[at+1]:"performance-probe.txt";
            File.WriteAllText(file,report);UnityEngine.Debug.Log("PORTFOLIO_PERFORMANCE_PROBE_OK\n"+report);
        }
        catch(Exception e){exit=1;UnityEngine.Debug.LogException(e);}
        finally{EditorSettings.enterPlayModeOptionsEnabled=oldEnabled;EditorSettings.enterPlayModeOptions=oldOptions;EditorApplication.Exit(exit);}
    }
    static string Measure(string name,Action action,int batches)
    {
        for(int i=0;i<8;i++)action();var timings=new double[batches];long bytes=0;
        for(int i=0;i<batches;i++){long before=GC.GetAllocatedBytesForCurrentThread();long start=Stopwatch.GetTimestamp();action();long end=Stopwatch.GetTimestamp();bytes+=GC.GetAllocatedBytesForCurrentThread()-before;timings[i]=(end-start)*1000d/Stopwatch.Frequency;}
        Array.Sort(timings);return name+": median="+timings[batches/2].ToString("F3")+"ms p95="+timings[(int)(batches*.95)-1].ToString("F3")+"ms allocated/batch="+(allocationCounterValid?(bytes/batches)+"B":"unavailable")+"\n";
    }
}
