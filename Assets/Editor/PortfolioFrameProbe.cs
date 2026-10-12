using System;
using System.IO;
using System.Reflection;
using Portfolio.Domain;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.Profiling;

// Synthetic real-frame stress capture. Editor timings must not be advertised as player FPS.
public static class PortfolioFrameProbe
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    static RuinsGame game;static Action<Vector2> fire;static double started,lastTime;static int frames,lastFrame=-1;
    static readonly double[] wall=new double[360],main=new double[360],gc=new double[360];
    static ProfilerRecorder mainRecorder,gcRecorder;
    static bool oldEnabled;static EnterPlayModeOptions oldOptions;static int oldVsync,oldRate;static float oldCapture;
    static object Get(string n)=>typeof(RuinsGame).GetField(n,F).GetValue(game);
    static void Set(string n,object value)=>typeof(RuinsGame).GetField(n,F).SetValue(game,value);
    static object Call(string n,params object[] args)=>typeof(RuinsGame).GetMethod(n,F).Invoke(game,args);
    public static void Run()
    {
        PortfolioSetup.CreateScene();EditorSceneManager.OpenScene("Assets/Scenes/Ruins.unity");
        oldEnabled=EditorSettings.enterPlayModeOptionsEnabled;oldOptions=EditorSettings.enterPlayModeOptions;
        EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
        oldVsync=QualitySettings.vSyncCount;oldRate=Application.targetFrameRate;oldCapture=Time.captureDeltaTime;
        QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;Time.captureDeltaTime=1f/60;
        started=EditorApplication.timeSinceStartup;EditorApplication.update+=Tick;EditorApplication.isPlaying=true;
    }
    static void Tick()
    {
        if(EditorApplication.timeSinceStartup-started>150){End(1,"Frame probe timeout");return;}
        if(!EditorApplication.isPlaying || EditorApplication.timeSinceStartup-started<3)return;
        try
        {
            if(game==null)
            {
                game=UnityEngine.Object.FindObjectOfType<RuinsGame>();UnityEngine.Random.InitState(4701);
                // Entering Play Mode resets Time settings; apply the fixed simulation step here.
                Time.captureDeltaTime=1f/60;QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;
                Call("StartRun");Set("room",3);Call("SpawnRoom");Call("ClearActors");((WaveDirector)Get("waves")).CancelReinforcements();
                Set("player",new Vector2(1600,1100));Set("cameraFocus",new Vector2(1600,1100));Set("invincible",10000f);
                var skills=(SkillLoadout)Get("loadout");for(int i=0;i<9;i++)skills.Add(i);
                for(int i=0;i<60;i++)Call("SpawnSlime",new Vector2(1250+(i%10)*70,800+(i/10)*100),1000000f,false,0f);
                fire=(Action<Vector2>)Delegate.CreateDelegate(typeof(Action<Vector2>),game,typeof(RuinsGame).GetMethod("Fire",F));
                mainRecorder=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Main Thread",1);
                gcRecorder=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame",1);
                lastFrame=Time.frameCount;lastTime=EditorApplication.timeSinceStartup;return;
            }
            if(Time.frameCount==lastFrame)return;lastFrame=Time.frameCount;
            double now=EditorApplication.timeSinceStartup,delta=(now-lastTime)*1000;lastTime=now;
            fire(new Vector2(1600+Mathf.Cos(frames*.025f)*400,1100+Mathf.Sin(frames*.025f)*300));
            if(frames>=120){int i=frames-120;wall[i]=delta;main[i]=mainRecorder.Valid?mainRecorder.LastValue/1000000d:-1;gc[i]=gcRecorder.Valid?gcRecorder.LastValue:-1;}
            if(++frames<480)return;
            string report="Unity "+Application.unityVersion+" | "+SystemInfo.processorType+" | "+SystemInfo.graphicsDeviceName+"\nEditor/batch rendered-frame fixture. 60 high-HP slimes, all 9 ranged skills, automatic fire. 120 warmup + 360 samples. Simulation fixed 1/60; wall timings measured separately. Editor overhead included, not player FPS or manual gameplay.\n";
            report+=Stats("Frame interval ms",wall)+Stats("Main Thread recorder ms (valid="+mainRecorder.Valid+")",main)+Stats("GC Allocated In Frame bytes (valid="+gcRecorder.Valid+")",gc);
            report+="Shots="+Get("fired")+" Hits="+Get("hits")+"\n"+game.PoolDiagnostics+"\n";
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-probe-output");File.WriteAllText(at>=0?args[at+1]:"frame-probe.txt",report);End(0,"PORTFOLIO_FRAME_PROBE_OK\n"+report);
        }
        catch(Exception e){End(1,e.ToString());}
    }
    static string Stats(string name,double[] data){Array.Sort(data);return name+": median="+data[180].ToString("F2")+" p95="+data[341].ToString("F2")+" max="+data[359].ToString("F2")+"\n";}
    static void End(int code,string report)
    {
        EditorApplication.update-=Tick;mainRecorder.Dispose();gcRecorder.Dispose();QualitySettings.vSyncCount=oldVsync;Application.targetFrameRate=oldRate;Time.captureDeltaTime=oldCapture;
        EditorSettings.enterPlayModeOptionsEnabled=oldEnabled;EditorSettings.enterPlayModeOptions=oldOptions;
        if(code==0)Debug.Log(report);else Debug.LogError(report);EditorApplication.Exit(code);
    }
}
