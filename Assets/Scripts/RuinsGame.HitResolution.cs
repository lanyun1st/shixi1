using UnityEngine;
using Portfolio.Domain;

public sealed partial class RuinsGame
{
    static EnemyHurtbox Body(Enemy e)=>EnemyHurtbox.Create(e.p,e.elite!=null,e.sentry!=null,e.fast);
    static bool BodyInRange(Enemy e,Vector2 origin,float range)=>Body(e).Overlaps(origin,range);
    bool BodyInMelee(Enemy e,float range,float dot)=>Body(e).OverlapsSector(player,meleeDirection,range,dot);
    readonly System.Collections.Generic.List<Obstacle> shotObstacleCandidates=new System.Collections.Generic.List<Obstacle>(48);
    static float CircleEntry(Vector2 from,Vector2 to,Vector2 center,float radius)
    {
        var d=to-from;var offset=from-center;float c=offset.sqrMagnitude-radius*radius;
        if(c<=0)return 0;
        float a=d.sqrMagnitude;if(a<.0001f)return float.PositiveInfinity;
        float b=Vector2.Dot(offset,d),disc=b*b-a*c;
        if(disc<0)return float.PositiveInfinity;
        float t=(-b-Mathf.Sqrt(disc))/a;
        return t>=0 && t<=1?t:float.PositiveInfinity;
    }
    // Sweep in travel order: a nearby enemy must not be shielded by a wall farther away.
    bool ResolveShot(Shot shot,Vector2 from,Vector2 to)
    {
        float wallT=float.PositiveInfinity;Obstacle wall=null;
        shotObstacleCandidates.Clear();foreach(var obstacle in obstacles)
            if(obstacle.hp!=0 && ArenaLayout.SweepMayOverlap(from,to,5,obstacle.footprint))shotObstacleCandidates.Add(obstacle);
        int steps=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(from,to)/2));
        for(int i=0;i<=steps;i++)
        {
            var p=Vector2.Lerp(from,to,i/(float)steps);
            if(!ArenaLayout.Bounds.Contains(p)){wallT=i/(float)steps;break;}
            foreach(var o in shotObstacleCandidates)if(o.hp!=0 && ArenaLayout.CircleOverlaps(p,5,o.footprint))
            {wall=o;wallT=i/(float)steps;break;}
            if(wall!=null)break;
        }
        while(shot.remaining>0)
        {
            Enemy nearest=null;float nearestT=wallT;
            foreach(var e in enemies)
            {
                if(e.hp<=0 || e.spawnDelay>0 || shot.hit.Contains(e))continue;
                float t=Body(e).Entry(from,to,5);
                if(t<nearestT){nearestT=t;nearest=e;}
            }
            if(nearest==null)break;
            if(shot.hit.Count==0)hits++;
            shot.hit.Add(nearest);Damage(nearest,shot.damage,true);shot.remaining--;
            if(shot.heavy)HeavyHit(nearest,shot.velocity);
        }
        if(shot.remaining<=0)return true;
        if(float.IsPositiveInfinity(wallT))return false;
        if(wall!=null)
        {
            SpawnImpact(Vector2.Lerp(from,to,wallT),70);
            if(wall.hp>0 && --wall.hp==0)BreakProp(wall);
            else PlayTone(hitSound,.12f);
        }
        return true;
    }
}
