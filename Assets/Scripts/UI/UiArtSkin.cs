using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.UI
{
    // Presentation tokens and idempotent layout. No access to combat state.
    public static class UiArtSkin
    {
        public static readonly Color Paper=new Color(.94f,.91f,.83f),Muted=new Color(.60f,.69f,.70f),Gold=new Color(.88f,.70f,.39f),Ink=new Color(.055f,.09f,.11f);
        public static Sprite Panel=>Resources.Load<Sprite>("UI/Skin/panel_v2")??Resources.Load<Sprite>("UI/Skin/tooltip_panel");
        public static Sprite Primary=>Resources.Load<Sprite>("UI/Skin/button_v2")??Panel;
        public static void Rect(Transform t,float x,float y,float w,float h)
        {if(t==null)return;var r=(RectTransform)t;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
        static void Child(Transform p,string n,float x,float y,float w,float h)=>Rect(p.Find(n),x,y,w,h);
        static void Bottom(Transform t,float x,float y,float w,float h)
        {Rect(t,x,0,w,h);var r=(RectTransform)t;r.anchorMin=r.anchorMax=r.pivot=Vector2.zero;r.anchoredPosition=new Vector2(x,y);}
        public static void Skin(Image image,bool primary=false,float cornerScale=4)
        {if(image==null)return;image.sprite=primary?Primary:Panel;image.type=Image.Type.Sliced;image.fillCenter=true;image.preserveAspect=false;image.pixelsPerUnitMultiplier=cornerScale;image.color=Color.white;}
        static void Hide(Transform p,string n){var t=p.Find(n);if(t!=null)t.gameObject.SetActive(false);}
        static void EdgesOff(Transform p){for(int i=0;i<4;i++)Hide(p,"Frame edge "+i);Hide(p,"Gold Edge");Hide(p,"Accent");}
        public static Image Plate(Transform p,string n,float x,float y,float w,float h,Color color)
        {
            var t=p.Find(n);if(t==null){var go=new GameObject(n,typeof(RectTransform),typeof(Image));go.transform.SetParent(p,false);t=go.transform;}
            Rect(t,x,y,w,h);var image=t.GetComponent<Image>();image.color=color;image.raycastTarget=false;return image;
        }
        public static Text Label(Transform p,string n,float x,float y,float w,float h,int size,Color color,TextAnchor align=TextAnchor.MiddleLeft)
        {
            var t=p.Find(n);if(t==null){var go=new GameObject(n,typeof(RectTransform),typeof(Text));go.transform.SetParent(p,false);t=go.transform;}
            Rect(t,x,y,w,h);var text=t.GetComponent<Text>();text.font=size>=26?UiTypography.Heading:UiTypography.Body;text.fontSize=size;text.color=color;text.alignment=align;text.raycastTarget=false;text.lineSpacing=1.2f;text.supportRichText=false;return text;
        }
        static void ButtonAt(Button button,float x,float y,float w,float h,bool primary)
        {
            Rect(button.transform,x,y,w,h);Skin(button.image,primary,3.8f);EdgesOff(button.transform);
            var label=button.transform.Find("Label");if(label!=null)Label(button.transform,"Label",16,4,w-32,h-8,18,primary?new Color(.17f,.12f,.06f):Paper,TextAnchor.MiddleCenter);
        }
        static void Modal(GameObject panel,float x,float y,float w,float h)
        {
            Rect(panel.transform,x,y,w,h);panel.GetComponent<Image>().color=Color.clear;EdgesOff(panel.transform);
            var frame=Plate(panel.transform,"Painted stone frame",0,0,w,h,Color.white);Skin(frame);frame.transform.SetAsFirstSibling();
        }
        public static void Apply(PortfolioCanvasUI ui)
        {
            foreach(var button in ui.GetComponentsInChildren<Button>(true))
            {
                Skin(button.image);EdgesOff(button.transform);
                var c=button.colors;c.normalColor=Color.white;c.highlightedColor=new Color(1.15f,1.15f,1.12f);c.selectedColor=Color.white;c.pressedColor=new Color(.7f,.72f,.72f);c.disabledColor=new Color(.4f,.45f,.46f,.65f);c.fadeDuration=.12f;button.colors=c;
            }
            ui.modal.GetComponent<Image>().color=new Color(.012f,.024f,.029f,.9f);
            var health=ui.health.transform;Rect(health,24,24,306,80);Skin(health.GetComponent<Image>(),false,5);EdgesOff(health);
            Child(health,"Portrait",10,8,64,64);Child(health,"Name",87,12,193,23);
            Label(health,"Name",87,12,193,23,16,Paper);
            Label(health,"Health Value",89,45,194,22,12,Paper,TextAnchor.MiddleCenter).transform.SetAsLastSibling();
            var track=health.Find("Health Track").GetComponent<Image>();track.color=new Color(.2f,.105f,.09f);Child(health,"Health Track",86,44,198,24);
            Child(health,"Health Fill",88,46,194,20);ui.health.fill.color=new Color(.68f,.23f,.18f);
            Plate(health,"Bar glint",88,46,194,1,new Color(1,.63f,.41f,.6f));
            ui.health.value.transform.SetAsLastSibling();
            var objective=ui.objective.transform;Rect(objective,0,0,350,64);Skin(objective.GetComponent<Image>(),false,6);EdgesOff(objective);
            var o=(RectTransform)objective;o.anchorMin=o.anchorMax=o.pivot=new Vector2(.5f,1);o.anchoredPosition=new Vector2(0,-24);
            Label(objective,"Room",24,10,302,21,14,Gold,TextAnchor.MiddleCenter);Label(objective,"Objective",18,32,314,20,12,Paper,TextAnchor.MiddleCenter);
            var pause=(RectTransform)ui.pauseButton.transform;Rect(pause,0,0,44,44);pause.anchorMin=pause.anchorMax=pause.pivot=Vector2.one;pause.anchoredPosition=new Vector2(-24,-24);
            Skin(ui.pauseButton.image,false,6);var pauseLabel=Label(pause,"Label",0,0,44,44,19,Paper,TextAnchor.MiddleCenter);pauseLabel.text="Ⅱ";pauseLabel.gameObject.SetActive(true);
            Label(pause,"Hotkey",0,47,44,16,10,Muted,TextAnchor.MiddleCenter).text="ESC";
            var dash=ui.dash.transform;Bottom(dash,24,28,70,70);Skin(dash.GetComponent<Image>(),false,5);EdgesOff(dash);
            Child(dash,"Dash Icon",13,7,44,44);Child(dash,"Cooldown Mask",13,7,44,44);
            dash.Find("Dash Icon").GetComponent<Image>().sprite=SkillIconCatalog.Dash(false);
            Label(dash,"Key",0,74,70,18,10,Muted,TextAnchor.MiddleCenter).text="SPACE";
            Label(dash,"Cooldown",0,49,70,16,11,Gold,TextAnchor.MiddleCenter);
            var buff=ui.buffName.transform.parent;Bottom(buff,110,80,420,22);buff.GetComponent<Image>().color=Color.clear;EdgesOff(buff);Hide(buff,"Caption");Hide(buff,"Upgrade Icon");
            Label(buff,"Name",0,0,420,22,13,Gold);
            Hide(ui.hud.transform,"Controls");Hide(ui.hud.transform,"Control hint backing");Bottom(ui.chargeStatus.transform,112,26,660,20);ui.chargeStatus.fontSize=12;ui.chargeStatus.alignment=TextAnchor.MiddleLeft;
            var strip=ui.hud.transform.Find("Loadout strip");if(strip==null){var go=new GameObject("Loadout strip",typeof(RectTransform),typeof(LoadoutStripView));go.transform.SetParent(ui.hud.transform,false);strip=go.transform;}
            Bottom(strip,108,48,450,34);strip.GetComponent<LoadoutStripView>().EnsureSlots();
            var elite=ui.elite.transform;Rect(elite,0,106,520,88);Skin(elite.GetComponent<Image>(),false,6);EdgesOff(elite);
            var er=(RectTransform)elite;er.anchorMin=er.anchorMax=er.pivot=new Vector2(.5f,1);er.anchoredPosition=new Vector2(0,-106);
            Label(elite,"Elite Name",22,9,476,26,16,Gold,TextAnchor.MiddleCenter);
            Label(elite,"State",22,53,476,27,14,Paper,TextAnchor.MiddleCenter);
            Child(elite,"Track",22,39,476,11);Child(elite,"Fill",22,39,476,11);
            ui.elite.ConfigureTrail(Plate(elite,"Damage trail",22,39,476,11,new Color(1,.48f,.24f)));
            var map=ui.hud.transform.Find("World navigation");if(map!=null){Skin(map.GetComponent<Image>(),false,5);EdgesOff(map);for(int i=0;i<4;i++)Hide(map,"Map border "+i);}
            Skin(ui.objective.promptPanel.GetComponent<Image>(),false,5);
            Bottom(ui.toastPanel.transform,440,126,400,42);Skin(ui.toastPanel.GetComponent<Image>(),false,6);EdgesOff(ui.toastPanel.transform);Label(ui.toastPanel.transform,"Message",20,7,360,28,15,Gold,TextAnchor.MiddleCenter);

            // Three separate cards, not frames nested inside another heavy frame.
            Modal(ui.upgradeScreen,130,36,1020,648);Hide(ui.upgradeScreen.transform,"Painted stone frame");
            Label(ui.upgradeScreen.transform,"Title",20,40,980,50,32,Paper,TextAnchor.MiddleCenter);
            Label(ui.upgradeScreen.transform,"Hint",20,94,980,28,15,Muted,TextAnchor.MiddleCenter);
            Label(ui.upgradeScreen.transform,"Journey",20,8,980,22,11,Gold,TextAnchor.MiddleCenter).text="遗 迹 补 给   /   选 择 一 项 强 化";
            Plate(ui.upgradeScreen.transform,"Header divider",340,132,340,1,new Color(.65f,.5f,.27f,.5f));
            for(int i=0;i<ui.cards.Length;i++){Rect(ui.cards[i].transform,18+i*334,156,316,446);Skin(ui.cards[i].button.image,false,3.8f);EdgesOff(ui.cards[i].transform);}
            Label(ui.upgradeScreen.transform,"Footer",20,620,980,22,12,Muted,TextAnchor.MiddleCenter).text="强化保留至本局结束   ·   鼠标悬停查看，点击选择";

            Modal(ui.titleScreen,210,104,860,512);var title=ui.titleScreen.transform;
            Label(title,"Kicker",56,42,600,22,11,Gold).text="R U I N S   /   E X P E D I T I O N";
            Label(title,"Title",52,70,550,66,46,Paper).text="遗迹工坊";
            Label(title,"Description",56,148,510,80,18,Muted);
            Plate(title,"Menu divider",56,247,486,1,new Color(.55f,.48f,.32f,.45f));
            Label(title,"Select caption",56,269,480,20,12,Gold).text="选择你的探索者";
            var art=Plate(title,"Hero illustration",582,96,206,284,Color.white);art.sprite=Resources.Load<Sprite>("UI/Skin/hero_ranged_v2");art.preserveAspect=true;
            Label(title,"Hero caption",552,389,264,26,12,Muted,TextAnchor.MiddleCenter).text="净化遗迹，构筑你的战斗流派";
            foreach(var pair in new[]{"Select Ranged","Select Shield"}){var t=title.Find(pair);if(t!=null)ButtonAt(t.GetComponent<Button>(),pair=="Select Ranged"?56:308,300,234,46,false);}
            Label(title,"Guide",56,362,490,48,12,Muted);
            ButtonAt(ui.startButton,56,425,486,48,true);

            Modal(ui.pauseScreen,390,168,500,384);var pauseRoot=ui.pauseScreen.transform;
            Label(pauseRoot,"Overline",40,34,420,22,11,Gold,TextAnchor.MiddleCenter).text="休 整 时 刻   /   PAUSED";
            Label(pauseRoot,"Title",40,70,420,56,32,Paper,TextAnchor.MiddleCenter);
            Label(pauseRoot,"Hint",40,132,420,28,15,Muted,TextAnchor.MiddleCenter);
            ButtonAt(ui.resumeButton,70,200,360,48,true);ButtonAt(ui.restartPauseButton,70,264,360,44,false);
            Label(pauseRoot,"Footer",40,330,420,22,11,Muted,TextAnchor.MiddleCenter).text="ESC 返回战斗";

            Modal(ui.resultScreen,270,84,740,552);var result=ui.resultScreen.transform;
            Label(result,"Kicker",48,35,644,22,11,Gold,TextAnchor.MiddleCenter).text="探 索 记 录   /   EXPEDITION";
            Label(result,"Title",48,78,644,56,34,Paper,TextAnchor.MiddleCenter);
            Plate(result,"Result divider",160,153,420,1,new Color(.55f,.48f,.32f,.5f));
            Label(result,"Stats",60,178,620,90,22,Paper,TextAnchor.MiddleCenter);
            Label(result,"Upgrade",60,286,620,66,15,Muted,TextAnchor.MiddleCenter);
            foreach(var pair in new[]{"Select Ranged","Select Shield"}){var t=result.Find(pair);if(t!=null)ButtonAt(t.GetComponent<Button>(),pair=="Select Ranged"?78:380,379,282,40,false);}
            ButtonAt(ui.restartResultButton,190,455,360,50,true);
        }
    }
}
