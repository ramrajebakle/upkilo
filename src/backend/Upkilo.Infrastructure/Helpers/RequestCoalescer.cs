using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Upkilo.Core.Interfaces;

namespace Upkilo.Infrastructure.Helpers;

public class RequestCoalescer : IRequestCoalescer
{
    private readonly ConcurrentDictionary<string, Lazy<Task<object>>> _tasks = new();

    public async Task<T> ExecuteAsync<T>(string key, Func<Task<T>> factory)
    {
        var lazyTask = _tasks.GetOrAdd(key, k => new Lazy<Task<object>>(async () =>
        {
            try
            {
                var result = await factory();
                return result!;
            }
            finally
            {
                _tasks.TryRemove(k, out _);
            }
        }));

        var rawResult = await lazyTask.Value;
        return (T)rawResult;
    }
}
