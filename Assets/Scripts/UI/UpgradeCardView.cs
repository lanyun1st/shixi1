using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Portfolio.UI
{
    public sealed class UpgradeCardView : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,ISelectHandler,IDeselectHandler
    {
        public Button button;
        public Image icon;
        public Text title,description;
        public int ChoiceId { get; private set; }
        Text category,synergy,action;
        Image iconFrame,accent,synergyPlate;
        Color routeTint;
        bool decorated,hover;
        Vector2 home;
        static void Rect(Transform t,float x,float y,float w,float h)
        {var r=(RectTransform)t;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
        Image Plate(string name,float x,float y,float w,float h,Color color)
        {
            return UiArtSkin.Plate(transform,name,x,y,w,h,color);
        }
        Text Label(string name,float x,float y,float w,float h,int size)
        {
            return UiArtSkin.Label(transform,name,x,y,w,h,size,UiArtSkin.Muted);
        }
        void Decorate()
        {
            if(decorated)return;decorated=true;home=((RectTransform)transform).anchoredPosition;
            accent=Plate("Route accent",32,59,252,2,Color.white);
            category=Label("Category",28,27,242,22,11);
            iconFrame=Plate("Relic socket",120,81,76,76,Color.clear);iconFrame.preserveAspect=true;
            Rect(icon.transform,120,81,76,76);icon.transform.SetAsLastSibling();
            Rect(title.transform,24,174,268,36);title.font=UiTypography.Heading??title.font;title.fontSize=24;title.alignment=TextAnchor.MiddleCenter;
            Rect(description.transform,30,218,256,94);description.alignment=TextAnchor.UpperLeft;description.font=UiTypography.Body??description.font;description.lineSpacing=1.25f;
            synergyPlate=Plate("Synergy backing",26,318,264,84,new Color(.10f,.15f,.17f));
            synergy=Label("Synergy detail",36,324,244,72,12);
            action=transform.Find("Select").GetComponent<Text>();Rect(action.transform,28,406,260,24);action.fontSize=14;action.alignment=TextAnchor.MiddleCenter;
            var edge=transform.Find("Gold Edge");if(edge!=null)edge.gameObject.SetActive(false);
            var number=Label("Choice number",272,27,20,22,11);int index;int.TryParse(name.Substring(name.LastIndexOf(' ')+1),out index);number.text=(index+1).ToString("00");
        }
        public void Configure(int id,int owned=0,bool shield=false)
        {
            Decorate();ChoiceId=id;hover=false;
            button.transition=Selectable.Transition.None;button.navigation=new Navigation{mode=Navigation.Mode.None};
            button.image.canvasRenderer.SetColor(Color.white);
            bool ready=Portfolio.Domain.SynergyRules.CompletedBy(id,owned,shield)!=null;
            var tint=shield?new Color(.88f,.73f,.43f):id==0||id==6?new Color(1,.55f,.3f):id==1||id==2?new Color(.42f,.81f,1):new Color(.7f,.61f,1);routeTint=tint;
            title.text=UiText.UpgradeName(id,shield);title.color=new Color(.95f,.91f,.78f);
            description.text=UiText.Details(id,shield);description.fontSize=15;
            icon.sprite=UiText.UpgradeIcon(id,shield);iconFrame.color=Color.clear;
            category.text=ready?"可激活联动":id>=6?"战术强化":"流派组件";category.color=tint;
            accent.color=tint;synergy.color=ready?new Color(1,.84f,.48f):new Color(.7f,.81f,.82f);
            synergy.text=(ready?"联动收益\n":"组合方向\n")+UiText.Synergy(id,owned,shield);
            synergyPlate.color=ready?new Color(.20f,.16f,.085f):new Color(.075f,.13f,.15f);
            action.text=ready?"选择 · 完成联动  →":"选择此强化  →";action.color=tint;
            button.image.color=new Color(.84f,.88f,.9f);
        }
        void Update()
        {
            if(!decorated)return;bool focus=hover;float blend=1-Mathf.Exp(-18*Time.unscaledDeltaTime);
            button.image.color=focus?new Color(1.12f,1.1f,1.04f):new Color(.84f,.88f,.9f);
            accent.color=focus?Color.Lerp(routeTint,Color.white,.3f):routeTint;
            transform.localScale=Vector3.Lerp(transform.localScale,Vector3.one*(focus?1.012f:1),blend);
            ((RectTransform)transform).anchoredPosition=Vector2.Lerp(((RectTransform)transform).anchoredPosition,home+Vector2.up*(focus?6:0),blend);
        }
        void OnDisable(){hover=false;if(decorated){transform.localScale=Vector3.one;((RectTransform)transform).anchoredPosition=home;}}
        public void OnPointerEnter(PointerEventData e){hover=true;}
        public void OnPointerExit(PointerEventData e){hover=false;}
        public void OnSelect(BaseEventData e){} // EventSystem selection is not pointer hover.
        public void OnDeselect(BaseEventData e){}
    }
}
