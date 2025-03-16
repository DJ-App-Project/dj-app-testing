using dj_api.Data;
using dj_api.Models;
using MongoDB.Driver;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace dj_api.Repositories
{
    public class SongRepository
    {
        private readonly IMongoCollection<Song> _songsCollection;
        private readonly IMemoryCache _memoryCache;
        private readonly MemoryCacheEntryOptions _cacheEntryOptions = new()
        {
            SlidingExpiration = TimeSpan.FromMinutes(30)
        };

        private static HashSet<string> _paginatedCacheKeys = new HashSet<string>();

        public SongRepository(MongoDbContext dbContext, IMemoryCache memoryCache)
        {
            _songsCollection = dbContext.GetCollection<Song>("Songs");
            _memoryCache = memoryCache;
        }

        public async Task<Song> GetSongByIdAsync(string objectId)
        {
            string cacheKey = $"song_{objectId}";

            if (!_memoryCache.TryGetValue(cacheKey, out Song? cachedSong))
            {
                cachedSong = await _songsCollection.Find(song => song.ObjectId == objectId).FirstOrDefaultAsync();
                if (cachedSong != null)
                {
                    _memoryCache.Set(cacheKey, cachedSong, _cacheEntryOptions);
                }
            }

            return cachedSong ?? throw new Exception($"Song with ID {objectId} not found.");
        }
      
        public async Task<List<Song>> GetAllSongsAsync()
        {
            const string cacheKey = "all_songs";

            if (!_memoryCache.TryGetValue(cacheKey, out List<Song>? cachedSongs))
            {
                cachedSongs = await _songsCollection.Find(_ => true).ToListAsync();
                _memoryCache.Set(cacheKey, cachedSongs, _cacheEntryOptions);
            }

            return cachedSongs ?? new List<Song>();
        }

        public async Task<List<Song>> GetPaginatedSongsAsync(int page, int pageSize)
        {
            string cacheKey = $"paginated_songs_page_{page}_size_{pageSize}";
            _paginatedCacheKeys.Add(cacheKey);

            if (!_memoryCache.TryGetValue(cacheKey, out List<Song>? cachedSongs))
            {
                cachedSongs = await _songsCollection
                    .Find(_ => true)
                    .Skip((page - 1) * pageSize)
                    .Limit(pageSize)
                    .ToListAsync();

                if (cachedSongs != null && cachedSongs.Any())
                {
                    _memoryCache.Set(cacheKey, cachedSongs, _cacheEntryOptions);
                }
            }

            return cachedSongs ?? new List<Song>();
        }

        public async Task CreateSongAsync(Song newSong)
        {
            var existing = await _songsCollection.Find(s => s.ObjectId == newSong.ObjectId).FirstOrDefaultAsync();
            if (existing != null)
                throw new Exception($"Song with ID {newSong.ObjectId} already exists");

            await _songsCollection.InsertOneAsync(newSong);

            _memoryCache.Remove("all_songs");
            RemovePaginatedSongsCache();
        }

        public async Task UpdateSongAsync(string ObjectId, Song updatedSong)
        {
            await _songsCollection.ReplaceOneAsync(s => s.ObjectId == ObjectId, updatedSong);
            _memoryCache.Remove($"song_{ObjectId}");
            _memoryCache.Remove("all_songs");
            RemovePaginatedSongsCache();
        }

        public async Task DeleteSongAsync(string ObjectId)
        {
            await _songsCollection.DeleteOneAsync(song => song.ObjectId == ObjectId);
            _memoryCache.Remove($"song_{ObjectId}");
            _memoryCache.Remove("all_songs");
            RemovePaginatedSongsCache();
        }

        public async Task<List<Song>> FindSongsByArtistAsync(string artist)
        {
            return await _songsCollection.Find(song => song.Artist.ToLower() == artist.ToLower()).ToListAsync();
        }

        private void RemovePaginatedSongsCache()
        {
            foreach (var cacheKey in _paginatedCacheKeys)
            {
                _memoryCache.Remove(cacheKey);
            }
            _paginatedCacheKeys.Clear();
        }
        public async Task<Song?> FindSongByTitleAsync(string title)
        {
            return await _songsCollection.Find(song => song.Name.ToLower() == title.ToLower()).FirstOrDefaultAsync();
        }

       
    }
}
