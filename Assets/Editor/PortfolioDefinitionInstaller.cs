using System.IO;
using Portfolio.Configuration;
using Portfolio.Domain;
using UnityEditor;
using UnityEngine;

// One-time defaults only. Existing designer-edited assets are never overwritten.
[InitializeOnLoad]
public static class PortfolioDefinitionInstaller
{
    static PortfolioDefinitionInstaller(){EditorApplication.delayCall+=Ensure;}
    [MenuItem("Portfolio/Configuration/Create Missing Definitions")]
    public static void Ensure()
    {
        Directory.CreateDirectory("Assets/Resources/Skills");
        bool changed=false;
        for(int i=0;i<18;i++)
        {
            string path="Assets/Resources/Skills/"+Keys[i]+".asset";
            if(AssetDatabase.LoadAssetAtPath<SkillDefinition>(path)!=null)continue;
            int id=i%9;bool shield=i>=9;
            var d=ScriptableObject.CreateInstance<SkillDefinition>();d.id=id;d.shield=shield;
            d.displayName=shield?ShieldNames[id]:RangedNames[id];d.description=shield?ShieldDetails[id]:RangedDetails[id];
            d.icon=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/UI/SkillIcons/"+Keys[i]+".png");
            if(d.icon==null){Object.DestroyImmediate(d);Debug.LogError("Missing skill icon: "+Keys[i]);continue;}
            if(shield)for(int j=0;j<SynergyRules.Count;j++){var r=SynergyRules.Get((SynergyId)j);if(r.Shield && r.Second==id)d.requiredMask|=1<<r.First;}
            if(!shield)
            {
                if(id==2){d.values=new[]{new SkillValue(SkillParameter.Penetration,3)};d.description=d.description.Replace("3","{Penetration}");}
                if(id==4){d.values=new[]{new SkillValue(SkillParameter.ProjectileCount,3),new SkillValue(SkillParameter.ProjectileDamage,17)};d.description=d.description.Replace("3","{ProjectileCount}").Replace("17","{ProjectileDamage}");}
                if(id==6){d.values=new[]{new SkillValue(SkillParameter.DamageMultiplier,1.2f)};d.description=d.description.Replace("20","{DamageMultiplierPercent}");}
                if(id==7){d.values=new[]{new SkillValue(SkillParameter.MovementMultiplier,1.15f)};d.description=d.description.Replace("15","{MovementMultiplierPercent}");}
                if(id==8){d.values=new[]{new SkillValue(SkillParameter.DashCooldownMultiplier,.75f)};d.description=d.description.Replace("25","{DashCooldownMultiplierPercent}");}
            }
            AssetDatabase.CreateAsset(d,path);changed=true;
        }
        const string timeline="Assets/Resources/AttackTimeline.asset";
        if(AssetDatabase.LoadAssetAtPath<AttackTimeline>(timeline)==null){AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<AttackTimeline>(),timeline);changed=true;}
        if(changed)AssetDatabase.SaveAssets();
    }
    static readonly string[] RangedNames={"灼烧核心","霜痕回路","穿透棱镜","连锁节点","多重弹","冲刺震波","能量增幅","轻盈步伐","迅捷回路"};
    static readonly string[] RangedDetails={
            "命中附加灼烧，持续 3 秒\n每 0.5 秒造成 7 点伤害\n重复命中刷新持续时间",
            "命中减速，持续 2.2 秒\n软泥怪移速降低 55%\n精英移速降低 35%",
            "一发弹丸最多命中 3 个敌人\n每个目标继承当前弹丸伤害\n无法穿过柱子、箱子或木桶",
            "命中连接 270 范围内另一敌人\n额外造成 16 点伤害\n传递灼烧/冰霜，不递归弹射",
            "每次发射 3 枚扇形弹丸\n每枚基础伤害调整为 17\n每枚均继承穿透与元素",
            "冲刺起点释放半径 125 震波\n造成 24 点伤害\n继承元素和连锁效果",
            "普通弹丸伤害提高 20%\n同时作用于多重弹的每枚弹丸",
            "普通移动速度提高 15%\n蓄力移动同样受益",
            "冲刺冷却缩短 25%\n提高震波与回响触发频率"};
    static readonly string[] ShieldNames={"坚守蓄势","三连重锤","碎甲锤头","盾震反攻","破绽追击","盾冲破阵","熟练挥击","长柄重锤","迅捷盾冲"};
    static readonly string[] ShieldDetails={
            "正面格挡蓄势 +1，精准格挡 +2\n上限3层，6秒未获得则消退\n下一次挥击每层增伤15%，空挥也消耗",
            "连续挥击的第三击成为重砸\n伤害180%，范围增大，扇形180°\n停手1.25秒重置，不要求同一目标",
            "重砸使幸存目标破甲4秒\n其受到的挥锤伤害提高25%\n第三关重砸追加半径180震地 / 24伤害",
            "蓄势挥击命中后追加一次前方盾震\n造成18伤害并推开普通敌人\n第三关满层反击回复3血，消耗恢复额度",
            "挥锤命中盾冲破绽目标增伤50%\n消耗该目标的破绽\n第三关盾冲撞墙追加32伤害和短暂硬直",
            "盾冲推力从70提升至110\n撞击目标获得2秒破绽\n精英可被标记，但不能被推走",
            "挥锤冷却从0.5秒缩短至0.4秒\n同步缩短挥击前摇和动作时间",
            "基础挥锤距离从110提升至145\n重砸在此基础上额外增加25\n所有挥击仍受实体障碍阻挡",
            "盾冲冷却从2秒缩短至1.5秒\n不改变精准格挡冷却"};
    static readonly string[] Keys={
            "ranged_burn","ranged_frost","ranged_pierce","ranged_chain","ranged_multishot",
            "ranged_nova","ranged_power","ranged_speed","ranged_haste",
            "shield_guard","shield_combo","shield_shatter","shield_counter","shield_exploit",
            "shield_charge","shield_swift","shield_reach","shield_haste"};
}

