using StackExchange.Redis;
using System;
using System.Threading.Tasks;
using Upkilo.Core.Interfaces;

namespace Upkilo.Infrastructure.Resilience;

public class RedisLockProvider : IDistributedLockProvider
{
    private readonly IConnectionMultiplexer _redis;
    private readonly string _lockInstanceId = Guid.NewGuid().ToString();

    public RedisLockProvider(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<IDisposable?> AcquireLockAsync(string resource, TimeSpan expiration, TimeSpan? wait = null, TimeSpan? retry = null)
    {
        var db = _redis.GetDatabase();
        var lockKey = $"lock:{resource}";

        var waitTime = wait ?? TimeSpan.Zero;
        var retryInterval = retry ?? TimeSpan.FromMilliseconds(100);
        var startTime = DateTime.UtcNow;

        do
        {
            if (await db.LockTakeAsync(lockKey, _lockInstanceId, expiration))
            {
                return new RedisLockHandle(db, lockKey, _lockInstanceId);
            }

            if (waitTime == TimeSpan.Zero) break;

            await Task.Delay(retryInterval);

        } while (DateTime.UtcNow - startTime < waitTime);

        return null;
    }

    private class RedisLockHandle : IDisposable
    {
        private readonly IDatabase _db;
        private readonly string _key;
        private readonly string _value;
        private bool _disposed;

        public RedisLockHandle(IDatabase db, string key, string value)
        {
            _db = db;
            _key = key;
            _value = value;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _db.LockRelease(_key, _value);
                _disposed = true;
            }
        }
    }
}
