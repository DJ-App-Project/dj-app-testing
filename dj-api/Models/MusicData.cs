using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace dj_api.Models
{
    [BsonIgnoreExtraElements]
    public class MusicData
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string ObjectId { get; set; } = null!;

        [BsonElement("MusicName")]
        public string MusicName { get; set; } = null!;
        [BsonElement("MusicArtist")]
        public string MusicArtist { get; set; } = null!;
        [BsonElement("MusicGenre")]
        public string MusicGenre { get; set; } = null!;

        [BsonElement("Visible")]
        public bool Visible { get; set; }

        [BsonElement("Votes")]
        public int Votes { get; set; }

        [BsonElement("VotersIDs")]
        public List<string> VotersIDs { get; set; } = null!;

        [BsonElement("IsUserRecommendation")]
        public bool IsUserRecommendation { get; set; }

        [BsonElement("RecommenderID")]
        public string RecommenderID { get; set; } = null!;
    }
}
