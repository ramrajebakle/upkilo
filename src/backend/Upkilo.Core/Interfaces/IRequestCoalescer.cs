using System;
using System.Threading.Tasks;

namespace Upkilo.Core.Interfaces;

public interface IRequestCoalescer
{
    /// <summary>
    /// Executes a factory method only once for concurrent callers using the same key.
    /// Subsequent callers wait for the original task and receive the same result.
    /// </summary>
    Task<T> ExecuteAsync<T>(string key, Func<Task<T>> factory);
}
