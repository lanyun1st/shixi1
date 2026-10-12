using System;
using System.Collections.Generic;
using Portfolio.Domain;
using UnityEngine;

namespace Portfolio.Presentation
{
    public enum ReactionKind { Overload, Chain, IceShatter, Thermal, Nova, Echo, Slash, HeavySlash, ShieldCone, Slam, DashImpact, Guard, ArmorBreak, Debris, Muzzle }
    // Value snapshots only: presentation never retains an enemy or deals damage.
    public readonly struct ReactionCue
    {
        public readonly ReactionKind Kind;
        public readonly Vector2 From, To;
        public readonly float Radius;
        public ReactionCue(ReactionKind kind,Vector2 from,Vector2 to,float radius=0)
        {Kind=kind;From=from;To=to;Radius=radius;}
    }

    public sealed partial class ReactionFeedback : IDisposable
    {
        sealed class Effect
        {
            public GameObject root;
            public SpriteRenderer flash;
            public LineRenderer rim, sparks;
            public LineRenderer[] branches;
            public readonly Vector2[] path=new Vector2[25];
            public int seed;
            public Mesh mesh;
            public MeshRenderer surface;
            public readonly List<Vector3> vertices=new List<Vector3>(384);
            public readonly List<Color> colors=new List<Color>(384);
            public readonly List<int> triangles=new List<int>(576);
            public ReactionCue cue;
            public float age,life;
        }
        readonly LeasePool<Effect> pool;
        readonly List<Effect> active=new List<Effect>(48);
        readonly Sprite[] frames;
        readonly Material material;
        readonly Material arcMaterial;
        int serial;
        readonly AudioSource audio;
        readonly AudioClip boom,zap;
        float boomGate,zapGate,shake;
        bool disposed;
        public bool Enabled {get;set;}=true;
        public int Active=>active.Count;
        public int Created=>pool.Created;
        public float ShakeAmplitude=>shake/.12f*1.6f;
        public ReactionFeedback(Transform parent,Sprite[] impactFrames)
        {
            frames=impactFrames;
            material=new Material(Shader.Find("Sprites/Default"));
            var arcShader=Resources.Load<Shader>("Map/ReactionArc");
            arcMaterial=new Material(arcShader!=null?arcShader:Shader.Find("Sprites/Default"));
            var sound=new GameObject("Reaction audio");sound.transform.SetParent(parent,false);
            audio=sound.AddComponent<AudioSource>();audio.playOnAwake=false;audio.spatialBlend=0;
            boom=MakeSound(false);zap=MakeSound(true);
            pool=new LeasePool<Effect>(()=>{
                var e=new Effect{root=new GameObject("Pooled reaction feedback")};e.root.transform.SetParent(parent,false);
                e.flash=MakeSprite(e.root.transform,"Reaction core");
                e.rim=MakeLine(e.root.transform,"Reaction rim",5051);
                e.sparks=MakeLine(e.root.transform,"Reaction sparks",5052);
                e.branches=new LineRenderer[3];
                for(int i=0;i<3;i++)e.branches[i]=MakeLine(e.root.transform,"Arc branch "+i,5052);
                var surface=new GameObject("Skill surface",typeof(MeshFilter),typeof(MeshRenderer));surface.transform.SetParent(e.root.transform,false);
                e.mesh=new Mesh{name="Pooled skill geometry"};e.mesh.MarkDynamic();surface.GetComponent<MeshFilter>().sharedMesh=e.mesh;
                e.surface=surface.GetComponent<MeshRenderer>();e.surface.sharedMaterial=material;e.surface.sortingOrder=5049;
                return e;
            },e=>{e.age=e.life=0;e.cue=default;e.flash.sprite=null;e.flash.color=Color.white;
                e.flash.transform.localScale=Vector3.one;e.root.transform.localRotation=Quaternion.identity;
                e.rim.positionCount=e.sparks.positionCount=0;e.root.SetActive(false);
                foreach(var branch in e.branches)branch.positionCount=0;
                e.mesh.Clear();e.surface.enabled=false;e.vertices.Clear();e.colors.Clear();e.triangles.Clear();
            },e=>{UnityEngine.Object.Destroy(e.mesh);UnityEngine.Object.Destroy(e.root);},24,48);
        }
        static SpriteRenderer MakeSprite(Transform parent,string label)
        {
            var go=new GameObject(label);go.transform.SetParent(parent,false);
            var sprite=go.AddComponent<SpriteRenderer>();sprite.sortingOrder=5050;return sprite;
        }
        LineRenderer MakeLine(Transform parent,string label,int order)
        {
            var go=new GameObject(label);go.transform.SetParent(parent,false);
            var line=go.AddComponent<LineRenderer>();line.sharedMaterial=material;line.useWorldSpace=true;
            line.sortingOrder=order;line.numCapVertices=2;line.numCornerVertices=2;return line;
        }
        public void Play(ReactionCue cue)
        {
            if(disposed || !Enabled)return;
            // Visual cap only; dropping an old cue cannot affect the combat result.
            if(active.Count>=48)Release(0);
            var e=pool.Rent();e.cue=cue;e.age=0;e.seed=++serial;e.life=Lifetime(cue.Kind);
            e.root.SetActive(true);active.Add(e);Render(e);
            if(cue.Kind==ReactionKind.Overload)
            {
                shake=.12f;
                if(boomGate<=0){audio.PlayOneShot(boom,.2f);boomGate=.12f;}
            }
            else if(cue.Kind==ReactionKind.Chain && zapGate<=0){audio.PlayOneShot(zap,.1f);zapGate=.055f;}
        }
        public void Tick(float dt)
        {
            if(disposed)return;
            dt=Mathf.Max(0,dt);boomGate-=dt;zapGate-=dt;shake=Mathf.Max(0,shake-dt);
            if(!Enabled){Clear();return;}
            for(int i=active.Count-1;i>=0;i--)
            {var e=active[i];e.age+=dt;if(e.age>=e.life)Release(i);else Render(e);}
        }
        static Vector3 World(Vector2 p)=>new Vector3(p.x,720-p.y,0);
        static void Style(LineRenderer line,Color color,float width)
        {line.startColor=line.endColor=color;line.startWidth=line.endWidth=width;}
        void Render(Effect e)
        {
            if(e.cue.Kind!=ReactionKind.Chain){RenderSkill(e);return;}
            e.flash.enabled=true;
            float t=e.age/e.life;
            e.rim.sharedMaterial=e.sparks.sharedMaterial=arcMaterial;
            Vector2 center=e.cue.To;
            var sprite=frames[Mathf.Min(frames.Length-1,(int)(t*frames.Length))];
            e.flash.sprite=sprite;
            // Center the visible frame irrespective of its imported sprite pivot.
            float size=Mathf.Lerp(42,64,t);
            float scale=sprite==null?1:size/sprite.rect.width;
            e.flash.transform.localScale=Vector3.one*scale;
            e.flash.transform.position=World(center)-(sprite==null?Vector3.zero:sprite.bounds.center*scale);
            RenderArc(e,t);
        }
        static float Noise(int key)
        {unchecked{uint n=(uint)key;n=(n^61)^(n>>16);n*=9;n^=n>>4;n*=0x27d4eb2d;n^=n>>15;return (n&65535)/32767.5f-1;}}
        void RenderArc(Effect e,float t)
        {
            var delta=e.cue.To-e.cue.From;float length=delta.magnitude;
            var direction=delta.normalized;var normal=new Vector2(-direction.y,direction.x);
            int frame=Mathf.FloorToInt(e.age*32),key=e.seed*137+frame*719;
            // Two discharges followed by a dim after-image, not continuous vibration.
            float pulse=e.age<.055f?1:e.age<.09f?.2f:e.age<.155f?.85f:.35f;
            float light=pulse*(1-t);
            e.rim.positionCount=e.sparks.positionCount=25;
            Style(e.rim,new Color(.16f,.5f,1,light*.8f),16*(1-t)+4);
            Style(e.sparks,new Color(.82f,.95f,1,light),3.8f*(1-t)+.7f);
            e.flash.color=new Color(.75f,.9f,1,light*.55f);
            for(int i=0;i<25;i++)
            {
                float u=i/24f;
                float coarse=Mathf.Lerp(Noise(key+i/6),Noise(key+i/6+1),(i%6)/6f);
                float jitter=(coarse*.7f+Noise(key+i*53)*.3f)*Mathf.Min(34,length*.2f)*Mathf.Sin(u*Mathf.PI);
                e.path[i]=Vector2.Lerp(e.cue.From,e.cue.To,u)+normal*jitter;
                e.rim.SetPosition(i,World(e.path[i]));e.sparks.SetPosition(i,World(e.path[i]));
            }
            for(int b=0;b<3;b++)
            {
                var branch=e.branches[b];branch.sharedMaterial=arcMaterial;branch.positionCount=7;
                float side=b%2==0?1:-1;var start=e.path[6+b*6];
                var end=start+direction*(length*.13f)+normal*side*Mathf.Min(34,length*.22f);
                Style(branch,new Color(.3f,.7f,1,light*.7f),2.4f*(1-t)+.2f);branch.endWidth=.15f;
                for(int i=0;i<7;i++)branch.SetPosition(i,World(Vector2.Lerp(start,end,i/6f)+normal*(i==0||i==6?0:Noise(key+b*93+i)*5)));
            }
        }
        void Release(int index){var e=active[index];active.RemoveAt(index);pool.Return(e);}
        public void Clear()
        {for(int i=active.Count-1;i>=0;i--)Release(i);shake=boomGate=zapGate=0;if(audio!=null)audio.Stop();}
        public void SetPaused(bool paused){if(audio==null)return;if(paused)audio.Pause();else audio.UnPause();}
        static AudioClip MakeSound(bool electric)
        {
            const int rate=22050;int count=(int)(rate*(electric?.12f:.26f));var data=new float[count];
            uint seed=7123;float phase=0;
            for(int i=0;i<count;i++)
            {
                float t=i/(float)count;seed=1664525*seed+1013904223;
                float noise=(seed&65535)/32767.5f-1;phase+=2*Mathf.PI*Mathf.Lerp(electric?1450:145,electric?260:45,t)/rate;
                data[i]=(Mathf.Sin(phase)*.65f+noise*.35f)*Mathf.Pow(1-t,3)*Mathf.Min(1,i/80f)*.6f;
            }
            var clip=AudioClip.Create(electric?"Reaction electric":"Reaction overload",count,1,rate,false);clip.SetData(data,0);return clip;
        }
        public void Dispose()
        {if(disposed)return;Clear();disposed=true;pool.Dispose();UnityEngine.Object.Destroy(material);UnityEngine.Object.Destroy(arcMaterial);UnityEngine.Object.Destroy(boom);UnityEngine.Object.Destroy(zap);if(audio!=null)UnityEngine.Object.Destroy(audio.gameObject);}
    }
}
