using System;
using Portfolio.Domain;
using UnityEngine;

// Presentation adapter. Movement is supplied by the room, not obtained from a singleton.
public sealed class EliteGuardActor : IDisposable
{
    public readonly EliteChargeBrain Brain=new EliteChargeBrain();
    readonly SpriteRenderer view,warning;
    readonly Func<string,Sprite> art;
    readonly Func<Vector2,Vector2,float,Vector2> move;
    readonly Sprite warningSprite;
    Vector2 previous,position;
    string facing="down";
    float deathAge;
    float stride;
    readonly Sprite[] walkFrames;
    public const float Radius=27;
    public EliteGuardActor(SpriteRenderer renderer,Func<string,Sprite> sprites,Func<Vector2,Vector2,float,Vector2> movement)
    {
        view=renderer;art=sprites;move=movement;
        walkFrames=Resources.LoadAll<Sprite>("Art/elite/walk_cycle");
        var go=new GameObject("Elite charge warning");go.transform.SetParent(view.transform.parent);
        warning=go.AddComponent<SpriteRenderer>();
        warningSprite=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),new Vector2(.5f,.5f),1);
        warning.sprite=warningSprite;warning.sortingOrder=-10;warning.enabled=false;
    }
    public Vector2 Tick(float dt,Vector2 p,Vector2 target,float speedFactor)
    {
        previous=p;Brain.Tick(dt,target-p);
        Vector2 delta=Vector2.zero;
        if(Brain.State==EliteChargeBrain.Mode.Chase)delta=(target-p).normalized*58*speedFactor*dt;
        if(Brain.State==EliteChargeBrain.Mode.Charge)delta=Brain.LockedDirection*EliteChargeBrain.ChargeSpeed*speedFactor*dt;
        Vector2 next=move(p,delta,Radius);
        if(Brain.State==EliteChargeBrain.Mode.Charge && (next-p-delta).sqrMagnitude>1)Brain.HitObstacle();
        if(Brain.State==EliteChargeBrain.Mode.Chase && delta.sqrMagnitude>0 && (next-p).sqrMagnitude<delta.sqrMagnitude*.25f)
            next=move(p,new Vector2(-delta.y,delta.x),Radius);
        position=next;
        Vector2 look=Brain.State==EliteChargeBrain.Mode.Chase?target-p:Brain.LockedDirection;
        facing=Mathf.Abs(look.x)>Mathf.Abs(look.y)?(look.x<0?"left":"right"):(look.y<0?"up":"down");
        string pose=Brain.State==EliteChargeBrain.Mode.Windup?"windup":Brain.State==EliteChargeBrain.Mode.Charge?"charge":Brain.State==EliteChargeBrain.Mode.Stunned?"stunned":"idle";
        view.sprite=art("elite/"+facing+"_"+pose);view.transform.localScale=Vector3.one*(190f/512);
        // Distance-driven walk: no skating while blocked; pauses and windup never advance the cycle.
        if(Brain.State==EliteChargeBrain.Mode.Chase && (next-p).sqrMagnitude>.0001f)
        {
            stride+=Vector2.Distance(next,p)/12f;
            string key="walk_"+facing+"_"+((int)stride%4);
            foreach(var frame in walkFrames)if(frame.name==key){view.sprite=frame;view.transform.localScale=Vector3.one*(190f*.64f/frame.rect.width);break;}
        }
        view.transform.position=new Vector3(next.x,720-next.y,0);view.sortingOrder=(int)next.y;
        warning.enabled=Brain.State==EliteChargeBrain.Mode.Windup;
        if(warning.enabled)
        {
            Vector2 end=p;
            for(int i=0;i<46;i++) { var step=move(end,Brain.LockedDirection*8,Radius);if((step-end-Brain.LockedDirection*8).sqrMagnitude>1)break;end=step; }
            Vector2 centre=(p+end)*.5f;
            warning.transform.position=new Vector3(centre.x,720-centre.y,0);
            warning.transform.rotation=Quaternion.Euler(0,0,-Mathf.Atan2(Brain.LockedDirection.y,Brain.LockedDirection.x)*Mathf.Rad2Deg);
            warning.transform.localScale=new Vector3(Vector2.Distance(p,end),Radius*2,1);
            warning.color=new Color(1,.38f,.06f,.22f+.15f*(1-Brain.Remaining/EliteChargeBrain.WindupSeconds));
        }
        return next;
    }
    public bool Touches(Vector2 target)
    {
        Vector2 delta=position-previous;
        float t=Mathf.Clamp01(Vector2.Dot(target-previous,delta)/Mathf.Max(.001f,delta.sqrMagnitude));
        return Vector2.Distance(target,previous+delta*t)<Radius+ArenaLayout.PlayerRadius;
    }
    public void RenderDeath(float dt)
    {
        Brain.Die();warning.enabled=false;deathAge+=dt;
        view.color=new Color(.4f,.4f,.4f,Mathf.Max(0,1-deathAge/1.2f));
    }
    public void Dispose() { if(warning!=null){warning.gameObject.SetActive(false);UnityEngine.Object.Destroy(warning.gameObject);}if(warningSprite!=null)UnityEngine.Object.Destroy(warningSprite); }
}
