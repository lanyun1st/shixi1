using Portfolio.Configuration;
using Portfolio.Presentation;
using UnityEngine;

// Adapter: translates gameplay observations into visual offsets. The presenter owns gait state.
public sealed partial class RuinsGame
{
    readonly HeroMotionPresenter heroMotion=new HeroMotionPresenter();
    AttackTimeline attackTimeline;
    Vector2 HeroGaitOffset=>heroMotion.Gait(attackTimeline);
    void ResetHeroMotion()=>heroMotion.Reset();
    void TickHeroMotion(float distance,float dt)=>heroMotion.Tick(distance,dt,dashTime>0,attackTimeline);
    void ApplyHeroMotion()
    {
        if(phase==Phase.Dying || phase==Phase.Result)return;
        Vector2 direction=shieldHero?meleeDirection:facing=="left"?Vector2.left:facing=="right"?Vector2.right:facing=="up"?Vector2.down:Vector2.up;
        var offset=heroMotion.Offset(attackTimeline,shieldHero && meleeRemaining>0,MeleeProgress,direction,!shieldHero && !chargeAttack.Charging,shotAge);
        heroView.transform.position+=new Vector3(offset.x,-offset.y,0);
    }
}
