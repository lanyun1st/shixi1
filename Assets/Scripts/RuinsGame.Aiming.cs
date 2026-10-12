using UnityEngine;

public sealed partial class RuinsGame
{
    // UVs calibrated against the four authored 768px aim poses, measured from bottom-left.
    Vector2 StaffMuzzle(string direction)
    {
        Vector2 uv=direction=="right"?new Vector2(.867f,.396f):direction=="left"?new Vector2(.174f,.407f):
            direction=="up"?new Vector2(.508f,.679f):new Vector2(.5f,.246f);
        var sprite=Art("hero_attack/attack_"+direction+"_aim");
        var pixel=Vector2.Scale(uv,sprite.rect.size)-sprite.pivot;
        var local=pixel*(180f/sprite.rect.width);
        // Firing starts at age zero: recoil begins afterwards, but gait is already visible.
        return player+new Vector2(local.x,-local.y)+HeroGaitOffset;
    }
    Vector2 PrepareRangedAim(Vector2 target,out Vector2 direction)
    {
        facing=Direction(target-(player+new Vector2(0,-42)));
        var muzzle=StaffMuzzle(facing);
        // A staff may visually reach into cover: don't create a bullet beyond a thin wall.
        var shoulder=player+new Vector2(0,-42);int steps=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(shoulder,muzzle)/2));
        for(int i=0;i<=steps;i++)
        {
            var p=Vector2.Lerp(shoulder,muzzle,i/(float)steps);bool blocked=false;
            foreach(var obstacle in obstacles)if(obstacle.hp!=0 && Portfolio.Domain.ArenaLayout.CircleOverlaps(p,5,obstacle.footprint)){blocked=true;break;}
            if(blocked){muzzle=p;break;}
        }
        direction=(target-muzzle).normalized;
        if(direction.sqrMagnitude<.001f)direction=facing=="left"?Vector2.left:facing=="right"?Vector2.right:facing=="up"?Vector2.down:Vector2.up;
        return muzzle;
    }
}
