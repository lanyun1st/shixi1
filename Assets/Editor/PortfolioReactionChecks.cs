using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Portfolio.Domain;
using Portfolio.Presentation;
using UnityEngine;

public static class PortfolioReactionChecks
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    static object Get(object o,string n)=>o.GetType().GetField(n,Flags).GetValue(o);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,Flags).SetValue(o,v);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,Flags).Invoke(o,a);
    static int checks;
    static void Check(bool ok,string reason){checks++;if(!ok)throw new Exception("Reaction: "+reason);}
    static void Reset(RuinsGame g)
    {
        Set(g,"shieldHero",false);Call(g,"StartRun");Call(g,"ClearActors");
        ((WaveDirector)Get(g,"waves")).CancelReinforcements();
        Set(g,"player",new Vector2(1500,1150));Set(g,"cameraFocus",new Vector2(1600,1100));
    }
    public static void Run(RuinsGame g)
    {
        checks=0;Reset(g);var feedback=(ReactionFeedback)Get(g,"reactionFeedback");
        var cues=new List<ReactionCue>();Action<ReactionCue> collect=c=>cues.Add(c);g.ReactionPresented+=collect;
        try
        {
            var loadout=(SkillLoadout)Get(g,"loadout");loadout.Add(0);loadout.Add(3);
            var first=Call(g,"SpawnSlime",new Vector2(1600,1100),500f,false,0f);
            var second=Call(g,"SpawnSlime",new Vector2(1680,1100),500f,false,0f);
            Call(g,"Damage",first,24f,true);
            Check(cues.Count==1 && cues[0].Kind==ReactionKind.Chain,"unburned hit only chains");
            Check(Mathf.Abs((float)Get(second,"hp")-484)<.01f,"chain damage remains 16");
            Check(cues[0].To==(Vector2)Get(second,"p"),"arc ends at actual damaged target");
            cues.Clear();Call(g,"Damage",first,24f,true);
            Check(cues.Count==2 && cues[0].Kind==ReactionKind.Overload && cues[1].Kind==ReactionKind.Chain,"burning hit emits one overload and one chain");
            Check(Mathf.Abs((float)Get(second,"hp")-448)<.01f,"blast plus chain remain 20 plus 16");
            Check(cues[0].Radius==110,"visual radius matches damage radius");
            feedback.Tick(.065f);Capture(g,"ReactionReview/OverloadChain.png");
            feedback.Tick(.085f);Capture(g,"ReactionReview/OverloadExpansion.png");
            feedback.Tick(.16f);Capture(g,"ReactionReview/OverloadFade.png");
            cues.Clear();Call(g,"Damage",first,24f,true);
            Check(cues.Count==1 && cues[0].Kind==ReactionKind.Chain,"reaction cooldown suppresses explosion");
            feedback.Enabled=false;feedback.Tick(0);cues.Clear();Set(first,"reaction",0f);
            float before=(float)Get(second,"hp");Call(g,"Damage",first,24f,true);
            Check(Mathf.Abs(before-(float)Get(second,"hp")-36)<.01f && feedback.Active==0,"disabled presentation preserves damage");
            feedback.Enabled=true;feedback.Clear();cues.Clear();Call(g,"ClearActors");
            first=Call(g,"SpawnSlime",new Vector2(1600,1100),500f,false,0f);Call(g,"Damage",first,24f,true);
            Check(cues.Count==0,"no fake chain without valid recipient");
            Call(g,"ClearActors");cues.Clear();
            first=Call(g,"SpawnSlime",new Vector2(1500,1100),500f,false,0f);
            second=Call(g,"SpawnSlime",new Vector2(1700,1100),500f,false,0f);
            Call(g,"AddObstacle","column",new Vector2(1600,1100),new Vector2(62,60),-1,75f);
            Call(g,"Damage",first,24f,true);
            Check(cues.Count==0 && (float)Get(second,"hp")==500,"wall blocks damage and arc together");
            Set(first,"hp",0f);Call(g,"Damage",first,24f,true);
            Check(cues.Count==0,"already dead enemy emits no cue");
            var cue=new ReactionCue(ReactionKind.Chain,new Vector2(1600,1100),new Vector2(1700,1100));
            feedback.Play(cue);Set(g,"phase",Enum.Parse(Get(g,"phase").GetType(),"Paused"));Call(g,"TickFeedback",1f);
            Check(feedback.Active==1,"paused game freezes effect lifetime");
            Set(g,"phase",Enum.Parse(Get(g,"phase").GetType(),"Playing"));feedback.Clear();int created=feedback.Created;
            for(int i=0;i<500;i++){feedback.Play(cue);feedback.Tick(.3f);}
            Check(feedback.Active==0 && feedback.Created==created,"500 cycles reuse pooled renderers");
            for(int i=0;i<100;i++)feedback.Play(cue);
            Check(feedback.Active==48,"visual burst capped independently of damage");
            Call(g,"ClearActors");Check(feedback.Active==0 && feedback.ShakeAmplitude==0,"room reset clears feedback");
            File.WriteAllText("ReactionReview/checks.txt","PASS "+checks+" checks: real hit targets, unchanged damage, cooldown, disabled effects, pause, 500 reuse cycles, burst cap and reset. Screenshot is a deterministic fixture; audio has not been manually auditioned.");
            Debug.Log("PORTFOLIO_REACTION_OK: "+checks+" checks");
        }
        finally{g.ReactionPresented-=collect;feedback.Enabled=true;Reset(g);}
    }
    static void Capture(RuinsGame g,string path)
    {
        Directory.CreateDirectory("ReactionReview");Call(g,"TickFeedback",0f);Call(g,"AnimatePlayer");
        var camera=g.GetComponentInChildren<Camera>();camera.orthographicSize=400;camera.aspect=16f/9;
        var rt=new RenderTexture(1280,720,24);rt.Create();var previous=camera.targetTexture;var active=RenderTexture.active;
        var image=new Texture2D(1280,720,TextureFormat.RGBA32,false);
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}
        finally{camera.targetTexture=previous;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);}
    }
}
