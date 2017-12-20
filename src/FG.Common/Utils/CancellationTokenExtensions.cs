using System.Threading;

namespace FG.Common.Utils
{
    public static class CancellationTokenExtensions
    {
        public static CancellationToken OrNone(this CancellationToken cancellationToken)
        {
            return cancellationToken == default(CancellationToken) ? CancellationToken.None : cancellationToken;
        }
    }
}