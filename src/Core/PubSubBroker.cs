using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;

namespace codecrafters_redis.src.Core
{
    public class PubSubBroker
    {
        private readonly ConcurrentDictionary<string, List<NetworkStream>> _subscribers = new();

        public void Subscribe(string channel, NetworkStream stream)
        {
            var list = _subscribers.GetOrAdd(channel, _ => new List<NetworkStream>());
            lock (list) { list.Add(stream); }
        }

        public int Unsubscribe(string channel, NetworkStream stream)
        {
            if (!_subscribers.TryGetValue(channel, out var list)) return 0;
            lock (list)
            {
                list.Remove(stream);
                return list.Count;
            }
        }

        public List<NetworkStream> Publish(string channel)
        {
            if (!_subscribers.TryGetValue(channel, out var list)) return new List<NetworkStream>();
            lock (list)
            {
                return new List<NetworkStream>(list);
            }
        }
    }
}
