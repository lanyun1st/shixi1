using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Portfolio.Domain;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class PortfolioConfigInstaller
{
    static PortfolioConfigInstaller() { EditorApplication.delayCall += Ensure; }
    public static void Ensure()
    {
        const string path="Assets/Resources/CombatSettings.asset";
        if(AssetDatabase.LoadAssetAtPath<CombatSettings>(path)!=null)return;
        Directory.CreateDirectory("Assets/Resources");
        AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<CombatSettings>(),path);
        AssetDatabase.SaveAssets();
    }
}

// Small dependency-free editor test runner; not a replacement for Unity Test Framework.
public static class PortfolioRuleTests
{
    sealed class PoolItem { public int value; }
    public static string LastReport { get; private set; }="点击运行规则测试。不启动场景，不修改游戏配置。";
    static void Equal(float actual,float expected)
    {
        if(Math.Abs(actual-expected)>.0001f)throw new Exception("预期 "+expected+"，实际 "+actual);
    }
    static void Check(bool value,string message) { if(!value)throw new Exception(message); }
    static void Reject(Action action)
    {
        try { action(); } catch(ArgumentOutOfRangeException) { return; }
        throw new Exception("无效输入应被拒绝");
    }
    public static bool RunAll(bool demo)
    {
        var watch=Stopwatch.StartNew();var lines=new List<string>();int passed=0,failed=0;
        Action<string,Action> test=(name,body)=>
        {
            try { body();passed++;lines.Add("PASS  "+name); }
            catch(Exception e) { failed++;lines.Add("FAIL  "+name+"\n      "+e.Message); }
        };
        test("扣血：100 - 24 = 76",()=>{IDamageable target=new HealthModel(100);target.TakeDamage(24);Equal(((HealthModel)target).Current,76);});
        test("过量伤害：生命值不会小于零",()=>{var h=new HealthModel(100);h.TakeDamage(999);Equal(h.Current,0);});
        test("死亡事件：重复攻击只通知一次",()=>{var h=new HealthModel(100);int count=0;h.Died+=()=>count++;h.TakeDamage(100);h.TakeDamage(10);Equal(count,1);});
        test("生命变化事件：携带最新数值",()=>{var h=new HealthModel(100);float value=0;h.Changed+=(v,m)=>value=v;h.TakeDamage(24);Equal(value,76);});
        test("治疗不能超过上限",()=>{var h=new HealthModel(100);h.TakeDamage(24);h.Heal(50);Equal(h.Current,100);});
        test("死亡后普通治疗不能复活",()=>{var h=new HealthModel(100);h.TakeDamage(100);h.Heal(25);Equal(h.Current,0);});
        test("重开：恢复满血并允许再次死亡通知",()=>{var h=new HealthModel(100);int count=0;h.Died+=()=>count++;h.TakeDamage(100);h.Reset();Equal(h.Current,100);h.TakeDamage(100);Equal(count,2);});
        test("非法伤害：拒绝负值和 NaN",()=>{var h=new HealthModel(100);Reject(()=>h.TakeDamage(-1));Reject(()=>h.TakeDamage(float.NaN));Equal(h.Current,100);});
        test("冲刺冷却：期间不允许再次触发",()=>{var c=new CooldownGate();Check(c.TryStart(1.1f),"首次触发失败");Check(!c.TryStart(1.1f),"冷却被重复触发");});
        test("冷却经过时间后重新就绪",()=>{var c=new CooldownGate();c.TryStart(1.1f);c.Tick(.5f);Equal(c.Remaining,.6f);c.Tick(.7f);Check(c.Ready,"冷却没有结束");});
        test("暂停：零时间步不减少冷却",()=>{var c=new CooldownGate();c.TryStart(1.1f);c.Tick(0);Equal(c.Remaining,1.1f);});
        test("冷却重置：新局立即可用",()=>{var c=new CooldownGate();c.TryStart(1.1f);c.Reset();Check(c.Ready,"残留上局冷却");});
        test("非法时间步被拒绝",()=>{var c=new CooldownGate();Reject(()=>c.Tick(-1));Reject(()=>c.Tick(float.PositiveInfinity));});
        test("配置检查：持续时间、冷却和速度有效",()=>{var s=AssetDatabase.LoadAssetAtPath<CombatSettings>("Assets/Resources/CombatSettings.asset");Check(s!=null,"缺少配置资源");Check(s.dashDuration>0 && s.dashCooldown>=s.dashDuration,"冷却不能短于冲刺持续时间");Check(s.dashSpeed>s.moveSpeed && s.moveSpeed>0,"冲刺应比普通移动快");Check(s.dashInvulnerability>=0,"无敌时间不能为负");});
        test("精英：蓄力开始锁定方向",()=>{var b=new EliteChargeBrain();b.Tick(1.5f,Vector2.right);Check(b.State==EliteChargeBrain.Mode.Windup,"未进入蓄力");b.Tick(.4f,Vector2.left);Check(b.LockedDirection==Vector2.right,"蓄力追踪了玩家");});
        test("精英：预警结束才冲锋，方向不追踪",()=>{var b=new EliteChargeBrain();b.Tick(1.5f,Vector2.right);b.Tick(.91f,Vector2.left);Check(b.State==EliteChargeBrain.Mode.Charge,"冲锋阶段错误");Check(b.LockedDirection==Vector2.right,"冲锋转向");});
        test("精英：撞柱进入硬直后恢复追击",()=>{var b=new EliteChargeBrain();b.Tick(2,Vector2.right);b.Tick(1,Vector2.right);b.HitObstacle();Check(b.State==EliteChargeBrain.Mode.Stunned,"无撞柱硬直");b.Tick(1.6f,Vector2.left);Check(b.State==EliteChargeBrain.Mode.Chase,"硬直不结束");});
        test("精英：非冲锋时碰撞不触发硬直",()=>{var b=new EliteChargeBrain();b.HitObstacle();Check(b.State==EliteChargeBrain.Mode.Chase,"误触发硬直");});
        test("精英：自然冲完恢复，暂停不推进",()=>{var b=new EliteChargeBrain();b.Tick(2,Vector2.right);b.Tick(1,Vector2.right);float before=b.Remaining;b.Tick(0,Vector2.left);Equal(b.Remaining,before);b.Tick(1,Vector2.left);Check(b.State==EliteChargeBrain.Mode.Recover,"没有恢复阶段");});
        test("精英：死亡不能再次攻击",()=>{var b=new EliteChargeBrain();b.Die();b.Tick(100,Vector2.right);b.HitObstacle();Check(b.State==EliteChargeBrain.Mode.Dead,"死亡状态退出");});
        test("构筑：第二次强化保留第一次",()=>{var b=new SkillLoadout();b.Add(0);b.Add(1);Check(b.Has(0)&&b.Has(1)&&b.ThermalShock,"组合丢失");});
        test("构筑：拒绝重复及越界技能",()=>{var b=new SkillLoadout();Check(b.Add(4),"首次添加失败");Check(!b.Add(4)&&!b.Add(-1)&&!b.Add(SkillLoadout.Count),"非法技能被接受");});
        test("构筑：多重与穿透组合",()=>{var b=new SkillLoadout();b.Add(4);b.Add(2);Equal(b.ProjectileCount,3);Equal(b.Penetration,3);Equal(b.ProjectileDamage,17);});
        test("构筑：重开清空强化",()=>{var b=new SkillLoadout();b.Add(5);b.Reset();Equal(b.Mask,0);Equal(b.ProjectileCount,1);});
        test("地面碰撞：矩形边缘和角落分离",()=>{var r=new Rect(100,100,60,30);foreach(var p in new[]{new Vector2(95,115),new Vector2(96,96),r.center}){var resolved=ArenaLayout.Resolve(p,16,r);Check(!ArenaLayout.CircleOverlaps(resolved,15.99f,r),"仍在碰撞体内");}});
        test("地图边界：考虑角色半径",()=>{var p=ArenaLayout.Clamp(Vector2.zero,16);Equal(p.x,ArenaLayout.Bounds.xMin+16);Equal(p.y,ArenaLayout.Bounds.yMin+16);});
        test("蓄力：不足时长不发射也不消耗冷却",()=>{var c=new ChargeAttack();c.Begin();c.Tick(.3f);Check(!c.Release(),"提前发射");Equal(c.Cooldown,0);});
        test("蓄力：释放后进入冷却",()=>{var c=new ChargeAttack();c.Begin();c.Tick(.65f);Check(c.Release(),"满蓄力未发射");Check(!c.Begin(),"绕过冷却");c.Tick(2.8f);Check(c.Begin(),"冷却未结束");});
        test("蓄力：零时间步不推进，取消无冷却",()=>{var c=new ChargeAttack();c.Begin();c.Tick(0);Equal(c.Fraction,0);c.Cancel();Check(!c.Charging,"取消失败");Equal(c.Cooldown,0);});
        test("破绽：仅暴露阶段有额外伤害",()=>{Equal(ChargeAttack.Vulnerability(false),1);Equal(ChargeAttack.Vulnerability(true),1.75f);});
        test("对象池：预热、复用、归还重置",()=>{using(var p=new LeasePool<PoolItem>(()=>new PoolItem(),x=>x.value=0,x=>{},2,4)){var a=p.Rent();a.value=9;Check(p.Return(a),"归还失败");Check(ReferenceEquals(a,p.Rent()),"没有复用");Equal(a.value,0);Equal(p.Created,2);}});
        test("对象池：拒绝重复与外部对象归还",()=>{using(var p=new LeasePool<PoolItem>(()=>new PoolItem(),x=>{},x=>{},0,2)){var a=p.Rent();Check(p.Return(a),"首次归还失败");Check(!p.Return(a)&&!p.Return(new PoolItem()),"非法归还污染池");Equal(p.Inactive,1);}});
        test("对象池：扩容后空闲保留有上限",()=>{int destroyed=0;using(var p=new LeasePool<PoolItem>(()=>new PoolItem(),x=>{},x=>destroyed++,1,2)){var a=p.Rent();var b=p.Rent();var c=p.Rent();p.Return(a);p.Return(b);p.Return(c);Equal(p.PeakActive,3);Equal(p.Inactive,2);Equal(destroyed,1);}});
        test("对象池：销毁覆盖活跃与空闲且幂等",()=>{int destroyed=0;var p=new LeasePool<PoolItem>(()=>new PoolItem(),x=>{},x=>destroyed++,2,2);p.Rent();p.Dispose();p.Dispose();Equal(destroyed,2);Equal(p.Active,0);Equal(p.Inactive,0);bool rejected=false;try{p.Rent();}catch(ObjectDisposedException){rejected=true;}Check(rejected,"销毁后仍可借出");});
        test("波次：预算与并发上限",()=>{var w=new WaveDirector(1);Equal(w.Pending,20);Check(!w.CanSpawn(24),"超出上限");for(int i=0;i<20;i++)w.CommitSpawn();Equal(w.Pending,0);w.Tick(3,24,false);Equal(w.Wave,1);w.Tick(1,18,false);Equal(w.Wave,2);});
        test("波次：暂停不推进，精英仅最终波出现",()=>{var w=new WaveDirector(3);while(w.Pending>0)w.CommitSpawn();w.Tick(0,0,false);Equal(w.Wave,1);w.Tick(1,0,true);Equal(w.Wave,2);Check(!w.NextIsGuard,"精英提前出现");while(w.Wave<4){while(w.Pending>0)w.CommitSpawn();w.Tick(1,0,false);}Check(w.NextIsGuard,"缺少精英");});
        test("波次：显式终止停止预算",()=>{var w=new WaveDirector(3);w.CancelReinforcements();Check(w.Finished&&!w.CanSpawn(0)&&w.Pending==0,"继续刷怪");});
        test("波次：三关预算100/180/260",()=>{for(int r=1;r<=3;r++){var w=new WaveDirector(r);int count=0;while(!w.Finished){while(w.Pending>0){w.CommitSpawn();count++;}w.Tick(1,0,false);}Equal(count,r==1?100:r==2?180:260);}});
        test("投射者：完整预警后仅发射一次",()=>{var b=new SentryBrain(new SentryRules());Check(!b.Tick(.5f,Vector2.right*300,true),"进入预警就发射");Check(b.State==SentryBrain.Mode.Windup,"未预警");Check(!b.Tick(.8f,Vector2.right*300,true),"提前发射");Check(b.Tick(.11f,Vector2.right*300,true),"未发射");Check(!b.Tick(.1f,Vector2.right*300,true),"重复发射");});
        test("投射者：锁定方向，不追踪走位",()=>{var b=new SentryBrain(new SentryRules());b.Tick(.5f,Vector2.right*300,true);b.Tick(.4f,Vector2.left*300,true);Check(b.LockedDirection==Vector2.right,"蓄力追踪");});
        test("投射者：遮挡和超出射程不蓄力",()=>{var b=new SentryBrain(new SentryRules());b.Tick(2,Vector2.right*300,false);Check(b.State==SentryBrain.Mode.Approach,"隔墙蓄力");b.Tick(2,Vector2.right*600,true);Check(b.State==SentryBrain.Mode.Approach,"超射程攻击");});
        test("投射者：短撤退后不无限逃跑",()=>{var b=new SentryBrain(new SentryRules());b.Tick(.1f,Vector2.right*100,true);Check(b.State==SentryBrain.Mode.Retreat,"没有撤退");b.Tick(.5f,Vector2.right*100,true);b.Tick(.1f,Vector2.right*100,true);Check(b.State==SentryBrain.Mode.Windup,"连续逃跑");});
        test("投射者：冻结中断需要重新预警",()=>{var b=new SentryBrain(new SentryRules());b.Tick(.5f,Vector2.right*300,true);b.Interrupt();Check(b.State==SentryBrain.Mode.Recover,"未取消预警");Check(!b.Tick(2,Vector2.right*300,true),"解冻补射");b.Tick(.1f,Vector2.right*300,true);Check(b.State==SentryBrain.Mode.Windup,"没有重新预警");});
        test("投射者：暂停与死亡不发射",()=>{var b=new SentryBrain(new SentryRules());b.Tick(.5f,Vector2.right*300,true);float time=b.Remaining;Check(!b.Tick(0,Vector2.left,true),"暂停射击");Equal(b.Remaining,time);b.Die();Check(!b.Tick(10,Vector2.right,true)&&b.State==SentryBrain.Mode.Dead,"死后攻击");});
        test("投射者：拒绝非法配置和时间",()=>{Reject(()=>new SentryRules(windup:0));Reject(()=>new SentryRules(keepDistance:600));var b=new SentryBrain(new SentryRules());Reject(()=>b.Tick(float.NaN,Vector2.right,true));});
        if(demo)test("故障演示：故意把预期剩余血量写为 77",()=>{var h=new HealthModel(100);h.TakeDamage(24);Equal(h.Current,77);});
        watch.Stop();
        LastReport=(demo?"故障演示（故意失败，不修改游戏）":"规则测试")+"\n"+passed+" 通过 / "+failed+" 失败 · "+watch.Elapsed.TotalMilliseconds.ToString("F1")+" ms\n\n"+string.Join("\n",lines);
        File.WriteAllText(demo?"rule-tests-demo.txt":"rule-tests-report.txt",LastReport);
        UnityEngine.Debug.Log(LastReport);
        return failed==0;
    }
    public static void Batch()
    {
        PortfolioConfigInstaller.Ensure();
        bool detected=!RunAll(true);
        bool passed=RunAll(false);
        UnityEngine.Debug.Log("RULE_TESTS_RESULT: passed="+passed+", demonstration_detected="+detected);
        EditorApplication.Exit(passed && detected?0:1);
    }
}

public sealed class PortfolioTestWindow : EditorWindow
{
    Vector2 scroll;
    [MenuItem("Portfolio/Tests/Open Test Panel")]
    public static void Open() { GetWindow<PortfolioTestWindow>("作品集测试"); }
    void OnGUI()
    {
        EditorGUILayout.HelpBox("规则测试无需进入 Play Mode。故障演示仅改变测试预期，不修改游戏。完整场景回归请使用 README 中的批处理命令。",MessageType.Info);
        if(GUILayout.Button("运行规则测试")){PortfolioConfigInstaller.Ensure();PortfolioRuleTests.RunAll(false);}
        if(GUILayout.Button("演示一次失败（故意错误断言）"))PortfolioRuleTests.RunAll(true);
        if(GUILayout.Button("选择冲刺配置"))Selection.activeObject=AssetDatabase.LoadAssetAtPath<CombatSettings>("Assets/Resources/CombatSettings.asset");
        scroll=EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.TextArea(PortfolioRuleTests.LastReport,GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }
}
