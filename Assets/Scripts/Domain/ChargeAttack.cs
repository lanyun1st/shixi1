using System;
namespace Portfolio.Domain
{
    // Explicit input commands and gameplay time; pause/hit-stop never advance this model.
    public sealed class ChargeAttack
    {
        public const float Windup=.65f,Recovery=2.8f;
        public bool Charging { get; private set; }
        public float Held { get; private set; }
        public float Cooldown { get; private set; }
        public float Fraction=>Math.Min(1,Held/Windup);
        public bool Begin(){if(Charging||Cooldown>0)return false;Charging=true;Held=0;return true;}
        public void Tick(float dt)
        {
            if(float.IsNaN(dt)||float.IsInfinity(dt)||dt<0)throw new ArgumentOutOfRangeException(nameof(dt));
            Cooldown=Math.Max(0,Cooldown-dt);if(Charging)Held=Math.Min(Windup,Held+dt);
        }
        public bool Release(){bool ready=Charging&&Held>=Windup;Cancel();if(ready)Cooldown=Recovery;return ready;}
        public void Cancel(){Charging=false;Held=0;}
        public void Reset(){Cancel();Cooldown=0;}
        public static float Vulnerability(bool exposed)=>exposed?1.75f:1;
    }
}
