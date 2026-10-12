using System;
using System.Collections.Generic;

namespace Portfolio.Domain
{
    // Scene-local ownership. No engine, timer, global registry or gameplay dependency.
    public sealed class LeasePool<T> : IDisposable where T:class
    {
        readonly Stack<T> idle=new Stack<T>();
        readonly HashSet<T> leased=new HashSet<T>();
        readonly Func<T> create;
        readonly Action<T> reset,destroy;
        readonly int capacity;
        bool disposed;
        public int Created { get; private set; }
        public int Destroyed { get; private set; }
        public int Rents { get; private set; }
        public int Returns { get; private set; }
        public int PeakActive { get; private set; }
        public int Active=>leased.Count;
        public int Inactive=>idle.Count;
        public LeasePool(Func<T> factory,Action<T> onReturn,Action<T> onDestroy,int prewarm,int maxRetained)
        {
            if(factory==null||onReturn==null||onDestroy==null)throw new ArgumentNullException();
            if(prewarm<0||maxRetained<prewarm)throw new ArgumentOutOfRangeException(nameof(prewarm));
            create=factory;reset=onReturn;destroy=onDestroy;capacity=maxRetained;
            for(int i=0;i<prewarm;i++){var item=Create();reset(item);idle.Push(item);}
        }
        T Create(){var item=create();if(item==null)throw new InvalidOperationException("Pool factory returned null");Created++;return item;}
        public T Rent()
        {
            if(disposed)throw new ObjectDisposedException(nameof(LeasePool<T>));
            var item=idle.Count>0?idle.Pop():Create();
            if(!leased.Add(item))throw new InvalidOperationException("Pool factory returned a leased instance");
            Rents++;PeakActive=Math.Max(PeakActive,Active);return item;
        }
        public bool Return(T item)
        {
            // Reject double returns and objects owned by another pool.
            if(disposed||item==null||!leased.Remove(item))return false;
            reset(item);Returns++;
            if(idle.Count<capacity)idle.Push(item);else{destroy(item);Destroyed++;}
            return true;
        }
        public void Dispose()
        {
            if(disposed)return;disposed=true;
            foreach(var item in leased){reset(item);destroy(item);Destroyed++;}leased.Clear();
            while(idle.Count>0){destroy(idle.Pop());Destroyed++;}
        }
    }
}
