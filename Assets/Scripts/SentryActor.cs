using System;
using Portfolio.Domain;
using UnityEngine;

// Presentation/movement adapter. The brain emits one fire event; the room owns projectiles.
public sealed class SentryActor : IDisposable
{
    public readonly SentryBrain Brain;
    readonly SpriteRenderer view;
    readonly LineRenderer warning;
    readonly Func<Vector2,Vector2,float,Vector2> move;
    readonly Func<Vector2,Vector2,bool> clearPath;
    readonly Action<Vector2,Vector2> fire;
    readonly Sprite[,] frames=new Sprite[4,4];
    readonly float speed,range,windup;
    int direction;
    float age,deathAge;
    public const float Radius=22;
    public bool WarningVisible=>warning!=null && warning.enabled && warning.gameObject.activeInHierarchy;
    public SentryActor(SpriteRenderer view,Func<string,Sprite> art,Func<Vector2,Vector2,float,Vector2> move,
        Func<Vector2,Vector2,bool> clearPath,Action<Vector2,Vector2> fire,Material material,SentrySettings settings)
    {
        this.view=view;this.move=move;this.clearPath=clearPath;this.fire=fire;
        speed=settings.moveSpeed;range=settings.projectileSpeed*settings.projectileLifetime;windup=settings.windup;Brain=new SentryBrain(settings.CreateRules());
        var directions=new[]{"down","up","left","right"};var poses=new[]{"idle","windup","fire","recover"};
        for(int d=0;d<4;d++)for(int p=0;p<4;p++)frames[d,p]=art("sentry/"+directions[d]+"_"+poses[p]);
        var go=new GameObject("Sentry locked aim");go.transform.SetParent(view.transform.parent,false);
        warning=go.AddComponent<LineRenderer>();warning.sharedMaterial=material;warning.useWorldSpace=true;
        warning.positionCount=2;warning.startWidth=warning.endWidth=3;warning.sortingOrder=4900;warning.enabled=false;
    }
    public Vector2 Tick(float dt,Vector2 position,Vector2 target,float speedFactor)
    {
        age+=dt;bool shoot=Brain.Tick(dt,target-position,clearPath(position,target));
        var delta=Brain.MoveDirection*speed*speedFactor*dt;
        var next=move(position,delta,Radius);
        if(delta.sqrMagnitude>0 && (next-position).sqrMagnitude<delta.sqrMagnitude*.25f)
            next=move(position,new Vector2(-delta.y,delta.x),Radius);
        var look=Brain.State==SentryBrain.Mode.Approach || Brain.State==SentryBrain.Mode.Retreat?target-next:Brain.LockedDirection;
        direction=Mathf.Abs(look.x)>Mathf.Abs(look.y)?(look.x<0?2:3):(look.y<0?1:0);
        int pose=Brain.State==SentryBrain.Mode.Windup?1:Brain.State==SentryBrain.Mode.Fire?2:Brain.State==SentryBrain.Mode.Recover?3:0;
        view.sprite=frames[direction,pose];view.transform.localScale=Vector3.one*(128f/512);
        view.transform.position=new Vector3(next.x,720-next.y+Mathf.Sin(age*3)*2,0);view.sortingOrder=(int)next.y;
        warning.enabled=Brain.State==SentryBrain.Mode.Windup;
        if(warning.enabled)
        {
            var start=next+Brain.LockedDirection*26;var end=start;
            for(float distance=12;distance<=range;distance+=12)
            {var candidate=start+Brain.LockedDirection*distance;if(!clearPath(end,candidate))break;end=candidate;}
            warning.SetPosition(0,new Vector3(start.x,752-start.y,0));warning.SetPosition(1,new Vector3(end.x,752-end.y,0));
            warning.startColor=warning.endColor=new Color(.8f,.4f,1,.35f+.5f*(1-Brain.Remaining/windup));
        }
        if(shoot)fire(next+Brain.LockedDirection*26,Brain.LockedDirection);
        return next;
    }
    public void Freeze(){Brain.Interrupt();warning.enabled=false;}
    public void Die(){Brain.Die();warning.enabled=false;}
    public void RenderDeath(float dt)
    {
        Die();deathAge+=dt;view.color=new Color(.55f,.4f,.7f,Mathf.Max(0,1-deathAge/.8f));
    }
    public void Dispose(){Brain.Die();if(warning!=null){warning.gameObject.SetActive(false);UnityEngine.Object.Destroy(warning.gameObject);}}
}
