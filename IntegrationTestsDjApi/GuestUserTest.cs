using dj_api.ApiModels;
using dj_api.Models;
using dj_api.Repositories;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

//Testi so bili izdelani in izvajani z strani Amar Tabaković znotraj te klase
namespace IntegrationTestsDjApi
{
    public class GuestUserTest : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public GuestUserTest(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetAllGuestUsersPage_ValidRequest_ReturnsPaginatedResults()
        {
            var tokenResponse = await _client.GetAsync("http://localhost:5152/api/auth/GenerateTestToken");
            tokenResponse.EnsureSuccessStatusCode();
            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
            var jsonObject = JsonConvert.DeserializeObject<dynamic>(tokenJson);
            var token = jsonObject.token.ToString();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.GetAsync("http://localhost:5152/api/GuestUsers?page=1&pageSize=5");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var guestUsersJson = await response.Content.ReadAsStringAsync();
            var guestUsers = JsonConvert.DeserializeObject<List<GuestUser>>(guestUsersJson);

            Assert.NotNull(guestUsers);
            Assert.True(guestUsers.Count <= 5, "Returned more users than requested page size");
        }

        [Fact]
        public async Task GetAllGuestUsersPage_InvalidRequest_ReturnsBadRequest()
        {
            var tokenResponse = await _client.GetAsync("http://localhost:5152/api/auth/GenerateTestToken");
            tokenResponse.EnsureSuccessStatusCode();
            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
            var jsonObject = JsonConvert.DeserializeObject<dynamic>(tokenJson);
            var token = jsonObject.token.ToString();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.GetAsync("http://localhost:5152/api/GuestUsers?page=0&pageSize=-5");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var errorMessage = await response.Content.ReadAsStringAsync();
            Assert.Contains("Page and pageSize must be greater than 0", errorMessage);
        }

        [Fact]
        public async Task CreateUser_BadRequest()
        {
            var tokenResponse = await _client.GetAsync("http://localhost:5152/api/auth/GenerateTestToken");
            tokenResponse.EnsureSuccessStatusCode();
            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
            var jsonObject = JsonConvert.DeserializeObject<dynamic>(tokenJson);
            var token = jsonObject.token.ToString();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            GuestUser CreateGuestUser = new GuestUser
            {
                Name = "New User",
                Username = "newuser123",
                Email = "newuser@example.com",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.MinValue,
            };

            var response = await _client.PostAsJsonAsync("http://localhost:5152/api/GuestUsers", CreateGuestUser);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

    }
}
