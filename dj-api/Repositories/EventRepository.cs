using dj_api.Data;
using dj_api.Models;
using MongoDB.Driver;
using QRCoder;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using dj_api.ApiModels.Event.Get;
using dj_api.ApiModels.Event.Post;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Reflection.Metadata.Ecma335;

namespace dj_api.Repositories
{
    public class EventRepository
    {
        private readonly IMongoCollection<Event> _eventsCollection;
        private readonly IMongoCollection<Song> _songsCollection;
        private readonly IMemoryCache _memoryCache;
        private readonly MemoryCacheEntryOptions _cacheEntryOptions = new()
        {
            SlidingExpiration = TimeSpan.FromMinutes(30)
        };

        private static HashSet<string> _paginatedCacheKeys = new HashSet<string>();

        public EventRepository(MongoDbContext dbContext, IMemoryCache memoryCache)
        {
            _eventsCollection = dbContext.GetCollection<Event>("DJEvent");
            _songsCollection = dbContext.GetCollection<Song>("Songs");
            _memoryCache = memoryCache;

        }

        public async Task<List<Event>> GetAllEventsAsync()
        {
            const string cacheKey = "all_events";

            if (!_memoryCache.TryGetValue(cacheKey, out List<Event>? cachedEvents))
            {
                cachedEvents = await _eventsCollection.Find(_ => true).ToListAsync();
                _memoryCache.Set(cacheKey, cachedEvents, _cacheEntryOptions);
            }

            return cachedEvents ?? new List<Event>();
        }

        public async Task<Event> GetEventByIdAsync(string id)
        {
            string cacheKey = $"event_{id}";

            if (!_memoryCache.TryGetValue(cacheKey, out Event? cachedEvent))
            {
                cachedEvent = await _eventsCollection.Find(e => e.ObjectId == id).FirstOrDefaultAsync();
                if (cachedEvent != null)
                {
                    _memoryCache.Set(cacheKey, cachedEvent, _cacheEntryOptions);
                }
            }

            return cachedEvent ?? throw new Exception($"Event with ID {id} not found");
        }

        public async Task CreateEventAsync(Event eventy)
        {
            var existing = await _eventsCollection.Find(e => e.ObjectId == eventy.ObjectId).FirstOrDefaultAsync();
            if (existing != null)
                throw new Exception($"Event with ID {eventy.ObjectId} already exists");

            await _eventsCollection.InsertOneAsync(eventy);

            _memoryCache.Remove("all_events");
            RemovePaginatedEventsCache();
        }

        public async Task DeleteEventAsync(string id)
        {
            await _eventsCollection.DeleteOneAsync(e => e.ObjectId == id);

            _memoryCache.Remove($"event_{id}");
            _memoryCache.Remove("all_events");
            RemovePaginatedEventsCache();
        }

        public async Task<bool> UpdateEventAsync(string id, Event updatedEvent)
        {
            var updateResult = await _eventsCollection.ReplaceOneAsync(e => e.ObjectId == id, updatedEvent);
            bool success = updateResult.ModifiedCount > 0;

            if (success)
            {
                _memoryCache.Set($"event_{id}", updatedEvent, _cacheEntryOptions);
                _memoryCache.Remove("all_events");
                RemovePaginatedEventsCache();
            }

            return success;
        }

        private void RemovePaginatedEventsCache()
        {
            foreach (var cacheKey in _paginatedCacheKeys)
            {
                _memoryCache.Remove(cacheKey);
            }
            _paginatedCacheKeys.Clear();
        }

        public async Task<byte[]> GenerateQRCode(string EventId)
        {
            byte[] qrCodeImg;
            Event eventy = await _eventsCollection.Find(e => e.ObjectId == EventId).FirstOrDefaultAsync();
            if (eventy == null)
                throw new Exception($"Event with ID {EventId} does not exist");

            using (var qrGenerator = new QRCodeGenerator())
            using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(eventy.QRCodeText, QRCodeGenerator.ECCLevel.Q))
            using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
            {
                qrCodeImg = qrCode.GetGraphic(20);
            }

            return qrCodeImg; // vrni QR kodo
        }

