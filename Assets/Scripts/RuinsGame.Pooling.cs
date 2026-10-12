using Portfolio.Domain;
using UnityEngine;

public sealed partial class RuinsGame
{
    sealed class LineEffect { public LineRenderer view;public float remaining; }
    LeasePool<Shot> shotPool;
    LeasePool<Impact> impactPool;
    LeasePool<FirePatch> firePool;
    LeasePool<LineEffect> linePool;
    const int MaxImpactViews=96;
    void InitializePools()
    {
        if(shotPool!=null)return;
        skillMaterial=new Material(Shader.Find("Sprites/Default"));
        shotPool=new LeasePool<Shot>(()=>new Shot{view=PoolSprite("Pooled projectile")},s=>{
            s.p=s.velocity=Vector2.zero;s.life=s.damage=0;s.heavy=false;s.remaining=0;s.hit.Clear();ResetSprite(s.view);
        },s=>{if(s.view!=null)Destroy(s.view.gameObject);},32,128);
        impactPool=new LeasePool<Impact>(()=>new Impact{view=PoolSprite("Pooled impact")},s=>{
            s.p=Vector2.zero;s.age=0;ResetSprite(s.view);
        },s=>{if(s.view!=null)Destroy(s.view.gameObject);},MaxImpactViews,128);
        firePool=new LeasePool<FirePatch>(()=>new FirePatch{view=PoolSprite("Pooled fire patch")},s=>{
            s.p=Vector2.zero;s.life=s.tick=0;ResetSprite(s.view);
        },s=>{if(s.view!=null)Destroy(s.view.gameObject);},12,16);
        linePool=new LeasePool<LineEffect>(()=>{
            var go=new GameObject("Pooled reaction line");go.transform.SetParent(transform,false);
            var view=go.AddComponent<LineRenderer>();view.sharedMaterial=skillMaterial;
            return new LineEffect{view=view};
        },s=>{
            s.remaining=0;if(s.view==null)return;s.view.positionCount=0;s.view.loop=false;s.view.numCapVertices=0;s.view.startColor=s.view.endColor=Color.white;
            s.view.startWidth=s.view.endWidth=4;s.view.useWorldSpace=true;s.view.sortingOrder=5100;
            s.view.transform.localPosition=Vector3.zero;s.view.transform.localRotation=Quaternion.identity;s.view.transform.localScale=Vector3.one;
            s.view.gameObject.SetActive(false);
        },s=>{if(s.view!=null)Destroy(s.view.gameObject);},16,96);
    }
    SpriteRenderer PoolSprite(string label)
    {
        var go=new GameObject(label);go.transform.SetParent(transform,false);go.SetActive(false);
        return go.AddComponent<SpriteRenderer>();
    }
    static void ResetSprite(SpriteRenderer view)
    {
        if(view==null)return;
        view.sprite=null;view.color=Color.white;view.flipX=view.flipY=false;view.enabled=true;
        view.sortingOrder=0;view.transform.localPosition=Vector3.zero;view.transform.localRotation=Quaternion.identity;
        view.transform.localScale=Vector3.one;view.gameObject.SetActive(false);
    }
    void ShowPooled(SpriteRenderer view,string key,Vector2 p,float width,int order)
    {
        SetSprite(view,key,width);Place(view,p,order);view.gameObject.SetActive(true);
    }
    void SpawnShot(Vector2 p,Vector2 velocity,float damage,int penetration,bool heavy)
    {
        var shot=shotPool.Rent();shot.p=p;shot.velocity=velocity;shot.damage=damage;shot.remaining=penetration;
        shot.heavy=heavy;shot.life=1.5f;
        ShowPooled(shot.view,"vfx/projectile_flight_01",p,heavy?112:62,5000);
        shot.view.transform.rotation=Quaternion.Euler(0,0,-Mathf.Atan2(velocity.y,velocity.x)*Mathf.Rad2Deg);
        shots.Add(shot);
    }
    void ReleaseShotAt(int index){var shot=shots[index];shots.RemoveAt(index);shotPool.Return(shot);}
    void SpawnImpact(Vector2 p,float width)
    {
        // Visual budget only: damage was already resolved. Reuse the oldest flash during bursts.
        if(impacts.Count>=MaxImpactViews){var oldest=impacts[0];impacts.RemoveAt(0);impactPool.Return(oldest);}
        var impact=impactPool.Rent();impact.p=p;impact.age=0;
        ShowPooled(impact.view,"vfx/impact_01",p,width,5100);impacts.Add(impact);
    }
    void SpawnFirePatch(Vector2 p)
    {
        if(firePatches.Count>=12){firePool.Return(firePatches[0]);firePatches.RemoveAt(0);}
        var patch=firePool.Rent();patch.p=p;patch.life=2.4f;patch.tick=0;
        ShowPooled(patch.view,"vfx/impact_02",p,130,-5);patch.view.color=new Color(1,.4f,.12f,.55f);firePatches.Add(patch);
    }
    LineRenderer SpawnLine(int count,Color color)
    {
        var effect=linePool.Rent();effect.remaining=.22f;var line=effect.view;
        line.positionCount=count;line.startColor=line.endColor=color;line.gameObject.SetActive(true);skillLines.Add(effect);return line;
    }
    void TickPooledLines(float dt)
    {
        for(int i=skillLines.Count-1;i>=0;i--){var effect=skillLines[i];effect.remaining-=dt;if(effect.remaining<=0){skillLines.RemoveAt(i);linePool.Return(effect);}}
    }
    void ClearPooledActors()
    {
        ClearHostileShots();
        foreach(var s in shots)shotPool.Return(s);shots.Clear();
        foreach(var s in impacts)impactPool.Return(s);impacts.Clear();
        foreach(var s in skillLines)linePool.Return(s);skillLines.Clear();
    }
    void DisposePools(){shotPool?.Dispose();impactPool?.Dispose();firePool?.Dispose();linePool?.Dispose();hostilePool?.Dispose();}
    static string PoolSummary<T>(string label,LeasePool<T> pool) where T:class
        =>pool==null?label+": 未初始化":label+"  创建="+pool.Created+"  销毁="+pool.Destroyed+"  活跃="+pool.Active+"  空闲="+pool.Inactive+"  峰值="+pool.PeakActive+"  借出="+pool.Rents+"  归还="+pool.Returns;
    public string PoolDiagnostics=>PoolSummary("子弹",shotPool)+"\n"+PoolSummary("敌方弹丸",hostilePool)+"\n"+PoolSummary("命中",impactPool)+"\n"+PoolSummary("燃烧",firePool)+"\n"+PoolSummary("线特效",linePool);
}
