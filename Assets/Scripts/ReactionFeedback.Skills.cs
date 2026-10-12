using UnityEngine;

namespace Portfolio.Presentation
{
    public sealed partial class ReactionFeedback
    {
        static float Lifetime(ReactionKind kind)
            =>kind==ReactionKind.Chain?.26f:kind==ReactionKind.Slash?.2f:kind==ReactionKind.HeavySlash?.29f:kind==ReactionKind.Guard?.22f:kind==ReactionKind.Muzzle?.16f:.44f;
        static Vector2 Polar(float a,float r)=>new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r;
        static Color Alpha(Color c,float a){c.a=a;return c;}
        static void Triangle(Effect e,Vector2 a,Vector2 b,Vector2 c,Color ca,Color cb,Color cc)
        {
            int n=e.vertices.Count;e.vertices.Add(World(a));e.vertices.Add(World(b));e.vertices.Add(World(c));
            e.colors.Add(ca);e.colors.Add(cb);e.colors.Add(cc);e.triangles.Add(n);e.triangles.Add(n+1);e.triangles.Add(n+2);
        }
        static void Shard(Effect e,Vector2 center,Vector2 dir,float length,float width,Color color,float opacity)
        {
            var side=new Vector2(-dir.y,dir.x)*width;var tip=center+dir*length;var tail=center-dir*length*.6f;
            Triangle(e,tail,center+side,tip,Alpha(color,0),Alpha(color,opacity),Alpha(Color.white,opacity));
            Triangle(e,tail,tip,center-side,Alpha(color,0),Alpha(Color.white,opacity),Alpha(color,opacity*.65f));
        }
        void RenderSkill(Effect e)
        {
            e.flash.enabled=false;
            e.rim.positionCount=e.sparks.positionCount=0;foreach(var branch in e.branches)branch.positionCount=0;
            e.surface.enabled=true;e.vertices.Clear();e.colors.Clear();e.triangles.Clear();
            float t=e.age/e.life,fade=1-t*t,radius=Mathf.Max(15,e.cue.Radius);
            Vector2 center=e.cue.From,direction=(e.cue.To-center).normalized;
            if(direction.sqrMagnitude<.1f)direction=Vector2.right;
            float heading=Mathf.Atan2(direction.y,direction.x);
            var kind=e.cue.Kind;
            Color color=kind==ReactionKind.IceShatter?new Color(.3f,.8f,1):kind==ReactionKind.Nova?new Color(.2f,.9f,.9f):kind==ReactionKind.Echo?new Color(.65f,.5f,1):kind==ReactionKind.ShieldCone||kind==ReactionKind.Guard?new Color(.5f,1,.8f):kind==ReactionKind.Debris||kind==ReactionKind.Slam?new Color(.78f,.6f,.34f):new Color(1,.57f,.16f);
            if(kind==ReactionKind.Slash || kind==ReactionKind.HeavySlash || kind==ReactionKind.Guard)
            {
                bool heavy=kind==ReactionKind.HeavySlash,guard=kind==ReactionKind.Guard;
                color=guard?color:heavy?new Color(1,.75f,.3f):new Color(.73f,.93f,.95f);
                float half=guard?.65f:heavy?Mathf.PI/2:Mathf.PI/3;
                float tip=heading-half+2*half*Mathf.Clamp01(t*1.6f),start=Mathf.Max(heading-half,tip-half*1.6f);
                for(int i=0;i<22;i++)
                {
                    float u=i/22f,v=(i+1)/22f,a=Mathf.Lerp(start,tip,u),b=Mathf.Lerp(start,tip,v);
                    float width=(heavy?38:guard?12:26)*Mathf.Sin(v*Mathf.PI*.8f)*(1-t*.35f);
                    float outer=radius*(guard?1:.82f),inner=outer-width;
                    var p=center+Polar(a,inner);var q=center+Polar(a,outer);var r=center+Polar(b,outer);var s=center+Polar(b,inner);
                    Triangle(e,p,q,r,Alpha(color,0),Alpha(color,fade*u),Alpha(Color.white,fade*v));
                    Triangle(e,p,r,s,Alpha(color,0),Alpha(Color.white,fade*v),Alpha(color,0));
                    Triangle(e,center+Polar(a,outer-2),q,r,Alpha(color,0),Alpha(Color.white,fade*u),Alpha(Color.white,fade*v));
                }
            }
            else if(kind==ReactionKind.Overload || kind==ReactionKind.Thermal)
            {
                for(int i=0;i<9;i++)
                {
                    float angle=i*Mathf.PI*2/9+Noise(e.seed+i)*.22f;
                    var dir=Polar(angle,1);var side=Polar(angle+Mathf.PI/2,1);
                    float reach=radius*(.22f+.7f*Mathf.Sqrt(t))*(.85f+.15f*Noise(e.seed+i*17));
                    var tail=center+dir*reach*.22f;var belly=center+dir*reach*.66f+side*12*fade;var tip=center+dir*reach;
                    var hot=kind==ReactionKind.Thermal && i%2==0?new Color(.25f,.85f,1):new Color(1,.46f,.04f);
                    Triangle(e,tail-side*19*fade,belly+side*16*fade,tip,Alpha(hot,0),Alpha(hot,fade*.8f),Alpha(new Color(1,.9f,.5f),0));
                    Triangle(e,tail,belly,tip,Alpha(Color.white,fade*.7f),Alpha(new Color(1,.8f,.2f),fade),Alpha(hot,0));
                }
            }
            else if(kind==ReactionKind.Nova || kind==ReactionKind.Echo)
            {
                // Six separated, outward-moving pressure wisps, not a range perimeter.
                for(int i=0;i<6;i++)
                    for(int j=0;j<6;j++)
                    {
                        float a=i*Mathf.PI/3+j*.065f,b=a+.065f,r=radius*(.12f+.85f*Mathf.Sqrt(t));
                        float width=25*fade*Mathf.Sin((j+1)/7f*Mathf.PI);
                        Triangle(e,center+Polar(a,r-width),center+Polar(a,r),center+Polar(b,r),Alpha(color,0),Alpha(color,fade*.65f),Alpha(Color.white,fade*.75f));
                    }
            }
            else
            {
                bool cone=kind==ReactionKind.ShieldCone || kind==ReactionKind.DashImpact || kind==ReactionKind.Muzzle;
                int count=kind==ReactionKind.IceShatter?16:cone?7:12;
                for(int i=0;i<count;i++)
                {
                    float angle=cone?heading+Mathf.Lerp(-.8f,.8f,i/(float)(count-1)):i*Mathf.PI*2/count+Noise(e.seed+i)*.16f;
                    var dir=Polar(angle,1);float variation=.8f+.2f*(Noise(e.seed+i*13)*.5f+.5f);
                    float reach=radius*Mathf.Sqrt(t)*variation;
                    float len=(kind==ReactionKind.IceShatter?23:kind==ReactionKind.Slam?38:27)*fade+4;
                    float width=(kind==ReactionKind.IceShatter?7:kind==ReactionKind.Slam?4:12)*fade;
                    var shade=kind==ReactionKind.Thermal && i%2==0?new Color(.3f,.85f,1):color;
                    Shard(e,center+dir*reach,dir,len,width,shade,fade*.85f);
                    if(kind==ReactionKind.Slam)
                        Shard(e,center+dir*reach*.6f,Polar(angle+.5f,1),len*.65f,2,color,fade*.55f);
                }
                // A short central flare, then only scattered moving fragments. No perimeter circle.
                if(t<.38f && kind!=ReactionKind.Debris)
                    for(int i=0;i<5;i++)Shard(e,center,Polar(i*Mathf.PI*2/5,.99f),radius*.33f,8,color,(1-t/.38f)*.75f);
            }
            e.mesh.Clear();e.mesh.SetVertices(e.vertices);e.mesh.SetColors(e.colors);e.mesh.SetTriangles(e.triangles,0);e.mesh.RecalculateBounds();
        }
    }
}
