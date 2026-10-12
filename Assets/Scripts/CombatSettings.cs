using UnityEngine;

[CreateAssetMenu(menuName="Portfolio/Combat Settings")]
public sealed class CombatSettings : ScriptableObject
{
    [Min(.01f)] public float dashCooldown=1.1f;
    [Min(.01f)] public float dashDuration=.16f;
    [Min(0)] public float dashInvulnerability=.2f;
    [Min(1)] public float dashSpeed=780;
    [Min(1)] public float moveSpeed=260;
}
