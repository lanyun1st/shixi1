using Portfolio.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.UI
{
    public sealed class HealthBarView : MonoBehaviour
    {
        public Image fill;
        public Text value;
        HealthModel model;
        public void Bind(HealthModel source)
        {
            Unbind();model=source;model.Changed+=Refresh;Refresh(model.Current,model.Maximum);
        }
        public void Unbind(){if(model!=null)model.Changed-=Refresh;model=null;}
        void Refresh(float current,float maximum)
        {
            fill.fillAmount=maximum<=0?0:current/maximum;
            value.text=Mathf.CeilToInt(current)+" / "+Mathf.CeilToInt(maximum);
        }
        void OnDestroy(){Unbind();}
    }
}
