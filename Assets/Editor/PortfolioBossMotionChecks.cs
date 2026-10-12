using System;
using System.Reflection;
using Portfolio.UI;
using UnityEngine;

public static class PortfolioBossMotionChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
    static void Set(object o,string n,object value)=>o.GetType().GetField(n,F).SetValue(o,value);
    static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,F).Invoke(o,args);
    static int checks;
    static void Check(bool ok,string why){checks++;if(!ok)throw new Exception("Boss/motion: "+why);}
    public static void Run(RuinsGame game)
    {
        checks=0;Set(game,"shieldHero",false);Call(game,"StartRun");Call(game,"ClearActors");
        var ui=(PortfolioCanvasUI)Get(game,"canvasUI");var boss=ui.elite;
        var state=game.CaptureUiState();state.mode=UiMode.Playing;state.room=3;state.eliteMaximum=420;state.eliteHealth=420;
        state.eliteState="破防蓄力 · 侧向躲避";
        var rect=(RectTransform)boss.transform;
        Check(rect.anchorMin==new Vector2(.5f,1) && rect.pivot==new Vector2(.5f,1),"centered anchor");
        Check(boss.fill.rectTransform.rect.height>=10,"readable bar thickness");
        ui.Render(state,new[]{0,1,2});Canvas.ForceUpdateCanvases();
        Check(boss.status.preferredWidth<=boss.status.rectTransform.rect.width,"long state fits width");
        Check(boss.status.preferredHeight<=boss.status.rectTransform.rect.height,"long state fits height");
        state.eliteHealth=210;ui.Render(state,new[]{0,1,2});
        Check(Mathf.Approximately(boss.fill.fillAmount,.5f),"health fraction exact");
        Check((float)Get(boss,"delayed")>.5f,"damage trail retains lost health");
        state.eliteHealth=999;ui.Render(state,new[]{0,1,2});Check(boss.fill.fillAmount==1,"overheal clamped");
        state.eliteHealth=300;for(int i=0;i<10;i++)Call(boss,"Present",state,.1f);
        Check(boss.GetComponent<CanvasGroup>().alpha==1,"fade in completes");
        UiArtSkin.Apply(ui);UiArtSkin.Apply(ui);
        Check(boss.transform.Find("Damage trail").GetSiblingIndex()<boss.fill.transform.GetSiblingIndex(),"restyling preserves trail order");
        System.IO.Directory.CreateDirectory("BossMotionReview");
        typeof(PortfolioPresentationChecks).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{game,"BossMotionReview/Boss.png",1280,720,true});
        typeof(PortfolioPresentationChecks).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{game,"BossMotionReview/BossWide.png",1920,800,true});
        state.eliteHealth=0;Call(boss,"Present",state,.05f);Check(boss.gameObject.activeSelf && boss.GetComponent<CanvasGroup>().alpha<1,"death fades rather than pops");
        Call(boss,"Present",state,.3f);Check(!boss.gameObject.activeSelf,"death fade completes");
        state.room=1;ui.Render(state,new[]{0,1,2});Check(!boss.gameObject.activeSelf,"room reset hides immediately");
        Call(game,"ResetHeroMotion");Call(game,"TickHeroMotion",17f,.1f);
        var motion=(Portfolio.Presentation.HeroMotionPresenter)Get(game,"heroMotion");
        Check(Mathf.Abs(motion.StridePhase-Mathf.PI/2)<.001f,"distance driven stride");
        float stride=motion.StridePhase;Call(game,"TickHeroMotion",0f,.1f);
        Check(motion.StridePhase==stride,"blocked movement does not advance stride");
        Call(game,"TickHeroMotion",17f,.1f);Set(game,"shotAge",.06f);Call(game,"AnimatePlayer");
        var view=(SpriteRenderer)Get(game,"heroView");Vector3 position=view.transform.position;Call(game,"AnimatePlayer");
        Check(Vector3.Distance(position,view.transform.position)<.001f,"visual offsets do not accumulate");
        Set(game,"shieldHero",true);
        foreach(float duration in new[]{.48f,.38f})
        {
            Set(game,"meleeDuration",duration);Set(game,"meleeRemaining",duration*.7f);
            Check((float)game.GetType().GetProperty("MeleeProgress",F).GetValue(game)<.375f,"windup before hit");
            Set(game,"meleeRemaining",duration*.6f);
            Check((float)game.GetType().GetProperty("MeleeProgress",F).GetValue(game)>.375f,"hit timing scales with attack speed");
        }
        Set(game,"shieldHero",false);Call(game,"StartRun");
        Debug.Log("PORTFOLIO_BOSS_MOTION_CHECKS_OK: "+checks+" checks");
    }
}
