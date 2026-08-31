using System.Threading;

namespace GuacamoleClient.Common.Updates
{
    internal sealed class AutomaticUpdateCheckSession
    {
        private int _started;

        internal bool TryStart()
            => Interlocked.CompareExchange(ref _started, 1, 0) == 0;
    }
}
