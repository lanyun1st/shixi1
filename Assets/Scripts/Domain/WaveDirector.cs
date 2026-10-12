using System;

namespace Portfolio.Domain
{
    // Owns pacing and budgets only; scene placement and rendering belong to the adapter.
    public sealed class WaveDirector
    {
        readonly int[] sizes;
        readonly int room;
        int issued;
        float rest = .6f;
        public int Wave { get; private set; } = 1;
        public int TotalWaves => sizes.Length;
        public int Cap { get; }
        public bool Cancelled { get; private set; }
        public int Pending => Cancelled ? 0 : sizes[Wave-1]-issued;
        public bool Finished => Cancelled || Wave==TotalWaves && Pending==0;
        public float Rest => rest;
        public bool NextIsGuard => room==3 && Wave==TotalWaves && issued==0;
        public WaveDirector(int room)
        {
            if(room<1 || room>3)throw new ArgumentOutOfRangeException(nameof(room));
            this.room=room;
            sizes=room==1?new[]{20,25,25,30}:room==2?new[]{35,40,45,60}:new[]{55,65,80,60};
            Cap=room==1?24:room==2?40:60;
        }
        public bool CanSpawn(int alive)=>!Cancelled && Pending>0 && alive<Cap;
        public void CommitSpawn(){if(Pending<=0)throw new InvalidOperationException("No remaining wave budget");issued++;}
        public void CancelReinforcements(){Cancelled=true;}
        public void Tick(float dt,int alive,bool guardWindup)
        {
            if(dt<0 || float.IsNaN(dt) || float.IsInfinity(dt))throw new ArgumentOutOfRangeException(nameof(dt));
            if(Finished || Pending>0 || alive>Cap*.75f)return;
            rest=Math.Max(0,rest-dt);
            if(rest>0)return;
            Wave++;issued=0;rest=.6f;
        }
    }
}
