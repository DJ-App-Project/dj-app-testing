using MongoDB.Bson.Serialization.Attributes;

namespace dj_api.ApiModels.Event.Post
{
    public class AddMusicToEventModelPost
    {
        public string EvendId { get; set; } = null!;
        public string MusicName { get; set; } = null!; 
        public string MusicArtist { get; set; } = null!;
        public string MusicGenre { get; set; } = null!;
        public bool Visible { get; set; }
      
        
    }
}
