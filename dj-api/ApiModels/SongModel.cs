using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace dj_api.ApiModels
{
    public class SongModel
    {
        [Required(ErrorMessage = "Title is required")]
        public string Name { get; set; } = null!;


        [Required(ErrorMessage = "Artist is required")]
        public string Artist { get; set; } = null!;

        [Required(ErrorMessage = "Genre is required")]
        public string Genre { get; set; } = null!;

    }
}