        public async Task<List<EventGet>> GetPaginatedEventsAsync(int page, int pageSize)
        {
            string cacheKey = $"paginated_events_page_{page}_size_{pageSize}";

            _paginatedCacheKeys.Add(cacheKey);

            if (!_memoryCache.TryGetValue(cacheKey, out List<EventGet>? cachedEvents))
            {
                var projection = Builders<Event>.Projection
                    .Include(e => e.ObjectId)
                    .Include(e => e.QRCodeText)
                    .Include(e => e.DJId)
                    .Include(e => e.Name)
                    .Include(e => e.Description)
                    .Include(e => e.Date)
                    .Include(e => e.Location)
                    .Include(e => e.Active);

                cachedEvents = await _eventsCollection
                    .Find(_ => true)
                    .Project<EventGet>(projection)
                    .Skip((page - 1) * pageSize)
                    .Limit(pageSize)
                    .ToListAsync();

                cachedEvents ??= new List<EventGet>();

                if (cachedEvents.Count > 0)
                {
                    _memoryCache.Set(cacheKey, cachedEvents, _cacheEntryOptions);
                }
            }

            return cachedEvents ?? new List<EventGet>();
        }

        public async Task<bool> AddSongToEventAsync(string eventId, Song song, string userId)
        {
            var eventFilter = Builders<Event>.Filter.Eq(e => e.Id, eventId);
            var eventy = await _eventsCollection.Find(eventFilter).FirstOrDefaultAsync();

            if (eventy == null)
            {
                throw new Exception("Event not found.");
            }

            if (eventy.MusicConfig.MusicPlaylist.Any(m =>
                string.Equals(m.MusicName, song.Name, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(m.MusicArtist, song.Artist, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            eventy.MusicConfig.MusicPlaylist.Add(new MusicData
            {
                ObjectId = song.ObjectId,
                MusicName = song.Name,

                Visible = true,
                Votes = 1,
                VotersIDs = new List<string> { userId },
                IsUserRecommendation = true,
                RecommenderID = userId
            });

            var update = Builders<Event>.Update.Set(e => e.MusicConfig, eventy.MusicConfig);
            await _eventsCollection.UpdateOneAsync(eventFilter, update);

            _memoryCache.Set($"event_{eventId}", eventy, _cacheEntryOptions);

            return true;
        }

        public async Task<List<Song>> GetSilimarSongsToEvent(string eventId)
        {
            var eventy = await _eventsCollection.Find(e => e.ObjectId == eventId).FirstOrDefaultAsync();

            if (eventy == null)
            {
                return null;
            }
            var playlist = eventy.MusicConfig.MusicPlaylist.ToList();

            if (playlist.Count == 0) //če je playlist prazen, vrni 10 random pesmi
            {
                var similarSongs = await _songsCollection
                   .Find(_ => true)
                   .Limit(10)
                   .ToListAsync();

                return similarSongs;
            }

            var genreCount = new Dictionary<string, int>();

            foreach (var song in playlist)
            {
                if (song.MusicGenre == null) //če je music genre prazen preskočimo
                {
                    continue; 
                }

                if (!genreCount.ContainsKey(song.MusicGenre))
                {
                    genreCount.Add(song.MusicGenre, 1);
                }
                else
                {
                    genreCount[song.MusicGenre]++;
                }
            }

            var leadGenre = "";
            if (genreCount.Count > 0)
            {
                leadGenre = genreCount.OrderByDescending(x => x.Value).First().Key;
                var playlistSongNames = playlist.Select(s => s.MusicName).ToList();
                var similarSongs = await _songsCollection
                   .Find(s => s.Genre == leadGenre && !playlistSongNames.Contains(s.Name))
                   .Limit(10)
                   .ToListAsync();

                return similarSongs; //vrni 10 pesmi iz najbolj popularnega žanra eventa
            }
            else
            {
                var similarSongs = await _songsCollection
                   .Find(_ => true)
                   .Limit(10)
                   .ToListAsync();
                return similarSongs; //če ni žanrov, vrni 10 random pesmi
            }

        }
    }
}
