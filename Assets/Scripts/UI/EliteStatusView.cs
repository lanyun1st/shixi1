using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.UI
{
    public sealed class EliteStatusView : MonoBehaviour
    {
        public Image fill;
        public Text status;
        Image trail;
        CanvasGroup group;
        float delayed,hold;
        bool visible;
        public void ConfigureTrail(Image image)
        {
            trail=image;trail.sprite=fill.sprite;trail.type=Image.Type.Filled;
            trail.fillMethod=Image.FillMethod.Horizontal;trail.fillOrigin=0;
            trail.transform.SetAsLastSibling();fill.transform.SetAsLastSibling();
            group=GetComponent<CanvasGroup>();if(group==null)group=gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts=false;group.interactable=false;
        }
        public void Render(GameUiSnapshot state)=>Present(state,Time.unscaledDeltaTime);
        void Present(GameUiSnapshot state,float dt)
        {
            bool show=state.room==3 && state.eliteHealth>0 && state.eliteMaximum>0;
            bool reset=state.room!=3 || state.mode==UiMode.Title || state.mode==UiMode.Result;
            if(reset)show=false;
            float target=state.eliteMaximum<=0?0:Mathf.Clamp01(state.eliteHealth/state.eliteMaximum);
            if(show && !visible){delayed=target;hold=0;if(group!=null)group.alpha=0;}
            if(target<fill.fillAmount)hold=.18f;
            fill.fillAmount=target;
            if(target>delayed)delayed=target;
            if(state.mode!=UiMode.Paused){hold-=dt;if(hold<=0)delayed=Mathf.MoveTowards(delayed,target,dt*1.2f);}
            if(trail!=null)trail.fillAmount=delayed;
            if(group!=null)group.alpha=reset?0:Mathf.MoveTowards(group.alpha,show?1:0,dt*5);
            gameObject.SetActive(show || !reset && group!=null && group.alpha>0);
            visible=show;
            status.text=show?(state.eliteStunned?"破绽 · 伤害×1.75":state.eliteState):"守卫已击败";
            status.color=state.eliteStunned?new Color(.5f,1,.85f):new Color(1,.8f,.43f);
        }
    }
}
