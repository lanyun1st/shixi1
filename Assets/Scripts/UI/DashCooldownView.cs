using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.UI
{
    public sealed class DashCooldownView : MonoBehaviour
    {
        public Image mask;
        public Text status;
        int previousTenth=-1;
        public void Render(float remaining,float duration)
        {
            mask.fillAmount=duration<=0?0:Mathf.Clamp01(remaining/duration);
            int tenth=remaining<=0?0:Mathf.Max(1,Mathf.RoundToInt(remaining*10));
            if(tenth!=previousTenth){status.text=remaining<=0?"就绪":(tenth*.1f).ToString("F1")+"s";previousTenth=tenth;}
            status.color=remaining<=0?new Color(1,.8f,.43f):Color.white;
        }
    }
}
