using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.UI
{
    // Small, resolution-independent UI mesh. Open center keeps the target readable.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class AimReticleGraphic : MaskableGraphic
    {
        public bool Melee { get; private set; }
        public float Kick { get; private set; }
        float charge;
        bool charging;
        static readonly Color Ink=new Color(.025f,.035f,.04f,.9f);
        static readonly Color Ivory=new Color(1,.95f,.79f,.96f);
        static readonly Color Gold=new Color(1,.73f,.31f,.98f);
        public void SetState(bool melee,float kick,bool isCharging,float fraction)
        {
            fraction=Mathf.Clamp01(fraction);
            if(Melee==melee && Mathf.Abs(Kick-kick)<.001f && charging==isCharging && Mathf.Abs(charge-fraction)<.001f)return;
            Melee=melee;Kick=kick;charging=isCharging;charge=fraction;SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();float r=7+Kick*2;
            if(Melee)
            {
                Stroke(vh,new Vector2(-r,0),new Vector2(0,r),Gold);
                Stroke(vh,new Vector2(0,r),new Vector2(r,0),Gold);
                Stroke(vh,new Vector2(r,0),new Vector2(3,-r+3),Gold);
                Stroke(vh,new Vector2(-r,0),new Vector2(-3,-r+3),Gold);
                Stroke(vh,new Vector2(0,-4),new Vector2(0,-8-Kick*2),Ivory);
            }
            else
            {
                for(int i=0;i<4;i++)
                {
                    float a=i*Mathf.PI*.5f;Vector2 u=new Vector2(Mathf.Cos(a),Mathf.Sin(a)),v=new Vector2(-u.y,u.x);
                    Stroke(vh,u*r+v*3,u*r+v*r,Ivory);
                    Stroke(vh,u*r+v*r,u*3+v*r,Ivory);
                }
                if(charging && charge>0)
                {
                    int segments=Mathf.Max(1,Mathf.CeilToInt(charge*40));
                    for(int i=0;i<segments;i++)
                    {
                        float a=Mathf.PI*.5f-charge*Mathf.PI*2*i/segments,b=Mathf.PI*.5f-charge*Mathf.PI*2*(i+1)/segments;
                        Stroke(vh,new Vector2(Mathf.Cos(a),Mathf.Sin(a))*13,new Vector2(Mathf.Cos(b),Mathf.Sin(b))*13,Gold);
                    }
                }
            }
            Dot(vh,2.2f,Ink);Dot(vh,1.15f,Melee?Gold:Ivory);
        }
        static void Stroke(VertexHelper vh,Vector2 a,Vector2 b,Color color)
        {Line(vh,a,b,3,Ink);Line(vh,a,b,1.25f,color);}
        static void Line(VertexHelper vh,Vector2 a,Vector2 b,float width,Color color)
        {
            var d=(b-a).normalized;var n=new Vector2(-d.y,d.x)*width*.5f;
            Quad(vh,a-n,a+n,b+n,b-n,color);
        }
        static void Dot(VertexHelper vh,float radius,Color color)=>Quad(vh,new Vector2(-radius,0),new Vector2(0,radius),new Vector2(radius,0),new Vector2(0,-radius),color);
        static void Quad(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color color)
        {
            int n=vh.currentVertCount;vh.AddVert(a,color,Vector2.zero);vh.AddVert(b,color,Vector2.zero);vh.AddVert(c,color,Vector2.zero);vh.AddVert(d,color,Vector2.zero);
            vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
        }
    }
}
