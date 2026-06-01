using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using codecrafters_redis.src.Redis;

namespace codecrafters_redis.src.Core
{
    public class DatabaseStorage
    {
        private readonly ConcurrentDictionary<string, RedisValue> _store = new();
        private readonly ConcurrentDictionary<string, string> _configs = new();
        private readonly ConcurrentDictionary<string, long> _keyVersions = new();
        private readonly ConcurrentDictionary<string, List<string>> _userPasswords = new();
        private readonly ConcurrentDictionary<string, SortedSet<(double score, string value)>> _zadd = new();
        private readonly ConcurrentDictionary<string, List<TaskCompletionSource<bool>>> _streamWaiters = new();
        private readonly ConcurrentDictionary<string, Queue<TaskCompletionSource<string>>> _waiters = new();

        public DatabaseStorage()
        {
            SetConfig("dir", System.IO.Directory.GetCurrentDirectory());
            SetConfig("appendonly", "no");
            SetConfig("appenddirname", "appendonlydir");
            SetConfig("appendfilename", "appendonly.aof");
            SetConfig("appendfsync", "everysec");
        }

        public ConcurrentDictionary<string, RedisValue> RawStore => _store;
        public ConcurrentDictionary<string, SortedSet<(double score, string value)>> ZAddStore => _zadd;
        public ConcurrentDictionary<string, List<TaskCompletionSource<bool>>> StreamWaiters => _streamWaiters;
        public ConcurrentDictionary<string, Queue<TaskCompletionSource<string>>> Waiters => _waiters;

        public void IncrementVersion(string key)
        {
            _keyVersions.AddOrUpdate(key, 1, (_, v) => v + 1);
        }

        public long GetVersion(string key)
        {
            return _keyVersions.TryGetValue(key, out var version) ? version : 0;
        }

        public bool Set(string key, string value, DateTime? expiresAt = null)
        {
            IncrementVersion(key);
            _store[key] = new RedisString { Value = value, ExpiresAt = expiresAt };
            return true;
        }

        public string? Get(string key)
        {
            if (!_store.TryGetValue(key, out var entry)) return null;

            if (entry.IsExpired)
            {
                _store.TryRemove(key, out _);
                return null;
            }

            return (entry as RedisString)?.Value;
        }

        public T GetOrCreate<T>(string key) where T : RedisValue, new()
        {
            return (T)_store.GetOrAdd(key, _ => new T());
        }

        public (bool, int) Incr(string key)
        {
            var entry = GetOrCreate<RedisString>(key);

            if (entry.Value == null)
            {
                IncrementVersion(key);
                entry.Value = "1";
                return (true, 1);
            }

            int number = 0;
            var success = int.TryParse(entry.Value, out number);
            if (success)
            {
                IncrementVersion(key);
                number++;
                entry.Value = number.ToString();
            }

            return (success, number);
        }

        public void SetConfig(string key, string value)
        {
            _configs[key] = value;
        }

        public string? GetConfig(string key)
        {
            return _configs.TryGetValue(key, out var value) ? value : null;
        }

        public List<string> Keys(string key)
        {
            var pattern = "^" + System.Text.RegularExpressions.Regex.Escape(key).Replace("\\*", ".*") + "$";
            var regex = new System.Text.RegularExpressions.Regex(pattern);
            return _store.Keys.Where(k => regex.IsMatch(k)).ToList();
        }

        public List<string> GetUserPasswords(string username)
        {
            return _userPasswords.GetOrAdd(username, _ => new List<string>());
        }

        public void AddUserPassword(string username, string passwordHash)
        {
            var passwords = _userPasswords.GetOrAdd(username, _ => new List<string>());
            lock (passwords)
            {
                if (!passwords.Contains(passwordHash))
                {
                    passwords.Add(passwordHash);
                }
            }
        }

        public void ClearUserPasswords(string username)
        {
            if (_userPasswords.TryGetValue(username, out var passwords))
            {
                lock (passwords)
                {
                    passwords.Clear();
                }
            }
        }

        public bool IsValidPassword(string username, string passwordHash)
        {
            var passwords = GetUserPasswords(username);
            return passwords.Contains(passwordHash);
        }
    }
}
