﻿using MongoDB.Bson.Serialization.Attributes;

namespace CoronavirusWebScraper.Data.Models
{
    public class VaccineType
    {
        [BsonElement("comirnaty")]
        public int Comirnaty { get; set; }

        [BsonElement("moderna")]
        public int Moderna { get; set; }

        [BsonElement("astrazeneca")]
        public int AstraZeneca { get; set; }

        [BsonElement("janssen")]
        public int Janssen { get; set; }
    }
}