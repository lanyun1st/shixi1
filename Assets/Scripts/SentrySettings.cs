using Portfolio.Domain;
using UnityEngine;

[CreateAssetMenu(menuName="Portfolio/Sentry Settings")]
public sealed class SentrySettings : ScriptableObject
{
    [Min(1)] public float health=60;
    [Min(1)] public float moveSpeed=85;
    [Min(.1f)] public float windup=.9f;
    [Min(.01f)] public float firePose=.12f;
    [Min(.1f)] public float recovery=1.6f;
    [Min(1)] public float attackRange=530;
    [Min(1)] public float keepDistance=220;
    [Min(.1f)] public float retreatDuration=.45f;
    [Min(1)] public float projectileSpeed=240;
    [Min(1)] public float projectileDamage=12;
    [Min(.1f)] public float projectileLifetime=3.2f;
    [Range(2,16)] public float projectileRadius=10;
    public SentryRules CreateRules()=>new SentryRules(windup,firePose,recovery,attackRange,keepDistance,retreatDuration);
    public void Validate()
    {
        CreateRules();
        foreach(float value in new[]{health,moveSpeed,projectileSpeed,projectileDamage,projectileLifetime,projectileRadius})
            if(value<=0 || float.IsNaN(value) || float.IsInfinity(value))throw new System.ArgumentException("Invalid sentry configuration");
    }
}
