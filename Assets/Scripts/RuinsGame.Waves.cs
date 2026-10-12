using Portfolio.Domain;
using UnityEngine;

public sealed partial class RuinsGame
{
    WaveDirector waves;
    float entryCooldown;
    bool midRewardGiven;
    enum RewardStage { Core, MidRoom, Exit }
    RewardStage rewardStage;
    int core=-1;
    void BeginCoreChoice()
    {
        rewardStage=RewardStage.Core;choices=new[]{0,1,5};phase=Phase.Upgrade;
    }
    void BeginReward(RewardStage stage)
    {
        rewardStage=stage;PrepareChoices();
        if((stage==RewardStage.Exit || stage==RewardStage.MidRoom) && room==1)
        {
            int component=core==0?3:core==1?2:4;
            if(!loadout.Has(component) && System.Array.IndexOf(choices,component)<0)choices[0]=component;
        }
        chargeAttack.Cancel();ClearBufferedInput();phase=Phase.Upgrade;
    }
    void TickWaves(float dt)
    {
        if(waves==null)return;
        int alive=0;bool windup=false,guardDead=false;
        foreach(var e in enemies)
        {
            if(e.hp>0)alive++;
            if(e.elite!=null){guardDead=e.hp<=0;windup=e.elite.Brain.State==EliteChargeBrain.Mode.Windup;}
        }
        // The guard appears in the final finite wave. Its death never deletes the room budget.
        // Mid-room rewards happen in an actual lull, never over active enemies.
        if(room<3 && !midRewardGiven && waves.Wave==2 && waves.Pending==0 && alive==0)
        {midRewardGiven=true;BeginReward(RewardStage.MidRoom);return;}
        bool awaitingLull=room<3 && !midRewardGiven && waves.Wave==2 && waves.Pending==0;
        if(!awaitingLull)waves.Tick(dt,alive,windup);
        entryCooldown-=dt;
        if(entryCooldown>0 || !waves.CanSpawn(alive))return;
        for(int batch=0;batch<3 && waves.CanSpawn(alive);batch++)
        {
        if(!TryFindEntry(out var p))break; // No safe slot: defer, do not force an unfair spawn.
        bool guard=waves.NextIsGuard;
        if(!windup && WantSentry())
        {
            SpawnSentry(p,1);rangedWave=waves.Wave;waves.CommitSpawn();alive++;continue;
        }
        bool fast=!guard && room>=2 && waves.Pending%5==0;
        float hp=guard?420:fast?30:24;
        if(!guard)SpawnSlime(p,hp,fast,1);
        else
        {
        var view=Make("elite/down_idle",p,190,(int)p.y);
        view.enabled=false;
        enemies.Add(new Enemy{p=p,hp=hp,max=hp,fast=fast,spawnDelay=1,view=view,
            elite=guard?new EliteGuardActor(view,Art,MoveSolid):null});
        }
        waves.CommitSpawn();alive++;
        }
        entryCooldown=.18f;
    }
    bool TryFindEntry(out Vector2 result)
    {
        // Two opposing sectors move with the player, leaving lateral escape lanes.
        result=Vector2.zero;float best=-1;
        for(int attempt=0;attempt<64;attempt++)
        {
            float angle=((waves.Wave+room)*37+(attempt%2)*180+Random.Range(-65f,65f))*Mathf.Deg2Rad;
            var p=player+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*Random.Range(350f,510f);
            if(p!=ArenaLayout.Clamp(p,48))continue;
            float distance=Vector2.Distance(player,p);if(distance<320)continue;
            bool free=true;
            foreach(var decoration in spawnExclusions)if(Vector2.Distance(p,decoration)<80){free=false;break;}
            foreach(var o in obstacles)if(o.hp!=0 && ArenaLayout.CircleOverlaps(p,40,o.footprint)){free=false;break;}
            foreach(var e in enemies)if(e.hp>0 && Vector2.Distance(p,e.p)<(e.elite!=null || waves.NextIsGuard?110:52)){free=false;break;}
            if(free){best=distance;result=p;break;}
        }
        return best>=0;
    }
}
