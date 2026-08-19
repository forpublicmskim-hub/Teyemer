namespace Teyemer.App;

internal sealed class SingleInstanceGuard : IDisposable
{
    private readonly Mutex _mutex;
    private bool _ownsMutex;

    private SingleInstanceGuard(Mutex mutex)
    {
        _mutex = mutex;
        _ownsMutex = true;
    }

    public static SingleInstanceGuard? TryAcquire(string mutexName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mutexName);
        var mutex = new Mutex(false, mutexName);
        try
        {
            if (!mutex.WaitOne(0))
            {
                mutex.Dispose();
                return null;
            }
        }
        catch (AbandonedMutexException)
        {
            // The previous process ended without releasing the mutex.
            // Ownership is granted to this process, so startup can continue safely.
        }

        return new SingleInstanceGuard(mutex);
    }

    public void Dispose()
    {
        if (!_ownsMutex) return;
        _ownsMutex = false;
        try { _mutex.ReleaseMutex(); }
        finally { _mutex.Dispose(); }
    }
}
