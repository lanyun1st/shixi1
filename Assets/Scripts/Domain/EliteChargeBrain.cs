using System;
using UnityEngine;

namespace Portfolio.Domain
{
    // No scene lookup, renderer, player reference or input: the caller supplies observations.
    public sealed class EliteChargeBrain
    {
        public enum Mode { Chase, Windup, Charge, Recover, Stunned, Dead }
        public Mode State { get; private set; }=Mode.Chase;
        public Vector2 LockedDirection { get; private set; }=Vector2.down;
        public float Remaining { get; private set; }=1.4f;
        public const float WindupSeconds=.9f, ChargeSeconds=.7f, ChargeSpeed=520;
        public void Tick(float dt,Vector2 directionToTarget)
        {
            if(float.IsNaN(dt)||float.IsInfinity(dt)||dt<0)throw new ArgumentOutOfRangeException(nameof(dt));
            if(State==Mode.Dead || dt==0)return;
            Remaining=Mathf.Max(0,Remaining-dt);
            if(Remaining>0)return;
            switch(State)
            {
                case Mode.Chase:
                    LockedDirection=directionToTarget.sqrMagnitude>.001f?directionToTarget.normalized:Vector2.down;
                    Enter(Mode.Windup,WindupSeconds);break;
                case Mode.Windup:Enter(Mode.Charge,ChargeSeconds);break;
                case Mode.Charge:Enter(Mode.Recover,.7f);break;
                case Mode.Recover:case Mode.Stunned:Enter(Mode.Chase,1.4f);break;
            }
        }
        public void HitObstacle() { if(State==Mode.Charge)Enter(Mode.Stunned,1.5f); }
        public void Die() { Enter(Mode.Dead,0); }
        void Enter(Mode state,float time) { State=state;Remaining=time; }
    }
}
