using System;
using System.Collections.Generic;

namespace FilesHash.Common
{
    internal sealed class SynchronizedQueue<T>
    {
        private readonly Object locker = new object();
        private readonly Queue<T> queue = new Queue<T>();

        public int Count { get { return queue.Count; } }

        public void Enqueue(T item)
        {
            lock (locker)
            {
                queue.Enqueue(item);
            }
        }

        public T Dequeue()
        {
            T item;

            lock (locker)
            {
                item = queue.Dequeue();
            }

            return item;
        }

    }
}
