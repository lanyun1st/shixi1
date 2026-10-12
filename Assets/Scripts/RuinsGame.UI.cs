using Portfolio.UI;
using Portfolio.Domain;
using UnityEngine;

// Adapter boundary: views receive snapshots, commands are validated by gameplay.
public sealed partial class RuinsGame : IGameUiCommands
{
    PortfolioCanvasUI canvasUI;
    readonly Vector2[] navigationEnemies=new Vector2[64];
    readonly int[] navigationKinds=new int[64];
    readonly Rect[] navigationObstacles=new Rect[128];
    int abilityMask=-1,abilityMomentum=-1,abilityCombo=-1,abilityBudget=-1;
    bool abilityGuard,abilityCounter;string cachedAbilityHint;
    string AbilityHint()
    {
        if(!shieldHero)return null;
        int budget=Mathf.RoundToInt(shieldDefense.RecoveryBudget);bool guard=shieldDefense.Guarding,counter=shieldDefense.CounterRemaining>0;
        if(abilityMask==loadout.Mask && abilityMomentum==momentum && abilityCombo==combo && abilityBudget==budget && abilityGuard==guard && abilityCounter==counter)return cachedAbilityHint;
        abilityMask=loadout.Mask;abilityMomentum=momentum;abilityCombo=combo;abilityBudget=budget;abilityGuard=guard;abilityCounter=counter;
        return cachedAbilityHint=(guard?"正面举盾 · ":"右键举盾 · ")+(loadout.Has(0)?"蓄势 "+momentum+"/3 · ":"")+(loadout.Has(1)?"连击 "+combo+"/3 · ":"")+(counter?"反击就绪 · ":"")+"恢复额度 "+budget+" / 30";
    }
    void InitializeUi()
    {
        var prefab=Resources.Load<GameObject>("UI/PortfolioCanvas");
        if(prefab==null){Debug.LogError("Missing PortfolioCanvas prefab. Use Portfolio/UI/Rebuild Generated Canvas Prefab.");enabled=false;return;}
        canvasUI=Instantiate(prefab,transform).GetComponent<PortfolioCanvasUI>();
        canvasUI.Bind(this,vitality);RefreshUi();
    }
    public GameUiSnapshot CaptureUiState()
    {
        var state=new GameUiSnapshot
        {
            mode=ViewMode(),room=room,upgrade=upgrade,kills=kills,shots=fired,hits=hits,
            upgradeMask=loadout.Mask,buildName=BuildName(),routeName=RouteName(),
            chargeFraction=chargeAttack.Fraction,chargeCooldown=chargeAttack.Cooldown,charging=chargeAttack.Charging,
            elapsed=elapsed,dashRemaining=dashCooldown,dashDuration=DashCooldownDuration,
            shieldHero=shieldHero,characterName=shieldHero?"盾卫":"遗迹修复师",
            regionName=RegionName,playerPosition=player,portalPosition=ArenaLayout.Portal,mapSize=new Vector2(ArenaLayout.Width,ArenaLayout.Height),
            cameraPosition=new Vector2(CameraHome.x,720-CameraHome.y),cameraSize=new Vector2(gameCamera.orthographicSize*2*gameCamera.aspect,gameCamera.orthographicSize*2),
            enemyPositions=navigationEnemies,
            enemyKinds=navigationKinds,mapObstacles=navigationObstacles,
            abilityHint=AbilityHint(),
            wave=waves==null?0:waves.Wave,waveTotal=waves==null?0:waves.TotalWaves,
            rewardTitle=rewardStage==RewardStage.Core?"选择起始核心":rewardStage==RewardStage.MidRoom?"补给 · 继续本关":room==2?"最终强化 · 组合进阶":"选择联动组件",
            rewardHint=rewardStage==RewardStage.Core?(shieldHero?"盾反蓄势 / 三连重锤 / 盾冲破阵":"火焰＋连锁 / 冰霜＋穿透 / 冲刺＋多重"):rewardStage==RewardStage.MidRoom?"保留当前房间进度 · 恢复 10 生命":"进入下一房间 · 恢复 25 生命 · 第三关自动强化已成型组合",
            cleared=roomCleared,canInteract=phase==Phase.Playing && roomCleared && Vector2.Distance(player,ArenaLayout.Portal)<=80,won=won
        };
        foreach(var e in enemies)
        {
            if(e.hp>0){state.remaining++;if(state.enemyPositionCount<navigationEnemies.Length){int i=state.enemyPositionCount++;navigationEnemies[i]=e.p;navigationKinds[i]=e.spawnDelay>0?3:e.elite!=null?2:e.sentry!=null?1:0;}}
            if(e.hp>0 && e.spawnDelay>0)state.pending++;
            if(e.elite==null || e.spawnDelay>0 || e.hp<=0)continue;
            state.eliteHealth=Mathf.Max(0,e.hp);state.eliteMaximum=e.max;state.eliteStunned=e.elite.Brain.State==EliteChargeBrain.Mode.Stunned;
            switch(e.elite.Brain.State)
            {
                case EliteChargeBrain.Mode.Windup:state.eliteState=shieldHero?"破防蓄力 · 侧向躲避":"蓄力 · 准备躲避";break;
                case EliteChargeBrain.Mode.Charge:state.eliteState=shieldHero?"破防冲锋 · 不可格挡":"冲锋";break;
                case EliteChargeBrain.Mode.Recover:state.eliteState="恢复中";break;
                default:state.eliteState="追击 · 引诱撞柱";break;
            }
        }
        foreach(var obstacle in obstacles)if(obstacle.hp!=0 && state.mapObstacleCount<navigationObstacles.Length)navigationObstacles[state.mapObstacleCount++]=obstacle.footprint;
        return state;
    }
    void LateUpdate(){if(canvasUI!=null)RefreshUi();}
    UiMode ViewMode()
    {
        switch(phase)
        {
            case Phase.Playing:return UiMode.Playing;
            case Phase.Paused:return UiMode.Paused;
            case Phase.Upgrade:return UiMode.Upgrade;
            case Phase.Dying:return UiMode.Dying;
            case Phase.Result:return UiMode.Result;
            default:return UiMode.Title;
        }
    }
    void RefreshUi()
    {using(HudMarker.Auto())RefreshUiCore();}
    static readonly Unity.Profiling.ProfilerMarker HudMarker=new Unity.Profiling.ProfilerMarker("Portfolio.HUD");
    void RefreshUiCore()
    {
        canvasUI.Render(CaptureUiState(),choices);
        if(heroView!=null && heroView.sprite!=null)
        {
            var bounds=heroView.bounds;var a=gameCamera.WorldToScreenPoint(bounds.min);var b=gameCamera.WorldToScreenPoint(bounds.max);
            canvasUI.AvoidPlayer(Rect.MinMaxRect(a.x,a.y,b.x,b.y));
        }
        int index=0;
        foreach(var e in enemies)if(e.hp>0 && e.spawnDelay<=0 && e.elite==null && (e.sentry!=null || e.hp<e.max))
        {
            var screen=gameCamera.WorldToScreenPoint(new Vector3(e.p.x,720-e.p.y+(e.sentry!=null?90:58),0));
            if(screen.x<0 || screen.x>Screen.width || screen.y<0 || screen.y>Screen.height)continue;
            canvasUI.EnemyBar(index++,screen,e.hp/e.max);
        }
        canvasUI.HideEnemyBarsFrom(index);
    }
    void IGameUiCommands.RequestStart()
    {
        if(phase!=Phase.Title && phase!=Phase.Paused && phase!=Phase.Result)return;
        StartRun();BeginCoreChoice();RefreshUi();
    }
    void IGameUiCommands.RequestPause(bool paused)
    {
        if(phase!=Phase.Playing && phase!=Phase.Paused)return;
        phase=paused?Phase.Paused:Phase.Playing;if(paused){chargeAttack.Cancel();shieldDefense.SetGuard(false);ClearBufferedInput();}RefreshUi();
    }
    void IGameUiCommands.RequestUpgrade(int id)
    {
        if(phase!=Phase.Upgrade || System.Array.IndexOf(choices,id)<0 || !loadout.Add(id))return;
        upgrade=id;phase=Phase.Playing;
        if(rewardStage==RewardStage.Core)core=id;
        else if(rewardStage==RewardStage.MidRoom)vitality.Heal(10);
        else
        {
            room++;player=ArenaLayout.Entrance;vitality.Heal(25);invincible=.5f;dashTime=0;dashGate.Reset();SpawnRoom();
        }
        RefreshUi();
    }
}
