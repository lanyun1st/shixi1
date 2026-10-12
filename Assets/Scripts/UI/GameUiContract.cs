using UnityEngine;

namespace Portfolio.UI
{
    public enum UiMode { Title, Playing, Upgrade, Paused, Dying, Result }
    public interface ICharacterUiCommands { void RequestCharacter(bool shield); }
    public interface IGameUiCommands
    {
        void RequestStart();
        void RequestPause(bool paused);
        void RequestUpgrade(int id);
    }
    public struct GameUiSnapshot
    {
        public UiMode mode;
        public int room,remaining,upgrade,kills,shots,hits,upgradeMask,pending,wave,waveTotal;
        public float dashRemaining,dashDuration,elapsed,eliteHealth,eliteMaximum,chargeFraction,chargeCooldown;
        public bool cleared,canInteract,won,eliteStunned,charging,shieldHero;
        public string characterName,abilityHint,regionName;
        public Vector2 playerPosition,portalPosition,mapSize,cameraPosition,cameraSize;
        public Vector2[] enemyPositions;
        public int[] enemyKinds;
        public Rect[] mapObstacles;
        public int mapObstacleCount;
        public int enemyPositionCount;
        public string eliteState,buildName,rewardTitle,rewardHint,routeName;
    }
    public static class UiText
    {
        public static string Objective(GameUiSnapshot state)
        {
            if(!state.cleared)return "波次 "+state.wave+" / "+state.waveTotal+" · 场内 "+state.remaining+(state.pending>0?"（预警 "+state.pending+"）":state.remaining==0?" · 下一波准备中":"");
            return state.canInteract ? (state.room==3?"[ E ] 离开遗迹":"[ E ] 进入传送门") : "已净化 · 前往上方传送门";
        }
        public static string UpgradeName(int id)=>UpgradeName(id,false);
        public static string UpgradeName(int id,bool shield)=>Portfolio.Configuration.SkillCatalog.Instance.Get(id,shield)?.displayName??"暂无强化";
        public static string Details(int id,bool shield)=>Portfolio.Configuration.SkillCatalog.Instance.Get(id,shield)?.Description??"";
        public static string Synergy(int id,int mask,bool shield)
        {
            var rule=Portfolio.Domain.SynergyRules.CompletedBy(id,mask,shield);if(rule!=null)return rule.Hint;
            if(!shield)return Synergy(id,mask);
            return Portfolio.Domain.ShieldSkillCatalog.Synergy(id);
        }
        public static Sprite UpgradeIcon(int id)=>SkillIconCatalog.Get(id);
        public static Sprite UpgradeIcon(int id,bool shield)=>SkillIconCatalog.Get(id,shield);
        public static string Synergy(int id,int mask)
        {
            var rule=Portfolio.Domain.SynergyRules.CompletedBy(id,mask,false);if(rule!=null)return rule.Hint;
            if(id==4 && (mask&4)!=0 || id==2 && (mask&16)!=0)return "组合：三路穿透 · 每枚最多3目标";
            if(id==3 && (mask&3)!=0 || id<2 && (mask&8)!=0)return "组合：弹射可传递元素状态";
            if(id==5 && (mask&11)!=0 || (id<2 || id==3) && (mask&32)!=0)return "组合：震波继承元素 / 弹射";
            return "保留已有强化，可与下一次选择组合";
        }
    }
}
