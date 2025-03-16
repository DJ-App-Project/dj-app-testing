using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace dj_api.Models
{
    [BsonIgnoreExtraElements]
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string ObjectId { get; set; } = null!;

        [BsonElement("ID")]
        public int Id { get; set; }

        [BsonElement("Name")]
        public string Name { get; set; } = null!;

        [BsonElement("FamilyName")]
        public string FamilyName { get; set; } = null!;

        [BsonElement("ImageUrl")]
        public string ImageUrl { get; set; } = null!;

        [BsonElement("Username")]
        public string Username { get; set; } = null!;

        [BsonElement("Email")]
        public string Email { get; set; } = null!;

        [BsonElement("Password")]
        public string Password { get; set; } = null!;

        [BsonElement("CreatedAt")]
        public DateTime CreatedAt { get; set; }

        [BsonElement("UpdatedAt")]
        public DateTime UpdatedAt { get; set; }

        //[BsonElement("DeletedAt")]
        //public DateTime DeletedAt { get; set; }
    }
}