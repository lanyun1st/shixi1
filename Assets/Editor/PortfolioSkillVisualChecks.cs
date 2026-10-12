using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Portfolio.Domain;
using Portfolio.Presentation;
using Portfolio.UI;
using UnityEngine;

public static class PortfolioSkillVisualChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,F).Invoke(o,a);
    static int checks;
    static void Check(bool pass,string why){checks++;if(!pass)throw new Exception("Skill visual: "+why);}
    static void Reset(RuinsGame g,bool shield=false)
    {Set(g,"shieldHero",shield);Call(g,"StartRun");Call(g,"ClearActors");((WaveDirector)Get(g,"waves")).CancelReinforcements();Set(g,"player",new Vector2(1600,1100));Set(g,"cameraFocus",new Vector2(1600,1100));Call(g,"TickFeedback",0f);Call(g,"AnimatePlayer");}
    static object Enemy(RuinsGame g)=>Call(g,"SpawnSlime",new Vector2(1680,1100),500f,false,0f);
    static void Capture(RuinsGame g,string name,bool ui=false)
    {typeof(PortfolioPresentationChecks).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{g,"SkillVisualReview/"+name+".png",1280,720,ui});}
    public static void Run(RuinsGame g)
    {
        checks=0;Directory.CreateDirectory("SkillVisualReview");var cues=new List<ReactionCue>();Action<ReactionCue> listener=c=>cues.Add(c);g.ReactionPresented+=listener;
        try
        {
            Reset(g);var enemy=Enemy(g);Call(g,"Damage",enemy,1000f,false);
            Check(((IList)Get(g,"skillLines")).Count==0,"death no longer draws ring");
            Reset(g);cues.Clear();var skills=(SkillLoadout)Get(g,"loadout");skills.Add(1);skills.Add(2);enemy=Enemy(g);Set(enemy,"slow",1f);Call(g,"Damage",enemy,24f,true);
            Check(cues.Exists(c=>c.Kind==ReactionKind.IceShatter),"ice route emits shards");Check(((IList)Get(g,"skillLines")).Count==0,"ice no outline ring");
            Reset(g);cues.Clear();Call(g,"DashNova");Set(g,"echoDelay",.01f);Set(g,"echoPosition",new Vector2(1600,1100));Call(g,"TickEcho",.02f);
            Check(cues.Exists(c=>c.Kind==ReactionKind.Nova)&&cues.Exists(c=>c.Kind==ReactionKind.Echo),"nova and delayed echo distinct");
            Reset(g,true);cues.Clear();skills=(SkillLoadout)Get(g,"loadout");skills.Add(1);skills.Add(2);Set(g,"room",3);Set(g,"combo",2);Enemy(g);Call(g,"BeginMelee",new Vector2(1700,1100));Call(g,"TickShield",.2f,new Vector2(1700,1100));
            Check(cues.Exists(c=>c.Kind==ReactionKind.HeavySlash)&&cues.Exists(c=>c.Kind==ReactionKind.Slam),"third heavy attack and ground impact");Check(((IList)Get(g,"skillLines")).Count==0,"melee no range line");
            Reset(g,true);cues.Clear();skills=(SkillLoadout)Get(g,"loadout");skills.Add(0);skills.Add(3);Set(g,"momentum",3);Enemy(g);Call(g,"BeginMelee",new Vector2(1700,1100));Call(g,"TickShield",.2f,new Vector2(1700,1100));
            Check(cues.Exists(c=>c.Kind==ReactionKind.ShieldCone)&&cues.Exists(c=>c.Kind==ReactionKind.Slash),"charged retaliation emits cone and slash");
            Reset(g,true);cues.Clear();Set(g,"shieldAim",Vector2.right);((ShieldDefense)Get(g,"shieldDefense")).SetGuard(true);Call(g,"ReceivePlayerHit",20f,Vector2.right,false);
            Check(cues.Exists(c=>c.Kind==ReactionKind.Guard),"frontal block feedback");
            Reset(g,true);cues.Clear();Set(g,"dashDirection",Vector2.right);Enemy(g);Call(g,"TickShieldDash",new Vector2(1640,1100),new Vector2(1700,1100));
            Check(cues.Exists(c=>c.Kind==ReactionKind.DashImpact),"shield dash impact feedback");
            Reset(g);var feedback=(ReactionFeedback)Get(g,"reactionFeedback");
            foreach(ReactionKind kind in Enum.GetValues(typeof(ReactionKind)))
            {
                feedback.Clear();feedback.Play(new ReactionCue(kind,new Vector2(1600,1100),new Vector2(1780,1100),130));feedback.Tick(.10f);
                var item=((IList)Get(feedback,"active"))[0];var mesh=(Mesh)Get(item,"mesh");
                if(kind!=ReactionKind.Chain)
                {Check(mesh.vertexCount>0,kind+" geometry");Check(((LineRenderer)Get(item,"rim")).positionCount==0,kind+" no circle renderer");}
                if(kind==ReactionKind.Slash || kind==ReactionKind.HeavySlash || kind==ReactionKind.IceShatter || kind==ReactionKind.Echo || kind==ReactionKind.Overload)Capture(g,kind.ToString());
                feedback.Tick(1);Check(feedback.Active==0 && mesh.vertexCount==0,kind+" pool reset");
            }
            var ui=UnityEngine.Object.FindObjectOfType<PortfolioCanvasUI>();var state=g.CaptureUiState();state.mode=UiMode.Upgrade;
            Check(UiTypography.Body!=null && UiTypography.Heading!=null,"bundled fonts imported");
            foreach(bool shield in new[]{false,true})
            {
                state.shieldHero=shield;state.upgradeMask=shield?35:3;state.rewardTitle="选择联动组件";state.rewardHint="保留已有强化 · 选择一项，让流派成型";
                ui.Render(state,shield?new[]{2,3,4}:new[]{3,2,4});Canvas.ForceUpdateCanvases();
                for(int id=0;id<9;id++)
                {
                    ui.cards[0].Configure(id,state.upgradeMask,shield);Canvas.ForceUpdateCanvases();
                    var text=ui.cards[0].description;
                    Check(text.preferredHeight<=text.rectTransform.rect.height+1,"body text fits "+shield+"/"+id);
                    Check(text.font==UiTypography.Body,"body font "+id);
                    var synergy=ui.cards[0].transform.Find("Synergy detail").GetComponent<UnityEngine.UI.Text>();
                    Check(synergy.preferredHeight<=synergy.rectTransform.rect.height+1,"synergy fits "+shield+"/"+id);
                }
                ui.cards[0].Configure(shield?2:3,state.upgradeMask,shield);Capture(g,shield?"ShieldCards":"RangedCards",true);
            }
            File.WriteAllText("SkillVisualReview/checks.txt","PASS "+checks+" checks: route events, no combat rings, all cue geometry/reset, bundled fonts, 18 descriptions and synergies fit. Screenshots are deterministic fixtures, not manual combat QA.");
            Debug.Log("PORTFOLIO_SKILL_VISUAL_OK: "+checks+" checks");
        }
        finally{g.ReactionPresented-=listener;Reset(g);}
    }
}
