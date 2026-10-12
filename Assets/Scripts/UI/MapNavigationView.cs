using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.UI
{
    // All map markers consume snapshots, never references to gameplay actors.
    public sealed class MapNavigationView
    {
        readonly RectTransform root,map,player,portal,viewport;
        readonly Text caption,status;
        readonly Image portalImage;
        readonly RectTransform[] enemies=new RectTransform[64],obstacles=new RectTransform[128];
        readonly Image[] enemyImages=new Image[64];
        int captionRoom=-1;
        readonly CanvasGroup visibility;
        readonly Canvas canvas;
        readonly Vector3[] corners=new Vector3[4];
        static readonly Color Border=new Color(.31f,.46f,.49f,.8f);
        public static Vector2 Project(Vector2 p,Vector2 world,Vector2 display)
            =>new Vector2((Mathf.Clamp01(p.x/world.x)-.5f)*display.x,(.5f-Mathf.Clamp01(p.y/world.y))*display.y);
        static RectTransform Box(Transform parent,string name,Vector2 size,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);
            var rect=(RectTransform)go.transform;rect.sizeDelta=size;
            var image=go.GetComponent<Image>();image.color=color;image.raycastTarget=false;return rect;
        }
        static void Frame(RectTransform parent,Color color)
        {
            for(int i=0;i<4;i++)
            {
                var edge=Box(parent,"Map border "+i,Vector2.zero,color);
                edge.anchorMin=i<2?new Vector2(0,i):new Vector2(i-2,0);
                edge.anchorMax=i<2?new Vector2(1,i):new Vector2(i-2,1);
                edge.sizeDelta=i<2?new Vector2(0,1):new Vector2(1,0);
            }
        }
        static Text Label(Transform parent,Font font,string name,Vector2 at,Vector2 size,int point,TextAnchor align)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);
            var rect=(RectTransform)go.transform;rect.anchoredPosition=at;rect.sizeDelta=size;
            var text=go.GetComponent<Text>();text.font=font;text.fontSize=point;text.alignment=align;text.color=new Color(.72f,.83f,.82f);text.raycastTarget=false;return text;
        }
        public MapNavigationView(Transform hud,Font font)
        {
            root=Box(hud,"World navigation",new Vector2(244,236),new Color(.035f,.065f,.08f,.96f));
            root.anchorMin=root.anchorMax=root.pivot=new Vector2(1,0);root.anchoredPosition=new Vector2(-20,22);Frame(root,Border);
            visibility=root.gameObject.AddComponent<CanvasGroup>();visibility.blocksRaycasts=false;visibility.interactable=false;
            canvas=root.GetComponentInParent<Canvas>();
            // Root pivot is bottom-right. Explicit center anchors keep children inside it.
            caption=Label(root,font,"Region label",new Vector2(0,99),new Vector2(220,26),14,TextAnchor.MiddleLeft);
            status=Label(root,font,"Map status",new Vector2(0,-84),new Vector2(220,24),12,TextAnchor.MiddleLeft);
            Label(root,font,"Map legend",new Vector2(0,-105),new Vector2(220,18),11,TextAnchor.MiddleLeft).text="◆ 自己  · 敌人  ◆ 精英  ◇ 出口";
            map=Box(root,"Map surface",new Vector2(220,154),new Color(.075f,.12f,.14f));map.anchoredPosition=new Vector2(0,7);map.gameObject.AddComponent<RectMask2D>();Frame(map,Border);
            for(int i=1;i<5;i++)
            {
                var v=Box(map,"Grid vertical",new Vector2(1,154),new Color(.35f,.5f,.5f,.1f));v.anchoredPosition=new Vector2(-110+i*44,0);
                var h=Box(map,"Grid horizontal",new Vector2(220,1),new Color(.35f,.5f,.5f,.1f));h.anchoredPosition=new Vector2(0,-77+i*30.8f);
            }
            for(int i=0;i<24;i++)
            {
                float y=(i+.5f)*2240/24;var road=Box(map,"North road",new Vector2(10,8),new Color(.38f,.44f,.41f,.5f));road.anchoredPosition=Project(new Vector2(1600+Mathf.Sin(y*.0035f)*42,y),new Vector2(3200,2240),map.sizeDelta);
                float x=(i+.5f)*3200/24;road=Box(map,"Cross road",new Vector2(11,8),new Color(.38f,.44f,.41f,.5f));road.anchoredPosition=Project(new Vector2(x,1200+Mathf.Sin(x*.003f)*30),new Vector2(3200,2240),map.sizeDelta);
            }
            for(int i=0;i<obstacles.Length;i++)obstacles[i]=Box(map,"Obstacle "+i,Vector2.one,new Color(.55f,.62f,.58f,.7f));
            viewport=Box(map,"Visible area",Vector2.one,new Color(.45f,.8f,.85f,.055f));Frame(viewport,new Color(.5f,.82f,.84f,.65f));
            for(int i=0;i<enemies.Length;i++){enemies[i]=Box(map,"Enemy "+i,new Vector2(3,3),Color.red);enemyImages[i]=enemies[i].GetComponent<Image>();}
            portal=Box(map,"Portal",new Vector2(8,8),Color.gray);portal.localRotation=Quaternion.Euler(0,0,45);portalImage=portal.GetComponent<Image>();
            player=Box(map,"Player",new Vector2(7,7),new Color(1,.85f,.4f));player.localRotation=Quaternion.Euler(0,0,45);Frame(player,new Color(1,1,.85f));
        }
        public void AvoidPlayer(Rect screenBounds,float dt)
        {
            root.GetWorldCorners(corners);
            var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
            var a=RectTransformUtility.WorldToScreenPoint(camera,corners[0]);var b=RectTransformUtility.WorldToScreenPoint(camera,corners[2]);
            var panel=Rect.MinMaxRect(a.x,a.y,b.x,b.y);
            // Full sprite bounds, screen coordinates, and a margin prevent the head being hidden.
            float margin=(visibility.alpha<.99f?100:65)*canvas.scaleFactor;
            var proximity=Rect.MinMaxRect(panel.xMin-margin,panel.yMin-margin,panel.xMax+margin,panel.yMax+margin);
            float target=proximity.Overlaps(screenBounds)?0:1;
            visibility.alpha=panel.Overlaps(screenBounds)?0:Mathf.MoveTowards(visibility.alpha,target,Mathf.Max(0,dt)*10);
        }
        public void Render(GameUiSnapshot state)
        {
            if(state.mapSize.x<=0 || state.mapSize.y<=0)return;
            var size=new Vector2(220,220*state.mapSize.y/state.mapSize.x);size.y=Mathf.Min(154,size.y);map.sizeDelta=size;
            player.anchoredPosition=Project(state.playerPosition,state.mapSize,size);portal.anchoredPosition=Project(state.portalPosition,state.mapSize,size);
            viewport.sizeDelta=new Vector2(Mathf.Min(size.x,state.cameraSize.x/state.mapSize.x*size.x),Mathf.Min(size.y,state.cameraSize.y/state.mapSize.y*size.y));
            var center=Project(state.cameraPosition,state.mapSize,size);var limit=(size-viewport.sizeDelta)*.5f;
            viewport.anchoredPosition=new Vector2(Mathf.Clamp(center.x,-limit.x,limit.x),Mathf.Clamp(center.y,-limit.y,limit.y));
            portalImage.color=state.cleared?new Color(.3f,1,.9f):new Color(.42f,.49f,.5f);
            for(int i=0;i<obstacles.Length;i++)
            {
                bool show=state.mapObstacles!=null && i<state.mapObstacleCount && i<state.mapObstacles.Length;obstacles[i].gameObject.SetActive(show);
                if(show){var r=state.mapObstacles[i];obstacles[i].anchoredPosition=Project(r.center,state.mapSize,size);obstacles[i].sizeDelta=new Vector2(Mathf.Max(2,r.width/state.mapSize.x*size.x),Mathf.Max(2,r.height/state.mapSize.y*size.y));}
            }
            for(int i=0;i<enemies.Length;i++)
            {
                bool show=state.enemyPositions!=null && i<state.enemyPositionCount && i<state.enemyPositions.Length;enemies[i].gameObject.SetActive(show);if(!show)continue;
                enemies[i].anchoredPosition=Project(state.enemyPositions[i],state.mapSize,size);
                int kind=state.enemyKinds!=null && i<state.enemyKinds.Length?state.enemyKinds[i]:0;
                enemies[i].sizeDelta=Vector2.one*(kind==2?7:kind==1?5:3);
                enemies[i].localRotation=Quaternion.Euler(0,0,kind==2?45:0);
                enemyImages[i].color=kind==2?new Color(1,.7f,.25f):kind==1?new Color(.8f,.5f,1):kind==3?new Color(1,.7f,.4f,.45f):new Color(1,.36f,.32f);
            }
            if(captionRoom!=state.room){caption.text="遗迹导航  /  "+state.room.ToString("00");captionRoom=state.room;}
            status.text=state.cleared?"出口已开启 · 向北前进":state.regionName;
        }
    }
}
