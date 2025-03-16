using dj_api.ApiModels;
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

namespace IntegrationTestsDjApi
{
    public class AuthTest : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public AuthTest(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GenerateTestToken_ReturnsToken()
        {
            var response = await _client.GetAsync("http://localhost:5152/api/auth/GenerateTestToken");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseJson = await response.Content.ReadAsStringAsync();

            Assert.Contains("token", responseJson);

            var jsonObject = JsonConvert.DeserializeObject<dynamic>(responseJson);
            var token = jsonObject.token.ToString();

            Assert.True(token.Split('.').Length == 3, "Token");
        }

        [Fact]
        public async Task GenerateTestToken_ReturnsToken_AndHandlesErrors()
        {
            try
            {
                var response = await _client.GetAsync("http://localhost:5152/api/auth/GenerateTestToken");

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    Assert.Fail($"Expected HTTP 200 OK, but received {response.StatusCode}");
                }

                var responseJson = await response.Content.ReadAsStringAsync();

                if (!responseJson.Contains("token"))
                {
                    Assert.Fail("Response does not contain 'token'. Response content: " + responseJson);
                }

                var jsonObject = JsonConvert.DeserializeObject<dynamic>(responseJson);
                var token = jsonObject.token.ToString();

                Assert.True(token.Split('.').Length == 3, "Token format is invalid. Token: " + token);

            }
            catch (Exception ex)
            {
                Assert.Fail($"Test failed due to an exception: {ex.Message}. StackTrace: {ex.StackTrace}");
            }
        }


        [Fact]
        public async Task UserRegister_ValidData_ReturnsOkWithToken()
        {
            var uniqueId = Guid.NewGuid().ToString();

            var registerDto = new RegisterModel
            {
                name = $"Test{uniqueId}",                    
                familyName = $"Example{uniqueId}",            
                username = $"testexample{uniqueId.Substring(0, 8)}",  
                email = $"testexample{uniqueId}@example.com",  
                password = "Pass321",                       
                imageUrl = $"http://example.com/{uniqueId}.jpg"  
            };


            var tokenResponse = await _client.GetAsync("http://localhost:5152/api/auth/GenerateTestToken");
            tokenResponse.EnsureSuccessStatusCode();
            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
            var jsonObject = JsonConvert.DeserializeObject<dynamic>(tokenJson);
            var token = jsonObject.token.ToString();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.PostAsJsonAsync("http://localhost:5152/api/auth/Register", registerDto);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseJson = await response.Content.ReadAsStringAsync();
            var jsonObject1 = JsonConvert.DeserializeObject<dynamic>(responseJson);

            Assert.Contains("token", responseJson);

            Assert.Equal(registerDto.username, jsonObject1.user.username.ToString());
            Assert.Equal(registerDto.email, jsonObject1.user.email.ToString());
            Assert.Equal(registerDto.name, jsonObject1.user.name.ToString());
        }

        [Fact]
        public async Task UserRegister_ExistingEmail_ReturnsConflict()
        {
            var registerDto = new RegisterModel
            {
                name = "Test",
                familyName = "Example",
                username = "testexample",
                email = "testexample@example.com",
                password = "Pass321",
                imageUrl = "QR321"
            };

            var tokenResponse = await _client.GetAsync("http://localhost:5152/api/auth/GenerateTestToken");
            tokenResponse.EnsureSuccessStatusCode();
            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
            var jsonObject = JsonConvert.DeserializeObject<dynamic>(tokenJson);
            var token = jsonObject.token.ToString();

            var response = await _client.PostAsJsonAsync("http://localhost:5152/api/auth/Register", registerDto);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

            var responseJson = await response.Content.ReadAsStringAsync();
            Assert.Contains("Email is already registered.", responseJson);
        }


    }
}
