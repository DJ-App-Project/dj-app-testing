using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace dj_api.ApiModels.Event.Get
{
    public class MusicGet
    {
        

        [BsonElement("MusicName")]
        public string MusicName { get; set; } = null!;
        [BsonElement("MusicArtist")]
        public string MusicArtist { get; set; } = null!;
        [BsonElement("MusicGenre")]
        public string MusicGenre { get; set; } = null!;

        [BsonElement("Votes")]
        public int Votes { get; set; }

        [BsonElement("Visible")]
        public bool Visible { get; set; }

        [BsonElement("IsUserRecommendation")]
        public bool IsUserRecommendation { get; set; } 
    }
}
