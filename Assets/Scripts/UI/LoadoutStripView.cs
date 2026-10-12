using UnityEngine;
using UnityEngine.UI;
namespace Portfolio.UI
{
    public sealed class LoadoutStripView:MonoBehaviour
    {
        public Image[] icons=new Image[9];
        int mask=-1;bool shield;
        public void EnsureSlots()
        {
            for(int i=0;i<9;i++)
            {
                if(icons[i]!=null){StyleSlot(icons[i]);continue;}
                var slot=new GameObject("Slot "+i,typeof(RectTransform),typeof(Image));slot.transform.SetParent(transform,false);
                var r=(RectTransform)slot.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(i*49,0);r.sizeDelta=new Vector2(44,44);
                var frame=slot.GetComponent<Image>();frame.sprite=Resources.Load<Sprite>("Art/ui/slot_silver");frame.color=new Color(.65f,.7f,.7f,.7f);frame.raycastTarget=false;
                var icon=new GameObject("Skill",typeof(RectTransform),typeof(Image));icon.transform.SetParent(slot.transform,false);
                var ir=(RectTransform)icon.transform;ir.sizeDelta=new Vector2(28,28);icons[i]=icon.GetComponent<Image>();icons[i].preserveAspect=true;icons[i].raycastTarget=false;
                StyleSlot(icons[i]);
            }
        }
        static void StyleSlot(Image icon)
        {
            var frame=icon.transform.parent.GetComponent<Image>();UiArtSkin.Skin(frame,false,8);frame.raycastTarget=false;
            ((RectTransform)frame.transform).sizeDelta=new Vector2(34,34);((RectTransform)icon.transform).sizeDelta=new Vector2(26,26);
        }
        public void Render(int owned,bool shieldHero)
        {
            if(mask==owned && shield==shieldHero)return;mask=owned;shield=shieldHero;
            int visible=0;
            for(int i=0;i<9;i++)
            {
                bool active=(owned&(1<<i))!=0;icons[i].enabled=active;icons[i].sprite=active?UiText.UpgradeIcon(i,shieldHero):null;
                var slot=(RectTransform)icons[i].transform.parent;slot.gameObject.SetActive(active);
                if(active){slot.anchoredPosition=new Vector2(visible++*40,0);slot.GetComponent<Image>().color=Color.white;}
            }
        }
    }
}
