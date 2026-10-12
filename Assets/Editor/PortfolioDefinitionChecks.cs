using System;
using System.Reflection;
using Portfolio.Configuration;
using Portfolio.Domain;
using Portfolio.Presentation;
using Portfolio.UI;
using UnityEngine;

public static class PortfolioDefinitionChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
    static void Set(object o,string n,object value)=>o.GetType().GetField(n,F).SetValue(o,value);
    static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,F).Invoke(o,args);
    static int checks;
    static void Check(bool ok,string why){checks++;if(!ok)throw new Exception("Definitions: "+why);}
    sealed class Values : ISkillParameters
    {
        public float Value(int id,SkillParameter p,float fallback)=>p==SkillParameter.ProjectileCount?5:p==SkillParameter.ProjectileDamage?12:p==SkillParameter.Penetration?4:p==SkillParameter.DamageMultiplier?1.5f:fallback;
    }
    public static void Run(RuinsGame game)
    {
        checks=0;var catalog=SkillCatalog.Instance;
        for(int role=0;role<2;role++)for(int id=0;id<9;id++)
        {
            var d=catalog.Get(id,role==1);Check(d.IsValid,"valid role/slot "+role+"/"+id);
            Check(UiText.UpgradeName(id,role==1)==d.displayName && UiText.UpgradeIcon(id,role==1)==d.icon,"UI shares definition");
            Check(UiText.Details(id,role==1)==d.Description && !d.Description.Contains("{"),"resolved description");
            for(int mask=0;mask<512;mask++)Check(d.Available(mask)==(role==0 || SynergyRules.ShieldAvailable(id,mask)),"default prerequisites preserved");
        }
        Check(catalog.Get(-1)==null && catalog.Get(9,true)==null,"invalid slot safe");
        var loadout=new SkillLoadout();loadout.Configure(catalog);loadout.Add(4);loadout.Add(2);loadout.Add(6);
        Check(loadout.ProjectileCount==3 && loadout.Penetration==3 && Mathf.Abs(loadout.ProjectileDamage-20.4f)<.001f,"default balance preserved");
        loadout.Configure(new Values());Check(loadout.ProjectileCount==5 && loadout.Penetration==4 && loadout.ProjectileDamage==18,"injected values reach rules without Unity");
        var copy=UnityEngine.Object.Instantiate(catalog.Get(4));
        try
        {
            copy.values=new[]{new SkillValue(SkillParameter.ProjectileCount,5),new SkillValue(SkillParameter.ProjectileDamage,12)};
            Check(copy.Description.Contains("5") && copy.Description.Contains("12"),"description follows configured numbers");
            copy.requiredMask=1<<copy.id;Check(!copy.IsValid,"self prerequisite rejected");copy.requiredMask=0;
            copy.values=new[]{new SkillValue(SkillParameter.ProjectileCount,float.NaN)};Check(!copy.IsValid,"nonfinite parameter rejected");
        }
        finally{UnityEngine.Object.DestroyImmediate(copy);}
        var original=(AttackTimeline)Get(game,"attackTimeline");var profile=UnityEngine.Object.Instantiate(original);
        try
        {
            Check(profile.IsValid,"default timeline valid");profile.stepDistance=68;
            var motion=new HeroMotionPresenter();motion.Tick(17,.1f,false,profile);
            Check(Mathf.Abs(motion.StridePhase-Mathf.PI/4)<.001f,"presenter reads configured stride");
            motion.Reset();Check(motion.Gait(profile)==Vector2.zero,"presenter resets independent state");
            profile.hitPhase=.25f;profile.recoveryPhase=.6f;
            Check(profile.MeleeFrame(.2f)==0 && profile.MeleeFrame(.3f)==1 && profile.MeleeFrame(.7f)==2,"configured phases select poses");
            profile.meleeDuration=.64f;profile.meleeCooldown=.7f;
            Set(game,"shieldHero",true);Call(game,"StartRun");Call(game,"ClearActors");Set(game,"attackTimeline",profile);
            var player=(Vector2)Get(game,"player");Call(game,"BeginMelee",player+Vector2.right*100);
            Check(Mathf.Approximately((float)Get(game,"meleeDuration"),.64f) && Mathf.Approximately((float)Get(game,"cooldown"),.7f),"attack execution consumes configured duration");
            Call(game,"TickShield",.1f,player+Vector2.right*100);Check(!(bool)Get(game,"meleeResolved"),"damage waits for configured hit phase");
            Call(game,"TickShield",.08f,player+Vector2.right*100);Check((bool)Get(game,"meleeResolved"),"damage fires at configured hit phase");
            profile.hitPhase=.8f;Check(!profile.IsValid,"invalid phase order rejected");
        }
        finally{Set(game,"attackTimeline",original);UnityEngine.Object.DestroyImmediate(profile);Set(game,"shieldHero",false);Call(game,"StartRun");}
        Debug.Log("PORTFOLIO_DEFINITIONS_OK: "+checks+" checks; assets, prerequisites, injected parameters, independent presentation, timeline execution.");
    }
}
