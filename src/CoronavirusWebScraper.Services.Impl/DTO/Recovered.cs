﻿using MongoDB.Bson.Serialization.Attributes;

namespace CoronavirusWebScraper.Services.Impl.DTO
{
    public class TotalAndLast
    {
        [BsonElement("total")]
        public int Total { get; set; }

        [BsonElement("last")]
        public int Last { get; set; }
    }
}