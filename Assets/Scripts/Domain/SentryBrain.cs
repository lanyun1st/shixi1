using System;
using UnityEngine;

namespace Portfolio.Domain
{
    // Immutable per-run snapshot. No scene, input, rendering or resource access.
    public sealed class SentryRules
    {
        public readonly float Windup,Fire,Recovery,Range,KeepDistance,RetreatDuration;
        public SentryRules(float windup=.9f,float fire=.12f,float recovery=1.6f,float range=530,float keepDistance=220,float retreatDuration=.45f)
        {
            foreach(float value in new[]{windup,fire,recovery,range,keepDistance,retreatDuration})
                if(value<=0 || float.IsNaN(value) || float.IsInfinity(value))throw new ArgumentOutOfRangeException(nameof(value));
            if(keepDistance>=range)throw new ArgumentOutOfRangeException(nameof(keepDistance),"Keep distance must be below attack range");
            Windup=windup;Fire=fire;Recovery=recovery;Range=range;KeepDistance=keepDistance;RetreatDuration=retreatDuration;
        }
    }
    public sealed class SentryBrain
    {
        public enum Mode { Approach, Retreat, Windup, Fire, Recover, Dead }
        readonly SentryRules rules;
        bool retreated;
        public Mode State { get; private set; } = Mode.Approach;
        public float Remaining { get; private set; } = .5f;
        public Vector2 LockedDirection { get; private set; } = Vector2.down;
        public Vector2 MoveDirection { get; private set; }
        public SentryBrain(SentryRules rules){this.rules=rules??throw new ArgumentNullException(nameof(rules));}
        public bool Tick(float dt,Vector2 toTarget,bool clearPath)
        {
            if(dt<0 || float.IsNaN(dt) || float.IsInfinity(dt))throw new ArgumentOutOfRangeException(nameof(dt));
            MoveDirection=Vector2.zero;
            if(dt==0 || State==Mode.Dead)return false;
            Remaining=Mathf.Max(0,Remaining-dt);
            var direction=toTarget.sqrMagnitude>.001f?toTarget.normalized:Vector2.down;
            switch(State)
            {
                case Mode.Approach:
                    if(!retreated && toTarget.magnitude<rules.KeepDistance)
                    {retreated=true;State=Mode.Retreat;Remaining=rules.RetreatDuration;LockedDirection=-direction;MoveDirection=LockedDirection;}
                    else if(clearPath && toTarget.magnitude<=rules.Range && Remaining<=0)
                    {State=Mode.Windup;Remaining=rules.Windup;LockedDirection=direction;}
                    else if(toTarget.magnitude>rules.Range || !clearPath)MoveDirection=direction;
                    break;
                case Mode.Retreat:
                    MoveDirection=LockedDirection;
                    if(Remaining<=0){State=Mode.Approach;MoveDirection=Vector2.zero;}
                    break;
                case Mode.Windup:
                    if(Remaining<=0){State=Mode.Fire;Remaining=rules.Fire;return true;}
                    break;
                case Mode.Fire:
                    if(Remaining<=0){State=Mode.Recover;Remaining=rules.Recovery;}
                    break;
                case Mode.Recover:
                    if(Remaining<=0){State=Mode.Approach;retreated=false;}
                    break;
            }
            return false;
        }
        public void Interrupt()
        {
            MoveDirection=Vector2.zero;
            if(State==Mode.Windup || State==Mode.Fire){State=Mode.Recover;Remaining=rules.Recovery;}
        }
        public void Die(){State=Mode.Dead;MoveDirection=Vector2.zero;Remaining=0;}
    }
}
