using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go.Tests.Integration.Utils
{
    public class FakeMemoryCache : IMemoryCache
    {
        private readonly Dictionary<object, object> _cache = new();

        public ICacheEntry CreateEntry(object key)
        {
            var entry = new MemoryCacheEntry(key, value => _cache[key] = value);
            return entry;
        }

        public void Dispose() { }

        public void Remove(object key) => _cache.Remove(key);

        public bool TryGetValue(object key, out object value) => _cache.TryGetValue(key, out value);

        private class MemoryCacheEntry : ICacheEntry
        {
            private readonly Action<object> _setValue;
            public object Key { get; }
            public object? Value { get; set; }
            public MemoryCacheEntry(object key, Action<object> setValue)
            {
                Key = key;
                _setValue = setValue;
            }
            public void Dispose() => _setValue(Value!);
            public DateTimeOffset? AbsoluteExpiration { get; set; }
            public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }
            public TimeSpan? SlidingExpiration { get; set; }
            public IList<IChangeToken> ExpirationTokens => new List<IChangeToken>();
            public IList<PostEvictionCallbackRegistration> PostEvictionCallbacks => new List<PostEvictionCallbackRegistration>();
            public CacheItemPriority Priority { get; set; }
            public long? Size { get; set; }
        }
    }
}
