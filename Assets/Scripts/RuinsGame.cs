using System.Collections.Generic;
using UnityEngine;
using Portfolio.Domain;

    // Screen-down world coordinates are converted only at rendering/input boundaries.
public sealed partial class RuinsGame : MonoBehaviour
{
    static readonly Unity.Profiling.ProfilerMarker SimulationMarker=new Unity.Profiling.ProfilerMarker("Portfolio.Simulation");
    static readonly string[] SlimeDown={"enemy/move_down_01","enemy/move_down_02"},SlimeLeft={"enemy/move_left_01","enemy/move_left_02"},SlimeRight={"enemy/move_right_01","enemy/move_right_02"};
    static readonly string[] ProjectileFrames={"vfx/projectile_flight_01","vfx/projectile_flight_02","vfx/projectile_flight_03"};
    static readonly string[] ImpactFrames={"vfx/impact_01","vfx/impact_02","vfx/impact_03","vfx/impact_04"};
    enum Phase { Title, Playing, Upgrade, Paused, Dying, Result }
    sealed class Enemy { public Vector2 p; public float hp, max, slow, burn, tick, hurt, reaction, frozen, spawnDelay, deathFade, breach, armor; public bool fast, pooled; public SpriteRenderer view; public EliteGuardActor elite; public SentryActor sentry; public LineEffect[] spawnMarks; }
    sealed class Shot { public Vector2 p, velocity; public float life = 1.5f, damage; public bool heavy; public int remaining; public HashSet<Enemy> hit = new HashSet<Enemy>(); public SpriteRenderer view; }
    readonly List<Sprite> floorSprites=new List<Sprite>();
    readonly List<Vector2> spawnExclusions=new List<Vector2>();
    sealed class Impact { public Vector2 p; public float age; public SpriteRenderer view; }
    readonly Dictionary<string, Sprite> art = new Dictionary<string, Sprite>();
    readonly List<Enemy> enemies = new List<Enemy>();
    readonly List<Shot> shots = new List<Shot>();
    readonly List<Impact> impacts = new List<Impact>();
    int[] choices = new int[3];
    Phase phase = Phase.Title;
    Camera gameCamera;
    SpriteRenderer heroView, portal;
    Vector2 player = ArenaLayout.Entrance;
    readonly HealthModel vitality = new HealthModel(100);
    float health => vitality.Current;
    float elapsed, cooldown, invincible, hurt, shotAge = 10, deathAge;
    CombatSettings settings;
    int room, upgrade = -1, kills, fired, hits;
    bool moving, won;
    string facing = "down";

