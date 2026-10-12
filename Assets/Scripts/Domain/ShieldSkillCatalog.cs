namespace Portfolio.Domain
{
    // IDs are storage slots only. Shield definitions and prerequisites do not use projectile rules.
    public static class ShieldSkillCatalog
    {
        public static bool Available(int id,SkillLoadout owned)
        {
            return SynergyRules.ShieldAvailable(id,owned.Mask);
        }
        public static string Synergy(int id)=>id==0 || id==3?"盾反流：坚守蓄势 ＋ 盾震反攻":id==1 || id==2?"重锤流：三连重锤 ＋ 碎甲锤头":id==5 || id==4?"冲阵流：盾冲破阵 ＋ 破绽追击":"通用强化：可搭配任意盾卫流派";
    }
}
