namespace Portfolio.Domain
{
    public enum SynergyId { Overload, IceShatter, DashEcho, ThermalShock, ShieldCounter, HeavySlam, ShieldBreach }
    // Immutable definitions, separate from per-run ownership and per-target cooldowns.
    public sealed class SynergyRule
    {
        public readonly SynergyId Id;public readonly string Key,Name,Hint;
        public readonly bool Shield;public readonly int First,Second,RequiredMask;
        public SynergyRule(SynergyId id,string key,string name,bool shield,int first,int second,string hint)
        {Id=id;Key=key;Name=name;Shield=shield;First=first;Second=second;RequiredMask=(1<<first)|(1<<second);Hint=hint;}
        public bool Active(int mask)=>(mask&RequiredMask)==RequiredMask;
        public bool Contains(int slot)=>slot==First || slot==Second;
    }
    public static class SynergyRules
    {
        public const float TargetReactionCooldown=1;
        static readonly SynergyRule[] rules={
            new SynergyRule(SynergyId.Overload,"ranged.overload","雷火过载",false,0,3,"雷火过载：再次命中燃烧目标爆破\n第三关半径150 / 30伤害"),
            new SynergyRule(SynergyId.IceShatter,"ranged.ice_shatter","冰裂穿透",false,1,2,"冰裂穿透：再次命中减速目标冰裂\n伤害周围敌人并冻结普通怪"),
            new SynergyRule(SynergyId.DashEcho,"ranged.dash_echo","冲刺回响",false,5,4,"冲刺回响：冲刺后延迟第二次震波\n第三关在落点释放 / 36伤害"),
            new SynergyRule(SynergyId.ThermalShock,"ranged.thermal","热冲击",false,0,1,"组合：热冲击 · 半径100 / 20伤害\n同一目标触发间隔 1 秒"),
            new SynergyRule(SynergyId.ShieldCounter,"shield.counter","坚守反攻",true,0,3,"盾反流：坚守蓄势 ＋ 盾震反攻"),
            new SynergyRule(SynergyId.HeavySlam,"shield.heavy_slam","震地重锤",true,1,2,"重锤流：三连重锤 ＋ 碎甲锤头"),
            new SynergyRule(SynergyId.ShieldBreach,"shield.breach","盾冲破阵",true,5,4,"冲阵流：盾冲破阵 ＋ 破绽追击")};
        public static int Count=>rules.Length;
        public static SynergyRule Get(SynergyId id)=>rules[(int)id];
        public static bool Active(SynergyId id,int mask)=>Get(id).Active(mask);
        public static SynergyRule CompletedBy(int slot,int owned,bool shield)
        {
            if(slot<0 || slot>=SkillLoadout.Count)return null;
            int combined=owned|(1<<slot);
            foreach(var rule in rules)if(rule.Shield==shield && rule.Contains(slot) && !rule.Active(owned) && rule.Active(combined))return rule;
            return null;
        }
        public static bool CanTriggerHit(SynergyId id,int mask,bool allowReactions,bool burning,bool chilled,float cooldown)
        {
            if(!allowReactions || cooldown>0 || !Active(id,mask))return false;
            return id==SynergyId.Overload?burning:id==SynergyId.IceShatter?chilled:id==SynergyId.ThermalShock;
        }
        // The shield's upgrade components cannot be offered before their core.
        public static bool ShieldAvailable(int slot,int mask)
        {
            foreach(var rule in rules)if(rule.Shield && rule.Second==slot)return (mask&(1<<rule.First))!=0;
            return slot>=0 && slot<SkillLoadout.Count;
        }
    }
}
