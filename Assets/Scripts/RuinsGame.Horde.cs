using System.Collections.Generic;
using Portfolio.Domain;
using UnityEngine;

public sealed partial class RuinsGame
{
    LeasePool<Enemy> enemyPool;
    readonly List<Enemy>[] enemyGrid=new List<Enemy>[40*28];
    readonly List<Enemy> nearby=new List<Enemy>(64);
    float regroupCooldown;
    void RegroupDistantSlimes(float dt)
    {
        regroupCooldown-=dt;if(regroupCooldown>0 || roomCleared)return;regroupCooldown=1;
        int moved=0;
        foreach(var e in enemies)
        {
            if(!e.pooled || e.hp<=0 || e.spawnDelay>0 || Vector2.Distance(e.p,player)<1200)continue;
            var screen=gameCamera.WorldToViewportPoint(new Vector3(e.p.x,720-e.p.y,0));
            if(screen.x>-.15f && screen.x<1.15f && screen.y>-.15f && screen.y<1.15f)continue;
            if(!TryFindEntry(out var p))break;
            // Preserve health/status and budget. Never relocate visible enemies or elites.
            e.p=p;e.spawnDelay=1;e.view.enabled=false;Place(e.view,p,(int)p.y);
            if(++moved==3)break;
        }
    }
    void InitializeHorde()
    {
        enemyPool=new LeasePool<Enemy>(()=>new Enemy{pooled=true,view=PoolSprite("Pooled slime")},e=>{
            e.p=Vector2.zero;e.hp=e.max=e.slow=e.burn=e.tick=e.hurt=e.reaction=e.frozen=e.spawnDelay=e.deathFade=e.breach=e.armor=0;
            e.fast=false;e.elite=null;e.sentry=null;e.spawnMarks=null;ResetSprite(e.view);
        },e=>{if(e.view!=null)Destroy(e.view.gameObject);},64,96);
        for(int i=0;i<enemyGrid.Length;i++)enemyGrid[i]=new List<Enemy>(8);
    }
    Enemy SpawnSlime(Vector2 p,float hp,bool fast,float delay)
    {
        var e=enemyPool.Rent();e.p=p;e.hp=e.max=hp;e.fast=fast;e.spawnDelay=delay;
        ShowPooled(e.view,"enemy/idle_down",p,fast?72:88,(int)p.y);e.view.enabled=delay<=0;
        enemies.Add(e);return e;
    }
    void ReleaseEnemy(Enemy e)
    {
        // Reused object identity must not remain in a live projectile's hit set.
        foreach(var shot in shots)shot.hit.Remove(e);shieldDashHits.Remove(e);
        if(e.pooled){enemyPool.Return(e);return;}
        e.elite?.Dispose();e.sentry?.Dispose();e.view.gameObject.SetActive(false);Destroy(e.view.gameObject);
    }
    void RecycleDefeated(float dt)
    {
        for(int i=enemies.Count-1;i>=0;i--)
        {
            var e=enemies[i];if(e.hp>0 || e.elite!=null)continue;
            if(e.sentry!=null)e.deathFade+=dt;
            if(e.deathFade<1.2f)continue;
            enemies.RemoveAt(i);ReleaseEnemy(e);
        }
    }
    void RebuildEnemyGrid()
    {
        foreach(var cell in enemyGrid)cell.Clear();
        foreach(var e in enemies)if(e.hp>0 && e.spawnDelay<=0)
            enemyGrid[Mathf.Clamp((int)(e.p.y/80),0,27)*40+Mathf.Clamp((int)(e.p.x/80),0,39)].Add(e);
    }
    List<Enemy> NearbyEnemies(Vector2 p)
    {
        nearby.Clear();int x=(int)(p.x/80),y=(int)(p.y/80);
        for(int row=Mathf.Max(0,y-1);row<=Mathf.Min(27,y+1);row++)
            for(int col=Mathf.Max(0,x-1);col<=Mathf.Min(39,x+1);col++)nearby.AddRange(enemyGrid[row*40+col]);
        return nearby;
    }
}
