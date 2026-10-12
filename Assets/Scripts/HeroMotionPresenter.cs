using Portfolio.Configuration;
using UnityEngine;

namespace Portfolio.Presentation
{
    // Owns only presentation state; no scene lookup, enemy references, input or damage calls.
    public sealed class HeroMotionPresenter
    {
        public float StridePhase { get; private set; }
        float blend;
        public int WalkFrame=>(int)(StridePhase/Mathf.PI)%2;
        public void Reset(){StridePhase=blend=0;}
        public void Tick(float distance,float dt,bool dashing,AttackTimeline profile)
        {
            bool walking=distance>.01f && !dashing;
            if(walking)StridePhase=Mathf.Repeat(StridePhase+distance*Mathf.PI/profile.stepDistance,Mathf.PI*2);
            blend=Mathf.MoveTowards(blend,walking?1:0,Mathf.Max(0,dt)*profile.blendSpeed);
        }
        public Vector2 Gait(AttackTimeline profile)=>new Vector2(0,-Mathf.Abs(Mathf.Sin(StridePhase))*profile.bobHeight*blend);
        public Vector2 Offset(AttackTimeline profile,bool melee,float progress,Vector2 attackDirection,bool firing,float shotAge)
        {
            var offset=Gait(profile);
            if(melee)
            {
                float push=progress<profile.hitPhase?-profile.windupPull*Mathf.Sin(progress/profile.hitPhase*Mathf.PI):
                    profile.followThrough*Mathf.Sin((progress-profile.hitPhase)/(1-profile.hitPhase)*Mathf.PI);
                offset+=attackDirection*push;
            }
            else if(firing && shotAge<profile.recoverEnd)offset-=attackDirection*(profile.recoilDistance*Mathf.Sin(shotAge/profile.recoverEnd*Mathf.PI));
            return offset;
        }
    }
}
