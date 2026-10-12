using System.Collections.Generic;
using UnityEngine;
using Portfolio.Domain;

public sealed partial class RuinsGame
{
    sealed class Obstacle { public Vector2 p; public Rect footprint; public int hp; public SpriteRenderer view; }
    readonly List<Obstacle> obstacles=new List<Obstacle>();
    bool roomCleared;
    readonly Portfolio.Domain.CooldownGate dashGate=new Portfolio.Domain.CooldownGate();
    float dashCooldown=>dashGate.Remaining;
    float dashTime;
    Vector2 dashDirection;
    AudioSource sound;
    AudioClip shotSound,hitSound,hurtSound;
    void SetupFeedback()
    {
        sound=gameObject.AddComponent<AudioSource>();sound.playOnAwake=false;sound.spatialBlend=0;
        shotSound=Tone("Pulse",440,.06f);hitSound=Tone("Hit",160,.09f);hurtSound=Tone("Hurt",80,.18f);
    }
    AudioClip Tone(string name,float frequency,float seconds)
    {
        int count=(int)(22050*seconds);var samples=new float[count];
        for(int i=0;i<count;i++)samples[i]=Mathf.Sin(i*frequency*Mathf.PI*2/22050)*Mathf.Pow(1-i/(float)count,2)*.4f;
        var clip=AudioClip.Create(name,count,1,22050,false);clip.SetData(samples,0);return clip;
    }
    float nextOrdinaryHitSound;
    void PlayTone(AudioClip clip,float volume)
    {
        if(sound==null || clip==null)return;
        // Do not stack dozens of identical hit voices in one chain burst. Heavy cues bypass this gate.
        if(clip==hitSound && volume<=.2f){if(Time.unscaledTime<nextOrdinaryHitSound)return;nextOrdinaryHitSound=Time.unscaledTime+.035f;}
        sound.PlayOneShot(clip,volume);
    }
    void ResetObstacles()
    {
        foreach(var o in obstacles){o.view.gameObject.SetActive(false);Destroy(o.view.gameObject);}obstacles.Clear();
        // Eight islands around broad intersecting lanes. No obstacles on entrance/portal axis.
        foreach(float y in new[]{600f,1060f,1510f,1980f})foreach(float x in new[]{700f,2500f})
        {
            float shift=(room==2?100:room==3?-100:0)*(x<1600?1:-1);
            var p=new Vector2(x+shift,y);
            AddObstacle(room==3?"broken_column":"column",p,new Vector2(62,30),-1,85);
            AddObstacle(room==2?"pot":"barrel",p+new Vector2(120,75),new Vector2(52,28),2,66);
            AddObstacle("crate",p+new Vector2(-100,85),new Vector2(58,32),3,70);
            if(room>=2)AddObstacle("rubble",p+new Vector2(180,-110),new Vector2(90,38),-1,105);
        }
        foreach(float x in new[]{1250f,1950f})
        {
            AddObstacle("column",new Vector2(x,800),new Vector2(62,30),-1,85);
            AddObstacle("column",new Vector2(x,1360),new Vector2(62,30),-1,85);
        }
        AddObstacle("crystal_pedestal",new Vector2(room==2?930:2270,1150),new Vector2(68,36),-1,110);
    }
    void AddObstacle(string sprite,Vector2 p,Vector2 footprint,int hp,float width)
    {
        var view=Make("props/"+sprite,p,width,(int)p.y);
        // Imported props already use a foot pivot: do not apply a second height offset.
        obstacles.Add(new Obstacle{p=p,footprint=new Rect(p.x-footprint.x*.5f,p.y-footprint.y*.65f,footprint.x,footprint.y),hp=hp,view=view});
    }
    Vector2 MoveSolid(Vector2 origin,Vector2 delta,float radius)
    {
        int steps=Mathf.Max(1,Mathf.CeilToInt(delta.magnitude/8));Vector2 p=origin;
        for(int i=0;i<steps;i++)
        {
            p+=delta/steps;
            foreach(var o in obstacles)
            {
                if(o.hp!=0)p=ArenaLayout.Resolve(p,radius,o.footprint);
            }
            p=ArenaLayout.Clamp(p,radius);
        }
        return p;
    }
    void TryDash(Vector2 direction)
    {
        if(phase!=Phase.Playing || !dashGate.TryStart(DashCooldownDuration))return;
        chargeAttack.Cancel();
        if(direction.sqrMagnitude<.01f)direction=facing=="left"?Vector2.left:facing=="right"?Vector2.right:facing=="up"?Vector2.down:Vector2.up;
        dashDirection=direction.normalized;dashTime=DashDuration;
        if(shieldHero)BeginShieldDash(dashDirection);else invincible=Mathf.Max(invincible,settings.dashInvulnerability);
        PlayTone(shotSound,.2f);
        if(!shieldHero && loadout.Has(5)){DashNova();if(loadout.HasRoute(SynergyId.DashEcho)){echoPosition=player;echoDelay=DashDuration+.25f;}}
    }
    bool BlockShot(Vector2 from,Vector2 to)
    {
        Vector2 delta=to-from;int steps=Mathf.Max(1,Mathf.CeilToInt(delta.magnitude/6));
        for(int i=0;i<=steps;i++)
        {
            Vector2 p=from+delta*(i/(float)steps);
            if(!ArenaLayout.Bounds.Contains(p))return true;
            foreach(var o in obstacles)
            {
                if(o.hp==0 || !ArenaLayout.CircleOverlaps(p,5,o.footprint))continue;
                SpawnImpact(p,70);
                if(o.hp>0 && --o.hp==0)BreakProp(o);
                else PlayTone(hitSound,.12f);
                return true;
            }
        }
        return false;
    }
    void TryPortal()
    {
        if(!roomCleared || phase!=Phase.Playing || Vector2.Distance(player,ArenaLayout.Portal)>80)return;
        if(room==3){won=true;phase=Phase.Result;}
        else BeginReward(RewardStage.Exit);
    }
    void OnDrawGizmosSelected()
    {
        Gizmos.color=Color.cyan;
        foreach(var o in obstacles)if(o.hp!=0){var r=o.footprint;Gizmos.DrawWireCube(new Vector3(r.center.x,720-r.center.y,0),new Vector3(r.width,r.height,1));}
        Gizmos.color=Color.green;Gizmos.DrawWireSphere(new Vector3(player.x,720-player.y,0),ArenaLayout.PlayerRadius);
    }
}
