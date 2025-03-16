using Microsoft.AspNetCore.Mvc.Testing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using dj_api.ApiModels;
using System.Net.Http.Json;

namespace IntegrationTestsDjApi
{
    public class PlayListTest : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public PlayListTest(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetAllPlayList_ReturnsOk()
        {
            var tokenResponse = await _client.GetAsync("http://localhost:5152/api/auth/GenerateTestToken");
            tokenResponse.EnsureSuccessStatusCode();
            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
            var jsonObject = JsonConvert.DeserializeObject<dynamic>(tokenJson);
            var token = jsonObject.token.ToString();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.GetAsync("http://localhost:5152/api/playlist/playlist-old");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var playlistsJson = await response.Content.ReadAsStringAsync();

            Assert.NotEmpty(playlistsJson);
        }

    }
}
