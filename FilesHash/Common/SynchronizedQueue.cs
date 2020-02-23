using System;
using System.Collections.Generic;
using System.Threading;

namespace FilesHash.Common
{
    internal sealed class SynchronizedQueue<T>
    {
        private readonly Object locker = new object();
        private readonly Queue<T> queue = new Queue<T>();
        private bool stopEnqueue = false;

        public int Count { get { return queue.Count; } }

        public void StopEnqueue()
        {
            lock (locker)
            {
                stopEnqueue = true;
                Monitor.PulseAll(locker);
            }
        }

        public void Enqueue(T item)
        {
            lock(locker)
            {
                queue.Enqueue(item);
                Monitor.PulseAll(locker);
            }
        }

        public bool TryDequeue(out T item)
        {
            lock (locker)
            {
                while (queue.Count == 0)
                {
                    Monitor.Wait(locker);

                    if (stopEnqueue)
                    {
                        item = default;
                        return false;
                    }
                }

                item = queue.Dequeue();
                return true;
            }
        }

    }
}
