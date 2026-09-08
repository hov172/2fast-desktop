#if TWOFAST_DESKTOP
namespace Project2FA.Services;
internal sealed class AccountCommitQueue<T>
{
    private readonly SemaphoreSlim gate = new(1, 1);
    internal async Task<bool> Commit(T item, Func<T, bool> contains, Action<T> add, Action<T> remove, Func<Task<bool>> save)
    {
        await gate.WaitAsync();
        try
        {
            if (contains(item)) return false;
            add(item);
            bool committed = false;
            try { committed = await save(); return committed; }
            finally { if (!committed) remove(item); }
        }
        finally { gate.Release(); }
    }
}
#endif
