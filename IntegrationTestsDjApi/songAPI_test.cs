using dj_api.ApiModels;
using dj_api.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;


//Testi so bili izdelani in izvajani z strani Davida Ajnika
namespace IntegrationTestsDjApi
{
    public class songAPI_test : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public songAPI_test(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }


        [Fact]
        public async Task GetAllSongs_WithValidAuthorization_ReturnsOk()
        {
            var tokenResponse = await _client.GetAsync("http://localhost:5152/api/auth/GenerateTestToken");
            tokenResponse.EnsureSuccessStatusCode();
            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
            dynamic tokenObj = JsonConvert.DeserializeObject(tokenJson);
            string token = tokenObj.token.ToString();

            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.GetAsync("http://localhost:5152/song-old");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
            var songs = JsonConvert.DeserializeObject<List<Song>>(responseContent);

            Assert.NotNull(songs);
            Assert.NotEmpty(songs);  // da prcakuje vsaj en song
        }

        [Fact]
        public async Task GetSongById_ExistingSong_ReturnsOk()
        {
            var tokenResponse = await _client.GetAsync("http://localhost:5152/api/auth/GenerateTestToken");
            tokenResponse.EnsureSuccessStatusCode();
            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
            dynamic tokenObj = JsonConvert.DeserializeObject(tokenJson);
            string token = tokenObj.token.ToString();

            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var existingId = "67d1e6a76a947d7c54ed3640";

            var response = await _client.GetAsync($"http://localhost:5152/api/songs/{existingId}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var song = JsonConvert.DeserializeObject<Song>(content);
            Assert.NotNull(song);
            Assert.Equal(existingId, song.ObjectId);
        }

        [Fact]
        public async Task GetSongById_NonExistingSong_ReturnsInternalServerError()
        {
            var tokenResponse = await _client.GetAsync("http://localhost:5152/api/auth/GenerateTestToken");
            tokenResponse.EnsureSuccessStatusCode();
            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
            dynamic tokenObj = JsonConvert.DeserializeObject(tokenJson);
            string token = tokenObj.token.ToString();

            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var nonExistingId = "64aaaaaaaaaaaaaaaaaaaaaa";
            var response = await _client.GetAsync($"http://localhost:5152/api/songs/{nonExistingId}");

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [Fact]
        public async Task DeleteSong_InsertViaRequestEndpointThenDelete_ReturnsOk()
        {
            var tokenResponse = await _client.GetAsync("/api/auth/GenerateTestToken");
            tokenResponse.EnsureSuccessStatusCode();
            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
            dynamic tokenObj = JsonConvert.DeserializeObject(tokenJson);
            string token = tokenObj.token.ToString();

            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            string uniqueName = "RequestSongTest_" + Guid.NewGuid();

            var requestModel = new SongModel
            {
                Name = uniqueName,
                Artist = "AnyArtist_" + Guid.NewGuid(),
                Genre = "TestGenre"
            };

            var requestJson = JsonConvert.SerializeObject(requestModel);
            var requestContent = new StringContent(requestJson, Encoding.UTF8, "application/json");

            var requestResponse = await _client.PostAsync("/api/songs/request/12345", requestContent);

            Assert.Equal(HttpStatusCode.OK, requestResponse.StatusCode);

            var getAllResponse = await _client.GetAsync("/song-old");
            Assert.Equal(HttpStatusCode.OK, getAllResponse.StatusCode);

            var getAllContent = await getAllResponse.Content.ReadAsStringAsync();
            var allSongs = JsonConvert.DeserializeObject<List<Song>>(getAllContent);
            Assert.NotNull(allSongs);

            var insertedSong = allSongs.FirstOrDefault(s => s.Name == uniqueName);
            Assert.NotNull(insertedSong); // If null => not found, test fails
            string newSongId = insertedSong.ObjectId;
            Assert.False(string.IsNullOrWhiteSpace(newSongId));

            var deleteResponse = await _client.DeleteAsync($"/api/songs/{newSongId}");
            Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

            var getAfterDeleteResponse = await _client.GetAsync($"/api/songs/{newSongId}");
            Assert.Equal(HttpStatusCode.InternalServerError, getAfterDeleteResponse.StatusCode);
        }

        [Fact]
        public async Task UpdateSong_ReturnsOkAndUpdatesData()
        {
            var tokenResponse = await _client.GetAsync("/api/auth/GenerateTestToken");
            tokenResponse.EnsureSuccessStatusCode();
            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
            dynamic tokenObj = JsonConvert.DeserializeObject(tokenJson);
            string token = tokenObj.token.ToString();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            string uniqueName = "PutTest_" + Guid.NewGuid();
            var newSong = new SongModel
            {
                Name = uniqueName,
                Artist = "Artist_" + Guid.NewGuid(),
                Genre = "Genre"
            };
            var newSongJson = JsonConvert.SerializeObject(newSong);
            var newSongContent = new StringContent(newSongJson, Encoding.UTF8, "application/json");
            var createResponse = await _client.PostAsync("/api/songs/request/12345", newSongContent);
            Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

            var getAllResponse = await _client.GetAsync("/song-old");
            Assert.Equal(HttpStatusCode.OK, getAllResponse.StatusCode);
            var getAllContent = await getAllResponse.Content.ReadAsStringAsync();
            var allSongs = JsonConvert.DeserializeObject<List<Song>>(getAllContent);
            var insertedSong = allSongs.FirstOrDefault(s => s.Name == uniqueName);
            Assert.NotNull(insertedSong);
            string songId = insertedSong.ObjectId;

            var updatedSong = new SongModel
            {
                Name = "UpdatedName",
                Artist = "UpdatedArtist",
                Genre = "UpdatedGenre"
            };
            var updateJson = JsonConvert.SerializeObject(updatedSong);
            var updateContent = new StringContent(updateJson, Encoding.UTF8, "application/json");
            var updateResponse = await _client.PutAsync($"/api/songs/{songId}", updateContent);
            Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

            var getUpdatedResponse = await _client.GetAsync($"/api/songs/{songId}");
            Assert.Equal(HttpStatusCode.OK, getUpdatedResponse.StatusCode);
            var updatedContent = await getUpdatedResponse.Content.ReadAsStringAsync();
            var updatedSongResult = JsonConvert.DeserializeObject<Song>(updatedContent);
            Assert.Equal(updatedSong.Name, updatedSongResult.Name);
            Assert.Equal(updatedSong.Artist, updatedSongResult.Artist);
            Assert.Equal(updatedSong.Genre, updatedSongResult.Genre);

            var deleteResponse = await _client.DeleteAsync($"/api/songs/{songId}");
            Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
        }

        [Fact]
        public async Task CreateSong_WithValidData_ReturnsConflictDueToDuplicationCheckBug()
        {
            var tokenResponse = await _client.GetAsync("/api/auth/GenerateTestToken");
            tokenResponse.EnsureSuccessStatusCode();
            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
            dynamic tokenObj = JsonConvert.DeserializeObject(tokenJson);
            string token = tokenObj.token.ToString();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var newSong = new SongModel
            {
                Name = "UniqueSong_" + Guid.NewGuid(),
                Artist = "UniqueArtist_" + Guid.NewGuid(),
                Genre = "Rock"
            };
            var newSongJson = JsonConvert.SerializeObject(newSong);
            var newSongContent = new StringContent(newSongJson, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/api/songs", newSongContent);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task GetAllSongPage_WithValidParameters_ReturnsOkAndNonEmptyList()
        {
            var tokenResponse = await _client.GetAsync("/api/auth/GenerateTestToken");
            tokenResponse.EnsureSuccessStatusCode();
            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
            dynamic tokenObj = JsonConvert.DeserializeObject(tokenJson);
            string token = tokenObj.token.ToString();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.GetAsync("/api/songs?page=1&pageSize=10");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            var paginatedSongs = JsonConvert.DeserializeObject<List<Song>>(content);
            Assert.NotNull(paginatedSongs);
            Assert.NotEmpty(paginatedSongs);
        }

        [Fact]
        public async Task GetAllSongPage_WithValidPagination_ReturnsOkAndNonEmptyList()
        {
            var tokenResponse = await _client.GetAsync("/api/auth/GenerateTestToken");
            tokenResponse.EnsureSuccessStatusCode();
            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
            dynamic tokenObj = JsonConvert.DeserializeObject(tokenJson);
            string token = tokenObj.token.ToString();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            string uniqueName = "PaginatedTest_" + Guid.NewGuid();
            var newSong = new SongModel
            {
                Name = uniqueName,
                Artist = "PaginatedArtist_" + Guid.NewGuid(),
                Genre = "TestGenre"
            };
            var newSongJson = JsonConvert.SerializeObject(newSong);
            var newSongContent = new StringContent(newSongJson, Encoding.UTF8, "application/json");
            var createResponse = await _client.PostAsync("/api/songs/request/12345", newSongContent);
            Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

            var paginatedResponse = await _client.GetAsync("/api/songs?page=1&pageSize=10");
            Assert.Equal(HttpStatusCode.OK, paginatedResponse.StatusCode);
            var paginatedContent = await paginatedResponse.Content.ReadAsStringAsync();
            var songs = JsonConvert.DeserializeObject<List<Song>>(paginatedContent);
            Assert.NotNull(songs);
            Assert.True(songs.Count > 0);

            var allSongsResponse = await _client.GetAsync("/song-old");
            Assert.Equal(HttpStatusCode.OK, allSongsResponse.StatusCode);
            var allSongsContent = await allSongsResponse.Content.ReadAsStringAsync();
            var allSongs = JsonConvert.DeserializeObject<List<Song>>(allSongsContent);
            var insertedSong = allSongs.FirstOrDefault(s => s.Name == uniqueName);
            Assert.NotNull(insertedSong);
            var deleteResponse = await _client.DeleteAsync($"/api/songs/{insertedSong.ObjectId}");
            Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
        }
    }
}
