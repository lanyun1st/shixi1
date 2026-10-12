using System;
namespace Portfolio.Domain
{
    // Pure run state: independent of input, presentation and enemy ownership.
    public sealed class SkillLoadout
    {
        public const int Count=9;
        ISkillParameters parameters;
        public void Configure(ISkillParameters source){parameters=source;}
        float Value(int id,SkillParameter parameter,float fallback)=>parameters?.Value(id,parameter,fallback)??fallback;
        public int Mask { get; private set; }
        public bool Has(int id)=>id>=0 && id<Count && (Mask & (1<<id))!=0;
        public bool Add(int id)
        {
            if(id<0 || id>=Count || Has(id))return false;
            Mask|=1<<id;return true;
        }
        public void Reset()=>Mask=0;
        public int ProjectileCount=>Has(4)?(int)Value(4,SkillParameter.ProjectileCount,3):1;
        public int Penetration=>Has(2)?(int)Value(2,SkillParameter.Penetration,3):1;
        public float ProjectileDamage=>(Has(4)?Value(4,SkillParameter.ProjectileDamage,17):24)*(Has(6)?Value(6,SkillParameter.DamageMultiplier,1.2f):1);
        public float MovementMultiplier=>Has(7)?Value(7,SkillParameter.MovementMultiplier,1.15f):1;
        public float DashCooldownMultiplier=>Has(8)?Value(8,SkillParameter.DashCooldownMultiplier,.75f):1;
        public bool HasRoute(SynergyId id)=>SynergyRules.Active(id,Mask);
        public bool ThermalShock=>HasRoute(SynergyId.ThermalShock);
    }
}
