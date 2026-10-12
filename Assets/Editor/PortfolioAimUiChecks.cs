using System;
using System.Collections;
using System.Reflection;
using Portfolio.Domain;
using Portfolio.UI;
using UnityEngine;
using UnityEngine.UI;

public static class PortfolioAimUiChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,F).Invoke(o,a);
    static int checks;
    static void Check(bool pass,string why){checks++;if(!pass)throw new Exception("Aim/UI: "+why);}
    static void Capture(RuinsGame game,string file,int w=1280,int h=720)
    {typeof(PortfolioPresentationChecks).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{game,"AimUiReview/"+file+".png",w,h,true});}
    public static void Run(RuinsGame game)
    {
        checks=0;System.IO.Directory.CreateDirectory("AimUiReview");Set(game,"shieldHero",false);Call(game,"StartRun");Call(game,"ClearActors");
        ((IList)Get(game,"obstacles")).Clear();Set(game,"player",new Vector2(1600,1100));Set(game,"cameraFocus",new Vector2(1600,1100));Call(game,"TickFeedback",0f);
        var camera=game.GetComponentInChildren<Camera>();var loadout=(SkillLoadout)Get(game,"loadout");
        for(int i=0;i<8;i++)
        {
            Call(game,"ClearPooledActors");Set(game,"cooldown",0f);float a=i*Mathf.PI/4;
            var target=new Vector2(1600,1058)+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*300;
            Call(game,"Fire",target);var shots=(IList)Get(game,"shots");Check(shots.Count==1,"single shot");
            var shot=shots[0];var origin=(Vector2)Get(shot,"p");var velocity=(Vector2)Get(shot,"velocity");
            var muzzle=(Vector2)Call(game,"StaffMuzzle",Get(game,"facing"));
            Check(Vector2.Distance(origin,muzzle)<.01f,"projectile originates at calibrated staff");
            Check(Vector2.Dot((target-origin).normalized,velocity.normalized)>.99999f,"projectile points at exact aim target");
            var view=(SpriteRenderer)Get(shot,"view");Check(Vector3.Distance(view.transform.position,new Vector3(origin.x,720-origin.y,0))<.01f,"render and collision centers agree");
            Call(game,"AnimatePlayer");Call(game,"RefreshUi");if(i%2==0)Capture(game,"Muzzle_"+Get(game,"facing"));
        }
        foreach(float aspect in new[]{16f/9,4f/3})foreach(float zoom in new[]{250f,400f})
        {
            camera.aspect=aspect;camera.orthographicSize=zoom;
            var target=new Vector3(1780,-510,0);var screen=camera.WorldToScreenPoint(target);var world=camera.ScreenToWorldPoint(screen);
            Check(Vector3.Distance(target,world)<.01f,"aim screen/world agreement at zoom and aspect");
        }
        camera.ResetAspect();camera.orthographicSize=400;
        Call(game,"ClearPooledActors");Set(game,"cooldown",0f);loadout.Add(4);Call(game,"Fire",new Vector2(1900,1058));
        var multiple=(IList)Get(game,"shots");Check(multiple.Count==3,"multishot preserved");
        Check((Vector2)Get(multiple[0],"p")== (Vector2)Get(multiple[1],"p") && (Vector2)Get(multiple[1],"p")== (Vector2)Get(multiple[2],"p"),"all pellets share staff socket");
        Call(game,"ClearPooledActors");var charge=(ChargeAttack)Get(game,"chargeAttack");charge.Reset();charge.Begin();charge.Tick(2);Call(game,"ReleaseHeavy",new Vector2(1900,1058));
        var heavy=((IList)Get(game,"shots"))[0];var hp=(Vector2)Get(heavy,"p");Check(Vector2.Distance(hp,(Vector2)Call(game,"StaffMuzzle",Get(game,"facing")))<.01f,"charged shot uses same muzzle");
        var ui=(PortfolioCanvasUI)Get(game,"canvasUI");
        var navigation=Get(ui,"navigation");Call(navigation,"AvoidPlayer",new Rect(-10000,-10000,20,20),1f);
        foreach(var key in new[]{"button_dark","button_gold","tooltip_panel"}){var sprite=Resources.Load<Sprite>("Art/ui/"+key);Check(sprite.pixelsPerUnit==100 && sprite.border.x>0,"authored UI nine-slice units "+key);}
        Check(ui.health.GetComponent<Image>().sprite!=null && ui.pauseButton.image.sprite!=null,"HUD uses authored art");
        var strip=ui.hud.GetComponentInChildren<LoadoutStripView>(true);Check(strip!=null && strip.icons.Length==9,"serialized skill slots");
        Check(ui.hud.GetComponentsInChildren<LoadoutStripView>(true).Length==1,"baked skin does not duplicate runtime slots");
        Check(UiArtSkin.Panel!=null && UiArtSkin.Panel.name=="panel_v2" && UiArtSkin.Panel.border.x>0,"generated panel loaded as nine-slice");
        Check(UiArtSkin.Primary!=null && UiArtSkin.Primary.name=="button_v2","generated primary button loaded");
        int decoratedCount=ui.GetComponentsInChildren<Graphic>(true).Length;UiArtSkin.Apply(ui);UiArtSkin.Apply(ui);
        Check(ui.GetComponentsInChildren<Graphic>(true).Length==decoratedCount,"reapplying skin does not duplicate decorations");
        var state=game.CaptureUiState();state.upgradeMask=9;state.upgrade=3;state.room=2;state.buildName="灼烧核心 + 连锁节点";state.routeName="雷火过载";state.rewardTitle="遗迹已净化 · 选择下一份强化";state.rewardHint="即将进入第三关 · 恢复 25 生命";
        foreach(var mode in new[]{UiMode.Playing,UiMode.Title,UiMode.Upgrade,UiMode.Paused,UiMode.Result})
        {state.mode=mode;state.won=true;ui.Render(state,new[]{1,2,5});Capture(game,mode.ToString());}
        Check(ui.resultStats.preferredHeight<=ui.resultStats.rectTransform.rect.height+1,"result statistics fit both lines");
        state.mode=UiMode.Playing;ui.Render(state,new[]{1,2,5});Check(strip.icons[0].enabled && strip.icons[3].enabled && !strip.icons[1].enabled,"strip follows actual ownership");
        Check(!strip.icons[1].transform.parent.gameObject.activeSelf,"unowned slots do not clutter combat HUD");
        Check(((RectTransform)strip.icons[3].transform.parent).anchoredPosition.x==40,"owned skills pack without holes");
        Capture(game,"Hud_1080p",1920,1080);Capture(game,"Hud_4x3",1024,768);
        state.mode=UiMode.Upgrade;ui.Render(state,new[]{1,2,5});Capture(game,"Upgrade_4x3",1024,768);
        foreach(bool shield in new[]{false,true})for(int i=0;i<9;i++)
        {
            ui.cards[0].Configure(i,511,shield);Canvas.ForceUpdateCanvases();
            Check(ui.cards[0].description.preferredHeight<=ui.cards[0].description.rectTransform.rect.height+1,"description fits "+shield+"/"+i);
            var detail=ui.cards[0].transform.Find("Synergy detail").GetComponent<Text>();
            Check(detail.preferredHeight<=detail.rectTransform.rect.height+1,"synergy fits "+shield+"/"+i);
        }
        state.shieldHero=true;state.mode=UiMode.Title;ui.Render(state,new[]{1,2,5});Capture(game,"Title_Shield");
        var illustration=ui.titleScreen.transform.Find("Hero illustration").GetComponent<Image>();
        Check(illustration.sprite==Resources.Load<Sprite>("UI/Skin/hero_shield_v2"),"menu illustration follows chosen hero");
        Call(game,"StartRun");Debug.Log("PORTFOLIO_AIM_UI_OK: "+checks+" checks; sockets, cursor rays, normal/multi/charged shots, authored art and skill slots.");
    }
}
