using System.Collections.Generic;
using UnityEngine;

namespace MBaske
{
    public struct TimedQueueItem<T>
    {
        public T value;
        public float time;
    }

    public class TimedQueue<T>
    {
        public string Name { get; private set; }
        public int Length => queue.Count;
        public TimedQueueItem<T> First => queue.Peek();
        public TimedQueueItem<T> Last => latest;

        private Queue<TimedQueueItem<T>> queue;
        private TimedQueueItem<T> latest;
        private int capacity;

        public TimedQueue(int initCapacity, string name = "")
        {
            queue = new Queue<TimedQueueItem<T>>(initCapacity);
            capacity = initCapacity;
            Name = name;
        }

        public void Clear()
        {
            queue.Clear();
        }

        public void Add(T value, int newCapacity)
        {
            capacity = newCapacity;
            Add(value);
        }

        public void Add(T value)
        {
            latest = new TimedQueueItem<T>() { value = value, time = Time.time };
            queue.Enqueue(latest);
            Prune();
        }

        public IEnumerable<TimedQueueItem<T>> Items()
        {
            foreach (TimedQueueItem<T> item in queue)
            {
                yield return item;
            }
        }

        public IEnumerable<T> Values()
        {
            foreach (TimedQueueItem<T> item in queue)
            {
                yield return item.value;
            }
        }

        private void Prune()
        {
            while (queue.Count > capacity)
            {
                queue.Dequeue();
            }
        }
    }
}