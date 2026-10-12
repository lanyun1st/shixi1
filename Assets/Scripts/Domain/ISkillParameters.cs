namespace Portfolio.Domain
{
    public enum SkillParameter { ProjectileCount, ProjectileDamage, Penetration, DamageMultiplier, MovementMultiplier, DashCooldownMultiplier }
    // Rules accept values, never load assets or know about Unity's Resource system.
    public interface ISkillParameters
    {
        float Value(int id, SkillParameter parameter, float fallback);
    }
}
