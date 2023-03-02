using System.Threading;
using System.Threading.Tasks;
using Hitbloq.Entries;
using Hitbloq.Interfaces;

namespace Hitbloq.Sources
{
    internal class DebugEventSource : IEventSource
    {
        public Task<HitbloqEvent?> GetAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new HitbloqEvent
            {
                ID = 1,
                Title = "Test Event",
                Image =
                    "https://static.wikia.nocookie.net/marvelmovies/images/b/b4/Milo_Morbius.jpg/revision/latest?cb=20220628170841",
                Description = "Milo",
                Pool = "midspeed_acc"
            })!;
        }
    }
}