    Sprite Art(string key)
    {
        if (!art.TryGetValue(key, out var value))
        {
            value = Resources.Load<Sprite>("Art/" + key);
            art[key] = value;
            if (value == null) Debug.LogError("Missing sprite: " + key);
        }
        return value;
    }
    SpriteRenderer Make(string key, Vector2 position, float width, int order = 0)
    {
        var go = new GameObject(key);
        go.transform.SetParent(transform);
        var sr = go.AddComponent<SpriteRenderer>();
        SetSprite(sr, key, width);
        Place(sr, position, order);
        return sr;
    }
    void SetSprite(SpriteRenderer view, string key, float width)
    {
        var sprite = Art(key);
        if(view.sprite!=sprite)view.sprite = sprite;
        if (sprite != null){var scale=Vector3.one*(width/sprite.rect.width);if(view.transform.localScale!=scale)view.transform.localScale=scale;}
    }
    void MakeDecoration(string key,Vector2 p,float width)
    {
        Make(key,p,width,(int)p.y);spawnExclusions.Add(p);
    }
    static void Place(SpriteRenderer view, Vector2 p, int order)
    {
        view.transform.position = new Vector3(p.x, 720 - p.y, 0);
        view.sortingOrder = order;
    }
    void Awake()
    {
        settings=Resources.Load<CombatSettings>("CombatSettings");
        if(settings==null) { Debug.LogError("Missing CombatSettings asset");enabled=false;return; }
        attackTimeline=Resources.Load<Portfolio.Configuration.AttackTimeline>("AttackTimeline");
        if(attackTimeline==null || !attackTimeline.IsValid){Debug.LogError("Missing or invalid AttackTimeline asset");enabled=false;return;}
        loadout.Configure(Portfolio.Configuration.SkillCatalog.Instance);
        var cameraObject = new GameObject("Game Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.transform.SetParent(transform);
        gameCamera = cameraObject.GetComponent<Camera>();
        gameCamera.orthographic = true;
        gameCamera.transform.position = new Vector3(ArenaLayout.Width/2,720-ArenaLayout.Height/2,-10);
        gameCamera.orthographicSize=500;
        gameCamera.clearFlags = CameraClearFlags.SolidColor;
        gameCamera.backgroundColor = new Color(.035f, .065f, .08f);
        InitializeMap();
        portal = Make("portals/portal_sealed", ArenaLayout.Portal, 155, (int)ArenaLayout.Portal.y);
        heroView = Make("hero_movement/idle_down", player, 180, 830);
        SetupFeedback();
        InitializePools();
        InitializeReactions();
        InitializeHorde();
        InitializeSentries();
        InitializeUi();
    }
    void ClearActors()
    {
        reactionFeedback?.Clear();
        ResetCombatFeel();
        foreach (var e in enemies) ReleaseEnemy(e);
        enemies.Clear();ClearPooledActors();
    }
    void StartRun()
    {
        ClearActors(); phase = Phase.Playing; room = 1; upgrade = -1;
        loadout.Reset();core=-1;won=false;
        vitality.Reset(shieldHero?ShieldDefense.MaxHealth:100); elapsed = cooldown = invincible = hurt = deathAge = 0;
        shotAge = 10; kills = fired = hits = 0; facing = "down";moving=false;ResetHeroMotion();
        player = ArenaLayout.Entrance; SpawnRoom();
        dashGate.Reset();dashTime = 0;
    }
    void SpawnRoom()
    {
        ClearActors();
        ResetShield();
        RebuildMap();
        ResetObstacles(); roomCleared = false;midRewardGiven=false;entryCooldown=0;rangedWave=0;regroupCooldown=0;
        SetSprite(portal, "portals/portal_sealed", 155);
        waves=new WaveDirector(room);
        TickWaves(0);
    }
    static string Direction(Vector2 delta) => Mathf.Abs(delta.x) > Mathf.Abs(delta.y) ? (delta.x < 0 ? "left" : "right") : (delta.y < 0 ? "up" : "down");
    void Fire(Vector2 target)
    {
        if(shieldHero){BeginMelee(target);return;}
        if (cooldown > 0 || chargeAttack.Charging) return;
        var origin=PrepareRangedAim(target,out var dir);
        shotAge = 0; cooldown = attackTimeline.rangedCooldown; fired+=loadout.ProjectileCount;
        PlayTone(shotSound,.12f);
        for(int i=0;i<loadout.ProjectileCount;i++)
        {
            float angle=loadout.ProjectileCount==1?0:(i-1)*12*Mathf.Deg2Rad;
            var heading=new Vector2(dir.x*Mathf.Cos(angle)-dir.y*Mathf.Sin(angle),dir.x*Mathf.Sin(angle)+dir.y*Mathf.Cos(angle));
            SpawnShot(origin,heading*720,loadout.ProjectileDamage,loadout.Penetration,false);
        }
    }
    void Damage(Enemy e, float amount, bool effects)
    {
        if (e.hp <= 0 || e.spawnDelay>0) return;
        bool exposed=e.elite!=null && e.elite.Brain.State==EliteChargeBrain.Mode.Stunned;
        amount*=ChargeAttack.Vulnerability(exposed);
        bool wasBurning=e.burn>0,wasChilled=e.slow>0 || e.frozen>0;
        e.hp -= amount; e.hurt = .14f;
        if(effects && e.elite==null)e.p=MoveSolid(e.p,(e.p-player).normalized*12,ArenaLayout.SlimeRadius);
        PlayTone(hitSound,.2f);
        SpawnImpact(e.p,95);
        if (e.hp <= 0) { kills++;e.elite?.Brain.Die();e.sentry?.Die(); }
        if (effects && !shieldHero)
        {
            TriggerRoutes(e,wasBurning,wasChilled);
            if(e.hp>0)ApplyElements(e);
            if (loadout.Has(3))
                foreach (var other in enemies)
                    if (other != e && other.hp > 0 && other.spawnDelay<=0 && Vector2.Distance(other.p, e.p) < 270 && ClearSkillPath(e.p,other.p))
                    { var from=e.p;var to=other.p;Damage(other,16,false);ReactionPresented?.Invoke(new Portfolio.Presentation.ReactionCue(Portfolio.Presentation.ReactionKind.Chain,from,to));if(other.hp>0)ApplyElements(other);break; }
        }
    }
    void Update()
    {using(SimulationMarker.Auto())TickSimulation();}
    void TickSimulation()
    {
        gameCamera.orthographicSize = Mathf.Min(400,ArenaLayout.Width/(2*Mathf.Max(.1f,gameCamera.aspect)));
        if (Input.GetKeyDown(KeyCode.R) && phase != Phase.Title) {StartRun();BeginCoreChoice();}
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (phase == Phase.Playing) phase = Phase.Paused;
            else if (phase == Phase.Paused) phase = Phase.Playing;
        }
        float dt = Mathf.Min(Time.deltaTime, .05f);
        TickFeedback(dt);
        if (phase == Phase.Dying)
        {
            deathAge += dt; AnimatePlayer();
            if (deathAge >= .9f) { won = false; phase = Phase.Result; }
            return;
        }
        if (phase != Phase.Playing) {ClearBufferedInput();shieldDefense.SetGuard(false);return;}
        CaptureBufferedInput();
        TickPooledLines(dt);
        if(hitStop>0){hitStop=Mathf.Max(0,hitStop-dt);return;}
        TickWaves(dt);if(phase!=Phase.Playing)return;
        RegroupDistantSlimes(dt);TickEntryWaves(dt);TickFire(dt);TickEcho(dt);
        elapsed += dt; cooldown -= dt; invincible -= dt; hurt -= dt; shotAge += dt;
        var aimWorld=gameCamera.ScreenToWorldPoint(Input.mousePosition);
        var aim=new Vector2(aimWorld.x,720-aimWorld.y);
        if(shieldHero)TickShield(dt,aim);else TickCharge(dt,aim);
        Vector2 move = new Vector2((Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0),
            (Input.GetKey(KeyCode.S) ? 1 : 0) - (Input.GetKey(KeyCode.W) ? 1 : 0)).normalized;
        moving = move.sqrMagnitude > 0;
        dashGate.Tick(dt);
        if(bufferedDash){bufferedDash=false;TryDash(shieldHero?(aim-player).normalized:bufferedMove);}
        var motionStart=player;
        MovePlayer(move,dt);
        TickHeroMotion(Vector2.Distance(motionStart,player),dt);
        dashTime=Mathf.Max(0,dashTime-dt);
        if (!shieldHero && moving && shotAge > attackTimeline.rangedWindow) facing = Direction(move);
        if (Input.GetMouseButton(0) && (canvasUI==null || !canvasUI.BlocksPointer))
        {
            var world = gameCamera.ScreenToWorldPoint(Input.mousePosition);
            Fire(new Vector2(world.x, 720 - world.y));
        }
        RebuildEnemyGrid();
        foreach (var e in enemies)
        {
            if(e.spawnDelay>0)continue;
            if (e.hp <= 0) { if(e.elite!=null)e.elite.RenderDeath(dt);else if(e.sentry!=null)e.sentry.RenderDeath(dt);else {SetSprite(e.view,"enemy/defeated",82);e.deathFade+=dt;e.view.color=new Color(.5f,.5f,.6f,Mathf.Max(0,1-e.deathFade/1.1f));}continue; }
            e.hurt -= dt; e.slow -= dt;e.reaction-=dt;e.breach-=dt;e.armor-=dt;
            if (e.burn > 0) { e.burn -= dt; e.tick -= dt; if (e.tick <= 0) { e.tick = .5f; Damage(e, 7, false); } }
            if (e.hp <= 0) continue;
            if(e.frozen>0){e.frozen-=dt;e.sentry?.Freeze();e.view.color=new Color(.45f,.85f,1);continue;}
            Vector2 delta = player - e.p;
            if(e.sentry!=null)
            {
                e.p=e.sentry.Tick(dt,e.p,player,e.slow>0?.45f:1);
                e.view.color=e.hurt>0?new Color(1,.5f,.5f):e.slow>0?new Color(.55f,.85f,1):e.burn>0?new Color(1,.7f,.5f):Color.white;
                continue;
            }
            if(e.elite!=null)
            {
                e.p=e.elite.Tick(dt,e.p,player,e.slow>0?.65f:1);
                e.view.color=e.hurt>0?new Color(1,.6f,.4f):e.elite.Brain.State==EliteChargeBrain.Mode.Stunned?new Color(1,.78f,.3f):Color.white;
                bool charging=e.elite.Brain.State==EliteChargeBrain.Mode.Charge;
                bool stunned=e.elite.Brain.State==EliteChargeBrain.Mode.Stunned;
                if(!stunned && e.elite.Touches(player) && invincible<=0)
                {
                    ReceivePlayerHit(charging?24:12,e.p-player,charging);
                    if(health<=0){phase=Phase.Dying;deathAge=0;AnimatePlayer();return;}
                }
                continue;
            }
            Vector2 chase=delta.normalized*(e.fast?145:room==1?102:112)*(e.slow>0?.45f:1)*dt;
            Vector2 next=MoveSolid(e.p,chase,ArenaLayout.SlimeRadius);
            if((next-e.p).sqrMagnitude<chase.sqrMagnitude*.25f)next=MoveSolid(e.p,new Vector2(-chase.y,chase.x),ArenaLayout.SlimeRadius);
            foreach(var other in NearbyEnemies(next))if(other!=e && other.hp>0 && other.spawnDelay<=0)
            {var away=next-other.p;float space=other.elite==null?42:58;if(away.sqrMagnitude<space*space && away.sqrMagnitude>.01f)next=MoveSolid(next,away.normalized*35*dt,ArenaLayout.SlimeRadius);}
            e.p=next;
            string d = Direction(delta);
            int frame=(int)(elapsed*5)%2;
            string key=e.hurt>0?"enemy/hurt":d=="up"?"enemy/idle_up":d=="left"?SlimeLeft[frame]:d=="right"?SlimeRight[frame]:SlimeDown[frame];
            SetSprite(e.view, key, e.fast?72:88); Place(e.view, e.p, (int)e.p.y);
            e.view.color = e.hurt>0 ? new Color(1,.45f,.35f) : e.slow > 0 ? new Color(.55f,.85f,1) : e.burn > 0 ? new Color(1,.65f,.4f) : e.fast?new Color(.85f,.65f,1):Color.white;
            if (delta.magnitude < ArenaLayout.PlayerRadius+ArenaLayout.SlimeRadius && invincible <= 0)
            {
                ReceivePlayerHit(14,e.p-player,false);
                if (health <= 0) { phase = Phase.Dying; deathAge = 0; AnimatePlayer(); return; }
            }
        }
        for (int i = shots.Count - 1; i >= 0; i--)
        {
            var s = shots[i]; Vector2 before = s.p; s.p += s.velocity * dt; s.life -= dt;
            if(ResolveShot(s,before,s.p)){ReleaseShotAt(i);continue;}
            if (s.remaining == 0 || s.life <= 0) { ReleaseShotAt(i);continue; }
            SetSprite(s.view, ProjectileFrames[(int)(elapsed*14)%3], s.heavy?112:62);
            Place(s.view, s.p, 5000);
            s.view.transform.rotation = Quaternion.Euler(0,0, -Mathf.Atan2(s.velocity.y, s.velocity.x) * Mathf.Rad2Deg);
        }
        for (int i = impacts.Count - 1; i >= 0; i--)
        {
            var effect = impacts[i]; effect.age += dt;
            if (effect.age >= .32f) { impacts.RemoveAt(i);impactPool.Return(effect);continue; }
            SetSprite(effect.view, ImpactFrames[Mathf.Min(3,(int)(effect.age/.08f))], 95);
        }
        TickHostileShots(dt);
        RecycleDefeated(dt);
        AnimatePlayer();
        if(phase!=Phase.Playing)return;
        if (waves!=null && waves.Finished && enemies.TrueForAll(e => e.hp <= 0))
        {
            roomCleared=true;ClearHostileShots();SetSprite(portal,"portals/portal_active",155);
            if(Input.GetKeyDown(KeyCode.E))TryPortal();
        }
    }
    void AnimatePlayer()
    {
        if(shieldHero){AnimateShield();return;}
        string key; bool flip = false;
        if (phase == Phase.Dying) key = "hero_hurt/" + facing + "_" + (deathAge < .18f ? "knockback" : deathAge < .4f ? "fall" : "defeated");
        else if (hurt > 0) key = "hero_hurt/" + facing + "_hurt";
        else if (shotAge < attackTimeline.rangedWindow) key = "hero_attack/attack_" + facing + "_" + attackTimeline.RangedPose(shotAge);
        else if (moving) { flip = facing == "left"; key = "hero_movement/run_" + (flip ? "right" : facing) + "_0" + (1 + heroMotion.WalkFrame); }
        else { flip = facing == "left"; key = "hero_movement/idle_" + (facing == "left" || facing == "right" ? "right_a" : facing); }
        SetSprite(heroView, key, 180); heroView.flipX = flip; Place(heroView, player, (int)player.y);
        ApplyHeroMotion();
        heroView.color=invincible>0 && (int)(elapsed*18)%2==0 ? new Color(1,1,1,.45f):Color.white;
    }
    void OnApplicationFocus(bool focus) { if (!focus && phase == Phase.Playing) {phase = Phase.Paused;chargeAttack.Cancel();} }
    void OnDestroy() { DisposeReactions();DisposeMap();DisposePools();enemyPool?.Dispose();foreach(var e in enemies){e.elite?.Dispose();e.sentry?.Dispose();}foreach(var clip in new[]{shotSound,hitSound,hurtSound})if(clip!=null)Destroy(clip);foreach(var sprite in floorSprites)Destroy(sprite);if(skillMaterial!=null)Destroy(skillMaterial); }
}
