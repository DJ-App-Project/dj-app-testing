using dj_api.ApiModels;
using dj_api.Data;
using dj_api.Models;
using dj_api.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace xUnitTestDjApi
{
    public class UserControllerTest
    {
        private readonly Mock<IMongoCollection<User>> _mockCollection;
        private readonly Mock<IMongoDatabase> _mockDatabase;
        private readonly Mock<IMongoClient> _mockClient;
        private readonly IMemoryCache _memoryCache;
        private readonly MongoDbContext _dbContext;
        private readonly UserRepository _userRepository;
        private readonly Mock<UserRepository> _mockUserRepository;
        private readonly UserController _controller;
        private readonly Mock<HttpContext> _mockHttpContext;

        public UserControllerTest()
        {
            _mockCollection = new Mock<IMongoCollection<User>>();
            _mockDatabase = new Mock<IMongoDatabase>();
            _mockClient = new Mock<IMongoClient>();
            _mockUserRepository = new Mock<UserRepository>();
            _memoryCache = new MemoryCache(new MemoryCacheOptions());

            var inMemorySettings = new Dictionary<string, string>
            {
                { "ConnectionStrings:DbConnection", "mongodb://djadmin:DJsuggester2025!@mongodbitk.duckdns.org:27017" }
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            _dbContext = new MongoDbContext(configuration);

            _mockDatabase.Setup(db => db.GetCollection<User>("DJUser", null))
                         .Returns(_mockCollection.Object);

            _userRepository = new UserRepository(_dbContext, _memoryCache);

            _controller = new UserController(_userRepository);

            _mockHttpContext = new Mock<HttpContext>();
            _mockHttpContext.Setup(m => m.Request.Headers["Authorization"]).Returns("Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjEyMzQ1IiwiaHR0cDovL3NjaGVtYXMueG1sc29hcC5vcmcvd3MvMjAwNS8wNS9pZGVudGl0eS9jbGFpbXMvbmFtZSI6IlRlc3QgVXNlciIsImV4cCI6MTc0MTkzNTY1MCwiaXNzIjoiZGpfYXBpIiwiYXVkIjoiZGpfdXNlcnMifQ.2UPdlw_szWJwNRGdcKStP4i53uNOY_92P6fWVpYf14k");
            _controller.ControllerContext.HttpContext = _mockHttpContext.Object;
        }

        [Fact]
        public async Task GetAllUsersAsync_CacheHit_ReturnsCachedUsers()
        {
            var cacheKey = "all_users";
            var cachedUsers = new List<User>
            {
                new User { ObjectId = "1", Username = "user1" },
                new User { ObjectId = "2", Username = "user2" }
            };

            _memoryCache.Set(cacheKey, cachedUsers);

            var result = await _userRepository.GetAllUsersAsync();

            Assert.NotNull(result);
            Assert.Equal(cachedUsers.Count, result.Count);
            Assert.Equal(cachedUsers[0].ObjectId, result[0].ObjectId);
        }

        [Fact]
        public async Task GetUserByIdAsync_CacheHit_ReturnsCachedUser()
        {
            string userId = "67d19c50f64702d730b8f6aa";
            var cachedUser = new User { ObjectId = userId };
            _memoryCache.Set($"user_{userId}", cachedUser);

            var result = await _controller.GetUserById(userId);
            var resultObject = result as OkObjectResult;

            Assert.Equal(200, resultObject.StatusCode);
        }

        [Fact]
        public async Task GetUserByIdAsync_DatabaseHit_ReturnsUser()
        {
            string userId = "67d19c50f64702d730b8f6aa";
            var cachedUser = new User { ObjectId = userId };

            var usersCollection = new List<User> { cachedUser };

            Func<string, Task<User>> findUserById = (id) =>
            {
                return Task.FromResult(usersCollection.FirstOrDefault(e => e.ObjectId == id));
            };

            var userRepository = new UserRepository(_dbContext, _memoryCache);

            var controller = new UserController(userRepository);

            var result = await controller.GetUserById(userId);
            var resultObject = result as OkObjectResult;
            var userObj = resultObject.Value as User;

            Assert.NotNull(result);
            Assert.Equal(userId, userObj.ObjectId);
        }

        [Fact]
        public async Task CreateUser_UserDoesNotExist_InsertsNewUser()
        {
            string userId = "1";
            var newUser = new User { ObjectId = userId, Username = "user1", Email = "user1@example.com" };

            await _controller.CreateUser(new UserModel
            {
                username = newUser.Username,
                email = newUser.Email,
                name = newUser.Name,
                familyName = newUser.FamilyName,
                imageUrl = newUser.ImageUrl,
                password = newUser.Password
            });

            var createdUser = await _dbContext.GetCollection<User>("DJUser")
                                               .Find(e => e.ObjectId == userId)
                                               .FirstOrDefaultAsync();
            Assert.NotNull(createdUser);
            Assert.Equal(userId, createdUser.ObjectId);
        }

        [Fact]
        public async Task CreateUser_UserAlreadyExists_ReturnsConflict()
        {
            var user = new UserModel { username = "user3", email = "user3@example.com" };

            _mockUserRepository.Setup(repo => repo.CreateUserAsync(It.IsAny<User>()))
                           .ThrowsAsync(new Exception($"User with email {user.email} already exists"));

            var result = await _controller.CreateUser(user);

            var conflictResult = Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal($"User with email {user.email} already exists", conflictResult.Value);
        }

        [Fact]
        public async Task DeleteUserAsync_UserExists_ReturnsOk()
        {
            string userId = "67d19c50f64702d730b8f6aa";

            _mockUserRepository.Setup(repo => repo.DeleteUserAsync(userId))
                           .Returns(Task.CompletedTask);

            var result = await _controller.DeleteUser(userId);

            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task DeleteUserAsync_DeleteFailed_ReturnsBadRequest()
        {
            string userId = "67d19c50f64702d730b8f6aa";

            _mockUserRepository.Setup(repo => repo.DeleteUserAsync(userId))
                           .ThrowsAsync(new Exception($"User with ID {userId} does not exist"));

            var result = await _controller.DeleteUser(userId);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal($"User with ID {userId} does not exist", badRequestResult.Value);
        }

        [Fact]
        public async Task UpdateUser_Successful_ReturnsOk()
        {
            var userId = "4";
            var existingUser = new User { ObjectId = userId, Username = "user4", Email = "user4@example.com" };
            var updatedUser = new UserModel { username = "updatedUser4", email = "updateduser4@example.com" };

            _mockUserRepository.Setup(repo => repo.GetUserByIdAsync(userId)).ReturnsAsync(existingUser);
            _mockUserRepository.Setup(repo => repo.UpdateUserAsync(userId, It.IsAny<User>())).Returns(Task.CompletedTask);

            var result = await _controller.UpdateUser(userId, updatedUser);

            var actionResult = Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task GetAllUsersPage_InvalidPageOrPageSize_ReturnsBadRequest()
        {
            var result1 = await _controller.GetAllUserPage(-1, 10);
            var result2 = await _controller.GetAllUserPage(1, -10);

            Assert.IsType<BadRequestObjectResult>(result1);
            Assert.IsType<BadRequestObjectResult>(result2);
        }

        [Fact]
        public async Task GetAllUsersPage_ValidPageAndPageSize_ReturnsUsers()
        {
            var users = new List<User>
            {
                new User { ObjectId = "1", Username = "user1" },
                new User { ObjectId = "2", Username = "user2" }
            };

            _mockUserRepository.Setup(repo => repo.GetPaginatedUserAsync(1, 10)).ReturnsAsync(users);

            var result = await _controller.GetAllUserPage(1, 10);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedUsers = Assert.IsType<List<User>>(okResult.Value);

            Assert.Equal(users.Count, returnedUsers.Count);
        }

        [Fact]
        public async Task GetAllUsersPage_NoUsersFound_ReturnsNotFound()
        {
            _mockUserRepository.Setup(repo => repo.GetPaginatedUserAsync(1, 10)).ReturnsAsync(new List<User>());

            var result = await _controller.GetAllUserPage(1, 10);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("No Users found.", notFoundResult.Value);
        }
    }
}