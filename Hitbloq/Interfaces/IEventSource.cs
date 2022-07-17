using System.Threading;
using System.Threading.Tasks;
using Hitbloq.Entries;

namespace Hitbloq.Interfaces
{
    internal interface IEventSource
    {
        public Task<HitbloqEvent?> GetAsync(CancellationToken cancellationToken = default);
    }
}