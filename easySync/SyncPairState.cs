
namespace easySync
{
    public enum SyncPairState
    {
        Unknown,
        NotReady,
        Initializing,
        NotInitialized,
        Ready,
        Synchronizing,
        BackingUp,
        Completed,
        Paused,
        Failure
    }
}
