using UnityEngine;

namespace Portfolio.Domain
{
    // Stable vertical body capsule in screen-down gameplay coordinates, independent of gait.
    public struct EnemyHurtbox
    {
        public Vector2 Top, Bottom;
        public float Radius;
        public Vector2 Center => (Top + Bottom) * .5f;
        public static EnemyHurtbox Create(Vector2 feet, bool elite, bool sentry, bool fast)
        {
            float upper=25, lower=23, radius=23, scale=fast?72f/88:1;
            if(elite){upper=70;lower=38;radius=38;scale=1;}
            else if(sentry){upper=48;lower=32;radius=35;scale=1;}
            return new EnemyHurtbox { Top=feet+Vector2.down*(upper*scale), Bottom=feet+Vector2.down*(lower*scale), Radius=radius*scale };
        }
        static Vector2 Closest(Vector2 p, Vector2 a, Vector2 b)
        { var d=b-a;return a+d*Mathf.Clamp01(Vector2.Dot(p-a,d)/Mathf.Max(.000001f,d.sqrMagnitude)); }
        public bool Overlaps(Vector2 origin,float range)
        { return (origin-Closest(origin,Top,Bottom)).sqrMagnitude<=(Radius+range)*(Radius+range)+.001f; }
        static float Circle(Vector2 a,Vector2 b,Vector2 c,float r)
        {
            var d=b-a;var o=a-c;float cc=o.sqrMagnitude-r*r;
            if(cc<=0)return 0;float aa=d.sqrMagnitude;if(aa<.000001f)return float.PositiveInfinity;
            float dot=Vector2.Dot(o,d),disc=dot*dot-aa*cc;if(disc<0)return float.PositiveInfinity;
            float t=(-dot-Mathf.Sqrt(disc))/aa;return t>=0&&t<=1?t:float.PositiveInfinity;
        }
        static bool Slab(float p,float d,float lo,float hi,ref float entry,ref float exit)
        {
            if(Mathf.Abs(d)<.000001f)return p>=lo&&p<=hi;
            float a=(lo-p)/d,b=(hi-p)/d;if(a>b){float swap=a;a=b;b=swap;}
            entry=Mathf.Max(entry,a);exit=Mathf.Min(exit,b);return entry<=exit;
        }
        public float Entry(Vector2 from,Vector2 to,float padding=0)
        {
            float r=Radius+padding;
            float t=Mathf.Min(Circle(from,to,Top,r),Circle(from,to,Bottom,r));
            float entry=0,exit=1;var d=to-from;
            if(Slab(from.x,d.x,Top.x-r,Top.x+r,ref entry,ref exit)&&Slab(from.y,d.y,Top.y,Bottom.y,ref entry,ref exit))t=Mathf.Min(t,entry);
            return t;
        }
        static float PointSectorDistance(Vector2 p,Vector2 origin,Vector2 dir,float range,float dot)
        {
            var delta=p-origin;float length=delta.magnitude;
            if(length<.00001f)return 0;
            if(Vector2.Dot(delta/length,dir)>=dot)return Mathf.Max(0,length-range);
            float sin=Mathf.Sqrt(Mathf.Max(0,1-dot*dot));var side=new Vector2(-dir.y,dir.x);
            var a=origin+(dir*dot+side*sin)*range;var b=origin+(dir*dot-side*sin)*range;
            return Mathf.Min(Vector2.Distance(p,Closest(p,origin,a)),Vector2.Distance(p,Closest(p,origin,b)));
        }
        bool TouchesSegment(Vector2 a,Vector2 b)
        {
            if(!float.IsPositiveInfinity(Entry(a,b)))return true;
            return (Top-Closest(Top,a,b)).sqrMagnitude<=Radius*Radius || (Bottom-Closest(Bottom,a,b)).sqrMagnitude<=Radius*Radius;
        }
        public bool OverlapsSector(Vector2 origin,Vector2 dir,float range,float minDot)
        {
            if(dir.sqrMagnitude<.00001f)return false;dir.Normalize();minDot=Mathf.Clamp01(minDot);
            if(PointSectorDistance(Top,origin,dir,range,minDot)<=Radius || PointSectorDistance(Bottom,origin,dir,range,minDot)<=Radius)return true;
            var nearest=Closest(origin,Top,Bottom);
            if(PointSectorDistance(nearest,origin,dir,range,minDot)<=Radius)return true;
            float sin=Mathf.Sqrt(1-minDot*minDot);var side=new Vector2(-dir.y,dir.x);
            return TouchesSegment(origin,origin+(dir*minDot+side*sin)*range) || TouchesSegment(origin,origin+(dir*minDot-side*sin)*range);
        }
    }
}
