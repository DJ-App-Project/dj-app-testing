using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace dj_api.Models
{
    [BsonIgnoreExtraElements]
    public class Song
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string ObjectId { get; set; }

        [BsonElement("Name")]
        public string Name { get; set; }

        [BsonElement("Artist")]
        public string Artist { get; set; }

        [BsonElement("Genre")]
        public string Genre { get; set; }

        [BsonElement("addedAt")]
        public DateTime AddedAt { get; set; }
    }
}