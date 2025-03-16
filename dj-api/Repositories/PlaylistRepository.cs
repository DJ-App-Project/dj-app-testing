using dj_api.Data;
using dj_api.Models;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Driver;

namespace dj_api.Repositories
{
    public class PlaylistRepository
    {
        private readonly IMongoCollection<Playlist> _playlistCollection;
        private readonly IMemoryCache _memoryCache;
        private readonly MemoryCacheEntryOptions _cacheEntryOptions = new()
        {
            SlidingExpiration = TimeSpan.FromMinutes(30) 
        };
        public PlaylistRepository(MongoDbContext dbContext, IMemoryCache memoryCache)
        {
            _playlistCollection = dbContext.GetCollection<Playlist>("Playlist");
            _memoryCache = memoryCache;
        }
        public async Task<List<Playlist>> GetAllPlaylist()
        {
            const string cacheKey = "all_events";

            if (!_memoryCache.TryGetValue(cacheKey, out List<Playlist>? cachedEvents))
            {
                cachedEvents = await _playlistCollection.Find(_ => true).ToListAsync();
                _memoryCache.Set(cacheKey, cachedEvents, _cacheEntryOptions);
            }

            return cachedEvents ?? new List<Playlist>();
        }

    }
}
