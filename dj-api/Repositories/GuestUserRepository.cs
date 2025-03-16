using dj_api.Data;
using dj_api.Models;
using MongoDB.Driver;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace dj_api.Repositories
{
    public class GuestUserRepository
    {
        private readonly IMongoCollection<GuestUser> _guestUsersCollection;
        private readonly IMemoryCache _memoryCache;
        private readonly MemoryCacheEntryOptions _cacheEntryOptions = new()
        {
            SlidingExpiration = TimeSpan.FromMinutes(30) // Cache expiration policy
        };

        private static HashSet<string> _paginatedCacheKeys = new HashSet<string>();

        public GuestUserRepository(MongoDbContext dbContext, IMemoryCache memoryCache)
        {
            _guestUsersCollection = dbContext.GetCollection<GuestUser>("GuestUser");
            _memoryCache = memoryCache;
        }

        public async Task<List<GuestUser>> GetAllUsersAsync()
        {
            const string cacheKey = "all_guest_users";
            if (!_memoryCache.TryGetValue(cacheKey, out List<GuestUser>? cachedUsers))
            {
                cachedUsers = await _guestUsersCollection.Find(_ => true).ToListAsync();
                _memoryCache.Set(cacheKey, cachedUsers, _cacheEntryOptions);
            }
            return cachedUsers ?? new List<GuestUser>();
        }

        public async Task<GuestUser> GetUserByIdAsync(string id)
        {
            string cacheKey = $"guest_user_{id}";
            if (!_memoryCache.TryGetValue(cacheKey, out GuestUser? cachedUser))
            {
                cachedUser = await _guestUsersCollection.Find(user => user.ObjectId == id).FirstOrDefaultAsync();
                if (cachedUser != null)
                {
                    _memoryCache.Set(cacheKey, cachedUser, _cacheEntryOptions);
                }
            }
            return cachedUser ?? throw new Exception($"User with ID {id} not found");
        }

        public async Task CreateUserAsync(GuestUser user)
        {
            var existing = await _guestUsersCollection.Find(u => u.Id == user.Id).FirstOrDefaultAsync();
            if (existing != null)
                throw new Exception($"User with ID {user.Id} already exists");

            await _guestUsersCollection.InsertOneAsync(user);

           
            _memoryCache.Remove("all_guest_users");
            RemovePaginatedGuestUserCache();
        }

        public async Task DeleteUserAsync(string id)
        {
           

            await _guestUsersCollection.DeleteOneAsync(u => u.ObjectId ==id);

            _memoryCache.Remove($"guest_user_{id}");
            _memoryCache.Remove("all_guest_users");
            RemovePaginatedGuestUserCache();
        }

        public async Task UpdateUserAsync(string id, GuestUser user)
        {

            await _guestUsersCollection.ReplaceOneAsync(u => u.ObjectId == id, user);
        
            _memoryCache.Remove($"guest_user_{id}");
            _memoryCache.Remove("all_guest_users");
            RemovePaginatedGuestUserCache();
        }
        private void RemovePaginatedGuestUserCache()
        {

            foreach (var cacheKey in _paginatedCacheKeys)
            {
                _memoryCache.Remove(cacheKey);
            }
 
            _paginatedCacheKeys.Clear();
        }

        public async Task<List<GuestUser>> GetPaginatedUserAsync(int page, int pageSize)
        {
            string cacheKey = $"paginated_guest_users_page_{page}_size_{pageSize}";

            _paginatedCacheKeys.Add(cacheKey);

            if (!_memoryCache.TryGetValue(cacheKey, out List<GuestUser>? cachedUsers))
            {
                cachedUsers = await _guestUsersCollection
                    .Find(_ => true)
                    .Skip((page - 1) * pageSize)
                    .Limit(pageSize)
                    .ToListAsync();

           
                cachedUsers ??= new List<GuestUser>();

         
                if (cachedUsers.Any())
                {
                    _memoryCache.Set(cacheKey, cachedUsers, _cacheEntryOptions);
                }
            }

            return cachedUsers ?? new List<GuestUser>();
        }

    }
}
