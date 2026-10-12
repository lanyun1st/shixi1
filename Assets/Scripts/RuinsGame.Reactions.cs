using System;
using Portfolio.Presentation;

public sealed partial class RuinsGame
{
    public event Action<ReactionCue> ReactionPresented;
    ReactionFeedback reactionFeedback;
    void SkillCue(ReactionKind kind,UnityEngine.Vector2 center,float radius,UnityEngine.Vector2 direction=default)
        =>ReactionPresented?.Invoke(new ReactionCue(kind,center,center+direction,radius));
    void InitializeReactions()
    {
        reactionFeedback=new ReactionFeedback(transform,new[]{Art("vfx/impact_01"),Art("vfx/impact_02"),Art("vfx/impact_03"),Art("vfx/impact_04")});
        ReactionPresented+=reactionFeedback.Play;
    }
    void DisposeReactions()
    {
        if(reactionFeedback==null)return;
        ReactionPresented-=reactionFeedback.Play;reactionFeedback.Dispose();reactionFeedback=null;
    }
}
