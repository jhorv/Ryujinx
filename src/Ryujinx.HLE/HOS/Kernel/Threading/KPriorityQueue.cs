using Ryujinx.Common.Collections;
using System.Collections.Generic;
using System.Numerics;

namespace Ryujinx.HLE.HOS.Kernel.Threading
{
    class KPriorityQueue
    {
        private readonly RyujinxLinkedList<KThread>[][] _scheduledThreadsPerPrioPerCore;
        private readonly RyujinxLinkedList<KThread>[][] _suggestedThreadsPerPrioPerCore;

        private readonly long[] _scheduledPrioritiesPerCore;
        private readonly long[] _suggestedPrioritiesPerCore;

        public KPriorityQueue()
        {
            _suggestedThreadsPerPrioPerCore = new RyujinxLinkedList<KThread>[KScheduler.PrioritiesCount][];
            _scheduledThreadsPerPrioPerCore = new RyujinxLinkedList<KThread>[KScheduler.PrioritiesCount][];

            for (int prio = 0; prio < KScheduler.PrioritiesCount; prio++)
            {
                _suggestedThreadsPerPrioPerCore[prio] = new RyujinxLinkedList<KThread>[KScheduler.CpuCoresCount];
                _scheduledThreadsPerPrioPerCore[prio] = new RyujinxLinkedList<KThread>[KScheduler.CpuCoresCount];

                for (int core = 0; core < KScheduler.CpuCoresCount; core++)
                {
                    _suggestedThreadsPerPrioPerCore[prio][core] = new RyujinxLinkedList<KThread>();
                    _scheduledThreadsPerPrioPerCore[prio][core] = new RyujinxLinkedList<KThread>();
                }
            }

            _scheduledPrioritiesPerCore = new long[KScheduler.CpuCoresCount];
            _suggestedPrioritiesPerCore = new long[KScheduler.CpuCoresCount];
        }

        public readonly ref struct KThreadEnumerable
        {
            readonly RyujinxLinkedList<KThread>[][] _listPerPrioPerCore;
            readonly long[] _prios;
            readonly int _core;

            public KThreadEnumerable(RyujinxLinkedList<KThread>[][] listPerPrioPerCore, long[] prios, int core)
            {
                _listPerPrioPerCore = listPerPrioPerCore;
                _prios = prios;
                _core = core;
            }

            public Enumerator GetEnumerator()
            {
                return new Enumerator(_listPerPrioPerCore, _prios, _core);
            }

            public ref struct Enumerator
            {
                private readonly RyujinxLinkedList<KThread>[][] _listPerPrioPerCore;
                private readonly int _core;
                private long _prioMask;
                private int _prio;
                private RyujinxLinkedList<KThread> _list;
                private RyujinxLinkedListNode<KThread> _node;

                public Enumerator(RyujinxLinkedList<KThread>[][] listPerPrioPerCore, long[] prios, int core)
                {
                    _listPerPrioPerCore = listPerPrioPerCore;
                    _core = core;
                    _prioMask = prios[core];
                    _prio = BitOperations.TrailingZeroCount(_prioMask);
                    _prioMask &= ~(1L << _prio);
                }

                public readonly KThread Current => _node?.Value;

                public bool MoveNext()
                {
                    _node = _node?.Next;

                    if (_node == null)
                    {
                        if (!MoveNextListAndFirstNode())
                        {
                            return false;
                        }
                    }

                    return _node != null;
                }

                private bool MoveNextListAndFirstNode()
                {
                    if (_prio < KScheduler.PrioritiesCount)
                    {
                        _list = _listPerPrioPerCore[_prio][_core];

                        _node = _list.First;

                        _prio = BitOperations.TrailingZeroCount(_prioMask);

                        _prioMask &= ~(1L << _prio);

                        return true;
                    }
                    else
                    {
                        _list = null;
                        _node = null;
                        return false;
                    }
                }
            }
        }

        public KThreadEnumerable ScheduledThreads(int core)
        {
            return new KThreadEnumerable(_scheduledThreadsPerPrioPerCore, _scheduledPrioritiesPerCore, core);
        }

        public KThreadEnumerable SuggestedThreads(int core)
        {
            return new KThreadEnumerable(_suggestedThreadsPerPrioPerCore, _suggestedPrioritiesPerCore, core);
        }

