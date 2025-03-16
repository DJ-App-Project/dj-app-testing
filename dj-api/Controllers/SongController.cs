using dj_api.ApiModels;
using dj_api.Models;
using dj_api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MongoDB.Bson.Serialization.Attributes;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using System.Threading.Tasks;

[ApiController]
[Route("api/songs")]
public class SongController : ControllerBase
{
    private readonly SongRepository _songRepository;

    public SongController(SongRepository songRepository)
    {
        _songRepository = songRepository;
    }
    [SwaggerOperation(Summary = "DEPRECATED: Get all songs (use paginated version)")]
    [HttpGet("/song-old")]
    [Authorize]
    public async Task<IActionResult> GetAllSongs()
    {
        var songs = await _songRepository.GetAllSongsAsync();
        return Ok(songs);
    }
    [HttpGet("{ObjectId}")]
    [Authorize]
    public async Task<IActionResult> GetSongById(string ObjectId)
    {
        var song = await _songRepository.GetSongByIdAsync(ObjectId);
        if (song == null)
            return NotFound();

        return Ok(song);
    }
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateSong(SongModel newSong)
    {
        if(newSong == null)
        {
            return BadRequest("Song data missing");
        }
        var SongTitleCheck = await _songRepository.FindSongByTitleAsync(newSong.Name);

        
        var SongCheckArtist = await _songRepository.FindSongsByArtistAsync(newSong.Artist);
        if (SongTitleCheck != null || SongCheckArtist != null)
        {
            return Conflict("Song duplicated");
        }

        try
        {
            Song CreateSong = new Song
            {

                Name = newSong.Name,
                Artist = newSong.Artist,
                Genre = newSong.Genre,
                AddedAt = DateTime.UtcNow,
            };
            await _songRepository.CreateSongAsync(CreateSong);
            return Ok(new
            {
                Message = "User created successfully.",
                ObjectId = CreateSong.ObjectId
            });
        }
        catch(Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
    [HttpDelete("{ObjectId}")]
    [Authorize]
    public async Task<IActionResult> DeleteSong (string ObjectId)
    {
        var user = await _songRepository.GetSongByIdAsync(ObjectId);
        if (user == null)
            return NotFound("Song not found"); 

        try
        {
            await _songRepository.DeleteSongAsync(ObjectId);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
    [HttpPut("{ObjectId}")]
    [Authorize]

    public async Task<IActionResult> UpdateSong(string ObjectId,SongModel UpdatedSong)
    {
        var existingSong = await _songRepository.GetSongByIdAsync(ObjectId);
        if(existingSong == null)
        {
            return NotFound("Song doesn't exist");
        }

        Song newSong = new Song
        {
            ObjectId = ObjectId,
            Name = UpdatedSong.Name,
            AddedAt = existingSong.AddedAt,
            Artist = UpdatedSong.Artist,
            Genre = UpdatedSong.Genre
        };

        await _songRepository.UpdateSongAsync(ObjectId, newSong);
        return Ok();
    }
    [SwaggerOperation(Summary = "Get paginated song data")]
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAllSongPage([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (page < 1 || pageSize < 1)
        {
            return BadRequest("Page and pageSize must be greater than 0.");
        }
        var paginatedResult = await _songRepository.GetPaginatedSongsAsync(page, pageSize);

        if (paginatedResult.Count == 0)
        {
            return NotFound("No Users found.");
        }

        return Ok(paginatedResult);
    }

    [HttpPost("request/{eventId}")]
    [Authorize]
    public async Task<IActionResult> RequestNewSong(string eventId, [FromBody] SongModel newSong)
    {
        if (newSong == null)
        {
            return BadRequest("Song data is missing.");
        }

        var existingSong = await _songRepository.FindSongByTitleAsync(newSong.Name);

        if (existingSong != null)
        {
            return Conflict("Song already exists in the Songs collection.");
        }

        var song = new Song
        {
            Name = newSong.Name,

            Artist = newSong.Artist,
            Genre = newSong.Genre,
            AddedAt = DateTime.UtcNow
        };

        await _songRepository.CreateSongAsync(song);

        return Ok(new
        {
            message = "Song added successfully to Songs collection. Ask the event organizer to approve it."
        });
    }
}
