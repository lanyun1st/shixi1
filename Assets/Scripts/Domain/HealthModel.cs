using System;

namespace Portfolio.Domain
{
    public interface IDamageable { void TakeDamage(float amount); }

    // Rules only: no scene, UI, sound, or Unity clock dependencies.
    public sealed class HealthModel : IDamageable
    {
        public float Maximum { get; private set; }
        public float Current { get; private set; }
        public bool IsDead => Current <= 0;
        public event Action<float, float> Changed;
        public event Action Died;
        public HealthModel(float maximum)
        {
            if(float.IsNaN(maximum)||float.IsInfinity(maximum)||maximum<=0)throw new ArgumentOutOfRangeException(nameof(maximum));
            Maximum=maximum;Current=maximum;
        }
        public void TakeDamage(float amount)
        {
            Validate(amount);if(IsDead||amount==0)return;
            Current=Math.Max(0,Current-amount);Changed?.Invoke(Current,Maximum);
            if(IsDead)Died?.Invoke();
        }
        public void Heal(float amount)
        {
            Validate(amount);if(IsDead||amount==0)return;
            SetAliveValue(Math.Min(Maximum,Current+amount));
        }
        public void Reset() { Current=Maximum;Changed?.Invoke(Current,Maximum); }
        public void Reset(float maximum)
        {
            Validate(maximum);if(maximum<=0)throw new ArgumentOutOfRangeException(nameof(maximum));
            Maximum=maximum;Reset();
        }
        void SetAliveValue(float value) { if(value==Current)return;Current=value;Changed?.Invoke(Current,Maximum); }
        static void Validate(float amount)
        {
            if(float.IsNaN(amount)||float.IsInfinity(amount)||amount<0)throw new ArgumentOutOfRangeException(nameof(amount));
        }
    }
    public sealed class CooldownGate
    {
        public float Remaining { get; private set; }
        public bool Ready => Remaining<=0;
        public bool TryStart(float seconds)
        {
            if(float.IsNaN(seconds)||float.IsInfinity(seconds)||seconds<0)throw new ArgumentOutOfRangeException(nameof(seconds));
            if(!Ready)return false;Remaining=seconds;return true;
        }
        public void Tick(float delta)
        {
            if(float.IsNaN(delta)||float.IsInfinity(delta)||delta<0)throw new ArgumentOutOfRangeException(nameof(delta));
            Remaining=Math.Max(0,Remaining-delta);
        }
        public void Reset() { Remaining=0; }
    }
}
