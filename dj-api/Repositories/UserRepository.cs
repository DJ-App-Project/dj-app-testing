using dj_api.Data;
using dj_api.Models;
using MongoDB.Driver;
using Microsoft.Extensions.Caching.Memory;
using dj_api.ApiModels;

namespace dj_api.Repositories
{
    public class UserRepository
    {
        private readonly IMongoCollection<User> _usersCollection;
        private readonly IMemoryCache _memoryCache;
        private readonly MemoryCacheEntryOptions _cacheEntryOptions = new()
        {
            SlidingExpiration = TimeSpan.FromMinutes(30)
        };

        private static HashSet<string> _paginatedCacheKeys = new HashSet<string>();

        public UserRepository(MongoDbContext dbContext, IMemoryCache memoryCache)
        {
            _usersCollection = dbContext.GetCollection<User>("User");
            _memoryCache = memoryCache;
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            const string cacheKey = "all_users";
            if (!_memoryCache.TryGetValue(cacheKey, out List<User>? cachedUsers))
            {
                cachedUsers = await _usersCollection.Find(_ => true).ToListAsync();
                _memoryCache.Set(cacheKey, cachedUsers, _cacheEntryOptions);
            }
            return cachedUsers ?? new List<User>();
        }

        public async Task<User> GetUserByIdAsync(string id)
        {
            string cacheKey = $"user_{id}";
            if (!_memoryCache.TryGetValue(cacheKey, out User? cachedUser))
            {
                cachedUser = await _usersCollection.Find(user => user.ObjectId == id).FirstOrDefaultAsync();
                if (cachedUser != null)
                {
                    _memoryCache.Set(cacheKey, cachedUser, _cacheEntryOptions);
                }
            }
            return cachedUser!;
        }

        public async Task CreateUserAsync(User newUser)
        {
          
            if (_usersCollection.Find(user => user.Username == newUser.Username || user.Email == newUser.Email).Any())
                throw new Exception($"Username or email already in use");

           
            await _usersCollection.InsertOneAsync(newUser);

          
            _memoryCache.Remove("all_users");
            RemovePaginatedUserCache();
        }

        public async Task DeleteUserAsync(string id)
        {
            
            await _usersCollection.DeleteOneAsync(user => user.ObjectId == id);

          
            _memoryCache.Remove($"user_{id}");
            _memoryCache.Remove("all_users");
            RemovePaginatedUserCache();
        }

        public async Task UpdateUserAsync(string id, User user)
        {
            await _usersCollection.ReplaceOneAsync(u => u.ObjectId == id, user);

           
            _memoryCache.Remove($"user_{id}");
            _memoryCache.Remove("all_users");
            RemovePaginatedUserCache();
        }

        private void RemovePaginatedUserCache()
        {
         
            foreach (var cacheKey in _paginatedCacheKeys)
            {
                _memoryCache.Remove(cacheKey);
            }
          
            _paginatedCacheKeys.Clear();
        }

        public async Task<User?> Authenticate(string username, string password)
        {
            User? user = await _usersCollection.Find(u => u.Username == username).FirstOrDefaultAsync();
            
            if (user == null )
            {
                return null;
            }
            if(user.Password.Length >10) { //for strings
            if( BCrypt.Net.BCrypt.Verify(password, user.Password))
            {
                return user;
            }
            }
            if (password == user.Password)//for now this is for strings with dummy data
            {
                return user;
            }
            return null;
        }

        public async Task<User> FindUserByUsername(string username)
        {
            return await _usersCollection.Find(user => user.Username == username).FirstOrDefaultAsync();
        }

        public async Task<User> FindUserByEmail(string email)
        {
            return await _usersCollection.Find(user => user.Email == email).FirstOrDefaultAsync();
        }

        public async Task<List<User>> GetPaginatedUserAsync(int page, int pageSize)
        {
            string cacheKey = $"paginated_users_page_{page}_size_{pageSize}";

            _paginatedCacheKeys.Add(cacheKey);

            if (!_memoryCache.TryGetValue(cacheKey, out List<User>? cachedUsers))
            {
                cachedUsers = await _usersCollection
                    .Find(_ => true)
                    .Skip((page - 1) * pageSize)
                    .Limit(pageSize)
                    .ToListAsync();

                cachedUsers ??= new List<User>();
                if (cachedUsers.Any())
                {
                    _memoryCache.Set(cacheKey, cachedUsers, _cacheEntryOptions);
                }
            }

            return cachedUsers;
        }

    }
}
