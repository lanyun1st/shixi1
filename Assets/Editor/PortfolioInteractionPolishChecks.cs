using System;
using System.Collections;
using System.Reflection;
using Portfolio.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class PortfolioInteractionPolishChecks
{
    const BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,F).Invoke(o,a);
    static int checks;
    static void Check(bool pass,string why){checks++;if(!pass)throw new Exception("Interaction polish: "+why);}
    static void Capture(RuinsGame g,string name)
    {typeof(PortfolioPresentationChecks).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{g,"InteractionReview/"+name+".png",1280,720,true});}
    public static void Run(RuinsGame game)
    {
        checks=0;System.IO.Directory.CreateDirectory("InteractionReview");Call(game,"StartRun");Call(game,"ClearActors");
        var ui=(PortfolioCanvasUI)Get(game,"canvasUI");var state=game.CaptureUiState();var choices=new[]{0,1,5};
        state.mode=UiMode.Playing;ui.Render(state,choices);state.mode=UiMode.Upgrade;ui.Render(state,choices);
        Check(EventSystem.current.currentSelectedGameObject==null,"no default selected skill");
        var pointer=new PointerEventData(EventSystem.current);var card=ui.cards[0];
        card.OnSelect(new BaseEventData(EventSystem.current));Call(card,"Update");
        Check(!(bool)Get(card,"hover") && card.transform.localScale==Vector3.one,"selection does not highlight");
        card.OnPointerEnter(pointer);Call(card,"Update");Check((bool)Get(card,"hover"),"pointer enters card");
        card.OnPointerExit(pointer);ui.cards[1].OnPointerEnter(pointer);
        Check(!(bool)Get(card,"hover") && (bool)Get(ui.cards[1],"hover"),"hover transfers to pointer target");
        ui.cards[1].OnPointerExit(pointer);
        state.mode=UiMode.Playing;ui.Render(state,choices);state.mode=UiMode.Upgrade;ui.Render(state,choices);
        foreach(var item in ui.cards)Check(!(bool)Get(item,"hover") && item.button.transition==Selectable.Transition.None,"reopen clears highlights");
        Canvas.ForceUpdateCanvases();Capture(game,"CardsNeutral");
        state.mode=UiMode.Playing;ui.Render(state,choices);Canvas.ForceUpdateCanvases();
        var navigation=Get(ui,"navigation");var root=(RectTransform)Get(navigation,"root");var group=root.GetComponent<CanvasGroup>();
        var corners=new Vector3[4];root.GetWorldCorners(corners);var canvas=root.GetComponentInParent<Canvas>();
        var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
        var p=RectTransformUtility.WorldToScreenPoint(camera,(corners[0]+corners[2])*.5f);
        Call(navigation,"AvoidPlayer",new Rect(p-Vector2.one*40,Vector2.one*80),.016f);
        Check(group.alpha==0 && !group.blocksRaycasts,"minimap fully yields to overlapping sprite");
        Call(navigation,"AvoidPlayer",new Rect(-10000,-10000,20,20),1f);Check(group.alpha==1,"minimap restores away from player");
        Set(game,"player",new Vector2(3090,2140));Set(game,"cameraFocus",new Vector2(3090,2140));
        Call(game,"AnimatePlayer");Call(game,"TickFeedback",0f);Call(game,"RefreshUi");Capture(game,"BottomRightSafe");
        Check(group.alpha==0,"actual bottom-right character bounds hide minimap");
        Call(game,"SpawnSlime",new Vector2(3000,1950),100f,false,1f);
        var enemy=((IList)Get(game,"enemies"))[0];
        Call(game,"TickEntryWaves",.1f);var marks=Get(enemy,"spawnMarks");
        Check(marks!=null && ((IList)Get(game,"skillLines")).Count==2,"one marker lease per stroke");
        Check(!((SpriteRenderer)Get(enemy,"view")).enabled,"early warning hides enemy");
        for(int i=0;i<8;i++){Call(game,"TickPooledLines",.1f);Call(game,"TickEntryWaves",.1f);}
        Check(ReferenceEquals(marks,Get(enemy,"spawnMarks")) && ((IList)Get(game,"skillLines")).Count==2,"warning reuses strokes without pulse bursts");
        Check(((SpriteRenderer)Get(enemy,"view")).enabled && ((SpriteRenderer)Get(enemy,"view")).color.a<1,"short arrival fade");
        Call(game,"Damage",enemy,50f,false);Check((float)Get(enemy,"hp")==100,"preview stays invulnerable");
        Capture(game,"SpawnCross");Call(game,"TickEntryWaves",.11f);Call(game,"TickPooledLines",.01f);
        Check((float)Get(enemy,"spawnDelay")==0 && Get(enemy,"spawnMarks")==null && ((IList)Get(game,"skillLines")).Count==0,"warning returned at activation");
        Check(((SpriteRenderer)Get(enemy,"view")).color.a==1,"arrival fully visible");
        Call(game,"ClearActors");Call(game,"StartRun");
        Debug.Log("PORTFOLIO_INTERACTION_POLISH_OK: "+checks+" checks; pointer-only cards, minimap avoidance, persistent spawn preview and leases.");
    }
}
