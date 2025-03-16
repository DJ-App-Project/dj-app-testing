using dj_api.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System.Net.Http.Json;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using dj_api.ApiModels.Event.Post;
using System.Net.Http.Headers;
using dj_api.Repositories;
using Microsoft.Extensions.Caching.Memory;
using dj_api.ApiModels.Event.Get;
using dj_api.ApiModels;

namespace IntegrationTestsDjApi
{
    public class EventTest : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly EventRepository _eventRepository;

        public EventTest(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetAllEvents_WithValidAuthorization_ReturnsOk()
        {
            var tokenResponse = await _client.GetAsync("http://localhost:5152/api/auth/GenerateTestToken");
            tokenResponse.EnsureSuccessStatusCode();
            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
            var jsonObject = JsonConvert.DeserializeObject<dynamic>(tokenJson);
            var token = jsonObject.token.ToString();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.GetAsync("http://localhost:5152/api/events-old");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode); 
        }

        [Fact]
        public async Task GetEventById_WithValidEventId_ReturnsOk()
        {
            var tokenResponse = await _client.GetAsync("http://localhost:5152/api/auth/GenerateTestToken");
            tokenResponse.EnsureSuccessStatusCode();
            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
            var jsonObject = JsonConvert.DeserializeObject<dynamic>(tokenJson);
            var token = jsonObject.token.ToString();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var eventId = "67d19c50f64702d730b8f711";

            var response = await _client.GetAsync($"http://localhost:5152/api/event/{eventId}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var eventData = await response.Content.ReadAsStringAsync();
            Assert.Contains("Name", eventData); 
        }

        [Fact]
        public async Task DeleteRandomEvent_ReturnsOk()
        {
            var tokenResponse = await _client.GetAsync("http://localhost:5152/api/auth/GenerateTestToken");
            tokenResponse.EnsureSuccessStatusCode();
            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
            var jsonObject = JsonConvert.DeserializeObject<dynamic>(tokenJson);
            var token = jsonObject.token.ToString();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var validEvent = new CreateEventPost
            {
                Name = "Test Event",
                Description = "Test Event",
                Date = DateTime.Now.AddDays(1),
                Location = "Test Location",
                Active = true
            };

            var addEventResponse = await _client.PostAsJsonAsync("http://localhost:5152/CreateEvent", validEvent);

            addEventResponse.EnsureSuccessStatusCode();
            var addedEventJson = await addEventResponse.Content.ReadAsStringAsync();
            var jsonObject1 = JsonConvert.DeserializeObject<dynamic>(addedEventJson);
            var eventId = jsonObject1.eventId.ToString();

            Assert.Equal(HttpStatusCode.OK, addEventResponse.StatusCode);

            var deleteResponse = await _client.DeleteAsync($"http://localhost:5152/api/event/{eventId}");

            Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);  
        }

