using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace dj_api.Models
{
    [BsonIgnoreExtraElements]
    public class Event
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string ObjectId { get; set; } = null!;

        [BsonElement("ID")]
        public string Id { get; set; } = null!;

        [BsonElement("DJID")]
        public string DJId { get; set; } = null!;

        [BsonElement("QRCode")] 
        public string QRCodeText { get; set; } = null!;
        [BsonElement("Name")]
        public string Name { get; set; } = null!;
        [BsonElement("Description")]
        public string Description { get; set; } = null!;
        [BsonElement("Date")]
        public DateTime Date { get; set; } = new DateTime()!;
        [BsonElement("Location")]
        public string Location { get; set; } = null!;
        [BsonElement("Active")]
        public bool Active { get; set; } = false!;

        [BsonElement("MusicConfig")]
        public MusicConfigClass MusicConfig { get; set; } = null!;

        public class MusicConfigClass
        {
            [BsonElement("MusicPlaylist")]
            public List<MusicData> MusicPlaylist { get; set; } = new List<MusicData>()!;

            [BsonElement("EnableUserRecommendation")]
            public bool EnableUserRecommendation { get; set; } = false;
        }

    }
}
