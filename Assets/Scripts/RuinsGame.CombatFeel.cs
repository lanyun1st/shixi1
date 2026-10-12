using System.Collections.Generic;
using Portfolio.Domain;
using UnityEngine;
using Portfolio.Presentation;

public sealed partial class RuinsGame
{
    readonly ChargeAttack chargeAttack=new ChargeAttack();
    sealed class FirePatch { public Vector2 p;public float life=2.4f,tick;public SpriteRenderer view; }
    readonly List<FirePatch> firePatches=new List<FirePatch>();
    LineRenderer chargeRing;
    float hitStop,shake;
    Vector3 CameraHome=>FollowCameraPosition();
    void ResetCombatFeel()
    {
        chargeAttack.Reset();hitStop=shake=0;echoDelay=0;ClearBufferedInput();
        if(chargeRing!=null)chargeRing.enabled=false;
        foreach(var f in firePatches)firePool.Return(f);firePatches.Clear();
        if(gameCamera!=null)gameCamera.transform.position=CameraHome;
    }
    void HeavyFeedback()
    {
        hitStop=Mathf.Max(hitStop,.045f);shake=.14f;PlayTone(hitSound,.36f);
    }
    void TickFeedback(float dt)
    {
        reactionFeedback?.SetPaused(phase!=Phase.Playing);
        if(phase==Phase.Playing)reactionFeedback?.Tick(dt);
        if(phase==Phase.Playing)cameraFocus=Vector2.Lerp(cameraFocus,player,1-Mathf.Exp(-9*dt));
        if(phase!=Phase.Playing){chargeAttack.Cancel();if(chargeRing!=null)chargeRing.enabled=false;gameCamera.transform.position=CameraHome;return;}
        shake=Mathf.Max(0,shake-dt);
        float amplitude=Mathf.Max(shake/.14f*3,reactionFeedback?.ShakeAmplitude??0);
        gameCamera.transform.position=CameraHome+new Vector3(Mathf.Sin(Time.unscaledTime*91),Mathf.Cos(Time.unscaledTime*73),0)*amplitude;
    }
    void TickCharge(float dt,Vector2 aim)
    {
        chargeAttack.Tick(dt);
        if(bufferedPress && dashTime<=0)chargeAttack.Begin();
        bufferedPress=false;
        if(chargeAttack.Charging){facing=Direction(aim-player);shotAge=.06f;}
        if(bufferedRelease){ReleaseHeavy(bufferedAim);bufferedRelease=false;}
        if(!Input.GetMouseButton(1) && chargeAttack.Charging)chargeAttack.Cancel();
        RenderCharge();
    }
    void ReleaseHeavy(Vector2 target)
    {
        if(phase!=Phase.Playing || !chargeAttack.Release())return;
        var p=PrepareRangedAim(target,out var dir);
        shotAge=0;cooldown=.3f;fired++;
        SpawnShot(p,dir*900,65,loadout.Has(2)?6:4,true);
        SkillCue(ReactionKind.Muzzle,p,35,dir);PlayTone(shotSound,.35f);
    }
    void HeavyHit(Enemy e,Vector2 direction)
    {
        HeavyFeedback();
        if(e.hp>0 && e.elite==null){e.p=MoveSolid(e.p,direction.normalized*65,ArenaLayout.SlimeRadius);Place(e.view,e.p,(int)e.p.y);if(loadout.Has(1))e.frozen=1.2f;}
        if(loadout.Has(0))
        {
            SpawnFirePatch(e.p);
        }
    }
    void TickFire(float dt)
    {
        for(int i=firePatches.Count-1;i>=0;i--)
        {
            var patch=firePatches[i];patch.life-=dt;patch.tick-=dt;
            patch.view.color=new Color(1,.4f,.12f,Mathf.Min(.55f,patch.life*.5f));
            if(patch.life<=0){firePatches.RemoveAt(i);firePool.Return(patch);continue;}
            if(patch.tick>0)continue;patch.tick=.4f;
            foreach(var e in enemies)if(e.hp>0 && e.spawnDelay<=0 && BodyInRange(e,patch.p,70) && ClearSkillPath(patch.p,e.p))Damage(e,5,false);
        }
    }
    void RenderCharge()
    {
        if(chargeRing==null)
        {
            if(skillMaterial==null)skillMaterial=new Material(Shader.Find("Sprites/Default"));
            chargeRing=new GameObject("Charge progress").AddComponent<LineRenderer>();chargeRing.transform.SetParent(transform);
            chargeRing.sharedMaterial=skillMaterial;chargeRing.startWidth=chargeRing.endWidth=3;chargeRing.sortingOrder=4990;
        }
        chargeRing.enabled=chargeAttack.Charging;if(!chargeAttack.Charging)return;
        int count=2+Mathf.RoundToInt(30*chargeAttack.Fraction);chargeRing.positionCount=count;
        chargeRing.startColor=chargeRing.endColor=chargeAttack.Fraction>=1?new Color(1,.8f,.3f):new Color(.5f,.85f,1);
        for(int i=0;i<count;i++){float a=i*Mathf.PI*2/32;chargeRing.SetPosition(i,new Vector3(player.x+Mathf.Cos(a)*34,720-player.y+Mathf.Sin(a)*22,0));}
    }
    void TickEntryWaves(float dt)
    {
        foreach(var e in enemies)
        {
            if(e.hp<=0||e.spawnDelay<=0)continue;
            // Reserved placement stays fixed for the complete one-second telegraph.
            e.spawnDelay=Mathf.Max(0,e.spawnDelay-dt);
            if(e.spawnDelay>0 && e.spawnDelay<=1)
            {
                if(e.spawnMarks==null)
                {
                    DrawSpawnMarker(e.p,e.elite!=null?34:21);
                    e.spawnMarks=new[]{skillLines[skillLines.Count-2],skillLines[skillLines.Count-1]};
                }
                float progress=1-e.spawnDelay;
                float alpha=Mathf.SmoothStep(0,.8f,Mathf.Min(1,progress/.16f))*(.8f+.2f*Mathf.Sin(progress*Mathf.PI*3));
                foreach(var marker in e.spawnMarks){marker.remaining=.25f;marker.view.startColor=marker.view.endColor=new Color(.95f,.32f,.22f,alpha);}
                if(e.spawnDelay<.14f){e.view.enabled=true;e.view.color=new Color(1,1,1,1-e.spawnDelay/.14f);Place(e.view,e.p,(int)e.p.y);}
            }
            if(e.spawnDelay==0)
            {
                e.view.enabled=true;e.view.color=Color.white;Place(e.view,e.p,(int)e.p.y);
                if(e.spawnMarks!=null){foreach(var marker in e.spawnMarks)marker.remaining=0;e.spawnMarks=null;}
            }
        }
    }
    void DrawSpawnMarker(Vector2 at,float radius)
    {
        // A quiet ground X persists throughout the warning; no repeated debris bursts.
        for(int branch=0;branch<2;branch++)
        {
            var dir=new Vector2(1,branch==0?.55f:-.55f)*radius;
            var line=SpawnLine(2,new Color(.95f,.32f,.22f,.65f));line.sortingOrder=-5;line.startWidth=line.endWidth=3;
            line.numCapVertices=3;
            var a=at-dir;var b=at+dir;line.SetPosition(0,new Vector3(a.x,720-a.y,0));line.SetPosition(1,new Vector3(b.x,720-b.y,0));
        }
    }
    void BreakProp(Obstacle o)
    {
        o.view.enabled=false;SkillCue(ReactionKind.Debris,o.p,100);PlayTone(hitSound,.35f);
        foreach(var e in enemies)if(e.hp>0&&e.spawnDelay<=0&&e.elite==null&&Vector2.Distance(e.p,o.p)<140)
        {var away=e.p-o.p;e.p=MoveSolid(e.p,(away.sqrMagnitude>.01f?away.normalized:Vector2.down)*65,ArenaLayout.SlimeRadius);e.hurt=.25f;}
    }
}
