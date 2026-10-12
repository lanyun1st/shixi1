using System;
using System.Collections;
using System.Reflection;
using Portfolio.Domain;
using Portfolio.UI;
using UnityEngine;

public static class PortfolioOptimizationChecks
{
    const BindingFlags F=BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance;
    static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
    static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,F).Invoke(o,args);
    static int checks;
    static void Check(bool ok,string why){checks++;if(!ok)throw new Exception("Optimization: "+why);}
    public static void Run(RuinsGame game)
    {
        checks=0;
        for(int mask=0;mask<512;mask++)for(int index=0;index<SynergyRules.Count;index++)
        {
            var id=(SynergyId)index;var rule=SynergyRules.Get(id);bool active=(mask&(1<<rule.First))!=0 && (mask&(1<<rule.Second))!=0;
            Check(rule.Active(mask)==active,"ownership "+rule.Key);
            Check(!SynergyRules.CanTriggerHit(id,mask,false,true,true,0),"secondary damage cannot trigger "+rule.Key);
            Check(!SynergyRules.CanTriggerHit(id,mask,true,true,true,.1f),"cooldown "+rule.Key);
            bool hitRule=index<=1 || id==SynergyId.ThermalShock;
            Check(SynergyRules.CanTriggerHit(id,mask,true,true,true,0)==(active&&hitRule),"hit prerequisites "+rule.Key);
        }
        for(int mask=0;mask<512;mask++)for(int slot=0;slot<9;slot++)foreach(bool shield in new[]{false,true})
        {
            var rule=SynergyRules.CompletedBy(slot,mask,shield);
            if(rule==null)continue;
            Check(rule.Shield==shield && rule.Contains(slot) && !rule.Active(mask) && rule.Active(mask|(1<<slot)),"card preview matches active rule");
            Check(UiText.Synergy(slot,mask,shield)==rule.Hint,"UI hint comes from rule");
        }
        Check(!SynergyRules.CanTriggerHit(SynergyId.Overload,9,true,false,true,0),"overload requires prior burn");
        Check(!SynergyRules.CanTriggerHit(SynergyId.IceShatter,6,true,true,false,0),"shatter requires prior chill");
        Call(game,"StartRun");Set(game,"room",3);Call(game,"SpawnRoom");Call(game,"ClearActors");
        var obstacles=(IList)Get(game,"obstacles");var boxes=new Rect[obstacles.Count];for(int i=0;i<boxes.Length;i++)boxes[i]=(Rect)Get(obstacles[i],"footprint");
        var rng=new System.Random(4701);
        for(int i=0;i<2048;i++)
        {
            var a=new Vector2(80+rng.Next(3040),220+rng.Next(1940));var b=a+new Vector2(rng.Next(-350,351),rng.Next(-350,351));
            bool old=true;int steps=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(a,b)/6));
            for(int step=0;step<=steps && old;step++)foreach(var box in boxes)if(ArenaLayout.CircleOverlaps(Vector2.Lerp(a,b,step/(float)steps),2,box)){old=false;break;}
            Check((bool)Call(game,"ClearSkillPath",a,b)==old,"sample-exact visibility equivalence "+i);
        }
        var loadout=(SkillLoadout)Get(game,"loadout");loadout.Add(0);loadout.Add(3);
        var first=game.CaptureUiState();var second=game.CaptureUiState();
        Check(object.ReferenceEquals(first.buildName,second.buildName) && object.ReferenceEquals(first.routeName,second.routeName),"stable strings reused");
        loadout.Add(1);Check(game.CaptureUiState().buildName!=first.buildName,"ownership invalidates cache");
        Set(game,"shieldHero",true);var shieldState=game.CaptureUiState();Check(shieldState.buildName.Contains("坚守蓄势"),"character invalidates cache");
        Set(game,"momentum",2);Check(game.CaptureUiState().abilityHint.Contains("2/3"),"shield counter invalidates hint");
        Set(game,"shieldHero",false);Call(game,"StartRun");Check(game.CaptureUiState().buildName=="暂无强化","restart invalidates cache");
        Call(game,"ClearActors");
        var pool=Get(game,"impactPool");var poolType=pool.GetType();
        int created=(int)poolType.GetProperty("Created").GetValue(pool);
        Call(game,"SpawnSlime",new Vector2(1600,1100),10000f,false,0f);
        var enemy=((IList)Get(game,"enemies"))[0];Set(enemy,"spawnDelay",0f);Set(enemy,"hp",10000f);
        for(int i=0;i<500;i++)Call(game,"Damage",enemy,1f,false);
        Check((float)Get(enemy,"hp")==9500f,"visual budget does not drop damage");
        Check(((IList)Get(game,"impacts")).Count==96,"impact active budget");
        Check((int)poolType.GetProperty("Created").GetValue(pool)==created,"burst reuses prewarmed impacts");
        Call(game,"ClearActors");
        Check((int)poolType.GetProperty("Active").GetValue(pool)==0,"impact leases returned on clear");
        var ui=(PortfolioCanvasUI)Get(game,"canvasUI");var chargeState=game.CaptureUiState();
        chargeState.shieldHero=false;chargeState.charging=true;chargeState.chargeFraction=.999f;
        ui.Render(chargeState,new[]{0,1,5});chargeState.chargeFraction=1;
        ui.Render(chargeState,new[]{0,1,5});
        Check(ui.chargeStatus.text=="蓄力完成 · 松开右键释放","full charge invalidates rounded percentage cache");
        Debug.Log("PORTFOLIO_OPTIMIZATION_OK: "+checks+" checks; all masks, reaction suppression, UI agreement, 2048 old/new path comparisons, cache invalidation.");
    }
}
