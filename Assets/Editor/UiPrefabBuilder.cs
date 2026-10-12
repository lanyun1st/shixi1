using System.IO;
using Portfolio.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad]
public static class UiPrefabBuilder
{
    public const string Path="Assets/Resources/UI/PortfolioCanvas.prefab";
    static readonly Color Ink=new Color(.055f,.095f,.12f,.97f), Panel=new Color(.095f,.16f,.19f,.97f), Gold=new Color(.94f,.72f,.37f), Muted=new Color(.61f,.73f,.73f);
    static UiPrefabBuilder(){EditorApplication.delayCall+=Ensure;}
    public static void Ensure(){if(AssetDatabase.LoadAssetAtPath<GameObject>(Path)==null)Build();}
    [MenuItem("Portfolio/UI/Rebuild Generated Canvas Prefab")]
    public static void Build()
    {
        Directory.CreateDirectory("Assets/Resources/UI");
        var root=new GameObject("Portfolio Canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=32760;
        var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
        var view=root.AddComponent<PortfolioCanvasUI>();
        var events=new GameObject("UI Event System",typeof(EventSystem),typeof(StandaloneInputModule));events.transform.SetParent(root.transform,false);
        var frame=Node(root.transform,"Reference Frame",0,0,1280,720);frame.anchorMin=frame.anchorMax=new Vector2(.5f,.5f);frame.pivot=new Vector2(.5f,.5f);frame.anchoredPosition=Vector2.zero;
        var hud=Node(frame,"HUD",0,0,1280,720);view.hud=hud.gameObject;
        var plate=Image(hud,"Health Panel",24,22,356,88,Ink);
        Image(plate.transform,"Accent",0,0,3,88,Gold);
        var avatar=Image(plate.transform,"Portrait",16,10,66,66,Color.white);avatar.sprite=Art("ui/portrait");avatar.preserveAspect=true;
        Label(plate.transform,"Name",94,12,220,24,"遗迹修复师",18,Color.white);
        var hv=plate.gameObject.AddComponent<HealthBarView>();view.health=hv;
        hv.value=Label(plate.transform,"Health Value",224,15,114,22,"100 / 100",15,Muted,TextAnchor.MiddleRight);
        Image(plate.transform,"Health Track",94,50,242,15,new Color(.02f,.05f,.06f));
        hv.fill=Fill(plate.transform,"Health Fill",96,52,238,11,new Color(.87f,.26f,.20f));
        var dashPanel=Image(hud,"Dash Panel",394,22,148,88,Ink);var dv=dashPanel.gameObject.AddComponent<DashCooldownView>();view.dash=dv;
        var dashIcon=Image(dashPanel.transform,"Dash Icon",12,14,54,54,Color.white);dashIcon.sprite=Art("ui/skill_pierce");dashIcon.preserveAspect=true;
        dv.mask=Fill(dashPanel.transform,"Cooldown Mask",12,14,54,54,new Color(.01f,.025f,.04f,.8f));dv.mask.fillMethod=UnityEngine.UI.Image.FillMethod.Radial360;
        Label(dashPanel.transform,"Key",75,15,64,22,"空格",16,Muted);
        dv.status=Label(dashPanel.transform,"Cooldown",75,43,66,27,"就绪",20,Gold);
        var buff=Image(hud,"Upgrade Panel",556,22,216,88,Ink);
        view.buffIcon=Image(buff.transform,"Upgrade Icon",12,16,52,52,Color.white);view.buffIcon.preserveAspect=true;
        Label(buff.transform,"Caption",76,12,128,24,"核心强化",14,Muted);
        view.buffName=Label(buff.transform,"Name",76,39,135,32,"暂无强化",20,Gold);
        var objective=Image(hud,"Objective Panel",786,22,402,88,Ink);var ov=objective.gameObject.AddComponent<RoomObjectiveView>();view.objective=ov;
        ov.roomLabel=Label(objective.transform,"Room",18,10,360,25,"遗迹回廊 / 01 — 03",16,Muted);
        ov.objective=Label(objective.transform,"Objective",18,42,362,28,"清理异常体",20,Color.white);
        view.pauseButton=Button(hud,"Pause",1200,22,56,88,"II");
        var elite=Image(hud,"Elite Status",408,124,464,66,Ink);var ev=elite.gameObject.AddComponent<EliteStatusView>();view.elite=ev;
        Label(elite.transform,"Elite Name",16,6,244,22,"精英 · 遗迹冲锋守卫",17,Gold);
        ev.status=Label(elite.transform,"State",248,6,200,22,"追击",15,Muted,TextAnchor.MiddleRight);
        Image(elite.transform,"Track",16,39,432,10,new Color(.025f,.045f,.055f));ev.fill=Fill(elite.transform,"Fill",16,39,432,10,Gold);
        var prompt=Image(hud,"Portal Prompt",465,208,350,44,Ink);ov.promptPanel=prompt.gameObject;ov.prompt=Label(prompt.transform,"Text",8,4,334,36,"E · 进入传送门",20,Gold,TextAnchor.MiddleCenter);
        Label(hud,"Controls",180,682,920,24,"WASD 移动    ·    左键攻击    ·    空格冲刺    ·    E 交互    ·    ESC 暂停",15,Muted,TextAnchor.MiddleCenter);
        view.chargeStatus=Label(hud,"Charge Status",310,654,660,25,"按住右键蓄力 · 松开释放 · 冲刺可取消",17,Gold,TextAnchor.MiddleCenter);
        view.enemyBars=new UnityEngine.UI.Image[16];
        for(int i=0;i<16;i++){var bar=Image(hud,"Enemy Bar "+i,0,0,50,5,Ink);view.enemyBars[i]=Fill(bar.transform,"Fill",0,0,50,5,new Color(.85f,.3f,.2f));bar.gameObject.SetActive(false);}
        var toast=Image(hud,"Acquired Toast",420,624,440,42,Panel);view.toastPanel=toast.gameObject;view.toast=Label(toast.transform,"Message",8,2,424,38,"已获得强化",20,Gold,TextAnchor.MiddleCenter);
        var modal=Image(root.transform,"Modal Blocker",0,0,1280,720,new Color(.015f,.03f,.045f,.85f));
        var mrt=modal.rectTransform;mrt.anchorMin=Vector2.zero;mrt.anchorMax=Vector2.one;mrt.offsetMin=mrt.offsetMax=Vector2.zero;modal.raycastTarget=true;view.modal=modal.gameObject;
        var screen=Node(modal.transform,"Modal Frame",0,0,1280,720);screen.anchorMin=screen.anchorMax=new Vector2(.5f,.5f);screen.pivot=new Vector2(.5f,.5f);screen.anchoredPosition=Vector2.zero;
        var title=ScreenPanel(screen,"Title Screen",280,155,720,440);view.titleScreen=title.gameObject;
        Label(title,"Kicker",30,28,660,24,"R U I N S   /   遗 迹 行 动",15,Gold,TextAnchor.MiddleCenter);
        Label(title,"Title",30,72,660,72,"遗迹工坊",48,Color.white,TextAnchor.MiddleCenter);
        Label(title,"Description",60,165,600,64,"净化三个房间，选择核心强化。\n利用地形，击败遗迹深处的石甲守卫。",22,Muted,TextAnchor.MiddleCenter);
        Label(title,"Guide",50,250,620,32,"WASD 移动   左键射击   右键蓄力   空格冲刺",18,Color.white,TextAnchor.MiddleCenter);
        view.startButton=Button(title,"Start",210,322,300,60,"进入遗迹",true);
        var pause=ScreenPanel(screen,"Pause Screen",370,200,540,340);view.pauseScreen=pause.gameObject;
        Label(pause,"Title",25,32,490,62,"探索已暂停",34,Color.white,TextAnchor.MiddleCenter);
        Label(pause,"Hint",25,104,490,30,"战斗和冷却已冻结",18,Muted,TextAnchor.MiddleCenter);
        view.resumeButton=Button(pause,"Resume",100,170,340,56,"继续探索",true);
        view.restartPauseButton=Button(pause,"Restart",100,242,340,52,"重新开始本局");
        var upgrades=ScreenPanel(screen,"Upgrade Screen",118,105,1044,520);view.upgradeScreen=upgrades.gameObject;
        Label(upgrades,"Title",24,20,996,46,"选择核心强化",32,Color.white,TextAnchor.MiddleCenter);
        Label(upgrades,"Hint",24,70,996,28,"叠加本局强化 · 进入下一房间，并恢复 25 点生命",18,Muted,TextAnchor.MiddleCenter);
        view.cards=new UpgradeCardView[3];
        for(int i=0;i<3;i++)
        {
            var button=Button(upgrades,"Upgrade Card "+i,30+i*338,128,308,346,"");var card=button.gameObject.AddComponent<UpgradeCardView>();card.button=button;
            card.icon=Image(button.transform,"Icon",114,26,80,80,Color.white);card.icon.preserveAspect=true;
            card.title=Label(button.transform,"Name",14,122,280,40,"强化",25,Gold,TextAnchor.MiddleCenter);
            card.description=Label(button.transform,"Description",22,167,264,124,"效果说明",16,Color.white,TextAnchor.MiddleCenter);
            Label(button.transform,"Select",16,294,276,30,"选择此核心  →",18,Gold,TextAnchor.MiddleCenter);view.cards[i]=card;
        }
        var result=ScreenPanel(screen,"Result Screen",280,145,720,450);view.resultScreen=result.gameObject;
        Label(result,"Kicker",30,25,660,24,"探索记录 / EXPEDITION",15,Gold,TextAnchor.MiddleCenter);
        view.resultTitle=Label(result,"Title",30,66,660,60,"遗迹已稳定",36,Color.white,TextAnchor.MiddleCenter);
        view.resultStats=Label(result,"Stats",60,155,600,98,"统计",22,Color.white,TextAnchor.MiddleCenter);
        view.resultUpgrade=Label(result,"Upgrade",60,273,600,34,"本局强化",20,Muted,TextAnchor.MiddleCenter);
        view.restartResultButton=Button(result,"Restart",210,344,300,58,"再探索一次",true);
        pause.gameObject.SetActive(false);upgrades.gameObject.SetActive(false);result.gameObject.SetActive(false);hud.gameObject.SetActive(false);
        PrefabUtility.SaveAsPrefabAsset(root,Path);Object.DestroyImmediate(root);AssetDatabase.SaveAssets();
        Debug.Log("PORTFOLIO_UI_PREFAB_OK");
    }
    static Sprite Art(string key)=>AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Art/"+key+".png");
    static RectTransform Node(Transform parent,string name,float x,float y,float w,float h)
    {
        var go=new GameObject(name,typeof(RectTransform));var rt=go.GetComponent<RectTransform>();rt.SetParent(parent,false);
        rt.anchorMin=rt.anchorMax=new Vector2(0,1);rt.pivot=new Vector2(0,1);rt.anchoredPosition=new Vector2(x,-y);rt.sizeDelta=new Vector2(w,h);return rt;
    }
    static UnityEngine.UI.Image Image(Transform p,string n,float x,float y,float w,float h,Color color)
    {
        var image=Node(p,n,x,y,w,h).gameObject.AddComponent<UnityEngine.UI.Image>();image.color=color;image.raycastTarget=false;return image;
    }
    static UnityEngine.UI.Image Fill(Transform p,string n,float x,float y,float w,float h,Color color)
    {
        var image=Image(p,n,x,y,w,h,color);image.sprite=Solid();image.type=UnityEngine.UI.Image.Type.Filled;image.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;return image;
    }
    static Sprite Solid()
    {
        const string asset="Assets/Resources/UI/SolidSprite.asset";
        var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(asset);if(sprite!=null)return sprite;
        var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);texture.name="UI Solid";texture.SetPixels(new[]{Color.white,Color.white,Color.white,Color.white});texture.Apply();
        AssetDatabase.CreateAsset(texture,asset);sprite=Sprite.Create(texture,new Rect(0,0,2,2),new Vector2(.5f,.5f),1);sprite.name="Solid";AssetDatabase.AddObjectToAsset(sprite,asset);AssetDatabase.SaveAssets();return sprite;
    }
    static Text Label(Transform p,string n,float x,float y,float w,float h,string text,int size,Color color,TextAnchor align=TextAnchor.MiddleLeft)
    {
        var label=Node(p,n,x,y,w,h).gameObject.AddComponent<Text>();label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.fontSize=size;label.text=text;label.color=color;label.alignment=align;label.raycastTarget=false;label.horizontalOverflow=HorizontalWrapMode.Wrap;label.verticalOverflow=VerticalWrapMode.Truncate;return label;
    }
    static Button Button(Transform p,string n,float x,float y,float w,float h,string text,bool primary=false)
    {
        var image=Image(p,n,x,y,w,h,primary?new Color(.4f,.29f,.14f):Panel);image.raycastTarget=true;
        var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;
        var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.35f,1.35f,1.2f);colors.selectedColor=colors.highlightedColor;colors.pressedColor=new Color(.72f,.72f,.65f);colors.fadeDuration=.1f;button.colors=colors;
        Image(image.transform,"Gold Edge",0,0,w,2,Gold);
        if(text.Length>0)Label(image.transform,"Label",5,3,w-10,h-6,text,22,Color.white,TextAnchor.MiddleCenter);
        return button;
    }
    static RectTransform ScreenPanel(Transform p,string n,float x,float y,float w,float h)
    {
        var panel=Image(p,n,x,y,w,h,Ink);Image(panel.transform,"Accent",0,0,w,3,Gold);return panel.rectTransform;
    }
}
