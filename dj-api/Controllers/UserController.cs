using dj_api.ApiModels;
using dj_api.Models;
using dj_api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

[ApiController]
[Route("api/users")]

// API za delo z uporabniki
public class UserController : ControllerBase
{
    private readonly UserRepository _userRepository;

    public UserController(UserRepository userRepository) // konstruktor za UserController
    {
        _userRepository = userRepository;
    }


    [SwaggerOperation(Summary = "DEPRECATED: Get all users (use paginated version)")]
    [HttpGet("/user-old")]
    [Authorize]
    public async Task<IActionResult> GetAllUsers()// GET api za vse uporabnike
    {
        var users = await _userRepository.GetAllUsersAsync();
        return Ok(users);
    }
    [SwaggerOperation(Summary = "Get paginated user data")]
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAllUserPage([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (page < 1 || pageSize < 1)
        {
            return BadRequest("Page and pageSize must be greater than 0.");
        }

        var paginatedResult = await _userRepository.GetPaginatedUserAsync(page, pageSize);

        if (paginatedResult.Count == 0)
        {
            return NotFound("No Users found.");
        }

        return Ok(paginatedResult);
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetUserById(string id) // GET api za enega uporabnika po ID
    {
        var user = await _userRepository.GetUserByIdAsync(id);
        if (user == null)
            return NotFound("User not found"); // če uporabnik ni najden, vrni NotFound
        return Ok(user);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateUser(UserModel user) // POST api za kreiranje novega uporabnika
    {
        if (user == null)
            return BadRequest("User data missing"); 

        var userCheck = await _userRepository.FindUserByEmail(user.email);
        if (userCheck != null)
        {
            return Conflict("Email is already registered.");
        }
        userCheck = await _userRepository.FindUserByUsername(user.username);
        if (userCheck != null)
        {
            return Conflict("Username is already registered.");
        }
        try
        {
            User CreateUser = new User
            {
                Name = user.name,
                FamilyName = user.familyName,
                ImageUrl = user.imageUrl,
                Username = user.username,
                Email = user.email,
                Password = user.password,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.MinValue,
            };
            await _userRepository.CreateUserAsync(CreateUser);
            return Ok(new
            {
                Message = "User created successfully.",
                ObjectId = CreateUser.ObjectId
            });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message); 
        }
    }


	[HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteUser(string id) // DELETE api za brisanje uporabnika po ID
    {
        var user = await _userRepository.GetUserByIdAsync(id);
        if (user == null)
            return NotFound("User not found"); // če uporabnik ni najden, vrni NotFound

        try
        {
            await _userRepository.DeleteUserAsync(id);
            return Ok(); // če je uporabnik uspešno izbrisan, vrni NoContent
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message); // če je prišlo do napake, vrni BadRequest z sporočilom napake
        }
    }

	[HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateUser(string id, UserModel UpdatedUser) // PUT api za posodabljanje uporabnika po ID
    {
       
        var existingUser = await _userRepository.GetUserByIdAsync(id);
        if (existingUser == null)
            return NotFound("User doesn't exist"); // če uporabnik ni najden, vrni NotFound

        User a = new User
        {
            ObjectId = id,
            Name = UpdatedUser.name,
            FamilyName = UpdatedUser.familyName,
            ImageUrl = UpdatedUser.imageUrl,
            Username = UpdatedUser.username,
            Email = UpdatedUser.email,
            Password = UpdatedUser.password,
            CreatedAt = existingUser.CreatedAt,
            UpdatedAt = DateTime.UtcNow,
        };
        

        await _userRepository.UpdateUserAsync(id, a);
        return Ok(); // če je uporabnik uspešno posodobljen, vrni NoContent
    }
    [SwaggerOperation(Summary = "Uporabi ko user sam sebe popravlja")]
    [HttpPut]
    [Authorize]
    public async Task<IActionResult> UpdateUser( UserModel UpdatedUser) // PUT api za posodabljanje uporabnika po ID
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "User authentication required." });
        }
        var existingUser = await _userRepository.GetUserByIdAsync(userId);
        if (existingUser == null)
            return NotFound("User doesn't exist"); // če uporabnik ni najden, vrni NotFound

        User a = new User
        {
            ObjectId = userId,
            Name = UpdatedUser.name,
            FamilyName = UpdatedUser.familyName,
            ImageUrl = UpdatedUser.imageUrl,
            Username = UpdatedUser.username,
            Email = UpdatedUser.email,
            Password = UpdatedUser.password,
            CreatedAt = existingUser.CreatedAt,
            UpdatedAt = DateTime.UtcNow,
        };


        await _userRepository.UpdateUserAsync(userId, a);
        return Ok(); // če je uporabnik uspešno posodobljen, vrni NoContent
    }
}
