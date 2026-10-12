using UnityEngine;
namespace Portfolio.Domain
{
    public static class ArenaLayout
    {
        public const float Width=3200,Height=2240,PlayerRadius=16,SlimeRadius=20;
        public static readonly Rect Bounds=Rect.MinMaxRect(80,220,3120,2160);
        public static readonly Vector2 Entrance=new Vector2(1600,1740),Portal=new Vector2(1600,270);
        public static readonly Vector2[] Spawns={new Vector2(270,370),new Vector2(1330,370),new Vector2(370,560),new Vector2(1230,560),new Vector2(680,380),new Vector2(920,380),new Vector2(800,550)};
        public static Vector2 Clamp(Vector2 p,float radius)=>new Vector2(Mathf.Clamp(p.x,Bounds.xMin+radius,Bounds.xMax-radius),Mathf.Clamp(p.y,Bounds.yMin+radius,Bounds.yMax-radius));
        public static bool CircleOverlaps(Vector2 p,float radius,Rect box)
        {
            var closest=new Vector2(Mathf.Clamp(p.x,box.xMin,box.xMax),Mathf.Clamp(p.y,box.yMin,box.yMax));
            return (p-closest).sqrMagnitude<radius*radius;
        }
        public static bool SweepMayOverlap(Vector2 a,Vector2 b,float radius,Rect box)
            =>Mathf.Max(a.x,b.x)+radius>=box.xMin && Mathf.Min(a.x,b.x)-radius<=box.xMax &&
              Mathf.Max(a.y,b.y)+radius>=box.yMin && Mathf.Min(a.y,b.y)-radius<=box.yMax;
        public static Vector2 Resolve(Vector2 p,float radius,Rect box)
        {
            var closest=new Vector2(Mathf.Clamp(p.x,box.xMin,box.xMax),Mathf.Clamp(p.y,box.yMin,box.yMax));
            var d=p-closest;
            if(d.sqrMagnitude>=radius*radius)return p;
            if(d.sqrMagnitude>.0001f)return closest+d.normalized*radius;
            float left=p.x-box.xMin,right=box.xMax-p.x,top=p.y-box.yMin,bottom=box.yMax-p.y;
            float least=Mathf.Min(left,right,top,bottom);
            if(least==left)p.x=box.xMin-radius;else if(least==right)p.x=box.xMax+radius;
            else if(least==top)p.y=box.yMin-radius;else p.y=box.yMax+radius;
            return p;
        }
    }
}
