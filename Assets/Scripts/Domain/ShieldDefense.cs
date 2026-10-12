using System;
namespace Portfolio.Domain
{
    // Rules only. Caller supplies attack-relative facing dot and simulation time.
    public sealed class ShieldDefense
    {
        public const float MaxHealth=150, GuardReduction=.75f, PerfectWindow=.2f;
        public bool Guarding { get; private set; }
        public float GuardAge { get; private set; }
        public float ParryCooldown { get; private set; }
        public float CounterRemaining { get; private set; }
        public float RecoveryBudget { get; private set; }=30;
        public float QuietTime { get; private set; }
        bool perfectEligible;
        public void SetGuard(bool held,bool allowPerfect=true)
        {
            if(held && !Guarding)
            {
                GuardAge=0;perfectEligible=allowPerfect && ParryCooldown<=0;
                if(allowPerfect)ParryCooldown=Math.Max(ParryCooldown,.55f);
            }
            Guarding=held;if(!held)perfectEligible=false;
        }
        public float Resolve(float amount,float facingDot,bool unblockable,out bool perfect)
        {
            Validate(amount);perfect=false;
            if(amount==0)return 0;
            if(!unblockable && Guarding && facingDot>=.5f)
            {
                perfect=perfectEligible && GuardAge<PerfectWindow;
                if(perfect){perfectEligible=false;CounterRemaining=3;return 0;}
                amount*=1-GuardReduction;
            }
            QuietTime=0;return amount;
        }
        public bool ConsumeCounter(){bool result=CounterRemaining>0;CounterRemaining=0;return result;}
        public void Tick(float dt,HealthModel health,bool active)
        {
            Validate(dt);if(!active || health.IsDead)return;
            GuardAge+=dt;ParryCooldown=Math.Max(0,ParryCooldown-dt);CounterRemaining=Math.Max(0,CounterRemaining-dt);
            float before=QuietTime;QuietTime+=dt;
            float healingTime=Math.Max(0,QuietTime-4)-Math.Max(0,before-4);
            float amount=Math.Min(RecoveryBudget,Math.Min(healingTime*2,Math.Max(0,health.Maximum*.6f-health.Current)));
            if(amount>0){health.Heal(amount);RecoveryBudget-=amount;}
        }
        public void Reset(){Guarding=perfectEligible=false;GuardAge=ParryCooldown=CounterRemaining=QuietTime=0;RecoveryBudget=30;}
        public float RecoverCounter(HealthModel health,float requested)
        {
            Validate(requested);if(health.IsDead)return 0;
            float amount=Math.Min(RecoveryBudget,Math.Min(requested,health.Maximum-health.Current));
            if(amount>0){health.Heal(amount);RecoveryBudget-=amount;}return amount;
        }
        static void Validate(float n){if(float.IsNaN(n)||float.IsInfinity(n)||n<0)throw new ArgumentOutOfRangeException(nameof(n));}
    }
}