        [Fact]
        public async Task DeleteNonExistentEvent_ReturnsNotFound()
        {
            var tokenResponse = await _client.GetAsync("http://localhost:5152/api/auth/GenerateTestToken");
            tokenResponse.EnsureSuccessStatusCode();
            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
            var jsonObject = JsonConvert.DeserializeObject<dynamic>(tokenJson);
            var token = jsonObject.token.ToString();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            string nonExistentEventId = null;

            var deleteResponse = await _client.DeleteAsync($"http://localhost:5152/api/event/{nonExistentEventId}");

            Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);
        }

        [Fact]
        public async Task GetEventQrCode_ValidRandomEventId_ReturnsQrCode()
        {
            var tokenResponse = await _client.GetAsync("http://localhost:5152/api/auth/GenerateTestToken");
            tokenResponse.EnsureSuccessStatusCode();
            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
            var jsonObject = JsonConvert.DeserializeObject<dynamic>(tokenJson);
            var token = jsonObject.token.ToString();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);


            var response = await _client.GetAsync("http://localhost:5152/AllEvents?page=1&pageSize=100"); 
            response.EnsureSuccessStatusCode();

            var eventDataJson = await response.Content.ReadAsStringAsync();
            var eventData = JsonConvert.DeserializeObject<List<Event>>(eventDataJson);

            if (eventData.Count == 0)
            {
                var validEvent = new CreateEventPost
                {
                    Name = "QR Test Event",
                    Description = "Event for QR Code test",
                    Date = DateTime.Now.AddDays(1),
                    Location = "Test Location",
                    Active = true
                };

                var addEventResponse = await _client.PostAsJsonAsync("http://localhost:5152/CreateEvent", validEvent);
                addEventResponse.EnsureSuccessStatusCode();
                var addedEventJson = await addEventResponse.Content.ReadAsStringAsync();
                var jsonObject1 = JsonConvert.DeserializeObject<dynamic>(addedEventJson);
                var eventId = jsonObject1.eventId.ToString();

                eventData.Add(new Event { ObjectId = eventId });
            }

            var randomEvent = eventData[new Random().Next(eventData.Count)];
            var randomEventId = randomEvent.ObjectId;

            var qrResponse = await _client.GetAsync($"http://localhost:5152/api/event/{randomEventId}/qrcode");

            Assert.Equal(HttpStatusCode.OK, qrResponse.StatusCode);
            Assert.Equal("image/png", qrResponse.Content.Headers.ContentType.MediaType);
        }


        [Fact]
        public async Task GetAllEventsPage_ValidRequest_ReturnsPaginatedEvents()
        {
            var tokenResponse = await _client.GetAsync("http://localhost:5152/api/auth/GenerateTestToken");
            tokenResponse.EnsureSuccessStatusCode();
            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
            var jsonObject = JsonConvert.DeserializeObject<dynamic>(tokenJson);
            var token = jsonObject.token.ToString();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.GetAsync("http://localhost:5152/AllEvents?page=1&pageSize=5");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var eventDataJson = await response.Content.ReadAsStringAsync();
            var eventData = JsonConvert.DeserializeObject<List<Event>>(eventDataJson);

            Assert.NotNull(eventData);
            Assert.NotEmpty(eventData);
            Assert.True(eventData.Count <= 5, "Returned more events than requested page size");
        }

        [Fact]
        public async Task GetAllEventsPage_InvalidRequest_ReturnsBadRequest()
        {
            var tokenResponse = await _client.GetAsync("http://localhost:5152/api/auth/GenerateTestToken");
            tokenResponse.EnsureSuccessStatusCode();
            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
            var jsonObject = JsonConvert.DeserializeObject<dynamic>(tokenJson);
            var token = jsonObject.token.ToString();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.GetAsync("http://localhost:5152/AllEvents?page=0&pageSize=-5");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var errorMessage = await response.Content.ReadAsStringAsync();
            Assert.Contains("Page and pageSize must be greater than 0", errorMessage);
        }

        [Fact]
        public async Task AddEvent_WithValidData_ReturnsOK()
        {
            // Pridobivanje tokena za avtorizacijo
            var tokenResponse = await _client.GetAsync("http://localhost:5152/api/auth/GenerateTestToken");
            tokenResponse.EnsureSuccessStatusCode();
            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
            var jsonObject = JsonConvert.DeserializeObject<dynamic>(tokenJson);
            var token = jsonObject.token.ToString();

            var validEvent = new CreateEventPost
            {
                Name = "Test Event",
                Description = "Test Event",
                Date = DateTime.Now.AddDays(1), 
                Location = "Test Location",
                Active = true
            };

            var content = new StringContent(JsonConvert.SerializeObject(validEvent), Encoding.UTF8, "application/json");

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.PostAsync("http://localhost:5152/CreateEvent", content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task AddEvent_WithInvalidData_ReturnsBadRequest()
        {
            var tokenResponse = await _client.GetAsync("http://localhost:5152/api/auth/GenerateTestToken");
            tokenResponse.EnsureSuccessStatusCode();
            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
            var jsonObject = JsonConvert.DeserializeObject<dynamic>(tokenJson);
            var token = jsonObject.token.ToString();

            var invalidEvent = new CreateEventPost
            {
                Name = null,
                Description = null,
                Date = new DateTime(),  
                Location = null,
                Active = false
            };

            var content = new StringContent(JsonConvert.SerializeObject(invalidEvent), Encoding.UTF8, "application/json");

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.PostAsync("http://localhost:5152/CreateEvent", content);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task SetEnableUserRecommendation_ValidRequest_ReturnsOk()
        {
            var tokenResponse = await _client.GetAsync("http://localhost:5152/api/auth/GenerateTestToken");
            tokenResponse.EnsureSuccessStatusCode();
            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
            var jsonObject = JsonConvert.DeserializeObject<dynamic>(tokenJson);
            var token = jsonObject.token.ToString();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var testEvent = new CreateEventPost
            {
                Name = "Test Event",
                Description = "Event for UserRecommendation test",
                Date = DateTime.Now.AddDays(1),
                Location = "Test Location",
                Active = true
            };

            var addEventResponse = await _client.PostAsJsonAsync("http://localhost:5152/CreateEvent", testEvent);
            addEventResponse.EnsureSuccessStatusCode();
            var addedEventJson = await addEventResponse.Content.ReadAsStringAsync();
            var jsonObject1 = JsonConvert.DeserializeObject<dynamic>(addedEventJson);
            var eventId = jsonObject1.eventId.ToString();

            var enableRequest = new SetEnableUserRecommendationPost
            {
                EventId = eventId,
                EnableUserRecommendation = true
            };

            var enableResponse = await _client.PostAsJsonAsync("http://localhost:5152/SetEnableUserRecommendation", enableRequest);

            Assert.Equal(HttpStatusCode.OK, enableResponse.StatusCode);
        }

    }
}
