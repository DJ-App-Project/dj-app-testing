using dj_api.ApiModels;
using dj_api.Authentication;
using dj_api.Models;
using dj_api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using System.Security.Claims;

namespace dj_api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : Controller
    {
        private readonly UserRepository _userRepository;
        private readonly TokenService _tokenService;
        public AuthController(UserRepository userRepository,TokenService tokenService)
        {
            _tokenService = tokenService;
            _userRepository = userRepository;
        }
        [HttpGet("GenerateTestToken")]
        public IActionResult Login()
        {
            var user = new User
            {
                ObjectId = "67d19c50f64702d730b8f646",  
                Name = "Glenn",  
                Email = "qsoto@gmail.com"
            };
            var token = _tokenService.GenerateJwtToken(user);
            return Ok(new { Token = token });
        }
        [HttpPost("login")]
        public async Task<IActionResult> UserLogin([FromBody] LoginModel loginDto)
        {
            var user = await _userRepository.Authenticate(loginDto.username, loginDto.password);

            if (user == null)
            {
                return Unauthorized(new { error = "Invalid username or password" });
            }
            

            
            var token = _tokenService.GenerateJwtToken(user);

            return Ok(new
            {
                token = token,
                user = new
                {
                    ObjectId = user.ObjectId,
                    name = user.Name,
                    username = user.Name,
                    email = user.Email,
                    familyName = user.FamilyName,
                    imageUrl= user.ImageUrl,
                }
            });
        }
        [HttpPost("Register")]
        public async Task<IActionResult> UserRegister([FromBody] RegisterModel registerDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userRepository.FindUserByEmail(registerDto.email);
            if (user != null)
            {
                return Conflict("Email is already registered.");
            }
            user = await _userRepository.FindUserByUsername(registerDto.username);
            if (user != null)
            {
                return Conflict("Username is already registered.");
            }
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(registerDto.password);

            var NewUser = new User
            {
               
               
                Name = registerDto.name,
                FamilyName = registerDto.familyName,
                ImageUrl = registerDto.imageUrl,
                Username = registerDto.username,
                Email = registerDto.email,
                Password = hashedPassword,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.MinValue,

            };

            //need to check if ok
            try
            {

           
            await _userRepository.CreateUserAsync(NewUser!);
                var token = _tokenService.GenerateJwtToken(NewUser);
                return Ok(new
                {
                    token = token,
                    user = new
                    {
                        ObjectId = NewUser.ObjectId,
                        username = NewUser.Username,
                        name = NewUser.Name,
                        email = NewUser.Email,
                        familyName = NewUser.FamilyName,
                        imageUrl = NewUser.ImageUrl,
                    }
                });
            }
            catch
            {
                return Conflict("Error when creating user.");
            }
           
        }
        
    }
}
