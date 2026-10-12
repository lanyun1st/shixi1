using System.Collections.Generic;
using Portfolio.Domain;
using Portfolio.UI;
using Portfolio.Presentation;
using UnityEngine;

public sealed partial class RuinsGame : ICharacterUiCommands
{
    bool shieldHero;
    readonly ShieldDefense shieldDefense=new ShieldDefense();
    readonly HashSet<Enemy> shieldDashHits=new HashSet<Enemy>();
    Vector2 shieldAim=Vector2.down,meleeDirection;
    float meleeRemaining,shieldRecoil,meleeDuration=.48f;
    float MeleeProgress=>Mathf.Clamp01(1-meleeRemaining/Mathf.Max(.01f,meleeDuration));
    bool meleeResolved,meleeCounter;
    int momentum,swingMomentum,combo;
    float momentumTime,blockStackGate,comboTime;
    bool heavySwing;
    float DashDuration=>shieldHero?.24f:settings.dashDuration;
    float DashCooldownDuration=>(shieldHero?2:settings.dashCooldown)*(shieldHero?(loadout.Has(8)?.75f:1):loadout.DashCooldownMultiplier);
    void ICharacterUiCommands.RequestCharacter(bool shield)
    {
        if(phase!=Phase.Title && phase!=Phase.Result)return;
        shieldHero=shield;ResetShield();vitality.Reset(shield?ShieldDefense.MaxHealth:100);
        heroView.transform.rotation=Quaternion.identity;AnimatePlayer();RefreshUi();
    }
    void ResetShield()
    {
        shieldDefense.Reset();shieldDashHits.Clear();meleeRemaining=shieldRecoil=0;
        meleeResolved=meleeCounter=false;shieldAim=Vector2.down;
        momentum=swingMomentum=combo=0;momentumTime=blockStackGate=comboTime=0;heavySwing=false;
        if(heroView!=null)heroView.transform.rotation=Quaternion.identity;
    }
    void TickShield(float dt,Vector2 aim)
    {
        if(phase!=Phase.Playing)return;
        var direction=aim-player;
        if(direction.sqrMagnitude>.01f && dashTime<=0 && meleeRemaining<=0)shieldAim=direction.normalized;
        shieldRecoil=Mathf.Max(0,shieldRecoil-dt);
        blockStackGate-=dt;momentumTime-=dt;comboTime-=dt;
        if(momentumTime<=0)momentum=0;if(comboTime<=0)combo=0;
        // Recovery is unavailable after clear, but cooldowns are no longer relevant there.
        shieldDefense.Tick(dt,vitality,!roomCleared);
        bool held=Input.GetMouseButton(1) && (canvasUI==null || !canvasUI.BlocksPointer);
        shieldDefense.SetGuard(dashTime>0 || held && meleeRemaining<=0,dashTime<=0);
        bufferedPress=bufferedRelease=false;
        if(meleeRemaining>0)
        {
            meleeRemaining=Mathf.Max(0,meleeRemaining-dt);
            if(!meleeResolved && MeleeProgress>=attackTimeline.hitPhase){meleeResolved=true;ResolveMelee();}
        }
        if(meleeRemaining<=0 && dashTime<=0)facing=Direction(shieldAim);
    }
    void BeginMelee(Vector2 target)
    {
        if(phase!=Phase.Playing || cooldown>0 || dashTime>0 || meleeRemaining>0 || shieldDefense.Guarding)return;
        var dir=target-player;meleeDirection=dir.sqrMagnitude>.01f?dir.normalized:shieldAim;
        shieldAim=meleeDirection;facing=Direction(meleeDirection);shotAge=0;cooldown=loadout.Has(6)?attackTimeline.fastMeleeCooldown:attackTimeline.meleeCooldown;fired++;
        meleeDuration=loadout.Has(6)?attackTimeline.fastMeleeDuration:attackTimeline.meleeDuration;meleeRemaining=meleeDuration;meleeResolved=false;meleeCounter=shieldDefense.ConsumeCounter();
        swingMomentum=momentum;momentum=0;
        combo=loadout.Has(1)?combo%3+1:0;comboTime=1.25f;heavySwing=combo==3;
        PlayTone(shotSound,.18f);
    }
    bool InMeleeArc(Vector2 target,float radius,float minDot)
    {
        var delta=target-player;return delta.sqrMagnitude<=radius*radius &&
            (delta.sqrMagnitude<1 || Vector2.Dot(delta.normalized,meleeDirection)>=minDot);
    }
    void ResolveMelee()
    {
        float reach=(loadout.Has(7)?145:110)+(heavySwing?25:0);
        float dot=heavySwing?0:.5f;
        float damage=36*(meleeCounter?1.5f:1)*(1+swingMomentum*.15f)*(heavySwing?1.8f:1);
        bool connected=false;
        foreach(var e in enemies)
        {
            if(e.hp<=0 || e.spawnDelay>0 || !InMeleeArc(e.p,reach,dot) || !ClearSkillPath(player,e.p))continue;
            float extra=(e.breach>0 && loadout.Has(4)?1.5f:1)*(e.armor>0?1.25f:1);
            if(extra>1)SkillCue(ReactionKind.ArmorBreak,e.p,45,meleeDirection);
            e.breach=0;Damage(e,damage*extra,false);connected=true;
            if(heavySwing && loadout.Has(2) && e.hp>0)e.armor=4;
            if(meleeCounter && e.hp>0 && e.elite==null)e.p=MoveSolid(e.p,meleeDirection*40,ArenaLayout.SlimeRadius);
        }
        // Destructibles use their existing hit-count durability; no through-wall strikes.
        foreach(var o in obstacles)
        {
            if(o.hp<=0 || !InMeleeArc(o.p,reach,dot))continue;
            var near=Vector2.MoveTowards(o.p,player,Mathf.Max(o.footprint.width,o.footprint.height)*.6f+4);
            if(!ClearSkillPath(player,near))continue;
            if(--o.hp==0)BreakProp(o);else SpawnImpact(o.p,60);
            connected=true;
        }
        if(connected){hits++;HeavyFeedback();}
        if(connected && swingMomentum>0 && loadout.HasRoute(SynergyId.ShieldCounter))
        {
            ShieldCone(reach+55,18,50);
            if(room==3 && swingMomentum==3)shieldDefense.RecoverCounter(vitality,3);
        }
        if(heavySwing && loadout.HasRoute(SynergyId.HeavySlam) && room==3)
        {
            SkillCue(ReactionKind.Slam,player,180);
            foreach(var e in enemies)if(e.hp>0 && e.spawnDelay<=0 && Vector2.Distance(player,e.p)<180 && ClearSkillPath(player,e.p))
            {Damage(e,24,false);if(e.elite==null)e.p=MoveSolid(e.p,(e.p-player).normalized*40,ArenaLayout.SlimeRadius);}
        }
        SkillCue(heavySwing?ReactionKind.HeavySlash:ReactionKind.Slash,player,reach,meleeDirection);
    }
    void ShieldCone(float reach,float damage,float push)
    {
        SkillCue(ReactionKind.ShieldCone,player,reach,meleeDirection);
        foreach(var e in enemies)if(e.hp>0 && e.spawnDelay<=0 && InMeleeArc(e.p,reach,.4f) && ClearSkillPath(player,e.p))
        {Damage(e,damage,false);if(e.elite==null)e.p=MoveSolid(e.p,meleeDirection*push,ArenaLayout.SlimeRadius);}
    }
    void BeginShieldDash(Vector2 direction)
    {
        shieldAim=direction;facing=Direction(direction);
        meleeRemaining=0;meleeResolved=true;shieldDashHits.Clear();
        shieldDefense.SetGuard(false);shieldDefense.SetGuard(true,false);
    }
    void TickShieldDash(Vector2 from,Vector2 to)
    {
        foreach(var e in enemies)
        {
            if(e.hp<=0 || e.spawnDelay>0 || shieldDashHits.Contains(e))continue;
            if(float.IsPositiveInfinity(CircleEntry(from,to,e.p,42)) || !ClearSkillPath(to,e.p))continue;
            shieldDashHits.Add(e);Damage(e,22,false);SkillCue(ReactionKind.DashImpact,e.p,50,dashDirection);
            if(loadout.Has(5))e.breach=2;
            if(e.elite==null && e.hp>0)
            {
                float push=loadout.Has(5)?110:70;var before=e.p;
                e.p=MoveSolid(e.p,dashDirection*push,ArenaLayout.SlimeRadius);
                if(room==3 && loadout.HasRoute(SynergyId.ShieldBreach) && Vector2.Dot(e.p-before,dashDirection)<push*.7f)
                {Damage(e,32,false);e.frozen=.45f;SkillCue(ReactionKind.Slam,e.p,65);}
            }
        }
    }
    void MovePlayer(Vector2 move,float dt)
    {
        var beforeMove=player;bool wasDashing=dashTime>0;
        var displacement=(wasDashing?dashDirection*(shieldHero?580:settings.dashSpeed):move*settings.moveSpeed*(shieldHero?.85f:1)*(shieldHero?1:loadout.MovementMultiplier)*(shieldHero && shieldDefense.Guarding?.5f:chargeAttack.Charging?.6f:1))*dt;
        player=MoveSolid(player,displacement,ArenaLayout.PlayerRadius);
        if(shieldHero && wasDashing)
        {
            TickShieldDash(beforeMove,player);
            if(Vector2.Dot(player-beforeMove,dashDirection)<displacement.magnitude*.75f)dashTime=0;
        }
    }
    void ReceivePlayerHit(float amount,Vector2 towardSource,bool unblockable)
    {
        if(phase!=Phase.Playing || invincible>0)return;
        bool perfect=false;
        if(shieldHero)
        {
            var dir=towardSource.sqrMagnitude>.001f?towardSource.normalized:-shieldAim;
            amount=shieldDefense.Resolve(amount,Vector2.Dot(shieldAim,dir),unblockable,out perfect);
            if(shieldDefense.Guarding && !unblockable && Vector2.Dot(shieldAim,dir)>=.5f)
            {
                shieldRecoil=.16f;SkillCue(ReactionKind.Guard,player,45,shieldAim);
                PlayTone(hitSound,.25f);
                if(loadout.Has(0) && (perfect || blockStackGate<=0))
                {momentum=Mathf.Min(3,momentum+(perfect?2:1));momentumTime=6;blockStackGate=.25f;}
            }
            if(perfect)
            {
                invincible=.12f;
                return;
            }
        }
        vitality.TakeDamage(amount);invincible=.8f;hurt=.2f;PlayTone(hurtSound,.3f);
        if(health<=0){phase=Phase.Dying;deathAge=0;shieldDefense.SetGuard(false);meleeRemaining=dashTime=0;SilenceSentries();}
    }
    void AnimateShield()
    {
        string action="idle";int frame=0;
        if(dashTime>0){action="dash";frame=dashTime>.16f?0:1;}
        else if(meleeRemaining>0){action="attack";frame=attackTimeline.MeleeFrame(MeleeProgress);}
        else if(shieldDefense.Guarding){action="block";frame=shieldRecoil>0?1:0;}
        else if(moving){action="move";frame=heroMotion.WalkFrame;}
        if(phase==Phase.Dying || phase==Phase.Result && !won){action="idle";frame=0;}
        SetSprite(heroView,"shield/"+facing+"_"+action+"_"+frame,156);
        heroView.flipX=false;Place(heroView,player,(int)player.y);
        ApplyHeroMotion();
        heroView.transform.rotation=Quaternion.Euler(0,0,phase==Phase.Dying || phase==Phase.Result && !won?Mathf.Min(1,deathAge/.65f)*-75:0);
        heroView.color=hurt>0?new Color(1,.55f,.4f):invincible>0 && (int)(elapsed*18)%2==0?new Color(1,1,1,.5f):Color.white;
    }
}
