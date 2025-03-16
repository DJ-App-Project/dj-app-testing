using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using static dj_api.Models.Event;

namespace dj_api.ApiModels.Event.Get
{
    public class EventGet
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string ObjectId { get; set; }
        [BsonElement("DJID")]
        public string DJId { get; set; }
        [BsonElement("QRCode")]
        public string QRCodeText { get; set; }

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
        [BsonElement("EnableUserRecommendation")]

        public bool EnableUserRecommendation { get; set; } = false;
    }
}
