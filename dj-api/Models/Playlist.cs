using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace dj_api.Models
{
    [BsonIgnoreExtraElements]
    public class Playlist
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string ObjectId { get; set; } = null!;
        [BsonElement("UserID")]
        public string UserID { get; set; } = null!;
        [BsonElement("MusicList")]
        public string[] MusicList { get; set; } = []!;
    }
}
