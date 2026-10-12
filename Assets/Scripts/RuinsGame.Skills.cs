using System.Collections.Generic;
using UnityEngine;
using Portfolio.Domain;
using Portfolio.UI;
using Portfolio.Presentation;

public sealed partial class RuinsGame
{
    readonly SkillLoadout loadout=new SkillLoadout();
    readonly List<LineEffect> skillLines=new List<LineEffect>();
    Material skillMaterial;
    float echoDelay;
    Vector2 echoPosition;
    int cachedBuildMask=-1,cachedRouteMask=-1,cachedRouteRoom=-1,cachedUpgrade=-2;
    bool cachedBuildShield,cachedRouteShield;
    string cachedBuildName,cachedRouteName;
    string RouteName()
    {
        if(cachedRouteMask==loadout.Mask && cachedRouteRoom==room && cachedUpgrade==upgrade && cachedRouteShield==shieldHero)return cachedRouteName;
        cachedRouteMask=loadout.Mask;cachedRouteRoom=room;cachedUpgrade=upgrade;cachedRouteShield=shieldHero;
        return cachedRouteName=ResolveRouteName();
    }
    string ResolveRouteName()
    {
        if(shieldHero)
        {
            string suffix=room==3?" · 进阶":"";
            if(loadout.HasRoute(SynergyId.HeavySlam))return SynergyRules.Get(SynergyId.HeavySlam).Name+suffix;
            if(loadout.HasRoute(SynergyId.ShieldCounter))return SynergyRules.Get(SynergyId.ShieldCounter).Name+suffix;
            if(loadout.HasRoute(SynergyId.ShieldBreach))return SynergyRules.Get(SynergyId.ShieldBreach).Name+suffix;
            return UiText.UpgradeName(upgrade,true);
        }
        if(loadout.HasRoute(SynergyId.Overload))return SynergyRules.Get(SynergyId.Overload).Name+(room==3?" · 进阶":"");
        if(loadout.HasRoute(SynergyId.IceShatter))return SynergyRules.Get(SynergyId.IceShatter).Name+(room==3?" · 进阶":"");
        if(loadout.HasRoute(SynergyId.DashEcho))return SynergyRules.Get(SynergyId.DashEcho).Name+(room==3?" · 进阶":"");
        return UiText.UpgradeName(upgrade,shieldHero);
    }
    void TickEcho(float dt)
    {
        if(echoDelay<=0)return;
        echoDelay-=dt;if(echoDelay>0)return;
        var center=room==3?player:echoPosition;
        SkillCue(ReactionKind.Echo,center,150);
        foreach(var target in enemies)if(target.hp>0 && target.spawnDelay<=0 && Vector2.Distance(target.p,center)<=150 && ClearSkillPath(center,target.p))Damage(target,room==3?36:24,true);
    }
    void TriggerRoutes(Enemy source,bool burning,bool chilled)
    {
        // Secondary damage never re-enters this method: no recursive explosion loops.
        bool overload=SynergyRules.CanTriggerHit(SynergyId.Overload,loadout.Mask,true,burning,chilled,source.reaction);
        bool shatter=SynergyRules.CanTriggerHit(SynergyId.IceShatter,loadout.Mask,true,burning,chilled,source.reaction);
        if(!overload && !shatter)return;
        source.reaction=SynergyRules.TargetReactionCooldown;
        float radius=room==3?150:110,damage=room==3?30:20;
        if(shatter)SkillCue(ReactionKind.IceShatter,source.p,radius);
        foreach(var target in enemies)
        {
            if(target==source || target.hp<=0 || target.spawnDelay>0 || Vector2.Distance(source.p,target.p)>radius || !ClearSkillPath(source.p,target.p))continue;
            Damage(target,damage,false);
            if(shatter && target.elite==null && target.hp>0)target.frozen=.6f;
        }
        if(overload)ReactionPresented?.Invoke(new Portfolio.Presentation.ReactionCue(Portfolio.Presentation.ReactionKind.Overload,source.p,source.p,radius));
    }
    void PrepareChoices()
    {
        var pool=new List<int>();for(int i=0;i<SkillLoadout.Count;i++)if(!loadout.Has(i) && Portfolio.Configuration.SkillCatalog.Instance.Get(i,shieldHero).Available(loadout.Mask))pool.Add(i);
        for(int i=0;i<choices.Length;i++){int pick=Random.Range(0,pool.Count);choices[i]=pool[pick];pool.RemoveAt(pick);}
    }
    void ApplyElements(Enemy e)
    {
        if(loadout.Has(0)){if(e.burn<=0)e.tick=.5f;e.burn=3;}
            if(loadout.Has(1))e.slow=2.2f;
        if(SynergyRules.CanTriggerHit(SynergyId.ThermalShock,loadout.Mask,true,false,false,e.reaction))
        {
            e.reaction=SynergyRules.TargetReactionCooldown;
            SkillCue(ReactionKind.Thermal,e.p,100);
            foreach(var target in enemies)if(target.hp>0 && Vector2.Distance(target.p,e.p)<=100 && ClearSkillPath(e.p,target.p))Damage(target,20,false);
        }
    }
    void DashNova()
    {
        SkillCue(ReactionKind.Nova,player,125);
        foreach(var target in enemies)if(target.hp>0 && Vector2.Distance(target.p,player)<=125 && ClearSkillPath(player,target.p))Damage(target,24,true);
    }
    void DrawRing(Vector2 at,float radius,Color color)
    {
        var line=SpawnLine(33,color);
        for(int i=0;i<33;i++){float a=i*Mathf.PI*2/32;var p=at+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius;line.SetPosition(i,new Vector3(p.x,740-p.y,0));}
    }
    void SkillLink(Vector2 from,Vector2 to,Color color)
    {
        var line=SpawnLine(2,color);line.SetPosition(0,new Vector3(from.x,740-from.y,0));line.SetPosition(1,new Vector3(to.x,740-to.y,0));
    }
    string BuildName()
    {
        if(cachedBuildMask==loadout.Mask && cachedBuildShield==shieldHero)return cachedBuildName;
        cachedBuildMask=loadout.Mask;cachedBuildShield=shieldHero;
        var names=new List<string>();for(int i=0;i<SkillLoadout.Count;i++)if(loadout.Has(i))names.Add(UiText.UpgradeName(i,shieldHero));
        return cachedBuildName=names.Count==0?"暂无强化":string.Join(" + ",names);
    }
    bool ClearSkillPath(Vector2 a,Vector2 b)
    {using(VisibilityMarker.Auto())return ClearSkillPathCore(a,b);}
    static readonly Unity.Profiling.ProfilerMarker VisibilityMarker=new Unity.Profiling.ProfilerMarker("Portfolio.SkillVisibility");
    bool ClearSkillPathCore(Vector2 a,Vector2 b)
    {
        int steps=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(a,b)/6));
        foreach(var o in obstacles)
        {
            if(o.hp==0 || !ArenaLayout.SweepMayOverlap(a,b,2,o.footprint))continue;
            for(int i=0;i<=steps;i++)if(ArenaLayout.CircleOverlaps(Vector2.Lerp(a,b,i/(float)steps),2,o.footprint))return false;
        }
        return true;
    }
}
