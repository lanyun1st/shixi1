using System.Collections.Generic;
using Portfolio.Domain;
using UnityEngine;

public sealed partial class RuinsGame
{
    sealed class HostileShot
    {
        public Vector2 p,velocity;
        public float life,damage,radius;
        public SpriteRenderer view;
    }
    readonly List<HostileShot> hostileShots=new List<HostileShot>();
    LeasePool<HostileShot> hostilePool;
    SentrySettings sentrySettings;
    int rangedWave;
    const int HostileShotLimit=16;
    void InitializeSentries()
    {
        sentrySettings=Resources.Load<SentrySettings>("SentrySettings");
        if(sentrySettings==null){Debug.LogError("Missing SentrySettings. Use SentryAssetInstaller.Ensure.");enabled=false;return;}
        sentrySettings.Validate();
        hostilePool=new LeasePool<HostileShot>(()=>new HostileShot{view=PoolSprite("Pooled enemy bolt")},s=>{
            s.p=s.velocity=Vector2.zero;s.life=s.damage=s.radius=0;ResetSprite(s.view);
        },s=>{if(s.view!=null)Destroy(s.view.gameObject);},8,32);
    }
    Enemy SpawnSentry(Vector2 p,float delay)
    {
        var view=Make("sentry/down_idle",p,128,(int)p.y);view.enabled=delay<=0;
        var e=new Enemy{p=p,hp=sentrySettings.health,max=sentrySettings.health,spawnDelay=delay,view=view};
        e.sentry=new SentryActor(view,Art,MoveSolid,ClearSentryPath,SpawnHostileShot,skillMaterial,sentrySettings);
        enemies.Add(e);return e;
    }
    bool ClearSentryPath(Vector2 a,Vector2 b)=>ArenaLayout.Bounds.Contains(a) && ArenaLayout.Bounds.Contains(b) && ClearSkillPath(a,b);
    bool WantSentry()
    {
        if(room<2 || waves.Wave==rangedWave || room==3 && waves.Wave<2)return false;
        // A single introductory caster in room 2 wave 1; later one per wave.
        // The guard remains first in its own wave. Pending telegraphs count toward the cap.
        if(waves.NextIsGuard)return false;
        int alive=0;foreach(var e in enemies)if(e.sentry!=null && e.hp>0)alive++;
        return alive<2;
    }
    void SpawnHostileShot(Vector2 position,Vector2 direction)
    {
        if(phase!=Phase.Playing || hostileShots.Count>=HostileShotLimit)return;
        var s=hostilePool.Rent();s.p=position;s.velocity=direction.normalized*sentrySettings.projectileSpeed;
        s.life=sentrySettings.projectileLifetime;s.damage=sentrySettings.projectileDamage;s.radius=sentrySettings.projectileRadius;
        ShowPooled(s.view,"sentry/bolt",position+new Vector2(0,-32),64,5000);
        s.view.transform.rotation=Quaternion.Euler(0,0,-Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);
        hostileShots.Add(s);PlayTone(shotSound,.16f);
    }
    void ReleaseHostileAt(int i){var s=hostileShots[i];hostileShots.RemoveAt(i);hostilePool.Return(s);}
    void ClearHostileShots(){foreach(var s in hostileShots)hostilePool.Return(s);hostileShots.Clear();}
    float HostileWallTime(Vector2 from,Vector2 to,float radius)
    {
        int steps=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(from,to)/2));
        for(int i=0;i<=steps;i++)
        {
            float t=i/(float)steps;var p=Vector2.Lerp(from,to,t);
            if(!ArenaLayout.Bounds.Contains(p))return t;
            foreach(var o in obstacles)if(o.hp!=0 && ArenaLayout.CircleOverlaps(p,radius,o.footprint))return t;
        }
        return float.PositiveInfinity;
    }
    void TickHostileShots(float dt)
    {
        if(phase!=Phase.Playing)return;
        for(int i=hostileShots.Count-1;i>=0;i--)
        {
            var s=hostileShots[i];var from=s.p;var to=from+s.velocity*dt;s.life-=dt;
            float wall=HostileWallTime(from,to,s.radius),playerHit=CircleEntry(from,to,player,ArenaLayout.PlayerRadius+s.radius);
            if(!float.IsPositiveInfinity(wall) && wall<=playerHit){ReleaseHostileAt(i);continue;}
            if(!float.IsPositiveInfinity(playerHit))
            {
                ReceivePlayerHit(s.damage,-s.velocity,false);
                ReleaseHostileAt(i);
                if(health<=0){phase=Phase.Dying;deathAge=0;SilenceSentries();return;}
                continue;
            }
            if(s.life<=0){ReleaseHostileAt(i);continue;}
            s.p=to;Place(s.view,s.p+new Vector2(0,-32),5000);
        }
    }
    void SilenceSentries(){foreach(var e in enemies)e.sentry?.Freeze();}
}