        public KThread ScheduledThreadsFirstOrDefault(int core)
        {
            return ScheduledThreadsElementAtOrDefault(core, 0);
        }

        public KThread ScheduledThreadsElementAtOrDefault(int core, int index)
        {
            int currentIndex = 0;
            foreach (var scheduledThread in ScheduledThreads(core))
            {
                if (currentIndex == index)
                {
                    return scheduledThread;
                }
                else
                {
                    currentIndex++;
                }
            }

            return null;
        }

        public KThread ScheduledThreadsWithDynamicPriorityFirstOrDefault(int core, int dynamicPriority)
        {
            foreach (var scheduledThread in ScheduledThreads(core))
            {
                if (scheduledThread.DynamicPriority == dynamicPriority)
                {
                    return scheduledThread;
                }
            }

            return null;
        }

        public bool HasScheduledThreads(int core)
        {
            return ScheduledThreadsFirstOrDefault(core) != null;
        }

        public void TransferToCore(int prio, int dstCore, KThread thread)
        {
            int srcCore = thread.ActiveCore;
            if (srcCore == dstCore)
            {
                return;
            }

            thread.ActiveCore = dstCore;

            if (srcCore >= 0)
            {
                Unschedule(prio, srcCore, thread);
            }

            if (dstCore >= 0)
            {
                Unsuggest(prio, dstCore, thread);
                Schedule(prio, dstCore, thread);
            }

            if (srcCore >= 0)
            {
                Suggest(prio, srcCore, thread);
            }
        }

        public void Suggest(int prio, int core, KThread thread)
        {
            if (prio >= KScheduler.PrioritiesCount)
            {
                return;
            }

            thread.SiblingsPerCore[core] = SuggestedQueue(prio, core).AddFirst(thread);

            _suggestedPrioritiesPerCore[core] |= 1L << prio;
        }

        public void Unsuggest(int prio, int core, KThread thread)
        {
            if (prio >= KScheduler.PrioritiesCount)
            {
                return;
            }

            RyujinxLinkedList<KThread> queue = SuggestedQueue(prio, core);

            queue.Remove(thread.SiblingsPerCore[core]);

            if (queue.First == null)
            {
                _suggestedPrioritiesPerCore[core] &= ~(1L << prio);
            }
        }

        public void Schedule(int prio, int core, KThread thread)
        {
            if (prio >= KScheduler.PrioritiesCount)
            {
                return;
            }

            thread.SiblingsPerCore[core] = ScheduledQueue(prio, core).AddLast(thread);

            _scheduledPrioritiesPerCore[core] |= 1L << prio;
        }

        public void SchedulePrepend(int prio, int core, KThread thread)
        {
            if (prio >= KScheduler.PrioritiesCount)
            {
                return;
            }

            thread.SiblingsPerCore[core] = ScheduledQueue(prio, core).AddFirst(thread);

            _scheduledPrioritiesPerCore[core] |= 1L << prio;
        }

        public KThread Reschedule(int prio, int core, KThread thread)
        {
            if (prio >= KScheduler.PrioritiesCount)
            {
                return null;
            }

            RyujinxLinkedList<KThread> queue = ScheduledQueue(prio, core);

            queue.Remove(thread.SiblingsPerCore[core]);

            thread.SiblingsPerCore[core] = queue.AddLast(thread);

            return queue.First.Value;
        }

        public void Unschedule(int prio, int core, KThread thread)
        {
            if (prio >= KScheduler.PrioritiesCount)
            {
                return;
            }

            RyujinxLinkedList<KThread> queue = ScheduledQueue(prio, core);

            queue.Remove(thread.SiblingsPerCore[core]);

            if (queue.First == null)
            {
                _scheduledPrioritiesPerCore[core] &= ~(1L << prio);
            }
        }

        private RyujinxLinkedList<KThread> SuggestedQueue(int prio, int core)
        {
            return _suggestedThreadsPerPrioPerCore[prio][core];
        }

        private RyujinxLinkedList<KThread> ScheduledQueue(int prio, int core)
        {
            return _scheduledThreadsPerPrioPerCore[prio][core];
        }
    }
}
