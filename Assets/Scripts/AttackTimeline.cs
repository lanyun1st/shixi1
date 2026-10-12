using UnityEngine;

namespace Portfolio.Configuration
{
    [CreateAssetMenu(menuName="Portfolio/Attack Timeline")]
    public sealed class AttackTimeline : ScriptableObject
    {
        [Header("Ranged / seconds")]
        public float rangedCooldown=.22f,rangedWindow=.24f,aimEnd=.035f,fireEnd=.085f,recoverEnd=.18f;
        [Header("Melee / seconds and normalized phase")]
        public float meleeDuration=.48f,fastMeleeDuration=.38f,meleeCooldown=.5f,fastMeleeCooldown=.4f;
        [Range(.01f,.99f)] public float hitPhase=.375f;
        [Range(.01f,.99f)] public float recoveryPhase=.5625f;
        [Header("Visual-only motion")]
        public float stepDistance=34,bobHeight=2.8f,blendSpeed=10,recoilDistance=2.5f,windupPull=2,followThrough=6;
        public string RangedPose(float age)=>age<aimEnd?"aim":age<fireEnd?"fire":age<recoverEnd?"recover":"aim";
        public int MeleeFrame(float progress)=>progress<hitPhase?0:progress<recoveryPhase?1:2;
        public bool IsValid=>!float.IsNaN(Sum) && !float.IsInfinity(Sum) && rangedCooldown>0 && aimEnd>0 && fireEnd>aimEnd && recoverEnd>fireEnd && rangedWindow>=recoverEnd &&
            meleeDuration>0 && fastMeleeDuration>0 && meleeCooldown>=meleeDuration && fastMeleeCooldown>=fastMeleeDuration &&
            hitPhase>0 && recoveryPhase>hitPhase && recoveryPhase<1 && stepDistance>0 && blendSpeed>0 &&
            bobHeight>=0 && recoilDistance>=0 && windupPull>=0 && followThrough>=0;
        float Sum=>rangedCooldown+rangedWindow+aimEnd+fireEnd+recoverEnd+meleeDuration+fastMeleeDuration+meleeCooldown+fastMeleeCooldown+hitPhase+recoveryPhase+stepDistance+bobHeight+blendSpeed+recoilDistance+windupPull+followThrough;
    }
}
